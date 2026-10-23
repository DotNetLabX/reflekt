# F40 — Architecture Reference & Conventions Docs

**Feature Spec:** `docs/specs/F40-ArchitectureDocs/definition/spec.md`
**Model:** claude-opus-5-5[1m]
**Build slices:** 1–4 · 5–10 · 11–14; why: Step 3 (`architecture.md`) reads the whole system and is heavy, so the first slice ends with the architecture docs; slice 2 holds the instruction and conventions files; slice 3 the agent-setup docs, small fixes, the graph and the verification. Every later slice reads the Step 1 fact sheet in `implementation.md` and does not re-derive it.
**Size:** 14 developer-built steps · 4 files · fullChecks: none — no fullChecks list declared (the count leaves out files under `docs/` and the generated `graphify-out/`, per the size rule: the four are `CLAUDE.md`, `src/Services/Reflekt/CLAUDE.md`, `README.md`, `.claude/README.md`; about 16 files under `docs/` also change)
**Lane:** full — normal gear (14 developer-built steps, more than 8)
**Build lane:** thorough — full team — normal gear
**Gear:** normal — campaign

## Context

The repo's current-state docs describe a different system: the architecture doc is the v1 proposal, the root `CLAUDE.md` is a starter template (modules, gateway, message queue, gRPC), the sign-in docs describe an allowlist nothing enforces, and the testing and EF conventions come from another app. After F35–F39 (all present on this branch), this docs-only feature makes every current-state doc describe the code as it is, adds `docs/architecture/architecture.md` and turns `docs/architecture/index.md` into a one-page map. Service impacted: none at runtime — only `*.md` files and the regenerated `graphify-out/`.

**Every fact is taken from the final source at build time** (spec Purpose). The audit inventory (`docs/specs/F34-ConventionsAudit/delivery/findings/architecture.md` § 3) and the sibling records are a starting list, not the answer. Step 1 builds the fact sheet every later step cites.

**Campaign rules that bind this plan:** `docs/backlog.md` is a shared file, so its change is a note in `delivery/change-notes.md` (P2). Work only inside this worktree (P3). The docs describe the state after the campaign applies the sibling change notes (P4).

## Scope

**In scope:** every row of the spec's Scope table, step by step below (each step's `Satisfies:` names its rows).

**Out of scope:** any source, test or config file (a deleted `graphify-out/` under the graph fallback excepted); `.claude/agents`, `.claude/rules`, `.claude/skills`; product scope decisions; the old evidence docs (`docs/features/`, `ai-coding/`, `docs/plans/`, past `docs/specs/*`); `docs/backlog.md` itself (its note is a change note).

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | (none) | — | no | fact listings with the command behind each | — expected: inventory work, no pattern |
| 2 | (none) | — | no | the `dotnet ef` commands of P3 | — expected |
| 3 | create-architecture-doc | Follow | no | skill inventory of `.claude/skills/` with "not used by this app" entries; reference-don't-restate; the spec's section list in place of the template's (spec Decisions row 4) | — |
| 4 | (none) | — | no | map sections; `v1.md` status and annotations | — expected |
| 5 | (none) | — | no | root `CLAUDE.md` sections | — expected; `create-service-claude-md` scaffolds a new service's file, not this one |
| 6 | (none) | — | no | `project-rules.md` edits; service `CLAUDE.md` corrections | — expected; `create-service-claude-md` checked, scaffolds new services only |
| 7 | (none) | — | no | sign-in prose in `docs/product/v1.md` | — expected |
| 8 | (none) | — | no | `testing.md` sections; `coding-conventions.md` index | — expected |
| 9 | (none) | — | no | the two tables of `known-deviations.md` | — expected |
| 10 | (none) | — | no | `ef-core.md`, `vue.md`, `csharp.md` edits | — expected |
| 11 | (none) | — | no | `.claude/README.md`, `kb-entry-schema.md` footnotes | — expected |
| 12 | (none) | — | no | `README.md`, product status lines, backlog change note | — expected |
| 13 | (none) | — | no | `graphify update . --force` | — expected |
| 14 | (none) | — | no | acceptance greps AC1–AC8 | — expected |

