# Timer per Phase

**Traces to:** `docs/specs/v2.md` §4.6
**Source:** Scratch
**Dependencies:** Inline Phase Controls (#28) — timer controls live in the top bar established by that feature
**Status:** Done

---

## Purpose

Retros run long without structure. Without timers, the Scrum Master either uses a phone timer (invisible to participants) or wings it (phases drag). V2 adds an optional countdown timer for applicable phases, visible to all participants and controlled by the SM. The timer is a facilitation aid — it signals pacing, not enforces it.

## Entities

### Sprint (modified)

Sprint gains three fields for timer default durations:

| Field | Type | Constraints |
|-------|------|-------------|
| WritingTimerDefault | integer (minutes) | Required, 1-60, default 5 |
| VotingTimerDefault | integer (minutes) | Required, 1-60, default 3 |
| DiscussingTimerDefault | integer (minutes) | Required, 1-60, default 10 |

Timer defaults are stored per sprint, not per team. Different sprints can have different timer durations.

**Deviation from v2.md:** v2.md §4.6 and §5 specify a single `TimerDefaults` JSON column. This spec refines that to three individual columns for type safety, database-level validation, and consistency with the existing per-sprint column name fields (`Column1Name`, `Column2Name`, `Column3Name`). v2.md should be updated to match.

### Timer State (ephemeral)

During timed phases (Writing, Voting, Discussing), the SM may activate a countdown timer. This state is:

- Tracked in server memory per sprint, not persisted to the database
- Scoped to the current phase — automatically cleared when the phase advances
- Lost on server restart — participants rejoin with no active timer
- Not associated with any persistent entity

Timer state includes: current status (running, paused, expired, stopped), remaining seconds, and end time (when running).

## User Flows

```
Flow 1: SM starts a timer
1. SM is in the retro room during Writing, Voting, or Discussing phase.
2. The top bar shows a timer area with a start control displaying the phase's default duration
   (e.g., "5:00").
3. SM clicks start.
4. The timer begins counting down. All participants see the countdown in the top bar.
5. SM now sees pause, reset, and +time controls accessible via the timer area.
```

```
Flow 2: SM pauses and resumes the timer
1. Timer is running.
2. SM clicks pause.
3. The countdown freezes at its current value. All participants see the paused state.
4. SM clicks resume.
5. The countdown resumes from where it was paused.
```

```
Flow 3: SM adds time
1. Timer is running, paused, or expired.
2. SM selects a +time increment: +1, +2, or +5 minutes.
3. The selected minutes are added to the remaining time.
4. If the timer was running, the countdown continues with the extended time.
5. If the timer was paused, the remaining time increases but the timer stays paused.
6. If the timer was expired (at 00:00), it resumes counting down from the added amount.
7. All participants see the updated time.
```

```
Flow 4: SM resets the timer
1. Timer is running, paused, or expired.
2. SM clicks reset.
3. The timer resets to the phase's default duration and enters the paused state (not running).
4. SM must click start to begin the countdown again.
5. All participants see the reset.
```

```
Flow 5: Timer expires
1. The countdown reaches 00:00.
2. The timer display shows "00:00" and flashes red.
3. An audio chime plays for all participants whose sound is not muted.
4. The board remains fully interactive — no actions are blocked, no inputs are disabled.
5. SM controls remain active: +time to extend, reset to start over, or ignore and continue untimed.
```

```
Flow 6: Phase advance stops the timer
1. A timer is running (or paused, or expired) during the current phase.
2. SM clicks "Next: {phase}" (the advance button from Inline Phase Controls).
3. The standard advance confirmation dialog appears. No additional timer-related warning.
4. SM confirms the advance.
5. The timer stops and is cleared. In the new phase, no timer is active.
6. If the new phase supports timers, the SM can start a new one.
```

```
Flow 7: Participant views the timer
1. A timer is active (running, paused, or expired).
2. The participant sees the countdown display in the top bar.
3. The participant sees a mute/unmute toggle for the expiry sound (local preference only).
4. The participant has no other timer controls — no start, pause, reset, or +time.
```

```
Flow 8: SM configures default durations at sprint creation
1. SM navigates to the sprint creation form.
2. Below the existing fields (name, votes per participant, column configuration), three timer
   duration inputs are visible: Writing, Voting, Discussing.
3. Each input is pre-filled with the system defaults (5, 3, 10 minutes).
4. SM can adjust any value (1-60 minutes).
5. SM submits the form. The sprint is created with the configured timer defaults.
6. During the retro, starting a timer uses the sprint's configured default for that phase.
```

```
Flow 9: Participant reconnects mid-timer
1. A participant disconnects while a timer is running.
2. The participant reconnects and joins the sprint's real-time group.
3. The server sends a timer snapshot with the current state (status, remaining time, end time).
4. The client renders the correct countdown — no gap or stale display.
```

## Top Bar Integration

The timer occupies space in the top bar established by Inline Phase Controls (#28), in Row 1 alongside the existing readiness badge, votes badge, and advance button.

Layout rules:

- The timer display is visible to all participants during Writing, Voting, and Discussing phases.
- During Grouping and Reporting, no timer area is shown.
- SM sees the countdown plus a click target that expands timer controls (start/pause/reset/+time) via a compact popover. This keeps Row 1 from overflowing.
- Participants see the countdown and a mute toggle only.
- Display format: `03:42` when running or paused, `00:00` with red flash when expired, start control with default duration when stopped (SM only — participants see nothing when no timer is active).
- The initial state before any timer has been started in a phase is `stopped`.

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|--------------|
| POST | /api/teams/{teamId}/sprints | SM | `{ ..., writingTimerDefault?, votingTimerDefault?, discussingTimerDefault? }` | Created sprint (includes timer defaults) | 201, 400 |

**POST /api/teams/{teamId}/sprints** (modified):

Sprint creation gains optional timer duration fields. When omitted, system defaults apply (5, 3, 10 minutes). This is a non-breaking change — existing clients that omit the fields get system defaults.

Error responses:
- **400** — any duration is outside the 1-60 minute range.

**Sprint response changes (GET endpoints):**

All endpoints that return sprint data include timer default fields in their response: `writingTimerDefault`, `votingTimerDefault`, `discussingTimerDefault`.

**No REST endpoints for timer control.** Starting, pausing, resetting, and adding time are real-time operations handled entirely through SignalR. There is no value in a REST endpoint for ephemeral, sub-second interactions.

## SignalR Events

### New Events

| Event | Payload | Trigger |
|-------|---------|---------|
| TimerUpdated | `{ state, remainingSeconds, endTimeUtc? }` | SM starts, pauses, resumes, resets, or adds time. Also fired on server-detected expiry. |
| TimerSnapshot | `{ state, remainingSeconds, endTimeUtc? }` | Client joins the sprint's real-time group (sent only to the joining client) |

`state` values: `running`, `paused`, `expired`, `stopped`.

When `state` is `running`, `endTimeUtc` is provided so clients compute the countdown locally without per-second broadcasts. When `state` is `paused`, only `remainingSeconds` is meaningful. When `state` is `expired`, `remainingSeconds` is 0. When `state` is `stopped`, both fields are absent — no timer is active.

### Hub Methods (client to server)

| Method | Parameters | Auth | Purpose |
|--------|-----------|------|---------|
| StartTimer | sprintId | SM only | Start the timer for the current phase using the sprint's configured default duration |
| PauseTimer | sprintId | SM only | Pause the running timer |
| ResumeTimer | sprintId | SM only | Resume a paused timer (v2.md lists "start" as one operation; this spec separates "start from stopped" and "resume from paused" for clarity) |
| ResetTimer | sprintId | SM only | Reset the timer to the phase default duration (paused state) |
| AddTime | sprintId, minutes | SM only | Add minutes (1, 2, or 5) to the current timer. Stays paused if paused; resumes countdown if expired. |

All hub methods are rejected with an error if:
- The caller is not the Scrum Master
- The current phase does not support timers (Grouping, Reporting)
- The operation is invalid for the current timer state (e.g., pause when already paused)

### Timer on Reconnect

Timer state is ephemeral and not available through any REST endpoint. When a client joins (or rejoins) the sprint's real-time group, the server sends a TimerSnapshot with the current timer state. This follows the same pattern as ReadinessSnapshot from Inline Phase Controls (#28). If no timer is active, the snapshot has `state: stopped`.

## Business Rules

1. Timers are available during Writing, Voting, and Discussing phases only. No timer controls are shown during Grouping or Reporting.
2. Only the Scrum Master can start, pause, resume, reset, or add time. Participants see the countdown but have no controls.
3. Starting a timer is optional. Phases can run entirely without a timer — the SM decides whether to use one.
4. The timer is advisory. Timer expiry does not auto-advance the phase, block any user action, or freeze the board. All interactions remain fully available after expiry.
5. When the timer reaches zero: the display shows "00:00" with a red visual flash, and an audio chime plays for all participants (unless individually muted).
6. After expiry, SM controls remain active. The SM can add time (resumes the countdown), reset (returns to phase default, paused), or do nothing and advance when ready.
7. Advancing the phase stops and clears any active timer. No warning about the running timer is shown — the advance confirmation dialog is sufficient.
8. Resetting the timer returns it to the phase's configured default duration in a paused state. The SM must explicitly start it again.
9. Adding time works in any active timer state: running (extends the countdown), paused (increases remaining time, stays paused), or expired (resumes countdown from the added amount, e.g., +2 min starts counting down from 2:00).
10. Timer state is ephemeral: tracked in server memory, not persisted to the database. Lost on server restart. Reconnecting clients receive a snapshot of the current state.
11. Default timer durations are configurable per sprint at creation: Writing (default 5 min), Voting (default 3 min), Discussing (default 10 min). Valid range: 1-60 minutes.
12. Timer synchronization is event-based. The server broadcasts state-change events; clients compute the countdown locally from the provided end time. No per-second broadcasts.
13. The audio mute preference is per-participant and stored locally in the browser. It is global — not scoped to a specific sprint or team. It is not synced across devices or persisted on the server.
14. The +time increments are fixed: +1, +2, and +5 minutes. No custom duration input during an active timer.
15. Only one timer runs per sprint at a time. There is no per-participant or per-column timer.

## Acceptance Criteria

- [ ] During Writing, Voting, and Discussing phases, the top bar shows a timer area visible to all participants.
- [ ] The SM sees a start control showing the phase's default duration when no timer is active.
- [ ] Participants see nothing in the timer area when no timer is active.
- [ ] Clicking start begins the countdown from the phase's configured default duration. All participants see the countdown update in real-time.
- [ ] The SM can pause a running timer. The countdown freezes and all participants see the paused state.
- [ ] The SM can resume a paused timer. The countdown resumes from where it paused.
- [ ] The SM can reset a timer (from running, paused, or expired). The timer returns to the phase default in a paused state.
- [ ] The SM can add time (+1, +2, or +5 minutes) to a running, paused, or expired timer. The countdown updates accordingly.
- [ ] Adding time to a paused timer increases the remaining time without resuming the countdown.
- [ ] Adding time to an expired timer resumes the countdown from the added amount.
- [ ] When the timer reaches 00:00, the display flashes red and an audio chime plays (unless muted).
- [ ] After expiry, the board remains fully interactive — no actions are blocked.
- [ ] After expiry, SM controls remain active (add time, reset).
- [ ] Advancing the phase stops and clears any active timer without an additional warning dialog.
- [ ] In the new phase after advancing, no timer is active until the SM starts one.
- [ ] During Grouping and Reporting phases, no timer area is shown in the top bar.
- [ ] Participants cannot start, pause, resume, reset, or add time — only the SM has these controls.
- [ ] Participants see a mute/unmute toggle for the timer expiry sound. The preference persists in the browser across sessions.
- [ ] Timer state is synchronized in real-time via SignalR. All participants see the same countdown and state changes.
- [ ] On reconnect, the client receives a TimerSnapshot and renders the correct timer state immediately.
- [ ] Sprint creation includes optional timer duration inputs for Writing, Voting, and Discussing, pre-filled with system defaults (5, 3, 10 minutes).
- [ ] Sprint creation without timer fields uses system defaults — non-breaking for existing clients.
- [ ] Sprint creation rejects timer durations outside the 1-60 minute range with a 400 response.
- [ ] Sprint GET responses include timer default fields (`writingTimerDefault`, `votingTimerDefault`, `discussingTimerDefault`).
- [ ] Timer hub methods reject non-SM callers with an error.
- [ ] Timer hub methods reject calls during Grouping or Reporting phases with an error.
- [ ] Timer hub methods reject invalid state transitions (e.g., pause when not running) with an error.
- [ ] SM timer controls are presented in a compact popover to avoid overcrowding the top bar.

## Out of Scope

- **Auto-advance on timer expiry** — The timer is advisory. Auto-advance would cut off participants mid-thought. No competitor auto-advances. The SM always decides when to move on.
- **Per-participant timers** — One timer per sprint, visible to all. Individual timers add complexity with no facilitation value.
- **Persistent timer history** — Timer state is ephemeral. No value in recording phase durations. Analytics are a v3 consideration.
- **Background music** — TeamRetro offers ambient music that fades on timer expiry. Adds audio complexity and content licensing concerns with no facilitation value.
- **Custom +time increments** — Fixed at +1, +2, +5 minutes. Covers practical needs without a custom duration input mid-retro.
- **Timer for Grouping or Reporting** — Grouping is SM-driven (pacing is self-evident). Reporting is terminal and read-only. No facilitation value in timing these phases.
- **Editable timer defaults after sprint creation** — Defaults are set at creation. Mid-retro, the SM uses +time and reset to adjust. A settings page for timer defaults adds UX complexity for minimal gain.
- **Manual "send alert" button** — Retrium lets the facilitator trigger a sound alert at any time. Useful but adds UI surface for a niche case. Revisit if real usage shows the SM wanting to nudge participants without starting a timer.
