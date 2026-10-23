# F35-BackendCorrectnessFixes — Lessons

## Architect Lessons

- **A spec written before the owner's no-new-tests rule carried test-shaped acceptance.** Phase 1 caught it as a question; Phase 2 had to restate every acceptance line as a grep, a code trace or a scratch evidence run, each listed in `## Decisions`. Next time, check a campaign's standing directives against the spec's acceptance column before Phase 1 ends, and name the replacement form in the question itself.
- **Plan-must-resolve items handed from the spec review were settled as writer's decisions (P2–P6).** Each one cites R1-18, so the plan critic can check closure against `review-critic.md` without re-reading the spec.
- **`dotnet-ef` was not installed in the worktree.** The plan names a git-ignored tool path and a hand-written fallback that the start-up `Migrate()` verifies. Precheck tool availability in Phase 1 for any plan that adds a migration.
- **Check which package an EF interface lives in before planning it into a building-blocks project.** `IDbTransactionInterceptor` is in EF Core Relational; `Blocks.Persistence` references base EF Core only, so the round-1 plan would not have compiled (R1-01). Grep the project's csproj against the interface's assembly at plan time.
- **A SignalR exclusion list taken at send time goes stale.** "Sprint group minus the author's connections" leaves a window for a connection joining between two sends. Where every connection has an identity, address per-identity groups instead (R1-04, P8).
- **Join versus revoke needs an order argument, not a single check.** Register, join the groups, then re-read the removed state; the revoker runs after its commit, so one side always sees the other (R1-02).
- **Integration tests are gated by the start-up migration.** `UseReflektPersistence` runs `Migrate()`, so any step that changes the EF model cannot run integration files until its migration step is built. Put a model change and its migration in one verification boundary (R2-01).
- **"Keep today's check" means today's predicates, not a stronger rule on the same input.** Applying the spec's full rule to the decoded form rejected inputs today accepts. Quote the existing predicates when a plan keeps them (R2-02).

## Skill Gaps

### error-handling — status mapping and body casing out of date
- **Kind:** skill fix (ill-fitting)
- **Seen in:** F35 plan Step 3
- **What the skill says:** `DomainException` → 400, PascalCase error body, exception message in a 500 body.
- **What the code does after F35:** `DomainException` → 409, any `HttpException` → its own status, camelCase body (`JsonSerializerDefaults.Web`), a generic 5xx message outside Development.
- **Proposed fix:** update the skill's middleware mapping and body example to match; F38/F40 conventions work is the natural home.

### domain-patterns / persistence-patterns — transaction-aware event interceptor missing
- **Kind:** skill gap
- **Seen in:** F35 plan Step 1
- **What is missing:** the scoped `SaveChangesInterceptor` + `IDbTransactionInterceptor` variant that holds events while a transaction is open, publishes on commit, and drops on rollback, failure or a dispose without commit. The skill's "transactional" variant is a different shape.
- **Proposed fix:** add the variant, with the scoped registration through `AddDbContext((sp, o) => …)`.

### (none) — response redaction, safe return URL, SSRF-guarded HttpClient
- **Kind:** skill gap (verified — no skill covers them)
- **Seen in:** F35 plan Steps 10, 12, 13
- **What is missing:** a read-side visibility rule shared by REST and broadcasts; a return-URL validation function; a `SocketsHttpHandler.ConnectCallback` address check for outbound fetches.
- **Proposed fix:** consider one security-patterns skill holding the last two; the redaction rule is project-specific and may stay a convention.

## Developer Lessons

