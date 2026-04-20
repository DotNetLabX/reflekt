# Teams Plan — Lessons

## Developer Lessons

### FastEndpoints `Config.ServiceResolver` is null during EF seed

`DomainEventPublishingInterceptor` calls `ev.PublishAsync()` which requires FastEndpoints to be initialized via `MapFastEndpoints()`. The seed runs in `UseReflektPersistence()` which is called before `UseReflektApi()` (which calls `MapFastEndpoints()`). Any aggregate created during seed that raises domain events will crash on save.

**Fix:** Call `aggregate.ClearDomainEvents()` before `SaveChangesAsync` in `DbInitializer`. Seed is infrastructure — event dispatch is not needed.

**Future pattern:** All seeders that create aggregates must clear domain events before saving.

### Registering a test auth handler under an existing scheme name throws

`AddScheme<T>(schemeName)` throws `InvalidOperationException` if a scheme with that name already exists. Do not try to replace `CookieAuthenticationDefaults.AuthenticationScheme` with a test handler in `ConfigureTestServices`.

**Fix:** Register the test handler under a distinct scheme name (e.g. `"Test"`), then re-register the relevant policy in `ConfigureTestServices` to use that scheme. `AddPolicy` in `AuthorizationOptions` replaces an existing policy of the same name — last registration wins.

### Two factories sharing the same SQLite file causes startup race

When two `WebApplicationFactory` instances both run `db.Database.Migrate()` on the same SQLite file and both `CreateClient()` are called in the same `InitializeAsync`, there's a potential locking conflict.

**Fix:** Give each factory its own DB path. For tests that only need 401 (no DB state), a separate path is harmless.

### `appsettings.Development.json` is loaded by the test host

`WebApplicationFactory` runs the app in `Development` environment by default, which loads `appsettings.Development.json`. If that file enables seeding or contains other environment-specific config that conflicts with test expectations, the test factory must explicitly override those keys in `ConfigureAppConfiguration`.

**Fix:** Always override `Seed:Enabled = "false"` in integration test factories.

### `ConfigureTestServices` requires `using Microsoft.AspNetCore.TestHost`

The extension method is in `Microsoft.AspNetCore.TestHost`, not in `Microsoft.AspNetCore.Mvc.Testing` directly. Add the using explicitly.

### Skills reference BuildingBlocks types that don't exist yet — build them, don't work around them

`create-aggregate`, `persistence-patterns`, and `domain-patterns` skills reference BuildingBlocks types not yet in Reflekt:

- `AuditedEntityConfiguration<T>` — needed by EF config skill pattern
- `Repository<T>` base — needed by repository skill pattern
- `IDomainEvent` — Reflekt uses FastEndpoints `IEvent`; skills reference `IDomainEvent`

The correct approach: build what the skill requires, not work around it. Skills define what the codebase should have. These types should be created in BuildingBlocks as part of following the skill. Previous approach ("signal and don't change") was wrong — it turned the Developer into a reporter instead of a builder.

### Feature folder structure: skill vs existing Reflekt pattern

`EndpointFastEndpoints.md` specifies `Features/{Domain}/{FeatureName}/` (per-feature subfolder). Existing Reflekt code (`MeEndpoint.cs`, `LogoutEndpoint.cs`) uses flat `Features/{Domain}/`. Until the Architect updates the skill or the existing code, follow the flat pattern to stay consistent with the codebase.

### `AuditedEntityConfiguration<T>` does not map to Reflekt — create `EntityConfiguration<T>` instead

The skill references `AuditedEntityConfiguration<T>` which assumes audit fields (`CreatedById`, `CreatedOn`, etc.) on all aggregates. Reflekt's `AggregateRoot` has no audit fields. Build `EntityConfiguration<T>` as the equivalent base: `HasKey + ValueGeneratedOnAdd`. Signal this to the Architect so the skill can note the Reflekt variant.

### Full `RepositoryBase` surface is out of scope for a single round — build minimal, note what's deferred

`persistence-patterns` defines a comprehensive repository with CRUD, batch, and extension methods. Build the methods the current feature actually needs (`AddAsync`, `SaveChangesAsync`, `GetByIdAsync`). Log what's missing in lessons so it can be completed incrementally.

## Architect Lessons

### Domain event ID capture requires an explicit pattern decision upfront

The plan initially recommended omitting the aggregate ID from events because `int` auto-increment IDs are `0` at raise time. This punted the problem. The correct pattern (events hold the aggregate reference, dispatched post-save so ID is populated) should have been identified and specified from the start — not left as an open question. Now baked into CLAUDE.md `## Domain` section.

### Seed + domain event interceptor ordering is a plan-level concern

The Developer discovered that `DbInitializer` runs during `UseReflektPersistence`, before `MapFastEndpoints` initializes `Config.ServiceResolver`. The plan should have flagged this: "Seed creates aggregates → aggregates raise events → interceptor tries `PublishAsync` → FastEndpoints not initialized → crash." The fix (`ClearDomainEvents` before save in seeders) is correct, but the Architect should anticipate startup ordering conflicts when seed data involves aggregates.

### Plans should specify SaveChangesAsync ownership explicitly, not defer it

The original plan said "Developer decides" for whether the repository or endpoint calls `SaveChangesAsync`. This caused unnecessary ambiguity. Convention decisions should be made in the plan and codified in CLAUDE.md, not left to individual Developer judgment. Now resolved: repositories wrap `SaveChangesAsync`.

### Global EF ignores belong in OnModelCreating, not per-entity configs

Initially placed the `DomainEvents` ignore in `TeamConfiguration`. Corrected to a global loop in `OnModelCreating` — one-time setup, no per-entity boilerplate. Future plans should default to global conventions for base-class properties.

### Inlined plan snippets bypass skills — the Architect caused the skill-skip problem

The plan inlined the aggregate shape, EF config pattern, and repository structure — duplicating what `create-aggregate` skill already defines. The Developer followed the plan's inline code directly and never read the skill. Result: partial class behavior split was missed, folder structure diverged, property style differed (`private set` vs `required init`). The fix is structural: when a skill exists for a step, the plan must reference it by name and provide only feature-specific inputs — never inline the pattern. Now baked into Architect.md "Plan writing rules."

### Skills are authoritative — build what they require, don't signal and stop

Skills are cross-project templates. When a skill references a base class (e.g., `AuditedEntityConfiguration<T>`, `Repository<T>`) that doesn't exist in the current codebase, the Developer builds it — the skill defines what should exist. Escalate only if the scope is too large. The original "signal the gap" rule was wrong — it blocked the Developer from doing their job. Now baked into agents-workflow.md "Skill Authority."

### Missing skills should be logged, not silently inlined

When no matching skill exists, the Architect may inline snippets as a one-time guide. But the missing skill must be logged in lessons (what it would be named, what it should cover, which files were used as reference) so it can be created later. Now baked into Architect.md "Plan writing rules."
