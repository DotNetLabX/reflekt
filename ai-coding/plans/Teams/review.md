# Teams — Review

## Step 1: Done check — PASS

All 11 plan steps have corresponding done.md entries. One minor doc gap: Step 4 (EF migration) not logged in done.md, but `Migrations/20260419224520_AddTeam.cs` exists. Five deviations reported, all reasonable.

## Step 2: Code review — PASS

Every plan instruction was verified against the implementation. No blocking issues.

### File-by-file verification

| File | Plan match | Notes |
|---|---|---|
| `Team.cs` | Exact | Sealed class, extends `AggregateRoot`, private ctor, `Create` factory with `ThrowIfNullOrWhiteSpace` guards, `AddDomainEvent(new TeamCreatedEvent(team))`, `DateTime.UtcNow`. |
| `TeamCreatedEvent.cs` | Exact | `sealed record TeamCreatedEvent(Team Team) : IEvent` |
| `TeamConfiguration.cs` | Exact | `ToTable`, max lengths, unique index on `CreatorEmail`, `CreatedAt` required. Internal sealed. |
| `ReflektDbContext.cs` | Exact | `DbSet<Team> Teams`, global `DomainEvents` ignore loop before `ApplyConfigurationsFromAssembly`. |
| `TeamRepository.cs` | Exact | All 5 methods present: `AddAsync`, `SaveChangesAsync` (wrapper), `GetByCreatorEmailAsync` (NOCASE collate), `ExistsByCreatorEmailAsync` (NOCASE collate), `GetByIdAsync`. Concrete class, no interface. |
| `DependencyInjection.cs` | Exact | `AddScoped<TeamRepository>()`, `app.Configuration` passed to `SeedAsync`. |
| `DbInitializer.cs` | Match + deviation | Reads `AllowedEmails`, checks empty, idempotent `!AnyAsync`. Deviation: `ClearDomainEvents()` before save — justified (FastEndpoints not initialized at seed time). |
| `CreateTeamEndpoint.cs` | Exact | POST `/api/teams`, ScrumMaster policy (alias pattern), handler flow matches plan 1-6: email extraction → allowlist check → exists check (409) → Create → AddAsync + SaveChangesAsync → 201 response. |
| `CreateTeamValidator.cs` | Exact | `NotEmpty().MaximumLength(100)` on Name. |
| `GetMyTeamsEndpoint.cs` | Exact | GET `/api/teams`, ScrumMaster policy, TeamSummary record, email → query → map → SendOkAsync. |
| `teams.ts` | Exact | State/getters/actions match plan. `credentials: 'include'` on both fetches. `createTeam` awaits response, pushes to array on success, throws on error. |
| `WelcomeView.vue` | Exact | Three states implemented. Watcher on `authStore.loaded` triggers `fetchMyTeams`. Client-side trim + empty check. Creating/error state. |
| `TeamTests.cs` | Exceeds | 4 test methods covering all plan cases. Theory with InlineData for guard clauses (empty + whitespace). `BeSameAs` assertion on event aggregate reference. |
| `TestAuthHandler.cs` | Match + deviation | Own "Test" scheme (not Cookie) — justified (duplicate scheme registration throws). Static email/name properties. |
| `TeamEndpointTests.cs` | Match + deviation | 7 test cases matching plan exactly. Two factories with separate DBs — justified (SQLite locking). Seed disabled — justified (state pollution). IAsyncLifetime with ClearAllPools + file delete. |

### Patterns and skills followed

- `create-aggregate` skill: aggregate shape follows DDD conventions (sealed, private ctor, factory method, event raising). ✅
- `create-feature` skill: endpoint shape follows FastEndpoints vertical slice (request + response in file, validator sibling, policy alias pattern). ✅
- `persistence-patterns` skill: EF config in `Configurations/`, concrete repository in `Repositories/`, scoped DI registration. ✅
- `MeEndpoint.cs` pattern reference: same alias pattern (`AuthPolicies`), response record in file, policy in Configure. ✅

### Null safety

- `CreateTeamEndpoint`: `email` from `FindFirstValue` could be null. `AllowlistChecker.IsAllowed` handles null (returns false → SendForbidden before null-forgiving `email!` is reached). Safe. ✅
- `GetMyTeamsEndpoint`: `email!` justified — `ScrumMaster` policy guarantees authenticated cookie user with email claim. ✅

### Minor observations (not blocking)

1. **WelcomeView flash of create form.** When signed in, `teamsStore.loaded` is not checked — the create form shows briefly before `fetchMyTeams` completes. Acceptable per plan judgment call ("keep it minimal"). Not a bug, just a UX polish item for later.
2. **Done.md omits migration step.** Migration exists and is correct. Documentation gap only.

### Guardrails check

- No service layer classes ✅
- No repository interfaces ✅
- No god folders ✅
- Domain rules in aggregate (guards), not endpoints ✅
- Program.cs unchanged ✅
- No new auth schemes or policies in production code ✅

## Verdict: APPROVED

Clean implementation. All plan instructions followed. Deviations documented and justified.
