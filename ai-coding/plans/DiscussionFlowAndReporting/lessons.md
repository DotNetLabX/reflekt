# Discussion Flow & Reporting — Lessons

## Planner Lessons
- When specifying seed data changes in a plan, explicitly list what already exists vs what is new. The plan said "8 new rows for Discussing" but 6 were already seeded in a prior migration — this caused the reviewer to raise a false-positive CRITICAL finding and waste a review cycle investigating.
- The plan suggestion to "consider extracting shared report logic" was too soft — either make it a plan step or do not mention it. Soft suggestions create ambiguity about whether they are requirements.
- Implementation.md description "added FocusGroup and ToggleDiscussed seed rows" was technically accurate but misleading — it only described the delta, not the full Discussing-phase seed state. The done check nearly missed this because the description was imprecise.

## Executor Lessons
- The PhaseAction seed data already had Discussing phase entries for action items/evidence links/advance. The plan initially noted it did not exist, but it was already in the codebase. The plan was updated before implementation to reflect this.
- Running `dotnet ef migrations add` can fail if the API process is running and locking DLLs — need to kill the process first.
- `CardGroupItem.vue`'s `groupBgClass` was a static string constructed in setup scope — had to convert to a computed property to make it reactive for focus/discussed state changes.

## Reviewer Lessons

## Skill Gaps
