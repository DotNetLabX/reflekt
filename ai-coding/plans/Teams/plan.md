# Teams Plan — Reflekt v1

**Status:** Ready for implementation
**Prerequisite:** Auth (landed)
**Prerequisite for:** Sprints (needs `TeamId`), and transitively all features that live under a team
**Companion docs:** `docs/specs/v1.md` (§3.1, §4), `docs/architecture/v1.md` (§5.1, §6)

## 0. Purpose

Introduce the first domain aggregate. A signed-in user can create one team and view it. Teams is the anchor entity — every sprint belongs to a team, and "Scrum Master" identity is "the person whose email matches `Team.CreatorEmail`."

**Non-goals for Teams:**
- No sprint management — that's the Sprints plan.
- No team invite flow, multi-admin, or role hierarchy — deferred to v2 (spec §11).
- No team update or delete — not in v1.
- No team settings page — nothing to configure beyond the name.

If you find yourself writing a `Sprint` aggregate, a `Participant` entity, or any endpoint under `/api/teams/{teamId}/sprints/`, **stop** — that belongs in the Sprints plan.

## 1. Relevant skills

Read each skill file before applying it — this plan only names them.

- **`create-aggregate`** — for the `Team` aggregate root. Follow its scaffold instructions.
- **`create-feature`** — for `CreateTeamEndpoint` and `GetMyTeamsEndpoint`. Follow the FastEndpoints vertical-slice pattern.
- **`persistence-patterns`** — for `TeamConfiguration`, `TeamRepository`, `DbSet` registration.
- **`domain-patterns`** — for the `TeamCreatedEvent` domain event definition.

Pattern reference: `Reflekt.API/Features/Auth/MeEndpoint.cs` — follow the same endpoint shape (response record in the same file, `using AuthPolicies = Reflekt.API.Auth.Policies` alias, policy gate in `Configure()`).

## 2. Judgment calls flagged for the Developer

Small decisions the Developer can make without coming back:

- **Migration name.** `AddTeam` or `InitialCreate` — either works. This is the first migration in the project.
- **Guard clause style in `Team.Create`.** `ArgumentException` or `InvalidOperationException` for empty name — either is fine. The validator catches it before the aggregate in the normal flow; the guard is defense-in-depth.
- **`SendAsync` vs `SendCreatedAtAsync` for 201 response.** `SendAsync(response, 201, ct)` is simpler — the SPA doesn't follow Location headers. Use whichever feels right.
- **Loading state in WelcomeView.** A simple `v-if="teamsStore.loaded"` with a brief skeleton/spinner, or just let the content flash in. Keep it minimal.

Decisions that should come back if they feel wrong mid-implementation:

- If `DbInitializer.SeedAsync` signature change (adding `IConfiguration`) causes cascading changes beyond `Persistence/DependencyInjection.cs`, flag it.

## 3. Domain model changes

### 3.1 Team aggregate — `src/Services/Reflekt/Reflekt.Domain/Teams/Team.cs`

- Extends `AggregateRoot`.
- Properties (all with `private set` or `init`):
  - `string Name` — team display name.
  - `string CreatorEmail` — the Google-authed user's email. Immutable after creation.
  - `DateTime CreatedAt` — UTC, set at creation.
- Private parameterless constructor for EF Core.
- Factory method: `public static Team Create(string name, string creatorEmail)`:
  - Guard: `name` must not be null/whitespace.
  - Guard: `creatorEmail` must not be null/whitespace.
  - Sets `CreatedAt = DateTime.UtcNow`.
  - Calls `AddDomainEvent(new TeamCreatedEvent(this))` — passes the aggregate reference per CLAUDE.md domain event convention.
  - Returns the new `Team` instance.

Skill: `create-aggregate`

### 3.2 TeamCreatedEvent — `src/Services/Reflekt/Reflekt.Domain/Teams/TeamCreatedEvent.cs`

```
public sealed record TeamCreatedEvent(Team Team) : IEvent;
```

The event holds a reference to the `Team` aggregate. Since the `DomainEventPublishingInterceptor` dispatches after `SaveChangesAsync`, `event.Team.Id` reads the real auto-incremented value at dispatch time. Handlers access all aggregate state via the reference.

No handler registered in v1. The event is raised, dispatched by the interceptor, and silently dropped (no subscribers).

## 4. Data model changes

