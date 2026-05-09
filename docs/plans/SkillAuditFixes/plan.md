# Skill Audit Fixes

## Context

A full skill audit (`docs/audits/full-skill-audit.md`) found 1 CRITICAL, 8 HIGH, 9 MEDIUM, and 2 LOW deviations across the codebase, plus 5 stale skill definitions. This plan addresses all findings in three phases: skill definition fixes, backend code fixes, and frontend decomposition.

## Scope

**In scope:**
- Fix 5 stale skill definitions to match actual BuildingBlocks and project conventions
- Standardize aggregate property accessors (`required init` vs `private set`)
- Convert manual static mappings to Mapster `IRegister` pattern
- Resolve endpoint exception-catching pattern
- Decompose all oversized Vue components per vue-component-architecture skill
- Implement `PageLayout`/`PageToolbar` shell components
- Seal HealthEndpoint

**Out of scope:**
- Implementing `AuditedEntityConfiguration<T>` in BuildingBlocks (update skill to match code instead)
- N/A skills (MassTransit, gRPC, MediatR pipeline behaviors)
- Business logic changes

## Domain Model Changes

None. All changes are structural/organizational.

## Data Model Changes

None.

## Skill Mapping

| Step | Skill | Disposition |
|------|-------|-------------|
| 1 | improve-skills | Update skill definitions |
| 2 | improve-skills | Update skill definitions |
| 3 | create-aggregate | Standardize property patterns |
| 4 | create-feature (Mappings workflow) | Convert to Mapster IRegister |
| 5 | create-feature (EndpointFastEndpoints) | Resolve exception handling |
| 6 | create-feature | Seal HealthEndpoint |
| 7 | vue-component-architecture | Create shell components |
| 8 | vue-component-architecture | Decompose RetroRoomView |
| 9 | vue-component-architecture | Decompose CardGroupItem + CardItem |
| 10 | vue-component-architecture | Decompose remaining views |

## Implementation Steps

### Phase 1: Skill Definition Fixes

#### Step 1 — Update backend skill definitions

Update the following skills to match actual BuildingBlocks and project conventions:

**create-aggregate (`AggregateEfCore.md`):**
- Change `AuditedEntityConfiguration<{AggregateName}>` → `EntityConfiguration<{AggregateName}>`
- Change file naming `{Name}EntityConfiguration.cs` → `{Name}Configuration.cs`
- Remove references to `DefaultDateSql`, `HasConcurrencyToken` override points (don't exist)
- Add note: composite-key lookup entities (e.g., PhaseAction) may implement `IEntityTypeConfiguration<T>` directly

**persistence-patterns (`SKILL.md`):**
- Update `AuditedEntityConfiguration<T>` section to describe `EntityConfiguration<T>` as it actually exists
- Remove `DefaultDateSql` and `HasConcurrencyToken` virtual overrides
- Add note about SQLite `HasDefaultValueSql` differences
- Document that child entities (not aggregate roots) don't get their own repositories

**Files to modify:**
- `.claude/skills/create-aggregate/workflows/AggregateEfCore.md`
- `.claude/skills/persistence-patterns/SKILL.md`

**Depends on:** Nothing

---

#### Step 2 — Update frontend and feature skill definitions

**create-feature (Mappings workflow):**
- Document that FastEndpoints variant uses Mapster `IRegister` at `{Svc}.API/Mappings/{Domain}Mappings.cs`
- Keep `.Adapt<T>()` inline usage documentation
- Remove `{Service}.Application/Mappings/` path for FastEndpoints (only applies to MediatR services)

**create-vue-feature (`SKILL.md`):**
- Replace `PageLayout` + `PageToolbar` references with documentation that these must exist before creating views
- Add cross-reference: "Ensure PageLayout/PageToolbar exist per vue-component-architecture skill"
- Update file structure to show `components/layout/` for shell components

**Files to modify:**
- `.claude/skills/create-feature/workflows/Mappings.md`
- `.claude/skills/create-vue-feature/SKILL.md`

**Depends on:** Nothing

---

### Phase 2: Backend Code Fixes

#### Step 3 — Standardize aggregate property accessors

For each aggregate, apply the rule: immutable-after-creation → `required init`, mutable-by-behavior → `private set`.

**Card.cs:** Change `SprintId`, `ParticipantId`, `ColumnType`, `CardType`, `CreatedAt` from `private set` to `required init` (these are set at creation and never mutated by any behavior method). Keep `Text`, `AssigneeName`, `GroupId`, `UpdatedAt` as `private set` (mutated by Edit/Group behaviors).

**Participant.cs:** Change `SprintId`, `DisplayName`, `GuestToken`, `JoinedAt` from `private set` to `required init` (all set at creation, never mutated).

**Vote.cs:** Change `GroupId`, `ParticipantId`, `CreatedAt` from `private set` to `required init` (immutable after creation).

**CardGroup.cs:** Change `SprintId`, `ColumnType`, `CreatedAt` from `private set` to `required init`. Keep `Title`, `DiscussedAt` as `private set` (mutated by UpdateTitle/ToggleDiscussed).

**EvidenceLink.cs:** Change `CardId`, `Url`, `Source` from `private set` to `required init`. Keep `Title`, `Description`, `IconUrl`, `ImageUrl`, `ExternalKey`, `FetchedAt`, `FetchStatus` as `private set` (mutated by UpdateMetadata).

After changing to `required init`, update each aggregate's `Create` factory method to use object initializer syntax for the `required` properties (matching the Team/Sprint pattern).

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLink.cs`
- `src/Services/Reflekt/Reflekt.Domain/Participants/Participant.cs`
- `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs`
- `src/Services/Reflekt/Reflekt.Domain/Votes/Vote.cs`
- `src/Services/Reflekt/Reflekt.Domain/Votes/Behaviors/Vote.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/CardGroup.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Behaviors/CardGroup.cs`

**Verify:** `dotnet build` passes. Run unit tests.

**Depends on:** Nothing

---

#### Step 4 — Convert mappings to Mapster IRegister pattern

Replace manual static `{Domain}Mappings` classes with Mapster `IRegister` configurations.

For each domain area (Cards, Sprints, Participants, CardGroups, Reports):

1. Create `{Svc}.API/Mappings/{Domain}Mappings.cs` implementing `IRegister`
2. Configure mappings in `Register(TypeAdapterConfig config)` — handle enum-to-string conversions, nested collections, computed properties (e.g., `VoteCount`)
3. Replace `{Mappings}.ToResponse(entity)` calls in endpoints with `entity.Adapt<{Response}>()` 
4. Replace `{Mappings}.ToResponse(entity)` calls in domain event handlers with `entity.Adapt<{Response}>()`
5. Remove the static mapping class from `{Domain}Contracts.cs` — keep only the response record types
6. Rename `{Domain}Contracts.cs` to keep only contracts (request/response records)

**Special cases — multi-parameter mappings that Mapster can't auto-map:**
- `CardMappings.ToResponse(Card c, int voteCount)` — use `card.Adapt<CardResponse>() with { VoteCount = voteCount }`
- `CardGroupMappings.ToResponse(CardGroup group, List<Card> cards, int voteCount, bool isFocused)` — use `group.Adapt<CardGroupResponse>() with { Cards = cards.Select(c => new CardGroupCardEntry(c.Id, c.Text, c.CreatedAt, c.EvidenceLinks.Select(e => e.Adapt<EvidenceLinkResponse>()).ToList())).ToList(), VoteCount = voteCount, IsFocused = isFocused, IsDiscussed = group.DiscussedAt.HasValue }`
- `EvidenceLinkResponse` mapping from `CardMappings` should be its own mapping rule
- Check `SprintMappings`, `ParticipantMappings`, and `ReportMappings` for any additional multi-parameter signatures before starting

**Why Mapster over manual mappings:** The audit noted that manual mappings are arguably simpler at this project's scale. The decision to convert to Mapster was made explicitly by the user — it aligns with the skill's prescribed pattern and prepares for growth. The manual pattern was a pragmatic shortcut, not an intentional convention.

Ensure Mapster assembly scanning is registered (already in DI: `TypeAdapterConfig.GlobalSettings` + `ServiceMapper`). Add `TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly)` in `AddReflektApi` if not already present.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Mappings/CardMappings.cs`
- `src/Services/Reflekt/Reflekt.API/Mappings/SprintMappings.cs`
- `src/Services/Reflekt/Reflekt.API/Mappings/ParticipantMappings.cs`
- `src/Services/Reflekt/Reflekt.API/Mappings/CardGroupMappings.cs`
- `src/Services/Reflekt/Reflekt.API/Mappings/ReportMappings.cs`

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs` (remove static mapping class)
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/ParticipantContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CardGroupContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs`
- All endpoints and handlers that reference `{Domain}Mappings.ToResponse()` (~25 files)
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` (add assembly scan if needed)

**Verify:** `dotnet build` passes. Hit a few endpoints to verify response shapes are identical.

**Depends on:** Step 3 (property changes may affect mapping config)

---

#### Step 5 — DEFERRED: Exception handling infrastructure

**Deferred to separate plan:** `docs/plans/ExceptionInfrastructure/plan.md`

The error-handling skill's full exception stack (Blocks.Exceptions, GlobalExceptionMiddleware, Guard utilities) doesn't exist in this project. Building it requires new BuildingBlocks packages and is a standalone infrastructure task.

**What stays in this plan:** Nothing. The 3 endpoint try/catches remain as-is until the exception infrastructure plan is implemented. Once that plan completes, removing the try/catches becomes a trivial follow-up.

**Depends on:** ExceptionInfrastructure plan completion

---

#### Step 6 — Seal HealthEndpoint

Add `sealed` keyword to `HealthEndpoint`.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Health/HealthEndpoint.cs`

