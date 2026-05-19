# Open Actions from Previous Retro

**Traces to:** `docs/specs/v2.md` §4.7
**Source:** Scratch
**Dependencies:** Coordination: extends the AdvancePhaseCheck endpoint introduced by Inline Phase Controls (#28) for the Writing phase advance dialog. No build-time dependency.
**Status:** Ready
**Plan:** `docs/specs/F30-OpenActions/delivery/plan.md`

---

## Purpose

Retros should start by reviewing commitments from last time. Currently each retro starts from zero -- there is no connection between sprints, so action items are created, discussed, and forgotten. V2 adds an action item review section at the top of the sprint board during the Writing phase, giving the team continuity across retros and holding the SM accountable for follow-through. Industry consensus (Scrum.org, Derby/Larsen) identifies skipping action item review as a first-tier retrospective anti-pattern.

## Entities

### Card (modified)

Card gains two fields for cross-sprint action item review:

| Field | Type | Constraints |
|-------|------|-------------|
| ReviewStatus | enum (None, Done, CarriedOver, Dropped) | Default: None. Only meaningful when CardType = ActionItem. |
| ReviewedInSprintId | integer, nullable | References the sprint that reviewed this action item. Null until reviewed. |

- ReviewStatus records the outcome of the review: the SM marked this action item as completed (Done), still needed (CarriedOver), or no longer relevant (Dropped).
- ReviewedInSprintId records which sprint performed the review, enabling cross-sprint traceability without duplicating cards.
- Both fields are only set on action items. For notes, they remain at their defaults and are never exposed.

## User Flows

```
Flow 1: SM reviews previous action items during Writing
1. SM opens the retro room for a sprint in Writing phase.
2. The system finds action items from the most recently closed sprint in the same team
   that have not yet been reviewed (ReviewedInSprintId is null).
3. If unreviewed action items exist, a collapsible section appears at the top of the sprint
   board: "Actions from last sprint (N)".
4. Each item shows: action item text, assignee name, and three status buttons
   (Done / Carried Over / Dropped).
5. SM clicks a status button on an item.
6. The status is saved on the original card in the previous sprint. The item visually
   updates to reflect the selected status.
7. The status change is broadcast to all participants in real-time.
8. SM can change a status by clicking a different button (e.g., Done to Carried Over).
   The previous selection is replaced.
9. Once all items have a status, the section auto-collapses with a summary line:
   "All N actions reviewed (X done, Y carried over, Z dropped)".
10. SM can re-expand the section to review or change statuses at any time during Writing.
```

```
Flow 2: Participant views the review section (read-only)
1. Participant opens the retro room during Writing phase.
2. The "Actions from last sprint" section is visible at the top of the board.
3. Participant can see each action item's text, assignee, and current review status.
4. Participant has no status buttons -- the section is read-only for non-SM users.
5. Status changes made by the SM appear in real-time.
```

```
Flow 3: SM advances phase with unreviewed items
1. SM clicks "Next: Grouping" while some action items have not been reviewed.
2. A confirmation dialog appears: "N of M previous action items haven't been reviewed yet.
   Advance anyway?"
3. SM can proceed (advance) or cancel.
4. If SM proceeds, the phase advances. Unreviewed items retain their default status.
5. The review section disappears after Writing.
```

```
Flow 4: No previous action items
1. SM opens the retro room for a sprint in Writing phase.
2. The system checks for the most recently closed sprint in the same team.
3. Either: there is no closed sprint (first retro), or the most recent closed sprint
   has no action items, or all its action items have already been reviewed.
4. The review section does not appear. Writing begins normally with no extra UI.
```

```
Flow 5: Review statuses appear in the current sprint's report
1. SM or participant views the report for the current sprint.
2. The report includes a "Previous Actions Review" section listing action items
   that were reviewed during this sprint.
3. Each item shows: text, assignee, and review status (Done / Carried Over / Dropped).
4. Items the SM did not review appear as "Not reviewed".
5. The section header identifies the source: "Actions from: {previous sprint name}".
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|--------------|
| GET | /api/sprints/{sprintId}/previous-actions | Any participant | -- | Array of previous action items with review statuses | 200, 404 |
| PATCH | /api/teams/{teamId}/sprints/{sprintId}/previous-actions/{cardId} | SM | `{ reviewStatus }` | Updated action item | 200, 400, 403, 404, 409 |

**GET /api/sprints/{sprintId}/previous-actions:**

Returns action items from the most recently closed sprint in the same team that have not been reviewed by another sprint (ReviewedInSprintId is null). Each item includes: card ID, text, assignee name, and current review status.

Error responses:
- **200** -- action items returned (empty array if none exist).
- **404** -- sprint not found.

When the array is empty, the frontend hides the review section.

**PATCH /api/teams/{teamId}/sprints/{sprintId}/previous-actions/{cardId}:**

Sets the review status on an action item from the previous sprint. The `sprintId` in the route is the current sprint (the reviewing sprint), not the sprint that owns the card. The server validates that the card belongs to the correct previous sprint. This endpoint follows the team-scoped route convention used by all SM-only mutation endpoints.

- `reviewStatus` accepts: `Done`, `CarriedOver`, `Dropped`.
- Setting a status also sets ReviewedInSprintId to the current sprint.
- Changing a status replaces the existing value. Setting to the same status is a no-op (200).

Error responses:
- **400** -- invalid review status value.
- **403** -- caller is not the Scrum Master for this sprint's team.
- **404** -- sprint or card not found, or card does not belong to the expected previous sprint.
- **409** -- sprint is not in the Writing phase.

### Report endpoint changes

Both report endpoints (the SM team-scoped report and the participant sprint-scoped report) share the same response contract. The response gains a `previousActionsReview` section when at least one action item from the previous sprint was eligible for review during this sprint. The section includes:

- The previous sprint's name (for context)
- All action items from the previous sprint that were eligible for review (not just the ones the SM marked): items with ReviewedInSprintId pointing to this sprint show their status (Done / Carried Over / Dropped); items that were eligible but not reviewed appear as "Not reviewed"

This is a non-breaking addition -- existing clients that do not render this section ignore it.

### AdvancePhaseCheck endpoint changes

The existing AdvancePhaseCheck endpoint is extended to return previous action item counts for the Writing phase, following the same pattern used for non-voting participant counts during Voting and discussed group counts during Discussing. The response includes `unreviewedActionCount` and `totalPreviousActionCount` when the current phase is Writing and previous action items exist. The frontend uses these counts to populate the confirmation dialog ("N of M previous action items haven't been reviewed yet. Advance anyway?").

## Real-time Events

| Event | Payload | Trigger |
|-------|---------|---------|
| ReviewStatusChanged | cardId, reviewStatus, reviewedCount, totalCount | SM marks or changes an action item's review status |

No snapshot event is needed on reconnect. Review statuses are persisted, so the client fetches the current state via the GET endpoint on join.

## Business Rules

1. The review section appears only during the Writing phase. It is hidden in all other phases.
2. "Previous sprint" means the most recently closed sprint in the same team, determined by the ClosedAt timestamp.
3. Only action items that have not been reviewed by any sprint are shown (ReviewedInSprintId is null). Items already reviewed by another sprint are excluded.
4. Only the Scrum Master can set or change review statuses. Participants see the section as read-only.
5. Review statuses are mutable within the Writing phase. The SM can change a status to Done, Carried Over, or Dropped at any time before the phase advances. Resetting to None (un-reviewing) is not supported.
6. Once the phase advances past Writing, review statuses are locked. The PATCH endpoint returns 409 for non-Writing phases.
7. Review status is persisted on the original action item card in the previous sprint. No card is duplicated into the current sprint.
8. "Carried Over" is a label indicating the commitment is still relevant. It does not auto-create a copy in the current sprint. The SM manually creates a new action item in the Discussing phase if needed.
9. If the SM advances past Writing without reviewing all items, a confirmation dialog warns with the count of unreviewed items. The SM can proceed or cancel.
10. Unreviewed items retain their default ReviewStatus and null ReviewedInSprintId. They remain eligible for review by a future sprint.
11. If no previous closed sprint exists, or the previous sprint has no action items, or all its action items are already reviewed, the review section does not appear. No empty-state message is shown.
12. Review status changes are broadcast to all participants via SignalR in real-time.
13. The current sprint's report includes a "Previous Actions Review" section showing all action items that were reviewed during this sprint, with their statuses. Items not reviewed appear as "Not reviewed".
14. The review section does not block note-writing. Participants can write notes in the columns below while the SM reviews previous actions.

## Acceptance Criteria

- [ ] During Writing phase, the sprint board shows a collapsible "Actions from last sprint (N)" section at the top when unreviewed action items exist from the most recently closed sprint in the same team.
- [ ] Each action item in the section displays its text and assignee name.
- [ ] The SM sees three status buttons per item: Done, Carried Over, Dropped.
- [ ] Non-SM participants see the section as read-only (no status buttons, current statuses visible).
- [ ] Clicking a status button saves the review status on the original card in the previous sprint via the PATCH endpoint and returns 200.
- [ ] The SM can change a previously set status by clicking a different button.
- [ ] Review status changes are broadcast to all participants via the ReviewStatusChanged SignalR event.
- [ ] Once all items have a status, the section auto-collapses with a summary: "All N actions reviewed (X done, Y carried over, Z dropped)".
- [ ] The SM can re-expand the collapsed section to view or change statuses.
- [ ] Advancing past Writing with unreviewed items shows a confirmation dialog with the count of unreviewed items.
- [ ] The SM can proceed past the warning or cancel to stay in Writing.
- [ ] The review section is hidden in all phases other than Writing.
- [ ] The PATCH endpoint returns 409 when called outside the Writing phase.
- [ ] The PATCH endpoint returns 403 when called by a non-SM participant.
- [ ] The PATCH endpoint returns 404 when the card does not belong to the expected previous sprint.
- [ ] The GET endpoint returns action items from the most recently closed sprint in the same team, filtered to unreviewed items only (ReviewedInSprintId is null).
- [ ] The GET endpoint returns an empty array when there is no previous closed sprint, no action items, or all are already reviewed.
- [ ] The review section does not appear when the GET endpoint returns an empty array.
- [ ] The sprint report includes a "Previous Actions Review" section when action items were reviewed during this sprint.
- [ ] Each report entry shows: text, assignee, and review status (Done / Carried Over / Dropped / Not reviewed).
- [ ] Action items already reviewed by another sprint do not appear in the review section.
- [ ] The review section does not block note-writing -- participants can interact with the columns below while the section is visible.

## Out of Scope

- **Auto-copy on Carried Over** -- Carried Over is a label, not a duplication trigger. The SM manually creates new action items during Discussing if needed. Auto-copy would break card ownership (no natural author) and lose traceability across sprints.
- **Persistent action item tracker / dashboard** -- TeamRetro and Retrium offer dedicated cross-retro dashboards. For v2, the in-retro review section covers the critical accountability loop. A standalone tracker is a v3 consideration.
- **Review during phases other than Writing** -- Industry consensus places action review first. Writing is the first active phase in the flow.
- **Participant-initiated review statuses** -- Only the SM reviews previous actions, matching the SM-only action item creation pattern in the Discussing phase.
- **Action items from multiple previous sprints** -- Only the most recently closed sprint. Reviewing older sprints adds complexity with diminishing value. Chronic items carry over sprint by sprint, creating a natural trail.
- **Hard gate on unreviewed items** -- No competitor hard-blocks phase advance for unreviewed items. The confirmation dialog provides meaningful friction without annoying blocks.
- **Clearing / resetting a review status to None** -- The SM can change between Done, Carried Over, and Dropped, but cannot un-review an item. If they marked it incorrectly, they switch to the correct status. This keeps the interaction simple (three choices, no "undo" concept).
