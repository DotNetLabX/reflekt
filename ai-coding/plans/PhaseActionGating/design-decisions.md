# Phase Action Gating — Design Decisions

**Source:** Solo-session agreement (2026-04-21) between user and assistant, before a new session picks this up for planning + implementation.
**Companion to:** `docs/features/PhaseActionGating.md`
**Purpose:** Capture the technical direction agreed on so the planner does not have to re-derive it. This is not a plan — the planner produces `plan.md` next to this file.

---

## Context: what was rejected and why

Two approaches were discussed and rejected:

### Rejected — In-code static matrix (switch statement in the Sprint aggregate)

**Shape:** `static readonly Dictionary<SprintPhase, HashSet<SprintAction>>` in Domain, or a `switch` expression inside a `Sprint.IsActionAllowed(action)` method. Endpoints call `sprint.GuardAction(action)` inline.

**Why rejected:** Inconsistent with the master-data pattern already used by the user's other project (`dotnet-microservices`). The user's convention is to store reference/policy data in a DB table seeded from JSON and cached at startup. Keeping the matrix as code would be a one-off style in this codebase, and would also make the FE policy endpoint more awkward (it would have to serialise an in-memory structure with no backing entity).

### Rejected — Stateless library (as used in `dotnet-microservices`)

**Shape:** `Stateless.StateMachine<SprintPhase, SprintAction>` built from cached `ArticleStageTransition`-style rows, with `CanFire(action)`.

**Why rejected:** In the reference project, Stateless is used because article actions *cause* stage transitions (Submit moves Created → Submitted). Reflekt's model is different: only `AdvancePhase` causes a phase transition; every other action is a no-op against the phase. So there is no state-machine graph to traverse — just a flat `(Phase, Action)` permission lookup. Using Stateless would be overkill for a simple set-membership check.

---

## Chosen approach

A simplified version of the `dotnet-microservices` pattern, dropping Stateless and the audit apparatus, keeping the master-data + cache + action-carrying-command shape.

### 1. SprintAction enum (Domain)

- Location: `Reflekt.Domain/Sprints/SprintAction.cs`
- Values exactly as listed in the feature spec: `CreateNote, EditNote, DeleteNote, CreateActionItem, EditActionItem, DeleteActionItem, CastVote, RetractVote, AttachEvidenceLink, DetachEvidenceLink, AdvancePhase`.
- This is the canonical identifier for every gated action in the sprint surface.

### 2. PhaseAction entity + master data table

- Location: `Reflekt.Domain/Sprints/PhaseAction.cs`
- Shape: composite key `(SprintPhase Phase, SprintAction Action)`. No `DestinationPhase` column — actions do not cause transitions (except AdvancePhase itself, whose logic stays in the aggregate).
- EF configuration in `Reflekt.Persistence/EntityConfigurations/PhaseActionConfiguration.cs`.
- Seed file in `Reflekt.Persistence/MasterData/PhaseAction.json` (match existing master-data seeding convention — check whether one exists; if not, establish it here and document for future master-data features).
- Migration: new EF Core migration adds the table and seeds it. Migration also handles the phase rename (see §5 below).

### 3. Caching layer

- `PhaseActionCache` service — loads all `PhaseAction` rows once at application startup, exposes `bool IsAllowed(SprintPhase phase, SprintAction action)` via an in-memory `HashSet<(SprintPhase, SprintAction)>`.
- DI registration: singleton. Rows don't change at runtime.
- Seeded into an in-memory cache, not per-request `IMemoryCache`. Simpler and matches the "static product data" mental model.

### 4. Command-carries-action pattern

- Interface in Domain: `ISprintAction { SprintAction ActionType { get; } }`.
- Every FastEndpoints request class for a gated endpoint implements this interface:

```csharp
public sealed record CreateCardRequest(string Text, ColumnType ColumnType, CardType CardType)
    : ISprintAction
{
    public SprintAction ActionType => CardType == CardType.ActionItem
        ? SprintAction.CreateActionItem
        : SprintAction.CreateNote;
}
```

