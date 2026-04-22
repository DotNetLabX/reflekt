# Card Grouping

## Context

During retros, teams surface duplicate or related observations that scatter across the board, split votes, and fragment discussion. Card Grouping lets the facilitator cluster related notes into a single votable unit before voting begins, surfacing themes instead of individual comments.

This introduces a new **Grouping** phase between Writing and Voting, a new `CardGroup` aggregate root, and changes voting from card-scoped to group-scoped. Impacts: Reflekt.Domain, Reflekt.Persistence, Reflekt.API, and the Vue frontend.

## Scope

**In scope:**
- New `CardGroup` aggregate root with domain events
- `Card.GroupId` nullable FK
- `Vote` changes from `CardId` to `GroupId`
- New `Grouping` phase in `SprintPhase` enum
- Three new `SprintAction` values: `GroupCards`, `UngroupCards`, `RenameGroup`
- Phase action matrix update (seed data migration)
- Auto-creation of solo groups on Writing -> Grouping transition
- 6 new API endpoints for group CRUD
- Modified vote endpoints (card-scoped -> group-scoped)
- Modified report endpoint (nested cards within groups)
- New SignalR events for grouping operations
- Modified SignalR events for voting (cardId -> groupId)
- Frontend: new Grouping phase UI, voting changes, report changes, phase stepper
- Data migration for existing sprints

**Out of scope (per spec):**
- Participant-driven grouping
- Cross-column grouping
- Drag-and-drop reordering within groups
- AI/keyword-based auto-grouping
- Voting on individual cards within a group
- Skipping the Grouping phase
- Re-grouping after Voting starts
- Group-level evidence links or comments

## Domain Model Changes

### New: `CardGroup` aggregate root

```
Reflekt.Domain/CardGroups/CardGroup.cs        — data properties
Reflekt.Domain/CardGroups/Behaviors/CardGroup.cs — factory + behavior methods
```

Properties:
- `Id: int` (auto-increment PK, from AggregateRoot)
- `SprintId: int` (required)
- `ColumnType: ColumnType` (WentWell or DidntGoWell)
- `Title: string?` (max 100, facilitator-assigned theme label)
- `CreatedAt: DateTime` (UTC)

Factory method: `CardGroup.Create(int sprintId, ColumnType columnType, string? title = null)` — raises `CardGroupCreatedEvent`.

Behavior methods:
- `SetTitle(string? title)` — validates max 100 chars, raises `CardGroupTitleChangedEvent`

### New: Domain events

```
Reflekt.Domain/CardGroups/Events/CardGroupCreatedEvent.cs
Reflekt.Domain/CardGroups/Events/CardsGroupedEvent.cs
Reflekt.Domain/CardGroups/Events/CardUngroupedEvent.cs
Reflekt.Domain/CardGroups/Events/CardGroupTitleChangedEvent.cs
```

All follow the existing pattern: `sealed record XEvent(CardGroup Group) : IDomainEvent;`

For `CardsGroupedEvent` and `CardUngroupedEvent`, include additional context:
- `CardsGroupedEvent(CardGroup Group, int CardId, int SourceGroupId)`
- `CardUngroupedEvent(CardGroup SourceGroup, int CardId, int NewGroupId)`

### Modified: `Card`

Add to `Reflekt.Domain/Cards/Card.cs`:
- `public int? GroupId { get; private set; }` — nullable FK, null during Writing, populated from Grouping onward

Add to `Reflekt.Domain/Cards/Behaviors/Card.cs`:
- `public void SetGroupId(int? groupId)` — simple setter, no domain event (group-level events handle signaling)

### Modified: `Vote`

Change in `Reflekt.Domain/Votes/Vote.cs`:
- Remove `public int CardId { get; private set; }`
- Add `public int GroupId { get; private set; }`

Change in `Reflekt.Domain/Votes/Behaviors/Vote.cs`:
- `Vote.Create(int groupId, int participantId)` — parameter changes from `cardId` to `groupId`

### Modified: `SprintPhase`

In `Reflekt.Domain/Sprints/SprintPhase.cs`:
- Add `Grouping` between `Writing` and `Voting`: `{ Writing, Grouping, Voting, Discussing, Reporting }`

