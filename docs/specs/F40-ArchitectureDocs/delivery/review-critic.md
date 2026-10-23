## Round 1 — 2026-10-22 — spec — primary (codex)

Header note: two readers — Codex primary (read-only job, verdict persisted verbatim in `review-critic-codex.md`) and a Sonnet nexus critic as second reader; pair `codex + sonnet`, picked by the owner. The spec had been committed as Ready without review; the owner caught it and it was set back to Draft before this round.

Primary reader: see `review-critic-codex.md` — `## Verdict: NO-GO`, 2 HIGH, 7 MEDIUM, 4 LOW.

## Second reader — round 1 (sonnet)

Verdict: REVISE. (Condensed by the architect, not verbatim: every finding is kept with its level, evidence and fix; the reader's full message is in the session transcript.)

- **[HIGH, band HIGH–MEDIUM] F40-01 — The mandated skill cannot produce the required doc.** `create-architecture-doc` writes `{version}.md`, forbids sections beyond its template and implementation detail outside Gaps, and inventories ~10 template-only skills. Fix: say what the skill governs, name the file, how template skills show, record the departure.
- **[MEDIUM, band MEDIUM–HIGH] F40-02 — Import or link from `index.md` unspecified;** four agents load it at start-up; agent files still call `v1.md` the architecture.
- **[MEDIUM] F40-03 — Live docs F36/F38 make false have no row:** `vue.md` White Flash; `src/Services/Reflekt/CLAUDE.md`.
- **[MEDIUM] F40-04 — `project-rules.md` has no row;** `docker-compose.*`, validator wording, `dotnet ef` paths, `npx vitest run`, "No secrets" home.
- **[MEDIUM] F40-05 — Acceptance 2 passes while the allowlist is still documented** (`docs/product/v1.md` prose; `docs/backlog.md` outside the path).
- **[MEDIUM] F40-06 — Remaining acceptance checks prove presence, not accuracy;** graph check date-only; `git diff --stat` misses untracked files.
- **[MEDIUM] F40-07 — `docs/backlog.md` stale content outside section A** (preamble `CardVote`, allowlist in B and C).
- **[MEDIUM] F40-08 — `known-deviations.md` purpose and schema vs design decisions; BE-24 recorded twice; drift input unnamed.**
- **[MEDIUM] F40-09 — Unverified audit claims become outcomes** (`dotnet ef` host-boot; FluentAssertions licence); `ef-core.md` design-time factory advice contradicts.
- **[MEDIUM] F40-10 — `.claude/README.md` design-decision row broken by F40's own edits;** AR-27 hooks/verify.json listing unassigned.
- **[LOW] L1 — Small loose ends:** "eight files" self-count; README line 98; `v2.md` status; `kb-entry-schema.md`.
- **[LOW] L2 — Graph regeneration mechanics** (`graphify` at `D:\tools\graphify-venv`; `--force`; honours `.gitignore`). Route: plan must resolve.
- **[LOW] L3 — Dependencies omit F35/F36; Acceptance 3 would fail on a sentence like "no NSubstitute".** Route: plan must resolve.

Coverage ledger (second reader): AR/TS/DP/CV/BE mapping, graph, sibling specs, code facts COVERED; architecture-doc row, map, root CLAUDE.md, project-rules, sign-in, conventions, agent setup docs, small fixes PARTIAL; `docs/product/v1.md`, `docs/architecture/v1.md`, `csharp.md` in full NOT OPENED.

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | sonnet | HIGH | seed-0004 | Mandated skill's template forbids the required content | two instructions, no precedence | thin doc or a broken skill rule | quoted | — | scope | Scope architecture row | F40-01 |
| R1-02 | codex, sonnet | HIGH | contested: HIGH / MEDIUM — settled HIGH: agents would keep recreating the F36 defect | `vue.md` contradicts F36; service `CLAUDE.md` stale; no owner | scope gap | conventions prescribe a removed defect | quoted | — | sibling | Scope | codex-1, F40-03 |
| R1-03 | codex, sonnet | HIGH | contested: HIGH / MEDIUM — settled HIGH: a live rule contradicts the adopted convention | `csharp.md:73` contradicts F38 | conflicting live rules | new code violates a rule | quoted | — | sibling | Scope conventions row | codex-2, F38-09 |
| R1-04 | sonnet | MEDIUM | seed-0022 | `index.md` import vs link; agent pointers to `v1.md` | two plausible builds | agents lose context or pay for it | path | — | scope | Scope map row | F40-02 |
| R1-05 | sonnet | MEDIUM | seed-0022 | `project-rules.md` has no row | ambiguous edit scope | wrong commands survive | quoted | — | scope | Scope | F40-04 |
| R1-06 | codex, sonnet | MEDIUM | seed-0310 | Allowlist acceptance misses prose and the backlog | acceptance does not prove outcome | stale sign-in guidance passes | path | — | acceptance | AC2 | codex-5, F40-05 |
| R1-07 | codex, sonnet | MEDIUM | seed-0310 | Accuracy, graph and diff checks prove presence only | acceptance does not prove outcome | stale doc set passes | scenario | — | acceptance | AC5–AC7 | codex-4, codex-8, codex-9, F40-06 |
| R1-08 | codex, sonnet | MEDIUM | seed-0023 | Backlog stale content outside section A | scope narrower than finding | stale decisions read as current | path | — | scope | Scope small fixes | codex-3, F40-07 |
| R1-09 | sonnet | MEDIUM | seed-0022 | `known-deviations.md` purpose/schema; BE-24 twice; drift input | ambiguous deliverable | wrong kind of rows | quoted | — | scope | Scope conventions row | F40-08 |
| R1-10 | sonnet | MEDIUM | seed-0130 | Unverified `dotnet ef` and licence claims; `ef-core.md` advice | unverified claim as fact | wrong instructions to agents | quoted | — | scope | Scope CLAUDE.md, testing rows | F40-09 |
| R1-11 | sonnet | MEDIUM | seed-0023 | `.claude/README.md` row broken by F40; hooks/verify.json listing | self-inflicted drift | README documents removed duplication | quoted | — | scope | Scope agent docs row | F40-10 |
| R1-12 | codex | MEDIUM | unranked — Codex severity MEDIUM | Naming files ≠ importing `testing.md` | acceptance does not prove outcome | `testing.md` stays orphaned | path | — | acceptance | AC4 | codex-6 |
| R1-13 | codex | MEDIUM | unranked — Codex severity MEDIUM | Foreign-example grep misses known examples | weak acceptance | foreign examples survive | path | — | acceptance | AC3 | codex-7 |
| R1-14 | codex, sonnet | LOW | seed-0127 | `v2.md` status; README line 98; `kb-entry-schema.md`; "eight files" | missed stale line | small stale statements | path | — | scope | Scope small fixes | codex-10, codex-13, L1 |
| R1-15 | codex | LOW | unranked — Codex severity LOW | Test naming presented as universal | inaccurate claim | doc contradicts 41 tests | path | — | scope | Scope testing row | codex-11 |
| R1-16 | codex | LOW | unranked — Codex severity LOW | High-severity count misattributed to `v1.md` | inaccurate count | — | path | — | count | Decisions | codex-12 |
| R1-17 | sonnet | LOW | seed-0364 | Graph mechanics; dependencies; negative-grep wording | plan must resolve | — | path | — | plan | Scope graph row, Dependencies, AC3 | L2, L3 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-22)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | Scope architecture row; Decisions row 4; known-deviations accepted table |
| R1-02 | fixed-real | — | — | Scope `vue.md`/`csharp.md` row; service `CLAUDE.md` row |
| R1-03 | fixed-real | — | — | Scope `vue.md`/`csharp.md` row; F38 Decisions row 1 |
| R1-04 | fixed-real | — | — | Scope map row (`@architecture.md`); agent docs row (pointer note); AC4 |
| R1-05 | fixed-real | — | — | Scope `project-rules.md` row; CLAUDE.md row ("No secrets" moves) |
| R1-06 | fixed-real | — | — | Scope sign-in row; AC2 |
| R1-07 | fixed-real | — | — | Purpose; AC5–AC7 |
| R1-08 | fixed-real | — | — | Scope small fixes (everything above item 23 marked historical) |
| R1-09 | fixed-real | — | — | Scope other-conventions row (two tables; BE-24 in CLAUDE.md only; F38/F39 drift lists as input) |
| R1-10 | fixed-real | — | — | Scope CLAUDE.md row (run and record `dotnet ef`); testing row (licence cited or omitted); other-conventions row (`ef-core.md` advice) |
| R1-11 | fixed-real | — | — | Scope agent docs row |
| R1-12 | fixed-real | — | — | Scope testing row; AC4 |
| R1-13 | fixed-real | — | — | AC3 |
| R1-14 | fixed-real | — | — | Scope small fixes; agent docs row (`kb-entry-schema.md`); testing row (files named) |
| R1-15 | fixed-real | — | — | Scope testing row (short form named) |
| R1-16 | fixed-real | — | — | Decisions row 3 ("ten") |
| R1-17 | fixed-real | — | — | Scope graph row (`--force`, install path); Dependencies (F35–F39); AC3 grep set |

