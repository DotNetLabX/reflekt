# Phase Action Gating

## Context

Today, sprint phases are informational only — participants can perform any action in any phase. This feature makes phases enforceable: every mutation action is gated by the current phase using an action matrix stored as seeded master data. The backend rejects forbidden actions with 409; the frontend proactively hides/disables controls. As part of this work, the phase enum value `Closed` is renamed to `Reporting` to remove the naming collision with the sprint state `Closed`.

**Services impacted:** Reflekt (all three projects) + frontend SPA.

## Scope

**In scope:**
- `SprintAction` enum in Domain
- `PhaseAction` entity + EF configuration + seed data
- `PhaseActionCache` singleton loaded at startup
- `ISprintAction` interface on request classes
- Phase gate check in all 8 mutation endpoints
- `GET /api/phase-actions` anonymous endpoint
- Phase rename `Closed` → `Reporting` in enum, aggregate, endpoints, frontend, and Sprints feature spec
- `SprintSummaryResponse` extended with `CurrentPhase`
- Frontend policy store, RetroRoomView gating, SprintView phase rename, SignalR handler update
- Doc updates: `docs/features/Sprints.md` rule #4 and phase references, `docs/backlog.md` §17

**Explicitly out of scope:**
- Per-team or per-sprint custom rules
- Runtime admin UI for editing the matrix
- Audit log of blocked actions
- Phase-gating for CloseSprint or StartSprint (state-machine actions, not phase actions)
- Card text blurring in Writing phase (BUG-003 — separate feature)
- Grace window in Reporting phase

## Domain Model Changes

### New enum: `SprintAction`
Values: `CreateNote`, `EditNote`, `DeleteNote`, `CreateActionItem`, `EditActionItem`, `DeleteActionItem`, `CastVote`, `RetractVote`, `AttachEvidenceLink`, `DetachEvidenceLink`, `AdvancePhase`.

### New entity: `PhaseAction`
Composite key `(SprintPhase Phase, SprintAction Action)`. Not an aggregate root — it is static product data with no lifecycle or domain events. No `Id` column, no inheritance from `Entity<int>`.

### Modified enum: `SprintPhase`
`Closed` renamed to `Reporting`. Integer value stays the same (ordinal 3) so no data migration needed for the `CurrentPhase` column.

### New interface: `ISprintAction`
`SprintAction ActionType { get; }` — implemented by each gated request class.

## Data Model Changes

### New table: `PhaseActions`
| Column | Type | Constraint |
|--------|------|-----------|
| Phase | int (SprintPhase) | Composite PK part 1 |
| Action | int (SprintAction) | Composite PK part 2 |

Seeded with 14 rows matching the spec matrix. No FK relationships.

### No data migration needed for phase rename
`SprintPhase` is stored as an integer. Renaming the C# identifier from `Closed` to `Reporting` doesn't change the stored value (ordinal 3 stays ordinal 3). Existing sprints with `CurrentPhase = 3` are automatically "Reporting" after the code change. The EF migration for this feature only adds the `PhaseActions` table and seeds it.

## Implementation Steps

### Step 1: Phase rename (Closed → Reporting) — Domain + API

Rename the enum value and update all code references. This is a pure rename with no behavior change.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintPhase.cs` — rename `Closed` to `Reporting`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` — update `AdvancePhase()` switch: `Discussing => SprintPhase.Reporting`, `SprintPhase.Reporting => throw`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs:38` — change `SprintPhase.Closed` guard to `SprintPhase.Reporting`

**Pattern:** Follow existing `Sprint.AdvancePhase()` switch style at `Reflekt.Domain/Sprints/Behaviors/Sprint.cs`.

**Verify:** `dotnet build` passes.

### Step 2: SprintAction enum + ISprintAction interface — Domain

Create the action vocabulary and the interface that request classes will implement.

**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintAction.cs` — enum with 11 values as listed above
- `src/Services/Reflekt/Reflekt.Domain/Sprints/ISprintAction.cs` — interface with single property `SprintAction ActionType { get; }`

