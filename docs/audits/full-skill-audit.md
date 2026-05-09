# Skill Audit — Full Codebase

**Date:** 2026-05-09
**Scope:** All 13 auditable skills vs Reflekt codebase

## Summary

| Skill | Files Scanned | Conformant | Deviations |
|-------|--------------|------------|------------|
| create-aggregate | 18 | 14 | 4 (1 HIGH, 2 MEDIUM, 1 LOW) |
| create-feature | 38 | 34 | 4 (1 HIGH, 2 MEDIUM, 1 LOW) |
| create-domain-event-handler | 18 | 18 | 0 |
| create-service | 7 (projects) | 7 | 0 |
| create-module | 2 (BB projects) | 2 | 0 |
| persistence-patterns | 14 | 12 | 2 (1 MEDIUM, 1 LOW) |
| create-vue-feature | 10 | 4 | 6 (3 HIGH, 3 MEDIUM) |
| vue-component-architecture | 14 | 8 | 6 (1 CRITICAL, 3 HIGH, 2 MEDIUM) |
| add-integration-event | N/A | — | N/A (no MassTransit in project) |
| create-grpc-contract | N/A | — | N/A (no gRPC in project) |
| add-pipeline-behavior | N/A | — | N/A (no MediatR in project) |
| extract-feature-service | N/A | — | N/A (no operation services yet) |

**Totals:** 1 CRITICAL, 8 HIGH, 9 MEDIUM, 2 LOW + 2 skill definition issues

---

## Findings

### [CRITICAL] RetroRoomView.vue is 951 lines — 9.5x over L0 limit

**Skill:** vue-component-architecture
**Files:** `web/reflekt-web/src/views/RetroRoomView.vue` (951 lines)
**Rule:** L0 (Page/View) components must be < 100 lines. Views delegate to L1 feature components; they orchestrate, not implement.
**Actual:** RetroRoomView contains the entire retro board implementation — card rendering, grouping, voting, phase logic, drag/drop, modals — all inline in a single file.
**Impact:** Unmaintainable. Any change to the retro room risks regressions across unrelated features. Cannot be tested in isolation. Violates every splitting signal (3+ v-if branches > 30 lines, v-for bodies > 20 lines, multiple collapsible sections > 50 lines).
**Fix:** Decompose into L1 feature containers (WritingBoard, VotingBoard, DiscussionBoard, GroupingPanel) with L3 humble components for cards, groups, and vote controls.

---

### [HIGH] CardGroupItem.vue and CardItem.vue exceed component level limits

**Skill:** vue-component-architecture
**Files:**
- `web/reflekt-web/src/components/CardGroupItem.vue` (356 lines, limit: < 100 for L3)
- `web/reflekt-web/src/components/CardItem.vue` (261 lines, limit: < 100 for L3)
**Rule:** L3 (Humble) components must be < 100 lines, props-only, no store access, no composables.
**Actual:** Both components are 2.5-3.5x over limit. Likely contain logic that belongs in L1/L2 controllers or composables.
**Fix:** Extract interactive logic (vote actions, group operations, evidence link management) into L1/L2 controller components. Keep CardItem and CardGroupItem as pure display (L3).

---

### [HIGH] All page views exceed L0 size limit

**Skill:** vue-component-architecture
**Files:**
- `web/reflekt-web/src/views/ReportView.vue` (228 lines, limit: < 100)
- `web/reflekt-web/src/views/SprintView.vue` (200 lines, limit: < 100)
- `web/reflekt-web/src/views/WelcomeView.vue` (166 lines, limit: < 100)
**Rule:** L0 views must be < 100 lines. They compose L1 children and read store state — they don't implement features.
**Actual:** All views exceed limit by 1.6-2.3x. Contain inline feature logic that should be extracted to L1 components.
**Fix:** Extract feature sections from each view into dedicated L1 components. Views should become thin orchestrators.

---

### [HIGH] Endpoints catch domain exceptions — violates error handling rule

**Skill:** create-feature (EndpointFastEndpoints workflow)
**Files:**
- `Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs:67-75`
- `Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs:69`
- `Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs:48`
**Rule:** "Never catch exceptions to return HTTP responses; GlobalExceptionMiddleware handles mapping."
**Actual:** Three endpoints wrap `{Aggregate}.Create()`/`.Update()` calls in `try/catch (ArgumentException ex)` to convert to `ThrowError(ex.Message)`. The project has `UseExceptionHandler()` configured but these endpoints bypass it.
**Fix:** Either: (a) add a FastEndpoints exception handler that maps `ArgumentException` to validation errors globally, or (b) move the validation into the endpoint before calling the domain method (pre-validate), or (c) document this as an accepted local pattern and update the skill.

---

### [HIGH] Skill `create-aggregate` references `AuditedEntityConfiguration<T>` which doesn't exist