### Modified: `SprintAction`

In `Reflekt.Domain/Sprints/SprintAction.cs`:
- Add `GroupCards`, `UngroupCards`, `RenameGroup`

### Modified: `Sprint.AdvancePhase()`

In `Reflekt.Domain/Sprints/Behaviors/Sprint.cs`:
- Change: `SprintPhase.Writing => SprintPhase.Grouping`
- Add: `SprintPhase.Grouping => SprintPhase.Voting`

## Data Model Changes

### New table: `CardGroups`

| Column | Type | Constraints |
|--------|------|-------------|
| Id | INTEGER | PK, auto-increment |
| SprintId | INTEGER | NOT NULL, FK -> Sprints(Id), CASCADE DELETE |
| ColumnType | INTEGER | NOT NULL (enum stored as int) |
| Title | TEXT | nullable, max 100 |
| CreatedAt | TEXT | NOT NULL |

Index on `SprintId`.

### Modified table: `Cards`

- Add column `GroupId INTEGER` nullable, FK -> CardGroups(Id), SET NULL on delete
- Index on `GroupId`

### Modified table: `Votes`

- Remove column `CardId`
- Add column `GroupId INTEGER` NOT NULL, FK -> CardGroups(Id), CASCADE DELETE
- Index on `GroupId`
- Remove index on `CardId`

### Phase action seed data

New rows for the Grouping phase:
- `(Grouping, GroupCards)`
- `(Grouping, UngroupCards)`
- `(Grouping, RenameGroup)`
- `(Grouping, AdvancePhase)`

### Data migration

Existing sprints need retroactive group creation:
1. For every Note card in every existing sprint: create a solo CardGroup, set Card.GroupId
2. For every Vote row: look up the card's GroupId, set Vote.GroupId, drop Vote.CardId
3. This is handled in the EF migration's `Up()` method using raw SQL

## Implementation Steps

### Step 1: Domain — CardGroup aggregate and modified enums

**What:** Create the CardGroup aggregate root, its domain events, and update SprintPhase, SprintAction, and Sprint.AdvancePhase().

**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/CardGroup.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Behaviors/CardGroup.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/CardGroupCreatedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/CardsGroupedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/CardUngroupedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/CardGroupTitleChangedEvent.cs`

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintPhase.cs` — add `Grouping` between `Writing` and `Voting`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/SprintAction.cs` — add `GroupCards`, `UngroupCards`, `RenameGroup`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` — update `AdvancePhase()` switch: `Writing => Grouping`, add `Grouping => Voting`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs` — add `GroupId` property
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs` — add `SetGroupId(int? groupId)` method
- `src/Services/Reflekt/Reflekt.Domain/Votes/Vote.cs` — replace `CardId` with `GroupId`
- `src/Services/Reflekt/Reflekt.Domain/Votes/Behaviors/Vote.cs` — change `Create` parameter from `cardId` to `groupId`

**Pattern:** Follow `Team` aggregate structure (data in `CardGroup.cs`, behaviors in `Behaviors/CardGroup.cs`). Domain events follow `TeamCreatedEvent` pattern.

**Dependencies:** None.

### Step 2: Persistence — EF configuration, migration, repository

**What:** Create the CardGroup EF configuration, update Card and Vote configurations, add migration with data migration SQL, create CardGroupRepository, register in DI.

