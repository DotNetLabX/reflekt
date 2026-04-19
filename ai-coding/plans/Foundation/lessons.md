# Foundation — Lessons

## Developer Lessons

- **`.slnx` is the new default in .NET 10**: `dotnet new sln` creates `.slnx` (XML format), not `.sln`. The `dotnet` CLI accepts both. Plans should reference `Reflekt.slnx` going forward, or accept either extension.

- **`FastEndpoints.IMapper` conflicts with `MapsterMapper.IMapper`**: FastEndpoints 5.x exports its own `IMapper` interface. When using Mapster alongside FastEndpoints, always qualify as `MapsterMapper.IMapper` and `MapsterMapper.ServiceMapper` in DI registration.

- **Package versions drift**: Several versions in `Directory.Packages.props` were stale at scaffold time:
  - `Mapster` 7.4.0 → 10.0.0; `Mapster.DependencyInjection` 1.0.2 → 10.0.0 (major version bump; now aligned)
  - `Scalar.AspNetCore` 2.5.2 → 2.5.3
  - Always run a restore before coding to catch version mismatches early.

- **`create-vue` CLI flags changed**: `create-vue@3.22` removed `--no-vitest`, `--no-eslint`, `--no-prettier` flags. To scaffold with only TypeScript + Router + Pinia, use `npm create vue@latest <name> -- --ts --router --pinia`.

- **`Reflekt.Persistence` classlib needs `<FrameworkReference Include="Microsoft.AspNetCore.App" />`** to access `WebApplication` and `IsDevelopment()`. This is the correct pattern for classlibs that host ASP.NET Core extension methods.

- **`AngleSharp.IConfiguration` vs `Microsoft.Extensions.Configuration.IConfiguration` name collision**: Both share the `IConfiguration` name. In a web project with implicit usings, avoid using the bare name; use `Configuration.Default` (resolves to `AngleSharp.Configuration`) and let the compiler infer types.

- **Tailwind CSS v4 setup for Vite**: Use `@tailwindcss/vite` plugin + `@import "tailwindcss"` in CSS. No `tailwind.config.js` or `postcss.config.js` needed.

- **FastEndpoints 5.x requires at least one endpoint class**: `AddFastEndpoints()` scans assemblies at startup and throws `InvalidOperationException` if no `IEndpoint` implementations are found. A minimal `HealthEndpoint : EndpointWithoutRequest` (anonymous, `GET /health`) is sufficient to unblock startup and integration tests. Plans that describe "no feature endpoints at Foundation" must still include this infrastructure endpoint.

- **`global using FastEndpoints` belongs in `GlobalUsings.cs`**: All endpoint classes use `EndpointWithoutRequest`, `Endpoint<TReq>`, etc. from the `FastEndpoints` namespace. Adding it as a global using avoids per-file imports across every feature slice.

- **Check `.claude/skills/` before each plan step**: A `create-service` skill exists with workflows for scaffolding folders, csproj files, and all three service projects — it was skipped during Foundation because the skills directory wasn't scanned first. Before starting any plan step, check whether a matching skill exists and follow it.

- **SQLite connection pooling outlives `WebApplicationFactory.DisposeAsync()`**: After disposing the factory, the SQLite connection pool keeps the `.db` file locked. `Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools()` releases all pooled connections. Call it after `DisposeAsync()` and before `File.Delete()` in integration test cleanup. Requires `Microsoft.Data.Sqlite` as a direct package reference (not just transitive) so the type is accessible in test source code.

## Architect Lessons

- **Don't over-specify sequences that are semantically equivalent.** The Foundation plan said "publish then clear" for `DomainEventPublishingInterceptor`; the Developer wrote "clear then publish," which is identical under `Mode.WaitForNone` because events are already captured into a local list. Plans should describe invariants (events are captured before any mutation; clear happens once) rather than micro-ordering when the outcome is the same.

- **`Microsoft.EntityFrameworkCore.Design` belongs on the startup project, not the migrated project.** The `dotnet ef` CLI resolves Design from the `-s` (startup) project. The Persistence project doesn't strictly need it. Visual Studio / `dotnet ef` templates may auto-add it to the migrated project as well — harmless but redundant. Plan should call out startup-project placement explicitly.

- **FastEndpoints 5.x requires ≥1 endpoint at startup.** Any Foundation/scaffolding plan that uses FastEndpoints must include a minimal infrastructure endpoint (e.g., `GET /health`) or the app will throw `InvalidOperationException` before the first feature can land. The "no feature code at Foundation" rule should explicitly exempt this kind of startup-infrastructure endpoint.

- **Default to `int` PKs unless URL opacity is load-bearing.** Guid was chosen initially on the (wrong) premise that unguessable URLs were a security boundary. In this system, auth (guest token in v1, login in v2) is the real boundary — `int` gives readable URLs, URL-hackable dev navigation, smaller indexes, and domain-layer ID assignment via `SaveChanges`. Revisit only if a future feature genuinely exposes non-authenticated ID-keyed URLs.

- **Separate the "run a real thing" driver from the "demo" driver.** This codebase doubles as course material, but technical decisions should be ranked by what helps the real-retro use case. If the only justification for a choice is the demo, flag it as such instead of folding it into the recommendation.

- **Review cycle: plan updates batch at the end of Step 1, not mid-review.** When the Developer flags a sensible deviation (e.g., HealthEndpoint), ratify it by updating the plan *before* Step 2 code reading — that way Step 2 compares code against the updated contract, not a stale one.
