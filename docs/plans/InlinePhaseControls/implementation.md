# Inline Phase Controls — Implementation

## Files Created

- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseCheckEndpoint.cs` — New pre-check endpoint `POST /api/teams/{teamId}/sprints/{sprintId}/advance-phase/check`. Returns `AdvancePhaseCheckResponse` with phase-specific data (nonVotingParticipantCount for Voting, discussedGroupCount+focusedGroupId for Discussing). Auth: SM policy. Guard ordering: team ownership → sprint active + not Reporting → compute counts → respond.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CloseSprint/BroadcastSprintClosedHandler.cs` — Domain event handler for `SprintClosedEvent`. Broadcasts `SprintClosed` with `{ sprintId }` to the sprint's SignalR group. Follows `BroadcastPhaseChangedHandler` pattern.
- `web/reflekt-web/src/stores/readiness.ts` — Pinia store for ephemeral readiness state. State: `readyParticipantIds`, `readyCount`, `totalCount`. Actions: `setFromSnapshot`, `applyChange`, `reset`. Getter: `isReady(participantId)`.
- `web/reflekt-web/src/composables/useReadiness.ts` — Composable wrapping readiness store. Exposes `toggleReady(sprintId)` (invokes `ToggleReady` hub method via `getHubConnection()`), `isCurrentParticipantReady`, `readyCount`, `totalCount`, `readyParticipants` (names), `notReadyParticipants` (names for SM tooltip).

## Files Modified

- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` — Added `TeamId` and `State` parameters to `SprintSummaryResponse` record.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` — Updated constructor call to pass `sprint.TeamId` and `sprint.State.ToString()`.
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/VoteRepository.cs` — Added `GetParticipantIdsWithVotesInSprintAsync(int sprintId, CancellationToken ct)`. Joins through CardGroups since Vote has no SprintId — uses `Db.CardGroups.Any(g => g.Id == v.GroupId && g.SprintId == sprintId)` subquery. Returns distinct participant IDs.
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` — Complete rewrite. Added static `ReadinessState` (`ConcurrentDictionary<int, ConcurrentDictionary<int, string>>`), static `ConnectionState` (`ConcurrentDictionary<int, ConcurrentDictionary<string, int>>`). Added `ToggleReady(int sprintId)` hub method. Modified `JoinSprint` to track connections and send `ReadinessSnapshot` to caller. Modified `LeaveSprint` to remove from connection tracking. Modified `OnDisconnectedAsync` to clean up readiness + connections and broadcast `ReadinessChanged` if needed. Added static `ClearReadiness(int sprintId)` and `GetParticipantCount(int sprintId)`.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/BroadcastPhaseChangedHandler.cs` — After broadcasting `PhaseChanged`, calls `SprintHub.ClearReadiness(sprintId)` and broadcasts `ReadinessReset` to the sprint group.
- `web/reflekt-web/src/stores/sprint.ts` — Added `teamId: number` and `state: string` to `SprintSummary` interface. Added exported `AdvancePhaseCheckResponse` interface. Added `advancePhaseCheck(teamId, sprintId)` action calling the new pre-check endpoint.
- `web/reflekt-web/src/composables/useSprintHub.ts` — Exported `getHubConnection()` function for composable access. Added `useReadinessStore` import. Added handlers for `SprintClosed` (sets `currentSummary.state = 'Closed'`), `ReadinessChanged` (delegates to `readinessStore.applyChange`), `ReadinessReset` (delegates to `readinessStore.reset`), `ReadinessSnapshot` (delegates to `readinessStore.setFromSnapshot`).
- `web/reflekt-web/src/composables/useRetroRoom.ts` — Added `useReadinessStore` import. Added `readinessStore.reset()` call in `enterRoom` before `connect()`. Added computed properties: `isClosed`, `teamId`, `isScrumMaster`. Exposed all three in return.
- `web/reflekt-web/src/components/retro/RetroPhaseHeader.vue` — Complete rewrite into 3-row consolidated top bar. Row 1: sprint name | readiness badge + votes badge + advance/close button (SM-only). Row 2: PhaseIndicator (moved from RetroRoomView). Row 3: RetroParticipantList (moved from RetroRoomView). SM readiness tooltip shows ready/not-ready names on hover. Votes badge scoped to Voting phase only. Close Sprint button appears only during Reporting.
- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue` — Added "I'm done" toggle button above board columns. Visible when `showReadinessButton` prop is true (Writing/Voting with >1 participant). Button style toggles filled/outlined based on `isReady` prop. Emits `toggle-ready`.
- `web/reflekt-web/src/views/RetroRoomView.vue` — Complete rewrite. Removed standalone `<PhaseIndicator>` and `<RetroParticipantList>` (now inside RetroPhaseHeader). Passes all new props to RetroPhaseHeader. Added dialog state (`showAdvanceDialog`, `showCloseDialog`, `advanceDialogTitle`, `advanceDialogMessage`, `advanceError`). Added handlers: `onAdvanceClick` (calls pre-check, builds dialog message per phase), `confirmAdvance`, `onCloseClick`, `confirmClose`. Wired `@toggle-ready` to `onToggleReady`. Added `isClosed` banner. Added `watch(isClosed)` to show toast on sprint closure. Added BaseModal dialogs for advance and close.
- `web/reflekt-web/src/components/sprints/SprintDetailPanel.vue` — Removed "Advance to {phase}" button, "Close Sprint" button, `nextPhaseLabel` computed, and `advance-phase`/`close` emit declarations.
- `web/reflekt-web/src/views/SprintView.vue` — Removed `handleAdvancePhase` and `handleClose` functions. Removed `@advance-phase` and `@close` event bindings on SprintDetailPanel.

## Key Decisions

- **Vote SprintId join**: Vote entity has no `SprintId` — used a correlated subquery through `CardGroups` rather than adding a denormalized column or changing the domain model.
- **Connection tracking uses connectionId → participantId map**: Allows correct unique-participant counting when one participant has multiple tabs. `totalCount` = `connections.Values.Distinct().Count()`.
- **Pre-check endpoint vs. single-call**: Followed the plan's two-call pattern. Advance endpoint remains idempotent; pre-check provides context for the confirmation dialog without advancing state.
- **`getHubConnection()` export**: Added to `useSprintHub.ts` module to give `useReadiness` composable access to the live connection without prop-drilling or store indirection.
- **Readiness reset in `enterRoom`**: Clears stale client-side readiness state before connecting, ensuring the `ReadinessSnapshot` from the server is the authoritative source.

## Deviations from Plan

- Step 4 plan mentions `GetParticipantCount(int sprintId)` as a static method on SprintHub — implemented as specified but not called from outside yet (no current caller beyond potential future use). It's available if needed.
- `RetroRoomView.vue` was a full rewrite rather than incremental modification because the component restructuring (removing PhaseIndicator/RetroParticipantList, adding dialogs, wiring new composables) was extensive enough that incremental edits would have been error-prone.