### 4.1 Table: `Teams`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `INTEGER` | PK, auto-increment (from `Entity` base) |
| `Name` | `TEXT` | Required, max 100 |
| `CreatorEmail` | `TEXT` | Required, max 256, unique index |
| `CreatedAt` | `TEXT` (SQLite datetime) | Required |

Unique index on `CreatorEmail` enforces one team per creator at the DB level (spec decision summary).

### 4.2 EF Configuration — `src/Services/Reflekt/Reflekt.Persistence/Configurations/TeamConfiguration.cs`

`IEntityTypeConfiguration<Team>`:
- `ToTable("Teams")`
- `Property(t => t.Name).IsRequired().HasMaxLength(100)`
- `Property(t => t.CreatorEmail).IsRequired().HasMaxLength(256)`
- `HasIndex(t => t.CreatorEmail).IsUnique()`
- `Property(t => t.CreatedAt).IsRequired()`

Skill: `persistence-patterns`

## 5. Implementation steps

### Step 1: Team aggregate + domain event

**What:** Create the `Team` aggregate root and `TeamCreatedEvent`.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Teams/Team.cs`
- `src/Services/Reflekt/Reflekt.Domain/Teams/TeamCreatedEvent.cs`
**Pattern:** Follow `create-aggregate` skill. Reference `BuildingBlocks.Domain/AggregateRoot.cs` for the base class shape.
**Dependencies:** None.

### Step 2: EF configuration + DbSet + global DomainEvents ignore

**What:** Add the EF entity configuration, register the `DbSet`, and add a global ignore for `DomainEvents` in `OnModelCreating`.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Persistence/Configurations/TeamConfiguration.cs`
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs`:
  - Add `public DbSet<Team> Teams => Set<Team>();`
  - In `OnModelCreating`, **before** `ApplyConfigurationsFromAssembly`, add a global ignore for `DomainEvents` on all `AggregateRoot`-derived entities. This prevents EF from trying to map the `IReadOnlyList<IEvent>` property (which is not a primitive or entity type). Shape:
    ```
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
            modelBuilder.Entity(entityType.ClrType).Ignore("DomainEvents");
    }
    ```
  This is a one-time setup — future aggregates inherit the ignore automatically.
**Pattern:** Follow `persistence-patterns` skill.
**Dependencies:** Step 1.

### Step 3: TeamRepository + DI registration

**What:** Create a concrete repository (no interface — guardrail) and register it in DI.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/TeamRepository.cs`
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `TeamRepository` as scoped in `AddReflektPersistence`.

**Repository methods:**
- `AddAsync(Team team, CancellationToken ct)` — `db.Teams.AddAsync(team, ct)`
- `SaveChangesAsync(CancellationToken ct)` — wraps `db.SaveChangesAsync(ct)`. Endpoints call this instead of touching `ReflektDbContext` directly (per CLAUDE.md repository convention).
- `GetByCreatorEmailAsync(string email, CancellationToken ct)` — returns `List<Team>` filtered by email (case-insensitive via `StringComparison.OrdinalIgnoreCase` or SQL `COLLATE NOCASE`). Note: SQLite default text comparison is case-sensitive; use `EF.Functions.Collate(t.CreatorEmail, "NOCASE")` or normalize to lowercase.
- `ExistsByCreatorEmailAsync(string email, CancellationToken ct)` — returns `bool`. Used by CreateTeam for one-team-per-creator check.
- `GetByIdAsync(int id, CancellationToken ct)` — returns `Team?`. Needed by the Sprints plan (next feature) for team ownership verification.

**Pattern:** Follow `persistence-patterns` skill. Constructor takes `ReflektDbContext`.
**Dependencies:** Step 2.

### Step 4: EF Migration

**What:** Generate and apply the first EF Core migration.
**Command:**
```bash
dotnet ef migrations add AddTeam -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```
**Verify:** `dotnet ef migrations list -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API` shows the migration. `dotnet run` auto-applies it in Development (existing `UseReflektPersistence` logic).
**Dependencies:** Step 2.

### Step 5: CreateTeamEndpoint + Validator

