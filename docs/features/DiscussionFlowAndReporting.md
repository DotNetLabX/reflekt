# Discussion Flow & Reporting Redesign

**Traces to:** `docs/specs/v1.md` §5.2 (phases), §5.5 (card ordering), §8 (action items), §10 (report)
**Dependencies:** Auth, Teams, Sprints, Participants, Cards, Voting, CardGrouping, PhaseActionGating, Reports
**Status:** Ready

---

## Purpose

The Discussing and Reporting phases lack structure. In Discussing, there is no way for participants to know which group is being discussed or which groups have already been covered. In Reporting, only the Scrum Master can access the report — participants see the same board as Discussing with an action item form that cannot submit. This feature adds a structured discussion flow and makes the Reporting phase a shared experience for all participants.

## Entities

### CardGroup (modified)

| Field | Type | Change |
|-------|------|--------|
| DiscussedAt | DateTime? | **New.** Null = not yet discussed. Set when the group is marked as discussed — either automatically (when focus moves to the next group) or manually (SM toggles). Cleared if the SM un-marks the group. |

### Discussion Focus (new concept)

At most one group per sprint can be "in focus" at any time during the Discussing phase. The focus state is visible to all sprint participants, including those who join or reconnect mid-discussion.

When the SM focuses a new group:
1. The previously focused group (if any) is automatically marked as discussed.
2. The new group becomes the active focus.

When the SM advances to Reporting:
1. The currently focused group (if any) is automatically marked as discussed.
2. Focus is cleared — no group is focused in Reporting.

The focus state must survive participant reconnection (a participant who refreshes during Discussion should see which group is currently focused).

## Phase Action Matrix Changes

### New SprintActions

| Value | Meaning |
|-------|---------|
| FocusGroup | Set the discussion focus to a group |
| ToggleDiscussed | Mark or un-mark a group as discussed |

### Updated matrix (Discussing column only — other phases unchanged)

| Action | Discussing |
|--------|------------|
| CreateActionItem | yes (SM-only — see Business Rules) |
| EditActionItem | yes (SM-only — see Business Rules) |
| DeleteActionItem | yes (SM-only — see Business Rules) |
| AttachEvidenceLink | yes |
| DetachEvidenceLink | yes |
| FocusGroup | yes |
| ToggleDiscussed | yes |
| AdvancePhase | yes |

### Authorization change

Action item creation, editing, and deletion are restricted to the Scrum Master in the Discussing phase. This is an authorization change — the phase gating remains unchanged (Discussing only), but the caller must now also be the SM. Previously any participant could create action items.

## Advance Phase Gate

Advancing from Discussing to Reporting requires at least 2 groups to be marked as discussed. If fewer than 2 groups are discussed:
- The backend rejects the advance with 409 and a message indicating how many groups are discussed and the minimum required.
- The frontend should prevent the attempt (disable the advance button or show a message).

When the minimum is met, the frontend shows a confirmation dialog: "You've discussed N of M groups. Advance to Reporting?" The SM must confirm before the phase advances.

When the advance is confirmed, the currently focused group (if any) is automatically marked as discussed before the phase transition occurs.

## Reporting Phase Redesign

When the sprint is in Reporting phase, the retro room shows two views that participants can toggle between:

### Report View (default)

The report is rendered inside the retro room for all participants — not behind a separate route that only the SM knows about. It shows:

- Sprint name and date
- Participant list
- **Notes** (Went Well and Didn't Go Well): all groups where `discussed OR voteCount > 1`, sorted by vote count descending. Each group shows its title (or first card's text if untitled), vote count, individual cards within the group, and a "Discussed" indicator on groups that were discussed.
- **Action items**: all action items with assignee name

"Copy as markdown" is available to all participants. The markdown output includes the discussed/undiscussed distinction on each group entry.

### Board View

A read-only version of the board as it appeared at the end of Discussion. All groups, cards, vote counts, and discussed marks are visible. No forms, no mutation controls, no add buttons. Action items are displayed as a read-only list (no creation form).

The toggle between Report and Board is a simple switch visible to all participants. Both SM and participants see the same views.

