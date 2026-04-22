# Card Grouping

**Traces to:** Industry research (Parabol, Retrium, TeamRetro, EasyRetro, Metro Retro, Neatro, GoRetro)
**Dependencies:** Auth, Teams, Sprints, Participants, Cards, Voting, PhaseActionGating, Report
**Status:** Ready

---

## Purpose

During retros, teams often surface duplicate or closely related observations. Without grouping, these scatter across the board, split votes, and fragment discussion. Card Grouping lets the facilitator cluster related notes into a single votable and discussable unit before voting begins — surfacing themes instead of individual comments.

This feature introduces a new **Grouping** phase between Writing and Voting, following the universal pattern established by every major retro tool (Parabol, Retrium, TeamRetro, EasyRetro, Metro Retro, Neatro, GoRetro). The facilitator groups cards during this dedicated phase; participants then vote on groups (not individual cards) during Voting.

## Phase Change

The sprint phase machine adds a Grouping phase:

**Before:** Writing → Voting → Discussing → Reporting
**After:** Writing → **Grouping** → Voting → Discussing → Reporting

Grouping is a first-class phase with its own phase actions in the action matrix. It sits between Writing and Voting because grouping must complete before voting starts — otherwise vote aggregation becomes ambiguous.

## Entities

### CardGroup (new aggregate root)

| Field | Type | Constraints |
|-------|------|-------------|
| Id | int | Auto-increment primary key |
| SprintId | int | Required, FK to Sprint |
| ColumnType | ColumnType | WentWell or DidntGoWell (never ActionItems) |
| Title | string? | Optional, max 100 chars. Facilitator-assigned theme label |
| CreatedAt | DateTime | UTC, stamped at creation |

A CardGroup belongs to one Sprint. A Sprint can have many CardGroups. A CardGroup contains one or more Cards of the same ColumnType.

CardGroups are created automatically (solo groups) at the Writing → Grouping phase transition and manually (merged groups) by the facilitator during the Grouping phase.

**Title semantics:** If Title is null, the UI displays the text of the earliest card in the group (by CreatedAt) as the group label. The facilitator can set a title at any time during Grouping to name the theme (e.g., "CI/CD issues", "Team morale").

**Domain events:**
- CardGroupCreated — raised when a group is created (auto or manual)
- CardsGrouped — raised when a card is moved into a group
- CardUngrouped — raised when a card is removed from a group (into a new solo group)
- CardGroupTitleChanged — raised when the facilitator sets or changes a group title

### Card (modified)

| Field | Type | Change |
|-------|------|--------|
| GroupId | int? | **New.** Nullable FK to CardGroup. Null during Writing phase. Populated when phase advances to Grouping (auto-created solo groups). Required from Grouping phase onward for Note cards. Always null for ActionItem cards. |

A Note card belongs to exactly one CardGroup from the Grouping phase onward. Moving a card between groups updates this FK.

### Vote (modified)

| Field | Type | Change |
|-------|------|--------|
| CardId | — | **Removed.** Votes no longer target individual cards. |
| GroupId | int | **New.** Required FK to CardGroup. Votes target groups. |

Since grouping completes before voting begins, every Note card has a group when voting starts. Ungrouped cards are in solo groups (group of one) and are votable just like multi-card groups. The participant's vote budget remains per-sprint, unchanged.

## Auto-Creation of Solo Groups

When the sprint phase advances from Writing to Grouping:

1. The system creates one CardGroup for every Note card in the sprint (WentWell and DidntGoWell columns).
2. Each CardGroup has ColumnType matching its card's ColumnType, Title = null, and SprintId matching the sprint.
3. Each Note card's GroupId is set to its auto-created CardGroup.
4. Action item cards (which don't exist yet — they're created during Discussing) are not affected.

This is a bulk operation triggered by the AdvancePhase endpoint. After it completes, every Note card belongs to its own solo group, and the facilitator can begin merging.

## User Flows

```
Flow 1: Phase transitions to Grouping (auto-creation)
1. SM clicks Advance Phase during Writing
2. Sprint moves to Grouping phase
3. System auto-creates a solo CardGroup for every Note card
4. All cards are now ungrouped (solo groups) and visible to all participants
5. Cards are no longer blurred (Writing-phase anonymity ends)
6. Only the facilitator (SM) sees grouping controls
```

