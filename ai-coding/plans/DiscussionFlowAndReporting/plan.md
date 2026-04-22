# Discussion Flow & Reporting

## Context

The Discussing and Reporting phases lack structure. Participants cannot see which group is being discussed, and the report is only accessible to the SM via a separate route. This feature adds a structured discussion flow (focus/discussed state) and redesigns the Reporting phase as a shared in-room experience with Report and Board views.

**Services impacted:** Reflekt (single service — domain, persistence, API, frontend).

## Scope

**In scope:**
- Discussion focus (one group at a time, SM-controlled)
- Discussed marking (auto on focus move, manual toggle by SM)
- Advance phase gate (minimum 2 discussed groups to reach Reporting)
- SM-only action items during Discussing phase
- Participant-accessible report endpoint
- Report filter change (discussed OR voteCount > 1)
- Reporting phase redesign (Report view + Board view toggle, in-room)
- Copy-as-markdown with discussed indicator
- New SignalR events for focus/discussed changes

**Explicitly out of scope:** Phase revert, automatic top-N, cross-sprint carry-forward, discussion timer, participant-driven discussion requests, AI summaries, per-group notes, ROTI rating, re-grouping during Discussion.

## Domain Model Changes

### Sprint (modified)
- Add `FocusedGroupId` (int?) — the currently focused group. Null when no group is focused. Sprint owns this because the "at most one per sprint" invariant is a sprint-level concern.

### CardGroup (modified)
- Add `DiscussedAt` (DateTime?) — null = not yet discussed. Set when marked as discussed, cleared on un-mark.

### New domain events
- `GroupFocusedEvent(Sprint sprint, int groupId, int? previousGroupId)` — raised when SM focuses a group.
- `GroupFocusCleared(Sprint sprint, int? previousGroupId)` — raised when focus is cleared (explicit or on phase advance).
- `GroupDiscussedChangedEvent(CardGroup group)` — raised when discussed state toggles.

### SprintAction enum additions
- `FocusGroup = 14`
- `ToggleDiscussed = 15`

## Data Model Changes

### CardGroups table
- Add column `DiscussedAt` (TEXT, nullable) to CardGroups.

### Sprints table
- Add column `FocusedGroupId` (INTEGER, nullable) to Sprints. Foreign key to CardGroups is NOT needed (Sprint references CardGroup by ID across aggregate boundaries per architecture — no navigation property).

### PhaseActions seed data
- Add `(Discussing, FocusGroup)` — enum values `(3, 14)`
- Add `(Discussing, ToggleDiscussed)` — enum values `(3, 15)`
- Add `(Discussing, CreateActionItem)` — enum values `(3, 3)` — already exists? **No: checking migration seed — Discussing phase (3) is not seeded at all currently.** The current seed only covers Writing (0), Grouping (1), and Voting (2). Discussing needs: CreateActionItem (3), EditActionItem (4), DeleteActionItem (5), AttachEvidenceLink (8), DetachEvidenceLink (9), FocusGroup (14), ToggleDiscussed (15), AdvancePhase (10).

### Migration
One migration covering: new columns on CardGroups and Sprints, new SprintAction enum values seeded into PhaseActions for Discussing phase.

## Implementation Steps

### Step 1: Domain — Sprint focus behavior + CardGroup discussed state

**What:** Add `FocusedGroupId` property to `Sprint`. Add behavior methods `SetFocus(int groupId)`, `ClearFocus()` to Sprint. Add `DiscussedAt` property to `CardGroup`. Add `MarkDiscussed()` and `UnmarkDiscussed()` methods to CardGroup. Add new domain events. Add `FocusGroup` and `ToggleDiscussed` to SprintAction enum.

**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/GroupFocusedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/GroupFocusClearedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/GroupDiscussedChangedEvent.cs`

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs` — add `FocusedGroupId` property
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` — add `SetFocus()` and `ClearFocus()` methods
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintAction.cs` — add `FocusGroup = 14`, `ToggleDiscussed = 15`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/CardGroup.cs` — add `DiscussedAt` property
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Behaviors/CardGroup.cs` — add `MarkDiscussed()`, `UnmarkDiscussed()` methods

**Pattern to follow:** Existing behavior split pattern in `Sprint.cs` / `Behaviors/Sprint.cs` and `CardGroup.cs` / `Behaviors/CardGroup.cs`.

