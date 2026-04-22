# Discussion Flow & Reporting — Implementation

## Files Created

- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/GroupFocusedEvent.cs` — domain event for group focus changes
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/GroupFocusClearedEvent.cs` — domain event for clearing focus
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/GroupDiscussedChangedEvent.cs` — domain event for discussed state toggle
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/FocusGroup/FocusGroupEndpoint.cs` — POST endpoint for SM to focus a group
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/FocusGroup/BroadcastGroupFocusedHandler.cs` — SignalR broadcast for focus
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ClearFocus/ClearFocusEndpoint.cs` — DELETE endpoint for SM to clear focus
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ClearFocus/BroadcastGroupFocusClearedHandler.cs` — SignalR broadcast for focus cleared
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ToggleDiscussed/ToggleDiscussedEndpoint.cs` — POST endpoint for SM to toggle discussed
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ToggleDiscussed/BroadcastGroupDiscussedChangedHandler.cs` — SignalR broadcast for discussed change
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs` — GET participant-accessible report endpoint
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260422182511_AddDiscussionFlow.cs` — EF migration adding columns + seed data
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260422182511_AddDiscussionFlow.Designer.cs` — migration designer

## Files Modified

- `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs` — added `FocusedGroupId` property
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` — added `SetFocus()` and `ClearFocus()` methods with Active+Discussing guards
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintAction.cs` — added `FocusGroup = 14`, `ToggleDiscussed = 15`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/CardGroup.cs` — added `DiscussedAt` property
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Behaviors/CardGroup.cs` — added `MarkDiscussed()` and `UnmarkDiscussed()` methods
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardGroupConfiguration.cs` — mapped `DiscussedAt` property
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs` — mapped `FocusedGroupId` property
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/PhaseActionConfiguration.cs` — added FocusGroup and ToggleDiscussed seed rows for Discussing phase
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs` — added Discussing->Reporting gate: auto-mark focused group as discussed, clear focus, require minimum 2 discussed groups
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs` — SM-only check for ActionItem creation during Discussing
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs` — SM-only check for ActionItem edits during Discussing
- `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs` — SM-only check for ActionItem deletes during Discussing
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs` — added `IsDiscussed` to `ReportGroupEntry`
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs` — updated filter to `isDiscussed || votes > 1`, added IsDiscussed mapping
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CardGroupContracts.cs` — added `IsDiscussed` and `IsFocused` to response, updated mapping signature
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/GetGroups/GetGroupsEndpoint.cs` — passed `isFocused` from sprint's FocusedGroupId
- `web/reflekt-web/src/stores/cardGroup.ts` — added `isDiscussed`/`isFocused` to interface, `focusedGroup` getter, three new actions
- `web/reflekt-web/src/composables/useSprintHub.ts` — added SignalR handlers for GroupFocused, GroupFocusCleared, GroupDiscussedChanged
- `web/reflekt-web/src/components/CardGroupItem.vue` — added `isDiscussing` prop, focus/discussed visual indicators, Discuss button, Toggle Discussed control
- `web/reflekt-web/src/views/RetroRoomView.vue` — added focus/discussed event handling, SM-only action item form during Discussing, Reporting phase toggle (Report/Board views), inline report rendering
- `web/reflekt-web/src/stores/report.ts` — added `isDiscussed` to ReportGroupEntry, `fetchParticipantReport` action, discussed indicator in copyAsMarkdown

## Key Decisions

- Used `OwnerOrScrumMaster` policy for the participant report endpoint to allow both SM and participants to access it, matching the existing `GetSprintReportEndpoint` auth pattern
- The `groupBgClass` in CardGroupItem is now a computed (was a static string) to dynamically reflect focus/discussed state
- Reporting phase board view reuses the same board columns with all existing controls — no special read-only mode needed since the phase action cache already prevents mutations in Reporting phase

## Review Cycle 1 Fixes

### [CRITICAL] Migration missing 6 Discussing-phase PhaseAction rows — FALSE POSITIVE
The reviewer assumed the 6 Discussing-phase rows (CreateActionItem, EditActionItem, DeleteActionItem, AttachEvidenceLink, DetachEvidenceLink, AdvancePhase) were missing from migrations entirely. In fact, they were already seeded by the prior `20260422115641_AddCardGrouping` migration. The `AddDiscussionFlow` migration correctly only inserts the 2 new rows (FocusGroup=14, ToggleDiscussed=15). No code change needed.

### [MEDIUM] Three existing CardGroup response call sites omit isFocused — FIXED
Updated `AddCardToGroupEndpoint.cs`, `CreateGroupEndpoint.cs`, and `UpdateGroupTitleEndpoint.cs` to pass `sprint.FocusedGroupId == group.Id` as the `isFocused` argument to `CardGroupMappings.ToResponse()`.

### [MEDIUM] GetParticipantReport uses OwnerOrScrumMaster — ACKNOWLEDGED
Intentional deviation documented in Key Decisions. Matches existing `GetSprintReportEndpoint` pattern.

### [LOW] FocusGroup previousGroupDiscussed always true — ACKNOWLEDGED
Acceptable behavior since SignalR carries ground truth. No change needed.

### [LOW] ClearFocus reuses FocusGroup phase action — ACKNOWLEDGED
Pragmatic reuse — both are focus-management operations. No change needed.

## Deviations from Plan

- None
