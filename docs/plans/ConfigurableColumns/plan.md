# Configurable Reflection Columns

## Context

V1 has two hardcoded reflection columns ("Went Well", "Didn't Go Well") plus a fixed "Action Items" column. The system uses a `ColumnType` enum with semantic values (`WentWell`, `DidntGoWell`, `ActionItems`) which limits retro formats. V2 adds a third configurable reflection column and makes all three configurable by name and color, enabling any retro format (Start/Stop/Continue, Glad/Sad/Mad, Liked/Lacked/Learned). Column identity shifts from semantic enum values to positional values (`Column1`, `Column2`, `Column3`, `ActionItems`).

**Service impacted:** Reflekt (single service -- API, Domain, Persistence, and frontend).

## Scope

**In scope:**
- Add column configuration fields (3 names + 3 colors) to the Sprint aggregate
- Introduce a `ColumnColor` value object (validated palette of 8 colors)
- Change `ColumnType` enum from semantic (`WentWell`, `DidntGoWell`, `ActionItems`) to positional (`Column1`, `Column2`, `Column3`, `ActionItems`)
- Add a new domain event `ColumnConfigUpdatedEvent`
- Modify sprint creation to accept optional column config (with pre-fill from most recent sprint in team)
- New endpoint: `PUT /api/sprints/{sprintId}/columns` for SM to update column config
- SignalR broadcast of `ColumnConfigUpdated` on column changes
- EF Core migration: add columns to Sprint table, migrate Card/CardGroup `ColumnType` data
- Update all response DTOs (`SprintResponse`, `CardResponse`, `CardGroupResponse`, `ReportResponse`)
- Restructure the report endpoint from hardcoded two-section to dynamic column-section list
- Frontend: update board, mobile tabs, sprint creation form, report panel, stores, composables, and CSS theme for dynamic column colors
- Markdown export uses configured column names

**Out of scope:**
- Variable column count (fixed at 3+1)
- Saved templates / template library
- Column reordering (fixed positional order)
- Per-column icons (removed -- color-only differentiation)
- Column descriptions / placeholder text

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | domain-patterns | Follow | Modify `ColumnType` enum, add `ColumnColor` value object | |
| 2 | domain-patterns | Follow | Add column config properties + `UpdateColumnConfig` method + `ColumnConfigUpdatedEvent` to Sprint | |
| 3 | persistence-patterns | Follow | Update `SprintConfiguration` with 6 new columns, add data migration for ColumnType values | |
| 4 | create-feature | Follow | `UpdateColumns` endpoint, FastEndpoints, `PUT /api/sprints/{sprintId}/columns` | |
| 5 | create-domain-event-handler | Follow | `BroadcastColumnConfigUpdatedHandler` for SignalR | |
| 6 | (none) | -- | Modify `CreateSprintCommand`, `CreateSprintEndpoint`, and `Sprint.Create` for column config | |
| 7 | (none) | -- | Update `SprintResponse` and all sprint-returning endpoints to include column config | |
| 8 | (none) | -- | Refactor report endpoints + contracts from hardcoded sections to dynamic column list | |
| 9 | (none) | -- | EF Core migration | |
| 10 | tailwind-theme | Follow | Add 8-color palette tokens to `main.css` | |
| 11 | vue-patterns | Follow | Update sprint store types + `createSprint` action for column config | |
| 12 | pinia-patterns | Follow | Update card store + card group store getters for positional column types | |
| 13 | vue-patterns | Follow | Update `useSprintHub` with `ColumnConfigUpdated` handler | |
| 14 | vue-patterns | Follow | Rewrite `CreateSprintForm` with column configuration rows | |
| 15 | vue-patterns | Follow | Rewrite `RetroBoardPanel` + `RetroMobileTabBar` + `useRetroBoardActions` for 3 dynamic columns | |
| 16 | vue-patterns | Follow | Rewrite report store, `SprintReportPanel`, `ReportSection`, markdown export for dynamic columns | |

## Domain Model Changes

### ColumnType (renamed values)

```
enum ColumnType { Column1, Column2, Column3, ActionItems }
```