Reconciled: Scope map row ↔ AC4 ↔ agent docs row; Scope CLAUDE.md row ↔ project-rules row ↔ What must not change (guardrails moved, not dropped); Scope other-conventions row ↔ F35 (team-count race) ↔ F36 Decisions row 3 (skill drift) ↔ F38 Decisions row 6; Scope `vue.md`/`csharp.md` row ↔ F36 Scope FE-05 ↔ F38 Decisions row 1.

## Round 2 — 2026-10-23 — spec — primary (sonnet)

Header note: the Codex round-2 job was lost (`collect`: "cancel failed: No job found", read error to the deadline), so by the fallback's late rule a fresh Sonnet nexus critic read the round alone as its main (author Opus → pair 3's main). Read: full — the slug's number (40) is divisible by 5. Floor: MEDIUM. Codex dropped: job record not found.

Verdict: ACCEPT. (Condensed by the architect, not verbatim; the reader's full message is in the session transcript.) Round-1 fold verification: R1-01…R1-05 and R1-07…R1-17 closed; R1-06 partial (AC2 fails on correct text). New findings at the floor:

- **[MEDIUM] R2-01 — AC2 fails on correct text, incl. live backlog item 35** ("allowlist is **not** enforced"); new docs saying "no allowlist" would trip it. Tag: fold.
- **[MEDIUM, band MEDIUM–LOW] R2-02 — AC6 "no allowlist node"** is unsatisfiable at the `graph.json` level: a forced rebuild turns evidence-doc headings into document nodes. Tag: fold.
- **[MEDIUM, band MEDIUM–LOW] R2-03 — Most Scope rows, incl. both round-1 HIGH fixes, have no acceptance line** (`vue.md`, `csharp.md`, `dotnet ef` run, inventory, known-deviations, project-rules, service CLAUDE.md, `.claude/README.md`, `v1.md` status, product status, README, backlog note). Tag: fold.
- **[MEDIUM] R2-04 — `project-rules.md` row leaves the unqualified domain-events line, template "modules" lines, and misses F39's lint step.** Tag: fold.
- Below floor (recorded): drift-input locations; `.claude/README.md` never mentions `skill-backlog.md`; graph-fallback wording; README lint command; frequency column; parked item 22; redaction not named in the architecture sections.

Coverage ledger (this reader): finding mapping, hand-offs, architecture-doc skill, root and service CLAUDE.md, conventions files, sign-in, agent docs, graph (scratch `graphify update --force` run), small fixes COVERED; code facts PARTIAL; `product/v1.md` outside 525–548, `architecture/v1.md` body, `bugs.md`, `deployment-azure.md` NOT OPENED.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | sonnet | MEDIUM | seed-0022 | AC2 trips on accurate "no allowlist" text | acceptance contradicts live text | check cannot pass honestly | quoted | — | fold | AC2 | R2-01 |
| R2-02 | sonnet | MEDIUM | seed-0022 | AC6 negative node check unsatisfiable on graph.json | acceptance unsatisfiable | good graph deleted | scenario | — | fold | AC6 | R2-02 |
| R2-03 | sonnet | MEDIUM | seed-0022 | Scope rows without acceptance lines | acceptance does not prove outcome | edits skipped with checks green | quoted | — | fold | AC | R2-03 |
| R2-04 | sonnet | MEDIUM | seed-0022 | project-rules.md events line, modules lines, lint step | conflicting live rules | reviewer flags legitimate events | quoted | — | fold | Scope project-rules row | R2-04 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | AC2 (statements that no allowlist exists allowed) |
| R2-02 | fixed-real | — | — | AC6 (report god nodes and communities; code nodes; evidence document nodes expected) |
| R2-03 | fixed-real | — | — | AC8 |
| R2-04 | fixed-real | — | — | Scope project-rules row |
| below floor | fixed-real (cheap) | — | — | testing row (F37 tooling); graph fallback wording; backlog note excludes parked item 22; AC7 base commit |

Reconciled: Scope project-rules row ↔ CLAUDE.md row (events rule lives in CLAUDE.md only) ↔ AC8; AC2 ↔ sign-in row ↔ backlog item 35; AC6 ↔ graph row. No round 3: round 2 holds no CRITICAL.

## Round 1 — 2026-10-23 — plan — primary (codex)

Header note: primary is Codex (job task-mvl8l3zh-gt1bux, completed, NO-GO); its verdict is persisted verbatim in `review-critic-codex.md` § Plan review — round 1. Second reader: nexus critic on Sonnet (ACCEPT). Persisted and merged by the team lead; the fold is the architect's.

Codex findings (verbatim, see review-critic-codex.md): M1 Step 2 `--tool-path` install leaves `dotnet ef` uninvokable; M2 Step 3 omits AR-16 / § 3.8 (OpenGraph detection, Jira bypass, background fetch); M3 Step 8 lacks TS-09 replacement coverage guidance and leaves the CRUD-skip guidance; M4 Step 9 F39 scan misses the `useTimerActions` composable-toast exception (F39 impl:460) and fix-round records; L1 Step 3 has no `Departs from:` row for the `create-architecture-doc` template; L2 P1 overstates "handlers get CancellationToken.None" (handlers get linked tokens).

## Second reader — round 1 (sonnet)

Verdict: ACCEPT — 3 MEDIUM, 4 LOW (relayed message, summarised by finding; full text in the team lead's session transcript):
- [MEDIUM] F40-01 (seed-0364) Step 3's section list has no home for AR-16 (OpenGraph/source detection/Jira short-circuit), AR-19 (package table) or § 3.8 (pipeline order, options, singletons); its Accept passes without them. Fix: name sections for them.
- [MEDIUM] F40-02 (seed-0129) Step 9 scans F39's Key Decisions/Deviations only; F39's skill difference sits in `## Skills Used` row 4 / Step 4 (`apiFetch` built in `src/api/client.ts`, create-vue-feature skill cites an absent file). Fix: scan the whole record incl. per-step Skills lines; name `apiFetch` as a starting row.
- [MEDIUM] F40-03 (seed-0130) Step 4 says "No other edit" but `v1.md:182` (§ 6 Teams: "Allowlist check.") lies outside the annotated regions while Accept requires every allowlist hit be marked superseded. Fix: annotate it, or state the status line covers the file and reword the accept.
- [LOW] F40-04 (seed-0364) Must-NOT-Change row 6 (events rule in CLAUDE.md only) claims "pinned" by checks that don't test it. Fix: add a grep or mark "by inspection".
- [LOW] F40-05 (seed-0153) Step 13 / Decisions row 6 claim graph document nodes reflect final docs; `graphify update` re-extracts code only. Fix: drop the claim; optional stale-node check.
- [LOW] F40-06 (seed-0364) AC3's "reviewed and listed in implementation.md" half has no producing step. Fix: add the listing to Step 8/10 and Step 14.
- [LOW] F40-07 (seed-0137) Header says 4 files (≈20 change); service CLAUDE.md port note ambiguous vs root rule "services don't duplicate port numbers". Fix: correct count; service file names only index 1 and points to the root table.
Gap-analysis notes: learner.md says "CLAUDE.md is stack-agnostic" (Step 11 could mention); Step 10's negative grep `inline .*style.*body` could match its own explanation; AC7 "only *.md" vs close-time json files in the feature folder. Open questions: design-time DB path relative to cwd (use `git status --porcelain --ignored` before/after); whether graphify regenerates community labels.

### Merge — round 1

Coverage: incomplete — the second reader opened F34 `backend.md`/`frontend.md`/`skills.md`, `src/`, `tests/` and `web/reflekt-web/src` by grep/listing only; Codex returned no coverage ledger.

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | MEDIUM | seed-0364 | Step 3 has no section for AR-16, AR-19, audit § 3.8 | requirement in Satisfies with no step content | as-built doc omits evidence-link fetch pipeline, package table, pipeline order and still passes its accept | quoted | — | coverage | plan § Step 3; spec Scope row 1; F34 architecture.md § 3.8 | codex M2, F40-01 |
| R1-02 | codex, sonnet | MEDIUM | seed-0129 | Step 9 F39 scan misses recorded skill/code differences | source consulted in wrong place | deviations table says "none listed" while F39 records `apiFetch` and the `useTimerActions` toast exception | quoted | — | coverage | plan § Step 9; F39 implementation.md Skills Used, Step 4, line 460, fix rounds | codex M4, F40-02 |
| R1-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | `--tool-path` install leaves `dotnet ef` uninvokable | missing invocation detail | the required design-time run cannot execute as written | quoted | — | mechanism | plan § Step 2 | codex M1 |
| R1-04 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | TS-09 replacement coverage guidance absent | spec row partly unplanned | testing.md keeps CRUD-skip guidance contradicting practice | quoted | — | coverage | plan § Step 8; F34 deps-tests-conventions TS-09 | codex M3 |
| R1-05 | sonnet | MEDIUM | seed-0130 | Step 4 "No other edit" vs its accept on v1.md:182 | step text contradicts accept | allowlist line left unmarked, or step limit broken | quoted | — | contradiction | plan § Step 4; v1.md:182 | F40-03 |
| R1-06 | codex | LOW | unranked — no anchor fits; Codex severity LOW | no `Departs from:` row for create-architecture-doc template | missing departure row | departure recorded only in narrative | quoted | — | plan grammar | plan § Step 3 | codex L1 |
| R1-07 | codex | LOW | unranked — no anchor fits; Codex severity LOW | P1 overstates CancellationToken.None for handlers | overstated claim | docs state a guarantee handlers don't get | quoted | — | accuracy | plan P1 | codex L2 |
| R1-08 | sonnet | LOW | seed-0364 | Must-NOT-Change row 6 pinned by no check | invariant claimed pinned without check | duplicate events rule could pass | quoted | — | verification | plan Must-NOT-Change; Step 6 | F40-04 |
| R1-09 | sonnet | LOW | seed-0153 | graph-last rationale wrong (doc nodes not refreshed) | rationale contradicts tool | stale doc nodes unchecked | path | — | accuracy | plan Decisions row 6; Step 13 | F40-05 |
| R1-10 | sonnet | LOW | seed-0364 | AC3 "reviewed and listed" has no producer step | acceptance clause with no producer | list never written | quoted | — | coverage | spec AC3; plan Steps 8, 10, 14 | F40-06 |
| R1-11 | sonnet | LOW | seed-0137 | header file count wrong; service port note ambiguous | lookup-cost inconsistency | misleads the builder | quoted | — | clarity | plan header; Steps 5, 6 | F40-07 |

### Fold — round 1 (architect, claude-opus-5-5[1m], 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | Step 1 (evidence-link pipeline and packages listings); Step 3 (three sections: evidence-link fetch pipeline AR-16, cross-cutting § 3.8, packages AR-19; accept grep for `EvidenceLinkSourceDetector`, `JiraDomain`, `Directory.Packages.props`, `UseReflektApi`) |
| R1-02 | fixed-real | — | — | Step 9 (the whole F39 record read; `useTimerActions` toast and `src/api/client.ts`/`apiFetch` named as starting rows; candidates recorded in `implementation.md` § Step 9; accept grep `useTimerActions`) |
| R1-03 | fixed-real | — | — | Step 2 (invoke `obj/dotnet-ef/dotnet-ef.exe`; `--version` recorded; accept) |
| R1-04 | fixed-real | — | — | Step 8 (TS-09 coverage rewording; CRUD-skip line removed; accept grep `simple CRUD|analytics`) |
| R1-05 | fixed-real | — | — | Step 4 (v1.md § 6 Teams row, line 182, annotated with § 7.1; grep before editing; "no edit beyond") |
| R1-06 | fixed-real | — | — | Decisions (new untagged row: spec section list and "not used by this app" disposition, required by spec Decisions row 4; why the `Departs from` tag does not apply without `**Mirrors:**`) |
| R1-07 | fixed-real | — | — | P1 (publication token vs handler token; amendment noted); Step 1 events bullet; Step 3 events section |
| R1-08 | fixed-real | — | — | Must-NOT-Change row 6 and Step 6 accept (grep `carr(y|ies) .*reference|not recoverable` on `project-rules.md`, which hits today's line 63, so the gate is real); Step 5 accept (events rule and exception listed) |
| R1-09 | fixed-real | — | — | Decisions row 6 and Step 13 (rationale is the fallback's file order; document-node refresh recorded, not assumed) |
| R1-10 | fixed-real | — | — | Step 8 and Step 10 (code-example lists in `implementation.md`); Step 14 (AC3 review half gathered, `project-rules.md` included) |
| R1-11 | fixed-real | — | — | Header Size line (the size rule's exclusions and the four counted files named; about 16 `docs/` files noted); Step 6 (service `CLAUDE.md` records index 1 and points to the root port table; accept grep `40[0-9]{2}` → no hit) |
| gap notes (below the merge) | fixed-real (cheap) | — | — | Step 11 (`learner.md` line 68 "stack-agnostic" named in the `.claude/README.md` note); Step 10 (the vue negative grep now matches today's wording "use inline `style`", not its own explanation); Step 14 (close-time `campaign-close.json`/`usage-record.json` out of the build-time AC7 check); Step 2 (design-time DB check by `git status --porcelain --ignored` before/after and the `Data Source` printed) |

Reconciled: Step 1 ↔ Step 3 (each new section has its fact listing); Step 3 ↔ Step 9 ↔ Decisions new row (template departure: plan row, accepted-deviation row, spec Decisions row 4); P1 ↔ Step 1 ↔ Step 3 ↔ Step 9's P1 deviation row (token wording identical in substance; the deviation row speaks only of failures being logged); Step 4 ↔ its accept (every allowlist hit in `v1.md` annotated) ↔ AC2; Step 5 accept ↔ Step 6 accept ↔ Must-NOT-Change row 6 (rule present in `CLAUDE.md`, absent from `project-rules.md`); Step 6 service-doc text ↔ root port rule in Step 5; Steps 8, 10 ↔ Step 14 ↔ spec AC3; Step 13 ↔ Decisions row 6 ↔ AC6; Step 14 ↔ AC7 ↔ Decisions row 5 (campaign files) and the new close-files clause; header Size ↔ the architect's size rule (docs/ and generated files not counted).

## Round 2 — 2026-10-23 — plan — primary (codex)

Header note: Codex alone (round ≥ 2 has no second reader), job task-mvl90l8e-3b0rfx, NO-GO. Floor MEDIUM, Read: full (round 1 coverage incomplete). Verbatim in `review-critic-codex.md` § Plan review — round 2. Fold verification: R1-01…R1-11 all closed; no MEDIUM+ contradiction introduced by the fold. Two new findings.

### Merge — round 2

Coverage: complete (single reader, full read; Codex returned no ledger — taken as full per its fold-verification table over the whole plan).

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Step 1 extracts endpoints "from each Configure()"; code has 44 attribute-declared endpoints, zero Configure() | wrong extraction source | endpoint inventory cannot be produced as prescribed; later slices reuse it | path | — | round-1 miss: endpoint inventory | plan.md:66; Features/**/*Endpoint.cs | codex R2-01 |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Step 1 enumerates literal SendAsync("…") names — misses CardCreated/CardUpdated sent via CardBroadcast helper | incomplete enumeration method | real-time doc can omit two central messages and still match its fact sheet | path | — | round-1 miss: real-time message inventory | plan.md:67; CardBroadcast.cs:19; useSprintHub.ts:65 | codex R2-02 |

### Fold — round 2 (architect, claude-opus-5-5[1m], 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | Step 1 API bullet (endpoints listed from their class attributes — `[Http*("…")]`, `[Authorize(Policy = …)]` / `[AllowAnonymous]` — over `Features/**/*Endpoint.cs`; count checked against the `*Endpoint.cs` classes; `Configure()` named as not used — `grep -rln "Configure()" Features` → 0 files) |
| R2-02 | fixed-real | — | — | Step 1 Real-time bullet (server names from `SendAsync`/`SendCoreAsync` including those reaching it through a helper such as `CardBroadcast` or a constant, reconciled with the client's `connection.on("…")` registrations in `useSprintHub.ts`; one-sided names listed with why) |

Reconciled: Step 1 API bullet ↔ Step 3 feature-slices section (verb, route, policy per endpoint) ↔ spec AC5 (counts match the listing); Step 1 Real-time bullet ↔ Step 3 real-time section ↔ AC5; no other clause names the endpoint or message extraction method (`grep -n "Configure()\|SendAsync" plan.md` → only the two folded bullets). Lane re-derived after the last fold: 14 developer-built steps, 4 counted files, no fullChecks list — still `full`, build lane unchanged; no tagged (`Adds:` / `Departs from`) rows.
