# F33 Publish Cleanup — Second Reader (reviewer-second)

Covers: first Step-2 review of the whole diff (working tree against the `pre-cleanup` tag, staged, unstaged and untracked). Primary seat is not mine; this file is the second reader's copy only.

## Reviewed By
reviewer (second reader, name `reviewer-second`)
**Model:** claude-sonnet-5-5

## Verdict: APPROVED

No CRITICAL or HIGH open. One MEDIUM (a machine path inside the architect's own `done-check.md`, which trips plan step 4's acceptance grep once committed) must be fixed before the close commit; four LOW are optional. Nothing under `src/` changed besides the two deleted binaries, and no secret, credential, real e-mail address or user-directory path remains in any tracked or untracked-unignored file.

## Pre-commitment Predictions
| Predicted | Found |
|-----------|-------|
| Private/sibling references the four plan greps cannot see (abbreviations, binaries) | Yes, abbreviation form: the sibling app's three-letter ticket-key prefix remains in two live files (LOW-2). No binary carries anything: the only tracked binary is the favicon. |
| Vote-test rework weakens what a test proves | Partly: the "second vote -> 409" test no longer proves its first vote succeeded; the distribution test can only assert 1 and 1 (forced). The two accidental-409 tests were made real, which is a gain. |
| README drifts from code and config | Config keys, port, Seed keys, Vite proxy list, Node range, test and build commands all match the code. The artifact-generation guide is inexact in two details (LOW-4). |
| `src/` drift | None: the `src/` diff is exactly the two deleted files. |
| A delivery artifact quotes what the feature removes | One: `done-check.md` holds an absolute drive-letter path to the worktree (MEDIUM-1). Every other delivery artifact is clean under the four greps. |

## Findings

### [MEDIUM] Absolute worktree path in `done-check.md` fails plan step 4's machine-path grep
**File:** `docs/specs/F33-PublishCleanup/delivery/done-check.md:13` (Step 1 row, "Backend green evidence" sentence)
**Origin:** implementation (architect artifact written after the step-4 greps ran)
**Issue:** The row names the checked tree by its absolute drive-letter path. Plan step 4's acceptance grep 3 (`[A-Za-z]:[\\/]+src[\\/]`, "expected zero lines over all tracked files") returns exactly this line when run with `--untracked`; the done check's own Step 4 row says the greps are "zero again with `--untracked`", which was true only before this line was written. Once the delivery folder is committed the grep fails, and the plan makes machine paths a release blocker. Every other delivery artifact returns zero for all four greps.
**Fix:** Replace the path with a description ("the F33 worktree"), then re-run the four step-4 greps with `--untracked` as the last act before the close commit. The file belongs to the architect; I did not touch it.
**Confidence:** 95/100