Replaces the old `{ WentWell, DidntGoWell, ActionItems }`. This is a positional model -- column names are stored on the Sprint, not embedded in the enum.

### ColumnColor (new value object)

```
ColumnColor {
  Value: string   // One of: "green", "red", "amber", "blue", "purple", "teal", "pink", "slate"
}
```

A simple value object with a static validation method. Stored as a string column in the database.

### Sprint (modified)

Add six new properties:

```
Sprint {
  ...existing properties...
  Column1Name: string       // Required, max 30, default "Went Well"
  Column2Name: string       // Required, max 30, default "Didn't Go Well"
  Column3Name: string       // Required, max 30, default "Start Doing"
  Column1Color: ColumnColor // Required, default green
  Column2Color: ColumnColor // Required, default red
  Column3Color: ColumnColor // Required, default amber
}
```

New behavior method: `UpdateColumnConfig(column1Name, column1Color, column2Name, column2Color, column3Name, column3Color)` -- validates all inputs and raises `ColumnConfigUpdatedEvent`.

Modify `Sprint.Create` factory to accept optional column config parameters (with system defaults when omitted).

### ColumnConfigUpdatedEvent (new domain event)

```
record ColumnConfigUpdatedEvent(Sprint Sprint) : IEvent
```

Raised by `Sprint.UpdateColumnConfig`.

## Data Model Changes

- `Sprints` table gains 6 columns: `Column1Name`, `Column2Name`, `Column3Name` (nvarchar(30), NOT NULL, default values), `Column1Color`, `Column2Color`, `Column3Color` (nvarchar(10), NOT NULL, default values)
- `Cards.ColumnType` stored values migrate: `0` (WentWell) -> `0` (Column1), `1` (DidntGoWell) -> `1` (Column2), `2` (ActionItems) -> `3` (ActionItems). NOTE: since we're adding Column3 at position 2, ActionItems shifts from ordinal 2 to ordinal 3.
- `CardGroups.ColumnType` follows the same migration as Cards.
- Existing sprints receive default column names and colors.

## Implementation Steps

### Step 1: Modify `ColumnType` enum and add `ColumnColor` value object

**What:** Replace the semantic enum values with positional values. Create a `ColumnColor` value object that validates against the 8-color palette.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Domain/Cards/ColumnType.cs` -- change enum to `{ Column1, Column2, Column3, ActionItems }`

**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/ColumnColor.cs` -- value object with `Value` property, static `Create(string)` factory that validates against allowed set, static `AllowedColors` readonly collection

**Skill:** Follow `domain-patterns` (value object pattern).

**Notes:** The enum ordinal shift is intentional: `ActionItems` moves from 2 to 3. The migration (Step 9) handles the stored value conversion.

---

### Step 2: Add column configuration to Sprint aggregate

**What:** Extend the Sprint aggregate with 6 column config properties, modify the `Create` factory to accept optional column config, and add an `UpdateColumnConfig` behavior method that raises `ColumnConfigUpdatedEvent`.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs` -- add 6 properties: `Column1Name`, `Column2Name`, `Column3Name` (string), `Column1Color`, `Column2Color`, `Column3Color` (ColumnColor)
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` -- modify `Create` to accept optional column name/color parameters (with system defaults); add `UpdateColumnConfig` method with validation (name length 1-30, trimmed, non-whitespace; color in palette)

**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/ColumnConfigUpdatedEvent.cs` -- `record ColumnConfigUpdatedEvent(Sprint Sprint) : IEvent`

**Skill:** Follow `domain-patterns` (aggregate behavior, domain event).

**Validation in `UpdateColumnConfig`:**
- Each name: trim, reject if empty/whitespace-only, reject if > 30 chars
- Each color: validate via `ColumnColor.Create` (throws if invalid)
- On success: set all 6 properties, raise `ColumnConfigUpdatedEvent`

---

### Step 3: Update EF configuration for Sprint column config

**What:** Add the 6 new Sprint columns to the EF configuration. Configure `ColumnColor` as a simple string conversion (owned value object stored as string). No relationship changes.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs` -- add property configs for `Column1Name` (required, max 30, default "Went Well"), `Column2Name` (required, max 30, default "Didn't Go Well"), `Column3Name` (required, max 30, default "Start Doing"), `Column1Color` (required, max 10, default "green", value conversion), `Column2Color` (required, max 10, default "red", value conversion), `Column3Color` (required, max 10, default "amber", value conversion)

