# F33 — Publish Cleanup

**Feature Spec:** None
**Model:** claude-opus-5-5
**Lane:** light — no fullChecks list declared
**Rule list:** not applicable — plan-only definition

## Context

The repo will be published free to students as "a full app built with agents". Before that it must be clean: no secrets, no personal or employer data, no build output or tool state, live docs that point at real files, and a green build with its existing tests. Backlog row: `docs/backlog.md` § F. Publish Readiness, item 33. Baseline tag: `pre-cleanup` (commit `bbe006d`).

**Owner scope limit (2026-10-13, binding):** cleanup, code quality, architecture reference/map and code conventions only — no new features, no new tests, no business-rule registries, no KB. The local agent setup (`.claude/agents`, `rules`, `conventions`, `skills`) and the old feature docs (`ai-coding/`, `docs/features/`, `docs/plans/`) stay as evidence of how the app was built: they are edited only where this plan names an edit.

**Delivery artifacts never quote what this feature removes.** The employer's name, the sibling app's name, private repo paths, the owner's email and the credential values are referred to only by description ("the employer", "the sibling app", "the owner's email") in `plan.md`, `implementation.md`, `review*.md`, `done-check.md`, `lessons.md` and `summary.md`. Grep patterns in this plan use character classes (`o[m]ni…`) so they never match their own text.

**Plan-time evidence (executed 2026-10-13 on `bbe006d`):**
- `src/Services/Reflekt/Reflekt.API/deploy.zip` (18 MB, committed in `a9b9177`) contains `appsettings.Development.json` with the real Google client ID and secret.
- `src/Services/Reflekt/Reflekt.API/data/reflekt.db` is tracked and holds 2 real user emails and the employer's team name.
- `web/reflekt-web/.omc/` (16 files) is tracked tool state from an older agent tool, with machine paths.
- `.claude/settings.local.json` is tracked and lists paths to the owner's other private repos.
- **Backend tests red at baseline:** `dotnet build Reflekt.slnx` fails with 17 errors, all in `tests/Reflekt.UnitTests` (`Cards/CardTests.cs`, `Cards/EvidenceLinkTests.cs`: `ColumnType.WentWell` no longer exists; `Teams/TeamTests.cs`: `Team.Create` argument 2 is now `int`, `Team.CreatorEmail` no longer exists). `tests/Reflekt.IntegrationTests`: 98 of 105 fail. Tests were last touched in `82fb833` (2026-05-09); features 23–32 changed the domain and auth after that. The app itself builds.
- **Frontend build red at baseline:** after `npm install`, `npm run build` fails in `type-check` (8 vue-tsc errors): 6 in `web/reflekt-web/src/components/teams/TeamSelector.vue` (uses `hasTeam`/`currentTeam`, which `stores/teams.ts` no longer has; no file references the component — dead code), 2 in `web/reflekt-web/src/stores/previousActions.ts:52-53` (`data[0]` possibly undefined). `npm ci` fails: `package-lock.json` is out of sync (missing `@emnapi/*`). `deploy.ps1` throws on the red build, so it cannot deploy today.
- The app creates its database on startup: `Reflekt.Persistence/DependencyInjection.cs` creates the `data/` folder and runs `db.Database.Migrate()`, so removing the tracked `.db` loses nothing. The frontend build output (`src/Services/Reflekt/Reflekt.API/wwwroot/`) is already git-ignored.

