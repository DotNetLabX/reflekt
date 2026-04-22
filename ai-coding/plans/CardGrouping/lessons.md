# Card Grouping — Lessons

## Planner Lessons
- When a plan specifies domain events, explicitly state which aggregate method raises each event and which endpoint calls that method. The plan listed CardsGroupedEvent and CardUngroupedEvent with their handlers but did not specify where they would be raised, leading to dead-code handlers that were caught in review.
- When a plan specifies a SignalR event (like GroupDissolved), decide upfront whether it goes through a domain event handler or is emitted directly from the endpoint. The plan assumed a domain event handler pattern, but no GroupDissolvedEvent was created, causing a gap.
- Data migration strategy should be explicit about dev vs production paths. "Raw SQL in migration Up()" was specified but skipping it for dev was a reasonable deviation — the plan should have acknowledged both paths.

## Executor Lessons

## Reviewer Lessons
