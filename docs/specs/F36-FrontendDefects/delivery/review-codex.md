# F36-FrontendDefects — Codex code review (persisted by team-lead session d941cd66)

Job task-mvkifp7l-issdkt, label "F36-FrontendDefects code-review round 1", first review, 2026-10-23.

## Verdict: NO-GO

Two navigation races reproduced with the actual stores and composables in memory:

- **HIGH — [useRetroRoom.ts:147](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useRetroRoom.ts:147):** Entry continues after unmount. Delay sprint A’s summary, navigate to sprint B, then release A’s response: A resumes identification and data loading, replaces B’s shared board state, and reconnects the hub to A. Request counters permit this because A starts newer requests. **Suggested fix:** invalidate entry on unmount and check cancellation after every await, before shared identity/token changes, subsequent loads, and hub operations.

- **MEDIUM — [SprintView.vue:57](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/views/SprintView.vue:57):** A superseded sprint load still starts a participant fetch. After B loads, A’s delayed request returns without committing, but A’s continuation sees B’s active sprint and calls `fetchParticipants(A)`. B then displays A’s participants. **Suggested fix:** stop disposed or superseded continuations and verify `currentSprint.id === sprintId` before fetching participants.

Every Carry-Over Findings row was assessed:

| Severity | File:line | Assessment and suggested fix |
|---|---|---|
| LOW | [RetroColumnPanel.vue:9](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/components/retro/RetroColumnPanel.vue:9), also :67 | **Confirmed.** Bare borders use `currentColor`; both predate F36. Add `border-border` in subsequent scoped work. |
| LOW | [useTimer.ts:12](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useTimer.ts:12) | **Confirmed.** `MUTE_KEY` is unused and predates F36. Remove during later cleanup. |
| LOW | [ReportView.vue:11](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/views/ReportView.vue:11) | **Confirmed.** Retry appears for 401/403 and repeats the unauthorized request for a tokenless guest. This follows the plan; any authentication-specific recovery change belongs in subsequent scope. |
| MEDIUM | [implementation.md:275](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/implementation.md:275) | **Confirmed.** Signed-in, phase-change, refresh-banner, and audible-chime observations remain pending. Complete and record Step 14; AC6 remains open as the plan permits. |

The passing audit’s hashes match all 45 changed web files. Permission checks match the current server rules; BaseInput forwarding, audio listener teardown, and required Tailwind output showed no additional issues. Campaign-protected files are unchanged. This review wrote no files.

---

Job task-mvkiuay0-e22elh, label "F36-FrontendDefects code-review round 2", re-review after fixes, cycle 1/3, 2026-10-23.

## Verdict: NO-GO

- **HIGH — [useRetroRoom.ts:85](/D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useRetroRoom.ts:85): Stale joins still overwrite shared identity.** `joinWithGoogle` commits `currentParticipant` before the cancellation check; guest `join` has the same problem at line 188. Reproduced: after B joins, releasing A’s delayed response changes the participant to A while retaining B’s token. **Suggested fix:** guard the identity commit inside the store, using cancellation or an identity generation shared across identity operations.

- **HIGH — [useRetroRoom.ts:145](/D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useRetroRoom.ts:145): The documented hub edge case remains.** `useSprintHub.connect()` awaits the previous connection’s teardown, then builds another connection without checking whether its room unmounted. Reproduced: closing the room during teardown leaves its newly built hub running. The check at line 151 only suppresses local completion. **Suggested fix:** make connection creation cancellation-aware after internal awaits and clean up the specific connection owned by that attempt.

- **MEDIUM — [CreateSprintForm.vue:144](/D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/components/sprints/CreateSprintForm.vue:144): New pre-fill watcher overwrites user edits.** The form remains editable during loading. When the response arrives, the watcher replaces all column names and colours, including values already entered. Reproduced with a custom name and colour. **Suggested fix:** pre-fill only untouched fields, or finish pre-filling before exposing editable controls.

Fix-list assessment:

| Row | Status | Evidence |
|---|---|---|
| 1 | **Not fixed** | Cancellation improves earlier stages, but the identity and hub races above remain. |
| 2 | **Fixed** | `SprintView.vue:60`, `:62`, `:69` guard stale/unmounted loads and verify the sprint ID. Reproduction fetched participants only for B. |
| 3 | **Fixed** | `useTimer.ts:12`: unused `MUTE_KEY` removed. |
| 4 | **Fixed, with regression** | `CreateSprintForm.vue:139` restores pre-fill after loading; the new overwrite defect is reported above. |

