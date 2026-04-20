# Foundation — Done

## Files Created

### Root / Build
- `Directory.Build.props` — common MSBuild settings (net10.0, Nullable, ImplicitUsings, LangVersion)
- `Directory.Packages.props` — CPM enabled; centralized package versions
- `Reflekt.slnx` — solution file (dotnet new sln creates .slnx in .NET 10, not .sln)
- `data/.gitkeep` — tracked empty data directory
- `.gitignore` — appended SQLite db entries, wwwroot Vite output

### Service CLAUDE.md
- `src/Services/Reflekt/CLAUDE.md` — service index, DB, auth schemes, SignalR, BuildingBlocks

### BuildingBlocks.Domain
- `src/BuildingBlocks/BuildingBlocks.Domain/AggregateRoot.cs` — abstract base with DomainEvents list, AddDomainEvent, ClearDomainEvents (public)
- `src/BuildingBlocks/BuildingBlocks.Domain/Entity.cs` — abstract base with int Id and equality by Id

### BuildingBlocks.Persistence
- `src/BuildingBlocks/BuildingBlocks.Persistence/DomainEventPublishingInterceptor.cs` — SavedChangesAsync override; walks ChangeTracker once, collects all events, clears aggregates, Task.WhenAll publish

### Reflekt.Domain (empty skeleton)
- Folder placeholders: Teams/, Sprints/, Participants/, Cards/, Events/

### Reflekt.Persistence
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — DbContext with ApplyConfigurationsFromAssembly; no DbSet at Foundation
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — AddReflektPersistence (singleton interceptor + DbContext with SQLite), UseReflektPersistence (dev-only migrate + seed)
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — empty stub returning Task.CompletedTask

### Reflekt.API
- `src/Services/Reflekt/Reflekt.API/GlobalUsings.cs` — global using Reflekt.API, Reflekt.Persistence so Program.cs needs no usings
- `src/Services/Reflekt/Reflekt.API/SeedOptions.cs` — options class for Seed section
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — AddReflektApi + UseReflektApi composing all sub-extensions
- `src/Services/Reflekt/Reflekt.API/Program.cs` — 10-line orchestration only; `public partial class Program` at bottom
- `src/Services/Reflekt/Reflekt.API/Auth/AuthOptions.cs` — AllowedEmails list
- `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs` — Cookies + Google + GuestToken scheme registration
- `src/Services/Reflekt/Reflekt.API/Auth/AuthorizationConfiguration.cs` — ScrumMaster, Participant, OwnerOrScrumMaster policies (minimal at Foundation)
- `src/Services/Reflekt/Reflekt.API/Auth/GuestTokenAuthenticationHandler.cs` — reads X-Guest-Token header + access_token query param; stub returns Fail when present (TODO:Participants)
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` — [Authorize(GuestToken)], OnConnectedAsync logs, JoinSprint/LeaveSprint group management
- `src/Services/Reflekt/Reflekt.API/OpenGraph/FetchJob.cs` — record(EvidenceLinkId, Url, SprintId)
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphFetcher.cs` — FetchResult record + fetcher with SSRF guard (private IPv4 ranges, loopback) + AngleSharp parse
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphBackgroundService.cs` — BackgroundService reading Channel, TODO:EvidenceLinks comment at log line
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphConfiguration.cs` — bounded Channel(100), named HttpClient(5s), hosted service, fetcher registration
- `src/Services/Reflekt/Reflekt.API/Properties/launchSettings.json` — Reflekt profile, port 4001, no HTTPS
- `src/Services/Reflekt/Reflekt.API/appsettings.json` — connection string, auth placeholders, seed disabled, logging
- `src/Services/Reflekt/Reflekt.API/appsettings.Development.json` — seed enabled, debug logging

### Tests
- `tests/Reflekt.UnitTests/BuildingBlocksTests/AggregateRootTests.cs` — smoke test: raise event, assert DomainEvents count
- `tests/Reflekt.IntegrationTests/FoundationSmokeTests.cs` — WebApplicationFactory with test SQLite path override; GET / → 200 or 404; cleanup in Dispose

