# Foundation Plan — Reflekt v1

**Status:** Ready for implementation
**Prerequisite for:** all feature-slice plans (Teams, Sprints, Participants, Cards, Votes, EvidenceLinks, Reports)
**Companion docs:** `docs/specs/v1.md`, `docs/architecture/v1.md`, `CLAUDE.md`

## 0. Purpose

Foundation scaffolds every piece of infrastructure that features depend on and contains **zero feature-specific code**. When Foundation is done, `dotnet run` starts a fully wired but empty application: no aggregates, no endpoints, no real SignalR events, but every pipeline a feature could need is in place.

**Non-goals for Foundation:**
- No aggregates (`Team`, `Sprint`, `Participant`, `Card`) — defined by feature plans.
- No endpoints — defined by feature plans.
- No domain event types — defined by feature plans.
- No SignalR event handlers — defined by feature plans.
- No seed data content — `DbInitializer` hook exists but body is empty; features populate.
- No real UI screens — single "Reflekt is running" welcome view only.

If you find yourself writing aggregate methods, endpoint classes, or hub broadcasters during Foundation, **stop** — that work belongs in a feature plan.

## 1. Relevant skills

Use these skills where they apply. Read each skill's own file before using it — this plan only names them.

- **`create-service`** — scaffolds a service skeleton from its `CLAUDE.md`. Use this after writing `src/Services/Reflekt/CLAUDE.md` (Step 3 below).
- **`persistence-patterns`** — EF Core DbContext, entity configurations, interceptor patterns. Relevant for Step 5 and Step 6.
- **`authorization-patterns`** — two-layer authorization (role + resource). Relevant for Step 9. Note: at Foundation only the shape is scaffolded; handler bodies land in feature plans.

No existing pattern file to follow — Foundation is the first thing in the codebase. Later plans will reference this structure as the pattern.

## 2. Judgment calls flagged for the Developer

Small decisions the Developer can make without coming back to the Architect:

- **Central Package Management (CPM) via `Directory.Packages.props`** — recommended. Seven projects share NuGet versions; CPM is the modern idiom. Switch on.
- **`Directory.Build.props` common settings** — `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`. **Do not** enable `TreatWarningsAsErrors` at Foundation — low-value friction during scaffolding; can be turned on after features land.
- **Test naming convention** — pick what reads cleanest; no house style yet.
- **Vue scaffolding tool** — `npm create vue@latest` with TypeScript + Pinia + Router is the baseline. Skip Vitest if offered; we're not writing Vue tests at Foundation.

Decisions that should come back to the Architect if they feel wrong mid-implementation:

- Placement of any primitive that feels "generic" — **BuildingBlocks or Reflekt.API?** Rule from architecture §4: > 2-3 lines + zero Reflekt domain knowledge → BuildingBlocks. Otherwise inside the service project.

## 3. Service CLAUDE.md

Write `src/Services/Reflekt/CLAUDE.md` first. This is the minimal service-level doc that the `create-service` skill consumes. Record only what differs from the root `CLAUDE.md`:

- Service index: `1` (host port derivation handled by the table in root CLAUDE.md).
- Database: SQLite file at `./data/reflekt.db`, connection string key `ConnectionStrings:Reflekt`.
- Auth schemes active: `Cookies` (backed by `Google`), `GuestToken` (custom handler, header `X-Guest-Token`).
- SignalR: one hub `SprintHub` at `/hubs/sprint`, groups `sprint:{sprintId}`.
- BuildingBlocks it depends on: `BuildingBlocks.Domain`, `BuildingBlocks.Persistence`.

Do **not** duplicate port numbers, the full tech stack, or the guardrails — they live in root `CLAUDE.md`.

## 4. Solution + project scaffold

Create `Reflekt.sln` at repo root. Create projects in this order (each via `dotnet new`), then add references:

