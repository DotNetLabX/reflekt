# C# Conventions

Programming conventions for C# / .NET specific to this codebase. Referenced by developer, reviewer, and solo agents via `@`.

## Namespace Imports

- C# does not auto-import parent namespaces. When a class in `Reflekt.API.Features.Sprints.CreateSprint` consumes a class in `Reflekt.API.Features.Sprints`, an explicit `using` is required.
- `GlobalUsings.cs` should exist in all three service projects (API, Domain, Persistence), not just API. Each project has its own frequently-used namespaces.

## Error Handling in Handlers

- Inside `HandleAsync`, throw: `DomainException` for a refused business rule, and the `Blocks.Exceptions` family (`NotFoundException` — usually through `Guard.NotFound` —, `ForbiddenException`, `ConflictException`, `BadRequestException`, `UnauthorizedException`) for the rest. The `GlobalExceptionMiddleware` maps them to HTTP responses.
- `AddError`/`SendErrorsAsync` is only for FluentValidation pre-handler validation, not for handler business logic errors.

## Method Overrides

- A repository that changes a base query overrides the virtual base method (`CardRepository` overrides `GetByIdAsync` to include `EvidenceLinks`). Use the `new` keyword only to hide a non-virtual base member on purpose — then it is intentional hiding, not an accident; suppress CS0108.

## Type Conventions

- Use `sealed record` for DTOs, value objects, and domain events.
- Use `required init` properties for entity creation when no business rules or domain events are involved.

## Solution Format

- The solution file uses `.slnx` format (XML-based, .NET 10 default). Adding projects means adding `<Project Path="...">` entries under the appropriate `<Folder>` element.

---

## Method Design

- **~60 lines max per method.** Decompose at natural seams (validation, data construction, persistence, notification).
- **One level of abstraction per method.** Don't mix high-level orchestration with low-level field mapping.
- **Guard clauses first, happy path last.** No deep nesting — early returns flatten the structure.
- **Plan sub-steps ≠ one method.** Never mirror plan structure literally in code. A plan step with 5+ sub-operations = multiple private methods.

## Decomposition Judgment

- **Extract when:** the block has a clear name, is >15 lines, or is called from a loop body.
- **Don't extract when:** single-call, <10 lines, trivial try/catch, or would need 5+ parameters passed.
- **Prefer pure helpers over side-effectful helpers.** Separate data construction (returns data) from persistence (calls repos). Builder methods are testable and readable.
- **Rule of three for DRY.** Don't extract a helper for 2 call sites. Wait for 3.
- **Named records for 3+ field returns** — `ReadinessTracker.ToggleReady` returns `ReadinessToggle(IsReady, ReadyCount, TotalCount)`. Tuples only for 2-field returns — `EvidenceLinkSourceDetector.Detect` returns `(EvidenceLinkSource Source, string? ExternalKey)`.

## Async / Await

- **Never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`** — blocks thread, causes deadlocks.
- **Never `async void`** outside event handlers — unhandled exceptions crash the process.
- **Always propagate `CancellationToken`** as the last parameter (`CardRepository.GetBySprintIdAsync(int sprintId, CancellationToken ct)`). Pass it to every awaitable call downstream.
- **`throw;` not `throw ex;`** — preserves the original stack trace (`OpenGraphConfiguration`'s connect callback disposes the socket, then `throw;`).
- **Use `ct.ThrowIfCancellationRequested()`** — don't silently return on cancellation.

## EF Core

- **`.AsNoTracking()` on all read-only queries.** GET endpoints, reports, background reads — `CardRepository.GetBySprintIdReadOnlyAsync`, `PhaseActionRepository`.
- **Never access navigation properties in a loop without `.Include()`.** This is N+1 (`CardRepository.GetBySprintIdAsync` includes `Card.EvidenceLinks`).
- **`.AsSplitQuery()` when including 2+ collection navigations** at the same level — avoids Cartesian explosion. No query needs it today: `Card.EvidenceLinks` is the only collection navigation.
- **Project with `.Select()` at the database layer** — don't load entire entities and map afterward (`TeamRepository` projects teams straight into `TeamSummaryResponse`).
- **Don't use EF for bulk operations in loops.** Use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` (EF Core 7+) for bulk work — `TeamRepository`'s team delete removes votes, evidence links, cards, groups, participants and sprints with `ExecuteDeleteAsync`.
- **Don't return `IQueryable<T>` from methods.** Compose all query predicates in one place, then materialize.

## LINQ

- **`.Any()` over `.Count() > 0`** — `Any()` short-circuits; on EF it generates `EXISTS` vs `COUNT(*)` (`TeamRepository.ExistsByOwnerAndNameAsync` uses `AnyAsync`).
- **`.ToList()` before iterating twice.** `IEnumerable<T>` from a LINQ chain re-executes on each enumeration.
- **No LINQ for side-effectful operations.** Use `foreach` for upserts, API calls, anything with side effects.
- **Verify generated SQL when using `GroupBy`** on `IQueryable` — it can pull all rows to the client. `VoteRepository`'s vote counts group by `GroupId` and select only the key and `Count()`, which translates to SQL.

## Error Handling

