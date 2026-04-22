# Phase Action Gating — Summary

## Status: COMPLETE

## What Was Built
Sprint phases are now enforced: every mutation action (card CRUD, voting, evidence links, advance phase) is gated by the current phase using a seeded action matrix. The backend rejects forbidden actions with 409; the frontend proactively hides/disables controls per phase. The phase enum value `Closed` was renamed to `Reporting` to eliminate the naming collision with sprint state `Closed`. A new anonymous endpoint `GET /api/phase-actions` exposes the matrix for frontend consumption.

## Key Outcomes
- 9 files created, 27 files modified across backend, frontend, and docs
- Backend build: 0 errors, 0 warnings
- Frontend build: clean
- Review verdict: APPROVED after 1 fix cycle (1 HIGH, 1 MEDIUM, 2 LOW — all resolved)

## Deviations from Plan
- Seed row count is 15 (not 14 as plan stated) — spec matrix is authoritative
- `ISprintAction` interface applied only to `CreateCardCommand` (where the action is conditional on card type); the other 4 request classes use inline static action values in their endpoints, making the interface unnecessary on them

## Notes
- The migration only adds the `PhaseActions` table with 15 seeded rows. No data fix-up needed for the phase rename (enum ordinal 3 is stable).
- `policyStore.fetch()` has no error handling — a failed fetch silently disables all actions for the session. This is acceptable for v1 but should be hardened if the app moves to a production deployment with unreliable networks.
- Test coverage for the phase matrix (every forbidden-phase cell returning 409) was specified in the plan but not yet written. The reviewer flagged this as a gap to address.
