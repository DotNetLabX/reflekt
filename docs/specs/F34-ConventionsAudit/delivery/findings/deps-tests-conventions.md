# F34 research: dependencies, tests vs testing.md, conventions hygiene

Repo: the F33 worktree (worktree). Nothing written in the repo. Tests not run. Date 2026-10-13.
Columns in finding tables: id | severity | location | issue | suggested fix | effort | behavior risk

## 1. Dependencies

### 1a. Vulnerable NuGet packages (dotnet list package --vulnerable --include-transitive)

| id | severity | location (package / provenance) | issue | suggested fix | effort | behavior risk |
|----|----------|---------------------------------|-------|---------------|--------|---------------|
| DP-01 | medium (GHSA-pgww-w46g-26qg, CVE-2026-54570, mXSS via annotation-xml) | AngleSharp 1.2.0. DIRECT in Directory.Packages.props (section "HTML parsing"); referenced only by src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj. Unit and Integration test projects get it transitively through the API ProjectReference | Fixed in 1.5.0 (vulnerable range < 1.5.0); latest stable 1.8.4. Bump type: minor (same 1.x major). Use: Reflekt.API/OpenGraph/OpenGraphFetcher.cs only (Configuration.Default, BrowsingContext.New, OpenAsync(req.Content(html)), QuerySelector, GetAttribute, TextContent). It parses attacker-influenced remote HTML (user-supplied evidence-link URLs) but only extracts og:title/description/image, link[rel=icon] and title text; it never re-serializes (no InnerHtml/OuterHtml), and mXSS needs re-serialization, so real exposure is low | UPGRADE to 1.8.4 (one-line bump; the API surface used is basic and stable). Reason: trivial change, removes a build warning on 3 projects, and a teaching repo should restore clean | XS | low (no OpenGraph tests exist, so smoke-test one fetch manually or add one) |
| DP-02 | high (GHSA-2m69-gcr7-jv3q, CVE-2025-6965, SQLite < 3.50.2 memory corruption) | SQLitePCLRaw.lib.e_sqlite3 2.1.10. TRANSITIVE: Microsoft.EntityFrameworkCore.Sqlite 9.0.4 -> Microsoft.Data.Sqlite(.Core) 9.0.4 -> SQLitePCLRaw.bundle_e_sqlite3 2.1.10 -> lib.e_sqlite3 2.1.10. Reaches Reflekt.Persistence, API and both test projects | GitHub advisory metadata says "<= 2.1.11, first_patched null", but NuGet has 2.1.12 and 2.1.13. I downloaded lib.e_sqlite3 2.1.13 and its win-x64 e_sqlite3.dll contains SQLite 3.53.3 (>= 3.50.2), so 2.1.13 is effectively fixed. A 3.x line (3.50.3, 3.53.3) also exists (major, avoid for now). Bump type: patch (2.1.10 -> 2.1.13). Use: SQLite is the primary store via EF Core (ReflektDbContext); grep finds no FromSql*/ExecuteSql*/SqliteCommand in src, so all SQL is EF-generated and parameterized and an attacker cannot supply SQL | UPGRADE by pin: add CentralPackageTransitivePinningEnabled=true to Directory.Packages.props and `<PackageVersion Include="SQLitePCLRaw.lib.e_sqlite3" Version="2.1.13" />`. Reason: severity High, zero code change; exploitability is low (no user SQL) so ACCEPT is defensible, but the pin is free | XS | low (not verified: Microsoft.Data.Sqlite 9.0.4 against lib 2.1.13 at runtime; expected fine, run the integration suite once) |

### 1b. Outdated packages (dotnet list package --outdated, top-level)

