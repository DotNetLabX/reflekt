# F40-ArchitectureDocs — Second Reader

## Verdict: COMMENT

## Step 2 — Code Review (second reader, first review)

## Reviewed By
reviewer (second reader). Change set: working tree against `82a791f`, untracked files included. Documentation feature: every claim a changed doc makes about the code was checked against the source at the working tree.
**Model:** claude-sonnet-5-5

## Pre-commitment Predictions
- Counts or tables in `architecture.md` drifting from the source: **mostly held up.** Endpoint table (44 rows: verb, route, policy), 26 hub messages (server `SendAsync` set against the client `connection.on` set), 25 events, 22 handlers, 9 DbSets, 16 migrations, 63/104 tests, 37 skills (16+11+10) all match. One state-machine sentence is wrong (F-1).
- Prose claims about behaviour overgeneralised: **found** (F-1, F-3, F-4).
- Generated graph polluted by non-repo files: **found** (F-2).
- Event-dispatch wording drifting back to "fire-and-forget": not found; § 3, `CLAUDE.md` and `known-deviations.md` match `DomainEventPublishingInterceptor.cs` (awaited, `CancellationToken.None`, failures logged, hold on explicit transaction).
- Owner rules: **hold.** No source, test or config file changed (`git diff --name-only 82a791f` is `*.md` plus `graphify-out/`). `docs/backlog.md` untouched, and the change note's text matches the backlog (items 6/7/15 allowlist; `CardVote` at lines 6, 80, 85). Every current-state doc says any Google account may sign in; nothing reads `Auth:AllowedEmails`.

## Findings

### [MEDIUM] `architecture.md` misstates which sprint operations need an Active sprint
**File:** `docs/architecture/architecture.md:102`
**Origin:** implementation
**Issue:** "State machine (`Sprint`): `Start` Draft → Active; `Close` Active → Closed; **every other change requires Active.**" The code splits in two. `EnsureActive` (13 uses) guards only `Close`, `AdvancePhase`, `SetFocus`/`ClearFocus`, the focus/toggle-discussed endpoints, both join endpoints, kick, and the advance-phase check. `EnsureOpen` (15 uses, "not Closed") guards card create/edit/delete, evidence attach/detach, group create/dissolve/retitle/add/remove, voting, and column update, and passes in Draft (`Sprint.cs` `EnsureOpen`; e.g. `CreateCardEndpoint.cs:33`). The spec's Purpose binds every fact to the final source, and a reader (or agent) relying on this sentence would get the Draft-state rules wrong.
**Fix:** State the two guards: Active required for close, phase advance (and its check), focus, join and kick; "not Closed" for card, vote, group, evidence and column changes.
**Confidence:** 92/100

### [MEDIUM] The regenerated graph indexed campaign scratch files, including a copy of the OLD graph report
**File:** `graphify-out/graph.json`, `graphify-out/manifest.json`, `graphify-out/GRAPH_REPORT.md` (God Nodes, line 510 onward)
**Origin:** implementation
**Issue:** Step 13 copied the previous `GRAPH_REPORT.md` to `.claude/tmp/f40-step13/GRAPH_REPORT.before.md` and then ran `graphify update . --force` with `.claude/tmp/` in the tree (untracked, not git-ignored). `manifest.json` lists 15 `.claude/tmp/*` paths plus `.claude/.personas.json`; `graph.json` holds 105 nodes whose `source_file` is under `.claude/tmp`, 102 of them from the before-report. Consequences, all measured:
- God node #2 in the report is `Communities` (92 edges), which is that old-report copy's heading, not an abstraction of the code.
- `graph.json` carries the document nodes `Community 0 - "Minified JS Bundle (Vue dist)"`, `Community 20 - "Auth Allowlist"`, `Community 72 - "HelloWorld"` and the other minified-bundle communities, the entries AR-26 and AC6 exist to remove.
- Those nodes point at files that will not exist in the repo after the merge.
AC6 as literally written (report god-node and community headings) passes, which is why the developer's check was green. The intent ("a map of the final code") is not met.
**Fix:** Move `.claude/tmp/f40-step13/` (and ideally the other scratch) out of the tree, or exclude it from the scan, re-run `graphify update . --force`, and re-run the AC6 checks. `Communities` should drop out of the god nodes and no `.claude/tmp` path should remain in `manifest.json`. The record's own headings (`Fact sheet…`, `Step 12 — Reads…`, F33 worktree doc) still rank as god nodes because evidence docs are in scope; AC6 expects document nodes, so that is not part of this finding.
**Confidence:** 90/100

