# Teams Plan — Done Log

## Files Created

- `Reflekt.Domain/Teams/Team.cs` — aggregate state partial class: private ctor, `required init` properties (`Name`, `CreatorEmail`, `CreatedAt`).
- `Reflekt.Domain/Teams/Behaviors/Team.cs` — aggregate behavior partial class: `Create` factory with guards and `AddDomainEvent`.
- `Reflekt.Domain/Teams/Events/TeamCreatedEvent.cs` — `sealed record TeamCreatedEvent(Team Team) : IDomainEvent`.
- `Reflekt.Persistence/Configurations/TeamConfiguration.cs` — EF config: `ToTable("Teams")`, max lengths, unique index on `CreatorEmail`.
- `Reflekt.Persistence/Repositories/TeamRepository.cs` — concrete repo (no interface). `GetByCreatorEmailAsync` uses `EF.Functions.Collate(... "NOCASE")` for SQLite case-insensitivity.
- `Reflekt.API/Features/Teams/CreateTeamEndpoint.cs` — POST `/api/teams`. Allowlist check + one-team guard + `Team.Create` + persist + 201. Request/response records in same file.
- `Reflekt.API/Features/Teams/CreateTeamValidator.cs` — `NotEmpty().MaximumLength(100)` on `Name`.
- `Reflekt.API/Features/Teams/GetMyTeamsEndpoint.cs` — GET `/api/teams`. Reads email from claims, queries by creator, maps to `TeamSummary`.
- `web/reflekt-web/src/stores/teams.ts` — Pinia store with `fetchMyTeams` / `createTeam`, `hasTeam` / `currentTeam` getters.
- `tests/Reflekt.UnitTests/Teams/TeamTests.cs` — 6 unit tests (properties, event raising, guard clauses via `[Theory]`).
- `tests/Reflekt.IntegrationTests/Helpers/TestAuthHandler.cs` — test auth handler registered under own scheme `"Test"`.
- `tests/Reflekt.IntegrationTests/TeamEndpointTests.cs` — 7 integration tests. Two factories (auth + anon) with separate DB paths.

## Files Modified

- `ReflektDbContext.cs` — added `DbSet<Team> Teams`, global `DomainEvents` ignore loop before `ApplyConfigurationsFromAssembly`.
- `Reflekt.Persistence/DependencyInjection.cs` — registered `TeamRepository` as scoped; passed `app.Configuration` to `SeedAsync`.
- `Reflekt.Persistence/DbInitializer.cs` — added team seed logic; calls `team.ClearDomainEvents()` before saving (FastEndpoints not yet initialized at seed time).
- `web/reflekt-web/src/views/WelcomeView.vue` — three-state UI: not signed in / signed in no team / signed in has team.

## Deviations from Plan

- **`TestAuthHandler` scheme:** Used a distinct `"Test"` scheme (not `CookieAuthenticationDefaults`) and re-registered the `ScrumMaster` policy in `ConfigureTestServices` to use it. Registering the same Cookie scheme name twice throws `InvalidOperationException` in ASP.NET Core.
- **Two test factories:** Used separate DB paths for auth and anon factories to avoid SQLite file-locking during concurrent host startup.
- **`Seed:Enabled = "false"` in test config:** Required because `appsettings.Development.json` enables seeding, and the seed creates a team for `TestAuthHandler.TestEmail`, polluting test state.
- **`ClearDomainEvents` in seeder:** `DomainEventPublishingInterceptor` fires before `MapFastEndpoints` initializes FastEndpoints' `Config.ServiceResolver`. Seed is infrastructure — clearing events before save is correct.
- **`using Microsoft.AspNetCore.TestHost`:** Needed for `ConfigureTestServices` — not included in the plan's implied usings.

## Refactor Round — Skill Alignment

### Files Created

- `BuildingBlocks.Domain/IDomainEvent.cs` — `public interface IDomainEvent : IEvent` — establishes the domain event contract per `domain-patterns` skill.
- `BuildingBlocks.Persistence/EntityConfigurations/EntityConfiguration.cs` — base EF config: `HasKey + ValueGeneratedOnAdd`. Reflekt equivalent of `AuditedEntityConfiguration<T>` (no audit fields in this codebase).
- `BuildingBlocks.Persistence/Repositories/RepositoryBase.cs` — generic base: `AddAsync`, `SaveChangesAsync`, `GetByIdAsync`. Minimal implementation; full CRUD surface deferred (scope too large for this round).
- `Reflekt.Persistence/Repositories/Repository.cs` — service-level binding of `RepositoryBase` to `ReflektDbContext`.

### Files Modified

- `BuildingBlocks.Domain/AggregateRoot.cs` — changed `List<IEvent>` / `IReadOnlyList<IEvent>` / `AddDomainEvent(IEvent)` to use `IDomainEvent` throughout. Removed `using FastEndpoints` (no longer needed directly).
- `Reflekt.Domain/Teams/Team.cs` — switched to `required init` properties; now state-only partial class.
- `Reflekt.Domain/Teams/Behaviors/Team.cs` — new file; behavior partial class with `Create` factory (was in `Team.cs`).
- `Reflekt.Domain/Teams/Events/TeamCreatedEvent.cs` — moved from `Teams/` root to `Teams/Events/`; changed from `IEvent` to `IDomainEvent`.
- `Reflekt.Persistence/Configurations/TeamConfiguration.cs` — now extends `EntityConfiguration<Team>` instead of `IEntityTypeConfiguration<Team>` directly; calls `base.Configure(builder)`.
- `Reflekt.Persistence/Repositories/TeamRepository.cs` — now extends `Repository<Team>`; removed `AddAsync`, `SaveChangesAsync`, `GetByIdAsync` (in base); kept custom queries.
- `tests/Reflekt.UnitTests/BuildingBlocksTests/AggregateRootTests.cs` — updated `TestEvent : IDomainEvent`, `RaiseTestEvent(IDomainEvent)`.
- `tests/Reflekt.UnitTests/Teams/TeamTests.cs` — added `using Reflekt.Domain.Teams.Events`.

### Additional changes (Architect)

- `BuildingBlocks.Persistence/ApplicationDbContext.cs` — new base `DbContext` that owns the global `DomainEvents` ignore loop in `OnModelCreating`. All service DbContexts extend this.
- `Reflekt.Persistence/ReflektDbContext.cs` — now extends `ApplicationDbContext` instead of `DbContext`; removed the inline `DomainEvents` ignore loop (moved to base). Calls `base.OnModelCreating` after `ApplyConfigurationsFromAssembly`.

### Skill incompatibilities signalled

- `AuditedEntityConfiguration<T>` — audit fields don't exist in Reflekt's `AggregateRoot`; created `EntityConfiguration<T>` as the Reflekt equivalent.
- Full `RepositoryBase` with CRUD/batch/FindByIdOrThrow — built minimal version; full surface deferred.
- Feature subfolder per endpoint (`Features/{Domain}/{FeatureName}/`) — existing Reflekt code uses flat `Features/{Domain}/`; kept flat to stay consistent.