| id | severity | location | issue | suggested fix | effort | behavior risk |
|----|----------|----------|-------|---------------|--------|---------------|
| DP-03 | medium | Directory.Packages.props: EF Core x4, Authentication.Google, AspNetCore.OpenApi, Mvc.Testing, Microsoft.Data.Sqlite all 9.0.4 (latest 10.0.12) | Project targets net10.0 and CLAUDE.md says ".NET 10 / OpenAPI built into .NET 10", but the Microsoft.* family is on 9.0.4 | Bump the whole family together to 10.0.12 in one commit; rebuild; run full suite | S-M | medium (EF Core major; SQLite provider and migrations behavior) |
| DP-04 | low | FastEndpoints 5.34.0 (latest 8.3.0), FluentValidation 11.11.0 (12.2.0) | Three majors behind on FastEndpoints | Accept for the published example, or make it its own feature; do not mix with the security fixes | L | high (breaking API) |
| DP-05 | low | Scalar.AspNetCore 2.5.3 (2.18.0), Mapster and Mapster.DependencyInjection 10.0.0 (10.0.13) | Minor/patch behind | Optional bump | XS | low |
| DP-06 | info | FluentAssertions 7.2.0 (8.11.0), coverlet.collector 6.0.4 (10.1.0), Microsoft.NET.Test.Sdk 17.14.1 (18.10.1), xunit.runner.visualstudio 3.1.4 (4.0.1) | Test tooling behind. FluentAssertions 8+ moved to a commercial license (from my knowledge, not verified this run) | ACCEPT FluentAssertions 7.2.0 deliberately and say so in testing.md; others optional | XS | low |
| DP-07 | info | Reflekt.slnx restore | Every restore/list prints NU1902/NU1903 warnings (6 lines, 3 projects x 2 packages), visible to anyone building the example | Cleared by DP-01 + DP-02 | - | - |

### 1c. npm audit --omit=dev (web/reflekt-web; node_modules present)

Totals: 6 high, 0 critical/moderate/low. All report fixAvailable=true (no breaking change needed). Raw JSON in scratchpad/npm-audit.json.

| id | severity | location (package / provenance) | issue | suggested fix | effort | behavior risk |
|----|----------|---------------------------------|-------|---------------|--------|---------------|
| DP-08 | high | vue 3.5.32 (DIRECT, package.json ^3.5.32) and @vue/server-renderer 3.5.32 | GHSA-g2v6-rqmx-r4w6 XSS in SSR attribute handling; fixed in >= 3.5.42 (patch within caret range). App is SPA only; grep finds no renderToString/server-renderer use, so path unreachable | UPGRADE (npm update vue + lockfile) | XS | low |
| DP-09 | high | postcss 8.5.10 (vue-router -> @vue/compiler-sfc -> postcss) | 3 advisories, sourceMappingURL arbitrary .map read; fixed > 8.5.22; build-time only on own CSS | UPGRADE via npm audit fix (lockfile) | XS | low |
| DP-10 | high | nanoid 3.3.11 (postcss -> nanoid) | 3 advisories (infinite loop / overflow in non-secure generators); range <= 3.3.17; not called by app code | UPGRADE via npm audit fix | XS | low |
| DP-11 | high | source-map-js 1.2.1 (compiler-core/postcss) | GHSA-68fv-2mgg-jv7q event-loop DoS; build-time | UPGRADE via npm audit fix | XS | low |
| DP-12 | high | ws 7.5.10 (@microsoft/signalr -> ws) | GHSA-96hv-2xvq-fx4p memory-exhaustion DoS; ws is the Node transport, browser build uses native WebSocket | UPGRADE via npm audit fix, or ACCEPT (never loaded in browser) | XS | low |

Overall npm: one `npm audit fix` (lockfile-only), then `npm run build`.

## 2. Tests vs docs/conventions/testing.md

Inventory: tests/Reflekt.UnitTests (7 files: aggregates, SourceDetector, AggregateRoot) and tests/Reflekt.IntegrationTests (8 test files + Helpers/ with TestAuthHandler, TestDataSeeder, TestGoogleCredentials). About 167 [Fact]/[Theory]. xunit 2.9.3 + FluentAssertions 7.2.0 + Mvc.Testing + Microsoft.Data.Sqlite. No Assert.*, no NSubstitute, no Moq.