**Skill:** Follow `persistence-patterns` (entity configuration, value conversion).

**Value conversion for ColumnColor:** `HasConversion(c => c.Value, s => ColumnColor.Create(s))` -- stores the raw string, reconstitutes through the factory.

---

### Step 4: Create `UpdateColumns` endpoint

**What:** New endpoint for the SM to update column names and colors on an existing sprint. Full replacement (all 6 fields required in request body). No phase restriction.

**Route:** `PUT /api/sprints/{sprintId}/columns`
**Auth:** `ScrumMaster` policy (team ownership check same as other sprint-mutating endpoints)
**Request:** `{ sprintId (route), column1Name, column1Color, column2Name, column2Color, column3Name, column3Color }`
**Response:** `SprintResponse` (updated sprint with column config)
**Status codes:** 200 (success), 400 (validation), 403 (not SM)

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/UpdateColumns/UpdateColumnsCommand.cs` -- command record + validator
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/UpdateColumns/UpdateColumnsEndpoint.cs` -- endpoint

**Skill:** Follow `create-feature` (FastEndpoints vertical slice).

**Guard ordering:** Auth policy -> load sprint -> verify team ownership -> call `sprint.UpdateColumnConfig(...)` -> save -> respond.

**Validator rules:**
- All 6 name fields: NotEmpty, MaxLength(30)
- All 3 color fields: NotEmpty, Must(ColumnColor.IsValid)

---

### Step 5: SignalR broadcast handler for column config changes

**What:** Create a domain event handler that broadcasts column configuration changes to all participants in the sprint's SignalR group.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/UpdateColumns/BroadcastColumnConfigUpdatedHandler.cs`

**Skill:** Follow `create-domain-event-handler`.

**Pattern reference:** `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/BroadcastPhaseChangedHandler.cs`

**Event:** `ColumnConfigUpdatedEvent`
**SignalR event name:** `ColumnConfigUpdated`
**Payload:** `{ sprintId, column1Name, column1Color, column2Name, column2Color, column3Name, column3Color }`
**Group:** `sprint:{sprintId}`

---

### Step 6: Modify sprint creation for column configuration

**What:** Extend the create sprint command, validator, and endpoint to accept optional column name/color fields. When omitted, the endpoint checks for the most recent sprint in the team and uses its column config as pre-fill defaults. If no previous sprint exists, system defaults apply.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintCommand.cs` -- add 6 optional nullable string properties to the command record; add conditional validation rules (same rules as UpdateColumns, but only when value is provided)
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintEndpoint.cs` -- before calling `Sprint.Create`, resolve column config: if any column fields provided in command, use them; otherwise query `sprintRepository.GetByTeamIdAsync` and take column config from the first result (most recent). Pass resolved values to `Sprint.Create`.

**Skill:** None (modifying existing feature, logic is unique).

**Important:** The pre-fill logic (copying from most recent sprint) lives in the **endpoint** (application layer), not in the domain. The domain `Sprint.Create` just receives resolved values. The frontend ALSO pre-fills from the sprint list response (for the form UI), but the backend is the fallback for API-only clients that omit the fields.

---

### Step 7: Update sprint response DTOs and all sprint-returning endpoints