### [LOW] `ef-core.md` says every `CardRepository` read includes `EvidenceLinks`
**File:** `docs/conventions/ef-core.md:5`
**Origin:** implementation
**Issue:** "Every `CardRepository` read that hands a `Card` to a caller includes it." `GetUnreviewedActionItemsBySprintIdAsync` and `GetActionItemsReviewedInSprintAsync` (`CardRepository.cs:33-52`) return cards `AsNoTracking()` with no `Include`. They are harmless (the previous-actions response carries no links), but the sentence reads as a rule the code already breaks.
**Fix:** "Reads that hand a card on for broadcast, report or regroup (`GetByIdAsync`, `GetBySprintId…`) include it; the previous-actions reads do not need the links."
**Confidence:** 88/100

### [LOW] "8 repositories, each wrapping `SaveChangesAsync`" is 7
**File:** `docs/architecture/architecture.md:322`; same shape in `CLAUDE.md:46` ("one per aggregate … over `RepositoryBase`")
**Origin:** implementation
**Issue:** `PhaseActionRepository` is a plain read-only class with `GetAllAsync` only; the other seven derive from `RepositoryBase` (which owns `SaveChangesAsync`). `known-deviations.md` already says so; the two other docs do not.
**Fix:** "7 repositories over `RepositoryBase`, plus the read-only `PhaseActionRepository`."
**Confidence:** 90/100

## Positive Observations
- The fact sheet and per-section check table in `implementation.md` are real: the counts, the endpoint table and the message sets reproduce from the source.
- The event-dispatch rewrite (P1) is faithful to the interceptor, including the hold/drop rules and the token wording the plan amended.
- The `dotnet ef` run is recorded with exit codes and a before/after ignored-files listing; the `CLAUDE.md` design-time claims (no factory, no credentials, no start-up migration, working directory) rest on it.
- `known-deviations.md` rows carry correct file anchors (skill line numbers sampled: `error-handling` :19/:48/:76-95, `domain-patterns` :24/:43, `extract-feature-service` :30-33; the unique index `IX_Teams_OwnerId_Name`; 7 `*Contracts.cs`).
- The FluentAssertions 8.0.0 line is quoted exactly; I re-fetched the release notes ("License change": "Update ownership and license to the Xceed Community License").
- `v1.md` edits are limited to the status line and in-place annotations; the backlog note text is accurate and `docs/backlog.md` is untouched.

## Gaps
- `architecture.md` § 4 lists the shared static helpers as `CardBroadcast` and `WritingPhaseRedaction`; `CardGroupResponseBuilder` (static, `Features/CardGroups/`) is also named in F38's difference list and is absent.
- `testing.md` "Each test class creates its hosts in `InitializeAsync` … and deletes the file in `DisposeAsync`": `AuthSmokeTests` and `FoundationSmokeTests` do not use `DeleteDatabaseFilesAsync` (they clean up their own way). Cosmetic.
- `architecture.md` § 1 says one folder per aggregate holds "state file + Behaviors/ + Events/"; `Users/` has only `User.cs`.

