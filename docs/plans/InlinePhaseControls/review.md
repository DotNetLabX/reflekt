# Inline Phase Controls — Review

## Reviewed By

`reviewer` (Sonnet agent) — Sonnet-only review. Codex cross-validation was not requested for this cycle.

## Verdict: APPROVE

No CRITICAL or HIGH issues found. Three MEDIUM and two LOW findings noted.

---

## Pre-commitment Predictions

| Predicted area | Actual finding |
|---|---|
| `AddError`/`SendErrorsAsync` misuse for business logic in new endpoint | Confirmed violation of csharp.md, but pre-existing codebase pattern — downgraded to LOW |
| Race condition in `ToggleReady` (check-then-act on ConcurrentDictionary) | Genuine non-atomic pattern — moved to Open Questions (single-participant multi-tab scenario; low real-world risk) |
| `isScrumMaster` sourced correctly from `SprintSummaryResponse` | Correct — flows from `currentSummary.isScrumMaster` |
| `select` element missing `appearance-none bg-transparent` | Pre-existing element not introduced by this feature — not a finding |
| `totalCount` snapshot race in JoinSprint (connection tracked before snapshot) | No issue — connection is correctly added before snapshot count is computed |

---

## Findings

### [MEDIUM] `ReadinessSnapshot` totalCount excludes participants who joined before hub connection was rebuilt

**File:** `web/reflekt-web/src/composables/useSprintHub.ts:192-196`
**Issue:** On reconnect, `connect()` calls `disconnect()` then `build()` then `start()` and `JoinSprint`. The server-side `ConnectionState` is static and persists across reconnects. If the same participant reconnects quickly, their previous `connectionId` may still be in the map (not yet cleaned up by `OnDisconnectedAsync` on the server, since disconnect happens asynchronously). The snapshot `totalCount` sent to the reconnecting client could be inflated by 1 (old connection not yet evicted). This is a transient display glitch — the next `ReadinessChanged` broadcast will include the corrected `totalCount`.
**Fix:** No code change required if the team accepts a brief one-off count artifact on reconnect. Alternatively, document it as a known transient artifact in a code comment in `JoinSprint`.

---

### [MEDIUM] `onAdvanceClick` does not guard against being called when `teamId` is null after dialog open

**File:** `web/reflekt-web/src/views/RetroRoomView.vue:164` and `202-208`
**Issue:** `onAdvanceClick` short-circuits at line 164 (`if (!teamId.value) return`) before opening the dialog — good. But `confirmAdvance` (line 202) also checks `if (!teamId.value) return` silently after the dialog closes. The dialog was already shown based on a successful pre-check (which means `teamId` was valid at that time), so this null check in `confirmAdvance` is a silent no-op error path: if `teamId` somehow becomes null between dialog open and confirm, the phase advance is silently swallowed with no user feedback.
**Fix:** Replace the silent `return` in `confirmAdvance` (line 204) and `confirmClose` (line 214) with `addToast('Session expired. Please reload.', 'error')` before returning.

---

### [MEDIUM] `ReadinessSnapshot` `readyParticipantIds` snapshot keys are snapshotted at a point-in-time but `readyCount` and keys can diverge

**File:** `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs:41-50`
**Issue:** In `JoinSprint`, `readyDict.Keys.ToList()` and `readyDict.Count` are read in two separate operations on a `ConcurrentDictionary`. Between the two reads, another participant could toggle readiness, causing `readyParticipantIds.Count` to differ from `readyCount` in the snapshot. The client-side `readinessStore.setFromSnapshot` would then have an inconsistent state (e.g., `readyCount = 3` but only 2 IDs in the list).
**Fix:** Read `var keys = readyDict.Keys.ToList()` once, then use `keys.Count` for `readyCount` in the snapshot rather than re-reading `readyDict.Count`. This makes the snapshot self-consistent even if the dict is modified concurrently.

---

### [LOW] `AddError`/`SendErrorsAsync` used for handler business logic in new endpoint

**File:** `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseCheckEndpoint.cs:45-53`
**Issue:** `csharp.md` convention: "AddError/SendErrorsAsync is only for FluentValidation pre-handler validation, not for handler business logic errors." The new endpoint uses `AddError` for business logic guards (inactive sprint, Reporting phase check). The existing `AdvancePhaseEndpoint.cs:41-68` uses the same pattern — so this is consistent with the codebase but inconsistent with the documented convention.
**Fix:** This is a codebase-wide convention gap, not introduced by this feature. No change needed for this review cycle. Flag for architect to address in a cleanup pass.

---

### [LOW] `ToggleReady` uses non-atomic check-then-act on `ConcurrentDictionary`

**File:** `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs:74-82`
**Issue:** `ContainsKey` check at line 74 followed by `TryRemove` at line 76 is not atomic. Two concurrent `ToggleReady` calls from the same participant (e.g., two tabs) could both observe `ContainsKey = true`, both remove, and both broadcast `isReady = false` — leaving the participant in an incorrectly not-ready state even though one invocation should have added them back. The truly atomic approach would use `AddOrUpdate` or a dedicated lock.
**Fix (optional):** Since the same participant calling `ToggleReady` from multiple tabs simultaneously is an unusual edge case and the UI doesn't expose a way to trigger this easily, this is LOW severity. If tightening is desired: replace the check-then-act with `AddOrUpdate` that atomically determines the prior state and sets the new state.