**Pattern:** Follow existing enum style at `SprintPhase.cs` (single-line enum in its own file).

**Verify:** `dotnet build` passes.

### Step 3: PhaseAction entity + EF configuration + seed data — Domain + Persistence

Create the entity, configure the composite key, and seed the matrix.

**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/PhaseAction.cs` — simple entity with two properties: `SprintPhase Phase` and `SprintAction Action`. No base class (not `Entity<int>` — composite key, no single Id). Private parameterless constructor for EF + public constructor `PhaseAction(SprintPhase phase, SprintAction action)`.
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/PhaseActionConfiguration.cs` — `IEntityTypeConfiguration<PhaseAction>`. Table name `PhaseActions`. Composite key `builder.HasKey(x => new { x.Phase, x.Action })`. Both properties stored as int (enum default). Seed data via `builder.HasData(...)` with all 14 rows from the spec matrix.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `DbSet<PhaseAction> PhaseActions => Set<PhaseAction>();`

**Pattern:** Follow existing EF configuration style at `SprintConfiguration.cs`. Use `HasData` for seeding since this is static product data that belongs in the migration, not in `DbInitializer`.

**Note:** `PhaseActionConfiguration` does NOT inherit from the `EntityConfiguration<T>` base class because `PhaseAction` is not an `Entity<int>`. It implements `IEntityTypeConfiguration<PhaseAction>` directly.

**Verify:** `dotnet build` passes.

### Step 4: EF Core migration

Generate the migration that creates the `PhaseActions` table with seeded data.

**Command:**
```bash
dotnet ef migrations add AddPhaseActions -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

**Verify:** Inspect generated migration file. Confirm it creates the `PhaseActions` table with composite PK and seeds 14 rows. Confirm it does NOT modify the `Sprints` table (the phase rename is integer-stable). Run `dotnet build`.

### Step 5: PhaseActionCache — API

Create the singleton cache that loads all PhaseAction rows at startup and exposes the permission check.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/PhaseActionCache.cs` — class with:
  - Private `HashSet<(SprintPhase, SprintAction)> _allowed`
  - Constructor takes `IServiceProvider` (or use `IServiceScopeFactory`), creates a scope, reads `ReflektDbContext.PhaseActions.ToListAsync()`, populates the HashSet
  - `bool IsAllowed(SprintPhase phase, SprintAction action)` — HashSet lookup
  - `Dictionary<SprintPhase, List<SprintAction>> GetMatrix()` — groups all entries by phase, used by the read endpoint

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — register `PhaseActionCache` as singleton. Because it needs to resolve a scoped `DbContext`, use the factory pattern: `services.AddSingleton<PhaseActionCache>(sp => { var scope = sp.CreateScope(); ... })` or use `IServiceScopeFactory` inside the cache constructor.

**Pattern:** Singleton service registered in the existing `AddReflektApi` method, following the DI registration style at `DependencyInjection.cs:17-18`.

**Design decision:** Use `IServiceScopeFactory` in the constructor rather than accepting `IServiceProvider` directly — this is cleaner and makes the scoping intent explicit. Load data in the constructor (sync-over-async via `.GetAwaiter().GetResult()` at startup is acceptable for a one-time seed load). Alternative: use an `IHostedService` to load async. Either approach is fine — pick the simpler one.

**Verify:** `dotnet build` passes.

### Step 6: Phase guard extension method — Domain

Create the guard that endpoints will call. Lives in Domain because it operates on the `Sprint` aggregate and `SprintAction`/`SprintPhase` types which are all Domain types. However, it also needs `PhaseActionCache` which is in API. 