Carry-Over Findings assessment:

- **LOW — `RetroColumnPanel.vue:9`, `:67`: Confirmed.** Uncoloured borders remain and predate F36. Suggested later fix: add the appropriate border colour utility.
- **LOW — `useTimer.ts:12`: Refuted in the current tree.** `MUTE_KEY` is removed; the carry-over row is stale.
- **LOW — `ReportView.vue:11`: Confirmed.** Retry still appears for authorization errors. This follows the plan and remains outside this fix round. Suggested later fix: provide authorization-specific recovery.
- **MEDIUM — `implementation.md:275`: Confirmed.** Signed-in, phase-change, refresh-banner and audible-chime observations remain pending. Suggested action: complete and record operator Step 14; AC6 remains open.

Validation used read-only, in-memory reproductions. No files were modified or tests added. The recorded .NET and type-check passes do not exercise these asynchronous races.

---

Job task-mvkj64so-j06o4h, label "F36-FrontendDefects code-review round 3", re-review after fixes, cycle 2/3, 2026-10-23.

## Verdict: NO-GO

Cycle 2 fix-list assessment:

1. **HIGH — Row 1: fixed.** [participant.ts:42](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/stores/participant.ts:42), also lines 55 and 81, guards identity commits with a shared generation. [useRetroRoom.ts:220](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useRetroRoom.ts:220) invalidates pending identity operations on unmount.

2. **HIGH — Row 2: not fixed; cleanup introduces a regression.** [useRetroRoom.ts:155](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useRetroRoom.ts:155) can disconnect the current room’s hub. If stale entry A and reopened entry B await the same previous teardown, both subsequently build connections. When B connects first, A’s continuation invokes `JoinSprint` on the shared B connection; its new cleanup then stops B and leaves A’s connection open.

   A read-only, in-memory reproduction using the current composables and installed SignalR client produced: B still `joined=true`, B’s hub `Disconnected`, A’s hub `Connected`, and the shared connection `null`. Q1’s prescribed branch is implemented, but its ownership assumption fails with concurrent teardown waiters.

   **Suggested fix:** serialize hub lifecycle operations across room instances inside `useRetroRoom.ts`, recheck supersession before connecting, and retain ownership through stale cleanup. Keep `useSprintHub.ts` pinned to its eight argument changes.

3. **MEDIUM — Row 3: fixed.** [CreateSprintForm.vue:24](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/components/sprints/CreateSprintForm.vue:24) and the corresponding name/colour handlers record edits; lines 142–143 prefill only untouched fields.

Carry-over table assessment:

- **LOW — Bare borders: confirmed.** [RetroColumnPanel.vue:9](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/components/retro/RetroColumnPanel.vue:9), also line 67, still lacks border-colour utilities. This predates F36. **Suggested fix:** add the existing theme border colour in later scope.
- **LOW — Unused `MUTE_KEY`: refuted; record is stale.** [implementation.md:364](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/implementation.md:364) describes a constant already removed. **Suggested fix:** mark that carry-over row resolved.
- **LOW — Report Retry on 401/403: confirmed.** [ReportView.vue:11](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/views/ReportView.vue:11) displays Retry for every error, including authorization failures. This follows the plan. **Suggested fix:** defer authorization-specific recovery to later scope.
- **MEDIUM — Guest-only walkthrough: confirmed.** [implementation.md:275](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/implementation.md:275) still lists signed-in, phase-change, refresh-banner and audible-chime observations as pending. **Suggested fix:** complete and record operator Step 14; AC6 remains open as planned.

No files were modified. The recorded .NET and `vue-tsc` passes do not cover the reproduced hub race.

---

Job task-mvkjljra-mdf4n0, label "F36-FrontendDefects code-review round 4", re-review after fixes, cycle 3/3, 2026-10-23.

## Verdict: NO-GO

**HIGH — [useRetroRoom.ts:71](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useRetroRoom.ts:71): New regression—pending joins block teardown and later rooms.** `releaseHub()` queues disconnect behind the locked `connect()`, which awaits `JoinSprint`. Reproduced with the actual composables, stores and installed SignalR client: after A unmounts, A remains Connected with zero stop calls; B stays loading; A’s events still mutate B’s shared participants. Keepalives can sustain this blockage indefinitely. **Suggested fix:** make the active locked operation cancellation-aware, capture its connection before awaiting completion, and stop that connection on cancellation while retaining exclusion until cleanup settles. Keep `useSprintHub.ts` pinned.