```
Flow 2: Facilitator groups two ungrouped cards
1. SM drags card A onto card B (both in the same column)
2. Server creates a new CardGroup containing both cards
3. The two solo groups that previously held A and B are deleted
4. All connected clients see the cards collapse into a single group stack
5. The group displays the earlier card's text as its label (no explicit title yet)
```

```
Flow 3: Facilitator adds a card to an existing group
1. SM drags card C onto an existing group (A+B)
2. Server moves card C into the existing group
3. Card C's previous solo group is deleted
4. All connected clients see C join the group stack (card count updates)
```

```
Flow 4: Facilitator removes a card from a group
1. SM drags card A out of a group
2. Server creates a new solo group for card A and moves it there
3. If the source group now has only one card, it remains as a solo group (no auto-dissolve)
4. All connected clients see the card separate from the group
```

```
Flow 5: Facilitator titles a group
1. SM clicks the group header and types a theme title (e.g., "Deployment pain")
2. Server updates CardGroup.Title
3. All connected clients see the title replace the default card-text label
```

```
Flow 6: Facilitator dissolves a group
1. SM clicks "Ungroup" on a multi-card group
2. Server creates a new solo group for each card in the group
3. The original group is deleted
4. All connected clients see the cards separate back into individual items
```

```
Flow 7: Voting on a group
1. Sprint is in Voting phase
2. Participant clicks vote on a group (displayed as a collapsed stack)
3. Server creates a Vote with GroupId referencing the group
4. All connected clients see the group's vote count increment
5. Budget mechanics are unchanged — the participant spends from the same per-sprint budget
```

```
Flow 8: Group in the report
1. User views the sprint report
2. Groups appear as the top-level items in Went Well and Didn't Go Well sections
3. Each group shows its title (or first card's text if untitled), vote count, and individual cards within
4. Zero-vote groups are excluded (same rule as zero-vote cards today)
5. Groups are sorted by vote count descending
```

## API Surface

### New endpoints

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | `/api/sprints/{sprintId}/card-groups` | SM cookie | `{ cardIds: [int, int, ...], title? }` | Created group with cards | 201, 400, 401, 403, 409 |
| POST | `/api/sprints/{sprintId}/card-groups/{groupId}/cards` | SM cookie | `{ cardId }` | Updated group with cards | 200, 400, 401, 403, 404, 409 |
| DELETE | `/api/sprints/{sprintId}/card-groups/{groupId}/cards/{cardId}` | SM cookie | — | — | 200, 401, 403, 404, 409 |
| DELETE | `/api/sprints/{sprintId}/card-groups/{groupId}` | SM cookie | — | — | 204, 401, 403, 404, 409 |
| PATCH | `/api/sprints/{sprintId}/card-groups/{groupId}` | SM cookie | `{ title }` | Updated group | 200, 400, 401, 403, 404, 409 |
| GET | `/api/sprints/{sprintId}/card-groups` | Guest token or SM cookie | — | All groups with their cards | 200, 401, 404 |

**POST `/card-groups`** — Create a group from 2+ card IDs. Cards must be in the same column and same sprint. Cards are removed from their previous groups; empty source groups are auto-deleted. Returns the new group with its cards.

**POST `/card-groups/{groupId}/cards`** — Add a card to an existing group. Card must be in the same column as the group. Card's previous solo group is deleted if it becomes empty.

**DELETE `/card-groups/{groupId}/cards/{cardId}`** — Remove a card from a group. A new solo group is created for the removed card.

**DELETE `/card-groups/{groupId}`** — Dissolve a group. Every card in the group gets a new solo group. The original group is deleted.

**PATCH `/card-groups/{groupId}`** — Set or update the group's title.

**GET `/card-groups`** — List all groups in the sprint with their cards. Used by the frontend to render the Grouping, Voting, and Discussing phase boards.

Error responses:
- **Create group with fewer than 2 card IDs:** 400
- **Create group with cards from different columns:** 400
- **Create group with cards from different sprints:** 400
- **Create group with ActionItem cards:** 400
- **Create group by non-SM:** 403
- **Create group outside Grouping phase:** 409
- **Add card to group in a different column:** 400
- **Dissolve a solo group (only 1 card):** 400
- **Any mutation on a Closed sprint:** 409
- **Group or card not found:** 404

### Modified endpoints

**Vote endpoints** — Vote cast and retract routes change from card-scoped to group-scoped:

| Before | After |
|--------|-------|
| `POST /api/sprints/{sprintId}/cards/{cardId}/votes` | `POST /api/sprints/{sprintId}/card-groups/{groupId}/votes` |
| `DELETE /api/sprints/{sprintId}/cards/{cardId}/votes` | `DELETE /api/sprints/{sprintId}/card-groups/{groupId}/votes` |

Response shape changes: `cardId` → `groupId` in the response and in SignalR payloads.

**GET `/api/sprints/{sprintId}/votes/mine`** — Response changes from `[{ cardId, count }]` to `[{ groupId, count }]`.

**GET `/api/sprints/{sprintId}/cards`** — Response adds `groupId` (nullable int) to each card. Null during Writing phase; populated from Grouping onward.

**Report endpoint** — Response structure changes to nest cards within groups. See Report Impact section.

## SignalR Events

### New events (sprint group)

| Event | Payload | Trigger |
|-------|---------|---------|
| GroupCreated | `{ group: { id, sprintId, columnType, title, cardIds } }` | Solo group auto-creation or manual group creation |
| CardsGrouped | `{ groupId, cardId, sourceGroupId }` | Card moved into a group |
| CardUngrouped | `{ cardId, newGroupId, sourceGroupId }` | Card removed from a group |
| GroupDissolved | `{ groupId, newGroups: [{ id, cardIds }] }` | Group dissolved into solo groups |
| GroupTitleChanged | `{ groupId, title }` | Facilitator sets/changes group title |

### Modified events

| Event | Change |
|-------|--------|
| VoteCast | Payload changes from `{ cardId, newCount }` to `{ groupId, newCount }` |
| VoteRetracted | Payload changes from `{ cardId, newCount }` to `{ groupId, newCount }` |

**Note on auto-creation:** When phase advances from Writing to Grouping, the system creates N solo groups (one per Note card). Broadcasting N individual GroupCreated events would be noisy. Instead, the phase transition triggers a bulk event or the frontend re-fetches groups after receiving the PhaseChanged event (same pattern as the existing Writing → Voting transition).

## Phase Action Matrix Changes

### New SprintActions

| Value | Meaning |
|-------|---------|
| GroupCards | Move cards into a group, create a group, or add a card to a group |
| UngroupCards | Remove a card from a group or dissolve a group |
| RenameGroup | Set or change a group's title |

### Updated matrix

| Action | Writing | Grouping | Voting | Discussing | Reporting |
|--------|---------|----------|--------|------------|-----------|
| CreateNote | yes | | | | |
| EditNote | yes | | | | |
| DeleteNote | yes | | | | |
| GroupCards | | yes | | | |
| UngroupCards | | yes | | | |
| RenameGroup | | yes | | | |
| CastVote | | | yes | | |
| RetractVote | | | yes | | |
| CreateActionItem | | | | yes | |
| EditActionItem | | | | yes | |
| DeleteActionItem | | | | yes | |
| AttachEvidenceLink | yes | | | yes | |
| DetachEvidenceLink | yes | | | yes | |
| AdvancePhase | yes | yes | yes | yes | |

### Authorization

All grouping actions (GroupCards, UngroupCards, RenameGroup) are **SM-only**. Phase-gating checks the phase; authorization checks the caller is the SM. Both must pass.

Participants see the groups update in real-time but cannot create, modify, or dissolve groups.

## Report Impact

The report response structure changes to nest cards within groups:

**Before (flat notes):**
```json
{
  "notes": {
    "wentWell": [{ "text": "...", "evidenceLinks": [] }],
    "didntGoWell": [{ "text": "...", "evidenceLinks": [] }]
  }
}
```

**After (grouped notes):**
```json
{
  "notes": {
    "wentWell": [
      {
        "groupTitle": "Deployment improvements",
        "voteCount": 5,
        "cards": [
          { "text": "...", "evidenceLinks": [] },
          { "text": "...", "evidenceLinks": [] }
        ]
      }
    ],
    "didntGoWell": [...]
  }
}
```

- `groupTitle` is the CardGroup.Title if set, or the earliest card's text if not set.
- `voteCount` is the total votes on the group.
- `cards` lists all cards in the group with their evidence links.
- Zero-vote groups are excluded (unchanged rule — just applied to groups instead of cards).
- Groups are sorted by voteCount descending.
- Solo groups (one card, no explicit title) render identically to how individual cards rendered before — the grouping is transparent.
- Action items section is unchanged.

## Cross-Feature Impact

