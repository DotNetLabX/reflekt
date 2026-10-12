# F33 — Publish Cleanup — Summary

Mode: architect-led fast lane (light)

## Status: COMPLETE

## What Was Built
- The repo is cleaned for public release as a teaching example: secrets, personal and employer data, build output and tool state removed from the current tree and blocked by `.gitignore`; private references replaced; live-doc links fixed; the deploy script parameterized; both READMEs rewritten.
- The existing backend tests (red at baseline: 17 compile errors, 98/105 integration failures) and the frontend build (red at baseline) are green again — test code, two frontend type guards, one dead component deleted and the lockfile synced; no change under `src/` beyond deleting two committed binaries.

## Key Outcomes
- About 60 files changed (tests, docs, `.claude/` text edits, `.gitignore`, `deploy.ps1`, READMEs, `.claude/verify.json` declared); 22 tracked files removed (binaries, database, old tool state, graph temp files); `.claude/settings.local.json` untracked.
- Build: `dotnet build Reflekt.slnx` and `npm run build` green.
- **Full suite:** pass — 53 s (recorded 2026-10-12T23:38:16Z; complete red on a fast-green tree: not measured; filtered: no). Run with the Google credential settings blanked, proving a clean clone does not depend on this machine's user-secrets.
- **Map delta:** none
- Review: plan review round 1 (Codex NO-GO + Sonnet REVISE → 13 of 14 fixed, 1 deferred to F34); done check PASS (check 1); code review Codex NO-GO (1 HIGH, 3 MEDIUM, 1 LOW) + second reader passed (1 MEDIUM, 4 LOW); one fix round (cycle 1/3) carrying all 7 items; light lane — no follow-up review.
- Test counts: unit 64 → 63 attributes, integration 105 → 104 (one test per removed behaviour deleted, each named in `implementation.md`).

## Deviations from Plan
- Step 1: four Vote tests and three spec lines in `docs/product/v1.md` follow the one-vote-per-group rule (`questions.md` Q1 — the rule is the deliberate BUG-007 fix; the old spec wording was stale).
- Step 2: the dead component's deletion was staged with `git rm` (accepted; part of this commit).
- Step 5: the 10 `docs/backlog.md` link fixes were applied by the architect (file was coordinator-owned in the dispatch).
- Step 8 (close): `roles.full` also runs the frontend build; `measured` and `stepLimitSeconds: 30` added.
- `graphify update .` (CLAUDE.md) was not run: the `graphify` command is not installed on this machine; the tracked graph predates the test changes.

## Notes
- **Fix round items** — all 7 reported fixed by the fix-round developer. The HIGH (tests depending on machine credentials) was re-verified by the close's complete run with the credentials blanked; the other six are fixed, not re-verified by a reader (light lane).
- **Under the bar:** none — **Carry-over row:** none (no MEDIUM)
- **Deferred to F34 (backlog row 34 amended):** dependency advisories for AngleSharp 1.2.0 and SQLitePCLRaw.lib.e_sqlite3 2.1.10; whether `docs/conventions/testing.md` should describe Reflekt's own test helpers; architecture-doc drift (e.g. `docs/architecture/v1.md` still mentions `src/Modules/`).
- **Decided by the architect while the owner was away** (each reversible): fixing the existing tests and frontend build inside F33; Q1's ruling; the owner's first name stays in product/architecture docs (also in `LICENSE`); feature slugs in `known-deviations.md` stay; `docs/kb/` and `docs/skill-backlog.md` mentions stay and the README says they were never created; starter-template skill paths stay.
- **Owner actions outside F33:** (1) rotate the Google OAuth client secret — the commit carrying it inside `deploy.zip` is reachable from `origin/main`; (2) publish only the history-scrubbed fresh copy; (3) never push the `pre-cleanup` tag publicly.
- **Merge:** not done — the owner is away; the work is committed on branch `F33-PublishCleanup` in the worktree `../reflekt-F33`. Before merging into `main`, remove the main checkout's leftover untracked database side files under `src/Services/Reflekt/Reflekt.API/data/` if git reports them in the way.