- **Don't catch `Exception` without a filter or re-throw.** Swallowing the base type hides bugs.
- **Use exception filters** (`catch (HttpException ex) when (ex.StatusCode == ...)`) to narrow catches.
- **"Not found" is an exception, not a result.** A missing entity, a refused rule or a forbidden call throws (`Guard.NotFound` → `NotFoundException`, `DomainException`, `ForbiddenException`), and the middleware maps it — see Error Handling in Handlers above. Request-shape validation is the validator's job; FastEndpoints runs it before the handler.
- **Don't use exceptions for outcomes that are not errors.** A check the client asks for returns its answer as data: `AdvancePhaseCheck` returns the counts in `AdvancePhaseCheckResponse`, `PhaseActionPolicy.IsAllowed` returns a `bool` (`EnsureAllowed` is the throwing form).
- **Order catch blocks most-derived first.** Otherwise specific handlers are unreachable.
- **Centralize unhandled exceptions via middleware** — endpoints let exceptions propagate, not catch-all.

## Null Handling

- **`ArgumentNullException.ThrowIfNull(param)`** at the top of public methods — shorter, analyzer-recognized (`OpenGraphAddressPolicy.IsAllowed` starts with `ArgumentNullException.ThrowIfNull(address)`).
- **`is not null` over `!= null`** — works correctly with overloaded equality operators (`sprint is not null` in `SprintHub`).
- **Don't use `?.` as a silent suppressor** when you expect the value to always exist. Assert or throw.
- **Optional navigation properties are explicitly `?`** — required ones use `required` or constructor.

## Return Types

- **Return `IReadOnlyList<T>` from public methods** that return materialized collections — not `List<T>` (`Card.EvidenceLinks` is an `IReadOnlyList<EvidenceLink>`).
- **Accept `IEnumerable<T>` or `IReadOnlyList<T>` as parameters** — not `List<T>` (`VoteRepository.GetVoteCountsByGroupIdsAsync(IEnumerable<int> groupIds, …)`).
- **`Task<IReadOnlyList<T>>` over `Task<IEnumerable<T>>`** for async query methods — the data is already materialized (`CardRepository.GetBySprintIdAsync` returns `Task<IReadOnlyList<Card>>`).

## Records

- **Use `record` for:** DTOs, commands/queries, domain events, value objects.
- **Never use `record` for EF Core entities** — EF relies on reference equality and mutable state.
- **No `set` accessors on record properties** — use `init` if post-construction initialization needed.
- **`IReadOnlyList<T>` for collection properties on records** — shallow immutability doesn't protect `List<T>` contents (`ReadinessSnapshot(IReadOnlyList<int> ReadyParticipantIds, …)`).
- **Prefer non-positional `init` properties when extending positional records.** Adding a new positional parameter breaks all existing construction sites. Add an `init` property in the record body instead — existing call sites compile unchanged. `GlobalExceptionMiddleware.ErrorResponse(StatusCode, Message, TraceId)` carries `Code`, `Errors` and `Details` that way (`public string? Code { get; init; }`).

## Naming by Intent

- **Methods = verb phrase.** If no verb fits, the method has unclear scope — split it.
- **Consistent verb prefixes:** `Get` (sync read), `Find` (may return null), `Build` (pure, returns data), `Sync`/`Persist` (side-effectful), `Extract`/`Parse` (static, transforms input).
- **Boolean = affirmative prefix:** `IsRemoved` (Participant), `IsOwner`, `HasTimer`. Never `NotRemoved`.
- **Events = past tense verb phrase:** `PhaseAdvanced`, `SprintClosed` (records `PhaseAdvancedEvent`, `SprintClosedEvent`). Never noun-only (`SprintEvent`).
- **Don't use:** `data`, `info`, `result`, `temp`, `obj`, `item` as variable names. Name by semantic content.

## Modern C# Idioms

- **Collection expressions `[]`** for empty or inline collections (`private readonly List<EvidenceLink> _evidenceLinks = [];` in `Card`).
- **`var` when RHS makes type obvious.** Explicit type when return type is unclear from method name.
- **File-scoped namespaces** (`namespace Reflekt.Domain.Cards;`) — not the braced block form.
- **Declaration-form `using`** (`using var scope = scopeFactory.CreateScope();`, as in `BroadcastGroupCreatedHandler`) — not the block form when scope is rest of method.
- **Pattern matching** (`is`, `switch` expressions) over `is` + cast pairs (`minutes is not (1 or 2 or 5)` in `SprintHub.AddTime`; the phase `switch` in `Sprint.AdvancePhase`).
- **`??` coalescing** for single-fallback (`participant.UserId ?? await FindCookieUserIdAsync()` in `SprintHub`). Multi-step fallback gets its own method.
- **`ArgumentOutOfRangeException.ThrowIfLessThan`** and related static helpers (C# 11+) — not manual guard boilerplate (`Sprint.Create` checks `votesPerParticipant` this way).

## Domain Model (DDD reinforcements)

- **No public setters on entities.** State changes go through intention-named methods.
- **Collection navigation properties exposed as `IReadOnlyList<T>`** — never the internal `List<T>`.
- **Cross-aggregate references by ID only** — never navigation property.
- **Domain events raised after state transitions succeed** — they represent facts, not intents.
- **No database queries inside entity methods.** Queries belong in handlers or repositories.