**Files to create:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardGroupConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/CardGroupRepository.cs`

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `DbSet<CardGroup> CardGroups`
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs` — add `GroupId` FK to `CardGroup`, SET NULL on delete, index
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/VoteConfiguration.cs` — replace `CardId` FK with `GroupId` FK to `CardGroup`, CASCADE delete, update index
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/PhaseActionConfiguration.cs` — add seed data for Grouping phase (GroupCards, UngroupCards, RenameGroup, AdvancePhase)
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/VoteRepository.cs` — change all `CardId` references to `GroupId` (CountByGroupAsync, GetOneByParticipantAndGroupAsync, GetVoteDistributionAsync, GetVoteCountsByGroupIdsAsync)
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `CardGroupRepository`
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — update seed data to create solo groups for seed cards, update seed votes to use GroupId

**Migration:** Run `dotnet ef migrations add AddCardGrouping -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API` from `src/`. The migration's `Up()` must include SQL for data migration:
1. Create CardGroups table
2. Add GroupId column to Cards
3. Insert a solo CardGroup for every existing Note card (matching SprintId, ColumnType)
4. Update each Note card's GroupId to point to its solo group
5. Add GroupId column to Votes
6. For each Vote, set GroupId from the card's GroupId (via JOIN)
7. Drop CardId column from Votes (SQLite requires table rebuild — EF handles this)

**Pattern:** Follow `CardConfiguration` / `VoteConfiguration` for EF config. Follow `CardRepository` for repository. Follow `PhaseActionConfiguration.HasData()` for seed rows.

**CardGroupRepository methods needed:**
- `GetByIdAsync(int id, ct)` — inherited
- `GetBySprintIdAsync(int sprintId, ct)` — all groups for a sprint, with their cards loaded via a join/query
- `GetByIdsAsync(IEnumerable<int> ids, ct)` — batch lookup
- `Remove(CardGroup group)` — delete a group
- `AddRangeAsync(IEnumerable<CardGroup> groups, ct)` — bulk insert for auto-creation

**Dependencies:** Step 1.

### Step 3: API — AdvancePhase modification (auto-create solo groups)

**What:** Modify the AdvancePhase endpoint to auto-create solo groups when transitioning from Writing to Grouping.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs` — after `sprint.AdvancePhase()`, check if the new phase is `Grouping`. If so: load all Note cards for the sprint, create a solo `CardGroup` for each, set each card's `GroupId`, bulk save. Use `CardGroupRepository.AddRangeAsync()` and `CardRepository` to update cards. Save via a single `SaveChangesAsync` call on the sprint repository (all changes tracked in the same DbContext).

**Pattern:** Follow existing AdvancePhase pattern. The bulk creation is a handler-level operation (not domain — per spec, this is a system operation, not a user action). The domain events from CardGroup.Create will fire via the interceptor.

**Note on SignalR:** The auto-created solo groups should NOT broadcast N individual GroupCreated events. The frontend will re-fetch groups after receiving the PhaseChanged event (same pattern as the existing Writing -> Voting transition, which triggers `cardStore.fetchCards`). The domain event handlers for `CardGroupCreatedEvent` should check if this is a bulk auto-creation (e.g., by not broadcasting during the Grouping transition) OR the handler simply broadcasts and the frontend deduplicates. Recommendation: the handler broadcasts, but the frontend uses the PhaseChanged handler to re-fetch groups in bulk rather than relying on individual events. This keeps the handler simple.

**Dependencies:** Steps 1, 2.

### Step 4: API — CardGroup endpoints (6 new endpoints)

**What:** Create the 6 card group endpoints following FastEndpoints patterns.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CardGroupContracts.cs` — request/response records and mapping
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/AddCardToGroup/AddCardToGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/RemoveCardFromGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/DissolveGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/GetGroups/GetGroupsEndpoint.cs`

**Endpoint details:**

1. **CreateGroupEndpoint** — `POST /api/sprints/{sprintId}/card-groups`
   - Auth: ScrumMaster policy
   - Phase gate: `SprintAction.GroupCards`
   - Validates: 2+ cardIds, same column, same sprint, Note cards only
   - Creates new CardGroup, moves cards to it, deletes empty source groups
   - Returns 201 with created group

2. **AddCardToGroupEndpoint** — `POST /api/sprints/{sprintId}/card-groups/{groupId}/cards`
   - Auth: ScrumMaster policy
   - Phase gate: `SprintAction.GroupCards`
   - Validates: card same column as group, same sprint, Note card
   - Moves card to group, deletes empty source group
   - Returns 200 with updated group

3. **RemoveCardFromGroupEndpoint** — `DELETE /api/sprints/{sprintId}/card-groups/{groupId}/cards/{cardId}`
   - Auth: ScrumMaster policy
   - Phase gate: `SprintAction.UngroupCards`
   - Creates new solo group for the removed card
   - Returns 200

