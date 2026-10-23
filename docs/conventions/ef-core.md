# EF Core Conventions

Programming conventions for EF Core specific to this codebase. Referenced by developer and reviewer agents.

## Navigation Loading

- Aggregates reference each other by id only, so the only navigation is an aggregate's own child collection: `Card.EvidenceLinks`. The `CardRepository` reads whose callers use the links include it — `GetByIdAsync`, `GetBySprintIdAsync` and `GetBySprintIdReadOnlyAsync`: `Db.Cards.Include(c => c.EvidenceLinks)`. The two previous-action reads, `GetUnreviewedActionItemsBySprintIdAsync` and `GetActionItemsReviewedInSprintAsync`, do not include it: their cards come back with an empty `EvidenceLinks`, and their callers (`GetPreviousActionsEndpoint`, `SprintReportBuilder`) do not read the links.
- `ThenInclude` chains continue from the last included navigation. No aggregate here needs one today; add one only when a child gets a navigation of its own.
- `Contains` on a materialized collection translates to SQL `IN (...)` / `NOT IN (...)` correctly — for example `!excludedCardIds.Contains(c.Id)` in `CardRepository.HasCardsInGroupAsync`. For very large sets this would need batching, but the Reflekt dataset is bounded.
- **Missing `Include` causes a silent empty collection — not a runtime error.** If a query loads `Card` without `.Include(c => c.EvidenceLinks)`, `card.EvidenceLinks` is empty: the broadcast, the report or the regroup silently works without the links. Always include the children the downstream code reads.

## Cross-Aggregate Queries

- For queries that read across aggregates, add the query method to the repository that owns the primary data (e.g., `CardRepository.GetActionItemsReviewedInSprintAsync` for the previous-actions review, which reads cards by the sprint that reviewed them). Don't create a separate read-model query class until the pattern recurs.

## Migrations

- Commands, from the repository root, and the `dotnet-ef` install line: root `CLAUDE.md` § EF Core Migrations.
- `dotnet ef migrations remove --force` works cleanly when the migration has not been applied to a database. No need to delete files manually.
- **No `IDesignTimeDbContextFactory` is needed.** `dotnet ef` takes `ReflektDbContext` from the app's own host (`Program.cs`), which it builds without starting: no Google credentials are needed, and no start-up migration or seeding runs. The host's working directory is `src/Services/Reflekt/Reflekt.API`, so the relative `Data Source=./data/reflekt.db` resolves there (recorded run: `docs/specs/F40-ArchitectureDocs/delivery/implementation.md` § Step 2).
- The app applies pending migrations on every start (`MigrateAndSeedAsync` → `Database.MigrateAsync`), so `dotnet ef database update` is rarely needed.

## Migration Defaults

- **Verify `defaultValue` in `AddColumn` migrations matches the entity property initializer.** EF Core scaffolds the CLR default (`false` for `bool`, `0` for `int`, `null` for `string`) unless `HasDefaultValue` is configured. This is almost always wrong for columns with a meaningful default: `Sprint.Column1Name` starts as `"Went Well"` and `Card.ReviewStatus` as `ReviewStatus.None`, so `SprintConfiguration` sets `HasDefaultValue("Went Well")` and `CardConfiguration` sets `HasDefaultValue(ReviewStatus.None)`. Compare every new `AddColumn<T>` migration default against both the C# property initializer and the value the aggregate's factory sets. If they differ, set `HasDefaultValue(...)` in the entity configuration so EF generates the correct default, then regenerate or patch the migration.

## Additional Gotchas

- `First` vs `FirstOrDefault` at service boundaries: `First` throws `InvalidOperationException`, which the middleware turns into a 500. Use `FirstOrDefaultAsync` and pass the result to `Guard.NotFound` (→ `NotFoundException`, 404), as `JoinSprintEndpoint` does with `Guard.NotFound(await sprintRepository.GetByIdAsync(…))`.
- `required` on foreign-key and key members is more defensively correct than a plain `int` — an `int` defaults to 0, which can silently produce invalid FK records (`Card.SprintId` is `required int`).
- `HasData` + `ToJson()` are incompatible. EF Core cannot seed entities via `HasData` when the entity (or an owned type on it) uses `ToJson()` column mapping. `PhaseAction`'s rows use `HasData` (no JSON column); seed anything JSON-mapped at runtime (`DbInitializer.SeedAsync`).
- Collection expressions (`[]`) fail in expression tree lambdas. EF Core translates lambdas to SQL via expression trees. C# 12 collection expressions are not representable as expression trees — build the collection outside the query (as `excludedCardIds` is passed into `HasCardsInGroupAsync`) or use `new List<int>()` / `Array.Empty<int>()` explicitly.