**Skill:** create-aggregate (AggregateEfCore workflow) — SKILL DEFINITION ISSUE
**Rule:** Skill says entity configs should extend `AuditedEntityConfiguration<{AggregateName}>` with override points for `HasGeneratedId`, `DefaultDateSql`, `HasConcurrencyToken`.
**Actual:** BuildingBlocks only has `EntityConfiguration<T>` (at `BuildingBlocks.Persistence/EntityConfigurations/EntityConfiguration.cs`). No `AuditedEntityConfiguration` exists. The skill was written for a richer BuildingBlocks than what's implemented.
**Impact:** Any future developer or agent following the create-aggregate skill will hit a compile error.
**Fix:** Update the skill to reference `EntityConfiguration<T>` (the actual base class). Either: (a) update skill to match current code, or (b) implement `AuditedEntityConfiguration<T>` in BuildingBlocks and migrate configs.

---

### [MEDIUM] Inconsistent aggregate property patterns — `required init` vs `private set` for immutable props

**Skill:** create-aggregate
**Files:**
- `Reflekt.Domain/Teams/Team.cs` — uses `required init` for immutable props (Name, CreatorEmail, CreatedAt) ✓
- `Reflekt.Domain/Sprints/Sprint.cs` — uses `required init` for immutable props (TeamId, Name) + `private set` for mutable (State, CurrentPhase) ✓
- `Reflekt.Domain/Cards/Card.cs` — uses `private set` for ALL props, including immutable ones (SprintId, ParticipantId)
- `Reflekt.Domain/Participants/Participant.cs` — uses `private set` for ALL props, including immutable (SprintId)
- `Reflekt.Domain/Votes/Vote.cs` — uses `private set` for ALL props, including immutable (GroupId, ParticipantId)
- `Reflekt.Domain/CardGroups/CardGroup.cs` — uses `private set` for ALL props, including immutable (SprintId)
**Rule:** Skill prescribes `required` + `init` for state properties. Team and Sprint follow this correctly for immutable props. Card, Participant, Vote, CardGroup use `private set` for everything — including foreign keys that never change after creation.
**Fix:** Standardize: immutable-after-creation props → `required init`, mutable-by-behavior props → `private set`.

---

### [MEDIUM] Mapping pattern mismatch — manual static classes vs Mapster

**Skill:** create-feature (Mappings workflow)
**Files:**
- `Features/Cards/CardContracts.cs` → `CardMappings` (static class, manual mapping)
- `Features/Sprints/SprintContracts.cs` → `SprintMappings`
- `Features/Participants/ParticipantContracts.cs` → `ParticipantMappings`
- `Features/CardGroups/CardGroupContracts.cs` → `CardGroupMappings`
- `Features/Reports/ReportContracts.cs` → `ReportMappings`
**Rule:** Skill prescribes Mapster-based mappings: `IRegister` classes for configuration, `.Adapt<T>()` for inline, `.AdaptWith<T>()` for post-mutation. Mappings in `{Service}.Application/Mappings/{Domain}Mappings.cs`.
**Actual:** All mappings are manual `static class` methods (`ToResponse(entity)`) co-located with contracts in `{Domain}Contracts.cs`. Mapster IS registered in DI (`TypeAdapterConfig.GlobalSettings` + `ServiceMapper`) but never used for endpoint mappings.
**Fix:** Either: (a) convert to Mapster `IRegister` pattern, or (b) update the skill to document the manual static mapping pattern as a valid FastEndpoints variant (simpler, more explicit, no reflection). Given the project's scale, the manual pattern is arguably better — update the skill.

---

### [MEDIUM] Contracts and mappings co-located in `{Domain}Contracts.cs` instead of separate files

**Skill:** create-feature
**Files:** All 5 `*Contracts.cs` files above
**Rule:** Skill implies separate files: response records in the feature folder, mapping classes in a `Mappings/` folder.
**Actual:** Response records + mapping static class are bundled in a single `{Domain}Contracts.cs` file at the domain-feature level (e.g., `Features/Cards/CardContracts.cs`).
**Fix:** This is a pragmatic pattern for a small codebase. Update skill to document this as the FastEndpoints convention: co-locate contracts (request/response records + mappings) at the domain level.

---

### [MEDIUM] Entity config naming convention differs from skill

**Skill:** create-aggregate, persistence-patterns
**Files:** All 7 entity configuration files in `Reflekt.Persistence/EntityConfigurations/`
**Rule:** Skill says `{AggregateName}EntityConfiguration.cs`
**Actual:** All files use `{AggregateName}Configuration.cs` (e.g., `TeamConfiguration`, not `TeamEntityConfiguration`)
**Fix:** Skill definition issue. Update skill to use `{AggregateName}Configuration.cs` — this is the actual project convention and more concise.

---

### [MEDIUM] No three-state template pattern in Vue views

**Skill:** create-vue-feature
**Files:** All 4 views in `web/reflekt-web/src/views/`
**Rule:** Every view should use `PageLayout` + `PageToolbar` composition with three states: `v-if="store.initializing"`, `v-else-if="!store.data"`, `v-else`.
**Actual:** Views don't use `PageLayout`/`PageToolbar` components (these don't exist in the codebase). Loading states may be handled differently.
**Fix:** Either implement `PageLayout`/`PageToolbar` components and refactor views, or update the skill to document the actual view shell pattern used in Reflekt (since the skill was written for a different frontend structure).

---

### [MEDIUM] PhaseActionConfiguration doesn't extend base EntityConfiguration