**What:** Add column configuration fields to `SprintResponse` so every endpoint that returns sprint data includes column config. This enables the frontend to read column config from the sprint list for pre-fill.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` -- add 6 fields to `SprintResponse`: `Column1Name`, `Column1Color`, `Column2Name`, `Column2Color`, `Column3Name`, `Column3Color`
- `src/Services/Reflekt/Reflekt.API/Mappings/` -- if Sprint -> SprintResponse mapping needs explicit config for ColumnColor (value -> string), add it. Mapster should handle the ColumnColor.Value extraction with a custom mapping rule.

**Pattern:** Since `SprintResponse` is a shared record used by `CreateSprintEndpoint`, `GetSprintEndpoint`, `GetTeamSprintsEndpoint`, `StartSprintEndpoint`, `CloseSprintEndpoint`, `AdvancePhaseEndpoint`, and `UpdateColumnsEndpoint`, modifying the record propagates to all. Verify the Mapster mapping handles `ColumnColor` -> `string` conversion.

**Skill:** None (DTO extension, mapping config).

**Also update `SprintSummaryResponse`:** Add the 6 column config fields here too, since the participant-facing sprint summary endpoint needs column config for rendering.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` -- extend `SprintSummaryResponse`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` -- include column fields in construction

---

### Step 8: Refactor report endpoints and contracts for dynamic columns

**What:** Replace the hardcoded `ReportNotesSection(WentWell, DidntGoWell)` structure with a list of column sections. Each section carries its column name, color, and group entries. The three reflection columns appear in positional order (1, 2, 3), followed by Action Items.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs` -- replace `ReportNotesSection` with `ReportColumnSection(string ColumnName, string ColumnColor, List<ReportGroupEntry> Groups)`. Change `ReportResponse` to use `List<ReportColumnSection> Columns` instead of `ReportNotesSection Notes`.
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs` -- refactor: filter groupEntries by `ColumnType.Column1`, `ColumnType.Column2`, `ColumnType.Column3` (not semantic names); build 3 `ReportColumnSection` entries using `sprint.Column1Name`/`sprint.Column1Color` etc.; construct response with `Columns` list.
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs` -- same refactoring as above.

**Skill:** None (refactoring existing endpoint logic, unique to this feature).

**Response shape change:**
```
Before: { notes: { wentWell: [...], didntGoWell: [...] }, actionItems: [...] }
After:  { columns: [ { columnName, columnColor, groups: [...] }, ... ], actionItems: [...] }
```

---

### Step 9: EF Core migration

**What:** Add the migration that:
1. Adds 6 new columns to the `Sprints` table (with defaults so existing rows get populated)
2. Migrates `ColumnType` stored values in `Cards` and `CardGroups` tables: old ordinal 2 (ActionItems) becomes ordinal 3; add Column3 at ordinal 2 (no existing data at this ordinal)

**Commands:**
```bash
dotnet ef migrations add AddConfigurableColumns -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
dotnet ef database update -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
```

**Skill:** None (migration is feature-specific).

**Migration SQL for ColumnType ordinal shift (in the `Up` method):**
```sql
-- ActionItems was stored as ordinal 2, needs to become ordinal 3
UPDATE Cards SET ColumnType = 3 WHERE ColumnType = 2;
UPDATE CardGroups SET ColumnType = 3 WHERE ColumnType = 2;
```

This works because no existing data uses ordinal 2 for anything other than ActionItems, and no Column3 data exists yet.

**Dependencies:** Steps 1-3 must be complete before generating the migration.

---

### Step 10: Add color palette theme tokens to frontend CSS

**What:** Replace the hardcoded semantic column colors (`went-well`, `didnt-go-well`, `action-items`) with a dynamic 8-color palette. Each palette color gets a base + light variant. Keep the semantic colors as aliases initially for backward compat during the transition within this feature.

**Files to modify:**
- `web/reflekt-web/src/assets/main.css` -- add palette tokens under `@theme`:
  - `--color-col-green` / `--color-col-green-light`
  - `--color-col-red` / `--color-col-red-light`
  - `--color-col-amber` / `--color-col-amber-light`
  - `--color-col-blue` / `--color-col-blue-light`
  - `--color-col-purple` / `--color-col-purple-light`
  - `--color-col-teal` / `--color-col-teal-light`
  - `--color-col-pink` / `--color-col-pink-light`
  - `--color-col-slate` / `--color-col-slate-light`

**Skill:** Follow `tailwind-theme` (design tokens, @theme directive).

**Note:** The old semantic tokens (`--color-went-well`, etc.) can be removed once the board components are updated in Step 15. Remove them as part of that step.

---