### [LOW] Sibling app's ticket-key prefix survives in two live files
**File:** `.claude/agents/po.md:43`; `docs/conventions/testing.md:48`
**Origin:** design (the plan's greps match the sibling's full name only; the developer replaced the employer's keys at `po.md` line 1 and the sibling-named lines but not these two)
**Issue:** A three-letter uppercase ticket key (the first letters of the sibling app's name) is used as the example in `po.md` ("the ticket key (e.g., ...)") and in the `TestData.FeatureTicket(...)` sample. It is the same class of residue as the employer Jira keys step 4 replaced, one line away in the same file. Harmless in content, but it keeps a derivative of the sibling's name in a published tree.
**Fix:** Use the neutral shape step 4 already chose (`RET-102`, `RET-1`). Two one-line edits.
**Confidence:** 85/100

### [LOW] Host-composition example in `service-registration` now names Reflekt symbols that do not exist
**File:** `.claude/skills/service-registration/SKILL.md:159-170`
**Origin:** implementation (plan step 4: "the Reflekt equivalent where one exists, else a neutral phrase")
**Issue:** The replacement uses `AddReflektModule`, `Migrate<ReflektDbContext>()` and `MapReflektEndpoints()`. Reflekt has `AddReflektApi` and `AddReflektPersistence` (`src/Services/Reflekt/Reflekt.API/Program.cs:3-4`) and no `Migrate<T>` helper or `Map...Endpoints` extension. A student searching the repo for these names finds nothing; the same example's `AddIdentityModule` / `MapIdentityEndpoints` are template placeholders, so the Reflekt names read as real ones.
**Fix:** Use the file's own placeholder convention (`Add{Service}Module`, `{Service}DbContext`, `Map{Service}Endpoints`, as at the section above it), or keep the sibling-neutral phrase from the plan.
**Confidence:** 90/100

### [LOW] README guide to the artifact generations is inexact in two details
**File:** `README.md:88` and `README.md:90`
**Origin:** implementation
**Issue:** (a) Generation 1 is described as `plan, implementation, review, lessons, summary`. Of the 23 folders under `ai-coding/plans/`, 11 hold `done.md` instead of `implementation.md`, and most lack `summary.md`. (b) Generation 3 is described as "one folder per feature (`F29-…` onward), with `definition/` (the spec) and `delivery/`". Only F31 and F32 have `definition/`; F29 and F30 hold `delivery/` only (their specs are in `docs/features/`), and the ad-hoc folder is `delivery/`-only. A reader who follows the guide to an F29 folder will look for a `definition/` that is not there.
**Fix:** "plan, done-or-implementation record, review, lessons (a summary in some)"; "`delivery/` always, `definition/` where the spec was written there (F29 and F30 specs are in `docs/features/`)".
**Confidence:** 85/100

### [LOW] `CastVote_SameGroupTwice_Returns409` does not prove its 409 comes from the duplicate rule
**File:** `tests/Reflekt.IntegrationTests/VoteEndpointTests.cs:111-120`
**Origin:** implementation
**Issue:** The test casts a vote, casts again, and asserts 409; the first call's status is not asserted. The old test asserted 201 on the second call, which implied the first had worked. Now a first call that fails with some other 409 (closed sprint, phase gate) still passes the test, and the duplicate check at `CastVoteEndpoint.cs:68-70` is never reached. Reachability trace: the budget check runs first (`:62-64`) but cannot fire with one vote of three used, so the only 409 source on a healthy second call is the duplicate check; the weakness is only the unasserted first call. `RetractVote_Returns200` has the same shape in a smaller way: with two participants it asserts the group count is 1, which a retract that deleted the other participant's vote would also produce.
**Fix:** Assert `Created` on the first `PostVote` (no new test case, one assertion). Optional for the retract test: assert the caller's own `myVoteCount` drops to 0.
**Confidence:** 80/100

## Carry-Over Findings (implementation.md)
| Title | My ruling | Evidence |
|-------|-----------|----------|
| Backend suite verified per class, not as one run | **Refuted as an open risk.** The whole-solution run exists and passed. | Complete record below: `dotnet test Reflekt.slnx` (plus `npm ci` and the build) as one run, pass. |
| Changed assertion in a kept vote test | **Confirmed, acceptable.** | `GetMyVotes_ReturnsDistribution` now expects 1 and 1. Forced by the rule: `CastVoteEndpoint.cs:68-70` allows at most one vote per participant per group, so a participant's own distribution cannot show 2. The test still proves per-group counting and exclusion of an unvoted group (it now creates three cards and votes on two, expecting two entries). Its power to tell "count" from "constant 1" is lower; unavoidable. |
| Test user id fixed at 1 | **Confirmed, acceptable.** | `TestDataSeeder.SeedTestUserAsync` seeds through the app's own `ReflektDbContext` into a fresh temp database per fixture, then throws if the id is not `TestAuthHandler.TestUserId`; the second `WebApplicationFactory` in `SprintEndpointTests` is seeded too. A broken assumption fails loudly at setup, not as a 403 in a test body. |

## Plan conformance, checked by reading
- **Step 1.** Test code and fixtures only; `src/` diff against `pre-cleanup` is the two deleted files. Attribute counts re-counted from git: unit 64 -> 63, integration 105 -> 104, matching implementation.md. Both deletions are behavior the app removed: `Team.Create` takes an owner id and no longer takes an e-mail (`Reflekt.Domain/Teams/Behaviors/Team.cs`), and `CreateTeamEndpoint.cs` replaced one-team-per-creator with a per-user limit and a per-user duplicate-name check. `ColumnType.WentWell` -> `Column1` is the positional enum rename (`ColumnType.cs`); `Create_ActionItem_InWrongColumn_Throws` still throws for the right reason (`Card.cs` requires `ActionItems`). The three action-item tests now send the Scrum Master cookie because `CreateCardEndpoint.cs` returns 403 for an action item in Discussing without one; with the cookie they reach the 400 paths they assert, so they are stronger, not weaker. `AdvancePhase_OnReportingPhase_Returns409` was passing only because the Discussing -> Reporting gate refused; the rework reaches Reporting first (three advances, seed two discussed groups, advance, then the 409), which is a genuine fix of an accidental pass. `CastVote_BudgetExhausted_Returns409` and `RetractVote_ReturnsBudget` now spend the budget on distinct groups, and the budget check precedes the duplicate check in the endpoint, so their 409 is the budget's. Q1's ruling (BUG-007) is in `docs/bugs.md` and matches the code.
- **Step 2.** `TeamSelector.vue` has zero references in `web/reflekt-web` (the remaining hits are old plan and review prose, which stay as evidence). `previousActions.ts` reads `data[0]` once into a guarded local; a non-empty list behaves as before, an empty one still clears both fields. Lockfile diff is additive (`@emnapi/*` entries) plus removal of `"peer": true` flags; no version bumps; `package.json` untouched.
- **Step 3.** All four `git ls-files` accepts print nothing; 16 paths (the plan's nine plus the `-shm`, `publish/`, `wwwroot/`, `.pipeline-state`, chunk file and `appsettings.Development.json`) are ignored; the three graph files stay tracked and `.claude/verify.json` is not ignored. Ignored files present in the worktree are only `.claude/audit/`, `.claude/settings.local.json` and `wwwroot/`.
- **Steps 5 and 6.** Both step-5 greps and both step-6 greps return zero. `deploy.ps1` parses with 0 errors; `AppName` and `ResourceGroup` are `Mandatory`, no defaults, used in the `az webapp deploy` call and the final URL. In `deployment-azure.md` the manual `cd` sequence resolves from the repo root (`web\reflekt-web`, then `..\..\src\Services\Reflekt\Reflekt.API`).
- **Step 7, each must-contain item read against code and config:**
  | Item | Result |
  |------|--------|
  | What Reflekt is | Accurate: phases, configurable columns, drag-and-drop grouping (`RetroGroupItem.vue` has the drag handlers), dot voting, discussion flow, evidence links (GitHub / Jira / generic), per-phase timer, report, carried-over open actions. |
  | Stack as built | `.NET 10` (`Directory.Build.props` `net10.0`), FastEndpoints, FluentValidation, EF Core + SQLite, SignalR, Vue 3 / Pinia / Tailwind / Vite all present in the package files. No `.NET 9` anywhere. |
  | Prerequisites | Node `20.19+ or 22.12+` equals `package.json` `engines`. |
  | Local run, OAuth | Keys `Auth:Google:ClientId` / `ClientSecret` match `AuthenticationConfiguration.cs:36-37`; `UserSecretsId` exists in the csproj; `/signin-google` is the default callback and is in the Vite proxy; both origins are plausible redirect hosts. Placeholders only. |
  | Backend / Seed | `http://localhost:4001` matches `launchSettings.json`; `data/reflekt.db` matches the connection string and the startup `Migrate()`; `Seed:Enabled` / `Seed:SeedUserEmail` match `SeedOptions` and the Development-only gate in `Reflekt.Persistence/DependencyInjection.cs:49`; the seeded user is looked up by e-mail, so the "team is yours" claim holds. (The seed also creates cards, groups and an evidence link; the README understates it, harmless.) |
  | Frontend | `npm install` / `npm run dev`, port 5173 default, proxy list (`/api`, `/auth`, `/signin-google`, `/hubs` with ws) match `vite.config.ts`. |
  | Tests and build | `dotnet test Reflekt.slnx` and the solution file exist and list both test projects; `npm ci` / `npm run build` match the lockfile state and the `build` script. |
  | Deploy | Matches `deploy.ps1`; pointer to `docs/deployment-azure.md` resolves; "Linux" matches `--is-linux`. |
  | How this repo was built | Folders and claims resolve (`.claude/{agents,rules,conventions,skills}`, `docs/{product,architecture,bugs.md,conventions}`, `graphify-out/GRAPH_REPORT.md`); never-created `docs/kb/` and `docs/skill-backlog.md` confirmed absent. Two inexact details: LOW-4. |
  | License line | `LICENSE` is MIT. |
  | Frontend README | Stock Vite text gone; dev server, proxy, build output into `wwwroot/` (matches `outDir`), pointer to the root README all correct. |

## Positive Observations
- Fixtures seed through the app's own `ReflektDbContext` and mint the Scrum Master cookie with the app's own ticket format, so no production auth path is bypassed or replaced in tests (`Helpers/TestDataSeeder.cs`).
- The developer found two tests passing for the wrong reason (the budget pair and the Reporting-phase test) and made them real rather than leaving green-by-accident results; the lessons note records it.
- Plan scoping held: old feature docs, `.claude/` and `src/` carry only the edits the plan names; `docs/product/v1.md` was corrected to the shipped rule with the BUG-007 pointer instead of bending a test.
- Credential, key-format, client-id, GUID and URL-host sweeps over tracked and untracked-unignored files find nothing but example domains and public links.

## Gaps
- No test now covers `CreateTeamEndpoint`'s per-user team limit or its duplicate-name 409 (the deleted test covered a rule that no longer exists). New tests are out of scope by the owner's rule; flagged for F34 only.
- `Auth:AllowedEmails` is still set in five integration fixtures but nothing in `src/` reads it (dead pre-existing test config). Left alone; not F33's.
- `docs/features/Voting.md` still describes multiple votes per group; deliberately kept as evidence per Q1.

## Open Questions
- The owner's first name appears in `docs/architecture/v1.md` (lines 4, 352, 362) and `docs/product/{v1,v2}.md`, and `product/v1.md:22,41` describe the first release as shipped for the owner's team and as course material. The plan's greps and the `LICENSE` note cover only the full name; is first-name-plus-team wording intended for a public repo? (Confidence this needs a change: about 40; it is the owner's call.)
- `docs/conventions/known-deviations.md` cites feature slugs from the sibling app's pipeline as "First Seen" (F28 to F32 with other apps' feature names) that collide with Reflekt's own F29-F32. The plan keeps `.claude`-adjacent setup content as built, so I left it; confirm that wording is meant to ship. (About 50.)
- `testing.md` is written for the sibling app's test layout (`Should_..._When_...`, `_Fixtures/TestData.cs`) and does not describe Reflekt's tests; a conventions-audit item for F34, not a defect here.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pass (complete, role `full`, slug `F33-PublishCleanup`) | the brief said "pending"; the record landed in the main checkout's `.claude/audit/verify-verdict.json` | `dotnet test Reflekt.slnx && npm ci && npm run build`, pass, 55 s, 2026-10-12T23:00:03Z, tree hash prefix `23d7077ecde4`. Staleness against later artifact writes is the close's to re-measure. |
| `src/` untouched | pass | `git diff --name-only pre-cleanup -- src/` | two lines: the deleted `deploy.zip` and `reflekt.db` |
| Step 4 greps 1, 2, 4 (`--untracked`) | pass, zero lines | `git grep -n -I --untracked -i ...` | empty |
| Step 4 grep 3 (`--untracked`) | **fail** | `git grep -n -I --untracked -E '[A-Za-z]:[\\/]+src[\\/]\|/[d]/src/\|c[o]de-forge'` | one hit: `done-check.md:13` (MEDIUM-1) |
| Step 5 and step 6 accepts | pass, zero lines | the plan's four greps | empty |
| `deploy.ps1` | pass | `Parser::ParseFile` | 0 errors; both parameters Mandatory, no defaults |
| Test attribute counts | pass | `git grep -E "^\s*\[(Fact\|Theory)" pre-cleanup -- tests/...` vs working tree | unit 64 -> 63, integration 105 -> 104 |
| Credentials, key formats, e-mails, user-dir paths, GUIDs, URL hosts | pass | pattern sweeps over `git grep --untracked` | only placeholder addresses, a `UserSecretsId`, example and public hosts |
| Ignore / tracked state | pass | `git check-ignore`, `git ls-files` loops | all 16 paths ignored; graph files tracked; `.claude/verify.json` not ignored |
| Named test files | not run | no finding needed a targeted run; the complete record covers every class | n/a |

*Status: COMPLETE — reviewer-second, 2026-10-13*
