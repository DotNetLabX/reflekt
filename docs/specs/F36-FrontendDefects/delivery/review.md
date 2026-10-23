# F36 — Frontend Visual & UX Defects — Review

## Step 2 — Code Review

## Verdict: REQUEST CHANGES

Primary: Codex — NO-GO, persisted by team-lead session d941cd66
**Model:** codex
Job: task-mvkifp7l-issdkt (first review). Verbatim output: `review-codex.md`. Second reader: `review-second.md` (Sonnet, COMMENT). Run record: verify-run mvkidqtd-2616b88d, profile complete, pass.

### [HIGH] Retro-room entry continues after unmount
**File:** `web/reflekt-web/src/composables/useRetroRoom.ts:147`
**Issue:** Delay sprint A's summary, navigate to sprint B, then release A's response: A resumes identification and data loading, replaces B's shared board state, and reconnects the hub to A. Request counters permit this because A starts newer requests.
**Fix:** Invalidate entry on unmount and check cancellation after every await, before shared identity/token changes, subsequent loads, and hub operations.

### [MEDIUM] Superseded sprint load still starts a participant fetch
**File:** `web/reflekt-web/src/views/SprintView.vue:57`
**Issue:** After B loads, A's delayed request returns without committing, but A's continuation sees B's active sprint and calls `fetchParticipants(A)`. B then displays A's participants.
**Fix:** Stop disposed or superseded continuations and verify `currentSprint.id === sprintId` before fetching participants.

### Carry-over rows (Codex assessment)
- [LOW] `RetroColumnPanel.vue:9`, `:67` — bare `border-b`/`border-t` use `currentColor`; confirmed, predates F36.
- [LOW] `useTimer.ts:12` — `MUTE_KEY` unused; confirmed, predates F36.
- [LOW] `ReportView.vue:11` — Retry appears for 401/403; confirmed, follows the plan; auth-specific recovery belongs in later scope.
- [MEDIUM] `implementation.md:275` — signed-in, phase-change, refresh-banner and audible-chime observations pending; Step 14 (operator) owed; AC6 open as the plan permits.

Positive: audit hashes match all 45 changed web files; permission checks match server rules; BaseInput forwarding, audio listener teardown and Tailwind output showed no issues; campaign-protected files unchanged.

*Status: COMPLETE — Codex, persisted by team-lead, 2026-10-23*

## Step 2 — Fix list, cycle 1

**Done check (Check 1, PASS):** no items.

**Codex (primary):**
1. [HIGH] `web/reflekt-web/src/composables/useRetroRoom.ts:147` — retro-room entry continues after unmount; a superseded sprint's entry can overwrite the new sprint's board state and reconnect the hub to the old sprint. Fix: invalidate the entry on unmount (and on a new entry) and check it after every await, before identity/token changes, later loads and hub operations.
2. [MEDIUM] `web/reflekt-web/src/views/SprintView.vue:57` — a superseded sprint load still calls `fetchParticipants` for the old sprint, so the new sprint shows the old sprint's participants. Fix: stop disposed/superseded continuations and check `currentSprint.id === sprintId` before fetching participants.
3. [LOW] `web/reflekt-web/src/composables/useTimer.ts:12` — unused `MUTE_KEY` constant in a file this feature changed. Fix: remove it.

**Second reader (Sonnet):**
4. [MEDIUM] `web/reflekt-web/src/components/sprints/CreateSprintForm.vue:134-141` with `stores/sprint.ts:93-96` — clear-on-start empties `sprints` before the form mounts, so the form's pre-fill from the previous sprint no longer runs (behaviour lost versus before F36). Fix: apply the pre-fill when the list load ends (or mount the form after it), restoring today's pre-fill.

Not fix-list items (recorded, not sent):
- AC6 / Step 14 operator walkthrough (Codex MEDIUM carry-over row; second reader concurs) — the plan's operator-owned step, not a code defect; reported at close as open acceptance.
- `RetroColumnPanel.vue:9`, `:67` bare borders [LOW] — predates F36, outside its defects; F39 candidate (developer carry-over).
- `ReportView.vue:11` Retry on 401/403 [LOW] — follows the plan; later scope.

