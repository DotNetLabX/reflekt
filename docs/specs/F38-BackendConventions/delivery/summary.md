# Backend Conventions Conformance — Summary
Mode: team pipeline (spec)
Gear: normal — campaign

## Status: COMPLETE

## What Was Built
- The backend now follows one set of conventions: one error convention and error body (with `code`), typed claims and merged policies, one `SprintAccess` service and phase-action guard, event handlers that log every failure, domain rules moved into the aggregates, one report builder, no EF in the API layer, no-tracking reads, limits as domain constants, endpoint type layout, validated options, and one shared integration-test host — with no change to what users see.

## Key Outcomes
- 241 paths changed in the worktree (77 added, 150 modified, 13 deleted, 1 renamed), untracked feature files included; nothing under `web/`.
- Build: clean rebuild 0 warnings, 0 errors (Step 17).
- **Full suite:** pass — 91 s (recorded 2026-10-23T17:05:18Z, run mvl80mew-1d7d55e7; complete red on a fast-green tree: no; filtered: no)
- **Tests and builds:** 2 h 6 min across 197 runs — complete 4 min 22 s, step runs 2 h 1 min, gate runs 0, builds 20 s
- **Map delta:** none
- Review: Codex NO-GO (1 HIGH, 1 MEDIUM, 3 LOW) → one fix round → Codex GO; second reader (Sonnet) COMMENT both times; done check PASS (Check 1 and the re-check, Check 2). Cycles: 1/3.
- Rule list: not applicable — restructuring with no intended behaviour change. Nothing to promote; no registry touched (owner's rule).

## Deviations from Plan
- Fix round 1 (questions.md Q2, Q3): the event publisher now awaits its batch on save and commit (plan P7 supersedes P5; P8 added — no snapshot values on events). Live updates again go out before the response, in order, as before F38. Plan Step 4, its Decisions rows and the spec's events row were amended by the architect.
- Fix round 1 also moved `SeedOptions` into `Reflekt.Persistence` (read via `IOptions`) and removed the unused `Blocks.Core` reference from `Blocks.Persistence` (the API now references `Blocks.Core` and `Blocks.Exceptions` directly).
- The `CreateTeamForm.vue` edit was dropped (shared answer AR-2): F39 owns every SPA error consumer and merges before F38.
- Step deviations the done check accepted are listed in `done-check.md` Check 1.

## Notes
- **AC10 open:** Step 18, the two-browser walkthrough with a real Google sign-in, is operator-owned and not done. The developer's two-hub-client run saw identical frames on both clients and exactly one `GroupCreated` (on create). Record the operator result under Step 17's `OPERATOR ACTION REQUIRED` list.
- **Merge order (binding):** F39 merges before F38 (AR-2); keep F39's `CreateTeamForm.vue` if both touch it.
- **Solo fix 1 — fixed at the close run, not reviewed:** `tests/Reflekt.IntegrationTests/TestParallelization.cs` turns off parallel running of the integration test classes. Cause: FastEndpoints keeps its JSON options in one process-wide value that each parallel test host's start-up replaced and edited, so a host intermittently failed to start (read-only `JsonSerializerOptions` in `MapFastEndpoints`). It pre-dates F38 (on record in F35 and F37). The integration project now takes about 45–100 s longer.
- **Accepted residuals (P7/P8):** a commit landing between this request's commit and its handler's read sends the newer value; while the link-preview queue holds 100 jobs, attaching a link waits for a free slot before answering.
- **Open observation (carry-over):** FastEndpoints also keeps its service lookup in one process-wide value; within one test, the signed-in host's event handlers can read the anonymous host's database. No test checks broadcasts, so nothing fails today.
- **For F40:** the Step 17 list in `implementation.md` (nine code/skill differences, the BE-30 guards left out on purpose); `GetSprintSummaryEndpoint.cs:26` keeps its own owner check (anonymous endpoint, not a defect); `graphify update` not run (tool not installed); `src/Services/Reflekt/CLAUDE.md` still lists only two building blocks.
- **Backlog:** written as a change note (`change-notes.md`), not a direct edit — `docs/backlog.md` is a shared file.
- **Under the bar:**
  [LOW] Step 17's F40 list item 3 and the Step 4 block still describe the detached publication superseded by P7 — implementation.md:456 (second reader, cycle-1 follow-up)
  — **Carry-over row:** none (no MEDIUM)
- **Slow tests kept:** none
- **Timed out, sent to solo:** none
- **Flaky — passed when re-run alone:** tests/Reflekt.IntegrationTests/EvidenceLinkEndpointTests.cs — DeleteCard_CascadeDeletesEvidenceLinks (handover run; passed on the unchanged complete re-run). Same host-start cause that solo fix 1 removed. — **Backlog row:** none (cause fixed in this run)