**What:** Create the `POST /api/teams` endpoint with request validation.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Teams/CreateTeamEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Teams/CreateTeamValidator.cs`

**Endpoint shape:**
- `public sealed class CreateTeamEndpoint : Endpoint<CreateTeamRequest, CreateTeamResponse>`
- `Configure()`: `Post("/api/teams")`, `Policies(AuthPolicies.ScrumMaster)` (use the alias pattern from `LogoutEndpoint.cs`).
- Request record (same file or separate — Developer's call): `public sealed record CreateTeamRequest(string Name);`
- Response record (same file): `public sealed record CreateTeamResponse(int Id, string Name, DateTime CreatedAt);`

**Handler flow:**
1. Extract `email` from `User.FindFirstValue(ClaimTypes.Email)`.
2. Defense-in-depth allowlist check: inject `IOptions<AuthOptions>`, call `AllowlistChecker.IsAllowed(email, options.Value.AllowedEmails)`. If not allowed → `await SendForbiddenAsync(ct); return;`.
3. One-team-per-creator check: call `teamRepository.ExistsByCreatorEmailAsync(email, ct)`. If true → `AddError("You already have a team"); await SendErrorsAsync(409, ct); return;`.
4. Create aggregate: `var team = Team.Create(request.Name, email);`.
5. Persist: `await teamRepository.AddAsync(team, ct);` then `await teamRepository.SaveChangesAsync(ct);`. The repository's `SaveChangesAsync` delegates to `ReflektDbContext`, which triggers the `DomainEventPublishingInterceptor`.
6. Respond: `await SendAsync(new CreateTeamResponse(team.Id, team.Name, team.CreatedAt), 201, ct);`. `team.Id` is populated after step 5.

**Validator shape:**
- `public sealed class CreateTeamValidator : Validator<CreateTeamRequest>`
- Rules: `RuleFor(x => x.Name).NotEmpty().MaximumLength(100);`

**Pattern:** Follow `create-feature` skill. Reference `MeEndpoint.cs` for the policy alias pattern.
**Dependencies:** Steps 1, 3.

### Step 6: GetMyTeamsEndpoint

**What:** Create the `GET /api/teams` endpoint.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Teams/GetMyTeamsEndpoint.cs`

**Endpoint shape:**
- `public sealed class GetMyTeamsEndpoint : EndpointWithoutRequest<List<TeamSummary>>`
- `Configure()`: `Get("/api/teams")`, `Policies(AuthPolicies.ScrumMaster)`.
- Response record (same file): `public sealed record TeamSummary(int Id, string Name, DateTime CreatedAt);`

**Handler flow:**
1. Extract `email` from `User.FindFirstValue(ClaimTypes.Email)`.
2. Query: `var teams = await teamRepository.GetByCreatorEmailAsync(email, ct);`
3. Map to response: `teams.Select(t => new TeamSummary(t.Id, t.Name, t.CreatedAt)).ToList()`.
4. `await SendOkAsync(result, ct);`

No validator needed — no request body.

**Pattern:** Follow `create-feature` skill.
**Dependencies:** Step 3.

### Step 7: Seed data

**What:** Populate `DbInitializer` to create one test team in Development.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — add team seeding logic.
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — pass `IConfiguration` to `SeedAsync` so it can read `Auth:AllowedEmails`.

**Seed logic:**
1. Read `Auth:AllowedEmails` from configuration. If empty, skip seeding (no email to use as creator).
2. If `!db.Teams.Any()`: create `Team.Create("Dev Team", allowedEmails[0])`, add, save.
3. Idempotent — safe to run on every startup.

**`DbInitializer` signature change:**
```
internal static async Task SeedAsync(ReflektDbContext db, IConfiguration config, CancellationToken ct = default)
```

**Call site update** in `UseReflektPersistence`:
```csharp
DbInitializer.SeedAsync(db, app.Configuration).GetAwaiter().GetResult();
```

**Dependencies:** Steps 1–4.

### Step 8: Frontend — teams store

**What:** Create a Pinia store for team state.
**Files to create:**
- `web/reflekt-web/src/stores/teams.ts`

**Store shape:**
```ts
interface Team { id: number; name: string; createdAt: string; }

state: () => ({
  teams: [] as Team[],
  loaded: false,
}),
getters: {
  hasTeam: (s) => s.teams.length > 0,
  currentTeam: (s) => s.teams[0] ?? null,  // v1: one team per creator
},
actions: {
  async fetchMyTeams() { ... },   // GET /api/teams, credentials: 'include'
  async createTeam(name: string) { ... },  // POST /api/teams, on success push to teams array
}
```