Follow-up review: due — row 1 is HIGH; Covers: the round's changes; steps built or redone: none.

Under the bar: none (round 1 carries every item; the three "not fix-list items" above are recorded, not sent).

## Step 2 — Re-review (cycle 1)

## Verdict: REQUEST CHANGES

Primary: Codex — NO-GO, persisted by team-lead session d941cd66
**Model:** codex
Job: task-mvkiuay0-e22elh (re-review after fixes, cycle 1/3; Codex alone — Covers lists no step). Verbatim: `review-codex.md` second section. Run record: dev-fix-c1 fast record mvkiscxu-e263bd1d (pass) + vue-tsc pass.

Fix-list status: row 1 not fixed; row 2 fixed (`SprintView.vue:60`, `:62`, `:69`); row 3 fixed; row 4 fixed with a regression (below).

### [HIGH] Stale joins still overwrite shared identity
**File:** `web/reflekt-web/src/composables/useRetroRoom.ts:85` (also guest `join` at :188)
**Issue:** `joinWithGoogle` commits `currentParticipant` before the cancellation check. After B joins, releasing A's delayed response changes the participant to A while retaining B's token.
**Fix:** guard the identity commit inside the store, using cancellation or an identity generation shared across identity operations.

### [HIGH] Hub connection built after the room unmounted keeps running
**File:** `web/reflekt-web/src/composables/useRetroRoom.ts:145` (with `useSprintHub.connect()`)
**Issue:** `connect()` awaits the previous connection's teardown, then builds another without checking whether its room unmounted; closing the room during teardown leaves the new hub running. The check at :151 only suppresses local completion.
**Fix:** make connection creation cancellation-aware after internal awaits and clean up the specific connection owned by that attempt.

### [MEDIUM] Pre-fill watcher overwrites user edits
**File:** `web/reflekt-web/src/components/sprints/CreateSprintForm.vue:144`
**Issue:** the form stays editable during loading; when the response arrives the watcher replaces all column names and colours, including values already entered.
**Fix:** pre-fill only untouched fields, or finish pre-filling before exposing editable controls.

Carry-over: bare borders LOW confirmed (predates F36); `MUTE_KEY` row now stale (removed); ReportView Retry on 401/403 LOW confirmed (follows the plan); AC6 / Step 14 confirmed pending.

*Status: COMPLETE — Codex, persisted by team-lead, 2026-10-23*

## Step 2 — Fix list, cycle 2

Bar: always-in items and findings at MEDIUM and above.

**Codex (re-review, cycle 1):**
1. [HIGH] `web/reflekt-web/src/composables/useRetroRoom.ts:85`, `:188` — stale Google/guest joins commit `currentParticipant` before the cancellation check, so a superseded room's identity overwrites the current one (B's token, A's participant). Fix: guard the identity commit (cancellation or an identity generation shared across identity operations) so a stale attempt never writes identity.
2. [HIGH] `web/reflekt-web/src/composables/useRetroRoom.ts:145` with `web/reflekt-web/src/composables/useSprintHub.ts` `connect()` — after awaiting the previous connection's teardown, `connect()` builds a new connection without checking that the room is still mounted; closing the room during teardown leaves the new hub running. Fix: make connection creation cancellation-aware after its internal awaits, and stop the specific connection a stale attempt built. **Ruled — `questions.md` Q1 (option 2):** keep the `useSprintHub.ts` pin; fix in `useRetroRoom.ts` `enterData` only — the superseded branch after a successful `connect()` calls `disconnect()` as its first statement, nothing awaited before it, then returns without setting `joined` or `loading`.
3. [MEDIUM] `web/reflekt-web/src/components/sprints/CreateSprintForm.vue:144` — the pre-fill watcher overwrites names and colours the user already entered while the list was loading. Fix: pre-fill only fields the user has not touched (or keep the form unavailable until pre-fill is applied) — restore today's behaviour, add none.

