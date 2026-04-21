# Domain Event Handler Refactor — Lessons

## Planner Lessons
- The `create-aggregate/workflows/DomainEvent.md` already documented the correct handler location rule (`{Service}.API/Features/{Domain}/{Feature}/{HandlerName}.cs`) but the actual codebase didn't follow it. Plans should verify codebase compliance with existing skill documentation, not just assume it.
- Multi-handler placement needed an explicit rule that wasn't in any skill or convention file. The rule "handler lives with the feature it serves, not the feature that raised the event" should be documented in a convention or the skill itself — which we did via the new skill.
- When the user pushes back on a naming recommendation, take the feedback seriously. The initial recommendation to keep short `{EventName}Handler` names was wrong — the user correctly identified that names should be self-documenting (describe the effect). Don't default to brevity over clarity.
- When a plan evolves mid-implementation (renames added after first round), explicitly state in the handoff to executor that skill files must also be updated to match — don't assume skill updates from the original plan will carry forward.

## Executor Lessons
- When a second round of changes is requested after the first round is done-checked, update done.md and message the planner. The executor completed the renames but didn't update done.md or send a completion message, causing a pipeline stall.

## Reviewer Lessons
- (to be filled by reviewer if applicable)
