# Card Grouping — Implementation

## Files Created

### Domain (Step 1)
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/CardGroup.cs` — Data properties: Id, SprintId, ColumnType, Title, CreatedAt
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Behaviors/CardGroup.cs` — Factory Create() and SetTitle() behavior methods
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/CardGroupCreatedEvent.cs` — Domain event
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/CardsGroupedEvent.cs` — Domain event with CardId and SourceGroupId
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/CardUngroupedEvent.cs` — Domain event with CardId and NewGroupId
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/CardGroupTitleChangedEvent.cs` — Domain event

### Persistence (Step 2)
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardGroupConfiguration.cs` — EF config: FK to Sprint (CASCADE), Title max 100, index on SprintId
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/CardGroupRepository.cs` — GetBySprintIdAsync, GetByIdsAsync, AddRangeAsync, Remove
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260422115641_AddCardGrouping.cs` — EF migration

### API — CardGroup Endpoints (Steps 4, 6)
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CardGroupContracts.cs` — CardGroupResponse, CardGroupCardEntry, mappings
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/GetGroups/GetGroupsEndpoint.cs` — GET, OwnerOrScrumMaster auth, includes vote counts from Voting onward
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs` — POST, ScrumMaster, validates 2+ same-column Note cards
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/BroadcastGroupCreatedHandler.cs` — SignalR handler
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/AddCardToGroup/AddCardToGroupEndpoint.cs` — POST /{groupId}/cards, ScrumMaster
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/AddCardToGroup/BroadcastCardsGroupedHandler.cs` — SignalR handler
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/RemoveCardFromGroupEndpoint.cs` — DELETE /{groupId}/cards/{cardId}, creates solo group
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/BroadcastCardUngroupedHandler.cs` — SignalR handler
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/DissolveGroupEndpoint.cs` — DELETE /{groupId}, validates 2+ cards
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs` — PATCH /{groupId}, title max 100
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/BroadcastGroupTitleChangedHandler.cs` — SignalR handler

### Frontend (Steps 9, 12)
- `web/reflekt-web/src/stores/cardGroup.ts` — Pinia store: fetchGroups, createGroup, addCardToGroup, removeCardFromGroup, dissolveGroup, updateGroupTitle
- `web/reflekt-web/src/components/CardGroupItem.vue` — Group stack component: title, card count, expand/collapse, voting, SM rename/dissolve

## Files Modified

### Domain (Step 1)
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintPhase.cs` — Added Grouping between Writing and Voting
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintAction.cs` — Added GroupCards, UngroupCards, RenameGroup
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` — AdvancePhase: Writing => Grouping, added Grouping => Voting
- `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs` — Added GroupId nullable property
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs` — Added SetGroupId(int?) method
- `src/Services/Reflekt/Reflekt.Domain/Votes/Vote.cs` — Replaced CardId with GroupId
- `src/Services/Reflekt/Reflekt.Domain/Votes/Behaviors/Vote.cs` — Create parameter: cardId -> groupId

### Persistence (Step 2)
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — Added DbSet<CardGroup> CardGroups
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs` — Added GroupId FK (SET NULL), index
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/VoteConfiguration.cs` — Changed CardId FK to GroupId FK (CASCADE)
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/PhaseActionConfiguration.cs` — Added Grouping phase seed data
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/VoteRepository.cs` — All CardId methods renamed to GroupId equivalents
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — Registered CardGroupRepository
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — Creates solo groups for seed Note cards, votes reference GroupId

