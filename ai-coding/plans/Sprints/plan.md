# Sprints Plan — Reflekt v1

**Status:** Ready for implementation
**Prerequisite:** Auth (landed), Teams (landed)
**Prerequisite for:** Participants (needs `SprintId`), Cards, Votes, EvidenceLinks, Reports
**Companion docs:** `docs/features/Sprints.md`, `docs/architecture/v1.md` (§5.1–5.2, §6, §7.2)

## 0. Purpose

Introduce the Sprint aggregate — the time-boxed retro session. A Scrum Master creates sprints under their team, starts them, advances retro phases, and closes them. This feature proves the state machine (`Draft → Active → Closed`), the phase machine (`Writing → Voting → Discussing → Closed`), team-ownership authorization, and the first SignalR push (`PhaseChanged`).

**Non-goals for Sprints:**
- No participant joining, guest tokens, or SignalR group membership — that's the **Participants plan**.
- No cards, votes, or evidence links — those are separate features.
- No report generation — that's the **Reports plan**.
- No retro room UI (card columns, voting UI, discussion view) — that's built by Cards/Votes/Participants.
- No auto-close when phase reaches Closed — spec §9 is explicit: state and phase are independent.

If you find yourself writing a `Participant` entity, a `Card` aggregate, or any endpoint under `/api/.../cards/`, **stop** — that belongs in a different plan.

## 1. Relevant skills

Read each skill file before applying it — this plan only names them.

- **`create-aggregate`** — for the `Sprint` aggregate root. Follow its scaffold instructions for the partial-class split.
- **`create-feature`** — for each endpoint. Follow the subfolder-per-endpoint pattern established in Teams.
- **`persistence-patterns`** — for `SprintConfiguration`, `SprintRepository`, `DbSet` registration.
- **`domain-patterns`** — for the domain event definitions and enum placement.

**Pattern references (actual codebase, not plans):**
- Aggregate split: `Reflekt.Domain/Teams/Team.cs` (properties) + `Reflekt.Domain/Teams/Behaviors/Team.cs` (factory + methods)
- Domain event: `Reflekt.Domain/Teams/Events/TeamCreatedEvent.cs`
- Endpoint: `Reflekt.API/Features/Teams/CreateTeam/CreateTeamEndpoint.cs`
- Command + response + validator: `Reflekt.API/Features/Teams/CreateTeam/CreateTeamCommand.cs`
- Repository: `Reflekt.Persistence/Repositories/TeamRepository.cs` (extends `Repository<T>`)
- Entity config: `Reflekt.Persistence/EntityConfigurations/TeamConfiguration.cs` (extends `EntityConfiguration<T>`)
- Integration test: `tests/Reflekt.IntegrationTests/TeamEndpointTests.cs`
- Unit test: `tests/Reflekt.UnitTests/Teams/TeamTests.cs`

## 2. Judgment calls flagged for the Developer

Small decisions the Developer can make without coming back:

- **Migration name.** `AddSprint` is the natural choice.
- **Domain events: one file per event or consolidated.** The Team pattern uses one file per event (`Events/TeamCreatedEvent.cs`). Sprint has four events. Either follow Team's pattern exactly (4 files) or consolidate into `Events/SprintEvents.cs`. Developer picks — consistency with Team or pragmatism.
- **Enum serialization.** Sprint is the first feature with enums in API responses. Choose between: `[JsonConverter(typeof(JsonStringEnumConverter))]` on each enum, or global config in `Program.cs` / `JsonSerializerOptions`. The frontend expects string values (`"Draft"`, not `0`).
- **Action endpoint response shape.** The spec lists partial responses (`{ id, state }` for Start). This plan recommends returning the full `SprintResponse` for all endpoints (simpler frontend, one shape to parse). Developer can use partial responses if they prefer — just be consistent.
- **Pre-check vs catch for state violations.** Endpoints can either check `sprint.State` before calling the domain method and return 409, OR catch `InvalidOperationException` from the domain method. Pre-check is recommended (clearer error messages, no exception-based control flow). The domain guards remain as defense-in-depth.
- **SprintHub body.** The hub is a shell for now — no client-callable methods. Group management (`JoinSprint`) is added by the Participants plan.

Decisions that should come back if they feel wrong mid-implementation:

