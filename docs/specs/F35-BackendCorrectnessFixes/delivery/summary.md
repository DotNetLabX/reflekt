# F35 — Backend Correctness & Security Fixes — Summary
Mode: team pipeline (spec)
Gear: normal — campaign

## Status: COMPLETE

## What Was Built
- The backend defects and security holes from the F34 audit are fixed. Domain events now wait for the transaction to commit and are dropped on a failed or rolled-back save. Other participants' card text is hidden during Writing on every read path and live update. Rule violations return 409, and 5xx bodies outside Development carry no exception text. A closed sprint can no longer be edited.
- Also fixed: kicking a participant is now a soft kick that cuts off their live connections; display names are unique per sprint (with a migration that renames existing duplicates); the sign-in return URL is checked; and the link-preview fetcher checks every address it connects to (SSRF guard).

## Key Outcomes
- 57 source/test/build files: 52 modified, 5 new (`WritingPhaseRedaction.cs`, `CardBroadcast.cs`, `OpenGraphAddressPolicy.cs`, the `SoftKickParticipants` migration + Designer), plus one added `integration` role in `.claude/verify.json`.
- Build: green at every step (14 steps in 3 build slices, 3 developers + 1 fix-round developer).
- **Full suite:** pass — 63.4 s (recorded 2026-10-23T07:35:37.429Z, run mvkno0d1-be0697e2; complete red on a fast-green tree: no; filtered: no)
- **Tests and builds:** 29 min 52 s across 60 runs — complete 2 min 9 s, step runs 26 min 45 s, gate runs 0, builds 58 s
- **Map delta:** none
- Rule list: not applicable (defect fixes; spec header) — nothing to promote. No registry created (owner rule).
- Done check: PASS (Check 1). Code review: COMMENT (primary Fable — Codex job dropped early; second reader Sonnet COMMENT), 1 fix round (cycle 1/3): 1 MEDIUM + 3 LOW + the owner's Q5 teardown retry, all reported fixed; follow-up review none due (no HIGH) — fixed, not re-verified.
- Plan review: 2 critic rounds (Codex primary + Sonnet second, then Codex alone): 2 HIGH, 11 MEDIUM, 2 LOW folded.

## Deviations from Plan
- Step 7: the duplicate-name rename runs before the collation change (EF table-rebuild warning); still before the unique index exists.
- Step 11: `IsWritingPhaseCard` added to Step 10's `WritingPhaseRedaction.cs` so the Writing check stays in one place.
- Step 14: `graphify update .` not run — `graphify` is not installed; F40's graph refresh covers it.
- Step 1: a cancelled save also discards its pending events (in the spirit of BE-01).
- Fix round: the integration test classes' `DisposeAsync` delete lines retry on `IOException` — owner-allowed (Q5, directive d2).

## Notes
- Q1 (no new tests) answered by the owner's standing campaign directive; Q5 (teardown retry) answered by the owner (directive d2). Four plan departures from skills were kept on the architect's recommendation, unattended — not owner-confirmed (plan.md § Plan Review).
- Behaviour notes for reviewers of F38: `CreateCard` with an invalid body and a wrong-sprint token now gets 400, not 401 (validation first); `JoinSprint` requires the `participantId` claim; link fetches still allow `240.0.0.0/4` and `fec0::/10` (outside the spec's closed range list); the kick path has no automated test (P1).
- Fixed, not re-verified (fix round 1, no follow-up due): [MEDIUM] duplicated Writing-phase recipient logic in four handlers → `CardBroadcast.cs`; [LOW] implementation.md kick-tests claim corrected; [LOW] `Cascade(Stop)` on CreateCard enum rules; [LOW] interceptor comment corrected.
- Open review doubts (below the readers' confidence cutoff, not fixed): proxy bypass of the SSRF check (`UseProxy = false`), and the attach handler's read-before-queue leaving a link `Pending` — follow-up row via change note 2.
- Shared files: backlog status and the follow-up row are change notes (`change-notes.md`), not direct edits.
- Codex code-review job `task-mvkmsuzx-8n745w` failed in its early window with no output; the typed-marker fallback (Fable main) ran.
- **Under the bar:** none — **Carry-over row:** none (no MEDIUM)
- **Slow tests kept:** none
- **Timed out, sent to solo:** none
- **Flaky — passed when re-run alone:** none at the close run. During the build two intermittent integration failures were seen and passed on re-run: the pre-existing host-start failure (`CardEndpointTests` line 78, recorded by F37) and the teardown `IOException` (Q4), the latter now addressed by the owner-allowed retry. — **Backlog row:** none