### Frontend
- `web/reflekt-web/` — scaffolded via `npm create vue@latest` with TypeScript, Pinia, Router
- `web/reflekt-web/vite.config.ts` — build.outDir → wwwroot, emptyOutDir=true, Tailwind plugin
- `web/reflekt-web/src/assets/main.css` — @import "tailwindcss" (Tailwind v4)
- `web/reflekt-web/src/App.vue` — simplified to just `<RouterView />`
- `web/reflekt-web/src/router/index.ts` — single route `/` → WelcomeView
- `web/reflekt-web/src/views/WelcomeView.vue` — "Reflekt v1 — foundation running."
- `web/reflekt-web/src/stores/auth.ts` — empty Pinia store, commented "Populated by feature plans"
- `web/reflekt-web/src/stores/sprint.ts` — empty Pinia store
- `web/reflekt-web/src/stores/guest.ts` — empty store + `guestTokenKey(sprintId)` helper locked at Foundation
- `web/reflekt-web/package.json` — `@microsoft/signalr ^10.0.0` installed (connection not wired; import confirms package resolves)

### PK type change (plan update)
- `AggregateRoot.cs` — `Id` changed from `Guid` to `int`
- `Entity.cs` — `Id` changed from `Guid` to `int`
- `FetchJob.cs` — `EvidenceLinkId` and `SprintId` changed from `Guid` to `int`
- `SprintHub.cs` — `JoinSprint`/`LeaveSprint` parameters changed from `Guid` to `int`
- `AggregateRootTests.cs` — test subclass `Id` initializer changed from `Guid.NewGuid()` to `1`