- If FastEndpoints doesn't auto-bind route params (`teamId`, `sprintId`) to the request record alongside body properties — flag it and we'll adjust the binding strategy.
- If the `DomainEventPublishingInterceptor` doesn't dispatch Sprint events (e.g., if Sprint isn't tracked as an `AggregateRoot` by the interceptor) — flag it.

## 3. Domain model changes

### 3.1 SprintState enum — `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintState.cs`

```
public enum SprintState { Draft, Active, Closed }
```

### 3.2 SprintPhase enum — `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintPhase.cs`

```
public enum SprintPhase { Writing, Voting, Discussing, Closed }
```

### 3.3 Sprint aggregate — partial class split

**Properties file:** `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs`

- Extends `AggregateRoot`.
- Properties (all `private set` or `required init`, following Team pattern):
  - `int TeamId` — FK to Team (by ID, no navigation — cross-aggregate reference).
  - `string Name` — display name.
  - `SprintState State` — current lifecycle state.
  - `SprintPhase CurrentPhase` — current retro phase.
  - `int VotesPerParticipant` — vote budget per participant.
  - `DateTime CreatedAt` — UTC, set at creation.
  - `DateTime? ClosedAt` — UTC, set when state transitions to Closed.
- Private parameterless constructor for EF Core.

**Behaviors file:** `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs`

Four methods:

**`Create(string name, int teamId, int votesPerParticipant = 3)`** — static factory:
- Guard: `name` must not be null/whitespace.
- Guard: `votesPerParticipant` must be ≥ 1.
- Sets `State = Draft`, `CurrentPhase = Writing`, `CreatedAt = DateTime.UtcNow`.
- Raises `SprintCreatedEvent(this)`.

**`Start()`** — instance method:
- Guard: `State` must be `Draft` → `InvalidOperationException` if not.
- Sets `State = Active`.
- Raises `SprintStartedEvent(this)`.

**`AdvancePhase()`** — instance method:
- Guard: `State` must be `Active` → `InvalidOperationException` if not.
- Transitions `CurrentPhase` forward one step using a switch expression:
  - `Writing → Voting`, `Voting → Discussing`, `Discussing → Closed`.
  - If already `Closed` → `InvalidOperationException`.
- Raises `PhaseAdvancedEvent(this)`.

**`Close()`** — instance method:
- Guard: `State` must be `Active` → `InvalidOperationException` if not.
- Sets `State = Closed`, `ClosedAt = DateTime.UtcNow`.
- Raises `SprintClosedEvent(this)`.

Note: `State` and `CurrentPhase` properties need `private set` (not `init`) since the behavior methods mutate them after construction. `Name`, `TeamId`, `VotesPerParticipant`, `CreatedAt` can stay `required init`. Follow the same accessibility pattern Team uses, but adapt for mutable state.

Skill: `create-aggregate`

### 3.4 Domain events

Four events, all in `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/`:

```
SprintCreatedEvent(Sprint Sprint) : IDomainEvent
SprintStartedEvent(Sprint Sprint) : IDomainEvent
PhaseAdvancedEvent(Sprint Sprint) : IDomainEvent
SprintClosedEvent(Sprint Sprint) : IDomainEvent
```

All follow the CLAUDE.md convention: carry the aggregate reference, not individual properties. At dispatch time (post-`SaveChangesAsync`), all Sprint state is populated.

No handlers for `SprintCreated`, `SprintStarted`, or `SprintClosed` in v1 — they're raised, dispatched, and silently dropped. `PhaseAdvancedEvent` gets a SignalR broadcaster (Step 6).

Skill: `domain-patterns`

## 4. Data model changes

### 4.1 Table: `Sprints`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `INTEGER` | PK, auto-increment (from `Entity` base) |
| `TeamId` | `INTEGER` | Required, FK to `Teams.Id` |
| `Name` | `TEXT` | Required, max 100 |
| `State` | `INTEGER` | Required (enum stored as int) |
| `CurrentPhase` | `INTEGER` | Required (enum stored as int) |
| `VotesPerParticipant` | `INTEGER` | Required, default 3 |
| `CreatedAt` | `TEXT` (SQLite datetime) | Required |
| `ClosedAt` | `TEXT` (SQLite datetime) | Nullable |