**Owner actions outside F33 (not developer work; listed in `summary.md`):**
1. Rotate the Google OAuth client secret now — commit `a9b9177` (with the secret inside `deploy.zip`) is reachable from `origin/main`.
2. Publish only the history-scrubbed fresh copy (the scrub runs after F34, on the owner's word).
3. Never push the `pre-cleanup` tag to a public remote — it points at unscrubbed history.

## Scope

**In scope:** restore the existing backend tests and the frontend build to green; remove secrets, personal/employer data, build output and tool state from the current tree; `.gitignore` fixes; replace private and employer references; fix broken links in live docs; parameterize the deploy script; rewrite the two READMEs; finalize the verify declaration.

**Out of scope:** the git-history scrub (one-way, owner's word, fresh copy); new tests; app-behavior changes; package upgrades for the two advisories found in review (AngleSharp 1.2.0, SQLitePCLRaw 2.1.10 — they change runtime, so they go to F34's dependency triage); the conventions and architecture audit (F34); editing old feature docs beyond the named replacements; creating `docs/kb/` or `docs/skill-backlog.md`.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | (none) | — | no | backend tests vs current domain/auth | — (repairing existing tests; no new behavior) |
| 2 | (none) | — | no | dead component, two type errors, lockfile | — (frontend hygiene, expected) |
| 3 | (none) | — | no | files to untrack, `.gitignore` rules | — (repo hygiene, expected) |
| 4 | (none) | — | no | private-reference replacements | — (text edits, expected) |
| 5 | (none) | — | no | live-doc link fixes | — (text edits, expected) |
| 6 | (none) | — | no | `deploy.ps1`, `docs/deployment-azure.md` | — (script/doc, expected) |
| 7 | (none) | — | no | `README.md`, `web/reflekt-web/README.md` | — (docs, expected) |
| 8 | (none) | — | no | `.claude/verify.json` | — (close-owned) |

TDD is `no` throughout: no step adds behavior, and the owner forbids new tests.

## Domain Model Changes

None.

## Data Model Changes

None. No migration.

## Implementation Steps

### Step 1 — Restore the existing backend tests to green
Make `dotnet test Reflekt.slnx` pass by updating the tests to the current domain and auth. Test code only.
- Files: `tests/Reflekt.UnitTests/**`, `tests/Reflekt.IntegrationTests/**`.
- Known causes (plan-time, reproduced by both plan reviewers):
  - Unit: `ColumnType.WentWell` removed by Configurable Columns (#27); `Team.Create` signature and `Team.CreatorEmail` changed by User Entity & Team Ownership (#23).
  - Integration: endpoints read the user id through `GetUserId()` (`Reflekt.API/Auth/ClaimsPrincipalExtensions.cs`), which throws `NameIdentifier claim is missing` → 500. `Helpers/TestAuthHandler.cs` issues only Email and Name claims. Team creation also needs a persisted owner (`User` row, foreign key). So each suite's test setup must seed a `User` in its temporary database and the test principal must carry a `NameIdentifier` equal to that user's id. The `CreateTeamResponse` shape is unchanged — keep the success-response assertions.
- Vote tests (amended 2026-10-13, `questions.md` Q1): the one-vote-per-group 409 is intended (BUG-007); the four tests that vote twice on one group are updated to the current rule, the two budget tests are made to exhaust the budget across distinct groups, and `docs/product/v1.md` lines 242 and 558 are corrected to the rule.
- Rules: no new test cases (new setup/fixture code to make existing cases run is allowed); keep every existing test. A test whose behavior no longer exists in the app may be deleted — one line per deletion in implementation.md naming the test and the removing feature. A test that fails because the **app** is wrong is not bent to pass: stop and hand back `Bug found` with the test name and evidence (the coordinator routes it). No change under `src/`.
- Accept: `node {plugin}/hooks/scripts/verify-run.js --profile complete --slug F33-PublishCleanup --tree {worktree}` — or, from the developer seat, `verify-run.js --role full --slug F33-PublishCleanup --tree {worktree}` — verdict `pass` for the backend part; `git diff --name-only pre-cleanup -- src/ ':(exclude)src/Services/Reflekt/Reflekt.API/deploy.zip' ':(exclude)src/Services/Reflekt/Reflekt.API/data/reflekt.db'` empty; test counts reported before/after (baseline: 64 `[Fact]/[Theory]` attributes in unit; 105 integration tests); every deleted test listed.

### Step 2 — Restore the frontend build to green
- Delete `web/reflekt-web/src/components/teams/TeamSelector.vue` (dead: re-confirm zero references with `git grep -n TeamSelector -- web/reflekt-web/src` before deleting).
- Fix the two `possibly undefined` errors at `web/reflekt-web/src/stores/previousActions.ts:52-53` with a guard that keeps behavior for the non-empty case.
- Sync the lockfile: run `npm install` in `web/reflekt-web/` and commit the resulting `package-lock.json`; `package.json` is not edited.
- Accept: from `web/reflekt-web/`, `npm ci` exits 0, then `npm run build` exits 0; `git diff --name-only pre-cleanup -- web/reflekt-web ':(exclude)web/reflekt-web/.omc' ':(exclude)web/reflekt-web/README.md'` lists exactly `TeamSelector.vue`, `previousActions.ts`, `package-lock.json`.

### Step 3 — Remove secrets, personal data, build output and tool state; fix `.gitignore`
- `git rm` (deleted from the tree): `src/Services/Reflekt/Reflekt.API/deploy.zip`, `src/Services/Reflekt/Reflekt.API/data/reflekt.db`, `web/reflekt-web/.omc/` (all 16 files), `graphify-out/.graphify_chunk_01.json`, `graphify-out/cost.json`.
- `git rm --cached` (file stays on disk): `.claude/settings.local.json`.
- `.gitignore` — add unanchored rules: `*.db`, `*.db-shm`, `*.db-wal`, `deploy.zip`, `.omc/`, `.claude/settings.local.json`, `.claude/audit/`, `.claude/.current-agent`, `.claude/.worktree-target`, `.claude/.approval-scope`, `.claude/.pipeline-state`, `graphify-out/.graphify_chunk_*`, `graphify-out/cost.json`. The existing `publish/` rule already covers publish output. The old root-anchored `data/*.db` lines may be replaced.
- Keep tracked: `graphify-out/GRAPH_REPORT.md`, `graph.json`, `manifest.json`.
- Accept (each separately, expected zero output): `git ls-files | grep -E '\.(db|db-shm|db-wal|zip)$'`; `git ls-files | grep -E '(^|/)\.omc/'`; `git ls-files .claude/settings.local.json`; `git ls-files | grep -E 'graphify_chunk|graphify-out/cost\.json'`. Ignore proof, one path per call: `for p in src/Services/Reflekt/Reflekt.API/data/reflekt.db src/Services/Reflekt/Reflekt.API/data/reflekt.db-wal src/Services/Reflekt/Reflekt.API/deploy.zip web/reflekt-web/.omc/x .claude/audit/x .claude/settings.local.json .claude/.current-agent .claude/.worktree-target graphify-out/cost.json; do git check-ignore -q "$p" || echo "NOT IGNORED: $p"; done` prints nothing.

### Step 4 — Replace private and employer references (live files and evidence docs)
The site list **is** the output of the four acceptance greps below, run at plan time (every file type, tracked files); do not hand-curate it. Plan-time sites: the sibling app in two `.claude/skills` files, three `docs/conventions` files, the F31 spec (lines 13, 19, 22) and two `adhoc-AgentRulesCleanup` delivery files; the employer's Jira keys in `.claude/agents/po.md`, `.claude/rules/agents-workflow.md`, `.claude/skills/create-feature-spec/SKILL.md`; machine paths in `ai-coding/plans/Report/plan.md`, `docs/plans/ExceptionInfrastructure/{plan.md,implementation.md,lessons.md}`, `docs/deployment-azure.md`; the owner's email in `ai-coding/plans/Auth/done.md` and `ai-coding/plans/Auth/plan.md`.
- Sibling app → the Reflekt equivalent where one exists (e.g. the KB key-file example → `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs`; a namespace example → a Reflekt namespace), else a neutral phrase ("a sibling app", `{App}.API/...`). In the F31 spec keep the meaning (two apps share a token system, distinct accents) with the sibling unnamed.
- Employer Jira keys → neutral keys of the same shape (e.g. `RET-101-board-export`, `RET-100-retro-insights`).
- Machine paths → repo-relative paths; the private reference-template path → "the reference template project". The bare template name in `ai-coding/plans/PhaseActionGating/design-decisions.md` may stay (a name, not a path).
- Owner's email → `you@example.com`.
- Accept (each separately, over all tracked files after step 3; expected zero lines):
  1. `git grep -n -I -i 'f[o]kus'`
  2. `git grep -n -I -i -E 'o[m]nishelf|o[m]niaz|P[D]-[0-9]{3,}'`
  3. `git grep -n -I -E 'c[o]de-forge|[A-Za-z]:[\\/]+src[\\/]|/[d]/src/'`
  4. `git grep -n -I -i -E 'd[u]mitrescu\.laurentiu|@g[m]ail\.com'`
  The `LICENSE` copyright name stays (the owner's name as author is intended).

### Step 5 — Fix broken links in live docs
Live docs = `CLAUDE.md`, `README.md`, `.claude/**`, `docs/conventions/**`, `docs/architecture/**`, `docs/product/**`, `docs/backlog.md`. Old feature docs are not touched.
- `docs/specs/v1.md` → `docs/product/v1.md`, `docs/specs/v2.md` → `docs/product/v2.md` (plan-time sites: `docs/architecture/v1.md`, `docs/backlog.md` — 10 lines, `docs/product/v2.md`).
- `client/` → `web/reflekt-web/` in `docs/conventions/**` (plan-time sites: `project-rules.md` lines 10–12, 17, 77, 78; `testing.md:67`).
- **Left as they are (see ## Decisions):** `docs/kb/**` and `docs/skill-backlog.md` mentions; starter-template example paths inside `.claude/skills/**`; `docs/architecture/v1.md` `src/Modules/` (architecture-vs-code drift belongs to F34).
- Accept (each separately, expected zero): `git grep -n -E 'docs/specs/v[12]\.md' -- CLAUDE.md README.md .claude docs/conventions docs/architecture docs/product docs/backlog.md`; `git grep -n 'client/' -- docs/conventions`.

### Step 6 — Parameterize the deploy script; scrub the deployment doc
- `deploy.ps1`: `-AppName` and `-ResourceGroup` become mandatory parameters with no defaults; every `az` argument that names the app or the resource group, and the final URL, uses them. Build/publish/zip/deploy behavior otherwise unchanged.
- `docs/deployment-azure.md`: the owner's own Azure names (app, plan, resource group), URLs and emails become placeholders; show the new `deploy.ps1` parameters. (Its machine paths are already gone in step 4.)
- Accept (each separately, expected zero): `git grep -n -E 'reflekt-rg|reflekt-plan|reflekt\.azurewebsites|--name reflekt\b|-n reflekt\b' -- deploy.ps1 docs/deployment-azure.md`; `git grep -n -E '[A-Za-z]:\\src' -- docs/deployment-azure.md` (this pattern returns 4 lines at baseline — confirmed). In PowerShell, `[System.Management.Automation.Language.Parser]::ParseFile` on `deploy.ps1` reports 0 errors, both parameters are `Mandatory`, and each `$AppName` / `$ResourceGroup` is referenced in the `az` call(s) (`Select-String -Path deploy.ps1 -Pattern '\$AppName','\$ResourceGroup'` finds each at least twice: declaration + use).

### Step 7 — Rewrite the READMEs
- `README.md` must contain: what Reflekt is; the stack as it is (.NET 10, Vue 3, SignalR, SQLite, FastEndpoints); prerequisites; how to run locally — backend, frontend (`npm install` / `npm run dev`), Google OAuth via `dotnet user-secrets` with placeholder values, the `Seed` setting; how to run the tests (`dotnet test Reflekt.slnx`) and the frontend build; how to deploy (`deploy.ps1 -AppName … -ResourceGroup …`); a **"How this repo was built"** section — built by AI agents, with a guide to the artifact generations: `ai-coding/` (first generation), `docs/features/` + `docs/plans/` (second), `docs/specs/` (current pipeline layout), `.claude/` (the agent setup as used — including starter-template skills this app never used, and the `docs/kb/` knowledge base its rules mention but which was never created); a license line.
- `web/reflekt-web/README.md`: replace the stock Vite template with a short frontend README (dev server, build output into the API's `wwwroot`, pointer to the root README).
- Accept: `README.md` contains `.NET 10`, `dotnet test Reflekt.slnx`, `user-secrets`, `deploy.ps1`, `ai-coding/`, `docs/specs/`, `.claude/`; not `.NET 9`; `web/reflekt-web/README.md` does not contain `Recommended IDE Setup`; step 4's greps stay at zero. The code reviewer checks each must-contain item above by reading, not only by keyword.

### Step 8 — Finalize the verify declaration (Owner: close)
Done by the closing main session, not the developer: set `roles.full` to the backend suite plus the frontend build (`dotnet test Reflekt.slnx && npm --prefix web/reflekt-web ci && npm --prefix web/reflekt-web run build`), time `unit` and `full` through `verify-run.js`, add `measured` and `stepLimitSeconds` (3× measured `unit`, rounded up to 15 s, capped at 110 s). Committed with F33.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| No app source changes under `src/` | `git diff --name-only pre-cleanup -- src/ ':(exclude)src/Services/Reflekt/Reflekt.API/deploy.zip' ':(exclude)src/Services/Reflekt/Reflekt.API/data/reflekt.db'` empty at close | pinned |
| Frontend changes limited to step 2 | step 2's `web/reflekt-web` diff lists exactly the three files (plus `.omc/` deletions and `README.md`) | pinned |
| App behavior | existing backend suite + frontend build green at close (`verify-run.js --profile complete --slug F33-PublishCleanup --tree {worktree}`) | pinned |
| Old feature docs keep their content apart from named replacements | `git diff --name-only pre-cleanup -- ai-coding docs/features docs/plans` lists only files hit by step 4's greps | pinned |
| Agent setup content apart from named edits | `git diff --name-only pre-cleanup -- .claude` lists only files named in steps 3–5 plus `.claude/verify.json` | pinned |
| Graph files CLAUDE.md relies on stay tracked | `git ls-files graphify-out/GRAPH_REPORT.md graphify-out/graph.json` returns both | pinned |

## Testing Strategy

No new tests (owner rule). Proof is the existing backend suite (step 1) and the frontend build (step 2), green at the close's complete run, plus the grep accepts above.

## KB Impact

None — the repo has no `docs/kb/`, and the owner rules out creating one.

## Decisions

| Decision | Why | Rejected alternative | Status |
|---|---|---|---|
| Restore the existing backend tests inside F33, test code and fixtures only | The declared suite is red at baseline, so no close can prove the cleanup safe; the owner allowed tests "we have already" | A separate feature (a new feature ID waits for the owner) | decided |
| Restore the frontend build inside F33 (delete a dead component, fix two type errors, sync the lockfile) | A red build blocks the deploy script and the README's instructions; it is code quality, no behavior change | Record the red build as an exception | decided |
| A test that exposes a real app bug is handed back, not bent to pass | Bending it hides a bug in a teaching repo | Skip-attribute or rewrite to match the bug | decided |
| `docs/kb/` and `docs/skill-backlog.md` mentions stay; the README says they were never created | Creating them is new content (owner rule); rewriting the rules edits the evidence | Remove the mentions | decided |
| Starter-template example paths inside skills stay | Skills are kept as built (owner: the local setup is the story) | Rewrite each skill to Reflekt paths | decided |
| Package advisories go to F34, not F33 | Upgrades change runtime; F33 is cleanup | Upgrade now | deferred |
| Neutral replacement names (`RET-101-…`, "a sibling app", `you@example.com`) | Same shape, no real data | Delete the examples | decided |
| `deploy.ps1` parameters mandatory, no defaults | A student must never deploy to the owner's app by accident | Defaults to the owner's names | decided |

## Open Questions

None.

## Plan Review

Round 1 (light lane, round 1 only): Codex primary NO-GO + Sonnet second reader REVISE; merged 14 findings (5 HIGH, 7 MEDIUM, 2 LOW), 13 fixed in this revision, 1 deferred to F34 (package advisories). Record: `review-critic.md` (merge and fold), `review-critic-codex.md`.
