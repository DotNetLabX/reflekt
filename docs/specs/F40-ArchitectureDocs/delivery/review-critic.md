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