**Depends on:** Nothing

---

### Phase 3: Frontend Decomposition

#### Step 7 — Create PageLayout and PageToolbar shell components

Create the view shell components prescribed by create-vue-feature:

**PageLayout.vue (L1, < 200 lines):**
- Slot-based layout: `header` (for PageToolbar), `default` (content area)
- Three-state orchestration support: props for `loading` and `empty` states
- Consistent page padding, max-width, responsive behavior

**PageToolbar.vue (L2, < 150 lines):**
- Slot-based toolbar: `title`, `actions`, `filters`
- Consistent toolbar styling across views

**Files to create:**
- `web/reflekt-web/src/components/layout/PageLayout.vue`
- `web/reflekt-web/src/components/layout/PageToolbar.vue`

**Depends on:** Nothing

---

#### Step 8 — Decompose RetroRoomView.vue (951 → < 100 lines)

This is the largest single change. RetroRoomView must become a thin L0 orchestrator that delegates to phase-specific L1 containers.

**Target structure:**
```
views/RetroRoomView.vue              (L0, < 100 lines — PageLayout + phase router)
components/retro/
├── RetroWritingBoard.vue            (L1, < 200 — writing phase container)
├── RetroGroupingBoard.vue           (L1, < 200 — grouping phase container)  
├── RetroVotingBoard.vue             (L1, < 200 — voting phase container)
├── RetroDiscussionBoard.vue         (L1, < 200 — discussion phase container)
├── RetroColumnPanel.vue             (L2, < 150 — column of cards within a phase)
├── RetroCardItem.vue                (L3, < 100 — single card display, props-only)
├── RetroGroupItem.vue               (L3, < 100 — single group display, props-only)
├── RetroVoteControls.vue            (L3, < 100 — vote buttons, emits events)
├── RetroEvidenceLinks.vue           (L3, < 100 — evidence link list + attach)
├── RetroPhaseHeader.vue             (L3, < 100 — phase name + advance button)
└── RetroParticipantList.vue         (L3, < 100 — participant sidebar)
```

