# Phase Action Gating

**Traces to:** `docs/specs/v1.md` §5.2 (retro phases), §9 (sprint lifecycle), §3.3 (action ownership)
**Dependencies:** Sprints, Cards, Voting, EvidenceLinks, Participants
**Status:** Complete

---

## Purpose

Today a sprint phase is informational only — it tells the team where they are in the retro, but does not restrict what any participant may do. A participant can write notes during Voting, vote during Discussing, and so on. This feature makes phases enforceable: every action a participant or Scrum Master can take is gated by the current phase, and the system rejects actions outside the permitted set.

The permitted (phase, action) pairs are defined as data and exposed to the frontend through a read endpoint, so the UI can disable forbidden actions proactively and the backend can enforce the same rule — both sides reading from the same source of truth.

This feature supersedes the "phases are soft" rule in the Sprints feature spec.

## Entities

### SprintAction

The set of actions a participant or Scrum Master can take inside a running sprint. The set is closed — every mutation endpoint in the sprint surface corresponds to exactly one SprintAction value.

| Value | Meaning |
|-------|---------|
| CreateNote | Add a new Note card to the Went Well or Didn't Go Well column |
| EditNote | Change the text of an existing Note |
| DeleteNote | Remove an existing Note |
| CreateActionItem | Add a new Action Item card |
| EditActionItem | Change the text or assignee of an existing Action Item |
| DeleteActionItem | Remove an existing Action Item |
| CastVote | Place a vote on a Note |
| RetractVote | Remove one of the caller's votes from a Note |
| AttachEvidenceLink | Attach a URL to a card |
| DetachEvidenceLink | Remove a URL from a card |
| AdvancePhase | Move the sprint forward one phase |

Authorization (who is allowed to edit/delete a given card, who can advance a phase, etc.) is a separate concern owned by each feature and is not changed by this feature.

### PhaseAction

A single (phase, action) pair indicating that the action is permitted while the sprint is in that phase. The set of all such pairs is the **action matrix** below.

The matrix is seeded product data. It is readable by the system at any time and is exposed to the frontend via a dedicated read endpoint. Changing the matrix requires a data migration — it is not runtime-tunable per team or per sprint.

## Phase Rename: Closed → Reporting

The last value of the phase enumeration is renamed from `Closed` to `Reporting`.

Reason: a sprint has two independent machines — a state machine (Draft → Active → Closed) and a phase machine (Writing → Voting → Discussing → Closed) — and both currently have a value named `Closed`, which causes ambiguity in conversation and in code reviews. Renaming the phase terminal value to `Reporting` removes the collision. The phase machine becomes:

`Writing → Voting → Discussing → Reporting`

Behavior is unchanged — Reporting is still a terminal phase, still reached via AdvancePhase, and the SM still closes the sprint separately to move the state to Closed. This rename is part of the scope of this feature and every reference in existing feature specs, backend code, frontend code, and tests updates together.

## Action Matrix

The authoritative mapping of which actions are permitted in which phases.

| Action | Writing | Voting | Discussing | Reporting |
|--------|---------|--------|------------|-----------|
| CreateNote | ✓ | | | |
| EditNote | ✓ | | | |
| DeleteNote | ✓ | | | |
| CreateActionItem | | | ✓ | |
| EditActionItem | | | ✓ | |
| DeleteActionItem | | | ✓ | |
| CastVote | | ✓ | | |
| RetractVote | | ✓ | | |
| AttachEvidenceLink | ✓ | | ✓ | |
| DetachEvidenceLink | ✓ | | ✓ | |
| AdvancePhase | ✓ | ✓ | ✓ | |

Reporting is an intentionally empty row — the retro is finished; the SM reviews outcomes and then closes the sprint. Closing the sprint is a state-machine action, not a phase action, and is not part of this matrix.

## User Flows

```
Flow 1: Permitted action succeeds
1. Sprint is in phase Writing
2. Participant types a note and hits Add
3. Frontend checks the cached matrix — CreateNote is permitted in Writing
4. Frontend submits the create-note request
5. Backend checks the current sprint phase against the action from the request
6. Action is permitted; the card is created and broadcast to the sprint group
```

```
Flow 2: Forbidden action is hidden by the frontend
1. Sprint is in phase Voting
2. Participant views the retro room
3. Frontend consults the cached matrix — CreateNote is not permitted in Voting
4. The note-add input is disabled (or hidden) for every Note column
5. Participant sees vote buttons on existing notes instead
```