| Feature | Impact |
|---------|--------|
| **Sprints** | SprintPhase enum gains `Grouping` value. Phase progression: Writing → Grouping → Voting → Discussing → Reporting. AdvancePhase from Writing triggers bulk solo-group creation. |
| **Cards** | Card entity gains nullable `GroupId` FK. Card response DTO includes `groupId`. |
| **Voting** | Vote entity: `CardId` replaced by `GroupId`. All vote endpoints, SignalR events, and frontend stores change from card-scoped to group-scoped. Vote budget logic is unchanged. |
| **PhaseActionGating** | Three new SprintAction values. New Grouping column in the matrix. Seed data migration. |
| **Report** | Response nests cards within groups. Zero-vote filter and sort apply to groups. Markdown export updated. |
| **Frontend** | New Grouping phase UI (drag-and-drop or click-to-group). Voting phase shows groups as collapsed stacks. Discussion phase shows groups. Vote store changes from cardId to groupId. Phase stepper adds Grouping step. |

## Data Migration

Existing sprints that have already progressed past Writing need retroactive group creation:
- For every Note card in every existing sprint: create a solo CardGroup and set the card's GroupId.
- For every Vote row: set GroupId from the card's newly-created solo group; remove CardId.
- This is a one-time data migration. Since solo groups are 1:1 with cards, the mapping is trivial and lossless.

## Business Rules

1. Grouping is only permitted during the Grouping phase. All group mutation endpoints return 409 outside this phase.
2. Only the Scrum Master can create, modify, or dissolve groups. Participants see groups but cannot change them.
3. Cards can only be grouped within the same column. A WentWell card cannot join a DidntGoWell group.
4. Only Note cards participate in grouping. ActionItem cards (created during Discussing) are never grouped.
5. A group must contain at least one card. Empty groups are auto-deleted.
6. Creating a group requires at least 2 card IDs. Single-card groups are only created automatically (solo groups).
7. When a card moves to a new group, its source group is auto-deleted if it becomes empty.
8. Group title is optional, max 100 characters. If null, the UI uses the earliest card's text as the display label.
9. When phase advances from Writing to Grouping, solo groups are auto-created for all Note cards in the sprint. This is a system operation, not a user action.
10. During Voting, votes target groups (not individual cards). An ungrouped card in a solo group is votable just like a multi-card group.
11. Vote budget mechanics are unchanged: per-sprint, VotesPerParticipant total, freely distributable across groups.
12. Groups persist through Voting, Discussing, and Reporting phases. They are not dissolved after Grouping ends.
13. The report nests cards within groups. Zero-vote groups are excluded. Groups are sorted by vote count descending.
14. Solo groups with one card are visually indistinguishable from ungrouped cards in all phases after Grouping. No special "group" chrome for single-card groups.
15. Dissolving a group during Grouping creates a new solo group for each card — cards are never orphaned (GroupId is never null after the Grouping phase begins).

## Acceptance Criteria

### Phase changes
- [ ] SprintPhase enum includes `Grouping` between `Writing` and `Voting`
- [ ] AdvancePhase from Writing moves to Grouping (not directly to Voting)
- [ ] AdvancePhase from Grouping moves to Voting
- [ ] Phase stepper UI shows 5 phases: Writing, Grouping, Voting, Discussing, Reporting
- [ ] PhaseChanged SignalR event fires with "Grouping" when advancing from Writing

### Auto-creation of solo groups
- [ ] When phase advances from Writing to Grouping, a solo CardGroup is created for every Note card in the sprint
- [ ] Each solo group has ColumnType matching its card and Title = null
- [ ] Each Note card's GroupId is set to its solo group's Id
- [ ] ActionItem cards are not affected (GroupId remains null)
- [ ] Frontend re-fetches or receives group data after PhaseChanged to Grouping

### Group creation
- [ ] `POST /api/sprints/{sprintId}/card-groups` with 2+ same-column card IDs creates a group, returns 201
- [ ] Create group with fewer than 2 card IDs returns 400
- [ ] Create group with cards from different columns returns 400
- [ ] Create group with ActionItem cards returns 400
- [ ] Create group by non-SM returns 403
- [ ] Create group outside Grouping phase returns 409
- [ ] Source solo groups are auto-deleted when their cards move to the new group
- [ ] GroupCreated SignalR event fires to sprint group

