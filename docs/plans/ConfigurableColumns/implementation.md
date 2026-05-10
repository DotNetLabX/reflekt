# Configurable Reflection Columns — Implementation

## Files Created

- `src/Services/Reflekt/Reflekt.Domain/Sprints/ColumnColor.cs` — Value object with `Value` string property, static `Create(string)` factory validating against 8-color palette, static `IsValid(string)` helper, `AllowedColors` collection
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/ColumnConfigUpdatedEvent.cs` — Domain event record carrying Sprint reference, implements `IDomainEvent`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/UpdateColumns/UpdateColumnsCommand.cs` — Command record + `UpdateColumnsCommandValidator` with NotEmpty/MaxLength/Must(IsValid) rules for all 6 fields
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/UpdateColumns/UpdateColumnsEndpoint.cs` — `PUT /api/sprints/{sprintId}/columns`, ScrumMaster auth, loads sprint then verifies team ownership, calls `sprint.UpdateColumnConfig`, saves, responds with SprintResponse
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/UpdateColumns/BroadcastColumnConfigUpdatedHandler.cs` — IEventHandler for `ColumnConfigUpdatedEvent`, broadcasts `ColumnConfigUpdated` SignalR event to `sprint:{id}` group with all 6 config fields
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260510180219_AddConfigurableColumns.cs` — Adds 6 columns to Sprints table with defaults; includes SQL to shift ActionItems ColumnType ordinal from 2 to 3 in Cards and CardGroups

## Files Modified

- `src/Services/Reflekt/Reflekt.Domain/Cards/ColumnType.cs` — Renamed enum values from `{WentWell, DidntGoWell, ActionItems}` to `{Column1, Column2, Column3, ActionItems}`; ActionItems ordinal shifts from 2 to 3
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs` — Added 6 column config properties (`Column1Name`, `Column2Name`, `Column3Name` as string; `Column1Color`, `Column2Color`, `Column3Color` as `ColumnColor`) with system defaults
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` — Extended `Create` factory to accept 6 optional column config parameters; added `UpdateColumnConfig` behavior method with validation + `ColumnConfigUpdatedEvent`; added private `ValidateColumnName` helper
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs` — Added 6 property configs with required, maxlength, default values; `ColumnColor` properties use `HasConversion(c => c.Value, s => ColumnColor.Create(s))`
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — Updated seed data to use `Column1`/`Column2` instead of `WentWell`/`DidntGoWell`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` — Extended `SprintResponse` and `SprintSummaryResponse` with 6 column config fields
- `src/Services/Reflekt/Reflekt.API/Mappings/SprintMappings.cs` — Added explicit column color mappings (`s.Column1Color.Value` etc.) since Mapster doesn't auto-extract value objects
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` — Extended `SprintSummaryResponse` construction with 6 column config fields
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintCommand.cs` — Added 6 optional nullable column config params; conditional validators (MaxLength + NotEmpty when provided); color validator uses `ColumnColor.IsValid`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintEndpoint.cs` — Added pre-fill logic: if no column fields provided, queries most recent sprint in team and copies its config; passes resolved values to `Sprint.Create`
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs` — Replaced `ReportNotesSection(WentWell, DidntGoWell)` with `ReportColumnSection(ColumnName, ColumnColor, Groups)`; `ReportResponse.Notes` → `Columns: List<ReportColumnSection>`
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs` — Refactored to build 3 `ReportColumnSection` entries using sprint column config; extracted `BuildColumnSection` private helper
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs` — Same refactoring as `GetSprintReportEndpoint`
- `web/reflekt-web/src/assets/main.css` — Replaced semantic color tokens with 8-color palette tokens (`--color-col-{name}` / `--color-col-{name}-light`); kept `--color-action-items` and `--color-action-items-light` as fixed tokens
- `web/reflekt-web/src/stores/sprint.ts` — Extended `Sprint` and `SprintSummary` interfaces with 6 column config fields; added `ColumnConfigPayload` and `CreateSprintPayload` interfaces; changed `createSprint` signature to accept full payload object; added `updateColumns` action that updates store in-place after API call
- `web/reflekt-web/src/stores/card.ts` — Renamed getters from `wentWell`/`didntGoWell`/`actionItems` to `column1Cards`/`column2Cards`/`column3Cards`/`actionItemCards` with positional filter values
- `web/reflekt-web/src/stores/cardGroup.ts` — Renamed getters from `wentWellGroups`/`didntGoWellGroups` to `column1Groups`/`column2Groups`; added `column3Groups`; updated filter values to positional strings
- `web/reflekt-web/src/stores/report.ts` — Replaced `ReportNotesSection` with `ReportColumnSection`; changed `ReportResponse.notes` to `columns`; rewrote `copyAsMarkdown` to iterate `columns` array using `columnName` as heading
- `web/reflekt-web/src/composables/useSprintHub.ts` — Added `ColumnConfigUpdated` handler that updates `sprintStore.currentSummary` column fields in-place
- `web/reflekt-web/src/composables/useRetroBoardActions.ts` — Replaced `wentWell`/`didntGoWell` with `column1Cards`/`column2Cards`/`column3Cards`; replaced group getters similarly; updated `addNote` to accept positional column types; updated `boardGridClasses` for 4-col layout when AI visible; added `updateColumnConfig` handler
- `web/reflekt-web/src/components/retro/RetroColumnPanel.vue` — Removed `headerIcon` prop; switched CSS classes to `col-{color}` palette; added inline edit mode for SM (text input + color swatch picker on header click); added `isScrumMaster` prop; emits `update-config`
- `web/reflekt-web/src/components/retro/RetroColumnBody.vue` — Removed `headerIcon` prop; added `isScrumMaster` prop; added `update-config` emit passthrough
- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue` — Rewrote to render 3 dynamic columns from sprint store; removed icon imports; uses `column1Cards`/`column2Cards`/`column3Cards` from composable; action items column uses fixed `blue` color; handles `update-config` via `onUpdateColumnConfig`
- `web/reflekt-web/src/components/retro/RetroMobileTabBar.vue` — Updated for 3 dynamic columns with name/color props; model value type changed to `'Column1' | 'Column2' | 'Column3' | 'ActionItems'`
- `web/reflekt-web/src/components/retro/RetroCardItem.vue` — Widened `columnColor` prop from literal union to `string`; updated `cardBgClass` to use `col-{color}-light` palette tokens
- `web/reflekt-web/src/components/retro/RetroReportPanel.vue` — Replaced hardcoded went-well/didn't-go-well sections with `v-for` over `report.columns`; uses dynamic `col-{color}` CSS classes
- `web/reflekt-web/src/components/reports/SprintReportPanel.vue` — Replaced hardcoded `ReportSection` instances with `v-for` over `report.columns`; action items section uses `col-blue` tokens
- `web/reflekt-web/src/components/reports/ReportSection.vue` — Replaced `title`/`headerClass`/`titleClass`/`borderClass`/`linkClass` props with `columnName`/`columnColor`; derives all CSS from `col-{color}` palette
- `web/reflekt-web/src/components/sprints/CreateSprintForm.vue` — Added 3 column config rows (name input + 8-color swatch picker per row); pre-fills from most recent sprint on mount; emits full payload including column config
- `web/reflekt-web/src/components/sprints/SprintList.vue` — Updated `create-sprint` emit type to include column config fields
- `web/reflekt-web/src/views/SprintListView.vue` — Updated `handleCreateSprint` to pass full payload to `sprintStore.createSprint`

## Key Decisions

- `ColumnColor` is a value class (not a record) to allow the EF conversion factory pattern cleanly. `Equals`/`GetHashCode` overridden manually.
- `Sprint.Create` takes nullable optional column params rather than a builder pattern — matches the existing simple factory style.
- Pre-fill logic for sprint creation lives in the endpoint (application layer), not the domain — the domain just receives resolved values. This follows the plan's explicit instruction.
- `CreateSprintForm.vue` uses 3 separate `ref` objects instead of a reactive array to avoid TypeScript's `noUncheckedIndexedAccess` errors on array element access.
- `RetroCardItem.vue` `columnColor` prop widened from a specific literal union to `string` — the old constraint was a v1 artifact from hardcoded semantic column names.
- `TeamSelector.vue` TypeScript errors are pre-existing and not related to this feature (confirmed via `git diff --name-only`).
- The vite production build succeeds (`✓ built in 596ms`); only the type-check phase reports pre-existing `TeamSelector.vue` errors.

## Deviations from Plan

- None. All 16 steps implemented as specified.