- `credentials: 'include'` on both fetches (cookie auth).
- `createTeam` awaits the API response (not optimistic — one-time action, not a hot path). On success, push the returned team into `teams`. On failure, surface error via return value or thrown error — let WelcomeView handle the toast.
- No error store — keep error handling local to the calling component for now.

**Dependencies:** Steps 5, 6 (API must exist; but frontend can be developed in parallel if using Vite dev proxy).

### Step 9: Frontend — WelcomeView update

**What:** Replace the Auth placeholder with real team UI.
**Files to modify:**
- `web/reflekt-web/src/views/WelcomeView.vue`

**Three states:**

1. **Not signed in** (existing): headline + "Sign in with Google" button. No change.
2. **Signed in, no team** (`authStore.isSignedIn && !teamsStore.hasTeam`): show a "Create your first team" card with:
   - Text input for team name.
   - "Create Team" button.
   - On submit: call `teamsStore.createTeam(name)`. On error, show inline message.
3. **Signed in, has team** (`authStore.isSignedIn && teamsStore.hasTeam`): show team card with:
   - Team name prominently.
   - Placeholder text: "Sprint management coming next." (literal — replaced by Sprints plan).

**On mount:** if `authStore.isSignedIn`, call `teamsStore.fetchMyTeams()`. Use a watcher or check after `authStore.fetchMe()` completes — the auth store's `loaded` flag gates this.

**Styling:** Tailwind. Simple card layout. Match the existing WelcomeView aesthetic. No design system, no animations.

**Dependencies:** Step 8.

### Step 10: Tests — unit

**What:** Unit-test the Team aggregate.
**Files to create:**
- `tests/Reflekt.UnitTests/Teams/TeamTests.cs`

**Test cases:**
1. `Create_SetsNameAndCreatorEmailAndCreatedAt` — verify all properties populated.
2. `Create_RaisesTeamCreatedEvent` — verify `DomainEvents` contains one `TeamCreatedEvent` with correct Name and CreatorEmail.
3. `Create_WithEmptyName_Throws` — verify guard clause rejects null/whitespace name.
4. `Create_WithEmptyEmail_Throws` — verify guard clause rejects null/whitespace email.

Use `FluentAssertions`. No setup/fixture — direct calls to `Team.Create(...)`.

**Dependencies:** Step 1.

### Step 11: Tests — integration (with auth test helper)

**What:** Integration-test the team endpoints. Introduce a reusable test auth handler.
**Files to create:**
- `tests/Reflekt.IntegrationTests/Helpers/TestAuthHandler.cs`
- `tests/Reflekt.IntegrationTests/TeamEndpointTests.cs`

**TestAuthHandler:**
- Extends `AuthenticationHandler<AuthenticationSchemeOptions>`.
- `HandleAuthenticateAsync()`: returns `AuthenticateResult.Success` with a `ClaimsPrincipal` containing `ClaimTypes.Email` and `ClaimTypes.Name` claims.
- Claims are configurable — either via a static property on the handler class, or via test-scoped `IOptions`. The simplest approach: a static `TestEmail` and `TestDisplayName` property that tests can set before each request.
- The handler uses `CookieAuthenticationDefaults.AuthenticationScheme` as the scheme name so it satisfies the `ScrumMaster` policy's scheme requirement. Alternatively, register it as the default scheme and adjust the policy. Developer picks the cleanest approach — the goal is: requests with the test handler pass the `ScrumMaster` policy gate.

**Factory setup pattern:**
```csharp
var factory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
        // SQLite unique-per-run file (same pattern as FoundationSmokeTests)
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Reflekt"] = $"Data Source={uniqueDbPath}",
                ["Auth:AllowedEmails:0"] = TestAuthHandler.TestEmail,
            });
        });
        builder.ConfigureTestServices(services =>
        {
            // Replace cookie auth with test auth handler
        });
    });
```

**Test cases:**
1. `CreateTeam_Authenticated_ReturnsCreatedTeam` — POST `/api/teams` with `{ "name": "My Team" }` → 201 with response body containing `id`, `name`, `createdAt`.
2. `CreateTeam_Unauthenticated_Returns401` — POST `/api/teams` without auth → 401.
3. `CreateTeam_DuplicateCreator_Returns409` — create a team, then try creating another → 409.
4. `CreateTeam_EmptyName_ReturnsValidationError` — POST with `{ "name": "" }` → 400.
5. `GetMyTeams_Authenticated_ReturnsTeams` — create a team, then GET `/api/teams` → 200 with one team.
6. `GetMyTeams_Unauthenticated_Returns401` — GET `/api/teams` without auth → 401.
7. `GetMyTeams_NoTeams_ReturnsEmptyArray` — GET `/api/teams` with no prior creation → 200 with `[]`.

