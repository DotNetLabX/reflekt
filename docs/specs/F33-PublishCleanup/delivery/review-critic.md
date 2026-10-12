# F33 — Plan review record

## Round 1 — 2026-10-13 — plan — primary (codex)

Header note: light lane — round 1 only, no later round verifies the fold; the owner's plan approval does (owner away: the approval is taken by the architect and listed for the owner). Codex's verdict is persisted at `review-critic-codex.md` (NO-GO, 4 HIGH · 7 MEDIUM · 1 LOW); it is this round's primary reader section. Codex ran with no lowered effort (the repo sets no `codex.effort`). The second reader ran at `high` (one below its `xhigh` frontmatter). The second reader's first message was truncated by the platform; its findings below are its own complete resend, verbatim in substance, with the owner's personal email address replaced by `<owner-email>` so this record does not republish it.

## Second reader — round 1 (sonnet)

Verdict: REVISE. Ruler: seed only (no overlay in the repo). Reproduced on a scratch copy of the baseline: 17 build errors, 98/105 integration failures.

- **F1 [HIGH]** Step 3 greps match the plan itself (plan lines 16, 68–70 carry the employer name, the sibling app name, the employer regex and private paths); once committed the check cannot reach zero, or the names ship. Fix: paraphrase the plan's own text; forbid quoting the strings in delivery artifacts. Anchor seed-0120.
- **F3 [HIGH]** Frontend build red at baseline: after `npm install`, `npm run build` fails in `type-check` with 8 vue-tsc errors — 6 in `src/components/teams/TeamSelector.vue` (uses `teamsStore.hasTeam`/`currentTeam`, which no longer exist; nothing references the component — dead code), 2 in `src/stores/previousActions.ts:52-53` (`data[0]` possibly undefined). `vite build` alone passes. `deploy.ps1` throws on the red build, so it cannot deploy. `npm ci` fails: `package-lock.json` out of sync (missing `@emnapi/*`). Fix: delete TeamSelector.vue, fix the two lines, gate on `npm install && npm run build`. Anchor seed-0120.
- **F2 [MEDIUM]** Step 2 `git check-ignore -q` with 5 paths exits 128 ("--quiet is only valid with a single pathname"). Fix: per-path `-q` loop. Anchor seed-0130.
- **F4 [MEDIUM]** Step 5 regex `'[A-Za-z]:\\\\src'` means two literal backslashes; never matches; passes before any edit. Fix: `'[A-Za-z]:\\src'` (4 lines at baseline). Anchor seed-0130.
- **F5 [MEDIUM]** Step 1 cause wrong: `CreateTeamResponse(int Id, …)` unchanged; the real cause is `InvalidOperationException: NameIdentifier claim is missing` from `GetUserId()` (500). `TestAuthHandler` issues only Email and Name. Fix needs a NameIdentifier claim equal to a real user id and a seeded User row in each of the 8 suites' temp databases. Anchor seed-0123.
- **F6 [MEDIUM]** "No change under `src/`" contradicts Step 2's `git rm` of `deploy.zip` and `reflekt.db` (both under `src/`). Fix: exclude those paths. Anchor seed-0130.
- **F8 [MEDIUM]** Owner's real email in `ai-coding/plans/Auth/done.md:36` and `ai-coding/plans/Auth/plan.md:233` (`Auth:AllowedEmails:0` example); no step or grep covers it. Fix: placeholder + an email grep. Anchor seed-0120.
- **F9 [MEDIUM]** The commit carrying `deploy.zip` (`a9b9177`) is reachable from `origin/main`; the plan never says the secret must be rotated; the `pre-cleanup` tag points at unscrubbed history. Fix: owner actions outside F33 — rotate, publish only the scrubbed copy, never push `pre-cleanup` publicly. Unranked; untouchable: security.
- **F7 [LOW]** `docs/conventions/testing.md:67` (`client/src/**/*.spec.ts`) and `project-rules.md:10-12` missed; Step 4's second grep covers only `project-rules.md`. Fix: widen to `docs/conventions`. Anchor seed-0153.
- **F10 [LOW]** Step 3's all-files zero cannot hold at its own boundary: `docs/deployment-azure.md` paths are edited in Step 5. Fix: move them to Step 3. Anchor seed-0137.
- **F11 [LOW]** Step 5 grep misses `--name reflekt` and `reflekt-plan` (`docs/deployment-azure.md:29,38,41,137,145,157`). Anchor seed-0127.
- Gaps (ungraded): no route for a real app bug in Step 1 (recommend pause + separate item); the audit hook stays tracked while its wiring lives in the untracked local settings; the `publish/` ignore rule already exists.

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | seed-0120 | Step 3 greps match the plan's own text | self-matching acceptance | check can't reach zero, or names ship | plan lines 16, 68–70 | privacy | acceptance | plan Step 3, Context | codex H3, sonnet F1 |
| R1-02 | codex, sonnet | HIGH | seed-0130 | `src/` invariant contradicts Step 2 deletions | contradictory gate | close cannot pass | Step 2 `git rm` under `src/` | — | acceptance | Step 1, Must-NOT-Change | codex H1, sonnet F6 |
| R1-03 | codex, sonnet | HIGH | seed-0120 | Owner's personal email in two old plans, no step | missed leak | personal data published | `ai-coding/plans/Auth/{done.md:36,plan.md:233}` | privacy | leak | Step 3 | codex H2, sonnet F8 |
| R1-04 | codex | HIGH | seed-0130 | verify-run calls omit `--slug` (exit 2) | broken command | gate never runs | runner preflight | — | acceptance | Step 1, Must-NOT-Change | codex H4 |
| R1-05 | sonnet, codex | HIGH | seed-0120 | Frontend build red at baseline; no frontend gate | missed red baseline | deploy script cannot deploy; green proof is partial | 8 vue-tsc errors; `npm ci` lock drift | — | scope | Context, verify roles, Step 6 | sonnet F3, codex M (frontend gate) |
| R1-06 | codex, sonnet | MEDIUM | seed-0123 | Step 1 cause misstated (missing NameIdentifier claim + owner FK) | wrong diagnosis | developer chases the wrong fix | `ClaimsPrincipalExtensions.cs:9`, `TestAuthHandler` | — | diagnosis | Step 1 | codex M, sonnet F5 |
| R1-07 | codex, sonnet | MEDIUM | seed-0130 | `git check-ignore -q` multi-path exits 128; misses `.db-shm/-wal`, markers | broken command | check cannot pass | git error text | — | acceptance | Step 2 | codex M, sonnet F2 |
| R1-08 | codex, sonnet | MEDIUM | seed-0153 | grep inventory misses F31 spec:13, ExceptionInfrastructure plan:5, project-rules:10-12, testing.md:67 | incomplete list | partial cleanup | greps | privacy | inventory | Steps 3, 4 | codex M×2, sonnet F7 |
| R1-09 | codex, sonnet | MEDIUM | seed-0127 | deploy accept does not prove parameterization (`--name reflekt`, `reflekt-plan`, unused param) | weak acceptance | owner's names survive | `deploy.ps1`, doc lines 29–157 | — | acceptance | Step 5 | codex M, sonnet F11 |
| R1-10 | sonnet | MEDIUM | seed-0130 | Step 5 backslash regex never matches | vacuous check | passes before any edit | PowerShell run | — | acceptance | Step 5 | sonnet F4 |
| R1-11 | sonnet | MEDIUM | unranked — no anchor fits | Secret reachable from `origin/main`; no rotation action | missing owner action | live credential exposure | `git branch -r --contains a9b9177` | security | owner-action | Context | sonnet F9 |
| R1-12 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Vulnerable packages (AngleSharp 1.2.0, SQLitePCLRaw 2.1.10) have no disposition | missing triage | known-vulnerable deps in a teaching repo | advisories GHSA-pgww-w46g-26qg, GHSA-2m69-gcr7-jv3q | security | scope | Directory.Packages.props | codex M |
| R1-13 | sonnet | LOW | seed-0137 | Step 3 all-files zero vs Step 5 doc edits | step ordering | Step 3 cannot close alone | plan Steps 3/5 | — | ordering | Steps 3, 5 | sonnet F10 |
| R1-14 | codex | LOW | unranked — no anchor fits; Codex severity LOW | README accept checks keywords only | weak acceptance | runnable-setup promise unproven | Step 6 | — | acceptance | Step 6 | codex L |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-13)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | — | Context (names paraphrased), Step 3 (patterns written with `[x]` character classes so they never match themselves; delivery artifacts must not quote the names) |
| R1-02 | fixed-real | HIGH | — | Step 1 Accept, Must-NOT-Change row 1 (pathspec exclusions for the two deleted binaries; `web/` allow-list) |
| R1-03 | fixed-real | HIGH | — | Step 3 (email list + `gmail\.com` grep) |
| R1-04 | fixed-real | HIGH | — | Step 1 Accept, Step 7, Must-NOT-Change (`--profile complete --slug F33-PublishCleanup --tree`) |
| R1-05 | fixed-real | HIGH | — | new Step 2 (frontend build green: delete dead `TeamSelector.vue`, fix `previousActions.ts:52-53`, sync `package-lock.json`); verify `full` role gains the frontend build; Decisions |
| R1-06 | fixed-real | MEDIUM | — | Step 1 known causes |
| R1-07 | fixed-real | MEDIUM | — | Step 3 (was 2) Accept: per-path loop incl. `.db-shm/-wal` and markers |
| R1-08 | fixed-real | MEDIUM | — | Steps 4, 5 (acceptance greps are the enumeration; widened to `docs/conventions`) |
| R1-09 | fixed-real | MEDIUM | — | Step 6 (was 5) Accept |
| R1-10 | fixed-real | MEDIUM | — | Step 6 Accept regex |
| R1-11 | fixed-real | MEDIUM | — | Context "Owner actions outside F33" |
| R1-12 | plan-must-resolve | MEDIUM | — | Out of scope → F34 dependency triage (backlog row 34 amended); package upgrades change runtime and are not cleanup |
| R1-13 | fixed-real | LOW | — | Step 4 (deployment-doc machine paths moved into the private-reference step) |
| R1-14 | fixed-real | LOW | — | Step 7 (was 6) content checklist for the code reviewer |

Reconciled: Context, Scope, Skill Mapping, Steps 1–8 renumbered (new Step 2), Must-NOT-Change, Decisions, backlog row 34; verify `full` role text in Step 8.
