# PhaseActionGating — Review

## Verdict: APPROVE (after Cycle 1 fixes)

---

## Cycle 1 Re-review

All four findings resolved and verified with fresh builds.

- HIGH (canAttachLink): `CardItem` now has `canAttachLink?: boolean` prop. All three `CardItem` usages in `RetroRoomView` pass `:can-attach-link="policyStore.isAllowed(currentPhase, 'AttachEvidenceLink')"`. The `??` fallback to `canEdit` is dead code (all callers pass the prop explicitly) but harmless. Evidence link and detach buttons now gate on the correct action.
- MEDIUM (ownership before phase gate): `EditCardEndpoint` and `DeleteCardEndpoint` now perform ownership check first (lines 32–50), then phase gate (lines 52–54). Non-owners get 403 before phase gate fires.
- LOW (ordering): `GetPhaseActionsEndpoint` now has `.OrderBy(kvp => kvp.Key)` — deterministic enum order in response.
- LOW (await): `policyStore.fetch()` is awaited before `sprintStore.fetchSprintSummary()` in `onMounted`. Race eliminated.

**Backend build:** 0 errors, 0 warnings. **Frontend build:** 0 type errors, clean Vite bundle.

---

## Original Review (Cycle 1)

## Pre-commitment Predictions

| Prediction | Outcome |
|------------|---------|
| PhaseActionCache DI lifetime (captive dependency) | WRONG — IServiceScopeFactory used correctly; scope created and disposed in constructor |
| Phase gate ordering — wrong position in some endpoints | PARTIALLY RIGHT — gate is after closed-sprint check in all endpoints (matches plan), but before ownership in EditCard/DeleteCard (see MEDIUM finding) |
| Frontend reactive update via direct property mutation | WRONG — Pinia reactive state supports direct mutation; no issue |
| Missing AdvancePhase in seed matrix | WRONG — AdvancePhase (ordinal 10) present in Writing, Voting, and Discussing phases |
| Sprint state vs phase confusion | RIGHT (no problem) — all remaining `Closed` refs in backend are `SprintState.Closed`; `SprintPhase.Closed` fully removed |

## Findings

### [HIGH] Frontend gates evidence link and delete buttons with wrong action type

**File:** `web/reflekt-web/src/views/RetroRoomView.vue:245-251` and `web/reflekt-web/src/components/CardItem.vue:89-101,156`

**Issue:** `canMutateCard()` uses a single `EditNote`/`EditActionItem` action check to gate edit, delete, AND evidence link buttons. `CardItem` receives one `canEdit` prop that controls all three. This causes incorrect frontend gating:

- For a **Note card in Discussing phase**: `canMutateCard` checks `EditNote`, which is NOT in the Discussing matrix (Writing-only). Result: evidence link attach/detach buttons are hidden even though `AttachEvidenceLink` IS allowed in Discussing. A participant cannot attach evidence to a note during discussion — frontend blocks it; backend would allow it.
- `DetachEvidenceLink` has the same issue as `AttachEvidenceLink`.
- Delete uses a different action (`DeleteNote`/`DeleteActionItem`) but the matrix happens to align for notes (Writing only) and action items (Discussing only), so there is no behavioral gap for delete in practice.

The backend correctly enforces separate actions per operation. The frontend incorrectly collapses them into one `canEdit` gate.

**Fix:** Either:
1. Break `canEdit` into separate props on `CardItem` — `canEdit`, `canDelete`, `canAttachLink` — and pass phase-correct checks from `RetroRoomView` for each. This matches the plan's intent for `canMutateCard` but requires propagating separate permissions.
2. Simpler: add a `canAttachLink` prop to `CardItem` computed from `policyStore.isAllowed(currentPhase, 'AttachEvidenceLink')` and use it to independently gate the evidence link UI. Keep `canEdit` for edit+delete (which share the same phase window per card type).

---

### [MEDIUM] Phase gate fires before ownership check in EditCard and DeleteCard

**File:** `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs:29-34` and `DeleteCardEndpoint.cs:31-36`

**Issue:** The phase gate runs before the ownership check. A participant who does not own the card in a forbidden phase receives 409 (phase-gated) rather than 403 (forbidden). This leaks information: a non-owner can infer the current phase by observing whether they get 403 or 409.