### Adding cards to groups
- [ ] `POST /api/sprints/{sprintId}/card-groups/{groupId}/cards` adds a card to the group
- [ ] Add card from a different column returns 400
- [ ] Card's previous solo group is auto-deleted if empty
- [ ] CardsGrouped SignalR event fires to sprint group

### Removing cards from groups
- [ ] `DELETE /api/sprints/{sprintId}/card-groups/{groupId}/cards/{cardId}` removes the card and creates a new solo group for it
- [ ] CardUngrouped SignalR event fires to sprint group

### Dissolving groups
- [ ] `DELETE /api/sprints/{sprintId}/card-groups/{groupId}` dissolves a multi-card group into solo groups
- [ ] Dissolving a solo group returns 400
- [ ] GroupDissolved SignalR event fires to sprint group

### Group titles
- [ ] `PATCH /api/sprints/{sprintId}/card-groups/{groupId}` sets the group title
- [ ] Title exceeding 100 chars returns 400
- [ ] GroupTitleChanged SignalR event fires to sprint group
- [ ] UI displays the group title when set, otherwise the earliest card's text

### Listing groups
- [ ] `GET /api/sprints/{sprintId}/card-groups` returns all groups with their cards
- [ ] Response includes group id, sprintId, columnType, title, cards, and (from Voting onward) vote counts
- [ ] Accessible with guest token or SM cookie

### Voting changes
- [ ] Vote endpoints use `/card-groups/{groupId}/votes` instead of `/cards/{cardId}/votes`
- [ ] VoteCast SignalR event payload uses `groupId` instead of `cardId`
- [ ] VoteRetracted SignalR event payload uses `groupId` instead of `cardId`
- [ ] `GET /api/sprints/{sprintId}/votes/mine` returns `groupId` instead of `cardId`
- [ ] Vote budget is unchanged (per-sprint, VotesPerParticipant)
- [ ] Voting on ActionItem groups is not possible (ActionItems are never grouped)

### Report changes
- [ ] Report response nests cards within groups
- [ ] Zero-vote groups are excluded
- [ ] Groups are sorted by vote count descending
- [ ] Group title (or earliest card text) is included in the response
- [ ] Markdown export reflects grouped structure
- [ ] Solo groups render identically to how individual cards rendered before

### Phase action gating
- [ ] GroupCards, UngroupCards, RenameGroup actions exist in SprintAction enum
- [ ] These actions are permitted only during Grouping phase
- [ ] Attempting these actions outside Grouping returns 409
- [ ] All three actions require SM authorization (403 for non-SM)

### UI — Grouping phase
- [ ] Facilitator sees grouping controls (drag targets, ungroup buttons)
- [ ] Participants see cards and groups update in real-time but have no grouping controls
- [ ] Stack/collapse visual: grouped cards collapse into one visual unit with a card count badge
- [ ] Clicking a group expands it to show individual cards
- [ ] Group label shows title (if set) or earliest card's text

### UI — Voting phase
- [ ] Groups display as collapsed stacks
- [ ] Vote button appears on each group (not on individual cards within a group)
- [ ] Group vote count is displayed prominently

### UI — Discussing phase
- [ ] Groups remain visible with their vote counts
- [ ] Groups are sorted by vote count for discussion priority

### Data migration
- [ ] Existing Note cards in all sprints receive retroactive solo CardGroups
- [ ] Existing Vote rows are migrated from CardId to GroupId
- [ ] No data loss during migration

### General
- [ ] Build passes with zero errors
- [ ] All existing card, voting, and report tests updated or replaced

## Out of Scope

- **Participant-driven grouping** — only the facilitator (SM) can group cards. No "suggest a group" or collaborative grouping in v1.
- **Cross-column grouping** — cards can only be grouped within the same column. A WentWell card cannot join a DidntGoWell group.
- **Drag-and-drop card reordering within a group** — cards within a group have no explicit sort order. They display by creation time.
- **Group templates or suggested groups** — no AI-powered or keyword-based auto-grouping suggestions.
- **Voting on individual cards within a group** — votes target the group as a whole. Individual card vote counts do not exist.
- **Skipping the Grouping phase** — the phase always exists in the progression. The SM can advance through it immediately if no grouping is needed.
- **Re-grouping after Voting starts** — groups are frozen once Voting begins. No ungrouping or re-merging during Voting, Discussing, or Reporting.
- **Group-level evidence links** — evidence links remain on individual cards, not on groups.
- **Group-level comments or discussion threads** — discussion is verbal/synchronous, not threaded.
