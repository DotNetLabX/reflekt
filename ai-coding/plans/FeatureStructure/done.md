# FeatureStructure — Done

## Step 1: Delete .gitkeep files
Removed all 10 `.gitkeep` files listed in the plan. Staged deletions via `git rm`.

## Step 2: Ensure `data/` directory at startup
- `Reflekt.Persistence/DependencyInjection.cs` — Added `SqliteConnectionStringBuilder` to extract `DataSource` path, then `Directory.CreateDirectory` on its parent directory. Placed outside the `IsDevelopment()` block so it runs in all environments. Added `using Microsoft.Data.Sqlite;`.

## Step 3: Restructure feature endpoints
All endpoints converted from `Configure()` override to attribute-based routing. Old flat files deleted.

- `Features/Auth/Login/LoginEndpoint.cs` — created; `[AllowAnonymous]`, `[HttpGet]`, `[Tags]`
- `Features/Auth/Logout/LogoutEndpoint.cs` — created; `[Authorize(Policy)]`, `[HttpPost]`, `[Tags]`
- `Features/Auth/Me/MeEndpoint.cs` — created; `[Authorize(Policy)]`, `[HttpGet]`, `[Tags]`; `MeResponse` record stays here
- `Features/Health/HealthEndpoint.cs` — rewritten in place; `[AllowAnonymous]`, `[HttpGet]`, `[Tags]`
- `Features/Teams/CreateTeam/CreateTeamEndpoint.cs` — created; endpoint class only
- `Features/Teams/CreateTeam/CreateTeamCommand.cs` — created; `CreateTeamCommand` + `CreateTeamResponse` + `CreateTeamCommandValidator`
- `Features/Teams/GetMyTeams/GetMyTeamsEndpoint.cs` — created; `TeamSummary` record stays here
- Deleted: `Auth/LoginEndpoint.cs`, `Auth/LogoutEndpoint.cs`, `Auth/MeEndpoint.cs`, `Teams/CreateTeamEndpoint.cs`, `Teams/CreateTeamValidator.cs`, `Teams/GetMyTeamsEndpoint.cs`

**Deviation:** Initially added `using Microsoft.AspNetCore.Mvc;` for `[HttpGet]`/`[HttpPost]` — caused CS0104 ambiguity with `FastEndpoints.HttpGetAttribute`. Removed the MVC using; FastEndpoints' own attributes resolve via global `using FastEndpoints;`.

## Step 4: Update tests
No changes needed — `TeamEndpointTests.cs` and `AuthSmokeTests.cs` use HTTP calls with anonymous objects, no direct imports of feature types.

## Step 5: Add guardrail to CLAUDE.md
Added `No .gitkeep files` rule to the Guardrails — DO NOT section.

## Step 6: Verify
- `dotnet build Reflekt.slnx` — 0 errors, 0 warnings
- `dotnet test` — 24 passed (13 unit + 11 integration)
- `git ls-files '*.gitkeep'` — returns nothing