### API (Steps 3, 5, 7)
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs` — Auto-creates solo groups on Writing->Grouping transition, ClearDomainEvents to suppress N broadcasts
- `src/Services/Reflekt/Reflekt.API/Features/Votes/VoteContracts.cs` — CardId => GroupId in VoteResponse and VoteDistributionEntry
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/CastVoteEndpoint.cs` — Route /card-groups/{groupId}/votes, group validation
- `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/RetractVoteEndpoint.cs` — Same route change, group-based methods
- `src/Services/Reflekt/Reflekt.API/Features/Votes/GetMyVotes/GetMyVotesEndpoint.cs` — Uses GroupId from distribution
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/BroadcastVoteCountOnVoteCastHandler.cs` — Uses groupId in SignalR payload
- `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/BroadcastVoteCountOnVoteRetractedHandler.cs` — Same change
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs` — Added GroupId to CardResponse and mapping
- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs` — Uses group-based vote counts
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs` — ReportNotesSection uses List<ReportGroupEntry>
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs` — Groups cards by GroupId, builds ReportGroupEntry

### Frontend (Steps 8, 10, 11, 12, 13)
- `web/reflekt-web/src/stores/vote.ts` — Fully rewritten: groupId-based distribution, routes, getters
- `web/reflekt-web/src/stores/card.ts` — Added groupId to Card interface
- `web/reflekt-web/src/stores/report.ts` — ReportNotesSection uses ReportGroupEntry[], copyAsMarkdown formats groups
- `web/reflekt-web/src/composables/useSprintHub.ts` — Group event handlers, vote events use groupId, PhaseChanged fetches groups
- `web/reflekt-web/src/components/PhaseIndicator.vue` — Added Grouping step with Layers icon
- `web/reflekt-web/src/views/RetroRoomView.vue` — CardGroupItem rendering, group-scoped voting, SM grouping controls
- `web/reflekt-web/src/views/ReportView.vue` — Renders grouped notes with title, vote count, nested cards

### Tests (Step 14)
- `tests/Reflekt.UnitTests/Votes/VoteTests.cs` — cardId => groupId throughout
- `tests/Reflekt.UnitTests/Sprints/SprintTests.cs` — Updated for Grouping phase in lifecycle
- `tests/Reflekt.IntegrationTests/VoteEndpointTests.cs` — Routes changed to /card-groups/{groupId}/votes, test helpers create groups via phase advancement, response assertions use groupId
- `tests/Reflekt.IntegrationTests/SprintEndpointTests.cs` — Full cycle includes Grouping phase (4 advances), last phase is Reporting
- `tests/Reflekt.IntegrationTests/CardEndpointTests.cs` — ActionItem tests advance to Discussing phase (3 advances through Grouping/Voting/Discussing)

## Files Deleted
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/BroadcastGroupDissolvedHandler.cs` — Was a no-op handler incorrectly listening to CardGroupCreatedEvent (no GroupDissolvedEvent domain event exists)

## Key Decisions
- Auto-created solo groups during Writing->Grouping transition call ClearDomainEvents() to avoid N individual SignalR broadcasts; the frontend re-fetches groups on PhaseChanged instead
- Data migration SQL skipped in EF migration — old dev DB is deleted for clean start
- VoteEndpointTests restructured around groups: SetupSprintWithNoteGroupAsync creates a card, advances to Voting (through Grouping), resolves the auto-created groupId
- Removed CastVote_OnActionItem_Returns400 test — action items have no groups, so the equivalent check is CastVote_NonexistentGroup_Returns404

## Deviations from Plan
- Plan Step 6 specified BroadcastGroupDissolvedHandler.cs — created initially but deleted because there is no GroupDissolvedEvent domain event. DissolveGroupEndpoint emits GroupDissolved directly via IHubContext instead of going through a domain event handler.
- Plan specified GroupDissolved SignalR event with `{ groupId, newGroups: [{ id, cardIds }] }` — simplified to `{ groupId }` since the frontend handler re-fetches groups on receipt.

## Review Cycle 1 Fixes
- **HIGH: CardsGroupedEvent and CardUngroupedEvent never raised** — Added `RecordCardAdded(int cardId, int sourceGroupId)` and `RecordCardRemoved(int cardId, int newGroupId)` behavior methods to CardGroup aggregate. Called in AddCardToGroupEndpoint (before SaveChangesAsync) and RemoveCardFromGroupEndpoint (before SaveChangesAsync). The DomainEventPublishingInterceptor now dispatches these events to the existing broadcast handlers.
- **MEDIUM: DissolveGroup emits no SignalR notification** — Injected IHubContext<SprintHub> into DissolveGroupEndpoint. Emits `GroupDissolved { groupId }` after removing the group and before SendNoContentAsync. The frontend handler at useSprintHub.ts already calls cardGroupStore.fetchGroups on receipt.
- Files modified: CardGroup Behaviors (added 2 methods), AddCardToGroupEndpoint (raise event), RemoveCardFromGroupEndpoint (raise event), DissolveGroupEndpoint (inject hub, emit event)