| Project | Path | Template | Notes |
|---|---|---|---|
| `BuildingBlocks.Domain` | `src/BuildingBlocks/BuildingBlocks.Domain/` | `classlib` | Pure abstractions. Package: `FastEndpoints` (for `IEvent` marker only — abstraction package if one exists, else main package). |
| `BuildingBlocks.Persistence` | `src/BuildingBlocks/BuildingBlocks.Persistence/` | `classlib` | References `BuildingBlocks.Domain`. Packages: `Microsoft.EntityFrameworkCore`, `FastEndpoints`. |
| `Reflekt.Domain` | `src/Services/Reflekt/Reflekt.Domain/` | `classlib` | References `BuildingBlocks.Domain`. No other packages. |
| `Reflekt.Persistence` | `src/Services/Reflekt/Reflekt.Persistence/` | `classlib` | References `Reflekt.Domain`, `BuildingBlocks.Persistence`. Packages: `Microsoft.EntityFrameworkCore.Sqlite`. Also `<FrameworkReference Include="Microsoft.AspNetCore.App" />` so `WebApplication` is accessible from `UseReflektPersistence`. |
| `Reflekt.API` | `src/Services/Reflekt/Reflekt.API/` | `web` (empty web) | References `Reflekt.Domain`, `Reflekt.Persistence`. Packages: see §5. |
| `Reflekt.UnitTests` | `tests/Reflekt.UnitTests/` | `xunit` | References `Reflekt.Domain`, `BuildingBlocks.Domain`. Packages: `FluentAssertions`. |
| `Reflekt.IntegrationTests` | `tests/Reflekt.IntegrationTests/` | `xunit` | References `Reflekt.API`. Packages: `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Data.Sqlite` (direct reference so `SqliteConnection.ClearAllPools()` is accessible in test cleanup — required on Windows to release the `.db` file lock after `WebApplicationFactory.DisposeAsync`). |

Also create:

- `Directory.Build.props` at repo root — common settings above.
- `Directory.Packages.props` at repo root — CPM on, centralized package versions.
- `.gitignore` additions — `data/*.db`, `data/*.db-journal`, `node_modules/`, Vite build artifacts if not already ignored.
- Empty folders (committed via `.gitkeep` if needed): `src/Services/Reflekt/Reflekt.Domain/{Teams,Sprints,Participants,Cards,Events}`, `src/Services/Reflekt/Reflekt.Persistence/{Configurations,Repositories,Migrations}`, `src/Services/Reflekt/Reflekt.API/{Features/Health,Events,Hubs,Auth,OpenGraph}`.
- **`src/Services/Reflekt/Reflekt.API/Features/Health/HealthEndpoint.cs`** — anonymous `EndpointWithoutRequest` returning `GET /health` → minimal OK body. Required because FastEndpoints 5.x throws `InvalidOperationException` on startup when it finds zero `IEndpoint` implementations. This is infrastructure, not a feature endpoint. A handful of lines.
- **`src/Services/Reflekt/Reflekt.API/GlobalUsings.cs`** — `global using Reflekt.API;`, `global using Reflekt.Persistence;`, `global using FastEndpoints;`. Keeps `Program.cs` usings-free and stops every endpoint class from repeating `using FastEndpoints;`.

**Note on `.sln` vs `.slnx`:** .NET 10's `dotnet new sln` emits `.slnx` (XML) by default. Use whichever the tool produces — both are accepted by `dotnet build` and `dotnet test`.

Acceptance: `dotnet build` on the solution succeeds with zero warnings and zero project-reference violations.

## 5. `Reflekt.API` package references

Add to `Reflekt.API.csproj`:

- `FastEndpoints` — endpoint classes, validation pipeline, `IEvent` bus.
- `FluentValidation` — explicit pin, auto-wired by FastEndpoints.
- `Mapster` + `Mapster.DependencyInjection` — object mapping.
- `Microsoft.AspNetCore.Authentication.Cookies` — session cookie.
- `Microsoft.AspNetCore.Authentication.Google` — Google OAuth.
- `Microsoft.AspNetCore.OpenApi` — OpenAPI document (not Swashbuckle).
- `Scalar.AspNetCore` — interactive OpenAPI UI, gated to `IsDevelopment`.
- `AngleSharp` — HTML parsing for OpenGraph.
- `Microsoft.EntityFrameworkCore.Design` with **`PrivateAssets=all`** — design-time tooling for `dotnet ef migrations`. Lives on the startup project (not `Reflekt.Persistence`) so the `dotnet ef` CLI resolves it correctly.