**Sprint.SetFocus(int groupId) logic:**
- Guard: State must be Active, CurrentPhase must be Discussing.
- If `FocusedGroupId` has a value (previous focus), store it for the event payload.
- Set `FocusedGroupId = groupId`.
- Raise `GroupFocusedEvent(this, groupId, previousGroupId)`.
- Note: The auto-marking of the previous group as discussed is NOT done in the Sprint aggregate — it's done in the endpoint handler, because marking discussed is a mutation on a different aggregate (CardGroup). Sprint only manages its own `FocusedGroupId`.

**Sprint.ClearFocus() logic:**
- Guard: State must be Active, CurrentPhase must be Discussing.
- Store current `FocusedGroupId` as `previousGroupId`.
- Set `FocusedGroupId = null`.
- Raise `GroupFocusClearedEvent(this, previousGroupId)`.

**CardGroup.MarkDiscussed() / UnmarkDiscussed() logic:**
- `MarkDiscussed()`: Set `DiscussedAt = DateTime.UtcNow`, raise `GroupDiscussedChangedEvent(this)`.
- `UnmarkDiscussed()`: Set `DiscussedAt = null`, raise `GroupDiscussedChangedEvent(this)`.

**Dependencies:** None.

### Step 2: Persistence — Migration + entity configurations

**What:** Add EF Core migration for `DiscussedAt` on CardGroups and `FocusedGroupId` on Sprints. Update entity configurations. Seed new PhaseAction rows for Discussing phase.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardGroupConfiguration.cs` — add `DiscussedAt` property mapping (optional DateTime)
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs` — add `FocusedGroupId` property mapping (optional int)
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/PhaseActionConfiguration.cs` — add seed data for Discussing phase: `(3, 3)`, `(3, 4)`, `(3, 5)`, `(3, 8)`, `(3, 9)`, `(3, 10)`, `(3, 14)`, `(3, 15)`

**Files to create:**
- New migration via `dotnet ef migrations add AddDiscussionFlow -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API` (run from `src/` directory)

**Pattern to follow:** Existing `CardGroupConfiguration.cs` for entity config. Existing `PhaseActionConfiguration.cs` for seed data pattern.

**Performance:** Bulk seed insert via `HasData()` — standard EF Core seeding.

**Dependencies:** Step 1.

### Step 3: Backend — Focus and toggle-discussed endpoints + SignalR event handlers

**What:** Create three new endpoints: FocusGroup, ClearFocus, ToggleDiscussed. Create three SignalR broadcast handlers for the new domain events.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/FocusGroup/FocusGroupEndpoint.cs` — POST `/api/sprints/{sprintId}/card-groups/{groupId}/focus`. SM auth (cookie). Loads sprint, validates phase/state, loads previous focused group (if any) and marks it discussed, calls `sprint.SetFocus(groupId)`, saves. Returns `{ focusedGroupId, previousGroupId?, previousGroupDiscussed? }`.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ClearFocus/ClearFocusEndpoint.cs` — DELETE `/api/sprints/{sprintId}/focus`. SM auth (cookie). Loads sprint, loads focused group and marks it discussed, calls `sprint.ClearFocus()`, saves. Returns `{ previousGroupId?, previousGroupDiscussed? }`.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ToggleDiscussed/ToggleDiscussedEndpoint.cs` — POST `/api/sprints/{sprintId}/card-groups/{groupId}/toggle-discussed`. SM auth (cookie). Loads group, toggles discussed state, saves. Returns `{ groupId, isDiscussed, discussedAt? }`.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/FocusGroup/BroadcastGroupFocusedHandler.cs` — IEventHandler<GroupFocusedEvent>, sends "GroupFocused" to sprint group.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ClearFocus/BroadcastGroupFocusClearedHandler.cs` — IEventHandler<GroupFocusClearedEvent>, sends "GroupFocusCleared" to sprint group.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ToggleDiscussed/BroadcastGroupDiscussedChangedHandler.cs` — IEventHandler<GroupDiscussedChangedEvent>, sends "GroupDiscussedChanged" to sprint group.

**Pattern to follow:** `AdvancePhaseEndpoint.cs` for SM-auth endpoint pattern. `BroadcastPhaseChangedHandler.cs` for event handler pattern. Use `create-feature` skill for endpoint structure (FastEndpoints variant).

**Key implementation detail for FocusGroup endpoint:**
1. Load sprint, verify Active + Discussing phase.
2. Load target group, verify it belongs to this sprint.
3. Check phase action cache: `IsAllowed(Discussing, FocusGroup)`.
4. If sprint has a `FocusedGroupId` (previous focus):
   a. Load previous group from `CardGroupRepository`.
   b. Call `previousGroup.MarkDiscussed()`.
5. Call `sprint.SetFocus(groupId)`.
6. Single `SaveChangesAsync` — both mutations saved atomically. Domain events dispatched after save.

**Dependencies:** Steps 1, 2.

### Step 4: Backend — Modify AdvancePhase endpoint for discussed gate + auto-discussed on advance

**What:** Add the minimum-2-discussed gate and auto-mark-focused-as-discussed logic to the existing AdvancePhase endpoint. When advancing from Discussing to Reporting: (1) auto-mark currently focused group as discussed, (2) clear focus, (3) check at least 2 groups are discussed, (4) advance phase.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs` — add a new block when `previousPhase == SprintPhase.Discussing`: load focused group and mark discussed if exists, clear focus on sprint, count discussed groups, reject with 409 if < 2, then proceed with `sprint.AdvancePhase()`.

