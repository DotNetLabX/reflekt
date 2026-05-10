# Configurable Reflection Columns

**Traces to:** `docs/specs/v2.md` §4.4
**Source:** Scratch
**Dependencies:** None (independent of Identity and Teams features)
**Status:** Ready

---

## Purpose

V1 has two hardcoded reflection columns ("Went Well", "Didn't Go Well") plus a fixed "Action Items" column. Common retro formats (Start/Stop/Continue, Glad/Sad/Mad, Liked/Lacked/Learned) have no home, and the "Start Doing" concept — observations about new ideas — has no column at all. V2 adds a third reflection column and makes all three configurable by name and color, letting teams run any standard retro format without structural limitations.

## Entities

### Sprint (modified)

Sprint gains six new fields for column configuration:

| Field | Type | Constraints |
|-------|------|-------------|
| Column1Name | string | Required, max 30 characters, default "Went Well" |
| Column2Name | string | Required, max 30 characters, default "Didn't Go Well" |
| Column3Name | string | Required, max 30 characters, default "Start Doing" |
| Column1Color | string | Required, one of the preset palette values, default "green" |
| Column2Color | string | Required, one of the preset palette values, default "red" |
| Column3Color | string | Required, one of the preset palette values, default "amber" |

Column names and colors are stored per sprint, not per team. Different sprints in the same team can use different configurations.

### Card (modified — column type migration)

The column membership identifier changes from semantic values to positional values:
- "Went Well" becomes position 1
- "Didn't Go Well" becomes position 2
- "Action Items" remains unchanged

A new position 3 is introduced for the third reflection column. The positional model is stable regardless of what names the SM assigns — renaming a column never orphans or moves cards.

### CardGroup (modified — same column type migration)

Card groups follow the same positional migration as cards. Groups remain scoped to exactly one column.

## Color Palette

The system offers a fixed palette of 8 colors. Each color represents a visual theme (background tint, text accent, badge variant) — not a single hex value.

| Color key | Vibe | Default assignment |
|-----------|------|-------------------|
| green | Positive, success | Column 1 |
| red | Negative, danger | Column 2 |
| amber | Constructive, warm | Column 3 |
| blue | Informational, neutral | Action Items (fixed) |
| purple | Creative, reflective | — |
| teal | Calm, balanced | — |
| pink | Energetic, celebratory | — |
| slate | Neutral, subdued | — |

The Action Items column is always blue — its color is not editable. The SM chooses from this palette for the three reflection columns.

## User Flows

```
Flow 1: Create sprint with custom columns
1. SM navigates to the sprint creation form.
2. Below the existing sprint name and votes-per-participant fields, three column configuration rows are visible (always shown, not hidden behind "Advanced").
3. Each row has: a text input (pre-filled with a name) and a color swatch picker (pre-filled with a color).
4. Pre-fill logic: if the team has at least one previous sprint, the names and colors from the most recent sprint are used. Otherwise, system defaults apply ("Went Well"/green, "Didn't Go Well"/red, "Start Doing"/amber).
5. SM can change any name (1-30 characters) and any color (from the 8-color palette).
6. SM submits the form. Sprint is created with the configured column names and colors.
```

```
Flow 2: Edit column name during a retro (SM only)
1. SM clicks on a reflection column header in the sprint board.
2. The header becomes an inline text input showing the current name, with a color swatch picker alongside.
3. SM edits the name and/or selects a different color.
4. On confirm (Enter or blur), the change is saved and broadcast to all participants in real-time.
5. All participants see the updated column header immediately.
```

```
Flow 3: Participant views custom columns
1. Participant joins a sprint.
2. The sprint board renders column headers using the sprint's configured names and colors.
3. On mobile, the tab bar shows the custom column names.
4. Participant cannot click-to-edit column headers — the edit interaction is SM-only.
```