Explicitly **not** added: `MediatR`, `Swashbuckle.AspNetCore`, any dedicated OpenGraph library, any UI kit runtime, `MassTransit`, `RabbitMQ.Client`, `Grpc.*`, `Yarp.*`.

## 6. `BuildingBlocks.Domain`

Files to create:

- **`Entity.cs`** — abstract base class, owns identity + equality:
  - `int Id { get; protected set; }` — EF Core maps `int Id` to SQLite `INTEGER PRIMARY KEY` (auto-increment). No `[Key]` attribute. IDs are populated by `SaveChanges`, which runs **before** the domain-event interceptor — so event payloads carry the real populated ID.
  - Equality by type + Id, `==`/`!=` operators, `GetHashCode` from Id.
- **`AggregateRoot.cs`** — abstract base class, **extends `Entity`**:
  - Does **not** redeclare `Id` — inherits from `Entity`.
  - `IReadOnlyList<IEvent> DomainEvents { get; }` (backed by a private `List<IEvent>`).
  - `protected void AddDomainEvent(IEvent ev)` — appends.
  - `ClearDomainEvents()` — used by the interceptor after dispatch. Either mark it `internal` and add `[assembly: InternalsVisibleTo("BuildingBlocks.Persistence")]` in `BuildingBlocks.Domain`, **or** make it `public` if you prefer a simpler surface. Either is acceptable.
- **No `ValueObject` base class at Foundation.** Records + `IEquatable<T>` handle all value objects we need. Add a base only if three+ VOs end up with duplicated equality boilerplate.

## 7. `BuildingBlocks.Persistence`

Files to create:

- **`DomainEventPublishingInterceptor.cs`** — extends `SaveChangesInterceptor`:
  - Override `SavedChangesAsync`.
  - After successful save, enumerate `eventData.Context.ChangeTracker.Entries<AggregateRoot>()`, collect their `DomainEvents` into a local list, then publish via `ev.PublishAsync(Mode.WaitForNone)` (FastEndpoints default) and clear each aggregate's `DomainEvents`. Clearing before or after publish is both acceptable — the events are already captured into the local list, and `WaitForNone` is fire-and-forget so the clear order doesn't affect what gets dispatched.
  - **Performance note:** walk the change tracker **once**, collect all events into a single list, await a single `Task.WhenAll` over the publish tasks. Do not per-aggregate `await` inside a loop — pointless serialization.
  - Swallows no exceptions — if publishing throws, let it propagate; the save already succeeded, so throwing here is a hard bug in an event handler and should be loud.

The interceptor is registered by `Reflekt.Persistence`'s DI (Step 8).

## 8. `Reflekt.Persistence`

### 8.1 `ReflektDbContext.cs`

- Inherits `DbContext`.
- Constructor takes `DbContextOptions<ReflektDbContext>`.
- **No `DbSet<T>` properties at Foundation** — each feature plan adds its own.
- `OnModelCreating` calls `modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReflektDbContext).Assembly)` so feature plans drop `IEntityTypeConfiguration<T>` files into `Configurations/` without touching this class again.

### 8.2 `DependencyInjection.cs`

Single file with two static methods on `IServiceCollection` and `WebApplication` extensions:

- **`AddReflektPersistence(this IServiceCollection, IConfiguration)`**:
  - Registers `DomainEventPublishingInterceptor` as a singleton.
  - Registers `ReflektDbContext` with SQLite via `ConnectionStrings:Reflekt`, and `AddInterceptors(...)` pulling the interceptor from DI.
  - No repositories at Foundation — feature plans add their own repository registrations here.
