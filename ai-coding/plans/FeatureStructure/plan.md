# Feature Structure Refactor

## Context

Three structural deviations from the `create-feature` skill found across all endpoints:

1. Features are flat in `Features/{Domain}/` — skill says `Features/{Domain}/{FeatureName}/`
2. Endpoints use `Configure()` override — skill's primary pattern uses attributes
3. Request/response records live in the endpoint file, validator in its own file — skill says command + response + validator co-located in `{FeatureName}Command.cs`, endpoint file has endpoint class only

Additionally: `.gitkeep` files scattered throughout the repo for empty placeholder directories. Not needed — remove all.

## Skills referenced

- `create-feature` → `workflows/EndpointFastEndpoints.md` (folder structure, attribute pattern, file naming)
- `create-feature` → `workflows/Validator.md` (validator naming and co-location with command)

## Steps

### Step 1: Delete all .gitkeep files

Remove every `.gitkeep` in the repo:

- `data/.gitkeep`
- `src/Services/Reflekt/Reflekt.API/Events/.gitkeep`
- `src/Services/Reflekt/Reflekt.API/Features/.gitkeep`
- `src/Services/Reflekt/Reflekt.Domain/Cards/.gitkeep`
- `src/Services/Reflekt/Reflekt.Domain/Events/.gitkeep`
- `src/Services/Reflekt/Reflekt.Domain/Participants/.gitkeep`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/.gitkeep`
- `src/Services/Reflekt/Reflekt.Domain/Teams/.gitkeep`
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/.gitkeep`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/.gitkeep`

### Step 2: Ensure `data/` directory at startup

With `.gitkeep` removed, `data/` won't exist on fresh clones. In `UseReflektPersistence` (`Reflekt.Persistence/DependencyInjection.cs`), add `Directory.CreateDirectory` for the SQLite data folder before `EnsureCreated`/`Migrate`. Extract the path from the connection string or use a convention (`./data/`).

### Step 3: Restructure feature endpoints

Follow `create-feature` → `workflows/EndpointFastEndpoints.md` for all changes below.

**Type renames:**
- `CreateTeamRequest` → `CreateTeamCommand`
- `CreateTeamValidator` → `CreateTeamCommandValidator`

**Auth adaptation:** Reflekt uses named policies, not roles. Use `[Authorize(Policy = AuthPolicies.ScrumMaster)]` where the skill shows `[Authorize(Roles = ...)]`.

#### Target structure after refactor

```
Features/
├── Auth/
│   ├── Login/
│   │   └── LoginEndpoint.cs                — [AllowAnonymous]
│   ├── Logout/
│   │   └── LogoutEndpoint.cs               — [Authorize(Policy)]
│   └── Me/
│       └── MeEndpoint.cs                   — endpoint + MeResponse record
├── Health/
│   └── HealthEndpoint.cs                   — infrastructure, stays flat, attributes only
└── Teams/
    ├── CreateTeam/
    │   ├── CreateTeamEndpoint.cs            — endpoint class only
    │   └── CreateTeamCommand.cs             — CreateTeamCommand + CreateTeamResponse + CreateTeamCommandValidator
    └── GetMyTeams/
        └── GetMyTeamsEndpoint.cs            — endpoint + TeamSummary record
```

**Per endpoint:**
- Convert `Configure()` to attributes per the skill's primary pattern
- Move endpoint class into `Features/{Domain}/{FeatureName}/` subfolder
- For endpoints with a command: extract command + response records + validator into `{FeatureName}Command.cs`
- For endpoints without a command (Login, Logout, Me, GetMyTeams): response record (if any) stays in the endpoint file
- Delete all old files from the flat `Features/{Domain}/` folders

### Step 4: Update tests

Type and namespace renames will break existing tests:
- `CreateTeamRequest` → `CreateTeamCommand`
- Namespace `Reflekt.API.Features.Teams` → `Reflekt.API.Features.Teams.CreateTeam` / `.GetMyTeams`
- Namespace `Reflekt.API.Features.Auth` → `Reflekt.API.Features.Auth.Login` / `.Logout` / `.Me`

Files to check and update:
- `tests/Reflekt.IntegrationTests/TeamEndpointTests.cs`
- `tests/Reflekt.IntegrationTests/AuthSmokeTests.cs`

### Step 5: Add guardrail to CLAUDE.md

Add to the Guardrails — DO NOT section:
- **No `.gitkeep` files** — don't track empty directories. Directories are created at runtime or when files are added.

### Step 6: Verify

- `dotnet build Reflekt.slnx` — 0 errors
- `dotnet test` — all existing tests pass
- `git ls-files '*.gitkeep'` — returns nothing