### Step 11: Update sprint store for column config

**What:** Extend the `Sprint` TypeScript interface with column config fields. Extend `createSprint` action to pass column config in the request body. Add an `updateColumns` action for the new PUT endpoint.

**Files to modify:**
- `web/reflekt-web/src/stores/sprint.ts` -- add `column1Name`, `column1Color`, `column2Name`, `column2Color`, `column3Name`, `column3Color` to `Sprint` interface and `SprintSummary` interface; add `updateColumns(sprintId, payload)` action; modify `createSprint` to accept optional column config payload.

**Skill:** Follow `vue-patterns` (store actions), `pinia-patterns` (action patterns).

---

### Step 12: Update card store and card group store for positional column types

**What:** Change all column type filters from semantic values to positional values. Add a `Column3` getter/filter.

**Files to modify:**
- `web/reflekt-web/src/stores/card.ts` -- rename getters: `wentWell` -> `column1Cards`, `didntGoWell` -> `column2Cards`, `actionItems` -> `actionItemCards` (filter value changes: `'Column1'`, `'Column2'`, `'ActionItems'`); add `column3Cards` getter filtering `'Column3'`
- `web/reflekt-web/src/stores/cardGroup.ts` -- rename getters: `wentWellGroups` -> `column1Groups`, `didntGoWellGroups` -> `column2Groups`; add `column3Groups` getter; update filter values to positional strings

**Skill:** Follow `pinia-patterns` (getter patterns).

---

### Step 13: Update SignalR hub handler for `ColumnConfigUpdated`

**What:** Add a `ColumnConfigUpdated` event listener in the frontend SignalR composable that updates the sprint store's column config in-place when received.

**Files to modify:**
- `web/reflekt-web/src/composables/useSprintHub.ts` -- add `connection.on('ColumnConfigUpdated', (payload) => { ... })` handler that updates `sprintStore.currentSummary` column fields in-place

**Skill:** Follow `vue-patterns` (composable patterns).

**Payload shape:** `{ sprintId, column1Name, column1Color, column2Name, column2Color, column3Name, column3Color }`

---

### Step 14: Rewrite `CreateSprintForm` with column configuration

**What:** Add three column configuration rows below the existing sprint name and votes inputs. Each row has a text input (column name) and a color swatch picker (8-color palette). Pre-fill from the most recent sprint in the team's sprint list (available in the sprint store). If no previous sprint, use system defaults.

**Files to modify:**
- `web/reflekt-web/src/components/sprints/CreateSprintForm.vue` -- add column config UI (3 rows), pre-fill logic from `sprintStore.sprints[0]`, emit full payload including column config

**Skill:** Follow `vue-patterns` (component patterns, form reactivity).

**Pre-fill source:** `sprintStore.sprints[0]` (the list is already ordered by `CreatedAt DESC` from the API).

**Color swatch picker:** Inline in this component (8 small colored circles, click to select). No separate component needed for a simple 8-option picker.

---

### Step 15: Rewrite board components for 3 dynamic columns

**What:** Transform the board from 2 hardcoded reflection columns to 3 dynamic columns driven by sprint column config. Remove semantic color references. Remove per-column icons (the spec removes them). Update the mobile tab bar for 3 columns + Action Items.