**Revised placement:** The guard lives in API, not Domain, because it depends on `PhaseActionCache` (an API-layer service). Domain stays dependency-free.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/SprintPhaseGuard.cs` — static class with extension method:
  ```
  public static void GuardPhaseAction(this Sprint sprint, SprintAction action, PhaseActionCache cache)
  ```
  If `!cache.IsAllowed(sprint.CurrentPhase, action)`, throw with a message naming the action and current phase. Use `ThrowError` is not available here (it's a FastEndpoints endpoint method), so throw a custom exception.

**Exception handling decision:** The existing codebase uses `ThrowError` inline in endpoints (FastEndpoints method). For the extension method that lives outside an endpoint class, we need a throwable exception that maps to 409. Two options:
1. Throw `InvalidOperationException` and catch it in each endpoint — messy, no better than inline.
2. Don't use an extension method. Instead, call `cache.IsAllowed()` directly in each endpoint and use `ThrowError` inline.

**Recommendation:** Option 2. Each endpoint already has the `ThrowError` method available. The guard becomes a one-liner in each endpoint: `if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, request.ActionType)) ThrowError($"{request.ActionType} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);`. No extension method file needed. This matches the existing pattern where each endpoint calls `ThrowError("Sprint is closed.", ...)` inline.

**Files to create:** None (guard is inline in Step 7).

**Verify:** N/A — no standalone file.

### Step 7: ISprintAction on request classes + phase gate in endpoints — API

Add `ISprintAction` to each gated request class and insert the phase check in each endpoint handler.

**Files to modify (request classes — add `ISprintAction`):**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardCommand.cs` — implement `ISprintAction`. `ActionType` is conditional: `CardType == "ActionItem" ? SprintAction.CreateActionItem : SprintAction.CreateNote`. Since `CardType` is a string in the request, the `ActionType` getter must parse it. Compute it after the string is validated: implement as a method body that checks `CardType` string. Note: the enum parse happens in the endpoint handler (line 36-39), not the request. So the `ISprintAction` on `CreateCardCommand` should use the string comparison: `public SprintAction ActionType => CardType == "ActionItem" ? SprintAction.CreateActionItem : SprintAction.CreateNote;`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardCommand.cs` — implement `ISprintAction`. `ActionType` must also be conditional on card type, but `EditCardCommand` doesn't carry `CardType`. The endpoint loads the card first and knows its type. Two approaches: (a) add a `CardType` property to the command (but it's not in the request body — it comes from the loaded card), or (b) don't put `ISprintAction` on edit/delete commands and instead compute the action in the endpoint after loading the card. **Decision:** For edit and delete card endpoints, compute the `SprintAction` in the endpoint handler after loading the card (the card's `CardType` determines whether it's `EditNote`/`EditActionItem` or `DeleteNote`/`DeleteActionItem`). These commands do NOT implement `ISprintAction`.
- `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs` — same reasoning as edit: the `DeleteCardRequest` doesn't know the card type. Compute the action inline.
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/CastVoteEndpoint.cs` — `CastVoteRequest` implements `ISprintAction` with `ActionType => SprintAction.CastVote` (always the same action).
- `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/RetractVoteEndpoint.cs` — `RetractVoteRequest` implements `ISprintAction` with `ActionType => SprintAction.RetractVote`.
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkCommand.cs` — implements `ISprintAction` with `ActionType => SprintAction.AttachEvidenceLink`.
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/DetachEvidenceLinkEndpoint.cs` — `DetachEvidenceLinkRequest` implements `ISprintAction` with `ActionType => SprintAction.DetachEvidenceLink`.

**Files to modify (endpoints — add phase gate + inject `PhaseActionCache`):**

Each endpoint adds `PhaseActionCache` to its constructor and inserts the phase check after the existing `sprint.State == SprintState.Closed` check. The check pattern:

```csharp
// After existing closed-sprint check:
var action = SprintAction.CreateNote; // or computed from card type, or from request.ActionType
if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, action))
    ThrowError($"{action} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);
```

Endpoints to modify (8 total):
1. `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs` — inject `PhaseActionCache`. After the closed-sprint check (line 45-46), compute action from request (`request.ActionType` since `CreateCardCommand` implements `ISprintAction`), then gate. Note: the `ActionType` getter on `CreateCardCommand` uses the string `CardType` field, which has already been validated as non-empty by the validator but not yet parsed to the enum. The string comparison `"ActionItem"` is sufficient for gating purposes because if the string is invalid, the endpoint's own enum parse (line 36-38) would have already thrown. However, the phase gate runs before the enum parse in the current flow. **Reorder:** Move the phase gate to after the `CardType` enum parse so we know the type is valid. This also means we can compute the action from the parsed `cardType` enum directly instead of the string.
2. `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs` — inject `PhaseActionCache`. After the closed-sprint check, compute action from `card.CardType`: `var action = card.CardType == CardType.ActionItem ? SprintAction.EditActionItem : SprintAction.EditNote;` then gate.
3. `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs` — inject `PhaseActionCache`. After the closed-sprint check, compute action from `card.CardType`: `var action = card.CardType == CardType.ActionItem ? SprintAction.DeleteActionItem : SprintAction.DeleteNote;` then gate.
4. `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/CastVoteEndpoint.cs` — inject `PhaseActionCache`. After the closed-sprint check (line 45-46), gate with `SprintAction.CastVote`.
5. `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/RetractVoteEndpoint.cs` — inject `PhaseActionCache`. After the closed-sprint check (line 36-37), gate with `SprintAction.RetractVote`.
6. `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkEndpoint.cs` — inject `PhaseActionCache`. After the closed-sprint check (line 28-29), gate with `SprintAction.AttachEvidenceLink`.
7. `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks\DetachEvidenceLink/DetachEvidenceLinkEndpoint.cs` — inject `PhaseActionCache`. After the closed-sprint check (line 30-31), gate with `SprintAction.DetachEvidenceLink`.
8. `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs` — inject `PhaseActionCache`. After the existing `sprint.State != Active` check (line 31-36), replace the existing `sprint.CurrentPhase == SprintPhase.Closed` check (line 38-43) with the phase gate: `if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.AdvancePhase))`. This replaces the old manual Closed-phase check with the matrix-driven check, which achieves the same result (AdvancePhase is not in Reporting's allowed set) but is now consistent with the gating infrastructure. Update the error message to match the standard format.

**Verify:** `dotnet build` passes.

### Step 8: GET /api/phase-actions endpoint — API

New anonymous read endpoint that returns the full action matrix.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/GetPhaseActions/GetPhaseActionsEndpoint.cs` — endpoint class. AllowAnonymous. Returns the matrix from `PhaseActionCache.GetMatrix()` as `[{ phase: "Writing", actions: ["CreateNote", "EditNote", ...] }, ...]`.
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/GetPhaseActions/PhaseActionsResponse.cs` — response record: `record PhaseActionsEntry(string Phase, List<string> Actions)`. The endpoint returns `List<PhaseActionsEntry>`.

**Pattern:** Follow existing anonymous endpoint style at `GetSprintSummaryEndpoint.cs`. Use `[AllowAnonymous]`, `[HttpGet("/api/phase-actions")]`, `[Tags("PhaseActions")]`.

**Skill:** `create-feature` (FastEndpoints variant) — but this is a simple read endpoint with no command/validator, so just create the endpoint + response directly.

**Verify:** `dotnet build` passes.

### Step 9: SprintSummaryResponse + PhaseChanged handler — API + Frontend wiring

Extend the sprint summary to include `CurrentPhase` so the retro room knows the phase, and wire the SignalR handler to update it.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs:15` — change `SprintSummaryResponse(int Id, string Name)` to `SprintSummaryResponse(int Id, string Name, string CurrentPhase)`.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs:19` — pass `sprint.CurrentPhase.ToString()` as the third argument.
- `web/reflekt-web/src/stores/sprint.ts` — add `currentPhase: string` to `SprintSummary` interface (line 14-17).
- `web/reflekt-web/src/composables/useSprintHub.ts:39-41` — replace the `console.log` in the `PhaseChanged` handler with: import `useSprintStore`, then `sprintStore.currentSummary.currentPhase = phase` (update the reactive state).

**Verify:** `dotnet build` passes. Frontend: `npm run build` in `web/reflekt-web/`.

### Step 10: Frontend policy store — Frontend

Create the Pinia store that fetches and caches the action matrix.

**Files to create:**
- `web/reflekt-web/src/stores/policy.ts` — Pinia store with:
  - State: `matrix` as `Record<string, Set<string>>` (keyed by phase name, value is set of allowed action names). Since Pinia can't serialize `Set` for devtools, use `Record<string, string[]>` instead and do `includes()` for the check.
  - Action: `fetch()` — calls `GET /api/phase-actions`, populates matrix.
  - Getter: `isAllowed(phase: string, action: string): boolean` — checks if `action` is in `matrix[phase]`. Since Pinia getters can't take arguments, expose as a regular method or as a getter returning a function: `isAllowed: (state) => (phase: string, action: string) => state.matrix[phase]?.includes(action) ?? false`.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue` — call `policyStore.fetch()` in `onMounted` (before `enterRoom`). Import `usePolicyStore`.