**Skill:** persistence-patterns
**Files:** `Reflekt.Persistence/EntityConfigurations/PhaseActionConfiguration.cs`
**Rule:** Entity configs should extend `EntityConfiguration<T>` for consistent key/id configuration.
**Actual:** Directly implements `IEntityTypeConfiguration<PhaseAction>`. Uses composite key (`Phase` + `Action`), so the base class's `HasKey(e => e.Id)` doesn't apply.
**Fix:** Acceptable deviation — PhaseAction is a lookup entity with a composite key, not a standard `Entity<int>`. Document this exception in the persistence-patterns skill.

---

### [LOW] HealthEndpoint is not sealed

**Skill:** create-feature
**Files:** `Reflekt.API/Features/Health/HealthEndpoint.cs`
**Rule:** All other endpoints in the project are `sealed`. Convention is consistent except for this one.
**Actual:** `public class HealthEndpoint` — missing `sealed` keyword.
**Fix:** Add `sealed` keyword.

---

### [LOW] No EvidenceLink repository

**Skill:** persistence-patterns
**Files:** `Reflekt.Persistence/Repositories/` — has 6 repositories but no `EvidenceLinkRepository`
**Rule:** Repositories exist for aggregates that need custom queries.
**Actual:** EvidenceLink is a child entity owned by Card, accessed through Card's navigation property. No separate repository needed.
**Fix:** None needed — this is correct. EvidenceLink is not an aggregate root. Document in persistence-patterns skill that child entities don't get their own repositories.

---

## Conformant Patterns

These areas fully conform to their governing skills:

**create-aggregate:**
- All 6 aggregates use `sealed partial class` with behavior split ✓
- Behavior files in `{Aggregate}/Behaviors/` ✓
- Domain events in `{Aggregate}/Events/` ✓
- Backing collections: `private List<T>` + `IReadOnlyList<T>.AsReadOnly()` ✓
- Factory methods with validation + `AddDomainEvent()` ✓

**create-feature:**
- Feature folder structure `Features/{Domain}/{FeatureName}/` ✓
- Co-located endpoint + command + validator ✓
- FastEndpoints `HandleAsync` pattern (no MediatR dispatch) ✓
- Validators use `Validator<T>` base ✓
- Auto-discovered endpoints ✓
- Query-only endpoints have no command/validator ✓

**create-domain-event-handler:**
- All 18 handlers follow naming convention perfectly ✓
- All `sealed` + `IEventHandler<T>` ✓
- All in correct feature folders ✓
- Method name `HandleAsync` (FastEndpoints) ✓
- Effect-based naming: `Broadcast{EventName}Handler`, `{Effect}On{EventName}Handler` ✓

**create-service / create-module:**
- Project structure matches skill's folder tree ✓
- Dependency direction correct: API → Persistence → Domain ✓
- No Application layer (correct for FastEndpoints without shared state) ✓
- `DependencyInjection.cs` static extension methods ✓

**persistence-patterns:**
- 3-tier repository pattern ✓
- `RepositoryBase<TContext, TEntity>` → `Repository<TEntity>` → domain-specific repos ✓
- Entity configs extend `EntityConfiguration<T>` with proper key setup ✓
- Domain event publishing via `SaveChangesInterceptor` ✓

---

## N/A Skills

| Skill | Reason |
|-------|--------|
| add-integration-event | No MassTransit / cross-service messaging |
| create-grpc-contract | No gRPC |
| add-pipeline-behavior | No MediatR |
| extract-feature-service | No operation services extracted yet |

---

## Skill Definition Issues

These findings are about the SKILLS being wrong, not the code:

1. **create-aggregate** references `AuditedEntityConfiguration<T>` — doesn't exist in BuildingBlocks
2. **create-aggregate** says config naming is `{Name}EntityConfiguration.cs` — codebase uses `{Name}Configuration.cs`
3. **create-feature** Mappings workflow assumes Mapster `IRegister` — codebase uses manual static mappings
4. **create-vue-feature** assumes `PageLayout`/`PageToolbar` — don't exist in this project
5. **persistence-patterns** references `AuditedEntityConfiguration` override points that don't exist

---

## Recommendations

### Requires a fix plan (CRITICAL + HIGH)

1. **Decompose RetroRoomView.vue** — Extract into phase-specific L1 containers + L3 humble components
2. **Decompose CardGroupItem.vue and CardItem.vue** — Extract interactive logic to L1/L2 controllers
3. **Decompose ReportView, SprintView, WelcomeView** — Extract feature sections to L1 components
4. **Resolve endpoint exception catching** — Choose a global handler or pre-validation strategy
5. **Fix create-aggregate skill** — Update to match actual BuildingBlocks base classes

### Optional improvements (MEDIUM + LOW)

6. Standardize aggregate property patterns (`required init` vs `private set`)
7. Update create-feature skill to document manual mapping pattern
8. Update create-feature skill to document `{Domain}Contracts.cs` co-location
9. Update persistence-patterns and entity config naming in skills
10. Add `sealed` to HealthEndpoint
11. Document composite-key entity config exception in persistence-patterns skill