### Report Access

The report data must be accessible to all sprint participants, not just the SM. The existing report route requires a team ID that participants do not have in their context. The retro room must be able to fetch and render the report for any authenticated participant of the sprint.

## User Flows

```
Flow 1: SM focuses a group for discussion
1. Sprint is in Discussing phase
2. SM clicks "Discuss" on a group (e.g., "Deployment pain" with 3 votes)
3. The group is highlighted for all participants (visible focus indicator)
4. All connected clients see the focus move to this group
5. Participants who join or reconnect also see the focused group
```

```
Flow 2: SM moves focus to the next group
1. "Deployment pain" is currently focused
2. SM clicks "Discuss" on a different group ("CI/CD issues")
3. "Deployment pain" is automatically marked as discussed (checkmark/badge appears)
4. "CI/CD issues" becomes the new focus (highlight moves)
5. All connected clients see both changes
```

```
Flow 3: SM toggles discussed state
1. "Deployment pain" was auto-marked as discussed
2. SM realizes they want to revisit it — clicks "Mark as not discussed" on the group
3. The discussed indicator is removed
4. All connected clients see the change
5. SM can re-mark it later by toggling again
```

```
Flow 4: SM creates an action item during discussion
1. Sprint is in Discussing phase, "CI/CD issues" is focused
2. SM types "Fix the flaky deploy pipeline" and assigns "Spiri"
3. Action item appears in the action items section for all participants
4. Participants see the action item list update in real time (read-only)
```

```
Flow 5: Participant tries to create an action item
1. Sprint is in Discussing phase
2. Participant views the retro room
3. The action item creation form is not visible — only the SM sees it
4. Participant sees the action items list (read-only) growing as the SM adds items
```

```
Flow 6: SM advances to Reporting
1. SM has discussed 4 of 6 groups
2. SM clicks "Advance Phase"
3. Confirmation dialog: "You've discussed 4 of 6 groups. Advance to Reporting?"
4. SM confirms
5. The currently focused group (if any) is auto-marked as discussed
6. Phase advances to Reporting
7. All connected clients switch to the Report view
```

```
Flow 7: SM tries to advance with insufficient discussed groups
1. SM has discussed 1 of 6 groups
2. SM clicks "Advance Phase"
3. System rejects: "At least 2 groups must be discussed before advancing. Currently discussed: 1."
4. Phase remains Discussing
```

```
Flow 8: Participant views the report in Reporting phase
1. Sprint is in Reporting phase
2. Participant sees the Report view (default): discussed and high-vote groups, action items
3. Participant clicks "Board" toggle to see the full board (read-only)
4. Participant clicks "Report" toggle to return to the report
5. Participant clicks "Copy as markdown" to copy the report to clipboard
```

## API Surface

### New endpoints

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | `/api/sprints/{sprintId}/card-groups/{groupId}/focus` | SM cookie | — | `{ focusedGroupId, previousGroupId?, previousGroupDiscussed? }` | 200, 401, 403, 404, 409 |
| DELETE | `/api/sprints/{sprintId}/focus` | SM cookie | — | `{ previousGroupId?, previousGroupDiscussed? }` | 200, 401, 403, 409 |
| POST | `/api/sprints/{sprintId}/card-groups/{groupId}/toggle-discussed` | SM cookie | — | `{ groupId, isDiscussed, discussedAt? }` | 200, 401, 403, 404, 409 |
| GET | `/api/sprints/{sprintId}/report` | Guest token or SM cookie | — | Same shape as existing report, plus `isDiscussed` per group | 200, 401, 404 |

**POST `.../focus`** — Set focus to the specified group. If another group was focused, it is auto-marked as discussed. Returns the new focus and any state changes to the previous group.

**DELETE `.../focus`** — Clear focus without moving it to another group. The previously focused group is marked as discussed.

**POST `.../toggle-discussed`** — Toggle the discussed state of a group. If discussed, un-mark it. If not discussed, mark it.

**GET `/api/sprints/{sprintId}/report`** — Participant-accessible report endpoint. Same data as the existing team-scoped report but accessible with a guest token. Does not require team ID.

