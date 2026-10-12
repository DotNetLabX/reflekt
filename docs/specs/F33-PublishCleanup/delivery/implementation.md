# F33 Publish Cleanup — Implementation

**Model:** claude-opus-5-5

## Step 1 — Restore the existing backend tests to green
**Files:** `tests/Reflekt.UnitTests/Cards/CardTests.cs` — `ColumnType.WentWell` → `ColumnType.Column1` (enum renamed by Configurable Columns, #27)
`tests/Reflekt.UnitTests/Cards/EvidenceLinkTests.cs` — same enum rename
`tests/Reflekt.UnitTests/Teams/TeamTests.cs` — `Team.Create(name, ownerId)` takes an owner id (#23); the creator-email assertion now asserts `OwnerId`; test renamed `Create_SetsNameAndOwnerIdAndCreatedAt`
`tests/Reflekt.IntegrationTests/Helpers/TestAuthHandler.cs` — the test principal carries a `NameIdentifier` claim (`TestUserId = 1`), which every endpoint reads through `GetUserId()`
`tests/Reflekt.IntegrationTests/Helpers/TestDataSeeder.cs` — new fixture helpers: `SeedTestUserAsync` (persists the `User` row the test principal points at, so team creation's owner foreign key holds), `SeedDiscussedGroupsAsync` (two discussed card groups, the Discussing → Reporting gate from #21), `CreateScrumMasterCookie` (a real protected auth cookie for the Scrum Master, which action-item creation in Discussing requires since #21)
`tests/Reflekt.IntegrationTests/{Card,EvidenceLink,Participant,Sprint,Team,Vote}EndpointTests.cs` — each authenticated fixture seeds the test user after the host starts; column names `WentWell`/`DidntGoWell` → `Column1`/`Column2` in card payloads and one response assertion
`tests/Reflekt.IntegrationTests/SprintEndpointTests.cs` — `AdvancePhase_FullCycle_Returns200` and `AdvancePhase_OnReportingPhase_Returns409` seed two discussed groups before the Discussing → Reporting advance (the second test was passing only because the advance into Reporting was refused)
`tests/Reflekt.IntegrationTests/CardEndpointTests.cs` — the three action-item tests in Discussing send the Scrum Master cookie beside the guest token (`PostCardAsScrumMaster`)
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** Fixture data is seeded through the app's own `ReflektDbContext` after startup migration, not through raw SQL; the Scrum Master cookie is minted with the app's own cookie ticket format, so the production cookie handler authenticates it unchanged.
**Deviations:** Deleted tests — `TeamTests.Create_WithEmptyEmail_Throws` (Theory, 2 cases): `Team.Create` no longer takes an email, removed by User Entity & Team Ownership (#23). `TeamEndpointTests.CreateTeam_DuplicateCreator_Returns409`: the one-team-per-creator rule was replaced by up to 10 teams per user, removed by Multi-Team Support (#24).
**Bug found:** a participant's second vote on the same card group is refused with 409 "Already voted on this group" — `VoteEndpointTests.CastVote_MultipleTimes_SameGroup_Succeeds`, `RetractVote_Returns200`, `GetMyVotes_ReturnsDistribution`, `ListCards_IncludesVoteCount` fail (each casts two votes on one group) — `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/CastVoteEndpoint.cs` (the `GetOneByParticipantAndGroupAsync` check, added in commit `f5b5229`), contradicting `docs/product/v1.md:242-243` ("multiple votes on the same group (budget permitting) — there is no one-vote-per-group limit"; retract removes one at a time) and `docs/features/Voting.md:28,103,121`. Two more tests in the same class pass only by accident under that rule: `CastVote_BudgetExhausted_Returns409` and `RetractVote_ReturnsBudget` vote three times on one group, so their 409s come from the duplicate check, not the budget.
**Resumed after Q1** (`questions.md` Q1: the 409 is intended — BUG-007; plan step 1 amended):
`tests/Reflekt.IntegrationTests/VoteEndpointTests.cs` — `CastVote_MultipleTimes_SameGroup_Succeeds` renamed `CastVote_SameGroupTwice_Returns409` and asserts the 409; `CastVote_BudgetExhausted_Returns409` votes on 3 distinct groups then a 4th (its 409 now comes from the budget); `RetractVote_ReturnsBudget` spends the budget on 3 distinct groups before retracting and re-casting; `RetractVote_Returns200` and `ListCards_IncludesVoteCount` get their two votes on one group from two participants (second participant "Ben" joins), so the group-count assertions (1 and 2) stay; `GetMyVotes_ReturnsDistribution` votes once on two of three groups and expects a count of 1 for each; new private fixture helper `SetupSprintWithNoteGroupsAsync(name, count)`
`docs/product/v1.md` — line 242 states the one-vote-per-group rule (409 on a second vote) pointing at BUG-007 in `docs/bugs.md`; the line-558 summary table row says one vote per group per participant
**Verify:** step close `verify-run.js --profile fast --light` pass (unit, 6 s, 45 s limit); integration classes run one at a time through `--files` — Team, Sprint, Participant, Card, EvidenceLink, Vote, AuthSmoke, FoundationSmoke all pass. The whole-solution run is the close gate's. Test attributes: unit 64 → 63, integration 105 → 104 (the two deletions above). `git diff --name-only pre-cleanup -- src/ …` empty.

## Step 2 — Restore the frontend build to green
**Files:** `web/reflekt-web/src/components/teams/TeamSelector.vue` — deleted; `git grep -n TeamSelector -- web/reflekt-web/src` returned zero references before the delete (dead component using store members `teams.ts` no longer has)
`web/reflekt-web/src/stores/previousActions.ts` — the two `possibly undefined` reads of `data[0]` go through `const first = data[0]; if (first)`; a non-empty list behaves as before, an empty one still clears both fields
`web/reflekt-web/package-lock.json` — synced by `npm install` (adds the missing `@emnapi/*` entries); `package.json` untouched
`docs/product/v1.md` — step 1 follow-up on the coordinator's word (Q1 covers the whole voting rule): line 238 now says votes spread across groups with at most one per group; line 243 now says retracting removes the participant's one vote on that group and returns it to the budget (the old "repeat retract" wording no longer held)
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** None
**Deviations:** The deletion of `TeamSelector.vue` was made with `git rm`, which stages it; the dispatch sanctions `git rm` only for step 3's removals. Left as staged rather than run a second git write to unstage it — the coordinator owns the commit. Verify: `npm ci` exit 0, `npm run build` exit 0 (type-check clean, output into the git-ignored `wwwroot/`); `git diff --name-only pre-cleanup -- web/reflekt-web ':(exclude)web/reflekt-web/.omc' ':(exclude)web/reflekt-web/README.md'` lists exactly the three files; unit step close (`--profile fast --light`) pass.

## Step 3 — Remove secrets, personal data, build output and tool state; fix `.gitignore`
**Files:** `src/Services/Reflekt/Reflekt.API/deploy.zip` — `git rm` (committed deploy package holding the OAuth credentials)
`src/Services/Reflekt/Reflekt.API/data/reflekt.db` — `git rm` (tracked database with real user data; the app recreates it on startup by migration)
`web/reflekt-web/.omc/` — `git rm -r`, all 16 files (old agent-tool state with machine paths)
`graphify-out/.graphify_chunk_01.json`, `graphify-out/cost.json` — `git rm` (graph tool scratch output)
`.claude/settings.local.json` — `git rm --cached`; the file stays on disk, untracked
`.gitignore` — the root-anchored `data/*.db` / `data/*.db-*` lines replaced by unanchored `*.db`, `*.db-shm`, `*.db-wal`; the `/.omc` line replaced by an agent-tool-state block (`.omc/`, `.claude/settings.local.json`, `.claude/audit/`, `.claude/.current-agent`, `.claude/.worktree-target`, `.claude/.approval-scope`, `.claude/.pipeline-state`) and a `deploy.zip` rule; `graphify-out/.graphify_chunk_*` and `graphify-out/cost.json` added under Graphify; line endings kept CRLF throughout
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** None
**Deviations:** None. Verify: the four `git ls-files` accepts print nothing; the nine-path `git check-ignore` loop prints nothing; `graphify-out/GRAPH_REPORT.md`, `graph.json`, `manifest.json` stay tracked; `.claude/verify.json` is not ignored (the close commits it); unit step close (`--profile fast --light`) pass.

## Step 4 — Replace private and employer references (live files and evidence docs)
**Files:** `.claude/skills/analytics-computation-service/SKILL.md` — the seven reference-file paths into the sibling app become `{App}.API/...` (Reflekt has no analytics services to point at)
`.claude/skills/service-registration/SKILL.md` — the sibling app named as the DI example becomes Reflekt; the host-composition example's module, DbContext and endpoint names become `AddReflektModule`, `ReflektDbContext`, `MapReflektEndpoints`
`docs/conventions/csharp.md` — the namespace example becomes `Reflekt.API.Features.Sprints.CreateSprint` consuming `Reflekt.API.Features.Sprints`
`docs/conventions/ef-core.md` — "the sibling app's dataset" becomes "the Reflekt dataset"
`docs/conventions/kb-entry-schema.md` — the key-file example becomes `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs`
`docs/specs/F31-DarkLightTheme/definition/spec.md` — lines 13, 19, 22 keep the meaning (two apps in one suite share the token system, each keeps its own accent) with the sibling unnamed ("a sibling app", "the sibling app")
`docs/specs/adhoc-AgentRulesCleanup/delivery/communication-log.md`, `summary.md` — the KB-format reference to the sibling app becomes "a sibling app's format"
`.claude/agents/po.md`, `.claude/rules/agents-workflow.md`, `.claude/skills/create-feature-spec/SKILL.md` — the employer's Jira example keys become `RET-100` / `RET-100-retro-insights` and `RET-101-board-export`
`ai-coding/plans/Report/plan.md` — two absolute machine paths become repo-relative (`Reflekt.sln`, `cd web/reflekt-web`)
`docs/deployment-azure.md` — four `cd` lines with the absolute machine path become repo-relative (`cd web\reflekt-web`, `cd src\Services\Reflekt\Reflekt.API`)
`docs/plans/ExceptionInfrastructure/{plan.md,implementation.md,lessons.md}` — the private template path becomes "the reference template project"; the file path inside it in plan.md:308 is kept repo-relative within that project
`ai-coding/plans/Auth/done.md`, `ai-coding/plans/Auth/plan.md` — the owner's email in the `user-secrets` example becomes `you@example.com`
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** Edits made by exact string substitution per site, line endings (CRLF) preserved; the bare template name in `ai-coding/plans/PhaseActionGating/design-decisions.md` left as the plan allows.
**Deviations:** None. Verify: all four acceptance greps (`git grep` over tracked files) return zero lines; this file checked against the same four patterns, zero; unit step close (`--profile fast --light`) pass.

## Step 5 — Fix broken links in live docs
**Files:** `docs/architecture/v1.md` — lines 5 and 12: `docs/specs/v1.md` → `docs/product/v1.md`
`docs/product/v2.md` — line 6: `docs/specs/v1.md` → `docs/product/v1.md`
`docs/conventions/project-rules.md` — lines 10–12, 17, 77, 78: `client/` → `web/reflekt-web/`
`docs/conventions/testing.md` — line 67: `client/src/**/*.spec.ts` → `web/reflekt-web/src/**/*.spec.ts`
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** None
**Deviations:** `docs/backlog.md` not edited — the dispatch makes it coordinator-owned ("Do not touch"), while the plan lists its 10 lines (116, 184, 190, 196, 202, 208, 214, 220, 226, 232) for the `docs/specs/v[12].md` → `docs/product/v[12].md` rewrite. The first acceptance grep therefore still returns those 10 lines, all in `docs/backlog.md`; the coordinator applies that rewrite. The `client/` grep returns zero; unit step close (`--profile fast --light`) pass.

## Step 6 — Parameterize the deploy script; scrub the deployment doc
**Files:** `deploy.ps1` — a `param()` block with `-AppName` and `-ResourceGroup`, both `Mandatory`, no defaults; the `az webapp deploy` call uses `$AppName` / `$ResourceGroup` and the final URL uses `$AppName`; build, publish, zip and deploy otherwise unchanged; CRLF kept
`docs/deployment-azure.md` — the owner's app, plan and resource-group names become `<APP_NAME>`, `<PLAN_NAME>`, `<RESOURCE_GROUP>` in every `az` command, and the site URLs become `https://<APP_NAME>.azurewebsites.net` (the doc had no emails); section 5 and the redeployment section now lead with `.\deploy.ps1 -AppName <APP_NAME> -ResourceGroup <RESOURCE_GROUP>`; the naming note says which names are the reader's to choose
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** In both manual build-and-deploy blocks the second `cd` became `cd ..\..\src\Services\Reflekt\Reflekt.API`: once step 4 made the paths repo-relative, that `cd` ran from the frontend folder and no longer resolved. The Azure region in `az group create` stays as an example value.
**Deviations:** None. Step 5 note: the coordinator applied the 10 `docs/backlog.md` link rewrites, so step 5's first grep now returns zero — its deviation is resolved. Verify: both step-6 greps return zero; `Parser::ParseFile` on `deploy.ps1` reports 0 errors, both parameters `Mandatory`; `Select-String` finds `$AppName` 3 times and `$ResourceGroup` 2 times (declaration + use); unit step close (`--profile fast --light`) pass.

## Step 7 — Rewrite the READMEs
**Files:** `README.md` — rewritten: what Reflekt is (phases, real-time room, voting, evidence links, timer, report, open actions); the stack as built (.NET 10, ASP.NET Core, FastEndpoints, EF Core + SQLite, SignalR, Google OAuth + guest tokens; Vue 3, TypeScript, Pinia, Tailwind, Vite into `wwwroot/`); prerequisites; running locally — Google OAuth credentials through `dotnet user-secrets` with placeholder values, `dotnet run` on port 4001 with the database created by migration at startup, the `Seed` setting (`Seed:Enabled`, `Seed:SeedUserEmail`, Development only), frontend `npm install` / `npm run dev` with the Vite proxy; tests (`dotnet test Reflekt.slnx`) and the frontend build (`npm ci`, `npm run build`); deploy with `.\deploy.ps1 -AppName … -ResourceGroup …` and a pointer to `docs/deployment-azure.md`; a "How this repo was built" section — built by AI agents, the three artifact generations (`ai-coding/`, `docs/features/` + `docs/plans/`, `docs/specs/`), the surrounding docs, `.claude/` as used including the starter-template skills the app never used, the never-created `docs/kb/` (and `docs/skill-backlog.md`), and `graphify-out/`; the MIT license line
`web/reflekt-web/README.md` — the stock Vite template replaced by a short frontend README: dev server and its proxy to the API, the build into the API's `wwwroot/`, pointer to the root README
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** Local OAuth redirect URIs are given for both origins (Vite on 5173 and the API on 4001), since which one Google sees depends on where the developer browses; both READMEs kept CRLF like the files they replace.
**Deviations:** None. Verify: `README.md` contains `.NET 10`, `dotnet test Reflekt.slnx`, `user-secrets`, `deploy.ps1`, `ai-coding/`, `docs/specs/`, `.claude/` and not `.NET 9`; `web/reflekt-web/README.md` has no `Recommended IDE Setup`; step 4's four greps stay at zero; Must-NOT-Change — `src/` diff against `pre-cleanup` empty, `.claude` diff lists only files named in steps 3–4, `ai-coding` / `docs/features` / `docs/plans` diff lists only step-4 grep hits; unit step close (`--profile fast --light`) pass.

## Fix Round 1
**Files:** `tests/Reflekt.IntegrationTests/Helpers/TestGoogleCredentials.cs` — new: `UsePlaceholderGoogleCredentials(this IWebHostBuilder)` adds non-empty placeholder `Auth:Google:ClientId` / `ClientSecret` through an in-memory configuration source, which overrides user secrets and environment variables (row 1)
`tests/Reflekt.IntegrationTests/{AuthSmoke,FoundationSmoke,Team,Sprint,Participant,Card,EvidenceLink,Vote}{Tests,EndpointTests}.cs` — every `WebApplicationFactory` (15: authenticated, anonymous, smoke, and the one-off empty-team factory in `SprintEndpointTests`) calls `builder.UsePlaceholderGoogleCredentials()` first in its `WithWebHostBuilder`; the two smoke classes gain the `Helpers` using; each file's line endings kept (row 1)
`tests/Reflekt.IntegrationTests/VoteEndpointTests.cs` — `RetractVote_Returns200` asserts both initial votes return `Created`, the response's `myVoteCount` is 0 and the second participant's `GetMyVotes` still holds one vote on that group (row 3); `CastVote_SameGroupTwice_Returns409` asserts the first vote returns `Created` before the 409 (row 4)
`README.md` — prerequisites and section 1 (now "Google OAuth credentials") say the authentication setup needs non-empty Google client values even for guest-only use: real values to sign in with Google, any placeholder text to run as a guest (row 2); each command block states where it runs from — repository root, or the API folder for the `Seed` user-secrets block; the generations table now says first-generation folders keep the delivery record as `done.md` in the earlier ones and `implementation.md` + `summary.md` in later ones (some hold only part of the set), and that `docs/specs/` also holds ad-hoc runs and only F31 and F32 have a `definition/` folder (row 5)
`.claude/agents/po.md`, `docs/conventions/testing.md` — the sibling app's ticket-key prefix in the two example keys becomes the neutral `RET` prefix (row 6)
`.claude/skills/service-registration/SKILL.md` — the host-composition example's module-registration, DbContext and endpoint-mapping names become `Add{App}Module`, `{App}DbContext`, `Map{App}Endpoints` (row 7)
**Skills:** None (fix list rows map to plan steps 1, 4 and 7 — all Skill none, TDD no; rows 3–4 tighten assertions on existing behavior)
**Decisions:** One shared builder extension rather than two keys copied into 15 dictionaries; it is added as its own configuration source, so the order against each factory's existing in-memory dictionary does not matter. `{App}DbContext` used as the fix list names it, although Reflekt has a DbContext of that name — the example is a multi-module host, and the three placeholders read as one set.
**Deviations:** None. Verify — row 1 proof: with `Auth__Google__ClientId=` and `Auth__Google__ClientSecret=` set on the runner command (an empty value was confirmed to reach the runner's child processes), all eight integration classes through `verify-run.js --profile fast --light --files`, one class per call — Team, Vote, AuthSmoke, FoundationSmoke, Sprint, Participant, Card, EvidenceLink — each pass; a single call with all eight files was cut at the 45 s light limit after the build, so the classes were run one at a time as in step 1. Row 6: step 4's four greps and the ticket-prefix grep return zero. Step close `verify-run.js --profile fast --light` pass (unit, 6 s).

## Key Decisions
- Integration fixtures seed through the app's own `ReflektDbContext` after the startup migration (user, discussed card groups), and the Scrum Master cookie is minted with the app's own cookie ticket format, so no production auth code is replaced in tests (step 1).
- The vote tests follow the one-vote-per-group rule (Q1, BUG-007): two-vote-on-one-group cases use a second participant where the group count is asserted, distinct groups where a participant's budget or distribution is asserted (step 1).
- `previousActions.ts` reads `data[0]` once through a guarded local, keeping the non-empty behavior (step 2).
- Replacement names: `{App}.API/...` where Reflekt has no equivalent, Reflekt names where it does, `RET-100` / `RET-101` Jira examples, "the reference template project", `you@example.com` (step 4).
- The deployment doc's manual `cd` into the API folder now climbs from the frontend folder (step 6).

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | None | plan: Skill (none), TDD no — repairing existing tests |
| 2 | None | plan: Skill (none), TDD no — frontend hygiene |
| 3 | None | plan: Skill (none), TDD no — repo hygiene |
| 4 | None | plan: Skill (none), TDD no — text edits |
| 5 | None | plan: Skill (none), TDD no — text edits |
| 6 | None | plan: Skill (none), TDD no — script and doc |
| 7 | None | plan: Skill (none), TDD no — docs |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| Backend suite verified per class, not as one run | medium | reviewer | unit step close green; each integration class green through `--files` | The whole-solution `dotnet test Reflekt.slnx` is the close gate's complete run; classes share no state but run in parallel there |
| Changed assertion in a kept vote test | low | reviewer | `VoteEndpointTests.GetMyVotes_ReturnsDistribution` now expects 1 and 1 (was 2 and 1) | Forced by the one-vote-per-group rule (Q1); the other reworked vote tests keep their assertions |
| Test user id fixed at 1 | low | reviewer | `Helpers/TestDataSeeder.SeedTestUserAsync` throws if the seeded user does not get id 1 | Holds because every fixture uses a fresh database; the throw makes a broken assumption loud instead of a 403 |

## Deviations from Plan
- Step 1: two tests deleted (behavior removed by #23 and #24); the vote tests and `docs/product/v1.md` lines 238, 242, 243, 558 updated per Q1 and the coordinator's follow-up.
- Step 2: `TeamSelector.vue` deleted with `git rm` (staged) — outside the dispatch's git allowance; accepted by the coordinator.
- Step 5: `docs/backlog.md` left to the coordinator (coordinator-owned); the coordinator applied the 10 link rewrites.

*Status: COMPLETE — developer, 2026-10-13*