- **`UseReflektPersistence(this WebApplication)`**:
  - Dev-only (`app.Environment.IsDevelopment()`): resolve `ReflektDbContext`, call `Database.Migrate()`.
  - Dev-only: if `Seed:Enabled` is true, run `DbInitializer.SeedAsync(dbContext)` — **`DbInitializer` body is empty at Foundation**. Feature plans populate it as their aggregates land.

### 8.3 `DbInitializer.cs`

Empty stub under `Reflekt.Persistence/`:

```csharp
internal static class DbInitializer
{
    public static Task SeedAsync(ReflektDbContext db, CancellationToken ct = default)
        => Task.CompletedTask; // Features populate this as their aggregates land.
}
```

## 9. `Reflekt.API` DI composition

**Rule from architecture §4:** every registration > 2-3 lines moves out of `AddReflektApi` into a sibling configuration file. Trivial registrations stay inline.

### 9.1 `Reflekt.API/DependencyInjection.cs`

Two methods:

- **`AddReflektApi(this IServiceCollection, IConfiguration)`** — composes:
  - Inline (trivial): `AddFastEndpoints()`, `AddSignalR()`, Mapster config registration, `AddOpenApi()`, options binding for `Auth:AllowedEmails` and `Seed`.
  - Sub-extension (> 2-3 lines): `services.AddReflektAuthentication(configuration)` (see §9.2).
  - Sub-extension: `services.AddReflektAuthorization()` (see §9.3).
  - Sub-extension: `services.AddReflektOpenGraph()` (see §9.4).
- **`UseReflektApi(this WebApplication)`** — middleware pipeline, in order:
  - Exception handler (developer page in dev, problem+json in prod).
  - Default files + static files (`wwwroot/`).
  - Routing.
  - Authentication, Authorization.
  - `MapFastEndpoints()`.
  - `MapHub<SprintHub>("/hubs/sprint")`.
  - Dev-only: `MapOpenApi()` + `MapScalarApiReference()` (Scalar UI gate).
  - SPA fallback: `MapFallbackToFile("index.html")`.

### 9.2 `Reflekt.API/Auth/AuthenticationConfiguration.cs`

- **`AddReflektAuthentication(this IServiceCollection, IConfiguration)`**:
  - Default scheme: `Cookies`.
  - Adds `Cookies` (session cookie config — `SameSite=Lax`, `HttpOnly=true`, secure in prod).
  - Adds `Google` (reads `Auth:Google:ClientId` / `ClientSecret` — leave as placeholders in `appsettings.json`, expect real values via user secrets locally). **Scopes limited to `openid`, `email`, `profile` per spec §3.1 — clear any defaults that pull photo or extended scopes.**
  - Adds custom `GuestToken` scheme backed by `GuestTokenAuthenticationHandler` (see §9.5).

### 9.3 `Reflekt.API/Auth/AuthorizationConfiguration.cs`

- **`AddReflektAuthorization(this IServiceCollection)`**:
  - Registers three policies via `Policies.*` constants (see below): `Policies.ScrumMaster`, `Policies.Participant`, `Policies.OwnerOrScrumMaster`.
  - **At Foundation, bodies are minimal.** Each policy just requires the correct authentication scheme + `RequireAuthenticatedUser()`. Resource-based authorization (card ownership, team membership) is added by the feature plans that use it (Teams adds SM team match; Cards adds owner-or-SM resource handler).

**Auth-name constants** (avoid magic strings across endpoints and hub attributes):

- **`Reflekt.API/Auth/GuestTokenDefaults.cs`** — `public static class` with `public const string AuthenticationScheme = "GuestToken"`. Referenced from `AuthenticationConfiguration`, `AuthorizationConfiguration`, and `SprintHub[Authorize(...)]`.
- **`Reflekt.API/Auth/Policies.cs`** — `public static class` with `public const string ScrumMaster`, `Participant`, `OwnerOrScrumMaster` (values match the names). Referenced from policy registration and `[Authorize(Policy = Policies.*)]` attributes on future endpoints.

### 9.4 `Reflekt.API/OpenGraph/OpenGraphConfiguration.cs`