**Cycle 3 fix-list row 1: fixed, with the regression above.** [useRetroRoom.ts:22](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useRetroRoom.ts:22), lines 76 and 83–85 implement Q2’s serialization, supersession check and ownership cleanup. The concurrent-teardown-waiters reproduction now leaves only B connected. The pinned hub diff contains only the eight permitted argument changes.

The earlier delayed-summary-after-unmount and stale Google/guest join reproductions pass: checks at [useRetroRoom.ts:112](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useRetroRoom.ts:112), lines 127 and 228, plus the participant store’s identity generation, preserve B’s state and token.

Every Carry-Over Findings row was assessed:

| Severity | File:line | Assessment and suggested fix |
|---|---|---|
| LOW | [RetroColumnPanel.vue:9](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/components/retro/RetroColumnPanel.vue:9), also :67 | **Confirmed.** Bare borders remain and predate F36. Add the theme border-colour utility in later scope. |
| LOW | [useTimer.ts:12](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useTimer.ts:12) | **Refuted as outstanding; resolved.** `MUTE_KEY` is removed, and the table now records that correctly. No further fix. |
| LOW | [ReportView.vue:11](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/views/ReportView.vue:11) | **Confirmed.** Retry includes 401/403, as planned. Defer authorization-specific recovery to later scope. |
| MEDIUM | [implementation.md:275](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/implementation.md:275) | **Confirmed.** Operator observations remain pending. Complete and record Step 14; AC6 remains open. |

Read-only verification covered the deferred races, failed-join retry and repeated reopening. The non-emitting frontend type-check passed. No files were modified or tests added.


<!-- Re-review after fixes, cycle 4 (owner-granted) — job task-mvklg43u-mwxpsf, label "F36-FrontendDefects code-review round 5", persisted by team-lead session a577d95e, 2026-10-23 -->

## Verdict: NO-GO

**HIGH — [useRetroRoom.ts:78](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useRetroRoom.ts:78): Transport startup can still block subsequent rooms.** Reproduced with the actual composables, stores, and installed SignalR 10.0.0, holding the WebSocket before `onopen`: leaving A calls `stop()`, but SignalR’s `HttpConnection._stopInternal()` waits for transport startup before closing it. A remains `Disconnecting`, its socket remains `CONNECTING` with zero close calls, and B remains `joined=false`, `loading=true`. No SignalR startup timeout is scheduled at this stage. Releasing `onopen` externally immediately allows cleanup and B’s entry.

**Suggested fix:** make transport startup cancellable or enforce a deadline that aborts the underlying attempt, ensuring teardown settles and releases the lock. Use Q3’s fallback pin amendment if the hub surface must change.

Reproduction results:

| Case | Result |
|---|---|
| (a) Leave during startup | Held hub handshake passes; held WebSocket opening fails as above. |
| (b) Leave during held `JoinSprint` | Pass: A disconnected, zero subsequent event effects, B connected and loading cleared. |
| (c) Q2 concurrent teardown waiters | Pass, including late old-room cleanup: only B remains connected, loading clears, zero stale event effects. |
| Delayed summary after unmount | Pass: B’s stores remain intact. |
| Stale Google/guest join | Both pass: B’s identity and token remain intact. |

Q3’s structural checks pass. Automated comparison confirms `useSprintHub.ts` differs from HEAD only by the eight permitted background arguments. The non-emitting frontend type-check passes.

Every carry-over row was assessed:

| Severity | File:line | Assessment and suggested fix |
|---|---|---|
| LOW | [RetroColumnPanel.vue:9](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/components/retro/RetroColumnPanel.vue:9), also :67 | **Confirmed.** Bare borders remain and predate F36. Add the theme border-colour utility in later scope. |
| LOW | [useTimer.ts:12](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/composables/useTimer.ts:12) | **Refuted as outstanding; resolved.** `MUTE_KEY` is removed and the table records its resolution. No further fix. |
| LOW | [ReportView.vue:11](D:/src/reflekt-F36-FrontendDefects/web/reflekt-web/src/views/ReportView.vue:11) | **Confirmed.** Retry includes 401/403, as planned. Add authorization-specific recovery in later scope. |
| MEDIUM | [implementation.md:275](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/implementation.md:275) | **Confirmed.** Operator observations remain pending. Complete and record Step 14; AC6 remains open as planned. |

No files were modified; reproductions ran entirely in memory.
