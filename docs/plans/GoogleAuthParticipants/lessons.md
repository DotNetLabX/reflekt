# GoogleAuthParticipants — Lessons

## Developer Lessons

- EF Core composite index `(SprintId, UserId)` subsumes a standalone `SprintId` index — the debug warning is expected and harmless. No need to keep a redundant single-column index when a composite covering the same leading column exists.
- When `JoinSprintResponse` is a positional record, adding a new optional property at the end (`int? UserId`) requires no changes to existing callers — Mapster maps it from the aggregate property and returns null for guest participants where `UserId` is null.
- Vue composables that return computed refs need the `computed` import even when other refs are already imported — the `showGoogleOption` addition required verifying the import was already present (`import { ref, computed, onMounted, onUnmounted } from 'vue'`).
- The plan explicitly directed Steps 5 and 6 to be implemented together in the same file. This is a valid pattern when a helper method is tightly coupled to a single endpoint and has no reuse elsewhere.
- When a loop has an explicit upper bound, always check what the fallback return value is — returning a value that was explicitly found to be invalid is a silent invariant violation that compiles cleanly and is easy to miss.

## Skill Gaps

- No missing skills. The `domain-patterns`, `persistence-patterns`, `vue-patterns`, and `create-feature` skills all applied cleanly to their respective steps.

## Architect Lessons

- When specifying fallback/overflow behavior for bounded loops (e.g., "reasonable upper bound of 100"), explicitly state what happens on overflow. "Prevent infinite loops" is not enough -- the plan must say whether to throw, return a default, or surface an error. The reviewer correctly caught that the plan was ambiguous here, and the developer chose to return the taken name (wrong). Next time: "On overflow, throw/return 409 with message."
- The plan's Skill Mapping table initially had 9 rows for 12 steps (frontend steps were lumped). Caught during self-review. Skill Mapping must have exactly one row per implementation step -- no lumping.
- Auth policy naming worked well. `GoogleUser` is semantically clear and avoids overloading `ScrumMaster` with a second meaning. When adding auth policies, name them by what identity they represent, not what action they gate.

## Reviewer Lessons

- Plans that say "reasonable upper bound to prevent infinite loops" must also specify what to return on overflow — returning a name already confirmed taken silently breaks a uniqueness invariant. Reviewer should always ask: what does the fallback return, and is it valid?
- `GetByIdAsync` is on the `RepositoryBase` base class, not on each concrete repository. Check the base before flagging a missing method as a finding.
- For open-redirect validation, percent-encoded slash sequences (`%2F%2F`) pass naive string guards. In this codebase the server-side `Response.Redirect` to a relative path keeps the same origin, so it is not a genuine open redirect — but a defence-in-depth decode-before-validate is worth noting as LOW.