FK: `TeamId → Teams.Id`. No cascade delete on Team removal in v1 (we don't delete teams). If the Developer wants to add cascade for forward-compat, that's fine — note it as a deviation.

### 4.2 EF Configuration — `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs`

`EntityConfiguration<Sprint>` (extends the base from BuildingBlocks):
- `ToTable("Sprints")`
- `Property(s => s.TeamId).IsRequired()`
- `HasOne<Team>().WithMany().HasForeignKey(s => s.TeamId)` — shadow navigation for FK constraint; no navigation property on the aggregate (cross-aggregate by ID only).
- `Property(s => s.Name).IsRequired().HasMaxLength(100)`
- `Property(s => s.State).IsRequired()`
- `Property(s => s.CurrentPhase).IsRequired()`
- `Property(s => s.VotesPerParticipant).IsRequired().HasDefaultValue(3)`
- `Property(s => s.CreatedAt).IsRequired()`
- `HasIndex(s => s.TeamId)` — non-unique; used by `GetByTeamIdAsync` queries.

Skill: `persistence-patterns`

## 5. Implementation steps

### Step 1: Sprint enums + aggregate + domain events

**What:** Create the Sprint domain model.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintState.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintPhase.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/SprintCreatedEvent.cs` (or consolidated — see §2)
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/SprintStartedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/PhaseAdvancedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/SprintClosedEvent.cs`
**Pattern:** Follow `create-aggregate` skill. Reference `Reflekt.Domain/Teams/` for the exact partial-class split, event shape, and `IDomainEvent` usage.
**Dependencies:** None.

### Step 2: EF config + DbSet + repository + DI

**What:** Add persistence for the Sprint aggregate.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/SprintRepository.cs`
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `public DbSet<Sprint> Sprints => Set<Sprint>();`
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `SprintRepository` as scoped in `AddReflektPersistence`.

**SprintRepository** extends `Repository<Sprint>`:
- Inherits `AddAsync`, `SaveChangesAsync`, `GetByIdAsync` from base.
- Custom method: `GetByTeamIdAsync(int teamId, CancellationToken ct)` — returns `List<Sprint>` ordered by `CreatedAt` descending.

**Pattern:** Follow `persistence-patterns` skill. Reference `TeamRepository.cs` and `TeamConfiguration.cs`.
**Dependencies:** Step 1.

### Step 3: EF Migration

**What:** Generate the Sprint migration.
**Command:**
```bash
dotnet ef migrations add AddSprint -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```
**Verify:** `dotnet ef migrations list` shows both `AddTeam` and `AddSprint`. `dotnet run` auto-applies in Development.
**Dependencies:** Step 2.

### Step 4: Shared contracts + CreateSprint endpoint

**What:** Create shared Sprint response/route records and the CreateSprint endpoint.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintEndpoint.cs`

**SprintContracts.cs** — shared records used across multiple Sprint endpoints:
- `SprintResponse(int Id, int TeamId, string Name, string State, string CurrentPhase, int VotesPerParticipant, DateTime CreatedAt, DateTime? ClosedAt)` — used by all endpoints that return a sprint.
- `SprintRouteRequest` with `int TeamId` and `int SprintId` — used by action + read endpoints with both route params. FastEndpoints auto-binds route params to matching property names.
- A static mapping helper `ToResponse(Sprint sprint)` to avoid repeating the mapping in every endpoint. Alternatively, put the mapping inline — Developer's call.

**CreateSprintCommand.cs:**
- Request record: `CreateSprintCommand { int TeamId, string Name, int? VotesPerParticipant }` — `TeamId` from route, `Name` + `VotesPerParticipant` from body.
- Validator: `Name` required, max 100. `VotesPerParticipant` must be ≥ 1 when provided.

**CreateSprintEndpoint.cs:**
- `[Authorize(Policy = AuthPolicies.ScrumMaster)]`, `[HttpPost("/api/teams/{teamId}/sprints")]`, `[Tags("Sprints")]`
- `Endpoint<CreateSprintCommand, SprintResponse>`
- Injects: `TeamRepository`, `SprintRepository`

**Handler flow:**
1. Extract `email` from `User.FindFirstValue(ClaimTypes.Email)`.
2. Load team: `teamRepository.GetByIdAsync(command.TeamId, ct)` → 404 if null.
3. Verify ownership: `team.CreatorEmail` equals caller email (case-insensitive) → 403 if not.
4. Create aggregate: `Sprint.Create(command.Name, command.TeamId, command.VotesPerParticipant ?? 3)`.
5. Persist: `sprintRepository.AddAsync` + `sprintRepository.SaveChangesAsync`.
6. Respond: 201 with `SprintResponse`.

**Team-ownership check pattern:** Steps 2–3 repeat across all mutation endpoints. Inline the check (it's ~4 lines). If the Developer wants a helper, extract to a static method in `SprintContracts.cs` or a small `TeamOwnershipGuard` — but not a service class (guardrail).

**Pattern:** Follow `create-feature` skill. Reference `CreateTeamEndpoint.cs` for the auth + handler structure.
**Dependencies:** Steps 1–3.

### Step 5: StartSprint + CloseSprint endpoints

**What:** Two state-transition endpoints with no request body.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/StartSprint/StartSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CloseSprint/CloseSprintEndpoint.cs`

Both use `Endpoint<SprintRouteRequest, SprintResponse>` (route params only, no body).

**StartSprintEndpoint — `POST /api/teams/{teamId}/sprints/{sprintId}/start`:**
1. Team-ownership check (same pattern as Step 4, steps 1–3).
2. Load sprint: `sprintRepository.GetByIdAsync(request.SprintId, ct)` → 404 if null.
3. Verify `sprint.TeamId == request.TeamId` → 404 if mismatch (prevents cross-team access).
4. Check `sprint.State == SprintState.Draft` → 409 with message if not.
5. Call `sprint.Start()`.
6. `sprintRepository.SaveChangesAsync(ct)`.
7. Respond: 200 with `SprintResponse`.

**CloseSprintEndpoint — `POST /api/teams/{teamId}/sprints/{sprintId}/close`:**
Same pattern as Start, but:
4. Check `sprint.State == SprintState.Active` → 409 if not.
5. Call `sprint.Close()`.

**Pattern:** Follow `create-feature` skill.
**Dependencies:** Step 4 (shared contracts).

### Step 6: AdvancePhase endpoint + SprintHub + event handler

**What:** The phase-advance endpoint plus the first SignalR push in the system.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` *(if not already created by Foundation)*
- `src/Services/Reflekt/Reflekt.API/Events/PhaseAdvancedEventHandler.cs`

**AdvancePhaseEndpoint — `POST /api/teams/{teamId}/sprints/{sprintId}/advance-phase`:**
Same team-ownership + sprint-load pattern as Step 5, then:
4. Check `sprint.State == SprintState.Active` → 409 if not Active.
5. Check `sprint.CurrentPhase != SprintPhase.Closed` → 409 if already at phase Closed.
6. Call `sprint.AdvancePhase()`.
7. `sprintRepository.SaveChangesAsync(ct)` — interceptor dispatches `PhaseAdvancedEvent`.
8. Respond: 200 with `SprintResponse`.

**SprintHub** — `Reflekt.API/Hubs/SprintHub.cs`:
- `public sealed class SprintHub : Hub { }` — empty shell. Group management (`JoinSprint`) is added by the Participants plan.
- **Registration:** Check if SignalR is already wired in `DependencyInjection.cs` / `Program.cs`. If not:
  - Add `services.AddSignalR()` to `AddReflektApi` in `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs`.
  - Add `app.MapHub<SprintHub>("/hubs/sprint")` to `UseReflektApi`.

**PhaseAdvancedEventHandler** — `Reflekt.API/Events/PhaseAdvancedEventHandler.cs`:
- Implements FastEndpoints `IEventHandler<PhaseAdvancedEvent>`.
- Injects `IHubContext<SprintHub>`.
- `HandleAsync`: sends `"PhaseChanged"` to SignalR group `$"sprint:{ev.Sprint.Id}"` with payload `ev.Sprint.CurrentPhase.ToString()` (string representation of the new phase).
- This is the first event handler in the codebase — it establishes the pattern for all future SignalR broadcasters.

**Dependencies:** Steps 1–4 (shared contracts, Sprint aggregate).

### Step 7: GetSprint + GetTeamSprints endpoints

**What:** Two read endpoints.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprint/GetSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetTeamSprints/GetTeamSprintsEndpoint.cs`

**GetSprintEndpoint — `GET /api/teams/{teamId}/sprints/{sprintId}`:**
- Auth: `ScrumMaster` policy for now. Participant access becomes functional after the Participants feature adds the `Participant` auth scheme. The endpoint itself is ready for both — when the auth layer supports `OwnerOrScrumMaster` with guest tokens, it will work without changes.
- `Endpoint<SprintRouteRequest, SprintResponse>`
- Handler: team-ownership check, load sprint, verify `sprint.TeamId == teamId` → 404 if mismatch, respond 200.

**GetTeamSprintsEndpoint — `GET /api/teams/{teamId}/sprints`:**
- Auth: `ScrumMaster` policy.
- Uses a request record with just `int TeamId` (from route). No body.
- Handler:
  1. Team-ownership check.
  2. `sprintRepository.GetByTeamIdAsync(request.TeamId, ct)`.
  3. Map to `List<SprintResponse>`.
  4. Respond 200.

**Pattern:** Reference `GetMyTeamsEndpoint.cs`.
**Dependencies:** Steps 2, 4.

### Step 8: Seed data update

**What:** Add a seeded sprint to the existing seeded team.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs`

**Seed logic (after team seeding):**
1. Load the seeded team (or use the one just created).
2. If `!db.Sprints.Any(s => s.TeamId == team.Id)`: create `Sprint.Create("Sprint 1", team.Id)`, clear domain events (same pattern as team seeding), add, save.
3. Idempotent.

**Dependencies:** Steps 1–3.

### Step 9: Frontend — sprint store

**What:** Populate the existing empty `sprint.ts` store with real API calls.
**Files to modify:**
- `web/reflekt-web/src/stores/sprint.ts`

**Store shape:**
```ts
interface Sprint {
  id: number
  teamId: number
  name: string
  state: string
  currentPhase: string
  votesPerParticipant: number
  createdAt: string
  closedAt: string | null
}

state: {
  sprints: Sprint[]
  currentSprint: Sprint | null
  loaded: boolean
}

actions: {
  fetchTeamSprints(teamId: number)    // GET /api/teams/{teamId}/sprints
  fetchSprint(teamId: number, sprintId: number)  // GET /api/teams/{teamId}/sprints/{sprintId}
  createSprint(teamId: number, name: string, votesPerParticipant?: number)  // POST
  startSprint(teamId: number, sprintId: number)   // POST .../start
  advancePhase(teamId: number, sprintId: number)   // POST .../advance-phase
  closeSprint(teamId: number, sprintId: number)    // POST .../close
}
```

All fetches use `credentials: 'include'` (cookie auth). Mutation actions update the local store on success (replace the sprint in the `sprints` array with the response). No optimistic updates for SM actions — they're not hot-path.

**Pattern:** Reference `stores/teams.ts`.
**Dependencies:** Steps 4–7 (API endpoints).

### Step 10: Frontend — UI updates

**What:** Replace the WelcomeView placeholder with sprint management. Add sprint detail route.
**Files to modify:**
- `web/reflekt-web/src/views/WelcomeView.vue`
- `web/reflekt-web/src/router/index.ts`
**Files to create:**
- `web/reflekt-web/src/views/SprintView.vue`

**WelcomeView changes** — replace the "Sprint management coming next." placeholder with:
1. **Sprint list** — for each sprint, show: name, state badge (Draft/Active/Closed), current phase. Each sprint links to `/sprint/{sprintId}` (param: `teamId` passed via store or route).
2. **Create sprint form** — text input for name, optional number input for votes-per-participant (placeholder: 3), "Create Sprint" button. Same pattern as the create-team form already there.
3. **Empty state** — "No sprints yet. Create your first sprint."

**SprintView** — `web/reflekt-web/src/views/SprintView.vue`:
- Route: `/teams/:teamId/sprints/:sprintId`
- On mount: `sprintStore.fetchSprint(teamId, sprintId)`.
- Displays: sprint name, state, current phase, votes-per-participant, created-at, closed-at.
- **SM controls** (shown when sprint matches the current user's team):
  - "Start Sprint" button (visible when state == Draft).
  - "Advance Phase" button with label showing next phase (visible when state == Active and phase != Closed).
  - "Close Sprint" button (visible when state == Active).
- This view will be extended by Participants/Cards features into the full retro room. For now it's the SM management panel.

**Router update:**
```ts
{
  path: '/teams/:teamId/sprints/:sprintId',
  name: 'sprint',
  component: SprintView,
}
```

**Styling:** Tailwind. Match WelcomeView aesthetic. Simple cards, buttons, badges.

**Dependencies:** Step 9.

### Step 11: Unit tests

**What:** Test the Sprint aggregate domain logic.
**Files to create:**
- `tests/Reflekt.UnitTests/Sprints/SprintTests.cs`

**Test cases:**
1. `Create_SetsAllPropertiesAndDraftState` — verify Name, TeamId, VotesPerParticipant, State=Draft, CurrentPhase=Writing, CreatedAt set.
2. `Create_DefaultsVotesPerParticipantTo3` — verify default when not specified.
3. `Create_RaisesSprintCreatedEvent` — verify DomainEvents contains one SprintCreatedEvent.
4. `Create_WithEmptyName_Throws` — ArgumentException.
5. `Create_WithZeroVotes_Throws` — ArgumentOutOfRangeException.
6. `Start_FromDraft_TransitionsToActive` — verify State = Active after Start().
7. `Start_FromDraft_RaisesSprintStartedEvent`.
8. `Start_FromActive_Throws` — InvalidOperationException.
9. `Start_FromClosed_Throws` — InvalidOperationException.
10. `AdvancePhase_WritingToVoting` — verify CurrentPhase after one advance.
11. `AdvancePhase_FullCycle` — advance three times: Writing → Voting → Discussing → Closed.
12. `AdvancePhase_FromClosed_Throws` — InvalidOperationException.
13. `AdvancePhase_OnDraftSprint_Throws` — InvalidOperationException.
14. `AdvancePhase_RaisesPhaseAdvancedEvent` — verify DomainEvents.
15. `Close_FromActive_TransitionsToClosedAndSetsClosedAt` — verify State, ClosedAt not null.
16. `Close_FromActive_RaisesSprintClosedEvent`.
17. `Close_FromDraft_Throws` — InvalidOperationException.
18. `Close_FromClosed_Throws` — InvalidOperationException.

Use FluentAssertions. Direct calls to `Sprint.Create(...)` + instance methods. No setup/fixture.

**Pattern:** Reference `tests/Reflekt.UnitTests/Teams/TeamTests.cs`.
**Dependencies:** Step 1.

### Step 12: Integration tests

**What:** Test the Sprint endpoints end-to-end.
**Files to create:**
- `tests/Reflekt.IntegrationTests/SprintEndpointTests.cs`

**Test setup:**
- Same `WebApplicationFactory<Program>` pattern as `TeamEndpointTests.cs`.
- Reuse `TestAuthHandler` from `Helpers/TestAuthHandler.cs`.
- Each test run gets its own SQLite file (same cleanup pattern).
- **Seed a team before sprint tests** — call `POST /api/teams` with `{ "name": "Test Team" }` in setup, capture the `teamId` from the response. All sprint operations use this `teamId`.

**Test cases:**
1. `CreateSprint_ReturnsCreated` — POST with valid name → 201 with SprintResponse, state=Draft, phase=Writing.
2. `CreateSprint_EmptyName_Returns400`.
3. `CreateSprint_ZeroVotes_Returns400`.
4. `CreateSprint_WrongTeam_Returns403or404` — use a teamId that doesn't belong to the caller.
5. `CreateSprint_Unauthenticated_Returns401`.
6. `StartSprint_FromDraft_Returns200` — create + start → state=Active.
7. `StartSprint_FromActive_Returns409`.
8. `AdvancePhase_FullCycle_Returns200` — create, start, advance 3x → phases cycle through Writing→Voting→Discussing→Closed.
9. `AdvancePhase_OnClosedPhase_Returns409`.
10. `AdvancePhase_OnDraftSprint_Returns409`.
11. `CloseSprint_FromActive_Returns200` — verify state=Closed, closedAt not null.
12. `CloseSprint_FromDraft_Returns409`.
13. `GetSprint_Returns200` — create sprint, GET by id → full sprint data.
14. `GetSprint_WrongTeam_Returns404or403`.
15. `GetTeamSprints_ReturnsList` — create 2 sprints, GET list → 2 items.
16. `GetTeamSprints_EmptyTeam_ReturnsEmptyArray`.

**Pattern:** Reference `TeamEndpointTests.cs`.
**Dependencies:** Steps 4–7 and all prior steps.

## 6. Migration notes

Second EF Core migration. Command:
```bash
dotnet ef migrations add AddSprint -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

Auto-applied on startup in Development by existing `UseReflektPersistence` logic. Delete `data/reflekt.db` before first run to get a clean schema (both migrations apply from scratch).

## 7. Testing strategy

- **Unit tests** (Step 11) cover the state machine, phase machine, guard clauses, event raising, and default values. The Sprint aggregate has more behavior than Team — tests are proportionally larger.
- **Integration tests** (Step 12) cover the full HTTP pipeline including team-ownership authorization, state-transition error codes, and multi-step flows (create → start → advance → close).
- **SignalR push is not integration-tested** — the `PhaseAdvancedEventHandler` pushes to a group that's empty (no participants). Verifying it doesn't throw is sufficient. Full SignalR integration testing is deferred to the Participants plan, which introduces group membership.
- **No frontend tests** — not in scope for v1.

## 8. Verification — acceptance criteria

Sprints is done when all of these pass:

1. `dotnet build Reflekt.slnx` — zero errors, zero warnings.
2. `dotnet test` — all tests pass (existing + new unit + new integration).
3. `dotnet run` in Development:
   - `data/reflekt.db` has `Sprints` table alongside `Teams`.
   - Seed data creates one team and one sprint.
4. Manual API flow (Scalar or curl):
   - `POST /api/teams/{teamId}/sprints` with `{ "name": "Sprint 1" }` → 201, state=Draft.
   - `POST .../start` → 200, state=Active.
   - `POST .../advance-phase` → 200, phase=Voting. Repeat → Discussing → Closed.
   - `POST .../close` → 200, state=Closed, closedAt populated.
   - `POST .../advance-phase` on closed sprint → 409.
   - `GET /api/teams/{teamId}/sprints` → list with the sprint.
   - `GET /api/teams/{teamId}/sprints/{sprintId}` → full sprint detail.
5. Frontend flow (npm run dev):
   - Sign in → team card shows sprint list (seeded sprint visible).
   - Create sprint → appears in list with Draft badge.
   - Click sprint → SprintView shows details + Start button.
   - Start, advance, close → UI updates reflect state/phase changes.
6. `Program.cs` unchanged beyond SignalR registration (if not already present).
7. No new authentication schemes. Existing `ScrumMaster` policy reused for all Sprint endpoints.

## 9. Out of scope

If tempted to add any of these, stop and check the relevant feature plan:

- `Participant` entity or guest-token endpoints — **Participants plan**.
- `Card` aggregate or card CRUD — **Cards plan**.
- Vote budget enforcement — **Votes plan**.
- SignalR group join/leave — **Participants plan**.
- Report generation — **Reports plan**.
- Retro room UI (card columns, voting buttons, discussion mode) — **Cards + Participants plans**.
- Sprint delete — **not in v1**.
- Sprint rename / edit after creation — **not in v1**.
- Auto-close when phase reaches Closed — **spec §9: state and phase are independent**.
- Create-and-start in one action — **separate endpoints for v1**.

## 10. File summary

**New files:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintState.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintPhase.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/SprintCreatedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/SprintStartedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/PhaseAdvancedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/SprintClosedEvent.cs`
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/SprintRepository.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/StartSprint/StartSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CloseSprint/CloseSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprint/GetSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetTeamSprints/GetTeamSprintsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` *(if not already exists)*
- `src/Services/Reflekt/Reflekt.API/Events/PhaseAdvancedEventHandler.cs`
- `web/reflekt-web/src/views/SprintView.vue`
- `tests/Reflekt.UnitTests/Sprints/SprintTests.cs`
- `tests/Reflekt.IntegrationTests/SprintEndpointTests.cs`

**Modified files:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `DbSet<Sprint> Sprints`.
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `SprintRepository`.
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — seed one sprint under the dev team.
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — add SignalR registration + hub mapping *(if not already present)*.
- `web/reflekt-web/src/stores/sprint.ts` — populate with API calls and state.
- `web/reflekt-web/src/views/WelcomeView.vue` — replace placeholder with sprint list + create form.
- `web/reflekt-web/src/router/index.ts` — add sprint detail route.

**Unchanged:**
- `Program.cs`, all Auth endpoints, all Team endpoints, `Policies.cs`, `AuthOptions.cs`, `appsettings*.json`, `TestAuthHandler.cs`, `Team.cs`, `TeamRepository.cs`.