Follow the same SQLite cleanup pattern from `FoundationSmokeTests.cs` (`SqliteConnection.ClearAllPools()` before file delete).

**Dependencies:** Steps 5, 6, and functionally all prior steps.

## 6. Migration notes

First EF Core migration in the project. Command:
```bash
dotnet ef migrations add AddTeam -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

Auto-applied on startup in Development by existing `UseReflektPersistence` logic. No manual `dotnet ef database update` needed for local dev.

Delete the existing `data/reflekt.db` file (if present from Foundation) before running — EF will recreate it with the new schema on next startup.

## 7. Testing strategy

- **Unit tests** cover domain invariants (aggregate creation, guard clauses, event raising).
- **Integration tests** cover the full HTTP pipeline (auth → validation → handler → persistence → response).
- **Test auth helper** is reusable — subsequent feature plans will reference `TestAuthHandler` for their integration tests.
- **No SignalR tests** — Teams has no real-time component.
- **No frontend tests** — not in scope for v1.

## 8. Verification — acceptance criteria

Teams is done when all of these pass:

1. `dotnet build Reflekt.slnx` — zero errors, zero warnings.
2. `dotnet test` — all tests pass (existing + new unit + new integration).
3. `dotnet run` in Development:
   - Creates `data/reflekt.db` with `Teams` table.
   - If `Auth:AllowedEmails` is configured + `Seed:Enabled` is true, the "Dev Team" is seeded.
4. Manual flow (dotnet run + npm run dev or build):
   - Sign in → WelcomeView shows "Create your first team" form.
   - Enter team name, click create → team card appears with name.
   - Refresh → team persists (fetched from API).
   - `GET http://localhost:4001/api/teams` with cookie → 200 JSON array with one team.
   - `POST http://localhost:4001/api/teams` again → 409 (one team per creator).
5. `Program.cs` unchanged — still <= 10 lines, no new `Add*`/`Use*` calls.
6. No new authentication schemes, no new authorization policies — existing `ScrumMaster` policy used as-is.

## 9. Out of scope (reminder)

If tempted to add any of these, stop and check the relevant feature plan:

- `Sprint` aggregate or any sprint endpoints — **Sprints plan**.
- `Participant` entity or guest token wiring — **Participants plan**.
- Team invite flow, multi-admin, role hierarchy — **v2** (spec §11).
- Team update/rename/delete endpoints — **not in v1**.
- Team settings or configuration UI — **not in v1**.
- Navigation to a `/teams/:teamId` detail page — **Sprints plan** will introduce if needed.
- SignalR events for team creation — **not needed** (single-user operation).

## 10. File summary

**New files:**
- `src/Services/Reflekt/Reflekt.Domain/Teams/Team.cs`
- `src/Services/Reflekt/Reflekt.Domain/Teams/TeamCreatedEvent.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Configurations/TeamConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/TeamRepository.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Teams/CreateTeamEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Teams/CreateTeamValidator.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Teams/GetMyTeamsEndpoint.cs`
- `web/reflekt-web/src/stores/teams.ts`
- `tests/Reflekt.UnitTests/Teams/TeamTests.cs`
- `tests/Reflekt.IntegrationTests/Helpers/TestAuthHandler.cs`
- `tests/Reflekt.IntegrationTests/TeamEndpointTests.cs`

**Modified files:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `DbSet<Team> Teams`.
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `TeamRepository`, pass `IConfiguration` to `SeedAsync`.
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — seed one team from `AllowedEmails[0]`.
- `web/reflekt-web/src/views/WelcomeView.vue` — replace auth placeholder with team create/view UI.

**Unchanged:**
- `Program.cs`, `Reflekt.API/DependencyInjection.cs`, `AuthenticationConfiguration.cs`, `AuthorizationConfiguration.cs`, `Policies.cs`, `AuthOptions.cs`, `appsettings*.json`, all Auth endpoints, `SprintHub.cs`.