**Pattern to follow:** Existing `previousPhase == SprintPhase.Writing` block in the same file for phase-transition-specific logic.

**Dependencies:** Steps 1, 2.

### Step 5: Backend — SM-only action item authorization during Discussing

**What:** Tighten action item endpoints (CreateCard, EditCard, DeleteCard) to SM-only during Discussing phase. When phase is Discussing and the operation is on an ActionItem, the caller must be authenticated via cookie (SM), not just guest token.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs` — after phase action check, if phase is Discussing and cardType is ActionItem, verify caller has cookie auth (SM). If not, send 403.
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs` — same SM check for ActionItem edits during Discussing.
- `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs` — same SM check for ActionItem deletes during Discussing.

**Implementation approach:** Check `User.FindFirstValue(ClaimTypes.Email)` — if null (guest token only), send 403. The SM is identified by having a cookie-auth identity with an email claim. This matches the pattern in `AdvancePhaseEndpoint.cs`.

**Dependencies:** Steps 1, 2.

### Step 6: Backend — Participant-accessible report endpoint + report filter change

**What:** Create new endpoint GET `/api/sprints/{sprintId}/report` accessible with guest token. Modify report query logic to filter groups where `isDiscussed OR voteCount > 1` (was: `voteCount > 0`). Add `isDiscussed` field to report response contracts.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs` — GET `/api/sprints/{sprintId}/report`. Auth: guest token (Participant policy). Loads sprint by ID, verifies participant's sprintId claim matches. Reuses report building logic. Includes `isDiscussed` per group.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs` — add `IsDiscussed` (bool) to `ReportGroupEntry`.
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs` — update filter from `.Where(x => x.Votes > 0)` to `.Where(x => x.IsDiscussed || x.Votes > 1)`. Add `IsDiscussed` to group mapping. Consider extracting shared report-building logic into a static helper method in `ReportContracts.cs` so both endpoints can reuse it.

**Pattern to follow:** `GetSprintReportEndpoint.cs` for report building. `GetCurrentParticipantEndpoint.cs` for guest-token auth pattern.

**Performance:** Single query per table — existing bulk approach is correct.

**Dependencies:** Steps 1, 2.

### Step 7: Backend — Update CardGroup response contracts

**What:** Add `isDiscussed` and `isFocused` fields to the CardGroup response so the frontend knows the state of each group.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CardGroupContracts.cs` — add `bool IsDiscussed` and `bool IsFocused` to `CardGroupResponse`. Update `CardGroupMappings.ToResponse()` to accept these values.
- All call sites of `CardGroupMappings.ToResponse()` — update to pass the new fields. Find with grep.

**Dependencies:** Steps 1, 2.

### Step 8: Frontend — CardGroup store + SignalR hub updates for focus/discussed

**What:** Add `isDiscussed` and `isFocused` to the `CardGroup` interface. Add actions for focus/discussed API calls. Add SignalR handlers for `GroupFocused`, `GroupFocusCleared`, `GroupDiscussedChanged`.

**Files to modify:**
- `web/reflekt-web/src/stores/cardGroup.ts` — add `isDiscussed: boolean` and `isFocused: boolean` to `CardGroup` interface. Add `focusGroup(sprintId, groupId)`, `clearFocus(sprintId)`, `toggleDiscussed(sprintId, groupId)` actions. Add a getter `focusedGroup` that returns the group with `isFocused === true`.
- `web/reflekt-web/src/composables/useSprintHub.ts` — add handlers for `GroupFocused` (update `isFocused` on groups), `GroupFocusCleared` (clear all `isFocused`), `GroupDiscussedChanged` (update `isDiscussed` on the target group).