**Approach:**
1. Read RetroRoomView.vue fully to understand the current structure
2. Identify phase-specific template blocks → extract to L1 containers
3. Identify repeated card/group rendering → extract to L3 humble components
4. Identify interactive logic (vote, group, focus) → extract to L2 controllers or keep in L1
5. Wire composables (`useSprintHub`) at the L0 level. L1 boards access stores directly (skill allows full CRUD at L1). L3 children receive props only.
6. RetroRoomView becomes: PageLayout + phase switch (`v-if="sprint.currentPhase"`) rendering the appropriate L1 board

**Files to create:** ~11 new components under `web/reflekt-web/src/components/retro/`
**Files to modify:** `web/reflekt-web/src/views/RetroRoomView.vue` (rewrite to L0 orchestrator)

**Verify:** `npm run build` passes. Manual testing of all 4 phases in browser.

**Depends on:** Step 7 (needs PageLayout)

---

#### Step 9 — Decompose CardGroupItem.vue (356 lines) and CardItem.vue (261 lines)

These components are currently L3 by name but L1/L2 by behavior. After Step 8, their logic should be split between the new retro components.

If Step 8 created `RetroCardItem.vue` and `RetroGroupItem.vue` as L3 replacements, this step verifies that the old `CardGroupItem.vue` and `CardItem.vue` are no longer imported anywhere and can be removed — or refactored into thin L3 wrappers.

**If still needed after Step 8:**
- Extract interactive handlers (vote, edit, delete, group) to parent L1/L2
- Keep only display props + emit events
- Target: < 100 lines each

**Files to modify or remove:**
- `web/reflekt-web/src/components/CardGroupItem.vue`
- `web/reflekt-web/src/components/CardItem.vue`

**Depends on:** Step 8

---

#### Step 10 — Decompose remaining views

**ReportView.vue (228 → < 100 lines):**
- Extract report data display into `components/reports/SprintReportPanel.vue` (L1)
- Extract participant breakdown into `components/reports/ParticipantBreakdown.vue` (L3)
- View becomes: PageLayout + store fetch + delegate to L1

**SprintView.vue (200 → < 100 lines):**
- Extract sprint list/management into `components/sprints/SprintList.vue` (L1)
- Extract sprint creation form into `components/sprints/CreateSprintForm.vue` (L3)
- View becomes: PageLayout + store orchestration

**WelcomeView.vue (166 → < 100 lines):**
- Extract team selection into `components/teams/TeamSelector.vue` (L1)
- Extract team creation into `components/teams/CreateTeamForm.vue` (L3)
- View becomes: PageLayout + team store

**Files to create:** ~6 new components
**Files to modify:** 3 view files

**Verify:** `npm run build` passes. Manual testing of each view.

**Depends on:** Step 7 (needs PageLayout)

---

## Cross-Service Changes

None. Single-service project.

## Migration Notes

None. No data model changes.

## Testing Strategy

- **Backend (Steps 3-6):** `dotnet build` + `dotnet test` after each step. Verify API responses are shape-identical before and after mapping refactor (Step 4).
- **Frontend (Steps 7-10):** `npm run build` after each step. Manual browser testing of all views and all sprint phases. Verify SignalR real-time updates still work after component decomposition.
- **Skills (Steps 1-2):** No automated test — manual review that skill SKILL.md files are internally consistent.

## Resolved Questions

1. **Step 4 (Mapster):** Extra `voteCount` parameter → use `card.Adapt<CardResponse>() with { VoteCount = voteCount }` (record `with` expression).

2. **Step 8 (RetroRoomView):** L1 phase boards access stores directly (skill allows full CRUD at L1). No provide/inject or prop drilling.

3. **Step 5 (Exception handling):** Deferred to `docs/plans/ExceptionInfrastructure/plan.md`. Once that plan completes, the 3 endpoint try/catches auto-resolve. The middleware maps `ArgumentException` → 400 globally.
