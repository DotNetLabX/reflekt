# F39-FrontendConventions — Summary

Mode: team pipeline (spec)

The Vue SPA now follows the frontend conventions: one API layer, setup stores, optimistic actions in the stores, split retro surfaces, one report component, design tokens, Base* components and accessibility attributes — with no new product behaviour.

**Status:** Complete (campaign unit audit-fixes, unattended, gear normal). Branch `F39-FrontendConventions`, not pushed, not merged.

## Outcome
- Steps 1–15 built by four slice developers (dev-s1/dev-r1, dev-s2, dev-s3, dev-s4); Step 16 (AC9 in-browser walkthrough) is `Owner: operator` and stays open.
- Done check: Check 1 — **PASS** (`done-check.md`); steps 1, 3, 12 deviated with valid reasons.
- Code review: Codex primary + Sonnet second reader (gear pair). Round 1 NO-GO (1 HIGH, 3 MEDIUM, 6 LOW incl. second reader) → fix cycle 1 (10 rows) → re-review: 9 closed, 1 MEDIUM partial → fix cycle 2 (1 row), no follow-up due (MEDIUM only). Final review section: **COMMENT** (`review.md`).
- Fix rounds: 2 of 3.

## Verification
- Full suite: **pass** — verify-run complete `mvl3hacc-5cb43b85`, 45.1 s, session 76c1a10a-1a00-4555-84ed-ab33c6d71fec (dotnet test Reflekt.slnx && npm ci && npm run build). Handover run `mvl2bnlv-b7a109e5` also passed.
- Rule list: not applicable — restructuring; nothing to promote (spec header). No registries (owner rule).
- Anchor check: no `docs/conventions/anchored-set.md` — no-op.
- Map delta: none (no `docs/architecture-map/`).
- Usage record: partial — `usage-record.json`.

## Change notes (shared files — applied by the campaign at merge)
`change-notes.md`:
1. `package.json` / `package-lock.json` — lint dev-dependencies and script (after F37's note; AC8 `audit --omit=dev` checked then).
2. `.claude/verify.json` — lint in the full role.
3. `docs/backlog.md` — pre-F38 error-reader removal row; F39 merges before F38.
4. `docs/backlog.md` — #39 Done, with the owed checks named.

## Notes
- **Fixed, not re-verified:** [MEDIUM] `stores/vote.ts` `confirmVote` now honours `ownSettled` (cycle 2). Side effect recorded by the developer: if the user's votes are reloaded within the same sprint while a vote is in flight, the own count may be one off until the next load.
- **Readiness:** after one toggle succeeds, an older in-flight toggle that later fails is no longer rolled back; the server's readiness event corrects the screen (Fix Round 1).
- **Reverted unlisted visible changes** (team-lead ruling on Codex C5, owner rule "no new product behaviour"): advance-dialog warning carry-over, team/sprint frame radii, info-icon navigation, timer hit area.
- **Owed at merge / by the operator:** AC9 walkthrough (Step 16); AC8 `run lint` and `audit --omit=dev` once Notes 1–2 apply.
- `graphify` not installed — graph not updated.
- Process: dev-s3 ran an unsanctioned `git rm --cached` (staging only), unstaged by the team lead; no subagent commit.
- Flaky tests: none. Untouched timeouts: none.