**Files to modify:**
- `web/reflekt-web/src/composables/useRetroBoardActions.ts` -- replace `wentWell`/`didntGoWell` with `column1Cards`/`column2Cards`/`column3Cards`; replace group getters similarly; update `addNote` to accept `'Column1' | 'Column2' | 'Column3'`; add column3 to `boardGridClasses` (always 3+AI when showing AI, else 3 cols); rename `newCardText` reactive keys to `Column1`/`Column2`/`Column3`
- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue` -- render 3 `RetroColumnBody` instances driven by column config from sprint store (name, color, count); remove `SmilePlus`/`Frown` icon imports (no icons in v2); use dynamic column color class; update `newCardText` keys; update `onAddNote` for 3 columns
- `web/reflekt-web/src/components/retro/RetroMobileTabBar.vue` -- render 3 column tabs + Action Items tab; use dynamic names from sprint store; use dynamic colors; accept `modelValue: 'Column1' | 'Column2' | 'Column3' | 'ActionItems'`
- `web/reflekt-web/src/components/retro/RetroColumnPanel.vue` -- update to use palette color classes (`col-{color}`) instead of semantic classes; remove `headerIcon` prop (no icons in v2)
- `web/reflekt-web/src/assets/main.css` -- remove old semantic color tokens (`--color-went-well`, `--color-didnt-go-well`) that are now replaced by the palette

**Skill:** Follow `vue-patterns` (component architecture, reactive props).

**Column header edit (SM only):** On `RetroColumnPanel`, when the user is SM, clicking the column header triggers an inline edit mode (text input + color picker). This is the Flow 2 interaction from the spec. Non-SM participants see a plain header.

**Note:** Steps 14 and 15 both modify board-related components but do not conflict -- Step 14 is the creation form, Step 15 is the active board.

---

### Step 16: Rewrite report components and markdown export for dynamic columns

**What:** Update the report store types, `SprintReportPanel`, `ReportSection`, and `copyAsMarkdown` to work with the new dynamic column list response shape.

**Files to modify:**
- `web/reflekt-web/src/stores/report.ts` -- replace `ReportNotesSection` interface with `ReportColumnSection { columnName: string; columnColor: string; groups: ReportGroupEntry[] }`; change `ReportResponse.notes` to `columns: ReportColumnSection[]`; update `copyAsMarkdown` to iterate `columns` array using `columnName` as heading
- `web/reflekt-web/src/components/reports/SprintReportPanel.vue` -- replace hardcoded "Went Well" / "Didn't Go Well" `ReportSection` instances with a `v-for` over `report.columns`, passing `columnName`, `columnColor`, and `groups`
- `web/reflekt-web/src/components/reports/ReportSection.vue` -- accept `columnName` and `columnColor` props instead of hardcoded class props; derive header/title/border/link classes from the palette color
- `web/reflekt-web/src/views/ReportView.vue` -- no changes needed (it already delegates to `SprintReportPanel`)

**Skill:** Follow `vue-patterns` (component props, computed iteration).

---

## Cross-Service Changes

None. This is a single-service change within the Reflekt service.

## Migration Notes

**Commands:**
```bash
dotnet ef migrations add AddConfigurableColumns -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
dotnet ef database update -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
```

**Data migration within the `Up` method:**
- `UPDATE Cards SET ColumnType = 3 WHERE ColumnType = 2;` (ActionItems: ordinal 2 -> 3)
- `UPDATE CardGroups SET ColumnType = 3 WHERE ColumnType = 2;` (same)
- Sprint columns get default values via column defaults, so existing rows auto-populate

**Rollback consideration:** The `Down` method should reverse: `UPDATE Cards SET ColumnType = 2 WHERE ColumnType = 3;` and drop the 6 Sprint columns.

## Testing Strategy

**Backend:**
- Sprint creation with explicit column config stores correctly
- Sprint creation without column config uses system defaults
- Sprint creation inherits from most recent sprint in team when fields omitted
- `UpdateColumnConfig` validates: empty name rejected, whitespace-only rejected, >30 chars rejected, invalid color rejected
- `UpdateColumnConfig` succeeds in all sprint states (Draft, Active, Closed)
- Cards and groups with Column3 position work identically to Column1/Column2 in all phases
- Report endpoint returns 3 column sections with correct names and colors
- SignalR `ColumnConfigUpdated` event fires on column update
- Migration: verify ActionItems cards/groups have ColumnType = 3 after migration

**Frontend:**
- Create sprint form shows 3 column config rows with correct pre-fill
- Board renders 3 dynamic columns with correct names and colors
- SM can click column header to edit name/color inline
- Non-SM cannot trigger column header edit
- `ColumnConfigUpdated` SignalR event updates all column headers in real-time
- Mobile tab bar shows 3 column names + Action Items
- Report panel renders dynamic column sections
- Markdown export uses configured column names as headings

## Open Questions

None -- the spec is comprehensive and all design decisions are resolved.
