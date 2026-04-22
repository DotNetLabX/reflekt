# PhaseActionGating — Implementation

## Files Created

- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintAction.cs` — enum with 11 values (CreateNote through AdvancePhase)
- `src/Services/Reflekt/Reflekt.Domain/Sprints/ISprintAction.cs` — interface with single `SprintAction ActionType { get; }` property
- `src/Services/Reflekt/Reflekt.Domain/Sprints/PhaseAction.cs` — entity with composite key (Phase, Action), private parameterless ctor for EF + public ctor for seeding
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/PhaseActionConfiguration.cs` — IEntityTypeConfiguration<PhaseAction>, composite PK, 15 seed rows matching the spec matrix
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260421183228_AddPhaseActions.cs` — creates PhaseActions table with composite PK, seeds 15 rows, does not touch Sprints table
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/PhaseActionCache.cs` — singleton, loads rows at construction via IServiceScopeFactory, exposes IsAllowed() and GetMatrix()
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/GetPhaseActions/PhaseActionsResponse.cs` — response record PhaseActionsEntry(string Phase, List<string> Actions)
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/GetPhaseActions/GetPhaseActionsEndpoint.cs` — anonymous GET /api/phase-actions, returns full matrix from cache
- `web/reflekt-web/src/stores/policy.ts` — Pinia store with matrix state, fetch() action, isAllowed() getter

## Files Modified

- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintPhase.cs` — renamed Closed → Reporting (ordinal 3 preserved)
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` — updated AdvancePhase() switch: Discussing → Reporting, Reporting → throw
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs` — replaced manual Reporting check with matrix-driven phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.AdvancePhase); injected PhaseActionCache
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — added DbSet<PhaseAction> PhaseActions
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — registered PhaseActionCache as singleton
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` — added CurrentPhase to SprintSummaryResponse
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` — passes sprint.CurrentPhase.ToString() to SprintSummaryResponse
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardCommand.cs` — implements ISprintAction; ActionType computed from CardType string
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs` — injected PhaseActionCache; phase gate after cardType enum parse using parsed enum (not string)
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs` — injected PhaseActionCache; phase gate after closed-sprint check, action computed from card.CardType
- `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs` — injected PhaseActionCache; phase gate after closed-sprint check, action computed from card.CardType
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/CastVoteEndpoint.cs` — injected PhaseActionCache; phase gate after closed-sprint check with SprintAction.CastVote
- `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/RetractVoteEndpoint.cs` — injected PhaseActionCache; phase gate after closed-sprint check with SprintAction.RetractVote
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkEndpoint.cs` — injected PhaseActionCache; phase gate after closed-sprint check with SprintAction.AttachEvidenceLink
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/DetachEvidenceLinkEndpoint.cs` — injected PhaseActionCache; phase gate after closed-sprint check with SprintAction.DetachEvidenceLink
- `web/reflekt-web/src/composables/useSprintHub.ts` — imported useSprintStore; PhaseChanged handler now updates sprintStore.currentSummary.currentPhase
- `web/reflekt-web/src/stores/sprint.ts` — added currentPhase: string to SprintSummary interface
- `web/reflekt-web/src/views/RetroRoomView.vue` — imported usePolicyStore; added policyStore and currentPhase computed; canEditCard → canMutateCard (checks ownership + phase); note add inputs gated with v-if CreateNote; Action Items column gated with v-if CreateActionItem; grid switches 3-col/2-col based on CreateActionItem; show-voting becomes dynamic CastVote check; policyStore.fetch() called in onMounted
- `web/reflekt-web/src/views/SprintView.vue` — 'Closed' → 'Reporting' in phase guard and phaseOrder array
- `docs/features/Sprints.md` — updated CurrentPhase type, phase machine, Flow 3, error responses, business rules #3 and #4, acceptance criteria, Out of Scope
- `docs/features/Cards.md` — updated business rule #10: phases are now enforced, references PhaseActionGating
- `docs/backlog.md` — §17 marked as absorbed into PhaseActionGating feature
- `docs/bugs.md` — added BUG-004 (absorbed into PhaseActionGating)

## Key Decisions

- **15 seed rows, not 14**: the spec matrix has 15 permitted (phase, action) pairs (Writing×6, Voting×3, Discussing×6). The plan's "14 rows" count was off by one; spec matrix is authoritative.
- **Phase gate placement in CreateCard**: moved after the CardType enum parse so the action is computed from the parsed enum, not the raw string. This is cleaner and avoids a redundant string comparison.
- **No ISprintAction on Edit/Delete commands**: commands don't carry CardType (it comes from the loaded entity). The action is computed inline after card load, consistent with the plan's decision.
- **canMutateCard replaces canEditCard**: combines ownership check and phase check in one function. The phase check uses the EditNote/EditActionItem action based on card type.

## Review Fixes (Cycle 1)

- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs` — moved phase gate to after ownership check; ownership check now runs first, returning 403 before 409 for non-owners
- `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs` — same: ownership check now precedes phase gate
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/GetPhaseActions/GetPhaseActionsEndpoint.cs` — added `.OrderBy(kvp => kvp.Key)` for deterministic phase ordering in response
- `web/reflekt-web/src/components/CardItem.vue` — added optional `canAttachLink` prop; evidence link attach UI and detach button now gate on `canAttachLink ?? canEdit` independently from edit/delete
- `web/reflekt-web/src/views/RetroRoomView.vue` — pass `:can-attach-link="policyStore.isAllowed(currentPhase, 'AttachEvidenceLink')"` to all three CardItem usages; `policyStore.fetch()` now awaited in onMounted to eliminate flash-of-hidden-controls race

## Deviations from Plan

- **Seed row count 15 vs 14**: spec matrix has 15 rows; plan said 14. Spec wins.
- **AdvancePhase endpoint**: plan said replace the old Reporting-check with the cache check. Done, but the old explicit check (`sprint.CurrentPhase == SprintPhase.Reporting`) was already updated to Reporting in Step 1 before being fully replaced by the matrix check in Step 7 — net result is identical (matrix check is the only guard now).
