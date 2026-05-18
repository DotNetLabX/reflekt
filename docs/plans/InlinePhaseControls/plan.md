# Inline Phase Controls

## Context

In v1, the Scrum Master must leave the retro room and navigate to a separate sprint manager page to advance phases or close the sprint. Every phase transition requires a context switch away from the team. This feature moves all live-retro controls (phase advancement, sprint closure) into the sprint board's top bar, so the SM never leaves the room. A participant readiness indicator ("I'm done") gives the SM visibility into pacing during Writing and Voting phases.

**Service impacted:** Reflekt (single service -- API, Domain, Persistence, and frontend).

## Scope

**In scope:**
- Restructure the retro room top bar into a consolidated 3-row layout (sprint name + controls, phase stepper, participant list)
- Add "Next: {phase}" button (SM-only) to the top bar during Writing through Discussing
- Add "Close Sprint" button (SM-only) to the top bar during Reporting
- Confirmation dialogs for all phase advances and sprint closure
- Voting -> Discussing confirmation shows count of non-voting participants (server-provided)
- Discussing -> Reporting confirmation shows discussed group count; gate at minimum 2 discussed
- Auto-mark focused group as discussed when advancing from Discussing
- Add `teamId` to `SprintSummaryResponse` so the retro room can call team-scoped endpoints
- Add `nonVotingParticipantCount` to the AdvancePhase response when advancing from Voting
- Create `BroadcastSprintClosedHandler` for SprintClosed SignalR broadcast
- Ephemeral readiness system: `ToggleReady` hub method, in-memory state, `ReadinessChanged`/`ReadinessReset`/`ReadinessSnapshot` SignalR events
- Readiness badge ("4/6 done") visible to all during Writing/Voting; SM tooltip with names
- Remove "Advance Phase" and "Close Sprint" buttons from the sprint manager page
- Votes remaining badge scoped to Voting phase only (was visible in all phases)