## Carry-Over Findings (implementation.md `## Carry-Over Findings`)
| Row | Disposition |
|---|---|
| "One team per creator" in `docs/product/v1.md:545` | **Confirmed**, LOW, product scope. Code: `TeamSettings:MaxTeamsPerUser`, default 10 (`appsettings.json`). The feature rewrote that very row for sign-in and left the stale tail; architect to decide whether to correct it. Not a blocker. |
| Store file naming differs from `create-vue-feature` | **Confirmed.** The skill says `client/src/stores/{name}Store.ts` (`SKILL.md:26`, `:97`); the code has `src/stores/card.ts` and so on. A real skill/code difference, not in F39's lists, so the spec did not require a row. Optional: add one row to Accepted deviations. |
| Generic idioms left in `csharp.md` / `vue.md` | **Accepted.** Those rows are BCL/C#/Vue syntax with no foreign type; AC3's purpose (no other app's types) is met and the grep is clean. No change needed. |
| New untracked `graphify-out/` files | **Confirmed**: `.graphify_labels.json`, `.sig`, `.graphify_root`; they hold no machine paths (grepped for `D:\`, `dumit`). They go with the graph at the close, after F-2 is fixed (the labels file carries the community names, so regenerate before committing). |
| Backlog parts of AC2/AC8 owed at merge | **Confirmed.** `git status --porcelain docs/backlog.md` is empty; Note 1 names the right items and lines. |
| Agent files still name `v1.md` | **Confirmed**: `architect.md:100,112`, `po.md:111,179`, `team-lead.md:52`, `learner.md:68`; `.claude/README.md` records them. |
| Backlog "How to resume" stale | **Confirmed**: `docs/backlog.md:305-310` ("pick up from Section B", "If Foundation is not yet built"). Not in the spec's backlog row. Note only. |

## Open Questions
- Merge order: `CLAUDE.md`, `project-rules.md`, `testing.md`, `.claude/README.md` and `architecture.md` § 7 describe the `lint` script and the lint leg of `roles.full` as merged (plan P4). In this worktree `package.json` has no `lint` script and `.claude/verify.json` has no lint leg. If F40 reaches the base before F39's change notes 1 and 2 are applied, `npm --prefix web/reflekt-web run lint` fails as documented. Coordinator to confirm the order. (Confidence 60 that this needs action; it is a plan decision.)
- `architecture.md:121` restates the BE-24 exception ("a few also carry ids not recoverable after the save"). The spec/plan pin is "rule and exception in `CLAUDE.md` only" and is checked against `project-rules.md`, which passes. A description of the 25 events is arguably fact, not the rule. Left as a question (confidence 40).

## Evidence
| Check | Result | Command | Output |
|---|---|---|---|
| Run record | pass (as named in the brief; not re-run) | `verify-run` `mvlc0qqy-5a8ffce8`, complete profile | verdict pass, per the brief |
| Endpoint table vs source | pass | attribute extraction over `Features/**/*Endpoint.cs` diffed against the `architecture.md` § 4 rows | 44 = 44, verb, route and policy equal |
| Hub messages | pass | `grep SendAsync("…")` over `Reflekt.API` plus `CardBroadcast` callers; `connection.on` count in `useSprintHub.ts` | 26 server names, 26 client registrations |
| Events/handlers/tests | pass | `grep` for `IEventHandler<>`, `record …(` in `*/Events`, `[Fact]`/`[Theory]` | 22, 25, 63 and 104 |
| Sprint guards | **fail** | `grep EnsureActive\|EnsureOpen` over `src` | 13 / 15 uses split as in F-1 |
| Graph contents | **fail** | `graph.json` and `manifest.json` parsed with Python | 105 `.claude/tmp` nodes, 15 manifest paths, god node `Communities` |
| AC1, AC2, AC3 greps | pass | the spec's commands from the repo root | AC1 one hit (the "not used" note); AC2 hits are no-allowlist statements or superseded text; AC3 no hits |
| Change set | pass | `git diff --name-only 82a791f`; `git status --porcelain` | `*.md` and `graphify-out/` only; `docs/backlog.md` clean |
| FluentAssertions quote | pass | WebFetch of the 8.0.0 release page | line present verbatim |

*Status: COMPLETE — reviewer (second reader), 2026-10-23*