```
Flow 3: Forbidden action is rejected by the backend
1. Sprint is in phase Voting
2. A client submits a create-note request anyway (e.g., stale UI, direct API call, race condition after the SM advanced the phase)
3. Backend checks the current sprint phase against the action from the request
4. Action is not permitted; request returns 409 with a message naming the action and the current phase
5. No card is created; no event is broadcast
```

```
Flow 4: Phase change redraws the available actions
1. Sprint is in phase Writing; participants are adding notes
2. SM clicks Advance Phase; sprint moves to Voting
3. Backend broadcasts the phase change to the sprint group
4. Every connected frontend receives the new phase and re-evaluates the matrix
5. Note inputs disable; vote buttons appear on Notes; Action Items column remains hidden
```

```
Flow 5: Frontend loads the matrix
1. Frontend starts up
2. Frontend requests the action matrix
3. Backend returns every permitted (phase, action) pair
4. Frontend caches the matrix for the session and uses it to gate every interactive control
```

## API Surface

### New endpoint

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|--------------|
| GET | `/api/phase-actions` | Anonymous | — | `[{ phase, actions: [ ... ] }]` — one entry per phase, each listing the permitted action names | 200 |

Response body is the complete matrix grouped by phase. The frontend fetches once per session and caches it.

### Changes to existing endpoints

Every mutation endpoint in the sprint surface gains a phase check before any other processing (after authentication and authorization, before domain invariants). If the action is not permitted in the current phase, the endpoint returns:

- **409** with a problem-details body that names the attempted action and the current phase.

Affected endpoints:

| Endpoint | Action |
|---|---|
| `POST /api/sprints/{sprintId}/cards` | CreateNote or CreateActionItem (per card type) |
| `PATCH /api/sprints/{sprintId}/cards/{cardId}` | EditNote or EditActionItem (per card type) |
| `DELETE /api/sprints/{sprintId}/cards/{cardId}` | DeleteNote or DeleteActionItem (per card type) |
| `POST /api/sprints/{sprintId}/cards/{cardId}/votes` | CastVote |
| `DELETE /api/sprints/{sprintId}/cards/{cardId}/votes` | RetractVote |
| `POST /api/sprints/{sprintId}/cards/{cardId}/evidence-links` | AttachEvidenceLink |
| `DELETE /api/sprints/{sprintId}/cards/{cardId}/evidence-links/{linkId}` | DetachEvidenceLink |
| `POST /api/teams/{teamId}/sprints/{sprintId}/advance-phase` | AdvancePhase |

No endpoint contract changes beyond the addition of a 409 response for phase violations.

## SignalR Events

No new events. The existing `PhaseChanged` event already fires when the SM advances the phase; this feature changes how the frontend reacts to it (re-evaluate the matrix) but does not alter the event itself.

## Business Rules

1. Every sprint mutation corresponds to exactly one SprintAction value. There are no ungated mutations.
2. The action matrix is the single source of truth for which actions are permitted in which phase. Backend enforcement and frontend gating both read from this matrix.
3. The matrix is seeded product data. It changes only via data migration, not at runtime.
4. The matrix is exposed to any caller (anonymous read endpoint) because it is product rules, not tenant data. No authentication required.
5. When a sprint's phase changes, every connected client re-evaluates the matrix for the new phase. The backend broadcasts the phase change via the existing SignalR mechanism; the frontend consumes that event.
6. When an action is not permitted in the current phase:
   - The frontend disables or hides the control that would submit it.
   - If the backend receives it anyway, it returns 409 naming the attempted action and the current phase.
7. Phase-gating runs after authentication and authorization. A request that is unauthenticated still returns 401 first; an authenticated-but-unauthorized request returns 403 first; a permitted-but-phase-forbidden request returns 409. This ordering is fixed.
8. Action ownership rules (e.g., "only the author or the Scrum Master may edit a card") are not changed by this feature. Those checks run in addition to phase-gating.
9. The phase value previously named `Closed` is renamed to `Reporting`. Every surface that exposes phase names (API responses, SignalR payloads, frontend strings) uses the new name. There is no backward compatibility alias — v1 has no external consumers of phase names.
10. The Reporting phase permits no actions. A sprint in Reporting is effectively read-only from the participant's point of view; the SM may close the sprint via the state-machine action, which is not phase-gated.
11. Closing the sprint (state transition) is not a SprintAction. It is gated by the sprint state machine, which is unaffected by this feature.
12. A Closed sprint state continues to reject all mutations regardless of phase, as today. Phase-gating applies only to sprints in the Active state.

