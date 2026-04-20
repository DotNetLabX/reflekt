# Participants — Lessons

## Developer Lessons

### Generic alias constraint must use the generic base, not the alias class

When making `Entity` generic and adding a non-generic alias `class Entity : Entity<int>`, constraints on repositories and configurations must use `Entity<int>` (not `Entity`). Existing aggregates inherit through `AggregateRoot<int> : Entity<int>`, not through the non-generic alias class. Using `where TEntity : Entity` (alias) would reject Sprint and Team with CS0311.

### `DomainEventPublishingInterceptor`: use `OfType<IHasDomainEvents>()` after any generic refactor of AggregateRoot

After making `AggregateRoot` generic, `Entries<AggregateRoot>()` only captures `AggregateRoot<int>` — not `AggregateRoot<Guid>` or future key types. Switching to `Entries().Select(e => e.Entity).OfType<IHasDomainEvents>()` makes the interceptor key-type-agnostic.

### Auth handler constructor injection with scoped services works in ASP.NET Core

`GuestTokenAuthenticationHandler` successfully injects `ParticipantRepository` (scoped) via constructor. ASP.NET Core instantiates auth handlers per-request via `Context.RequestServices`, so scoped services resolve correctly. No need for `Context.RequestServices.GetRequiredService<T>()` workaround.

### Integration test factory: GuestToken handler survives TestAuthHandler override

In `ConfigureTestServices`, calling `services.AddAuthentication(TestAuthHandler.SchemeName).AddScheme<TestAuthHandler>(...)` only changes the default scheme — it does NOT remove existing scheme registrations (including `GuestTokenAuthenticationHandler`). The `Participant` policy (which specifies `GuestTokenDefaults.AuthenticationScheme`) continues to work without being overridden.

### Detect auth type in endpoint via presence of a scheme-specific claim

In multi-scheme endpoints (`OwnerOrScrumMaster`), distinguish guest token vs SM by checking `User.FindFirstValue("participantId") is not null`. This is cleaner than checking `User.Identity?.AuthenticationType` and avoids coupling to scheme string constants.

### Seed data consistency: start sprint before seeding participants

The seed initializer was extended to start the sprint before adding participants. This deviates slightly from the plan (which only said "create participants"), but is justified: business rules state only Active sprints accept participants, so seeding participants for a Draft sprint produces inconsistent data.

### Missing skills encountered during implementation

Three skill gaps found during this feature:

1. **No skill for BuildingBlocks infrastructure refactoring.** Step 1 (generic ID refactor) had no applicable skill. The constraint subtlety (`Entity<int>` vs `Entity` alias) caused a build failure that a skill could have prevented. Candidate skill: `refactor-base-classes` or documented as a section in `domain-patterns`.

2. **No skill for integration test factory multi-scheme auth setup.** The pattern of coexisting TestAuthHandler + real GuestTokenHandler in `WebApplicationFactory` is not covered anywhere. It will recur every time a new auth scheme is added. Candidate skill: `integration-test-auth` or a section in `authorization-patterns`.

3. **`authorization-patterns` skill was wrong scope.** The skill describes a role-gate + resource-gate pattern from a different architecture (not applicable to Reflekt's policy-based setup). Reading it yielded nothing useful — the existing `AuthorizationConfiguration.cs` and the plan were the real guide. The skill should be updated or the Reflekt-specific pattern documented separately.

## Architect Lessons

### Non-generic alias constraints: use the parameterized base, not the alias class

Plan specified `where TEntity : Entity` for non-generic repository/configuration aliases. Wrong — existing aggregates inherit `AggregateRoot<int> : Entity<int>`, bypassing the `Entity` alias class entirely. Correct constraint is `Entity<int>`. When writing plans that introduce generic base classes with non-generic aliases, trace the full inheritance chain of existing types before specifying constraints.

### Read endpoints need existence checks too, not just write endpoints

Spec cross-check caught that `GetParticipants` was missing a sprint existence check — the plan only had it on write endpoints (JoinSprint). Any endpoint that takes a resource ID in the route should validate that resource exists and return 404, regardless of whether it's a read or write operation. Add this as a default instruction in future plans.