4. **DissolveGroupEndpoint** — `DELETE /api/sprints/{sprintId}/card-groups/{groupId}`
   - Auth: ScrumMaster policy
   - Phase gate: `SprintAction.UngroupCards`
   - Validates: group has 2+ cards (dissolving a solo group returns 400)
   - Creates solo groups for each card, deletes original group
   - Returns 204

5. **UpdateGroupTitleEndpoint** — `PATCH /api/sprints/{sprintId}/card-groups/{groupId}`
   - Auth: ScrumMaster policy
   - Phase gate: `SprintAction.RenameGroup`
   - Validates: title max 100 chars
   - Returns 200 with updated group

6. **GetGroupsEndpoint** — `GET /api/sprints/{sprintId}/card-groups`
   - Auth: OwnerOrScrumMaster policy (same as ListCards — guest token or SM cookie)
   - No phase gate (read-only)
   - Returns all groups with their cards, and (from Voting phase onward) vote counts

**Pattern:** Follow `CastVoteEndpoint` for auth + phase gating pattern. Follow `CreateCardEndpoint` for 201 returns. Follow `ListCardsEndpoint` for dual-auth (guest token or SM cookie).

**CardGroupContracts response shape:**
```
CardGroupResponse(int Id, int SprintId, string ColumnType, string? Title, List<CardGroupCardEntry> Cards, int VoteCount)
CardGroupCardEntry(int Id, string Text, DateTime CreatedAt, List<EvidenceLinkResponse> EvidenceLinks)
```

**Dependencies:** Steps 1, 2.

### Step 5: API — Modified vote endpoints (card-scoped -> group-scoped)

**What:** Change vote cast, retract, and GetMyVotes endpoints from card-scoped to group-scoped.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/CastVoteEndpoint.cs` — route changes to `/api/sprints/{sprintId}/card-groups/{groupId}/votes`, request uses `GroupId`, validate group exists and belongs to sprint, create Vote with `groupId`
- `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/RetractVoteEndpoint.cs` — same route change, use `GroupId` throughout
- `src/Services/Reflekt/Reflekt.API/Features/Votes/VoteContracts.cs` — `VoteResponse(int GroupId, ...)`, `VoteDistributionEntry(int GroupId, int Count)`
- `src/Services/Reflekt/Reflekt.API/Features/Votes\GetMyVotes/GetMyVotesEndpoint.cs` — use `GroupId` in distribution query

**Files to modify (event handlers):**
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/BroadcastVoteCountOnVoteCastHandler.cs` — look up group instead of card, send `{ groupId, newCount }` via SignalR
- `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/BroadcastVoteCountOnVoteRetractedHandler.cs` — same change

**Note:** The vote handlers need to resolve the sprintId from the group (via CardGroupRepository) to target the correct SignalR group.

**Pattern:** Follow existing CastVoteEndpoint structure but swap CardId for GroupId throughout.

**Dependencies:** Steps 1, 2.

### Step 6: API — SignalR event handlers for grouping

**What:** Create IEventHandler implementations that broadcast grouping events via SignalR.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/BroadcastGroupCreatedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/AddCardToGroup/BroadcastCardsGroupedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/BroadcastCardUngroupedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/BroadcastGroupDissolvedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/BroadcastGroupTitleChangedHandler.cs`

**SignalR event payloads (matching spec):**
- `GroupCreated` — `{ group: { id, sprintId, columnType, title, cardIds } }`
- `CardsGrouped` — `{ groupId, cardId, sourceGroupId }`
- `CardUngrouped` — `{ cardId, newGroupId, sourceGroupId }`
- `GroupDissolved` — `{ groupId, newGroups: [{ id, cardIds }] }`
- `GroupTitleChanged` — `{ groupId, title }`

**Pattern:** Follow `BroadcastVoteCountOnVoteCastHandler` — inject `IHubContext<SprintHub>` and the needed repositories.

**Dependencies:** Steps 1, 2.

### Step 7: API — Modified report endpoint and card list endpoint

**What:** Update the report response to nest cards within groups. Update ListCards to include GroupId.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs`:
  - Change `ReportNotesSection` to hold `List<ReportGroupEntry>` instead of `List<ReportCardEntry>`
  - Add `ReportGroupEntry(string GroupTitle, int VoteCount, List<ReportCardEntry> Cards)`
  - Keep `ReportCardEntry` as is (text + evidence links)
  - Update `ReportMappings` accordingly

- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs`:
  - Load card groups with their cards
  - Load vote counts by group (not by card)
  - Build grouped response: filter zero-vote groups, sort by vote count desc
  - For `GroupTitle`: use `CardGroup.Title ?? earliest card text (by CreatedAt)`

- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs` — add `int? GroupId` to `CardResponse`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs` — update `CardMappings.ToResponse` to include GroupId
- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs` — no logic change needed (GroupId flows through CardResponse automatically)

**Dependencies:** Steps 1, 2.

### Step 8: Frontend — Vote store changes (cardId -> groupId)

**What:** Change the vote store from card-scoped to group-scoped.

**Files to modify:**
- `web/reflekt-web/src/stores/vote.ts`:
  - Change `VoteDistribution.cardId` to `groupId`
  - Change `voteCounts` map key semantics from cardId to groupId
  - Change `myVotesOnCard` getter to `myVotesOnGroup(groupId)`
  - Change `voteCountForCard` getter to `voteCountForGroup(groupId)`
  - Update `castVote(sprintId, groupId)` — route changes to `/api/sprints/${sprintId}/card-groups/${groupId}/votes`
  - Update `retractVote(sprintId, groupId)` — same route change
  - Update `fetchMyVotes` — response now has `groupId`
  - Update `initVoteCounts` to accept group-based counts
  - Update `setVoteCount` to use groupId

**Dependencies:** Step 5.

### Step 9: Frontend — Card group store (new)

**What:** Create a new Pinia store for card groups.

**Files to create:**
- `web/reflekt-web/src/stores/cardGroup.ts`

**Store shape:**
```typescript
interface CardGroupCard { id, text, createdAt, evidenceLinks }
interface CardGroup { id, sprintId, columnType, title, cards: CardGroupCard[], voteCount }

state: { groups: CardGroup[] }
getters:
  wentWellGroups — groups where columnType === 'WentWell'
  didntGoWellGroups — groups where columnType === 'DidntGoWell'
actions:
  fetchGroups(sprintId) — GET /api/sprints/{sprintId}/card-groups
  createGroup(sprintId, cardIds, title?) — POST
  addCardToGroup(sprintId, groupId, cardId) — POST
  removeCardFromGroup(sprintId, groupId, cardId) — DELETE
  dissolveGroup(sprintId, groupId) — DELETE
  updateGroupTitle(sprintId, groupId, title) — PATCH
```

**Dependencies:** None (can be built in parallel with API steps).

### Step 10: Frontend — SignalR hub updates

**What:** Update the SignalR composable to handle new grouping events and modified vote events.

**Files to modify:**
- `web/reflekt-web/src/composables/useSprintHub.ts`:
  - Import and use the new `useCardGroupStore`
  - Add handlers for: `GroupCreated`, `CardsGrouped`, `CardUngrouped`, `GroupDissolved`, `GroupTitleChanged`
  - Modify `VoteCast` and `VoteRetracted` handlers: change from `{ cardId, newCount }` to `{ groupId, newCount }`
  - On `PhaseChanged` to `Grouping`: fetch groups (like how cards are re-fetched when leaving Writing)

**Dependencies:** Steps 8, 9.

### Step 11: Frontend — Phase indicator update

**What:** Add Grouping step to the phase stepper.

**Files to modify:**
- `web/reflekt-web/src/components/PhaseIndicator.vue`:
  - Add `{ name: 'Grouping', short: 'Group', icon: Layers }` between Writing and Voting
  - Import `Layers` from `lucide-vue-next`

**Dependencies:** None.

### Step 12: Frontend — RetroRoomView grouping phase UI