**Pattern:** Follow existing store style at `stores/sprint.ts`.

**Verify:** `npm run build` in `web/reflekt-web/`.

### Step 11: Frontend phase gating — RetroRoomView + SprintView + CardItem

Apply the policy store to gate every interactive control in the retro room, and update SprintView for the phase rename.

**Files to modify:**

**`web/reflekt-web/src/views/RetroRoomView.vue`:**
- Import `usePolicyStore` and get `policyStore`.
- Create a computed `currentPhase` from `sprintStore.currentSummary?.currentPhase`.
- **Note add inputs (Went Well / Didn't Go Well):** wrap the `<div class="p-3 border-t flex gap-2">` sections (lines 84-100 and 129-145) in `v-if="policyStore.isAllowed(currentPhase, 'CreateNote')"` — hides the input entirely when CreateNote is forbidden.
- **Action Items column (lines 148-194):** wrap the entire Action Items column `<div>` in `v-if="policyStore.isAllowed(currentPhase, 'CreateActionItem')"`. When hidden, change the grid from `grid-cols-3` to `grid-cols-2`: bind `:class` on the grid container (line 57) to `policyStore.isAllowed(currentPhase, 'CreateActionItem') ? 'grid grid-cols-3 gap-4' : 'grid grid-cols-2 gap-4'`.
- **Vote buttons on CardItem:** pass a new prop `:voting-allowed="policyStore.isAllowed(currentPhase, 'CastVote')"` to each `CardItem` in Went Well and Didn't Go Well columns. The `show-voting` prop already exists; replace its static `true` with the dynamic check.
- **Edit/Delete buttons on CardItem:** pass a new prop `:edit-allowed` that combines ownership check with phase check. For notes: `policyStore.isAllowed(currentPhase, 'EditNote')`. For action items: `policyStore.isAllowed(currentPhase, 'EditActionItem')`. Since `CardItem` doesn't know its card type for the phase check, pass the phase-permission as a separate prop and let `CardItem` combine it with the existing `canEdit`. Simpler: change `canEditCard(card)` to also check `policyStore.isAllowed(currentPhase, card.cardType === 'ActionItem' ? 'EditActionItem' : 'EditNote')`.
- **Evidence link attach/detach buttons on CardItem:** pass a new prop or compute within `canEditCard`. Since evidence link gating follows the same phase pattern as card edits (Writing + Discussing), and `canEditCard` is already used for gating edit/delete/evidence buttons, fold the evidence link phase check into the same computed: `policyStore.isAllowed(currentPhase, 'AttachEvidenceLink')`. But note that `canEditCard` is per-card (ownership-based), while phase gating is global. These are separate concerns — a card may be "editable by this user" but "not in a phase that allows edits". **Approach:** Create a `canMutateCard(card)` function that checks both ownership AND phase permission. Replace `canEditCard` with `canMutateCard` on each `CardItem`.

**`web/reflekt-web/src/views/SprintView.vue`:**
- Line 58: change `sprint.currentPhase !== 'Closed'` to `sprint.currentPhase !== 'Reporting'`
- Line 107: change `phaseOrder` from `['Writing', 'Voting', 'Discussing', 'Closed']` to `['Writing', 'Voting', 'Discussing', 'Reporting']`
- Line 119: the `'Closed'` case in `stateBadgeClass` refers to sprint **state**, not phase — leave unchanged.

**`web/reflekt-web/src/views/WelcomeView.vue`:**
- No changes needed — it displays `sprint.currentPhase` as a string, which will automatically show "Reporting" once the backend returns it.

**Verify:** `npm run build` in `web/reflekt-web/`. Manual testing in browser: start dev server, join a retro room, verify controls appear/disappear per phase.

### Step 12: Doc updates

Update feature specs and backlog to reflect the new behavior.

**Files to modify:**
- `docs/features/Sprints.md`:
  - Line 24: change `CurrentPhase` type to `Writing, Voting, Discussing, Reporting`
  - Line 34: change phase machine to `Writing → Voting → Discussing → Reporting`
  - Line 36: update prose about phase Closed → Reporting
  - Line 65 (Flow 3 step 3): update `Closed` → `Reporting`
  - Line 108: update "already at phase Closed" → "already at phase Reporting"
  - Rule #4 (line 125): replace "Phases are soft" with a cross-reference: "Phases are enforced. See `docs/features/PhaseActionGating.md` for the action matrix."
  - Line 144 (AC): update phase references from `Closed` to `Reporting`
- `docs/features/Cards.md`:
  - Rule #10 (line 139): remove "Phases are soft" rule. Replace with: "Phase-gating restricts which card mutations are permitted in each phase. See `docs/features/PhaseActionGating.md`."
- `docs/backlog.md`:
  - §17 (line 126-131): mark as "Absorbed into PhaseActionGating feature."
- `docs/bugs.md`:
  - Add BUG-004 entry referencing PhaseActionGating: "BUG-004 — Participants can perform actions outside their permitted phase. Status: Absorbed into PhaseActionGating feature."

**Verify:** No build check needed.

## Cross-Service Changes

None. Single service, no gRPC or integration events.

## Migration Notes

**Command:**
```bash
dotnet ef migrations add AddPhaseActions -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

**Phase rename is migration-safe:** `SprintPhase` is stored as integers. Renaming `Closed` (ordinal 3) to `Reporting` (still ordinal 3) requires no data fix-up. The migration only creates the `PhaseActions` table and seeds it.

**Existing sprints:** Any sprint with `CurrentPhase = 3` (previously rendered as "Closed") will now render as "Reporting" in API responses automatically.

## Testing Strategy

**Backend:**
- Each gated endpoint returns 409 in every forbidden phase (systematically test every cell in the matrix that is NOT checked).
- Each gated endpoint succeeds in every permitted phase.
- `GET /api/phase-actions` returns the correct matrix (compare against spec).
- `PhaseActionCache` loads seeded rows correctly.
- AdvancePhase from Discussing lands on Reporting (not Closed).
- AdvancePhase from Reporting returns 409.
- Existing closed-sprint (State = Closed) checks still work — a Closed sprint rejects all mutations regardless of phase.

**Frontend:**
- grep for `'Closed'` in `web/reflekt-web/src/` — zero hits for phase references (state references like `sprint.state === 'Closed'` are expected and correct).
- Manual: join a retro room, advance through phases, verify controls appear/disappear correctly.

## Open Questions

None — all design decisions resolved during investigation.