- **`AddReflektOpenGraph(this IServiceCollection)`**:
  - Registers a bounded `Channel<FetchJob>` (capacity 100, `BoundedChannelFullMode.Wait`); exposes `ChannelReader<FetchJob>` and `ChannelWriter<FetchJob>` as singletons.
  - Registers `IHttpClientFactory` config for a named client `"opengraph"` with 5-second timeout **and** `MaxResponseContentBufferSize = 2 * 1024 * 1024` (2 MB) to bound memory per fetch — pairs with the SSRF guard to harden the worker.
  - Registers `OpenGraphBackgroundService` as a hosted service.
  - Registers `OpenGraphFetcher` (implementation that does HttpClient GET + AngleSharp parse + SSRF guard). See §11.

### 9.5 `Reflekt.API/Auth/GuestTokenAuthenticationHandler.cs`

Custom `AuthenticationHandler<AuthenticationSchemeOptions>`:

- Reads `X-Guest-Token` from request headers.
- For SignalR handshake, also reads `access_token` query param (standard pattern).
- **At Foundation, the handler returns `AuthenticateResult.NoResult()` when the header is missing and `AuthenticateResult.Fail("Participant lookup not yet wired")` when present.** The `Participants` feature plan wires the real lookup (query `Participant` by `GuestToken`, issue claims). The stub exists so downstream code compiles and tests can assert 401 behavior.
- Emit a `// TODO(feature=Participants):` comment at the lookup site so `grep TODO` surfaces it.

### 9.6 File size expectation

`Reflekt.API/DependencyInjection.cs` should end at ~30-40 lines. If it starts growing past 60, a concern needs extracting.

## 10. `Reflekt.API/Program.cs`

Exact shape — **no framework `Add*` or `Use*` calls live here directly**:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReflektPersistence(builder.Configuration);
builder.Services.AddReflektApi(builder.Configuration);

var app = builder.Build();

app.UseReflektPersistence();
app.UseReflektApi();

app.Run();
```

That is the entire file. If it grows, the growth belongs inside an extension method.

## 11. SignalR skeleton

**`Reflekt.API/Hubs/SprintHub.cs`**:

- `[Authorize(AuthenticationSchemes = GuestTokenDefaults.AuthenticationScheme)]` on the class (constant from §9.3).
- `OnConnectedAsync()` — logs connection (ILogger), no group join yet.
- `JoinSprint(int sprintId)` method — calls `Groups.AddToGroupAsync(Context.ConnectionId, $"sprint:{sprintId}")`. **At Foundation, no participant-vs-sprint validation.** The `Participants` feature plan adds the check (participant exists, belongs to this sprint) before group join.
- `LeaveSprint(int sprintId)` — symmetric removal.

No hub methods that push to clients at Foundation — pushes live in `IEventHandler<T>` implementations added by feature plans.

## 12. OpenGraph pipeline skeleton

Files under `Reflekt.API/OpenGraph/`:

- **`FetchJob.cs`** — `record FetchJob(int EvidenceLinkId, string Url, int SprintId)`. The shape is internal-use; feel free to refine when `EvidenceLinks` feature lands.
- **`OpenGraphFetcher.cs`** — pure fetch primitive:
  - Takes a URL, returns a result record (`(bool Ok, string? Title, string? Description, string? ImageUrl, string? IconUrl)`).
  - Uses named `HttpClient` `"opengraph"` from `IHttpClientFactory`.
  - SSRF guard: reject non-HTTP(S) schemes; resolve host and reject localhost / private IP ranges (10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 169.254.0.0/16, ::1).
  - Parses OpenGraph tags via AngleSharp.
  - On any failure (network, timeout, parse error) returns `Ok = false`. Does not throw.
- **`OpenGraphBackgroundService.cs`** — `BackgroundService`:
  - Reads `ChannelReader<FetchJob>` until cancellation.
  - For each job: calls `OpenGraphFetcher`, then **at Foundation logs the result**. The `EvidenceLinks` feature plan replaces the log with a DB update on the `EvidenceLink` entity.
  - Emit `// TODO(feature=EvidenceLinks):` at the log line.

