# Team Management — Lessons

## Developer Lessons

- **EF migration ordering is not data-aware.** When a migration involves data transformation (seeding rows, copying foreign keys), EF generates schema changes in schema order — not safe execution order. Always hand-write the migration body when data migration is required; use `migrationBuilder.Sql()` for the custom SQL and verify the order manually before running.

- **SQLite table rebuild pattern for column removal.** SQLite does not support `ALTER TABLE DROP COLUMN` or `ALTER TABLE ALTER COLUMN`. Removing or changing a column requires: CREATE new table with new schema → INSERT from old → DROP old → RENAME new. EF Core generates this pattern automatically when the migration is correct, but when hand-writing, you must emit it explicitly.

- **`required` + `private set` is illegal in C#.** C# 11+ `required` members must be settable by the initializer — combining `required` with `private set` is a compile error (CS9032). For aggregates that need private mutation after construction, use `= default!` as a sentinel on the backing property and ensure the factory method (inside the class) sets the value.

- **Dual-auth endpoints need both claim namespaces.** Some endpoints accept both Cookie auth (ScrumMaster) and GuestToken auth (Participant). These call `FindFirstValue("participantId")` from `System.Security.Claims` AND `GetUserId()` from the local extension. Removing `using System.Security.Claims` from these files causes CS1061. Always grep for `FindFirstValue` before removing the using.

- **Migration round-trip: generate then rewrite.** The recommended workflow for complex data migrations: run `dotnet ef migrations add` to scaffold the migration files (gets the Designer file and snapshot correct), then rewrite only the `Up()` and `Down()` methods with the correct SQL. Do not hand-create the migration file from scratch — the snapshot and designer files must be EF-generated.

- **`TeamSummaryDto` belongs in the repository file.** Projection DTOs used only by a single repository method should live in the same file as the repository, not in a separate DTO folder. This keeps the persistence boundary self-contained and avoids proliferating small files.

## Skill Gaps

- **Missing skill:** Step 5 (wire User creation into auth callback) had no skill. Needed a pattern for `OnGoogleTicketReceived` + find-or-create + claim injection. Suggested skill name: `auth-callback-enrichment`. Reference files: `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs`.

- **Missing skill:** Step 9 (bulk ownership check migration across 20+ endpoints) had no skill. Needed a safe search-and-replace checklist for migrating claim-based ownership patterns. Suggested skill name: `ownership-claim-migration`. Reference files: any of the migrated endpoint files.

## Architect Lessons

- **MeEndpoint modification was unnecessary.** Plan Step 5 said to modify MeEndpoint to "verify User exists," but since the endpoint reads claims (not DB) and the response is unchanged, this was correctly skipped by the developer. Future plans: don't list files for modification when the actual behavior is unchanged.
- **Cascade delete with Restrict FKs needs explicit analysis upfront.** The critic correctly caught that the plan's initial cascade strategy was underspecified. The Restrict FKs (Card->Participant, Vote->Participant) required explicit ordered deletion in the endpoint. Always analyze the full FK graph when planning cascade deletes, not just the direct parent-child chain.
- **Data migration step must account for EF's generated ordering.** EF Core's auto-generated migration may not produce correct SQL for data-transformation migrations (it dropped the source column before reading from it). Plans involving data migration should note that hand-crafted SQL is likely needed and the developer should not trust the auto-generated migration output.
- **hasActiveSprint in list response enables pre-check UX.** The critic caught that the delete dialog's "blocked" state (spec Flow 6) requires knowing whether a team has an active sprint before attempting the DELETE. Always check if a destructive action has a pre-check UX that needs data from the list/summary endpoint.
- **SprintListView existence wasn't verified before planning.** Plan listed it as "create" but a basic version already existed. Always Glob for files before claiming they need to be created, especially views that map to existing routes.

## Reviewer Lessons

- **`ExecuteDeleteAsync` is not transactional by default.** When a plan says "all deletes in a single transaction", `ExecuteDeleteAsync` does NOT fulfill this — each call auto-commits. Must explicitly wrap in `BeginTransactionAsync` / `CommitAsync`. Flag any method using multiple sequential `ExecuteDeleteAsync` calls without a transaction scope.

- **EF index definitions vs query collation are independent.** A `EF.Functions.Collate(..., "NOCASE")` lookup in a repository is not sufficient to guarantee case-insensitive uniqueness at the DB level. The unique index itself must specify collation via `.UseCollation("NOCASE")`. Always check both the index definition and the query when a plan specifies case-insensitive uniqueness.

- **Migration Down() correctness must be independently verified.** Hand-crafted migrations are high risk for broken rollbacks. When Up() uses raw SQL (table rebuild, FK via CREATE TABLE), Down() cannot use EF's typed `DropForeignKey`/`DropColumn` — it must mirror the raw SQL approach. Always read both Up() and Down() in full; do not assume Down() is correct if Up() is correct.

- **Router guard cold-load pattern.** Vue Router guards that check `authStore.loaded && condition` silently pass through when auth is not yet loaded. Flag this pattern on protected routes — the guard should either await auth resolution or block navigation until the store is ready.