- **Named-file and tied test runs silently become the whole unit role when the runner has no language plugin.** Here `slow.runner` is `vstest` and no installed plugin serves it. So `verify-run.js --files tests/Reflekt.IntegrationTests/X.cs` ran `Reflekt.UnitTests` and never touched the named integration file, and a raw `dotnet test` is refused on the developer seat. The fix was a declared `integration` role in `.claude/verify.json`, run with `--role integration` (Q2). Before a plan prescribes `--files` runs, check whether the runner can compose them: look for the `runner_missing` field in a run record.
- **`verify-run.js --role <x>` keeps no per-test output.** Only the JSON record survives, with `verdict` and `failed_tests`. A plan accept that asks to read a warning in the suite output (here EF's `ManyServiceProvidersCreatedWarning`) cannot be met from the record; the developer can only argue it from a green run. Phrase such accepts as a pass or fail outcome, not as a check of log text.
- **Recursive grep accepts over a project folder also match built binaries.** `grep -rn "InvalidOperationException" src/Services/Reflekt/Reflekt.Domain` hits `bin/` and `obj/` `Reflekt.Domain.dll`. Add `--include=*.cs` to count accepts.
- **An event handler that reads the database races the integration tests' teardown.** Domain events publish with `Mode.WaitForNone`, so handlers run after the HTTP response. Each integration class deletes its SQLite file in `DisposeAsync` as soon as the test ends. A handler still reading that file makes `File.Delete` throw `IOException … being used by another process`. Step 11 made the card handlers read the sprint, which made this show in 2 of 4 runs; with the handlers back at `HEAD` it did not occur in 4 runs (Q4). Before a plan puts a database read in a broadcast handler, check how the tests tear down.
- **A failed run's `output_excerpt` cuts out the middle, which is where an exception's message usually is.** The record is in `.claude/audit/verify-verdict.json`, one JSON line per run (grep the `run_id`). The test name and the `Error Message` survive when they sit near the end; a host-start failure's message has now been lost three times (F37 Note 1, and twice here). When the message matters, a scratch console probe that runs the same code path (here 240 parallel migrations) can rule out a cause; it cannot read the message.
- **Write scratch-probe source with the Write tool, not a shell heredoc.** A Bash heredoc turned the C# literal `"/\\evil.com"` into `"/\evil.com"`. C# reads that as `/` + ESC + `vil.com`, so the Step 12 probe at first rejected a different value from the AC6 case it claimed to test. The verdict was right for the wrong reason. Only a visible stand-in for the backslash (`<BACKSLASH>`) in the printed output showed the mistake. Print each input back, with every non-printing character replaced, whenever a probe tests string escapes.
- **Never cite tests as evidence before grepping that they exist.** Step 8's record said "today's kick tests … stay green"; `grep -rni kick tests --include=*.cs` finds none, so a green run said nothing about the kick path and the review had to correct the record. Under a no-new-tests rule, name the paths no test reaches as gaps in the step block instead.
- **A teardown that deletes a SQLite file should clear the pools on every retry (reasoned, not measured).** A handler that runs after the response (`Mode.WaitForNone`) can open a pooled connection after the first `SqliteConnection.ClearAllPools()`, and the pooled handle keeps the file locked on Windows. A delete retry that waits without clearing the pools again can wait out its whole budget.

## Reviewer Lessons

- An `implementation.md` Verify line that credits "today's X tests" is a claim, not evidence: grep the test names before accepting it. F35 Step 8 wrote "today's kick tests … stay green" and `tests/` holds no kick test at all, so the green run pinned nothing on that path. A review that trusts the sentence records a security path (removed token → 401, kick revocation) as covered when it is not.
- Plan wording that isolates work per unit ("each handler loads … through a scope it creates") gets read as "no shared code": F35 Step 11 produced four verbatim ~30-line copies of the same read-and-send block. On a fan-out step over N files, check the N copies against each other before the dimensions — identical bodies are the finding, and the plan sentence is the origin to name.

## Team Lead Lessons

- **Stamp `.claude/.approval-scope` with file paths, not directories.** F35's stamp listed `src`, `tests` and the spec folder as absolute directories; the prohibition gate matched none of them and logged 25 approval-scope lines for in-plan edits. They were false positives, but every one had to be triaged at the verify point. Stamp the plan's file inventory (absolute, per file) plus the scratch folder.
- **Check a fix list's follow-up line against the bar before writing it.** I first wrote `Follow-up review: due` on a list with no HIGH row; the rule is `none due` unless a row reaches HIGH (or a done-check item / skill slip is on the list). Caught and corrected before the round ended.
- **A Codex code-review job can fail inside the early window with no output at all.** The fallback (fresh Fable main in a typed-marker run) worked as written; record the job id and `earlyFailure` in the round header so the drop is countable.