Follow-up review: due — rows 1 and 2 are HIGH; Covers: the round's changes; steps built or redone: none.

Under the bar: none (no item below MEDIUM was raised in the cycle-1 re-review; its carry-over LOWs are recorded in that section).

## Step 2 — Re-review (cycle 2)

## Verdict: REQUEST CHANGES

Primary: Codex — NO-GO, persisted by team-lead session d941cd66
**Model:** codex
Job: task-mvkj64so-j06o4h (re-review after fixes, cycle 2/3; Codex alone). Verbatim: `review-codex.md` third section. Run record: dev-fix-c2 fast record mvkj4ywh-6f838414 (pass) + vue-tsc pass.

Fix-list status: row 1 fixed (`participant.ts:42`, `:55`, `:81`; `useRetroRoom.ts:220`); row 2 not fixed (below); row 3 fixed (`CreateSprintForm.vue:24`, `:142-143`).

### [HIGH] Stale-entry cleanup can disconnect the current room's hub
**File:** `web/reflekt-web/src/composables/useRetroRoom.ts:155`
**Issue:** If stale entry A and reopened entry B both await the same previous teardown, both then build connections. When B connects first, A's continuation invokes `JoinSprint` on the shared B connection; its cleanup then stops B and leaves A's connection open. Reproduced in memory: B `joined=true`, B's hub Disconnected, A's hub Connected, shared connection `null`. Q1's prescribed branch is implemented, but its ownership assumption fails with concurrent teardown waiters.
**Fix:** serialize hub lifecycle operations across room instances inside `useRetroRoom.ts`, recheck supersession before connecting, and retain ownership through stale cleanup; keep `useSprintHub.ts` pinned to its eight argument changes.

Carry-over: bare borders LOW confirmed (predates F36); `MUTE_KEY` row stale (resolved); ReportView Retry on 401/403 LOW confirmed (follows the plan); AC6 / Step 14 confirmed pending.

*Status: COMPLETE — Codex, persisted by team-lead, 2026-10-23*

## Step 2 — Fix list, cycle 3

Bar: always-in items and findings at HIGH and above.

**Codex (re-review, cycle 2):**
1. [HIGH] `web/reflekt-web/src/composables/useRetroRoom.ts:155` — stale-entry cleanup can disconnect the current room's hub when two entries await the same teardown. Fix per architect ruling `questions.md` Q2: a module-level `withHubLock` serializing every connect/disconnect across rooms; per-room `ownedConnection` (via `getHubConnection()`); `stopOwned()` under the lock; `releaseHub()` replacing every bare `disconnect()` (unmount, kicked callback, the Q1 stale branch — removed); connecting as one locked operation that skips `connect()` when already superseded and returns `'ok' | 'failed' | 'stale'`. `useSprintHub.ts` stays pinned to its eight argument changes.

Follow-up review: due — row 1 is HIGH; Covers: the round's changes; steps built or redone: none.

Under the bar: none raised in the cycle-2 re-review beyond its recorded carry-over LOWs.

## Step 2 — Re-review (cycle 3)

## Verdict: REQUEST CHANGES

Primary: Codex — NO-GO, persisted by team-lead session d941cd66
**Model:** codex
Job: task-mvkjljra-mdf4n0 (re-review after fixes, cycle 3/3; Codex alone). Verbatim: `review-codex.md` fourth section. Run record: dev-fix-c3 fast record mvkjjd89-9d869908 (pass) + vue-tsc pass.

Fix-list status: row 1 fixed (`useRetroRoom.ts:22`, `:76`, `:83-85`; concurrent-teardown-waiters now leaves only B connected), with the regression below. Earlier races (delayed summary after unmount; stale Google/guest join) re-verified passing (`useRetroRoom.ts:112`, `:127`, `:228`).