Error responses:
- **Focus on a group outside Discussing phase:** 409
- **Focus on a group by non-SM:** 403
- **Focus on a group in a different sprint:** 404
- **Toggle discussed outside Discussing phase:** 409
- **Toggle discussed by non-SM:** 403
- **Advance to Reporting with fewer than 2 discussed groups:** 409

### Modified endpoints

**AdvancePhase** — When advancing from Discussing to Reporting:
1. If fewer than 2 groups are marked as discussed, return 409 with a message.
2. The currently focused group (if any) is auto-marked as discussed.
3. Focus is cleared.

**Existing report endpoint** (`GET /api/teams/{teamId}/sprints/{sprintId}/report`) — unchanged. Continues to work for SM access outside the retro room.

**Action item endpoints** (`POST`, `PATCH`, `DELETE` for cards with CardType = ActionItem) — authorization tightened to SM-only during Discussing phase. Participants calling these endpoints receive 403.

### Modified response shapes

**GET `/api/sprints/{sprintId}/card-groups`** — Each group in the response gains:
- `isDiscussed` (boolean) — whether the group has been marked as discussed
- `isFocused` (boolean) — whether the group is currently focused for discussion

**Report response** — Each group entry gains:
- `isDiscussed` (boolean) — whether the group was discussed

**Report filter change:** the report includes groups where `isDiscussed = true OR voteCount > 1` (previously: `voteCount > 0`).

## SignalR Events

| Event | Payload | Trigger |
|-------|---------|---------|
| GroupFocused | `{ groupId, previousGroupId?, previousGroupDiscussed? }` | SM focuses a group |
| GroupFocusCleared | `{ previousGroupId?, previousGroupDiscussed? }` | SM clears focus or phase advances to Reporting |
| GroupDiscussedChanged | `{ groupId, isDiscussed, discussedAt? }` | Group discussed state changes (auto or manual) |

## Business Rules

1. At most one group per sprint can be focused at any time. Focusing a new group clears the previous focus.
2. When focus moves from one group to another, the previously focused group is automatically marked as discussed. The SM does not need to manually mark it.
3. When the phase advances from Discussing to Reporting, the currently focused group (if any) is automatically marked as discussed and focus is cleared.
4. The SM can toggle the discussed state of any group during the Discussing phase. This allows correcting accidental marks or re-opening a topic.
5. Advancing from Discussing to Reporting requires at least 2 groups to be marked as discussed. The backend enforces this — the request is rejected with 409 if the minimum is not met.
6. The focus state is visible to all sprint participants, including those who reconnect mid-discussion.
7. Action item creation, editing, and deletion are restricted to the Scrum Master during the Discussing phase. Any participant calling these endpoints receives 403. The phase gating (Discussing only) is unchanged.
8. In the Discussing phase, participants see the action items list as read-only — the creation form is visible only to the SM.
9. The report includes groups where the group was discussed OR the group has more than 1 vote. Groups with exactly 1 vote that were not discussed are excluded.
10. The report is sorted by vote count descending, regardless of discussed status. The discussed indicator is a visual badge, not a sort criterion.
11. The report is accessible to all sprint participants from within the retro room. Participants do not need to know the team ID to view the report.
12. In the Reporting phase, the retro room defaults to the Report view. A toggle switches to a read-only Board view and back.
13. Both SM and participants see the same views in the Reporting phase. There is no SM-exclusive view.
14. The Board view in Reporting is fully read-only — no forms, no mutation controls. Groups, cards, vote counts, and discussed marks are all visible.
15. "Copy as markdown" is available to all participants in the Reporting phase.
16. The markdown export reflects the report content, including the discussed/undiscussed distinction per group.

## Acceptance Criteria

### Discussion focus
- [ ] SM can click "Discuss" on a group during Discussing phase — the group becomes visually highlighted for all participants
- [ ] Only one group can be focused at a time — focusing a new group removes focus from the previous one
- [ ] When focus moves, the previously focused group is automatically marked as discussed (visual indicator appears)
- [ ] The SM can clear focus without moving it to another group
- [ ] Focus state is visible to participants who join or reconnect mid-discussion
- [ ] Focus on a group outside Discussing phase returns 409
- [ ] Focus on a group by a non-SM returns 403