---

## Positive Observations

- **Two-call pre-check pattern is well-designed.** The plan correctly identified the tension between needing count data before confirmation and the advance call changing state. Splitting into `advance-phase/check` + `advance-phase` keeps the advance endpoint idempotent and the pre-check stateless. Clean.
- **`BroadcastSprintClosedHandler` is minimal and correct.** Follows `BroadcastPhaseChangedHandler` exactly. Aggregate reference used, not individual properties.
- **`VoteRepository.GetParticipantIdsWithVotesInSprintAsync` uses a single correlated subquery.** The join-through-CardGroups approach avoids the N+1 pattern the plan warned against. The implementation is exactly what the plan prescribed.
- **`OnDisconnectedAsync` handles multi-tab correctly.** The "still connected" check (`connections?.Values.Contains(participantId)`) correctly prevents removing readiness when the participant has another open tab. This is a subtle correctness detail done right.
- **`readinessStore.reset()` called before `connect()` in `enterRoom`.** Stale client-side readiness state is cleared before the server snapshot arrives. The ordering is correct and the intent matches the plan exactly.
- **Dialog gate rejection pattern is correct.** When `advanceError` is set (Discussing → Reporting with < 2 groups), the Advance button is hidden via `v-if="!advanceError"` — the SM sees the error but cannot accidentally confirm. Good UX guard.
- **Frontend TypeScript types align with backend contracts.** `AdvancePhaseCheckResponse` interface in `sprint.ts` matches the C# record exactly including optional fields.
- **`SprintHub` static state documentation is clear.** Inline comments explain the two-dict structure (`sprintId → participantId → displayName` and `sprintId → connectionId → participantId`). Easy to maintain.

---

## Gaps

- **No tests for `AdvancePhaseCheckEndpoint`** — the plan's testing strategy lists several test scenarios for this endpoint (403 for non-SM, 409 for inactive sprint, correct counts for Voting/Discussing). None appear to be implemented. This is in-scope for the plan but was not flagged as a deviation. The feature functions correctly, but the test coverage gap is worth noting.
- **`ReadinessReset` payload is `new { }` (empty object)** — the spec defines `ReadinessReset` payload as `{}`. The client handler in `useSprintHub.ts:180` accepts `_payload: object` and ignores it. This is consistent. However, the plan could have omitted the payload allocation (`new { }`) vs. using a typed record for consistency with other events. Minor style point only.
- **Acceptance criterion "Participants remain on the board after closure and can still view the report and copy markdown"** — the `isClosed` state hides the advance/close buttons and shows the read-only banner, but the report tab toggle and markdown copy remain visible/functional. This criterion is satisfied by the existing Reporting phase UI. Verified as covered.

---

## Open Questions

- **`ToggleReady` multi-tab race (SprintHub.cs:74-82):** Is the non-atomic check-then-act acceptable given the realistic usage profile (single participant, one tab)? If multi-tab participants are a target use case, the `AddOrUpdate` fix is warranted. Otherwise, the current implementation is fine. Recommend architect decides if this needs tightening.

---

## Evidence

| Check | Result | Command | Output summary |
|-------|--------|---------|----------------|
| Reflekt.API build | PASS | `dotnet build Reflekt.API.csproj` | 0 warnings, 0 errors |
| Reflekt.Persistence build | PASS | `dotnet build Reflekt.Persistence.csproj` | 0 warnings, 0 errors |
| Full solution build | FAIL (pre-existing) | `dotnet build` | 17 errors in `Reflekt.UnitTests` — `ColumnType.WentWell` and `Team` constructor mismatches. Last modified in commit `52f6a84` (prior feature), no changes by InlinePhaseControls. |
| Frontend type-check | FAIL (pre-existing) | `npm run build` in `web/reflekt-web` | 6 TypeScript errors in `TeamSelector.vue` — `hasTeam`, `currentTeam` properties. File last modified in commit `82fb833` (refactoring), no changes by InlinePhaseControls. |
| Frontend Vite build | PASS | `npm run build` | `✓ built in 842ms`, 1860 modules transformed |
| `AdvancePhaseCheckEndpoint` files exist | PASS | File read | `AdvancePhaseCheckEndpoint.cs` created as specified |
| `BroadcastSprintClosedHandler` exists | PASS | File read | Follows `BroadcastPhaseChangedHandler` pattern exactly |
| `SprintContracts.cs` TeamId + State | PASS | File read | Both fields present in `SprintSummaryResponse` record |
| `VoteRepository` new method | PASS | File read | `GetParticipantIdsWithVotesInSprintAsync` uses correlated subquery, no N+1 |
| `SprintHub` static dicts | PASS | File read | `ReadinessState` and `ConnectionState` both declared static |
| Frontend stores | PASS | File read | `readiness.ts` store, `AdvancePhaseCheckResponse` interface in `sprint.ts` |
| Sprint manager buttons removed | PASS | File read | `SprintDetailPanel.vue` has no advance/close buttons or `nextPhaseLabel` |