**What:** Update the main retro room view to show grouping UI during Grouping phase and group-scoped voting during Voting phase.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue`:
  - Import `useCardGroupStore`
  - On enter room: also fetch groups if phase >= Grouping
  - During Grouping phase: render groups instead of individual cards in WentWell/DidntGoWell columns. Show group stacks with card count badge. SM sees grouping controls (merge, ungroup, title editing). Participants see groups read-only.
  - During Voting phase: render groups as collapsed stacks with vote buttons on the group (not individual cards). Use `voteStore.castVote(sprintId, groupId)` instead of card-based voting.
  - During Discussing phase: render groups sorted by vote count.
  - Update `castVote` and `retractVote` methods to pass `groupId` instead of `cardId`.

**Files to create:**
- `web/reflekt-web/src/components/CardGroupItem.vue` — new component for rendering a group stack:
  - Shows group title (or first card text if no title)
  - Shows card count badge
  - Expandable to show individual cards
  - SM-only: title editing, ungroup button, dissolve button
  - Voting phase: vote button on the group, vote count display

**Dependencies:** Steps 8, 9, 10, 11.

### Step 13: Frontend — Report view and store update

**What:** Update the report store and view to display grouped notes.

**Files to modify:**
- `web/reflekt-web/src/stores/report.ts`:
  - Change `ReportCardEntry` to `ReportGroupEntry { groupTitle, voteCount, cards: ReportCardEntry[] }`
  - Change `ReportNotesSection` to use `ReportGroupEntry[]`
  - Update `copyAsMarkdown` to format groups: group title as heading, cards as sub-bullets

- `web/reflekt-web/src/views/ReportView.vue`:
  - Render each group as a section: title (or first card text) + vote count + nested cards
  - Solo groups with no title render identically to how individual cards rendered before

**Dependencies:** Step 7.

### Step 14: Seed data and integration verification

**What:** Update seed data, delete the old SQLite database, verify the full stack works end-to-end.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — update seed to create solo groups for seed cards, update vote creation to use GroupId. Since the seed sprint is in Active/Writing state, solo groups won't be auto-created yet, but the Vote seeding needs group references. Approach: after creating cards, create solo groups for Note cards, set GroupId on cards, then create Votes referencing groups.

**Verification:**
- Delete `src/Services/Reflekt/Reflekt.API/data/reflekt.db*` files
- Run `dotnet build` from solution root
- Start the API, verify migration runs, seed data creates
- Walk through all phases: Writing -> Grouping -> Voting -> Discussing -> Reporting
- Verify group creation, adding cards, removing cards, dissolving, title editing
- Verify voting on groups
- Verify report shows grouped data

**Dependencies:** All previous steps.

## Cross-Service Changes

Not applicable — Reflekt is a single service with no gRPC or integration events in v1.

## Migration Notes

```bash
# From src/ directory:
dotnet ef migrations add AddCardGrouping -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
```

The migration must handle:
1. Creating the CardGroups table
2. Adding Card.GroupId FK
3. Replacing Vote.CardId with Vote.GroupId
4. Data migration: retroactively creating solo groups for existing Note cards and remapping Vote.CardId -> Vote.GroupId
5. Updating PhaseAction seed data for the new Grouping phase

Since SQLite doesn't support `ALTER TABLE DROP COLUMN` natively, EF Core will generate a table rebuild for the Votes table (create temp, copy data, drop original, rename). The data migration SQL must run between the column additions and the final table structure.

The old SQLite database file should be deleted for clean development (auto-migrate on startup will recreate).

## Testing Strategy

**Key scenarios to test:**
1. Phase transition Writing -> Grouping auto-creates solo groups for all Note cards
2. Creating a group from 2+ same-column cards succeeds; cross-column returns 400
3. Adding a card to an existing group works; source solo group is cleaned up
4. Removing a card creates a new solo group
5. Dissolving a multi-card group creates solo groups; dissolving a solo group returns 400
6. Setting/clearing a group title
7. Voting on a group (not a card) — budget enforcement still works
8. Retracting a vote on a group
9. Report correctly nests cards in groups, excludes zero-vote groups, sorts by vote count
10. Phase gating: grouping actions blocked outside Grouping phase (409)
11. Auth: non-SM cannot create/modify/dissolve groups (403)
12. Data migration: existing cards and votes are correctly migrated

## Open Questions

None — the spec is comprehensive and all architectural decisions align with existing patterns.