Every `None` was checked against the frontmatter of the 37 skills in `.claude/skills/`. The only doc-writing skills are `create-architecture-doc` (Step 3), `create-feature-spec` (PO specs), `create-service-claude-md` and `create-module-claude-md` (both scaffold a *new* service's or module's `CLAUDE.md` — no match for correcting an existing one), and `improve-flow` (the learner's lesson promotion — no match). No step adds testable behaviour: TDD is `no` throughout (owner: write no new tests).

## Domain Model Changes

None.

## Data Model Changes

None.

## Implementation Steps

### Step 1 — Base commit and fact sheet

Write `docs/specs/F40-ArchitectureDocs/delivery/implementation.md` § Step 1, the fact sheet every later step cites. For each item, give the listing and the command or file that produced it:

- **Base commit:** `git rev-parse HEAD` at the start of the build. Expected `e5b43ce` plus the plan commit; the base for AC7 is `e5b43ce`.
- **Solution:** the projects in `Reflekt.slnx`, and the project reference graph read from every `*.csproj` `ProjectReference` (F38 changed it: the API references `Blocks.Core` and `Blocks.Exceptions` directly; `Blocks.Persistence` no longer references `Blocks.Core`). Central package versions come from `Directory.Packages.props`.
- **Domain:** the aggregate roots, the child entities, the reference data (`PhaseAction`), the enums (`SprintPhase`, `SprintState`, `SprintAction`), the domain event records, and the `IEventHandler<T>` classes with the event each handles (the events with no handler named).
- **Events:** confirm P1 in the source. `DomainEventPublishingInterceptor` (`src/BuildingBlocks/Blocks.Persistence/`) awaits `PublishInOrderAsync`, holds events inside explicit transactions, starts each publication with `CancellationToken.None` (the save's token is not passed), and logs handler failures.
- **Evidence-link fetch pipeline:** `EvidenceLinkSourceDetector` (GitHub / Jira / Generic) and the `EvidenceLinks:JiraDomain` key; the Jira short-circuit in `EvidenceLink` (no fetch, created with status Ok); the bounded `Channel<FetchJob>` (capacity, full mode) in `OpenGraphConfiguration`; the background service; the fetcher's limits (timeout, size cap, SSRF guard); the handler that broadcasts and queues.
- **Packages:** every `PackageVersion` in `Directory.Packages.props`, with the project that references it.
- **API:** every endpoint class per `Features/` slice with its verb, route and policy. The endpoints are declared by attributes on the class (`[HttpGet("…")]`/`[HttpPost("…")]`/…, `[Authorize(Policy = …)]` or `[AllowAnonymous]`), not in a `Configure()` override: list them from those attributes over `Features/**/*Endpoint.cs`, and check that the endpoint count equals the number of `*Endpoint.cs` classes (a class whose route is set another way is listed with how). `ErrorCodes` (the error `code` set, F38); the `GlobalExceptionMiddleware` status map; the order of `UseReflektApi`; the options classes and config keys; the singletons that hold state.
- **Real-time:** `SprintHub` route; the client-to-server methods; the server-to-client message names, taken from both ends and reconciled: on the server, every name passed to `SendAsync`/`SendCoreAsync`, including names that reach it through a helper (for example `CardBroadcast`, which sends `CardCreated` and `CardUpdated`) or a constant; on the client, every `connection.on("…")` registration in `web/reflekt-web/src/composables/useSprintHub.ts` and anywhere else it registers. List the names on one side only, with why; the participant groups and Writing-phase redaction (F35); the readiness and timer types.
- **Auth:** the schemes, the policies, the access service (`SprintAccess`, F38), the guest-token transports, how a kicked participant is handled (soft kick). Confirm that nothing reads `Auth:AllowedEmails`.
- **Persistence:** the DbSets, the migrations (count, first and last), `MigrateAndSeedAsync`, the seed flags.
- **Frontend (`web/reflekt-web`):** the routes, views, stores, composables, the `src/api/` layer, and the `package.json` dependency ranges (P4: ranges, not lockfile versions).
- **Tests:** the test projects; the `[Fact]`/`[Theory]` count per project; the helpers in `tests/Reflekt.IntegrationTests/Helpers/` (`ReflektApiFactory`, `TestAuthHandler`, `TestDataSeeder`, `TestGoogleCredentials`); whether any test carries a `Speed` trait.
- **Agent setup:** the skill folder count in `.claude/skills/`, the files in `.claude/hooks/`, `.claude/verify.json`, and which `docs/specs/*` folders have a `definition/` folder.

Satisfies: spec AC5 (the listings), Purpose (facts from the final source).
Accept: `implementation.md` § Step 1 holds the base commit and each listing above, each with its command or file.

### Step 2 — Run and record `dotnet ef`

Per plan P3, from the repo root:

1. `dotnet tool install dotnet-ef --version 10.0.13 --tool-path obj/dotnet-ef` (`obj/` is git-ignored; never `--global`). A tool-path install does not make `dotnet ef` available: invoke the installed executable directly, `obj/dotnet-ef/dotnet-ef.exe` (first `obj/dotnet-ef/dotnet-ef.exe --version`, recorded).
2. With `-p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API`, run `dbcontext info`, `migrations list --no-connect` and `migrations has-pending-model-changes` through that executable.
3. If any of them fails while starting the host, run it again with placeholder credentials set only in the shell environment (`Auth__Google__ClientId`, `Auth__Google__ClientSecret`).

Record what the run shows:
- whether the design-time host starts without a design-time factory;
- whether it needs non-empty Google credentials;
- whether start-up migration or seeding ran. The SQLite path `./data/reflekt.db` is relative to the working directory the host gets, so do not check one fixed path: capture `git status --porcelain --ignored` before and after the run and record any new or changed `*.db` or `data/` path. Also record the `Data Source` that `dbcontext info` prints.

Never run `migrations add`. Remove `obj/dotnet-ef/` at the end.

Satisfies: spec Scope CLAUDE.md row ("run once and recorded … before it is written"), the `ef-core.md` design-time advice, AC8 (the recorded run).
Accept: `implementation.md` § Step 2 holds the version line, each command, its exit code and the decisive output lines, the three findings above, and the before/after `--ignored` listings. After `obj/dotnet-ef/` is removed, `git status --porcelain` shows no new path from this step.

### Step 3 — `docs/architecture/architecture.md` (new, as-built)

Create `docs/architecture/architecture.md`. Follow create-architecture-doc for its **skill inventory** and its rule that a pattern a skill covers is referenced, not restated. Every `.claude/skills/` folder appears in the inventory; the template-only skills (MediatR, gRPC, Redis, MassTransit, module/service scaffolding and the like) are marked "not used by this app". The template's section list does **not** bind (spec Decisions row 4); Step 9 records that departure.

Sections, in the order of the spec's Scope architecture row, each filled from the Step 1 fact sheet:
- solution layout and project reference graph;
- the 7 aggregates, phase and state machines, phase-action gating;
- events and their dispatch: the transaction-aware hold; publication awaited before the save or commit returns (`WaitForAll`, collected order); handler failures logged, never rethrown; publication started with `CancellationToken.None`, so the save's token never reaches it — what token a handler then receives is FastEndpoints' own and is not promised (P1);
- feature slices and their endpoints, with verb, route and policy;
- the evidence-link fetch pipeline (AR-16): source detection (GitHub / Jira / Generic, `EvidenceLinks:JiraDomain`), the Jira short-circuit, the bounded queue and background fetch, the fetcher's limits, and the attach request waiting while the queue is full (F38's residual);
- cross-cutting (audit § 3.8): the middleware pipeline order in `UseReflektApi`, the options and their config keys, the singletons that hold state;
- packages (AR-19): the `Directory.Packages.props` table with the project that uses each, shared-framework parts named as such, and the frontend's `package.json` ranges (P4);
- real-time: hub methods, messages, per-participant delivery and redaction, readiness, timers, the single-instance constraint;
- auth: schemes, policies, the access service, guest-token transports, soft kick. Any Google account may sign in; no allowlist exists;
- persistence: migrations run on every start, seed flags;
- the error convention and its `code` set (F38);
- frontend: API layer, stores, optimistic rules (F39);
- deployment (Azure App Service, `deploy.ps1`);
- testing as it is (a pointer to `docs/conventions/testing.md` plus the gaps).

Counts in the doc equal Step 1's listings.

Satisfies: spec AR-04, AR-05…AR-11, AR-13…AR-19, AR-21…AR-24; AC5; AC8 (inventory list).
Accept: the file exists with a heading for each section above. `grep -nE "EvidenceLinkSourceDetector|JiraDomain|Directory.Packages.props|UseReflektApi" docs/architecture/architecture.md` → a hit for each of the four names. `implementation.md` § Step 3 records, per section, the commit it was checked against and how (the Step 1 listing it uses), plus the list of inventory entries marked "not used by this app". `grep -niE "allowlist|AllowedEmails" docs/architecture/architecture.md` returns only lines stating no allowlist exists.
Confidence: medium — large doc; the fact sheet keeps it grounded.

### Step 4 — The one-page map and `v1.md` as the decision log

- `docs/architecture/index.md`: replace `@v1.md` with the one-page map (projects, reference graph, the request flow for a mutation, and a concern → where-it-lives table), then the line `@architecture.md`. `v1.md` is a plain markdown link, never `@`. Keep it to about one page.
- `docs/architecture/v1.md`: status line → `**Status:** Accepted — superseded by architecture.md`. Annotate in place, as superseded, every allowlist statement — the § 6 Teams row ("Allowlist check.", line 182) and the § 7.1 paragraph (line 210) — and the deferred items and open questions (§§ 12–13), each pointing to the `architecture.md` section that replaces it. Find every allowlist line with `grep -niE "allowlist|AllowedEmails|not.invited" docs/architecture/v1.md` before editing. No edit beyond the status line and these annotations.

Satisfies: spec AR-25, AR-04, AR-12, AR-23; AC4 (index part); AC8 (`v1.md` status).
Accept: `grep -n "^@" docs/architecture/index.md` → `@architecture.md` only. `grep -n "Status" docs/architecture/v1.md` reads superseded. Every `allowlist` hit in `v1.md` sits in text marked superseded.

### Step 5 — Root `CLAUDE.md` rewritten for Reflekt

Rewrite `CLAUDE.md` (owner, 2026-10-13):
- **Kept in substance:** the Authority & Spec Files section; the graphify rules; Domain (events, aggregate creation, repositories); Architecture; Guardrails.
- **Repo tree and reference graph:** the real ones, from Step 1.
- **Tech stack:** the real packages after F37. No MassTransit, RabbitMQ, gRPC, YARP or compose; any mention sits in one "not used by this app" note.
- **Frontend:** the real stack; no shadcn-vue unless Step 1 finds it.
- **Port convention:** prefix 40, service index 1 (HTTP 4001). HTTPS, gRPC and container ports are not applicable. Production runs on Azure App Service.
- **Cross-cutting, as it is:** `GlobalExceptionMiddleware`, scheme and policy auth, the save interceptor, Mapster profiles in `API/Mappings/`. No pre/post processors.
- **Guardrails:** all kept. "No god folders" is qualified to *inside a service project*. "No secrets in commits or artifacts" moves here from `project-rules.md`.
- **Domain events rule with its exception** (BE-24): an event may carry, beside the aggregate, facts not recoverable after the save. Stated here only.
- **EF Core Migrations:** root-relative commands (`-p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API`); the global install line `dotnet tool install --global dotnet-ef --version 10.0.13`; what `dotnet ef` needs at design time, from Step 2.
- **Sign-in:** any Google account may sign in.

Depends on: Steps 1–2.
Satisfies: spec AR-01…AR-03, AR-20, AR-28, AR-29, CV-05, CV-06, CV-07, BE-24, AR-12 (CLAUDE.md); AC1; AC2 (CLAUDE.md part).
Accept: AC1 grep on `CLAUDE.md` — each hit sits inside the "not used by this app" note. `grep -n "^## Authority" CLAUDE.md` → one hit. Each of the 6 original guardrails plus "No secrets", and the domain-events rule with its exception, is present (listed in `implementation.md`). AC3 grep on `CLAUDE.md` → no hits.
Confidence: medium — every agent reads this file; the Must-NOT-Change table pins what survives.

### Step 6 — `docs/conventions/project-rules.md` and `src/Services/Reflekt/CLAUDE.md`

**`project-rules.md`:**
- The Repo Structure tree and the Project Reference Graph become one pointer to `CLAUDE.md`.
- The Guardrails list and the Conventions line on domain events point to `CLAUDE.md` (the events rule and its exception live there only).
- "No secrets" is removed here, now that it lives in `CLAUDE.md`.
- The template leftovers ("services, modules, and building blocks tree", "modules, cross-cutting") describe this repo.
- Build Verification adds `npm --prefix web/reflekt-web run lint` (F39, P4).
- `docker-compose.*` leaves the "not source files" list.
- The validator wording says validators live in the slice's `*Command.cs` / `*Query.cs`.
- The `dotnet ef` command becomes the root-relative one.
- `npx vitest run` becomes "frontend tests are not set up".
- The `SprintIssueSyncService` example is replaced by a real one from Step 1 (for example `CardRegrouping`).

**`src/Services/Reflekt/CLAUDE.md`:** the building blocks used (all four, per Step 1) and the auth schemes and token transports are corrected to the code. The port note records only the service index (1) and points to the root `CLAUDE.md` port table — no port numbers, per the root rule that services do not duplicate them.

Satisfies: spec AR-01, AR-28, AR-29, TS-07, CV-05 (project-rules row); service-doc row; AC1, AC3 (project-rules part); AC8 (`run lint`, no `vitest`).
Accept: AC1 grep on `project-rules.md`. `grep -niE "carr(y|ies) .*reference|not recoverable" docs/conventions/project-rules.md` → no hit (the events rule is only a pointer there). `grep -nE "40[0-9]{2}" src/Services/Reflekt/CLAUDE.md` → no hit. `grep -n "run lint" docs/conventions/project-rules.md` → hit. `grep -n "vitest" docs/conventions/project-rules.md` → no hit. `grep -n "SprintIssueSyncService\|docker-compose" docs/conventions/project-rules.md` → no hit.

### Step 7 — Sign-in in the product doc

In `docs/product/v1.md`, rewrite the allowlist prose (around lines 525–548, and any other `allowlist`/`not invited` hit) to say any Google account may sign in. Then run the AC2 grep over the whole path set and list every remaining hit with its disposition in `implementation.md`. `docs/backlog.md` hits are covered by Step 12's change note (P2).

Satisfies: spec AR-12 sign-in row; AC2.
Accept: AC2 grep — every hit is marked superseded or historical, states that no allowlist exists, or (for `docs/backlog.md`) is covered by the change note's historical note; the listing is in `implementation.md`.

### Step 8 — `testing.md` rewritten; `coding-conventions.md` index

**`docs/conventions/testing.md`**, rewritten to real practice:
- naming `{Action}_{Condition}_{Outcome}`, with two-part names (`Application_Starts`, `Login_RedirectsToGoogle`) as the accepted short form;
- the two test projects and their real layout;
- the real helpers (F38's shared `ReflektApiFactory`, `TestAuthHandler`, `TestDataSeeder`, placeholder Google credentials);
- no mocking library;
- FluentAssertions kept at 7.2.0 on purpose, later majors not adopted. Cite the reason from the package's own release notes if they can be fetched; otherwise state it without a reason. xunit 2.x, the test SDK and coverlet are kept as they are (F37);
- frontend testing not set up;
- the `Speed!=Slow` filter in `.claude/verify.json`'s unit role, and whether any test carries the trait (Step 1).

- coverage expectations (TS-09), reworded to this domain: unit tests for aggregate behaviour and the phase and state machines; integration tests through `WebApplicationFactory` for endpoints, authorization and phase-action gating — the practice the integration suite already follows. The "simple CRUD with no business rules" skip line goes, since the integration tests do cover CRUD; skip only pure wiring and UI layout and styling.

Drop `_sut`, the slice headers, `TestData.*` and the analytics coverage line. List every code example left in `testing.md`, with the Reflekt type it names, in `implementation.md` § Step 8 (AC3's review list).

**`docs/conventions/coding-conventions.md`:** add `@testing.md` beside `@csharp.md`, `@vue.md`, `@ef-core.md`. Then name all eight conventions files, saying which are loaded elsewhere (`project-rules.md` by the agent files, `known-deviations.md` by the developer agent, `kb-entry-schema.md` for the KB that does not exist).

Satisfies: spec TS-01…TS-09, TS-11, TS-12, DP-06, CV-01; AC3 (testing part), AC4.
Accept: `grep -n "^@" docs/conventions/coding-conventions.md` → four imports, `@testing.md` among them; the file names the eight files. AC3 grep on `testing.md` → no hits. `grep -niE "simple CRUD|analytics" docs/conventions/testing.md` → no hit. `implementation.md` § Step 8 holds the example list.

### Step 9 — `known-deviations.md`: two tables

In `docs/conventions/known-deviations.md`, keep the purpose line and the recurring-mistakes table. Rows citing another app's features keep their generic lesson and lose the foreign ids (`F28-`, `F30-`, `F31-`, `F32-`, `adhoc-Suite…`). Add a second table, **Accepted deviations** (what · why · where), holding:
- event-handler failures logged, not rethrown (P1, replacing "fire-and-forget"), with the OpenGraph queue-full wait F38 left;
- single-instance hub state;
- the two URL schemes;
- the team-count race (`TeamLimitCheck` counts, then inserts);
- the architecture-doc template departure (Step 3);
- the `tailwind-theme` skill's inline-body wording (F36);
- every skill/code difference in F38's `implementation.md` § Step 17 "Skill/code differences (for F40)" list, items 1, 2 and 4–9. Item 3 is read through its Fix Round 1 correction: the remaining difference is the P1 row;
- F39's: its record holds no labelled list, so read the whole of F39's `implementation.md` for skill/code differences — the per-step `**Skills:**` and `**Deviations:**` lines, `## Skills Used`, `## Key Decisions`, `## Deviations from Plan`, `## Carry-Over Findings` and both fix rounds — and check each candidate against the code. Two are known and are rows unless the code no longer shows them: `useTimerActions` toasting from a composable, against `vue-component-architecture` (F39 `implementation.md` Fix Round 1, row 9 / S4); the SPA's API layer built as `src/api/client.ts` (`apiFetch`) where `create-vue-feature` shows a `client/src/api/` layout and cites a file that does not exist here (F39 Step 4, `## Skills Used` row 4). Record in `implementation.md` § Step 9 each F39 candidate and whether it became a row.

Satisfies: spec CV-03, CV-04, AR-32, BE-36, SK-23 (team-count race), FE-05 skill drift; AC8 (known-deviations).
Accept: `grep -n "Accepted deviations" docs/conventions/known-deviations.md` → hit. `grep -n "useTimerActions" docs/conventions/known-deviations.md` → hit, unless `implementation.md` § Step 9 shows the code no longer toasts there. `grep -nE "F28-|F30-|F31-|F32-|adhoc-Suite" docs/conventions/known-deviations.md` → no hits. `grep -n "fire-and-forget" docs/conventions/known-deviations.md` → no hit, unless stated as removed.

### Step 10 — `ef-core.md`, `vue.md`, `csharp.md`

- `docs/conventions/ef-core.md`: rewrite every example with Reflekt types from Step 1 (for example `Card`, `EvidenceLink`, `CardGroup`, `Sprint`), keeping the generic gotchas. Correct the design-time-factory advice to what Step 2 showed.
- `docs/conventions/vue.md` § White Flash Prevention: describe F36's `<style>` block in `index.html`'s `<head>` (`html` and `html.light` backgrounds set before first paint). No inline body colour.
- `docs/conventions/csharp.md` line 73: rewrite so that "not found" is an exception, matching line 12 (F38's convention: handlers throw the exception family, and the middleware maps them). Keep the other point (do not use exceptions for flow that is not an error) only where it does not contradict.

Satisfies: spec other-conventions row (`ef-core.md`), `vue.md`/`csharp.md` row; AC3; AC8 (`vue.md`, `csharp.md`).
List every code example left in `ef-core.md`, `csharp.md` and `vue.md`, with the Reflekt type it names, in `implementation.md` § Step 10 (AC3's review list).

Accept: AC3 grep on `docs/conventions/*.md` → no hits. `grep -n "<style>" docs/conventions/vue.md` → hit, and `grep -niE "use inline .?style|style=\"background" docs/conventions/vue.md` → no hit (the first pattern is today's prescription, "use inline `style` on `<body>`"; prose that explains the change must not repeat either form). `implementation.md` § Step 10 holds the example list. `grep -n "not found" docs/conventions/csharp.md` shows no line calling it a result rather than an exception.

### Step 11 — `.claude/README.md` and `kb-entry-schema.md`

**`.claude/README.md`:**
- fix the counts (skills from Step 1);
- footnote the `docs/kb/` and `docs/skill-backlog.md` mentions as never created (owner decision 2026-10-12 — the rules stay);
- update the "repo structure in 3 files" design-decision row to the new homes (`CLAUDE.md` for tree and guardrails, `architecture.md`, `index.md`);
- list `.claude/hooks/` and `.claude/verify.json`;
- add a note that the agent files are not edited and so still say two things this feature makes stale: several name `v1.md` as the architecture context, and `learner.md` (line 68) calls `CLAUDE.md` stack-agnostic, while the root `CLAUDE.md` is now written for this app (owner, 2026-10-13).

**`docs/conventions/kb-entry-schema.md`:** the same never-created footnote.

Satisfies: spec AR-27, CV-09…CV-14; AC8 (`.claude/README.md` count).
Accept: the skill count in `.claude/README.md` equals Step 1's folder count. `grep -n "never created" .claude/README.md docs/conventions/kb-entry-schema.md` → hits.

### Step 12 — Small fixes and the backlog change note

- `README.md`: link `docs/architecture/index.md` (the map). Correct line 98's claim about which features have `definition/` folders (Step 1 listing).
- `docs/product/index.md` and `docs/product/v2.md`: v2 status → `Partially shipped (F23–F32)`.
- `docs/specs/F40-ArchitectureDocs/delivery/change-notes.md` (new, in the form of F35–F39's notes): one note for `docs/backlog.md` giving the exact text of the note to insert at the top of the file. It marks as historical everything above item 23 except parked item 22 (the preamble's "Already agreed" line and sections A–D), and says the allowlist and `CardVote` statements there were superseded. It names spec AC2 and AC8's backlog part as owed at merge (P2). Never edit `docs/backlog.md`.

Satisfies: spec AR-30, AR-31, AR-33; AC8 (product status, backlog note), AC2 (backlog part, at merge).
Accept: `grep -n "Partially shipped" docs/product/index.md docs/product/v2.md` → two hits. `grep -n "architecture/index.md" README.md` → hit. `change-notes.md` holds the backlog note with its exact text. `git diff --name-only e5b43ce -- docs/backlog.md` → empty.

### Step 13 — Regenerate the code graph (last doc-changing step)

From the repo root, run `D:/tools/graphify-venv/Scripts/graphify.exe update . --force` (it honours `.gitignore`). Then check `graphify-out/GRAPH_REPORT.md`, `graph.json` and `manifest.json` against AC6: no `wwwroot` bundle, `HelloWorld` or allowlist entry among the god nodes or community names; the code nodes (`file_type: code`) include `User` and `CardGroup` and no allowlist code; document nodes from the evidence docs are expected.

**Fallback** if it cannot run or AC6 fails:
- delete the tracked `graphify-out/` output;
- say in `README.md` how to generate it;
- make the `CLAUDE.md` graph rule conditional on `graphify-out/` existing.

Record which path was taken.

Depends on: Steps 1–12. It runs last because the fallback edits `README.md` and `CLAUDE.md`, which Steps 5 and 12 write first. Whether a rebuild refreshes document nodes is not assumed: record whether nodes from docs this feature edited (for example a `v1.md` allowlist heading) still show in the report, and whether community names were regenerated. AC6 expects document nodes and does not require them refreshed.
Satisfies: spec AR-26; AC6.
Accept: `implementation.md` § Step 13 records the command, its exit code and the AC6 checks (the god-node and community names quoted, `grep -c '"file_type": "code"'` and the `User`/`CardGroup` node hits) plus the document-node observation, or the fallback edits.

### Step 14 — Acceptance run and change-set check

Run spec AC1–AC8 exactly as written from the repo root and record each command and result in `implementation.md`. AC2 and AC8's backlog parts are met by Step 12's change note (P2). AC3's review half is the example lists of Steps 8 and 10, plus any code example in `project-rules.md` (Step 6), gathered here into one list. For AC7: `git diff --name-only e5b43ce` plus `git status --porcelain`, leaving out the campaign's own files (`.claude/.personas.json`, and `.claude/audit/` and `.claude/tmp/` where present). The change set holds only allowed `*.md` paths plus `graphify-out/`. Files the pipeline's close writes into this feature's `delivery/` folder after this step (`campaign-close.json`, `usage-record.json`) are the close's, not the build's, and are out of AC7's build-time check. Confirm `obj/dotnet-ef/` is gone and no `docs/pending-decisions/` file is left.

Satisfies: spec AC1–AC8.
Accept: `implementation.md` § Step 14 lists AC1–AC8, each with its command and pass, or the change-note reference for the backlog parts; the AC7 listing is included.

## Cross-Service Changes

None.

## Migration Notes

None — no schema change. Step 2 runs read-only `dotnet ef` commands only.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| No source, test or config file changes (a deleted `graphify-out/` under the fallback excepted) | AC7 change-set listing (Step 14) | pinned |
| Old evidence docs untouched (`docs/features/`, `ai-coding/`, `docs/plans/`, past `docs/specs/*`) | AC7 listing | pinned |
| `.claude/agents`, `.claude/rules`, `.claude/skills` untouched | AC7 listing (only `.claude/README.md` allowed) | pinned |
| `docs/backlog.md` untouched (shared file) | `git diff --name-only e5b43ce -- docs/backlog.md` empty (Step 12) | pinned |
| `CLAUDE.md` keeps its Authority section and every guardrail in substance (qualified or moved, never dropped) | Step 5 accept: Authority heading grep; per-guardrail listing in `implementation.md` | pinned |
| The domain-events rule and its exception live in `CLAUDE.md` only | Step 6 accept: `grep -niE "carr(y|ies) .*reference|not recoverable" docs/conventions/project-rules.md` → no hit; Step 5: the rule and its exception present in `CLAUDE.md` (listed in `implementation.md`) | pinned |

## Testing Strategy

No tests are written or changed (owner: write no new tests; docs only). Correctness is proven by the fact sheet (Step 1), the recorded `dotnet ef` run (Step 2), each step's grep accept, and the spec's acceptance run (Step 14). The developer's step close runs the repo's fast verify role as usual; no source changes, so it is a no-op check.

## KB Impact

None — `docs/kb/` does not exist (owner decision 2026-10-12, the rules stay); Step 11 footnotes the mentions.

## Decisions

| Decision | Why | Rejected alternative | Confidence | Status |
|---|---|---|---|---|
| Event dispatch is documented as awaited, with failures logged and not rethrown; the "fire-and-forget" accepted deviation becomes "failures logged, not rethrown" | F38's fix round 1 removed the detached task; facts come from the final source — see plan P1 | keep the spec's wording | high — read in the source | decided |
| `docs/backlog.md` note delivered as a change note; AC2/AC8 backlog parts owed at merge | campaign shared-file rule — see plan P2 | edit the backlog directly | high — binding directive, confirmed by the coordinator | decided |
| `dotnet-ef` installed to `obj/dotnet-ef/`, read-only commands only | no global tool, no writes outside the worktree, no source change — see plan P3 | global install; local tool manifest; `migrations add` then `remove` | high | decided |
| Docs describe the post-merge state; frontend versions cited as `package.json` ranges | the docs land after the sibling notes apply — see plan P4 | describe the worktree as it is | medium — a note may apply differently | decided |
| Campaign files (`.claude/.personas.json`, `.claude/audit/`, `.claude/tmp/`) left out of the AC7 change set | they are the campaign's, not this feature's, as in F38's changed-files check | count them and fail AC7 | high | decided |
| The graph is regenerated last (Step 13) | the code is final by then, and the fallback edits files Steps 5 and 12 write first; whether document nodes refresh is recorded, not assumed | regenerate first | high | decided |
| `architecture.md` uses the spec's section list, not the `create-architecture-doc` template's; its inventory adds a "not used by this app" disposition beside Referenced / Gap | required by spec Decisions row 4 (the template forbids the as-built detail this doc exists for); the departure is also a row of `known-deviations.md` § Accepted deviations (Step 9). Not a tagged `Departs from` row: that tag applies only under a `**Mirrors:**` header, which this plan has no reason to carry | follow the template strictly | high — the spec settles it | decided |
| Three build slices, cut after the architecture docs and after the conventions files | `architecture.md` is heavy; the later slices share only the Step 1 fact sheet | one slice; a split by developer | medium | decided |

## Decisions and why

### P1 — Event dispatch is documented as the final code does it, not as "fire-and-forget"
- **By:** architect
- **Decided:** `architecture.md` describes publication as awaited inside the save or commit (`Mode.WaitForAll`, in collected order; handler failures logged and never rethrown; each publication started with `CancellationToken.None`, so the save's token never reaches it — the token each handler then receives is FastEndpoints' own, which F38's probe shows can be a linked token, and the doc promises nothing about it). (Amended at the round-1 fold, plan review R1-07: the first wording said handlers get `CancellationToken.None`.) The spec's "fire-and-forget events" row in `known-deviations.md` § Accepted deviations becomes "event-handler failures are logged, not rethrown" (the one difference from `domain-patterns` F38 says is left), plus F38's residual: an evidence-link attach waits while the OpenGraph queue (100) is full.
- **Why:** the spec was written before F38's fix round 1, which removed the detached task (`DomainEventPublishingInterceptor.cs`: `SavedChangesAsync` awaits `PublishInOrderAsync`; F38 `implementation.md` § Fix Round 1, plan P7). The spec's Purpose binds every fact to the final source; documenting fire-and-forget would teach a removed behaviour.
- **Turned down:** keep the spec's wording (false against the code); ask the owner (the code decides it).
- **Rests on:** the interceptor still awaiting `PublishInOrderAsync` at build time — re-checked in Step 1.
- **Confidence:** high — read in the source and in F38's fix-round record.
- **Source:** Phase 1 analysis, 2026-10-23.
- **State:** current

### P2 — The `docs/backlog.md` note is delivered as a change note
- **By:** architect
- **Decided:** the historical note at the top of `docs/backlog.md` (spec Scope, small fixes row) is written as a note in `docs/specs/F40-ArchitectureDocs/delivery/change-notes.md`, with its exact text, applied at merge. Acceptance 2 and 8 for `docs/backlog.md` are checked against that note and are owed at merge, as F37 and F39 did for their shared files.
- **Why:** the campaign directive makes `docs/backlog.md` a shared file never edited by a unit directly; the coordinator confirmed this at plan time.
- **Turned down:** edit the backlog in the worktree (breaks the directive); drop the backlog row (loses a spec outcome).
- **Rests on:** the campaign's shared-file list.
- **Confidence:** high — binding directive.
- **Source:** campaign directive, dispatch of 2026-10-23; coordinator's Phase-2 message.
- **State:** current

### P3 — How `dotnet ef` is run and recorded
- **By:** architect
- **Decided:** `dotnet-ef` 10.0.13 (the EF Core version in `Directory.Packages.props`) is installed with `--tool-path` into the git-ignored `obj/dotnet-ef/` inside the worktree, never globally. Only read-only commands run — `dbcontext info`, `migrations list --no-connect`, `migrations has-pending-model-changes` — first bare, then, if the host fails, with placeholder Google credentials in environment variables. Each command and its output go into `implementation.md`. No `migrations add` (it writes source files). The docs give readers the global install line.
- **Why:** `dotnet-ef` is not installed on this machine. The run must not write outside the worktree or add tracked non-`.md` files (acceptance 7). `has-pending-model-changes` boots the same design-time host as `migrations add`, so it answers the audit's open question (AR-28) without writing a migration.
- **Turned down:** global install (writes outside the worktree); a local tool manifest `.config/dotnet-tools.json` (a config file, out of scope); a throw-away `migrations add` then `remove` (touches source files).
- **Rests on:** EF Core 10's `dotnet ef` supporting these three commands; checked by the run itself.
- **Confidence:** high — the run either works or its failure is the recorded fact.
- **Source:** Phase 1 analysis, 2026-10-23 (`dotnet tool list -g`: no `dotnet-ef`).
- **State:** current

### P4 — The docs describe the merged state, change notes included
- **By:** architect
- **Decided:** where a sibling unit's change note changes what a doc states (F39's `run lint` script and the lint leg in `.claude/verify.json`; F37's `npm audit fix`), the doc describes the state after the campaign applies the notes. Frontend package versions are cited as the ranges in `package.json`, never the lockfile's resolved versions.
- **Why:** the docs land on `main` after every note is applied; the spec itself requires `run lint` in `project-rules.md`.
- **Turned down:** describe the worktree as it is (would omit the lint step the spec requires and go stale at merge).
- **Rests on:** the notes in F37's and F39's `change-notes.md` being applied as written.
- **Confidence:** medium — a note could be applied differently (F39 Note 1 allows another `@vue/eslint-config-typescript` version), so no package versions from those notes are cited.
- **Source:** Phase 1 analysis, 2026-10-23.
- **State:** current

## Open Questions

None.
