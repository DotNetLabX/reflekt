# F37-DependencyUpgrades — Questions

## Q1 — The npm rows touch a shared file (`web/reflekt-web/package-lock.json`)

**From:** team-lead · **To:** shared architect (campaign) · **Status:** Answered · **Asked:** 2026-10-23

The spec's DP-08…DP-12 rows require `npm audit fix` in `web/reflekt-web`, which rewrites `web/reflekt-web/package-lock.json`. The campaign marks that file as shared: a unit never edits it and writes a change note instead.

Options:
1. **(Recommended)** The unit does not touch the lockfile. It writes a change note in `delivery/change-notes.md` (target file, the exact command `npm --prefix web/reflekt-web audit fix` with no `--force` / `--legacy-peer-deps` / `overrides`, the expected outcome `vue` ≥ 3.5.42 and `npm audit --omit=dev` = 0), and records the current `npm audit` output. Acceptance items 4 and the lockfile part of 7 are discharged at the campaign merge, not in this unit. Confidence: high — it is what the campaign's shared-file rule says.
2. The unit runs `npm audit fix` and commits the lockfile anyway. Confidence: low — breaks the shared-file rule.

Default if unanswered in 30 minutes: option 1, recorded presumed (proceed-default).

**Answer (AR-1, shared architect of the campaign — not the owner; Owner to rule: no), 2026-10-23:** Option 1. Do not edit `web/reflekt-web/package.json` or `package-lock.json`. Write a change note in `delivery/change-notes.md`:
- Target: `web/reflekt-web/package-lock.json` only. Command: `npm --prefix web/reflekt-web audit fix` (no `--force`, no `--legacy-peer-deps`, no `overrides`).
- Expected result: `vue` >= 3.5.42 in the lockfile (`package.json` keeps `"vue": "^3.5.32"`, and 3.5.42 is inside that range); `npm --prefix web/reflekt-web audit --omit=dev` reports 0; `git diff --name-only` after the command shows only `package-lock.json`. If `package.json` also changes, whoever applies the note records it.
- The current plain `npm audit` output is the "before" state; the "after" output is recorded when the note is applied.
- Acceptance 4 and the lockfile part of acceptance 7 are marked "owed at merge", not Missing.
- F37's note is applied before every other unit's note on these files (F39's comes after it).

## Q2: Who runs the whole-solution tests at the Microsoft.* step
**From:** architect
**To:** team-lead
**Status:** Answered
**Step:** Phase 1 analysis (plan Step 2 — Microsoft.* 9.0.4 -> 10.0.x)
**File:** `.claude/verify.json`

**Context:** The spec makes the Microsoft.* step run `dotnet test Reflekt.slnx`, and the owner's standing rule puts that upgrade behind the full suite. The declared `unit` role (the step close, `verify-run.js --profile fast`) runs only `tests/Reflekt.UnitTests` — the integration tests, where `Database.Migrate()` proves the bump changed no model, are not in it. Developers run tests only through `verify-run.js`; the `full` role (`dotnet test Reflekt.slnx && npm ci && npm run build`) measured 68 s on 2026-10-13, well inside a subagent's limits, and `npm ci` leaves the shared lockfile untouched.

**Question:** At the close of plan Step 2, should the developer run the full role itself, or hand back so the coordinating session runs it before Step 3?

**Recommendation:**
1. (Recommended) The developer runs `node {scripts}/verify-run.js --role full --slug F37-DependencyUpgrades` at Step 2's close and cites the record; a red that is not a compile error stops the step and is handed back as a blocker (spec Decisions row 4). One extra full run before the handover's complete run; no extra hand-back.
2. The developer hands back after Step 2 with "full run owed"; the coordinating session runs `--profile complete`, then resumes the developer for Steps 3–5. Keeps full runs in the main session, at the cost of one hand-back and resume.
**Confidence:** medium — option 1 is cheap and within limits, but whether a developer seat may run the `full` role is the coordinator's process call.

### Answer

**Answered by team-lead (coordinator's process call — not the owner), 2026-10-23:** Option 1. At Step 2's close the developer runs `verify-run.js --role full --slug F37-DependencyUpgrades` and cites the record; a red that is not a compile error stops the step and is handed back as a blocker. The full run stays well inside a subagent's limits (68 s measured).