**Out of scope:**
- Phase revert / undo (backlog #22)
- Auto-advance on timer expiry (separate feature #29)
- Readiness as a hard gate (informational only per spec)
- Per-participant voting breakdown (count only, not names)
- Readiness during Grouping or Discussing
- Participant-initiated phase advance requests
- Close Sprint from non-Reporting phases

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | -- | Extend `SprintSummaryResponse` with `teamId`, unique to this DTO | |
| 2 | (none) | -- | Add `nonVotingParticipantCount` to advance-phase response, unique query logic | |
| 3 | create-domain-event-handler | Follow | `BroadcastSprintClosedHandler` for `SprintClosedEvent`, SignalR broadcast | |
| 4 | (none) | -- | Readiness state tracking in `SprintHub` with in-memory `ConcurrentDictionary`, unique ephemeral pattern | Gap: no skill for ephemeral hub state |
| 5 | vue-patterns | Follow | `SprintSummaryResponse` type update, `advancePhase`/`closeSprint` actions in sprint store | |
| 6 | vue-patterns | Follow | `SprintClosed` + readiness SignalR handlers in `useSprintHub` | |
| 7 | vue-patterns | Follow | New `useReadiness` composable for `ToggleReady` invocation and readiness state | |
| 8 | vue-patterns | Follow | Rewrite `RetroPhaseHeader` into consolidated 3-row top bar | |
| 9 | vue-patterns | Follow | Confirmation dialog components for phase advance and sprint closure | |
| 10 | vue-patterns | Follow | Readiness badge + "I'm done" button components | |
| 11 | vue-patterns | Follow | Remove advance/close from `SprintDetailPanel`, update `SprintView` | |
| 12 | vue-patterns | Follow | Wire everything in `RetroRoomView` and `useRetroRoom` | |

## Domain Model Changes

**No domain model changes.** All existing domain entities, aggregates, events, and value objects remain unchanged. The `SprintClosedEvent` already exists in the domain (`Reflekt.Domain.Sprints.Events.SprintClosedEvent`) and is already raised by `Sprint.Close()` -- it just lacks a handler.

Readiness state is ephemeral (in-memory on the hub) and introduces no domain entities.

## Data Model Changes

**No data model changes.** No new tables, columns, or migrations. The only backend data change is extending two response DTOs with fields derived from existing data.

## Implementation Steps

### Step 1: Extend `SprintSummaryResponse` with `teamId` and `state`

**What:** Add `TeamId` and `State` to `SprintSummaryResponse`. The frontend needs `teamId` to construct team-scoped API calls (`/api/teams/{teamId}/sprints/{sprintId}/advance-phase` and `/close`). It needs `state` to detect sprint closure (`Active` -> `Closed`) and render the board as read-only after the `SprintClosed` SignalR event updates the local summary.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` -- add `int TeamId` and `string State` parameters to the `SprintSummaryResponse` record
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` -- pass `sprint.TeamId` and `sprint.State.ToString()` when constructing the response

**Skill:** None (simple DTO extension).

---

### Step 2: Add non-voting participant count to AdvancePhase response

**What:** When advancing from Voting to Discussing, the SM needs to see how many participants have not cast any votes. The spec says this count should come from the server (votes are anonymous, frontend cannot compute it). Approach: add a new response type for AdvancePhase that includes an optional `nonVotingParticipantCount`, populated only when the current phase is Voting (before advance). The endpoint returns this in the response so the frontend can show the confirmation dialog with the count.

**Design decision -- two-step flow (spec deviation):** The spec says "when the SM requests a phase advance from Voting, the response includes the count." This implies a single-call pattern. However, the spec also requires a confirmation dialog *before* the advance occurs (Flow 2: "A confirmation dialog appears with context: N participants haven't voted yet"). These two requirements are in tension -- the confirmation must show the count before the advance happens, but the advance endpoint returns data after the advance. Resolution: a two-call pattern:
1. Frontend calls a new **pre-check endpoint** `POST /api/teams/{teamId}/sprints/{sprintId}/advance-phase/check` that returns the count (and discussed count for Discussing) without advancing.
2. After SM confirms, frontend calls the existing `advance-phase` endpoint to actually advance.

This keeps the advance endpoint idempotent and avoids returning data about a state that no longer exists. The spec's "simplest approach" description was about server-side computation (which we follow), not the call pattern.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseCheckEndpoint.cs` -- new endpoint. Route: `POST /api/teams/{teamId}/sprints/{sprintId}/advance-phase/check`. Auth: SM policy. Returns `AdvancePhaseCheckResponse`. Logic: loads sprint, validates it's Active and not in Reporting phase, then:
  - If current phase is Voting: count participants with zero votes (query all participants in sprint, then for each check if they have any votes -- use a bulk query, not N+1). Return `{ currentPhase, nonVotingParticipantCount, totalParticipantCount }`.
  - If current phase is Discussing: count discussed groups, check focused group. Return `{ currentPhase, discussedGroupCount, focusedGroupId }`.
  - Other phases: return `{ currentPhase }` (no extra data needed for Writing -> Grouping or Grouping -> Voting).

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseCheckEndpoint.cs` -- endpoint + response records

**Repository method needed:** Add `GetParticipantIdsWithVotesInSprintAsync(int sprintId, CancellationToken ct)` to `VoteRepository` that returns the distinct set of participant IDs who have at least one vote in any group belonging to the sprint. This enables a single query to determine non-voting participants.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/VoteRepository.cs` -- add `GetParticipantIdsWithVotesInSprintAsync` method

**Skill:** None (unique query logic and endpoint, no existing skill covers pre-check patterns).

**Performance approach:** Bulk query -- one query gets all participant IDs with votes, one query gets all participant IDs in sprint. Difference = non-voters. No N+1.

**Guard ordering:** Auth policy -> load team -> verify ownership -> load sprint -> verify Active + not Reporting -> compute counts -> respond.

---

### Step 3: Create `BroadcastSprintClosedHandler`

**What:** The `SprintClosedEvent` domain event exists and is raised by `Sprint.Close()`, but no handler broadcasts it to connected participants. Create a handler that sends a `SprintClosed` SignalR event to the sprint's group.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CloseSprint/BroadcastSprintClosedHandler.cs`

**Skill:** Follow `create-domain-event-handler`.

**Pattern reference:** `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/BroadcastPhaseChangedHandler.cs`

**Event:** `SprintClosedEvent`
**SignalR event name:** `SprintClosed`
**Payload:** `{ sprintId }`
**Group:** `sprint:{sprintId}`

---

### Step 4: Add readiness state tracking to SprintHub

**What:** Implement ephemeral readiness tracking as in-memory state on the server. Readiness is not persisted -- it lives in a `ConcurrentDictionary` scoped to the hub's lifetime. The hub gains a `ToggleReady` method and readiness-related event broadcasting.

**State structure:** A static `ConcurrentDictionary<int, ConcurrentDictionary<int, string>>` keyed by `sprintId`, where inner dict maps `participantId` to `displayName` for ready participants. Static because the hub is transient (new instance per invocation).

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` -- add:
  - Static `ReadinessState` field (the `ConcurrentDictionary`)
  - `ToggleReady(int sprintId)` hub method: extract `participantId` and `displayName` from claims, toggle the participant in the readiness dict, broadcast `ReadinessChanged` to the sprint group with `{ participantId, displayName, isReady, readyCount, totalCount }`. Total count comes from the group's connection count (tracked via a separate `ConcurrentDictionary<int, int>` tracking connection count per sprint).
  - Modify `JoinSprint`: after adding to group, increment connection count, send `ReadinessSnapshot` to the *calling client only* (`Clients.Caller`) with `{ readyParticipantIds, readyCount, totalCount }`.
  - Modify `OnDisconnectedAsync`: remove participant from readiness dict if present, decrement connection count. Broadcast `ReadinessChanged` if state changed.
  - Static `ClearReadiness(int sprintId)` method: clears the readiness dict for a sprint. Called externally from the PhaseChanged handler.
  - Static `GetParticipantCount(int sprintId)` method: returns connection count.

**Files to modify (for reset on phase advance):**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/BroadcastPhaseChangedHandler.cs` -- after broadcasting `PhaseChanged`, call `SprintHub.ClearReadiness(sprintId)` and broadcast `ReadinessReset` to the sprint group.

**Skill:** None (ephemeral hub state is a unique pattern not covered by existing skills).

**Connection tracking note:** The hub needs to track connected participant count per sprint to compute `totalCount` for readiness. Track this via a separate `ConcurrentDictionary<int, HashSet<string>>` mapping sprintId to connection IDs. Increment on `JoinSprint`, decrement on `OnDisconnectedAsync`/`LeaveSprint`. The participant count is the count of *unique participant IDs* in the sprint's connection set (one participant might have multiple connections due to multiple tabs, but each tab has the same participantId claim).

**Alternative for totalCount:** Instead of tracking connections, use `Clients.Group(...).Count()` -- but SignalR does not expose group member counts. So explicit tracking is required.

---

### Step 5: Update sprint store for inline controls

**What:** Extend the `SprintSummary` TypeScript interface with `teamId`. Add `advancePhaseCheck` action for the pre-check endpoint. Update `advancePhase` and `closeSprint` actions to work from the retro room context (using teamId from the summary). Add `state` to `SprintSummary` to track closed state.

**Files to modify:**
- `web/reflekt-web/src/stores/sprint.ts`:
  - Add `teamId: number` and `state: string` to `SprintSummary` interface
  - Add `AdvancePhaseCheckResponse` interface: `{ currentPhase: string, nonVotingParticipantCount?: number, totalParticipantCount?: number, discussedGroupCount?: number, focusedGroupId?: number | null }`
  - Add `advancePhaseCheck(teamId: number, sprintId: number): Promise<AdvancePhaseCheckResponse>` action
  - The existing `advancePhase` and `closeSprint` actions already accept `teamId` -- no change needed to their signatures

**Skill:** Follow `vue-patterns` (store actions).

---

### Step 6: Add `SprintClosed` and readiness SignalR handlers

**What:** Register handlers in `useSprintHub` for the new SignalR events: `SprintClosed`, `ReadinessChanged`, `ReadinessReset`, `ReadinessSnapshot`.

**Files to modify:**
- `web/reflekt-web/src/composables/useSprintHub.ts`:
  - Add `SprintClosed` handler: set `sprintStore.currentSummary.state = 'Closed'`
  - Add `ReadinessChanged` handler: update readiness store state (delegates to `readinessStore`)
  - Add `ReadinessReset` handler: clear readiness store state
  - Add `ReadinessSnapshot` handler: set initial readiness state from snapshot
  - Import and use the new readiness store (created in Step 7)

**Skill:** Follow `vue-patterns` (composable patterns).

**Dependency:** Step 7 (readiness store must exist).

---

### Step 7: Create readiness store and `useReadiness` composable

**What:** Create a Pinia store for readiness state and a composable for toggling readiness via the SignalR hub.

**Files to create:**
- `web/reflekt-web/src/stores/readiness.ts`:
  - State: `readyParticipantIds: number[]`, `readyCount: number`, `totalCount: number`
  - Actions: `setFromSnapshot(payload)`, `applyChange(payload)`, `reset()`
  - Getters: `isReady(participantId)` -- checks if a given participant is in the ready list

- `web/reflekt-web/src/composables/useReadiness.ts`:
  - Wraps the readiness store
  - Exposes `toggleReady(sprintId)` -- invokes `connection.invoke('ToggleReady', sprintId)` on the SignalR connection
  - Exposes `isCurrentParticipantReady` computed (checks current participant ID against readiness store)
  - Exposes `readyCount`, `totalCount`, `readyParticipants` (participant IDs mapped to display names from participant store for tooltip)

**Skill:** Follow `vue-patterns` (composable pattern), `pinia-patterns` (store pattern).

**SignalR connection access:** The composable needs access to the SignalR connection. The `useSprintHub` composable currently uses a module-level `connection` variable. The readiness composable should import and use the same module-level variable pattern, or `useSprintHub` should expose `getConnection()`. Recommend: export a `getHubConnection()` function from `useSprintHub.ts` that returns the current connection.

---

### Step 8: Restructure retro room top bar into 3-row layout

**What:** Rewrite `RetroPhaseHeader` from a simple sprint-name + votes-badge row into the consolidated 3-row top bar described in the spec. The `PhaseIndicator` and `RetroParticipantList` components, currently rendered separately in `RetroRoomView`, move inside this new top bar component.

**New layout:**
- Row 1: Sprint name (left) | Readiness badge + Votes remaining badge + advance/close button (right, SM-only)
- Row 2: Phase stepper (centered, display-only) -- embed the existing `PhaseIndicator` component
- Row 3: Participant list (name badges) -- embed the existing `RetroParticipantList` component

**Files to modify:**
- `web/reflekt-web/src/components/retro/RetroPhaseHeader.vue` -- complete rewrite. New props: `sprintName`, `currentPhase`, `isScrumMaster`, `nextPhaseName` (computed label for "Next: {phase}"), `isReportingPhase`, `isClosed`, `participants`, `readyCount`, `totalCount`, `readyParticipants` (for tooltip), `isCurrentParticipantReady`, `showReadiness` (true when Writing/Voting AND participantCount > 1), `remainingBudget`, `votesPerParticipant`, `showVotesBadge` (true only during Voting). Emits: `advance-phase`, `close-sprint`, `toggle-ready`.

**Skill:** Follow `vue-patterns` (component architecture, props/emits).

**Votes remaining badge behavior change:** Currently visible in all phases. After this step, only visible during Voting phase (spec requirement).

**Phase stepper:** The existing `PhaseIndicator` component is already display-only and non-clickable. It moves from `RetroRoomView` into `RetroPhaseHeader` unchanged.

**Participant list:** The existing `RetroParticipantList` component moves from `RetroRoomView` into `RetroPhaseHeader` unchanged.

---

### Step 9: Add confirmation dialogs for phase advance and sprint closure

**What:** Add confirmation dialog logic to `RetroRoomView` (or a sub-composable) that shows the appropriate dialog when the SM clicks advance or close. The dialogs use the existing `BaseModal` component.

**Dialog variants:**
1. **Generic advance** (Writing -> Grouping, Grouping -> Voting): "Advance to {phase}? This cannot be undone."
2. **Voting -> Discussing**: "N participants haven't voted yet. Advance to Discussing anyway?" (N from pre-check endpoint)
3. **Discussing -> Reporting gate rejection**: "At least 2 groups must be discussed before advancing to Reporting. Currently discussed: N." (shown as error, not a confirmation)
4. **Discussing -> Reporting success**: "Advance to Reporting? N groups have been discussed."
5. **Close Sprint**: "Close this sprint? No more changes will be allowed."

**Flow:**
1. SM clicks advance/close button in top bar
2. Frontend calls `advancePhaseCheck` (Step 2 endpoint) to get context
3. For Discussing: if discussedGroupCount < 2, show rejection message (no advance). Otherwise show confirmation with count.
4. For Voting: show confirmation with non-voting count.
5. For other phases: show generic confirmation.
6. On confirm: call `advancePhase` or `closeSprint`.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue` -- add dialog state refs (`showAdvanceDialog`, `showCloseDialog`, `advanceCheckResult`, `advanceError`), handler functions (`onAdvanceClick`, `onCloseClick`, `confirmAdvance`, `confirmClose`), and `BaseModal` instances for each dialog variant.

**Skill:** Follow `vue-patterns` (component events, reactive state).

**Note on sprint closure UX:** After the SM confirms closure and the API responds, the `SprintClosed` SignalR event fires. The `useSprintHub` handler (Step 6) updates `sprintStore.currentSummary.state` to `Closed`. The top bar reacts to this by hiding the close button and showing a "This retro has been closed" notification toast. The board becomes read-only (no mutation controls rendered, because `policyStore.isAllowed` returns false for all actions when sprint is closed -- this is already how the action matrix works).

**Read-only after closure:** The board already hides mutation controls based on the phase action matrix. When the sprint is closed (`state === 'Closed'`), the frontend should additionally hide any remaining interactive controls. Add a `isClosed` computed to `useRetroRoom` that checks `sprintStore.currentSummary?.state === 'Closed'`.

---

### Step 10: Add readiness badge and "I'm done" button

**What:** Create the readiness UI elements that appear during Writing and Voting phases.

**Readiness badge:** Shows "4/6 done" in the top bar (Row 1, right side). Visible to all participants. SM can hover to see a tooltip listing ready vs not-ready participants by name. Hidden when participantCount <= 1.

**"I'm done" button:** A toggle button visible to all participants during Writing and Voting phases. Clicking toggles their readiness state via the `ToggleReady` hub method (from `useReadiness` composable). Visual toggle: not-ready = outlined, ready = filled/checked.

**Files to modify:**
- `web/reflekt-web/src/components/retro/RetroPhaseHeader.vue` -- (already being rewritten in Step 8) -- add the readiness badge in Row 1. The badge uses `BaseBadge` with variant "default". SM tooltip: render a native `title` attribute or a custom tooltip `div` on hover listing participant names.

- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue` -- add the "I'm done" button at the top of the board area (above the columns). It's a simple toggle button. Position: floating at the top-right of the board, or as a standalone row between the top bar and the columns. Props: `isReady` (boolean), `showButton` (boolean -- true during Writing/Voting). Emits: `toggle-ready`.

**Skill:** Follow `vue-patterns` (component architecture).

**Note:** Steps 8 and 10 both modify `RetroPhaseHeader.vue`. The developer should batch them -- Step 8 creates the structure, Step 10 adds the readiness elements within that structure.

---

### Step 11: Remove advance/close buttons from sprint manager page

**What:** Remove the "Advance Phase" and "Close Sprint" buttons from `SprintDetailPanel`. The sprint manager page retains: sprint creation, sprint list, sprint settings, and participant management (kick).

**Files to modify:**
- `web/reflekt-web/src/components/sprints/SprintDetailPanel.vue`:
  - Remove the "Advance to {{ nextPhaseLabel }}" button (line 31-32)
  - Remove the "Close Sprint" button (line 34-35)
  - Remove the `nextPhaseLabel` computed property (lines 86-89)
  - Remove the `advance-phase` and `close` emit declarations
- `web/reflekt-web/src/views/SprintView.vue`:
  - Remove `handleAdvancePhase` function
  - Remove `handleClose` function
  - Remove `@advance-phase` and `@close` event bindings on `SprintDetailPanel`

**Skill:** Follow `vue-patterns` (component cleanup).

---

### Step 12: Wire everything in `RetroRoomView` and `useRetroRoom`

**What:** Connect all the new components, composables, and store changes in the retro room entry point.

**Files to modify:**
- `web/reflekt-web/src/composables/useRetroRoom.ts`:
  - Import and initialize `useReadiness` composable
  - Add `isClosed` computed: `sprintStore.currentSummary?.state === 'Closed'`
  - Add `teamId` computed: `sprintStore.currentSummary?.teamId`
  - Expose `isClosed`, `teamId`, readiness state from the composable return

- `web/reflekt-web/src/views/RetroRoomView.vue`:
  - Remove standalone `<PhaseIndicator>` and `<RetroParticipantList>` from the template (they now live inside `RetroPhaseHeader`)
  - Pass new props to `RetroPhaseHeader`: `currentPhase`, `isScrumMaster`, `isReportingPhase`, `isClosed`, `participants`, readiness props, votes props
  - Wire `@advance-phase` and `@close-sprint` events to dialog handlers (Step 9)
  - Wire `@toggle-ready` to readiness composable
  - Add "I'm done" button rendering with readiness composable state
  - Show toast on `SprintClosed` event: "This retro has been closed by the Scrum Master."
  - When `isClosed` becomes true: hide mutation controls, show read-only state

**Skill:** Follow `vue-patterns` (view composition, composable wiring).

**Dependency:** All previous steps.

---

## Cross-Service Changes

None. This is a single-service change within the Reflekt service.

## Migration Notes

No database migrations required. This feature adds no persistent entities and modifies no existing schemas.

## Testing Strategy

**Backend:**
- `AdvancePhaseCheckEndpoint`: returns correct `nonVotingParticipantCount` when some participants have not voted
- `AdvancePhaseCheckEndpoint`: returns correct `discussedGroupCount` when advancing from Discussing
- `AdvancePhaseCheckEndpoint`: rejects with 403 for non-SM callers
- `AdvancePhaseCheckEndpoint`: returns 409 for inactive sprints
- `BroadcastSprintClosedHandler`: fires `SprintClosed` SignalR event on sprint closure
- `SprintSummaryResponse` includes `teamId`
- `SprintHub.ToggleReady`: toggles readiness state for the calling participant
- `SprintHub.ToggleReady`: broadcasts `ReadinessChanged` to all participants in the group
- `SprintHub.JoinSprint`: sends `ReadinessSnapshot` to the joining client
- Readiness clears on phase advance (`ReadinessReset` broadcast)
- Readiness clears on participant disconnect

**Frontend:**
- SM sees "Next: {phase}" button during Writing, Grouping, Voting, Discussing; hidden for non-SM
- SM sees "Close Sprint" button during Reporting only
- Non-SM participants see no advance/close buttons
- Clicking advance shows appropriate confirmation dialog per phase
- Voting -> Discussing dialog shows non-voting participant count from pre-check
- Discussing -> Reporting gate: rejected if < 2 discussed groups with error message
- Discussing -> Reporting: confirmed with discussed count shown
- Close sprint shows confirmation dialog; after closure, board becomes read-only and toast shown
- "I'm done" button visible during Writing and Voting only; toggles on click
- Readiness badge shows "N/M done" during Writing/Voting; hidden with 1 participant
- SM can hover readiness badge to see individual participant names
- Readiness resets on phase advance
- Sprint manager page no longer shows advance/close buttons
- Votes remaining badge only visible during Voting phase (not all phases)
- PhaseIndicator is display-only and non-clickable (unchanged but verified)

## Open Questions

None -- the spec is comprehensive and all design decisions are resolved.
