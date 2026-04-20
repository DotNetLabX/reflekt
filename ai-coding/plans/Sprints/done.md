# Sprints — Implementation Log

## Step 1: Sprint enums + aggregate + domain events
- Created `SprintState.cs`, `SprintPhase.cs` enums.
- Created `Sprint.cs` (properties) + `Behaviors/Sprint.cs` (factory + state methods). `State`/`CurrentPhase`/`ClosedAt` use `private set`; immutable fields use `required init`.
- Created 4 event files (one per event, matching Team pattern): `SprintCreatedEvent`, `SprintStartedEvent`, `PhaseAdvancedEvent`, `SprintClosedEvent`.

## Step 2: EF config + DbSet + repository + DI
- Created `SprintConfiguration.cs` with FK to Teams (shadow navigation, no nav property), index on `TeamId`.
- Created `SprintRepository.cs` with `GetByTeamIdAsync` ordered by `CreatedAt` descending.
- Added `DbSet<Sprint> Sprints` to `ReflektDbContext`.
- Registered `SprintRepository` as scoped in `DependencyInjection.cs`.

## Step 3: EF migration
- Generated `AddSprint` migration. Both `AddTeam` and `AddSprint` verified present.

## Step 4: Shared contracts + CreateSprint endpoint
- Created `SprintContracts.cs` with `SprintResponse`, `SprintRouteRequest`, and static `SprintMappings.ToResponse`. Enums mapped to string explicitly via `.ToString()` — no global JSON converter needed since response uses `string` types.
- Created `CreateSprintCommand.cs` with validator.
- Created `CreateSprintEndpoint.cs` with team-ownership check (load team → 404, email compare → 403).

## Step 5: StartSprint + CloseSprint endpoints
- Created `StartSprintEndpoint.cs` and `CloseSprintEndpoint.cs`. Both follow the ownership-check + sprint-load + state-precheck pattern. Return full `SprintResponse`.

## Step 6: AdvancePhase endpoint + PhaseAdvancedEventHandler
- Created `AdvancePhaseEndpoint.cs` with dual pre-checks (Active state + not-Closed phase).
- Created `PhaseAdvancedEventHandler.cs` — pushes `"PhaseChanged"` to SignalR group `sprint:{id}`.
- **Deviation:** `SprintHub` already existed (with `JoinSprint`/`LeaveSprint`/logger) — no file created. SignalR already registered in `DependencyInjection.cs` — no changes made there.

## Step 7: GetSprint + GetTeamSprints endpoints
- Created `GetSprintEndpoint.cs` and `GetTeamSprintsEndpoint.cs`. Both use ownership check. `GetTeamSprints` uses inline request record `GetTeamSprintsRequest(int TeamId)`.

## Step 8: Seed data update
- Updated `DbInitializer.cs` to seed one sprint under the dev team (idempotent, clears domain events before save).

## Review fix: Frontend POST actions missing Content-Type
- Added `headers: { 'Content-Type': 'application/json' }, body: '{}'` to `startSprint`, `advancePhase`, `closeSprint` in `sprint.ts`. FastEndpoints returns 415 for POST without Content-Type even when the request model is route-only.

## Step 9: Frontend sprint store
- Populated `sprint.ts` with full state shape, 6 actions, and `_replaceSprint` helper to update both `sprints[]` and `currentSprint` on mutation.

## Step 10: Frontend UI updates
- Created `SprintView.vue` — sprint detail with state/phase/dates, SM controls (Start/Advance/Close), state badge coloring.
- Updated `WelcomeView.vue` — replaced placeholder with sprint list (clickable rows, state badges, phase label) + create-sprint form. Added watch on `currentTeam` to auto-fetch sprints.
- Updated `router/index.ts` — added `/teams/:teamId/sprints/:sprintId` route.

## Step 11: Unit tests
- Created `SprintTests.cs` with 18 test cases covering all guard clauses, state transitions, phase cycle, event raising, and default values. 32/32 tests pass (18 new + 4 existing).

## Step 12: Integration tests
- Created `SprintEndpointTests.cs` with 16 test cases. **Fix applied:** action endpoints (start/close/advance-phase) require `PostAsJsonAsync(..., new { })` instead of `PostAsync(..., null)` — FastEndpoints returns 415 for POST with no Content-Type even when the request model binds only from route params. 27/27 tests pass.
