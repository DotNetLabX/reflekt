# C# Conventions

Programming conventions for C# / .NET specific to this codebase. Referenced by developer and reviewer agents.

## EF Core

### Navigation Loading
- `ThenInclude` chains continue from the last included navigation. Pattern: `Include(s => s.Memberships).ThenInclude(m => m.Ticket).ThenInclude(t => t.Assignee)`.
- `Contains` on a fetched `List<string>` translates to SQL `IN (...)` correctly. For very large sets this would need batching, but dataset is bounded.
- **Backing field + navigation requires both config and query changes.** When using a backing field for a collection navigation, configure `UsePropertyAccessMode(PropertyAccessMode.Field)` in the entity config AND use `.Include()` in queries. Forgetting either silently gives empty collections.

### Cross-Aggregate Queries
- For analytic queries that join across aggregates, add the query method to the repository that owns the primary data. Don't create a separate read-model query class until the pattern recurs.

### Migrations
- `dotnet ef migrations remove --force` works cleanly when the migration has not been applied to a database. No need to delete files manually.
- When running `dotnet ef migrations add` with FastEndpoints and no endpoints are registered yet, the host startup fails. Add `IDesignTimeDbContextFactory` in the Persistence project to bypass this.

### EF Core Gotchas
- **`HasData` + `ToJson()` are incompatible.** EF Core cannot seed entities via `HasData` when the entity (or an owned type on it) uses `ToJson()` column mapping. Use runtime seeding (`SeedTestData`) instead.
- **Collection expressions (`[]`) fail in expression tree lambdas.** EF Core translates lambdas to SQL via expression trees. C# 12 collection expressions are not representable as expression trees — use `new List<T>()` or `Array.Empty<T>()` explicitly.
- **Seed data with aggregates must clear domain events.** `DomainEventPublishingInterceptor` crashes when FastEndpoints is not yet initialized during seeding. All seeders creating aggregates must call `ClearDomainEvents()` before `SaveChangesAsync()`.
- `First` vs `FirstOrDefault` at service boundaries: prefer `FirstOrDefault` with null handling over `First` which throws an unhandled 500.
- `required int` on composite key members is more defensively correct than plain `int` — int defaults to 0 which can silently produce invalid FK records.

## Namespace Imports

- C# does not auto-import parent namespaces. When a class in `Fokus.API.Features.Sync.SyncSprints` consumes a class in `Fokus.API.Features.Sync`, an explicit `using` is required.
- `GlobalUsings.cs` should exist in all three service projects (API, Domain, Persistence), not just API. Each project has its own frequently-used namespaces.

## Error Handling in Handlers

- Inside `HandleAsync`, throw domain exceptions (`NotFoundException`, `BadRequestException`, etc.). The `GlobalExceptionMiddleware` maps them to HTTP responses.
- `AddError`/`SendErrorsAsync` is only for FluentValidation pre-handler validation, not for handler business logic errors.

## Method Overrides

- Use `new` keyword when a derived repository overrides a base method signature (e.g., `UpsertAsync` with entity-specific field-copy logic). This is intentional hiding, not an accident — suppress CS0108.

## Type Conventions

- Use `sealed record` for DTOs, value objects, and domain events.
- Use `required init` properties for entity creation when no business rules or domain events are involved.

## Endpoint Patterns

- **Read endpoints need existence checks too.** Any endpoint that takes a resource ID in the route must validate that the resource exists and return 404, regardless of read vs write.
- **Auth-before-lookup changes expected error codes.** When an endpoint checks auth claims against route params before DB lookup, the expected status for nonexistent resources is 401 (claim mismatch), not 404. Plan test cases must reflect actual handler flow order.
- **POST with route-only params returns 415 without Content-Type.** FastEndpoints requires `Content-Type: application/json` with an empty body (`{}`) even when the request model binds entirely from route parameters. Both backend tests and frontend stores must send this.
- **`PublishAsync<T>` uses compile-time generic, not runtime type.** When domain events are typed as `IEnumerable<IDomainEvent>`, the compile-time `T` is `IDomainEvent` — concrete handlers never fire. Use reflection-based `MakeGenericMethod` dispatch for polymorphic event publishing.

## Build (Windows)

- **MSBuild cache/DLL lock errors are not build failures.** `MSB3492` (cache lock) and `MSB3021`/`MSB3027` (DLL lock from running dev server) are transient file-locking issues. Filter for `CS[0-9]+` errors to determine real build status.

## Solution Format

- The solution file uses `.slnx` format (XML-based, .NET 10 default). Adding projects means adding `<Project Path="...">` entries under the appropriate `<Folder>` element.
