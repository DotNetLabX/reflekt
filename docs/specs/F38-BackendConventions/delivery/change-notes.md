# F38-BackendConventions — Change notes

Shared files this unit may not edit. The campaign applies each note at merge.

## 1. docs/backlog.md — #38 status

- **Target:** `docs/backlog.md`, section `### 38. Backend Conventions Conformance`, its status line.
- **Change:** `**Status: Spec Ready**` → `**Status: Done (AC10 operator walkthrough open)**`.
- **Why:** F38 closed 2026-10-23 (`docs/specs/F38-BackendConventions/delivery/summary.md`). AC10's two-browser walkthrough is operator-owned (plan Step 18) and stays open until recorded in `implementation.md` § Step 17 `OPERATOR ACTION REQUIRED`.
- **Merge order (binding, shared answer AR-2):** F39 merges before F38. If F38's branch still touches `web/reflekt-web/src/components/CreateTeamForm.vue` at merge, keep F39's version.