### Simplify pass
- `src/BuildingBlocks/BuildingBlocks.Domain/AggregateRoot.cs` — now extends `Entity`; duplicate `Id` removed
- `src/Services/Reflekt/Reflekt.API/Auth/GuestTokenDefaults.cs` — new: `const string AuthenticationScheme = "GuestToken"`
- `src/Services/Reflekt/Reflekt.API/Auth/Policies.cs` — new: `const string` fields for all three policy names
- `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs` — uses `GuestTokenDefaults.AuthenticationScheme`
- `src/Services/Reflekt/Reflekt.API/Auth/AuthorizationConfiguration.cs` — uses `Policies.*` + `GuestTokenDefaults.*`; meta-comment removed
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` — uses `GuestTokenDefaults.AuthenticationScheme`; comment changed to TODO format
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphConfiguration.cs` — dropped `Channel<FetchJob>` full-channel singleton; added `MaxResponseContentBufferSize = 2MB`
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphFetcher.cs` — bare `catch {}` → `when (ex is not OperationCanceledException)`; `EnsureSuccessStatusCode()` → `if (!IsSuccessStatusCode)`; `Configuration.Default` cached as `static readonly`; CancellationToken passed to DNS lookup
- `tests/Reflekt.IntegrationTests/FoundationSmokeTests.cs` — test DB path now unique per run (`Path.GetTempPath() + Guid`)

### Review cycle 2 fix
- `src/Services/Reflekt/Reflekt.Persistence/Reflekt.Persistence.csproj` — removed `Microsoft.EntityFrameworkCore.Design` (EF Design belongs on the startup project only; `Reflekt.API.csproj` already has it)

### Post-session fixes
- `src/Services/Reflekt/Reflekt.API/Features/Health/HealthEndpoint.cs` — minimal `GET /health` endpoint to satisfy FastEndpoints' "at least one endpoint" requirement
- `src/Services/Reflekt/Reflekt.API/GlobalUsings.cs` — added `global using FastEndpoints` so endpoint classes compile without per-file usings
- `src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj` — added `Microsoft.EntityFrameworkCore.Design` (PrivateAssets=all) for EF tooling
- `Directory.Packages.props` — added `Microsoft.Data.Sqlite` 9.0.4 version entry
- `tests/Reflekt.IntegrationTests/Reflekt.IntegrationTests.csproj` — added `Microsoft.Data.Sqlite` direct reference
- `tests/Reflekt.IntegrationTests/FoundationSmokeTests.cs` — switched to `IAsyncLifetime`; calls `SqliteConnection.ClearAllPools()` before file delete

## Verification (§16 acceptance criteria)

1. `dotnet build Reflekt.slnx` — 0 errors, 0 warnings ✓
2. `dotnet test` — 2/2 tests pass (unit + integration) ✓
3. `dotnet run` starts without exception; DB created at `./data/reflekt.db`, migration lock acquired ✓
4. `GET /scalar/v1` — 200 ✓
5. `GET /openapi/v1.json` — 200 ✓
6. `POST /hubs/sprint/negotiate?negotiateVersion=1` (no token) — 401 ✓
7. `npm run build` populates `wwwroot/` ✓ (confirmed via Vue build)
8. `dotnet ef migrations list` — "No migrations were found" ✓

Note: criteria 4–6 require non-empty `Auth:Google:ClientId/ClientSecret` (set via `dotnet user-secrets`; `Reflekt.API.csproj` now has `<UserSecretsId>` for this purpose).

## Key Decisions

- `ClearDomainEvents()` made `public` (plan offered either public or InternalsVisibleTo; public is simpler)
- `IMapper` qualified as `MapsterMapper.IMapper` to resolve ambiguity with `FastEndpoints.IMapper`
- `Reflekt.Persistence.csproj` has `<FrameworkReference Include="Microsoft.AspNetCore.App" />` so `WebApplication` is accessible
- `FetchResult` record defined inline in `OpenGraphFetcher.cs` (plan described it but didn't mandate a separate file)
- `HealthEndpoint` added as infrastructure necessity (FastEndpoints throws on startup with zero endpoints); not a feature endpoint

### Foundation Skill-Alignment Refactor

- `src/BuildingBlocks/BuildingBlocks.Persistence/ApplicationDbContext.cs` — NEW: abstract `ApplicationDbContext : DbContext` base; moves DomainEvents-ignore loop out of each service DbContext
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — now extends `ApplicationDbContext`; removed manual DomainEvents-ignore loop; calls `base.OnModelCreating`
- `src/Services/Reflekt/Reflekt.Domain/GlobalUsings.cs` — NEW: `global using BuildingBlocks.Domain`
- `src/Services/Reflekt/Reflekt.Persistence/GlobalUsings.cs` — NEW: `global using` for EF Core, BuildingBlocks.Persistence, Reflekt.Domain
- `src/Services/Reflekt/Reflekt.API/GlobalUsings.cs` — added standard `Microsoft.AspNetCore.*` and `Microsoft.Extensions.*` usings
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/TeamConfiguration.cs` — MOVED from `Configurations/`; namespace updated to `Reflekt.Persistence.EntityConfigurations`
- `src/Services/Reflekt/Reflekt.Persistence/Configurations/` — DELETED (folder renamed to `EntityConfigurations/`)

## Deviations from Plan

- Solution file is `Reflekt.slnx` not `Reflekt.sln` — .NET 10 tooling defaults to the new XML format. `dotnet build Reflekt.slnx` works identically.
- Mapster package versions updated to 10.0.0 (plan had 7.4.0/1.0.2 which no longer exist on NuGet)
- Scalar.AspNetCore updated from 2.5.2 to 2.5.3 (2.5.2 not available)
- Vue scaffold used `--ts --router --pinia` flags only (create-vue 3.22 removed `--no-vitest` etc.)
- `counter.ts` Pinia store from scaffold left in place (unused; will not affect any feature plans)
- Plan §17 said "no endpoints at Foundation" — added `HealthEndpoint` anyway because FastEndpoints 5.x throws `InvalidOperationException` at startup when no endpoint classes exist
