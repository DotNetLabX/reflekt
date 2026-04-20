# Teams Plan — Done Log

## Files Created

- `Reflekt.Domain/Teams/Team.cs` — aggregate root with `Create` factory, `ArgumentException.ThrowIfNullOrWhiteSpace` guards, `AddDomainEvent(new TeamCreatedEvent(this))`.
- `Reflekt.Domain/Teams/TeamCreatedEvent.cs` — `sealed record TeamCreatedEvent(Team Team) : IEvent`.
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
