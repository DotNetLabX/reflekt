# Creator Auto-Join — Lessons

## Planner Lessons
- When planning frontend auto-join flows that depend on auth state, explicitly call out the race condition between `App.vue` fire-and-forget auth fetch and view-level `onMounted` hooks. The plan should specify "await authStore.fetchMe if not yet loaded" rather than assuming it is ready.
- The `SprintSummaryResponse` needed `CurrentPhase` added alongside the planned `Participants` array — the plan should have caught that the existing summary response lacked phase info that the frontend already consumed via a separate field on the store type.

## Executor Lessons
- (none yet)

## Reviewer Lessons
- (none yet)