```
Flow 4: Create a card in the third column
1. During the Writing phase, a participant sees all three reflection columns (plus Action Items).
2. Participant creates a card in any reflection column, including the third column.
3. The card is associated with the column's position (not its name). Subsequent name changes do not affect the card's column membership.
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | /api/teams/{teamId}/sprints | SM | `{ name, votesPerParticipant?, column1Name?, column1Color?, column2Name?, column2Color?, column3Name?, column3Color? }` | Created sprint (includes column config) | 201, 400 |
| PUT | /api/sprints/{sprintId}/columns | SM | `{ column1Name, column1Color, column2Name, column2Color, column3Name, column3Color }` | Updated column config | 200, 400, 403 |

**POST /api/teams/{teamId}/sprints** (modified):

Sprint creation gains optional column fields. When omitted, defaults apply. This is a non-breaking change — existing clients that omit the fields get system defaults.

**PUT /api/sprints/{sprintId}/columns** (new):

Updates column names and/or colors on an existing sprint. All six fields are required in the request body (the frontend sends the full current state, not partial updates). This simplifies validation and avoids partial-state ambiguity.

Error responses:
- **400** — any name is empty, exceeds 30 characters, contains only whitespace, or any color is not in the preset palette.
- **403** — caller is not the sprint's Scrum Master.

No phase restriction — columns can be renamed in any phase, any sprint state (Draft, Active, Closed). Renaming is cosmetic; it does not affect card data.

**Sprint response changes (GET endpoints):**

All endpoints that return sprint data (`GET /api/teams/{teamId}/sprints`, `GET /api/sprints/{sprintId}`, sprint creation response) include column configuration fields in their response: `column1Name`, `column1Color`, `column2Name`, `column2Color`, `column3Name`, `column3Color`. This provides:
- The frontend with column config for rendering the sprint board (every participant load).
- Pre-fill data for sprint creation — the frontend reads column config from the most recent sprint in the team's sprint list. No dedicated pre-fill endpoint is needed.

**Card creation (coordinated change):**

The card creation endpoint (`POST /api/sprints/{sprintId}/cards`) changes its accepted column type values from semantic names ("WentWell", "DidntGoWell", "ActionItems") to positional names ("Column1", "Column2", "Column3", "ActionItems"). This is a coordinated breaking change — the frontend and backend are bundled (frontend builds into the API's wwwroot) and deploy atomically. No backward-compatible transition period is required.

**Report endpoint changes:**

The report endpoint (`GET /api/teams/{teamId}/sprints/{sprintId}/report`) changes its response structure from two hardcoded reflection sections to a list of column sections:

Each column section contains: the column's configured name, its color, and the list of groups (with cards sorted by votes). The three reflection columns appear in positional order (1, 2, 3), followed by Action Items. Column names in the report match whatever the SM configured — not hardcoded "Went Well" / "Didn't Go Well".

The markdown export follows the same structure: one heading per reflection column (using the configured name), then an "Action Items" heading.

## SignalR Events

| Event | Payload | Trigger |
|-------|---------|---------|
| ColumnConfigUpdated | `{ sprintId, column1Name, column1Color, column2Name, column2Color, column3Name, column3Color }` | SM updates column names or colors via PUT endpoint |

All participants in the sprint's SignalR group receive this event and re-render column headers immediately.

## Business Rules

1. A sprint has exactly three configurable reflection columns and one fixed Action Items column. The number of columns cannot be changed.
2. Column names are free-text, 1-30 characters, trimmed of leading/trailing whitespace. Empty or whitespace-only names are rejected.
3. Column colors must be one of the 8 preset palette values: green, red, amber, blue, purple, teal, pink, slate.
4. Duplicate column names are allowed — the SM may use the same name for multiple columns if they choose.
5. Duplicate column colors are allowed — the SM may assign the same color to multiple columns.
6. The Action Items column name is always "Action Items" and its color is always blue. Neither is editable.
7. Column names and colors can be changed at any time — during Draft, Active (any phase), or Closed state. Renaming does not delete, move, or modify any cards or groups.
8. Cards and groups are associated with a column by position (1, 2, 3, or Action Items), not by name. Renaming a column is purely a display change.
9. When creating a sprint, if the team has at least one previous sprint, column names and colors pre-fill from the most recently created sprint in that team. Otherwise, system defaults are used.
10. The sprint creation form always shows column configuration fields (not hidden behind an expandable section).
11. Only the Scrum Master can edit column names and colors. Participants see the configured names and colors but cannot modify them.
12. Column configuration changes are broadcast to all connected participants in real-time via SignalR.

## Data Migration

Existing v1 data requires a one-time migration:
- Cards with the "Went Well" column type migrate to position 1.
- Cards with the "Didn't Go Well" column type migrate to position 2.
- Cards with the "Action Items" column type remain at their position (unchanged).
- Card groups follow the same positional migration as cards.
- Existing sprints gain default column names ("Went Well" / "Didn't Go Well" / "Start Doing") and default colors (green / red / amber).
- No position 3 cards or groups exist in v1 data — the third column starts empty for all existing sprints.

**Deployment note:** The column type enum migration (semantic → positional values) changes the wire format for card creation and card responses. Because the frontend is bundled into the API's wwwroot and deployed as a single unit, this is an atomic change with no risk of client/server version mismatch. No backward-compatible alias period is needed.

## Acceptance Criteria

- [ ] Sprint creation includes three column name inputs and three color pickers, always visible below existing fields.
- [ ] Column name inputs are pre-filled from the team's most recent sprint (names and colors). If no previous sprint exists, system defaults are used.
- [ ] Sprint creation with custom names and colors stores them on the sprint and renders them on the board.
- [ ] Sprint creation without column fields (existing API clients) uses system defaults — non-breaking.
- [ ] `PUT /api/sprints/{sprintId}/columns` updates column names and colors, returns 200.
- [ ] `PUT /api/sprints/{sprintId}/columns` returns 400 for empty names, names exceeding 30 characters, whitespace-only names, or invalid color values.
- [ ] `PUT /api/sprints/{sprintId}/columns` returns 403 when called by a non-SM participant.
- [ ] Column names and colors can be changed during any sprint state (Draft, Active, Closed) and any phase.
- [ ] Changing a column name does not move, delete, or modify any existing cards or groups in that column.
- [ ] SM can click a column header to edit name and color inline on the sprint board.
- [ ] Non-SM participants cannot trigger the inline edit interaction.
- [ ] Column config changes are broadcast via `ColumnConfigUpdated` SignalR event to all connected participants.
- [ ] All connected participants see updated column headers immediately after a change.
- [ ] The mobile tab bar displays custom column names (not hardcoded values).
- [ ] Cards can be created in the third reflection column during the Writing phase.
- [ ] Cards in the third column participate in grouping (Grouping phase) and voting (Voting phase) the same as columns 1 and 2.
- [ ] The report endpoint returns a list of column sections (not hardcoded property names), each with the configured column name, color, and groups.
- [ ] The report includes all three reflection columns in positional order, followed by Action Items.
- [ ] The markdown export uses configured column names as section headings.
- [ ] Sprint response (GET endpoints) includes column names and colors in the response body.
- [ ] The frontend reads column config from the sprint list response for pre-fill — no dedicated endpoint needed.
- [ ] Card creation accepts positional column values ("Column1", "Column2", "Column3", "ActionItems") — semantic values ("WentWell", "DidntGoWell") are no longer accepted.
- [ ] Card responses return positional column type values.
- [ ] Data migration converts existing "Went Well" cards/groups to position 1 and "Didn't Go Well" to position 2.
- [ ] Existing sprints receive default column names and colors after migration.
- [ ] Action Items column remains fixed: name "Action Items", color blue, not editable.

## Out of Scope

- **Variable column count (add/remove columns)** — Fixed at 3+1. Covers 90% of retro formats without building a template engine. Revisit if real usage demands 4+ reflection columns.
- **Saved templates** — No reusable template library. Pre-fill from last sprint covers the repeat-use case. Templates are a v3 consideration.
- **Column reordering** — Columns render in fixed positional order (1, 2, 3, Action Items). Drag-to-reorder is unnecessary complexity for 3 columns.
- **Per-column icons** — V1 has semantic icons (smile, frown, checklist). With configurable names, icons lose their semantic meaning. Removed in favor of color-only differentiation. Revisit if user testing reveals confusion without icons.
- **Column descriptions / placeholder text** — Some competitors allow a description below the column name. Not needed for v2 — the name alone is sufficient for a 3-15 person team that chose the format together.
