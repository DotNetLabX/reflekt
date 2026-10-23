# F37 — Dependency Security Upgrades — Summary
Mode: team pipeline (spec)
Gear: normal — campaign

## Status: COMPLETE

## What Was Built
- Package versions only, in `Directory.Packages.props`: AngleSharp 1.2.0 → 1.8.4, and the seven Microsoft.* entries 9.0.4 → 10.0.13 (looked up 2026-10-23T04:18Z) as their own step behind the full suite. `SQLitePCLRaw.lib.e_sqlite3` now resolves to 2.1.13 through the bump, so no pin was added; the app's connection runs SQLite 3.53.3.
- The npm fix (`npm audit fix`, lockfile only) is a change note applied at the campaign merge, because the frontend lockfile is a shared file (Q1, answered by the campaign's shared architect).

## Key Outcomes
- 1 source file changed (`Directory.Packages.props`, 8 lines); no code change, no migration, no test change.
- Build: 0 errors, 0 warnings; no NU1901–NU1904 line; `dotnet list Reflekt.slnx package --vulnerable --include-transitive` clean in all nine projects.
- **Full suite:** pass — 69.9 s (recorded 2026-10-23T04:32:03Z, run `mvkh3y6o-2d7a44e2`; complete red on a fast-green tree: no; filtered: no)
- **Tests and builds:** 3 min 23 s across 9 runs — complete 2 min 39 s, step runs 43 s, gate runs 0, builds 0
- **Map delta:** none
- **Risky steps:** Risky steps: Step 1 (kind: dependency; architect), Step 2 (kind: dependency; architect), Step 3 (kind: dependency; architect) · Unplanned: .claude/.personas.json
- Rule list: not applicable — nothing to promote (spec header).
- Review: done check PASS (Check 1); code review Codex GO (primary) + Sonnet COMMENT (second reader, risky steps) → COMMENT; no fix round (0 cycles). Plan review: two critic rounds (Codex + Sonnet, then Codex), six findings folded.
- Spec acceptance: 1, 2, 3, 5, 6 and 7 (the unit's part) met; 4 and the lockfile part of 7 owed at merge (change-notes.md Note 2).

## Deviations from Plan
- Step 2's acceptance was amended during the build (Q3): a red that passes on an unchanged re-run is recorded as flaky and closes the step. Nothing else.

## Notes
- **Owed at merge:** apply `change-notes.md` Note 2 (npm lockfile) first, before any other unit's note on the npm files; then Notes 1 and 3 (backlog rows).
- **Under the bar:** [MEDIUM] Flaky host start in `CardEndpointTests.CreateCard_ActionItem_Returns201` — implementation.md:117 (Codex, carry-over) · [LOW] npm fix owed at merge — implementation.md:118 (Codex, carry-over) — **Carry-over row:** change-notes.md Note 1 (backlog is a shared file)
- **Slow tests kept:** none
- **Timed out, sent to solo:** none
- **Flaky — passed when re-run alone:** `tests/Reflekt.IntegrationTests/CardEndpointTests.cs` — `CreateCard_ActionItem_Returns201` (red in the Step 2 full run `mvkgn2o8-461c60a6`, green on the unchanged re-run `mvkgpdv7-eb5d9956` and in the complete run) — **Backlog row:** change-notes.md Note 1
- Usage record: partial (`usage-record.json`).
- Lessons: not processed this run (unattended; the learner was not started).