## Acceptance Criteria

- [ ] `GET /api/phase-actions` returns the full matrix: one entry per phase (Writing, Voting, Discussing, Reporting), each listing every permitted SprintAction by name
- [ ] `GET /api/phase-actions` requires no authentication and returns 200
- [ ] Every phase's entry in the response matches the action matrix in this spec exactly
- [ ] The phase named `Closed` no longer exists anywhere in API responses, SignalR payloads, or frontend state; the corresponding phase is named `Reporting`
- [ ] AdvancePhase on a sprint in phase Discussing moves it to phase Reporting (not Closed)
- [ ] AdvancePhase on a sprint in phase Reporting returns 409
- [ ] Creating a note during phase Writing succeeds (unchanged from today)
- [ ] Creating a note during phase Voting returns 409 with the current phase and action named in the response
- [ ] Creating a note during phase Discussing returns 409
- [ ] Creating a note during phase Reporting returns 409
- [ ] Editing or deleting a note during any phase other than Writing returns 409
- [ ] Creating an action item during phase Discussing succeeds
- [ ] Creating an action item during any phase other than Discussing returns 409
- [ ] Editing or deleting an action item during any phase other than Discussing returns 409
- [ ] Casting a vote during phase Voting succeeds
- [ ] Casting a vote during any phase other than Voting returns 409
- [ ] Retracting a vote during phase Voting succeeds
- [ ] Retracting a vote during any phase other than Voting returns 409
- [ ] Attaching an evidence link during phase Writing or Discussing succeeds
- [ ] Attaching an evidence link during phase Voting or Reporting returns 409
- [ ] Detaching an evidence link during phase Writing or Discussing succeeds
- [ ] Detaching an evidence link during phase Voting or Reporting returns 409
- [ ] 409 phase-violation responses include a problem-details body that names the attempted SprintAction and the sprint's current phase
- [ ] Unauthenticated requests return 401 before any phase check runs
- [ ] Authenticated-but-unauthorized requests (e.g., non-owner trying to edit a card) return 403 before any phase check runs
- [ ] A Closed sprint (state) continues to reject all mutations with 409 regardless of phase
- [ ] Frontend fetches the matrix once per session and caches it
- [ ] Frontend disables or hides every control whose corresponding SprintAction is not permitted in the current phase
- [ ] When the SM advances the phase, every connected client re-evaluates available actions within one second of receiving the PhaseChanged event
- [ ] The retro room Action Items column is visible only while the phase is Discussing
- [ ] The retro room Notes columns (Went Well, Didn't Go Well) remain visible in every phase; only the note-add input is gated
- [ ] Ownership rules (own-or-SM) continue to apply alongside phase-gating on edit and delete endpoints
- [ ] Existing sprints already in the old phase named `Closed` migrate to the new `Reporting` value automatically on deployment
- [ ] Build passes with zero errors

## Out of Scope

- **Per-team or per-sprint custom rules** — the matrix is a global product rule. Tenants cannot override which actions are permitted in which phase.
- **Runtime admin UI for editing the matrix** — changing the rules requires a data migration. No in-app editor in v1.
- **Audit log of blocked actions** — 409 responses are returned and logged by the server's standard request log, but there is no dedicated "attempted forbidden action" event stream or history view.
- **Phase-gating for CloseSprint** — closing the sprint is a state-machine action, not a phase action. It remains gated by the sprint's State, not its CurrentPhase.
- **Phase-gating for StartSprint** — same reason; StartSprint moves state from Draft to Active and has no phase yet.
- **Participant join/leave gating** — joining a sprint is not a phase action and is governed by sprint state (Active only). Not part of this matrix.
- **Backward-compatible phase-name alias** — there is no fallback accepting `Closed` as a synonym for `Reporting`. v1 has no external consumers.
- **Grace window in Reporting** — Reporting is immediately and fully read-only. There is no "last chance to edit an action item" between phase-advance and sprint-close.
- **Timed or automatic phase transitions** — phases change only when the SM clicks Advance Phase; no timers, no auto-advance.