**`OpenGraphFetcher` placement decision:** stays in `Reflekt.API` for now. It is > 2-3 lines but it carries some Reflekt-specific policy (exact timeout, exact SSRF ranges). If a second service needs SSRF-guarded HTTP fetching later, extract to `BuildingBlocks.Api` (new project) at that point. Flag this note in the file header.

## 13. Frontend scaffold

Under `web/reflekt-web/` (outside `src/`):

- Scaffold via `npm create vue@latest reflekt-web` — choose TypeScript, Pinia, Vue Router. Decline Vitest + ESLint + Prettier unless you want them (not required for Foundation).
- Add Tailwind CSS per the official Vite + Vue guide.
- `vite.config.ts`:
  - `build.outDir = '../../src/Services/Reflekt/Reflekt.API/wwwroot'`
  - `build.emptyOutDir = true`
- Three empty Pinia stores under `src/stores/`: `auth.ts`, `sprint.ts`, `guest.ts`. Each is `defineStore` with `state: () => ({})` and a short comment: "Populated by feature plans."
- **`guest.ts` bakes in the localStorage key format now: `reflekt:sprint:{sprintId}:token`** (spec §3.2). Writes happen in the Participants feature, but the key helper is defined at Foundation so the format is locked.
- Vue Router with one route: `/` → `WelcomeView.vue`.
- `WelcomeView.vue` — one paragraph: "Reflekt v1 — foundation running." Nothing more.
- Install `@microsoft/signalr` — connection is NOT established at Foundation; just import to confirm the package resolves. Feature plans wire the connection.

Verification: `npm run build` in `web/reflekt-web/` populates `src/Services/Reflekt/Reflekt.API/wwwroot/` with `index.html` + assets. `dotnet run` on the API then serves the welcome view at `/`.

## 14. Tests

### 14.1 `Reflekt.UnitTests`

One smoke test in `BuildingBlocksTests/AggregateRootTests.cs`:

- `AggregateRoot_Raise_AppendsEvent` — create a test subclass of `AggregateRoot`, add a fake event, assert `DomainEvents` count is 1 and contains the event.

### 14.2 `Reflekt.IntegrationTests`

**Required in `Reflekt.API/Program.cs`:** add `public partial class Program { }` at the bottom of the file. Top-level statements generate an `internal Program` class by default; `WebApplicationFactory<Program>` needs it visible to the test project.

One smoke test in `FoundationSmokeTests.cs`:

- `Application_Starts` — construct `WebApplicationFactory<Program>`, call `CreateClient()`, `GET /` returns 200 (SPA fallback serves `index.html` — rely on the Vue build being present; if not, assert 200 or 404 either way proves routing is wired without exception).
- Use a **unique-per-run** SQLite file path — e.g., `Path.Combine(Path.GetTempPath(), $"reflekt-test-{Guid.NewGuid():N}.db")` — passed in via in-memory config override of `ConnectionStrings:Reflekt`. Cleanup in `IAsyncLifetime.DisposeAsync`: call `SqliteConnection.ClearAllPools()` **before** `File.Delete(path)` (Windows file-lock race — SQLite connection pool outlives `WebApplicationFactory` disposal). Requires `Microsoft.Data.Sqlite` as a direct package reference on the test project. Do **not** use EF Core InMemory provider — integration tests must exercise the real SQLite driver (feedback from similar past projects: InMemory hides migration and relational-constraint bugs).

No SignalR integration test at Foundation — once a feature lands that actually uses the hub, its plan adds the SignalR happy-path test.

## 15. Config files

### 15.1 `src/Services/Reflekt/Reflekt.API/appsettings.json`

```json
{
  "ConnectionStrings": {
    "Reflekt": "Data Source=./data/reflekt.db"
  },
  "Auth": {
    "Google": {
      "ClientId": "",
      "ClientSecret": ""
    },
    "AllowedEmails": []
  },
  "Seed": {
    "Enabled": false
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "AllowedHosts": "*"
}
```

