# F37-DependencyUpgrades — Review

## Step 2 — Risky steps

Written by team-lead from `risk-recheck.js --repo . --slug F37-DependencyUpgrades --base f9832b3` (exit 0), 2026-10-23:

Risky steps: Step 1 (kind: dependency; architect), Step 2 (kind: dependency; architect), Step 3 (kind: dependency; architect) · Unplanned: .claude/.personas.json

`.claude/.personas.json` is the campaign's persona registry, not this unit's change (plan Step 5's allowed list names it).

## Step 2 — Code Review

**Verdict: COMMENT**

Primary: Codex — GO, persisted by team-lead session e1039f04-d258-4e3c-9dcc-7ef79f12a27a
**Model:** codex
Run record: complete run `mvkh3y6o-2d7a44e2` — pass (cited by Codex; it landed during the review). Codex's full text: `review-codex.md`.

Codex reports no new findings; both rows below are its confirmations of `implementation.md`'s own Carry-Over Findings table, kept as severity headings as it returned them.

### [MEDIUM] Flaky host start in `CardEndpointTests.CreateCard_ActionItem_Returns201` (implementation.md:117)
Confirmed as recorded: one red (`mvkgn2o8-461c60a6`), an unchanged green re-run, and a green complete run. Cause unknown; the exception message was cut from the record. Suggested fix: keep the backlog row (change-notes.md Note 1); if it fails again, capture the exception and compare against 9.0.4.

### [LOW] npm fix owed at merge (implementation.md:118)
Confirmed: `vue` still resolves to 3.5.32, and both npm files are unchanged, as Q1 requires. Suggested fix: apply change-notes.md Note 2 first, record both audits, and re-run the full role.

Verified by Codex: AngleSharp 1.8.4; the seven Microsoft entries at 10.0.13; Test SDK unchanged; SQLitePCLRaw 2.1.13 across all four consumers, including the linux-x64 assets; the API's native SQLite reports 3.53.3; no vulnerable package in the nine projects.

*Status: COMPLETE — Codex primary, persisted by team-lead, 2026-10-23*

## Step 2 — Merge, no fix round

Checks in:
- Done check: PASS (`done-check.md` Check 1).
- Codex (primary): GO.
- Second reader (Sonnet, risky steps 1–3): COMMENT, nothing at its cutoff (`review-second.md`).
- Complete run `mvkh3y6o-2d7a44e2`: pass. The Q3 tripwire did not fire.

No item at the round's bar. Codex returned no new findings: its two rows only confirm the implementation record's carry-over rows, whose suggested fixes are already in place (change-notes.md Notes 1 and 2), so there is nothing for a developer to change. The second reader's open question (is the flake also present on 9.0.4?) is the subject of Note 1's backlog row.

**Under the bar — recorded, not fixed:**
- [MEDIUM] Flaky host start in `CardEndpointTests.CreateCard_ActionItem_Returns201`, cause unknown; backlog row = change-notes.md Note 1.
- [LOW] npm fix owed at merge; change-notes.md Note 2.
