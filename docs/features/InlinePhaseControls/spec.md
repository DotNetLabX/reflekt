# Inline Phase Controls

**Traces to:** `docs/specs/v2.md` §4.5
**Source:** Scratch
**Dependencies:** None (independent of Identity, Teams, and Configurable Columns features)
**Status:** Ready

---

## Purpose

In v1, the Scrum Master must leave the retro room and navigate to a separate sprint manager page to advance phases or close the sprint. This breaks flow during a live retro — every phase transition requires a context switch away from the team. V2 moves all live-retro controls (phase advancement, sprint closure) into the sprint board itself, so the SM never leaves the room. A participant readiness indicator gives the SM visibility into pacing without blocking the retro.

## Entities

This feature introduces no new persistent entities and modifies no existing entity schemas.

### Readiness State (ephemeral)

During Writing and Voting phases, participants can signal readiness ("I'm done"). This state is:

- Tracked in server memory per sprint, not persisted to the database
- Scoped to the current phase — automatically cleared when the phase advances
- Lost on participant disconnection — participants must re-signal after reconnecting
- Not associated with any persistent entity

## User Flows

```
Flow 1: SM advances phase from the retro room
1. SM is in the retro room during an active sprint.
2. The top bar shows the current phase name in the phase stepper and a "Next: {phase}" button (right-aligned, SM-only).
3. SM clicks "Next: {phase}".
4. A confirmation dialog appears: "Advance to {phase}? This cannot be undone."
5. SM confirms.
6. The phase advances. All participants see the phase stepper update in real-time.
7. The "Next: {phase}" button label updates to show the subsequent phase.
```

```
Flow 2: SM advances from Voting to Discussing (with voting warning)
1. SM is in the retro room during the Voting phase.
2. SM clicks "Next: Discussing".
3. A confirmation dialog appears with context: "N participants haven't voted yet. Advance to Discussing anyway?"
   The count is derived from participants who have cast zero votes vs. total active participants.
4. SM can proceed (advance) or cancel.
5. If SM proceeds, the phase advances normally.
```

```
Flow 3: SM advances from Discussing to Reporting (with discussion gate)
1. SM is in the retro room during the Discussing phase.
2. SM clicks "Next: Reporting".
3. If fewer than 2 groups have been discussed, the system rejects the advance and shows:
   "At least 2 groups must be discussed before advancing to Reporting. Currently discussed: N."
4. If 2+ groups are discussed, a confirmation dialog appears:
   "Advance to Reporting? N groups have been discussed."
5. SM confirms. The currently focused group (if any) is auto-marked as discussed before the transition.
```

```
Flow 4: SM closes the sprint from the retro room
1. SM is in the retro room during the Reporting phase.
2. Instead of "Next: {phase}" (Reporting is the terminal phase), a "Close Sprint" button is shown.
3. SM clicks "Close Sprint".
4. A confirmation dialog appears: "Close this sprint? No more changes will be allowed."
5. SM confirms.
6. The sprint state transitions from Active to Closed.
7. All participants receive a real-time notification: "This retro has been closed by the Scrum Master."
8. The board becomes fully read-only for all participants. They remain on the board and can
   still view the report and copy markdown.
```

```
Flow 5: Participant signals readiness
1. During Writing or Voting phase, a participant sees an "I'm done" button.
2. Participant clicks "I'm done".
3. The button state changes to indicate readiness (visual toggle).
4. All participants see the readiness badge update (e.g., "4/6 done").
5. If the participant wants to un-ready (thought of something else), they click the button again to toggle back.
```

```
Flow 6: SM views readiness information
1. During Writing or Voting phase, the SM sees the readiness badge in the top bar: "4/6 done".
2. The SM hovers over the badge to see a tooltip listing which participants are done and which are not.
3. The readiness count is informational — it does not block the SM from advancing.
```

```
Flow 7: Phase advances and readiness resets
1. SM advances the phase.
2. All readiness state is cleared.
3. If the new phase supports readiness (Voting), the badge resets to "0/N done" and all
   participants see their "I'm done" button reset to the not-ready state.
4. If the new phase does not support readiness (Grouping, Discussing, Reporting), the
   readiness badge and "I'm done" button are hidden.
```

## Top Bar Layout

The current retro room header is a stack of independent components (sprint name, phase stepper, participant list). This feature restructures it into a consolidated 3-row top bar:

| Row | Left | Right |
|-----|------|-------|
| **1** | Sprint name | Readiness badge (Writing/Voting) + Votes remaining badge (Voting) + "Next: {phase}" button (SM) or "Close Sprint" button (SM, Reporting only) |
| **2** | Phase stepper (all 5 phases, centered, display-only) | |
| **3** | Participant list (name badges) | |

Layout rules:

- Non-SM participants see everything in the top bar except the advance/close button.
- During Reporting, the advance button is replaced by a "Close Sprint" button.
- The readiness badge is hidden when there is only 1 participant in the sprint (no value in "0/1 done").
- The votes remaining badge is only visible during the Voting phase (behavior change — v1 shows it in all phases; v2 scopes it to Voting where it's relevant).
- The phase stepper is display-only for all users — not clickable.

## API Surface

| Method | Route | Auth | Purpose | Status codes |
|--------|-------|------|---------|--------------|
| POST | /api/teams/{teamId}/sprints/{sprintId}/advance-phase | SM | Advance to next phase | 200, 403, 409 |
| POST | /api/teams/{teamId}/sprints/{sprintId}/close | SM | Close the sprint | 200, 403, 409 |

Both endpoint routes and their handler logic already exist. To call them from the retro room (which only knows the sprint ID, not the team ID), the sprint summary response must be extended to include the team ID. This is the only change to these endpoints.

Sprint closure currently raises a domain event but does not broadcast to connected participants. This feature requires that sprint closure broadcasts the SprintClosed event to all participants in the sprint's SignalR group — following the same pattern as phase advancement, which already broadcasts PhaseChanged.

**Readiness** is handled entirely through SignalR (see below) because it is ephemeral state with no persistence requirement. No REST endpoint is needed.

**Voting participation count** for the Voting → Discussing confirmation dialog (how many participants haven't voted) cannot be computed client-side — votes are anonymous and the frontend only sees aggregate counts per group, not per participant. The server must provide this count. The simplest approach: when the SM requests a phase advance from Voting, the response includes the count of participants who have cast zero votes. The frontend uses this to populate the confirmation dialog.

### Sprint Manager Page Changes

The following controls are **removed** from the sprint manager page:

- "Advance Phase" button
- "Close Sprint" button

The sprint manager page retains: sprint creation, sprint list, sprint settings (name, votes per participant, column configuration), and participant management (kick).

## SignalR Events

### New Events

| Event | Payload | Trigger |
|-------|---------|---------|
| ReadinessChanged | `{ participantId, displayName, isReady, readyCount, totalCount }` | Participant toggles readiness |
| ReadinessReset | `{ }` | Phase advances (server clears all readiness) |
| ReadinessSnapshot | `{ readyParticipantIds, readyCount, totalCount }` | Client joins the sprint's SignalR group (sent only to the joining client) |
| SprintClosed | `{ sprintId }` | SM closes the sprint |

### Existing Events (no changes)

| Event | Note |
|-------|------|
| PhaseChanged | No payload change. Frontend now handles this in the retro room to update the advance button and reset readiness UI. |

### Hub Methods (client to server)

| Method | Parameters | Auth | Purpose |
|--------|-----------|------|---------|
| ToggleReady | sprintId | Any participant | Toggle the calling participant's readiness state for the current phase |

### Readiness on Reconnect

Readiness state is ephemeral and not available through any REST endpoint. When a client joins (or rejoins) the sprint's SignalR group, the server immediately sends a ReadinessSnapshot event containing the list of ready participant IDs and the current counts. This allows reconnecting clients to render the correct badge state without a separate fetch. The snapshot is sent as part of the group-join flow, not on a separate request.

## Business Rules

1. Only the Scrum Master can advance phases or close the sprint. Other participants see the phase status but no action controls.
2. Phase advancement follows the existing sequence: Writing → Grouping → Voting → Discussing → Reporting. No phase can be skipped or revisited.
3. Every phase transition requires an explicit confirmation from the SM via a dialog. There is no silent or automatic phase advancement.
4. The Discussing → Reporting advance gate is preserved: at least 2 groups must be marked as discussed. The system rejects the advance if this minimum is not met.
5. When advancing from Discussing, the currently focused group (if any) is auto-marked as discussed before the transition occurs.
6. The Voting → Discussing confirmation dialog shows how many participants have not cast any votes. This is a soft warning — the SM can proceed regardless.
7. "Close Sprint" is a state transition (Active → Closed), not a phase advance. It appears as a separate action only during the Reporting phase.
8. Sprint closure is broadcast to all connected participants in real-time. The board becomes fully read-only. Participants remain on the board and can still view the report and copy markdown.
9. Participants can signal readiness ("I'm done") during Writing and Voting phases only. The button is hidden in all other phases.
10. Readiness is toggleable — participants can un-ready by clicking the button again.
11. Readiness state is ephemeral: not persisted, cleared on phase advance, lost on participant disconnection.
12. The readiness badge shows a count ("4/6 done"), visible to all participants. The SM can see individual participant names via tooltip.
13. The readiness badge is hidden when there is only 1 participant in the sprint.
14. Readiness does not gate phase advancement — it is purely informational. The SM can advance at any time regardless of the readiness count.
15. Phase advancement and sprint closure controls are removed from the sprint manager page. The retro room is the single location for all live-retro controls.

## Acceptance Criteria

- [ ] The retro room top bar shows a "Next: {phase}" button visible only to the SM during phases Writing through Discussing.
- [ ] Non-SM participants see the current phase in the stepper but no advance or close button.
- [ ] Clicking "Next: {phase}" shows a confirmation dialog before advancing.
- [ ] The sprint summary response includes the team ID so the retro room can call team-scoped endpoints.
- [ ] The confirmation dialog for Voting → Discussing shows the count of participants who haven't voted (server-provided): "N participants haven't voted yet. Advance to Discussing anyway?"
- [ ] The confirmation dialog for Discussing → Reporting shows the discussed group count.
- [ ] Advancing from Discussing → Reporting is rejected if fewer than 2 groups are discussed, with an error message showing the current count.
- [ ] The currently focused group is auto-marked as discussed when advancing from Discussing.
- [ ] During the Reporting phase, a "Close Sprint" button replaces the advance button (SM only).
- [ ] Clicking "Close Sprint" shows a confirmation dialog: "Close this sprint? No more changes will be allowed."
- [ ] After sprint closure, all connected participants receive a real-time notification via SprintClosed event and the board becomes read-only.
- [ ] Participants remain on the board after closure and can view the report and copy markdown.
- [ ] During Writing and Voting phases, all participants see an "I'm done" button.
- [ ] Clicking "I'm done" toggles the participant's readiness state and broadcasts ReadinessChanged to all participants.
- [ ] The readiness badge ("4/6 done") is visible to all participants during Writing and Voting phases.
- [ ] The SM can hover over the readiness badge to see which participants are done (tooltip with names).
- [ ] Readiness state resets when the phase advances — the badge resets and the "I'm done" button returns to the not-ready state.
- [ ] Readiness state is not persisted — a reconnecting participant starts as not-ready and must re-signal.
- [ ] On reconnect, the client receives the current readiness snapshot and renders the correct badge state.
- [ ] The readiness badge is hidden when there is only 1 participant.
- [ ] The readiness badge and "I'm done" button are hidden during Grouping, Discussing, and Reporting phases.
- [ ] The phase stepper remains display-only — it is not clickable for any user.
- [ ] Phase transitions are broadcast via the existing PhaseChanged SignalR event to all participants in real-time.
- [ ] Sprint closure is broadcast via a new SprintClosed SignalR event to all participants.
- [ ] The "Advance Phase" and "Close Sprint" buttons are removed from the sprint manager page.
- [ ] The sprint manager page retains sprint creation, sprint list, sprint settings, and participant management.

## Out of Scope

- **Phase revert / undo** — Parked (backlog #22). Confirmation dialogs and the discussion gate are the safety nets. No competitor offers phase revert.
- **Auto-advance on timer expiry** — Timer feature (#29) is a separate spec. Timer alerts but never auto-advances.
- **Readiness as a hard gate** — Competitor research (Retrium, Parabol) shows hard gates let a single AFK participant stall the retro. Readiness is informational only.
- **Per-participant voting breakdown** — The Voting → Discussing dialog shows a count, not names. Listing who hasn't voted creates social pressure incompatible with anonymous voting.
- **Readiness during Grouping or Discussing** — Grouping is SM-driven (pacing is self-evident). Discussing has its own focus/discussed flow. Readiness adds no value in these phases.
- **Participant-initiated phase advance requests** — Only the SM controls pacing. No "request advance" or "vote to advance" mechanism.
- **Close Sprint from non-Reporting phases** — Some competitors allow ending at any phase. We gate to Reporting to prevent accidental closure mid-retro.