### [HIGH] Pending joins block teardown and later rooms
**File:** `web/reflekt-web/src/composables/useRetroRoom.ts:71`
**Issue:** `releaseHub()` queues the disconnect behind the locked `connect()`, which awaits `JoinSprint`. Reproduced: after A unmounts, A stays Connected with zero stop calls; B stays loading; A's events still mutate B's shared participants. Keepalives can sustain this blockage indefinitely.
**Fix:** make the active locked operation cancellation-aware, capture its connection before awaiting completion, and stop that connection on cancellation while retaining exclusion until cleanup settles; keep `useSprintHub.ts` pinned.

Carry-over: bare borders LOW confirmed (predates F36); `MUTE_KEY` resolved; ReportView Retry on 401/403 LOW confirmed (as planned); AC6 / Step 14 pending.

*Status: COMPLETE — Codex, persisted by team-lead, 2026-10-23*

## Step 2 — Fix list, cycle 4

Bar: always-in items and findings at HIGH and above (continued cycle granted by the owner, directive d1, 2026-10-23: "continue").

**Codex (re-review, cycle 3):**
1. [HIGH] `web/reflekt-web/src/composables/useRetroRoom.ts:71` — pending joins block teardown and later rooms: `releaseHub()` queues the disconnect behind the locked `connect()`, which awaits `JoinSprint`. Fix per architect ruling `questions.md` Q3: inside the locked connect operation, disconnect any leftover connection, then call `connect(sprintId)` without awaiting it and capture `ownedConnection = getHubConnection()` plus `connectInFlight = true` on the next line, before any `await`; settle the pending connect, then the Q2 stale/ok/failed handling; a per-room `cancelHub()` (in place of `releaseHub()` in `onUnmounted` and the kicked callback) calls `ownedConnection.stop()` directly — not through the lock, not awaited — when a connect is in flight, then `releaseHub()`. No `disconnect(` outside `stopOwned` and the locked operation. `useSprintHub.ts` stays pinned to its eight argument changes.

Follow-up review: due — row 1 is HIGH; Covers: the round's changes; steps built or redone: none.

Under the bar: none.

## Step 2 — Re-review (cycle 4)

## Verdict: REQUEST CHANGES

Primary: Codex — NO-GO, persisted by team-lead session a577d95e
**Model:** codex
Job: task-mvklg43u-mwxpsf (re-review after fixes, cycle 4, owner-granted continued cycle; Codex alone). Verbatim: `review-codex.md` fifth section. Run record: dev-fix-c4 fast record mvklebrl-955403fb (pass) + vue-tsc pass.

Fix-list status: row 1 fixed for the held-`JoinSprint` case — reproduction (b) passes (A disconnected, zero later event effects, B connected, loading cleared); (c) concurrent teardown waiters passes; the held hub-handshake part of (a) passes; delayed summary after unmount and stale Google/guest join pass. Q3's structural checks pass; `useSprintHub.ts` differs only by its eight pinned arguments.

### [HIGH] Transport startup can still block subsequent rooms
**File:** `web/reflekt-web/src/composables/useRetroRoom.ts:78`
**Issue:** With the WebSocket held before `onopen`, leaving A calls `stop()`, but SignalR 10.0.0's `HttpConnection._stopInternal()` waits for transport startup before closing it. A stays `Disconnecting`, its socket stays `CONNECTING` with zero close calls, and B stays `joined=false`, `loading=true`. No SignalR startup timeout applies at that stage; releasing `onopen` lets cleanup and B's entry proceed.
**Fix:** make transport startup cancellable, or enforce a deadline that aborts the underlying attempt so teardown settles and the lock is released; use Q3's fallback pin amendment if the hub surface must change.

Carry-over: bare borders LOW confirmed (predates F36); `MUTE_KEY` resolved; ReportView Retry on 401/403 LOW confirmed (as planned); AC6 / Step 14 pending (MEDIUM, as planned).

*Status: COMPLETE — Codex, persisted by team-lead, 2026-10-23*