### Discussed marking
- [ ] Groups auto-marked as discussed show a visual indicator (checkmark, badge, or similar) visible to all participants
- [ ] SM can toggle the discussed state of any group during Discussing phase
- [ ] Toggling discussed is broadcast to all connected clients in real time
- [ ] Toggle discussed outside Discussing phase returns 409
- [ ] Toggle discussed by a non-SM returns 403
- [ ] The discussed state persists — it survives page refresh and reconnection

### SM-only action items
- [ ] In Discussing phase, only the SM sees the action item creation form
- [ ] Participants see the action items list (read-only) — no form, no edit/delete controls
- [ ] Action items created by the SM appear in real time for all participants
- [ ] Participant calling the create action item endpoint receives 403
- [ ] Participant calling the edit or delete action item endpoint receives 403
- [ ] SM can still create, edit, and delete action items as before

### Advance phase gate
- [ ] Advancing from Discussing to Reporting with fewer than 2 discussed groups returns 409
- [ ] The 409 response includes the current count of discussed groups and the minimum required
- [ ] When the minimum is met, the frontend shows a confirmation dialog with the count ("You've discussed N of M groups")
- [ ] On confirmation, the currently focused group (if any) is auto-marked as discussed
- [ ] On confirmation, focus is cleared and phase advances to Reporting

### Reporting phase — Report view
- [ ] When phase is Reporting, the retro room defaults to the Report view for all participants (SM and guests)
- [ ] The Report view shows: sprint info, participants, notes (grouped, filtered, sorted), action items
- [ ] Report includes groups where `isDiscussed = true OR voteCount > 1`
- [ ] Groups with exactly 1 vote that were not discussed are excluded from the report
- [ ] Groups are sorted by vote count descending
- [ ] Each group in the report shows a "Discussed" indicator if it was discussed
- [ ] Each group shows its title (or first card's text if untitled), vote count, and cards within
- [ ] Action items are shown as a read-only list with assignee names
- [ ] "Copy as markdown" is available to all participants
- [ ] Markdown output includes the discussed/undiscussed distinction per group

### Reporting phase — Board view
- [ ] A toggle switches between Report view and Board view
- [ ] Board view shows the full board (all groups, cards, vote counts, discussed marks) in read-only mode
- [ ] No forms, no add buttons, no edit/delete controls are visible in Board view
- [ ] Action items section in Board view shows as a read-only list (no creation form)
- [ ] Toggle is visible to both SM and participants

### Report access
- [ ] `GET /api/sprints/{sprintId}/report` returns the report for authenticated sprint participants
- [ ] Guest token holders with a matching sprint ID can access the participant-scoped report
- [ ] The existing team-scoped report endpoint continues to work for the SM
- [ ] Build passes with zero errors

## Out of Scope

- **Phase revert / undo advance** — accidental phase advance is mitigated by the confirmation dialog and minimum-discussed gate. Full phase revert is tracked separately (backlog #22).
- **Automatic top-N selection** — the report includes all qualifying groups. There is no "show top 5 only" control. Vote count ordering and the discussed/undiscussed badge provide sufficient prioritization.
- **Cross-sprint carry-forward** — discussed groups are not carried to the next sprint as context. That is v2 memory-layer territory.
- **Discussion timer** — no per-group or total timebox enforcement. Discussion pacing is the SM's judgment call.
- **Participant-driven discussion requests** — participants cannot request focus on a group or suggest topics for discussion. The SM controls the discussion flow.
- **AI-generated summaries** — no automated summarization of discussed groups or action items.
- **Per-group notes or comments** — discussion is verbal and synchronous. No in-app discussion threads per group.
- **ROTI / satisfaction rating** — no end-of-retro satisfaction survey. Could be a separate future feature.
- **Re-grouping during Discussion** — groups are frozen after Grouping phase ends. The SM cannot merge or split groups during Discussing.