**Dependencies:** Step 7.

### Step 9: Frontend — RetroRoomView Discussing phase enhancements

**What:** Update RetroRoomView to show focus/discussed UI during Discussing phase. Add "Discuss" button on groups (SM only), discussed indicators on groups, SM-only action item form visibility. Add advance-phase confirmation dialog with discussed count.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue` — 
  1. On `CardGroupItem`: pass `isFocused` and `isDiscussed` props. Show "Discuss" button for SM during Discussing phase. Show discussed indicator (checkmark badge) on discussed groups.
  2. Action items column: show the creation form only if SM (`isScrumMaster`), not just if the phase action is allowed. Participants see a read-only list.
  3. Add advance-phase confirmation dialog: when SM clicks advance and phase is Discussing, show "You've discussed N of M groups. Advance to Reporting?" Count from `cardGroupStore.groups.filter(g => g.isDiscussed).length` and total groups. On confirm, call `advancePhase`. Handle 409 (< 2 discussed) with a toast.
- `web/reflekt-web/src/components/CardGroupItem.vue` — add props for `isFocused` and `isDiscussed`. Apply visual highlight (border/bg) when focused. Show discussed checkmark badge. Add "Discuss" button (emits event). Add "Toggle Discussed" control for SM.

**Dependencies:** Step 8.

### Step 10: Frontend — Reporting phase redesign (Report view + Board view toggle)

**What:** When phase is Reporting, the RetroRoomView renders a toggle between Report view and Board view. Report view is the default — shows the report inline. Board view shows the existing board in read-only mode.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue` — when `currentPhase === 'Reporting'`, replace the board with a toggled view:
  1. Add a `reportingView` ref: `'report' | 'board'`, defaulting to `'report'`.
  2. In Report mode: render the report inline (sprint info, participants, notes filtered/sorted per spec, action items). Include "Copy as markdown" button.
  3. In Board mode: render the existing board columns but fully read-only (no add forms, no edit/delete controls, no drag). Groups show discussed marks and vote counts.
  4. Toggle switch visible to all participants.
- `web/reflekt-web/src/stores/report.ts` — add `fetchParticipantReport(sprintId)` action that calls GET `/api/sprints/{sprintId}/report` (the new participant-accessible endpoint). Update `ReportGroupEntry` interface to include `isDiscussed: boolean`. Update `copyAsMarkdown()` to include discussed/undiscussed indicator per group.

**Pattern to follow:** Existing `ReportView.vue` for report rendering style. Adapt to work inline within RetroRoomView.

**Dependencies:** Steps 6, 8, 9.

### Step 11: Verify build + smoke test

**What:** Run `dotnet build` for backend. Run `npm run build` for frontend. Verify zero errors.

**Files:** None created/modified.

**Dependencies:** All previous steps.

## Migration Notes

```bash
# From src/ directory:
dotnet ef migrations add AddDiscussionFlow -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API

# Apply (auto in dev, or manual):
dotnet ef database update -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
```

The migration adds:
- `DiscussedAt` (TEXT, nullable) to CardGroups
- `FocusedGroupId` (INTEGER, nullable) to Sprints
- 8 new rows in PhaseActions for the Discussing phase

## Testing Strategy

**Key scenarios:**
1. SM focuses group A → group A highlighted for all clients
2. SM focuses group B → group A auto-discussed, group B focused
3. SM toggles discussed off on group A → discussed removed
4. SM advances from Discussing with 1 discussed group → 409 rejected
5. SM advances from Discussing with 2+ discussed groups → confirmation → auto-discuss focused → phase advances
6. Participant calls CreateActionItem during Discussing → 403
7. SM calls CreateActionItem during Discussing → 201 success
8. Participant fetches `/api/sprints/{id}/report` → gets report with isDiscussed
9. Report filter: group with 1 vote + not discussed → excluded. Group with 1 vote + discussed → included. Group with 2 votes + not discussed → included.
10. Reporting phase: both SM and participant see Report view with toggle to Board view
11. Copy as markdown includes discussed indicator
12. Reconnecting participant sees current focus state

## Open Questions

None — the spec is complete and all decisions are derivable from the codebase.
