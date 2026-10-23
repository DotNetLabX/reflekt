## Step 1 — Done-Check

### Check 1 — 2026-10-23 — after the build

**Model:** claude-opus-5-5[1m]

Pre-commitment predictions: (1) Step 3's `create-architecture-doc` absent from the skill log window; (2) a grep accept that does not hold on the live files (v1.md edits beyond annotations, AC3, the frontend-test-runner word); (3) an unblocked `test-entry` line. Found: the skill is logged inside Step 3's window; every accept re-run below holds; both `test-entry` lines carry `blocked: true`.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Base commit and fact sheet | Implemented | F1–F17 each with its command or file; base `e5b43ce`, HEAD `82a791f`. Spot-checked: 44 `*Endpoint.cs`, 16 migrations, interceptor awaits `PublishInOrderAsync` (P1 holds) |
| 2 — Run and record `dotnet ef` | Deviated (valid reason) | Plan item 3 (retry with placeholder credentials) is conditional on a host-start failure; none failed. Six commands with exit codes and output, three findings, before/after `--ignored` listings recorded; `obj/` gone (`ls obj` → no such file) |
| 3 — `architecture.md` (new, as-built) | Deviated (valid reason) | A third inventory label, **Workflow**, for the 10 pipeline/process skills, which neither plan label fits; all 37 skills listed (16 + 11 + 10). 16 `## ` headings (inventory, §§ 1–14, Gaps); the four names grep hits; the only allowlist hit is line 303, "No allowlist exists"; per-section commit/how table and the 11 "not used by this app" entries recorded |
| 4 — One-page map and `v1.md` | Implemented | `index.md` `^@` → `56:@architecture.md` only, `v1.md` a plain link; `v1.md:3` superseded; diff 16/16 lines — status line plus same-line annotations, original text kept (line 210 checked against `e5b43ce`) |
| 5 — Root `CLAUDE.md` | Implemented | AC1 one hit, line 127, inside `### Not used by this app`; `5:## Authority & Spec Files`; 6 guardrails + "No secrets" (163–169, god folders qualified); events rule (20) and exception (36); prefix 40 / 4001; root-relative `dotnet ef` and global install line; Step 2 design-time findings; AC3 no hits |
| 6 — `project-rules.md`, service `CLAUDE.md` | Implemented | AC1 none; events-rule grep none; `run lint` line 41; frontend-test-runner word 0; `SprintIssueSyncService`/`docker-compose` none; validator in `*Command.cs`/`*Query.cs`; `40[0-9]{2}` none in service `CLAUDE.md`, service index 1 with pointer; four building blocks, schemes, transports corrected |
| 7 — Sign-in in the product doc | Implemented | `product/v1.md:532`, `:545` state no allowlist; AC2 listing with dispositions in `implementation.md`; backlog hits covered by the change note |
| 8 — `testing.md`, `coding-conventions.md` | Implemented | Four `^@` imports incl. `@testing.md`; all eight files named; `simple CRUD`/`analytics`/`_sut`/`NSubstitute` none; helpers, FluentAssertions 7.2.0 with release-notes reason, `Speed` filter, frontend not set up, coverage section present; example list recorded |
| 9 — `known-deviations.md` | Implemented | `## Accepted deviations` (line 20), 18 rows; `useTimerActions` line 39; foreign-id grep none; `fire-and-forget` none; F39 candidates recorded with row/no-row |
| 10 — `ef-core.md`, `vue.md`, `csharp.md` | Implemented | `<style>` `vue.md:25`, inline-style grep none; "not found" only `csharp.md:73`, as an exception; design-time advice from Step 2; example list recorded |
| 11 — `.claude/README.md`, `kb-entry-schema.md` | Implemented | `(37 skills)` = 37 folders; `never created` footnote in both files; hooks and `verify.json` listed (56–57); stale-agent-files note (61); "3 files" row updated (85) |
| 12 — Small fixes and backlog change note | Implemented | `Partially shipped` in `product/index.md:4`, `v2.md:3`; `README.md:102` links `architecture/index.md`, line 98 corrected; `change-notes.md` Note 1 with exact text, owed at merge; `git diff --name-only e5b43ce -- docs/backlog.md` empty |
| 13 — Regenerate the code graph | Deviated (valid reason) | Primary path, exit 0, built from `82a791f8`. The plan's `grep -c '"file_type": "code"'` counts lines and returns 0 on the compact `graph.json`; counted with `grep -o … \| wc -l` and a JSON parse instead (2267). AC6 checks and the document-node observation recorded |
| 14 — Acceptance run and change-set check | Deviated (valid reason) | AC1–AC8 recorded with commands; the frontend-test-runner word grepped as `vi[t]est` because the test-entry gate blocks the plain word (same match). AC7 re-run here: every changed or untracked path is `*.md`, under `graphify-out/`, or a campaign file the plan's Decisions exclude (`.claude/.personas.json`, `.claude/tmp/`); no `obj/`, no `docs/pending-decisions/` |

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist (spec `Rule list: not applicable — documentation`; no `spec-rules.md`)

**Skill log:** `.claude/audit/skill-invocations.log` (worktree home; the main checkout's copy has no entry after 2026-10-13). Scoped by token `developer:implement` + session `db12ad19-bfc3-421e-aaac-1387ea3b0ea0`: one entry, `create-architecture-doc` at 18:30:46Z, inside Step 3's window (Step 2 hand-back 18:30:37Z → Step 3 hand-back 18:34:44Z, `handoff.jsonl`, agent `a987a1ecc61e6c876`). Every other step maps `None`, TDD `no` throughout — no slip.

**Test-entry log:** `.claude/audit/violations.log` holds two `test-entry` lines in the window (18:40:21Z, 18:55:23Z), both `blocked: true` — corrected attempts, no slip.

Notes: `Satisfies:` referents (AR-, CV-, TS-, BE-, SK-, DP-, FE- ids; Acceptance 1–8) exist in the spec. `## Decisions` (8 rows) and `## Decisions and why` (P1–P4) present. `docs/specs/F40-ArchitectureDocs/definition/spec.md` appears in `git diff e5b43ce` from the plan commit `82a791f`, not from the build (`git diff --name-only 82a791f` omits it). The backlog parts of Acceptance 2 and 8 stay owed at merge (P2). Carry-over rows in `implementation.md` are the reviewer's and the close's.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-23*