| id | severity | location | issue | suggested fix | effort | behavior risk |
|----|----------|----------|-------|---------------|--------|---------------|
| TS-01 | medium | testing.md "Naming" | Doc mandates `Should_{Behavior}_When_{Condition}`. Actual: 0 of ~167 tests. Real pattern: unit `Create_WithEmptyName_Throws`, `Start_FromDraft_TransitionsToActive`; integration `CreateCard_ClosedSprint_Returns409` (`{Action}_{Condition}_{Outcome}`) | Rewrite the section to the real convention instead of renaming 167 tests | S (doc) | none |
| TS-02 | low | testing.md "Naming" (`_sut`) | `_sut` used in 0 files | Drop the rule | XS | none |
| TS-03 | medium | testing.md "Slice Organization" | Numbered `// --- Slice N ---` headers and "one test per slice, do not split a behavior across tests": 0 occurrences; tests are many small focused facts (e.g. 5+ Create_* tests per aggregate) | Remove section (it comes from another app's analytics tests) or mark optional | XS | none |
| TS-04 | medium | testing.md "Project Structure" | Doc: `{Svc}.Tests/Features/{Area}/...` plus `_Fixtures/TestData.cs`. Actual: two projects (Unit, Integration); unit tests in folders per aggregate (Cards, Sprints, Teams, Votes, Participants, EvidenceLinks, BuildingBlocksTests), no `Features/` mirror; integration tests flat at project root; no `_Fixtures/` | Replace diagram with the real layout | XS | none |
| TS-05 | high | testing.md "Test Data" | Whole section describes another app's helpers: `TestData.ActiveSprint`, `ActiveDeveloper`, `FeatureTicket`, `Membership`, `Transition` (developers, tickets, story points). None exist, no TestData.cs. Real helpers: aggregate factories called directly in unit tests (`Sprint.Create(...)`); in integration tests `Helpers/TestDataSeeder.cs` (extensions on WebApplicationFactory), `TestAuthHandler.cs`, `UsePlaceholderGoogleCredentials` | Rewrite around the real helpers | S | none |
| TS-06 | medium | testing.md "Mocking" | Recommends NSubstitute; not referenced by any csproj or Directory.Packages.props; nothing mocks. WebApplicationFactory rule IS followed | State "no mocking library used; endpoints tested through WebApplicationFactory + TestAuthHandler + per-class temp SQLite file" | XS | none |
| TS-07 | high | testing.md "Frontend" (also project-rules.md Build Verification `npx vitest run`) | Prescribes Vitest + @vue/test-utils + MSW and `src/**/*.spec.ts`. None in package.json, no spec/test files, no test script | Mark frontend testing as not set up (or add Vitest) | XS (doc) / M (add) | none |
| TS-08 | info | testing.md "Assertions", class naming | FluentAssertions only, no Assert.*: CONFORMS. `{ClassUnderTest}Tests` (SprintTests, CardTests, CardEndpointTests): CONFORMS | none | - | - |
| TS-09 | low | testing.md "Coverage Expectations" | "Computation services (analytics, metrics, deltas)" do not exist here; aggregate behavior tests (second bullet) do. Integration tests cover CRUD + auth rules, which the Skip list discourages | Reword to this domain (aggregate behavior, phase/state machine, authorization) | XS | none |
| TS-10 | medium | tests/Reflekt.IntegrationTests/*EndpointTests.cs | Each of 8 classes repeats ~50-70 lines of InitializeAsync (two WebApplicationFactory instances, temp db path, auth scheme, policy override). Undocumented, and well past the rule-of-three in csharp.md; a visible smell in a teaching repo | Extract a shared factory/fixture in Helpers/ and document it | M | low (test-only; run suite) |
| TS-11 | low | .claude/verify.json | `unit` role filters `Speed!=Slow` but no test has `[Trait("Speed", ...)]`; testing.md silent on it | Document trait or drop filter | XS | none |
| TS-12 | low | tests/Reflekt.IntegrationTests/Helpers/ | `Helpers/` conflicts with the unqualified "no god folders (Services/, Helpers/, Utils/)" guardrail wording and mixes 3 unrelated things | Qualify guardrail ("in src projects") or rename folder (Fixtures/Infrastructure) | XS | none |

## 3. Conventions set hygiene

Files in docs/conventions/: coding-conventions.md, csharp.md, ef-core.md, kb-entry-schema.md, known-deviations.md, project-rules.md, testing.md, vue.md.
coding-conventions.md (4 lines): heading, `@csharp.md`, `@vue.md`, `@ef-core.md`. Those relative imports resolve.

Who loads what: project-rules.md is `@`-imported directly by architect, developer, learner, reviewer, solo. coding-conventions.md only by developer, reviewer, solo. known-deviations.md is read by an explicit developer.md instruction (line 45), not `@`. testing.md is referenced by nothing (no agent, skill, rule, README). kb-entry-schema.md is referenced only by kb-maintenance.md.

| id | severity | location | issue | suggested fix | effort | behavior risk |
|----|----------|----------|-------|---------------|--------|---------------|
| CV-01 | high | docs/conventions/coding-conventions.md | Index covers 3 of 8 files. Missing: testing.md (true orphan, no agent ever loads it), project-rules.md (fine in practice, agents import it directly, but the index does not say so), known-deviations.md (only via explicit instruction), kb-entry-schema.md (only meaningful with docs/kb, absent) | Add `@testing.md` once TS-01..07 are fixed; add a one-line note for files loaded elsewhere; add a sentence stating the index's role | XS | low (a few more tokens for 3 agents) |
| CV-02 | info | docs/architecture/index.md | Premise check: coding-conventions.md does NOT import architecture/v1.md. The chain is agent -> `@docs/architecture/index.md` -> `@v1.md` (relative; resolves to docs/architecture/v1.md, which exists). No broken path | None; optionally document the chain in .claude/README.md | - | - |
| CV-03 | high | docs/conventions/known-deviations.md | Rows cite features that are not this repo's: F28-PerDeveloperStoryQuality, F31-EpicTestHealth, F30-TestExecutionTimeline, F32-DailyDeveloperProgress, adhoc-SuiteScaffolding. Here F30=OpenActions, F31=DarkLightTheme, F32=DarkThemeContrast; docs/specs holds only F29-F33 and adhoc-AgentRulesCleanup. IDs collide with different features | Empty the table to header only, or reseed with deviations observed in this repo | S | none |
| CV-04 | medium | ef-core.md, project-rules.md, CLAUDE.md, testing.md | Foreign-app content: ef-core.md `TicketRepository`, `link.Ticket?.IssueType`, `.ThenInclude(t => t.Assignee)`; `SprintIssueSyncService` example in CLAUDE.md and project-rules.md; testing.md (TS-05). Reflekt has no Ticket/Developer/Jira-sync concept | Re-example with Reflekt types (Card, Sprint, EvidenceLink); keep the generic gotchas | S | none |
| CV-05 | medium | CLAUDE.md vs project-rules.md | Guardrails, repo structure, reference graph and conventions maintained in both (.claude/README.md line 78 admits it). Drift: "No secrets in commits" only in project-rules.md; Tech Stack, Port Convention, Frontend only in CLAUDE.md; Source File Definition and Build Verification only in project-rules.md. No direct contradiction found | Pick one home for guardrails, point the other at it | S | none |
| CV-06 | low | CLAUDE.md / project-rules.md "No god folders (Services/, Helpers/, Utils/)" | CLAUDE.md's own structure uses src/Services/, and tests have Helpers/ (TS-12) | Qualify: "inside a service project" | XS | none |
| CV-07 | low | CLAUDE.md Tech Stack vs Directory.Packages.props | States .NET 10 / OpenAPI built into .NET 10; packages are 9.0.4 (DP-03) | Align after DP-03 | XS | none |
| CV-08 | info | csharp.md, vue.md vs CLAUDE.md | Checked: GlobalExceptionMiddleware exists (Reflekt.API/Middleware/, registered in DependencyInjection.cs); GlobalUsings.cs exists in API, Domain, Persistence; no contradictions with CLAUDE.md found | none | - | - |

### Dangling references (listed only)

| id | severity | source | points at | exists? |
|----|----------|--------|-----------|---------|
| CV-09 | high | .claude/rules/kb-navigation.md (check docs/kb/index.md first; glossary-first naming) | docs/kb/index.md, docs/kb/glossary.md | NO (docs/kb absent) |
| CV-10 | high | .claude/rules/kb-maintenance.md (entire rule, lint checks, on-demand update) | docs/kb/index.md, docs/kb/{area}/{concept}.md, glossary.md | NO |
| CV-11 | medium | .claude/agents/architect.md line 87; .claude/README.md lines 45, 54; .claude/conventions/implementation-format.md line 25; .claude/skills/create-implementation-plan/references/plan-template.md line 67 (KB Impact section); .claude/skills/improve-architecture/SKILL.md line 35 | docs/kb/... | NO |
| CV-12 | medium | .claude/agents/learner.md line 133; .claude/skills/improve-skills/SKILL.md lines 15, 56 | docs/skill-backlog.md | NO (improve-skills says create if missing; learner.md row dangles) |
| CV-13 | low | docs/conventions/kb-entry-schema.md | schema for a KB that does not exist | file exists, purpose orphaned |
| CV-14 | low | CLAUDE.md "if graphify-out/wiki/index.md exists" | graphify-out/wiki/ | absent; sentence is conditional; graphify-out/GRAPH_REPORT.md exists |
| CV-15 | info | docs/skill-gaps/ (untracked in main checkout per git status) | not present in this worktree; no rule references it | n/a |

Suggested fix for CV-09..13 (one decision): either create minimal docs/kb/index.md + glossary.md so the rules work, or remove kb-navigation.md, kb-maintenance.md, the KB steps/sections, and kb-entry-schema.md from the published example. Effort S (remove) or M (author). Risk low; today agents are told to read a missing file on every run.