### 15.2 `src/Services/Reflekt/Reflekt.API/appsettings.Development.json`

```json
{
  "Seed": { "Enabled": true },
  "Logging": { "LogLevel": { "Default": "Debug", "Microsoft.AspNetCore": "Information" } }
}
```

Note: `Auth:AllowedEmails` and Google OAuth credentials are **not** committed. Developer sets them via `dotnet user-secrets` in development.

### 15.3 `src/Services/Reflekt/Reflekt.API/Properties/launchSettings.json`

HTTP profile only, host port `4001` (per architecture §9 and root CLAUDE.md port table):

- Profile name: `Reflekt`.
- `applicationUrl`: `http://localhost:4001`.
- `environmentVariables`: `ASPNETCORE_ENVIRONMENT=Development`.
- No HTTPS profile. No IIS Express profile.

### 15.4 Data directory

Create `data/` at repo root with a `.gitkeep`. Add `data/*.db` and `data/*.db-*` to `.gitignore`.

## 16. Verification — acceptance criteria

Foundation is done when all of these pass without modification:

1. `dotnet build Reflekt.sln` — zero errors, zero warnings.
2. `dotnet test` — both test projects pass with ≥1 test each.
3. `dotnet run --project src/Services/Reflekt/Reflekt.API`:
   - Starts without exception.
   - In Development: creates `data/reflekt.db`, applies migrations (empty first migration OK), runs empty seed.
   - `GET http://localhost:4001/scalar/v1` — Scalar UI loads.
   - `GET http://localhost:4001/openapi/v1.json` — returns a valid (empty) OpenAPI document.
   - `POST http://localhost:4001/hubs/sprint/negotiate?negotiateVersion=1` with no `access_token` — returns 401 (proves the `GuestToken` scheme is wired on the hub).
4. Frontend build: `cd web/reflekt-web && npm install && npm run build` — populates `src/Services/Reflekt/Reflekt.API/wwwroot/`. Re-running `dotnet run` and hitting `GET http://localhost:4001/` serves the welcome page.
5. EF Core tooling is wired: `dotnet ef migrations list -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API` runs without error and reports zero migrations. The first real migration lands with the `Teams` feature plan, not Foundation — EF Core won't generate a migration with no entity changes.
6. Solution structure matches architecture §4 tree exactly — no stray folders, no missing ones.
7. Project reference graph matches architecture §4:
   - `Reflekt.API → Reflekt.Domain, Reflekt.Persistence` only.
   - `Reflekt.Persistence → Reflekt.Domain, BuildingBlocks.Persistence`.
   - `Reflekt.Domain → BuildingBlocks.Domain`.
   - `BuildingBlocks.Persistence → BuildingBlocks.Domain`.
   - No backward references (Domain never references Persistence or API).
8. `Program.cs` in `Reflekt.API` is ≤ 10 lines of code (comments aside). No `builder.Services.AddXxx` or `app.UseXxx` calls beyond the four composite extensions.

## 17. Out of scope (reminder)

Explicitly not part of Foundation — if you feel tempted to add any of these, stop and check the feature plan for it:

- Any aggregate class (`Team`, `Sprint`, `Participant`, `Card`) — feature plans.
- Any **feature** endpoint class — feature plans. (The infrastructure `HealthEndpoint` at `Reflekt.API/Features/Health/` is Foundation scope — FastEndpoints 5.x requires ≥1 endpoint at startup.)
- Any `DbSet<T>` on `ReflektDbContext` — feature plans.
- Any `IEntityTypeConfiguration<T>` — feature plans.
- Any `IEventHandler<T>` — feature plans.
- Any hub push method — feature plans.
- Any SignalR authentication beyond the stub — `Participants` feature.
- Real OpenGraph DB update — `EvidenceLinks` feature.
- Real seed data — whichever feature introduces the aggregate being seeded.
- `BuildingBlocks.Api` project — deferred until a genuine reusable web primitive emerges.