The plan says to insert the gate "after the existing closed-sprint check" but does not specify its position relative to ownership. The pattern followed in `CreateCard`/`CastVote`/`RetractVote` is: auth claims first → load resources → state checks → business logic. `EditCard` and `DeleteCard` follow the same structure except the ownership check is deferred after load due to needing the card's `ParticipantId`. Placing the phase gate before ownership is pragmatically simpler (avoids an extra round-trip to load the team) but leaks phase state.

**Fix:** Move the phase gate to after the ownership check block (lines 36–53 in EditCard, lines 38–55 in DeleteCard). The check already has the card loaded, so the card type computation is unchanged.

---

### [LOW] `GetPhaseActionsEndpoint` response order is non-deterministic

**File:** `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/GetPhaseActions/GetPhaseActionsEndpoint.cs:14-16`

**Issue:** `PhaseActionCache.GetMatrix()` builds from a `HashSet<(SprintPhase, SprintAction)>` which has no defined iteration order. The endpoint's `Select(kvp => ...)` over the resulting `Dictionary` will return phases in hash-map order, not enum order. The frontend `isAllowed()` only does `includes()` lookups so ordering does not affect correctness, but the API response is non-deterministic between runs.

**Fix:** Sort the result by phase: `.OrderBy(kvp => kvp.Key)` before `Select`.

---

### [LOW] `policyStore.fetch()` not awaited in `onMounted`

**File:** `web/reflekt-web/src/views/RetroRoomView.vue:342`

**Issue:** `policyStore.fetch()` is called without `await`. If the policy matrix hasn't loaded before `currentPhase` is populated, `isAllowed` returns `false` for all actions and controls flash as hidden before appearing. This is a race condition that is typically invisible on fast local networks but will be visible on cold load against a real server.

**Fix:** `await policyStore.fetch()` before `sprintStore.fetchSprintSummary(sprintId)` (which also sets `currentPhase`), or ensure `policyStore.fetch()` is awaited before `joined.value = true` in `enterRoom()`.

## Positive Observations

- `PhaseActionCache` uses `IServiceScopeFactory` correctly — the scope is created and synchronously disposed in the constructor, avoiding captive dependency. Sync-over-async with `.GetAwaiter().GetResult()` is acceptable here and the plan explicitly blessed it.
- Migration ordinals verified precisely: enum ordering matches the 15 seeded rows in the migration — no silent offset errors.
- All 8 mutation endpoints consistently use `ThrowError(..., StatusCodes.Status409Conflict)` for both the closed-sprint guard and the phase gate, matching the existing codebase pattern.
- `Sprint.AdvancePhase()` correctly throws `InvalidOperationException` for `Reporting` as a domain-level guard, providing defense-in-depth behind the endpoint's phase gate.
- The `CreateCardCommand.ActionType` getter correctly uses string comparison rather than the parsed enum, matching the flow where validation runs before the enum parse in the endpoint.
- Frontend `canMutateCard` combining ownership and phase into one computed is clean — the issue is which action name it checks, not the pattern itself.
- `SprintView.vue`: `phaseOrder` array and `currentPhase !== 'Reporting'` guard are correctly updated; `stateBadgeClass` `'Closed'` case correctly left alone (that is sprint state, not phase).

## Gaps

- No test evidence provided for any scenario (per plan's testing strategy). The plan called for systematic testing of every forbidden-phase cell. This is out of scope for code review to verify but worth noting.
- `policyStore.fetch()` has no error handling — a failed fetch leaves `matrix` as `{}` and silently disables all actions for the session.

## Open Questions

- None. All findings have HIGH confidence based on direct code evidence.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS | `dotnet build Reflekt.API.csproj` | 0 errors, 0 warnings |
| Frontend build | PASS | `npm run build` (in web/reflekt-web) | 0 type errors, clean Vite bundle in 514ms |
| Migration ordinals | VERIFIED | Manual cross-check against SprintPhase/SprintAction enums | 15 rows correct |
| Phase rename completeness | VERIFIED | `grep -r "Closed" src/**/*.cs` | All hits are SprintState.Closed or ClosedAt — no SprintPhase.Closed remaining |
