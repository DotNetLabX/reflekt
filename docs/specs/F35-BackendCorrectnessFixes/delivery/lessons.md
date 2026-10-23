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