- Note the conditional — the same endpoint maps to two different SprintAction values depending on the card type. This keeps the gating granular (phase Writing permits CreateNote but not CreateActionItem, even though both go through `POST /api/sprints/{sprintId}/cards`).

### 5. Phase rename: Closed → Reporting

- `SprintPhase.Closed` → `SprintPhase.Reporting` in `Reflekt.Domain/Sprints/SprintPhase.cs`.
- Update `Sprint.AdvancePhase()` switch — the existing `SprintPhase.Discussing => SprintPhase.Closed` becomes `SprintPhase.Discussing => SprintPhase.Reporting`, and `SprintPhase.Closed => throw` becomes `SprintPhase.Reporting => throw`.
- Update `AdvancePhaseEndpoint.cs:38` guard: `if (sprint.CurrentPhase == SprintPhase.Closed)` → `Reporting`.
- Frontend string comparisons: `currentPhase === 'Closed'` → `'Reporting'` wherever the phase is compared (check `SprintView.vue`, `RetroRoomView.vue`, `ReportView.vue`, `stores/sprint.ts`, `composables/useSprintHub.ts`, `router/index.ts`).
- Existing feature specs: update `docs/features/Sprints.md` references (rule #6, the state/phase prose, the phase machine diagram if any).
- Migration: the EF migration that introduces the `PhaseAction` table also runs a data fix-up — any existing sprint with `CurrentPhase = Closed` (enum integer value) must be migrated to the new value. If the enum integer values are kept stable (just renaming the identifier, same integer), this is a no-op at the data layer; only code changes.
- The planner should confirm this assumption by reading the current enum definition before writing the migration.

### 6. Guard entry point

- Extension method or static helper (team can pick the style):

```csharp
public static void GuardAction(this Sprint sprint, SprintAction action, PhaseActionCache cache)
{
    if (!cache.IsAllowed(sprint.CurrentPhase, action))
        throw new DomainException($"{action} not allowed in phase {sprint.CurrentPhase}.");
}
```

- Called from each gated endpoint after loading the sprint, before domain operations:

```csharp
var sprint = await sprintRepo.GetByIdOrThrowAsync(request.SprintId, ct);
sprint.GuardAction(request.ActionType, phaseActionCache);
// ... existing logic
```

- `DomainException` mapped to HTTP 409 by the existing exception handler. The planner should verify the existing mapping returns a problem-details body (spec requires naming the action and current phase in the 409 response).

### 7. FE read endpoint

- New anonymous endpoint: `GET /api/phase-actions`.
- Returns the matrix as an array: `[{ "phase": "Writing", "actions": ["CreateNote", "EditNote", ...] }, ...]`.
- Source: iterates `PhaseActionCache` and groups by phase.
- No auth — product rules are public.

### 8. FE policy store

- Pinia store at `web/reflekt-web/src/stores/policy.ts`:
  - State: `matrix: Record<string, Set<string>>` keyed by phase name.
  - `fetch()` — called once at app bootstrap; populates matrix from `GET /api/phase-actions`.
  - `isAllowed(phase: string, action: string): boolean`.
- Call `policyStore.fetch()` in the root App mount or router beforeEach guard — once per session.

### 9. FE UI integration

- Every interactive control (note input, vote button, evidence-link form, advance-phase button, add-action-item form) checks `policyStore.isAllowed(sprint.currentPhase, '<ActionName>')`.
- Components either hide the control (`v-if`) or disable it (`:disabled`) based on the check.
  - **Hide** when the control makes no sense outside its phase (e.g., note-add input in Voting — there's nothing to add).
  - **Disable** when the control would be useful to see but must not fire (team preference; apply consistently — recommend the planner pick one default and call it out).
- Action Items column in `RetroRoomView.vue`: visible only when `policyStore.isAllowed(currentPhase, 'CreateActionItem')` is true. Grid layout switches between `grid-cols-3` and `grid-cols-2` based on this.
- Phase must be available on the RetroRoom. Today `fetchSprintSummary` returns only `{id, name}` — extend `SprintSummaryResponse` to include `currentPhase`. Wire `PhaseChanged` SignalR handler in `useSprintHub.ts` to update `sprintStore.currentSummary.currentPhase` on broadcast (currently the handler just logs).

### 10. Dropped for v1

- No `Stateless` dependency.
- No audit log of forbidden-action attempts (beyond the standard request log).
- No `ActionExecuted` domain event per action.
- No per-team / per-sprint rule overrides.
- No admin UI for editing the matrix.

---

## Seeded matrix (canonical)

From the feature spec. Every row below becomes one seeded `PhaseAction` entry.

| Phase | Allowed actions |
|---|---|
| Writing | CreateNote, EditNote, DeleteNote, AttachEvidenceLink, DetachEvidenceLink, AdvancePhase |
| Voting | CastVote, RetractVote, AdvancePhase |
| Discussing | CreateActionItem, EditActionItem, DeleteActionItem, AttachEvidenceLink, DetachEvidenceLink, AdvancePhase |
| Reporting | *(none)* |

---

## Scope interactions to flag to the planner

### Housekeeping before implementation

Before planning, update the following so this feature doesn't land on stale docs:

1. `docs/features/Sprints.md` — rule #4 ("Phases are soft") is superseded; update the rule or add a "See also: PhaseActionGating.md" note.
2. `docs/features/Sprints.md` — every mention of phase `Closed` becomes `Reporting`. Includes the phase machine diagram, API response field descriptions, user flow step wording, and acceptance criteria lines that reference "phase Closed" by name.
3. `docs/features/Voting.md` — rule mentions "Closed sprint" (state, not phase) — verify these stay as-is since they refer to `Sprint.State = Closed`, which is unchanged.
4. `docs/backlog.md` §17 (Action Items only in Discussing) — mark as absorbed into PhaseActionGating.
5. `docs/bugs.md` — add BUG-004 entry referencing PhaseActionGating.md, then mark it as "In plan" or "Absorbed" so it isn't picked up independently.

### Existing handler review

Check existing aggregate methods and endpoints for phase references that will no longer be needed once the gating infra is in place:

- `Sprint.AdvancePhase()` — keep, but `AdvancePhaseEndpoint` phase check becomes redundant once `GuardAction(SprintAction.AdvancePhase)` is added. Decide whether to remove the inline check or leave as belt-and-braces.
- No other endpoint currently has phase checks (confirmed during solo session).

### Test coverage expectations

The planner should specify test coverage for:

- Each gated endpoint returning 409 in every forbidden phase.
- Each gated endpoint succeeding in every permitted phase.
- `GET /api/phase-actions` returning the correct matrix.
- `PhaseActionCache` correctly loading seeded rows.
- The SprintPhase rename (`Closed` → `Reporting`) not leaving any stale string comparisons in the frontend (grep for `'Closed'` in `web/reflekt-web/src/` as a guard).

### Estimated footprint

Rough count from the solo-session investigation (for planning, not binding):

- **Backend:** ~12 files. Enum, entity, configuration, seed JSON, cache, cache registration, guard extension, new endpoint, ~7 request classes gaining `ISprintAction`, migration, `Sprint.AdvancePhase` edit, `AdvancePhaseEndpoint` edit.
- **Frontend:** ~6 files. Policy store, summary-response type update, `useSprintHub.ts` PhaseChanged handler, `RetroRoomView.vue` gating, `SprintView.vue` phase display strings, router or app bootstrap to call `policyStore.fetch()`.
- **Docs:** ~3 files. `Sprints.md` updates, `backlog.md` §17 mark, `bugs.md` BUG-004 entry.

Call it ~20 files total. Definitely team-pipeline shaped.

---

## Hand-off note

The new session should start by reading:

1. `docs/features/PhaseActionGating.md` — the product spec.
2. This file — the technical direction.
3. `docs/features/Sprints.md` and `docs/features/Cards.md` — to understand how existing features describe the phases and mutations.
4. `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs` and `Behaviors/Sprint.cs` — the existing aggregate surface.
5. `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs` — the only current place phase logic is enforced.

Then the Architect/Planner produces `plan.md` in this directory.
