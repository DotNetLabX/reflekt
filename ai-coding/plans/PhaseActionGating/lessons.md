# Phase Action Gating — Lessons

## Planner Lessons
- Plan miscounted seed rows (said 14, actual is 15). When counting matrix entries, enumerate them explicitly rather than estimating. The spec matrix is the source of truth — cross-count before committing a number.
- Plan specified ISprintAction on 5 request classes but only 1 (CreateCardCommand) actually benefits from it. The other 4 have static action types better expressed as inline constants. When designing an interface, check whether each implementer adds value over an inline constant.
- Plan should have specified the ordering of phase gate relative to ownership checks. The reviewer caught that edit/delete endpoints ran phase gate before ownership, leaking phase info to non-owners. Future plans for cross-cutting guards should explicitly state where in the endpoint flow they go (after auth, after ownership, before domain logic).
- Plan should have called out that evidence link gating needs a separate prop on CardItem (distinct from canEdit). Collapsing multiple permission dimensions into one prop is a recurring frontend pattern error. When multiple backend actions map to the same UI component, the plan should specify one permission prop per backend action.

## Executor Lessons

## Reviewer Lessons
