# Voting Plan — Reflekt v1

**Status:** Ready for implementation
**Prerequisite:** Auth (landed), Teams (landed), Sprints (landed), Participants (landed), Cards (landed)
**Prerequisite for:** Reports (filters notes by vote count)
**Companion docs:** `docs/features/Voting.md`, `docs/architecture/v1.md`

## 0. Purpose

Introduce the Vote aggregate — dot voting on note cards. Participants spend a limited vote budget (default 3, set on Sprint at creation) across Note cards to signal discussion priority. Vote counts drive card ordering in Voting/Discussing phases and determine which notes appear in the sprint report.

**Non-goals for Voting:**
- No card ordering logic — that's a frontend display concern (sort by vote count in Voting/Discussing phases). The API returns cards in creation order; the frontend re-sorts.
- No evidence links on cards — that's the EvidenceLinks feature.
- No report generation — that's the Reports feature.
- No weighted votes, ranked-choice, or "super votes."
- No vote visibility toggle (always visible in real-time).

If you find yourself writing an `EvidenceLink` entity, a report endpoint, or card sort-order logic in the API, **stop** — that belongs in a different plan.

## 1. Relevant skills

Read each skill file before applying it — this plan only names them.

- **`create-aggregate`** — for the `Vote` aggregate root. Follow its partial-class split.
- **`create-feature`** — for each endpoint (CastVote, RetractVote, GetMyVotes).
- **`persistence-patterns`** — for `VoteConfiguration`, `VoteRepository`, `DbSet` registration.
- **`domain-patterns`** — for domain event definitions.

**Pattern references (actual codebase):**
- Aggregate split: `Reflekt.Domain/Cards/Card.cs` (properties) + `Reflekt.Domain/Cards/Behaviors/Card.cs` (factory + methods)
- Domain event: `Reflekt.Domain/Cards/Events/CardCreatedEvent.cs`
- Endpoint (mutation, guest auth): `Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs`
- Endpoint (read, guest auth): `Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs`
- Repository: `Reflekt.Persistence/Repositories/CardRepository.cs`
- Entity config: `Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs`
- Event handler (SignalR broadcaster): `Reflekt.API/Events/CardCreatedEventHandler.cs`
- Hub: `Reflekt.API/Hubs/SprintHub.cs`
- Seed data: `Reflekt.Persistence/DbInitializer.cs`
- Unit test: `tests/Reflekt.UnitTests/Cards/CardTests.cs`
- Integration test: `tests/Reflekt.IntegrationTests/CardEndpointTests.cs`
- Frontend store: `web/reflekt-web/src/stores/card.ts`
- Frontend composable: `web/reflekt-web/src/composables/useSprintHub.ts`
- Frontend view: `web/reflekt-web/src/views/RetroRoomView.vue`

## 2. Judgment calls flagged for the Developer

Small decisions the Developer can make without coming back:

- **Vote button style.** Dot/circle icon vs thumbs-up vs arrow — Developer picks. Recommend a simple dot or upvote arrow.
- **Vote count display.** Badge on the button vs separate counter next to the card — Developer picks.
- **Retract UX.** Separate "remove vote" button vs clicking the vote button again to decrement — Developer picks. Recommend: vote button shows count + click to add, separate minus/x to retract (clearer than toggle when multiple votes per card are allowed).
- **Budget remaining display.** Where to show remaining votes — Developer picks (recommend: near the top of the board, e.g., "2/3 votes remaining").
- **GetMyVotes timing.** Whether to call `/votes/mine` on room entry or derive from local state after card fetch — Developer picks. Recommend: call on room entry alongside fetchCards for a clean initial state.

Decisions that should come back if they feel wrong mid-implementation:

- If the `MarkRetracted()` → `Remove()` → `SaveChangesAsync()` sequence doesn't work with the interceptor for deleted entities — flag it (same concern as Cards plan §2).
- If the budget count query through the Card join is slow or awkward — flag it. (It shouldn't be at v1 scale.)

## 3. Domain model changes

### 3.1 Vote aggregate — partial class split

**Properties file:** `src/Services/Reflekt/Reflekt.Domain/Votes/Vote.cs`

- Extends `AggregateRoot` (non-generic — int ID, consistent with all existing aggregates).
- Properties:
  - `int CardId` — FK to Card (by ID, no navigation).
  - `int ParticipantId` — FK to Participant (by ID, no navigation). The voter.
  - `DateTime CreatedAt` — UTC, stamped at creation.
- Private parameterless constructor for EF Core.

**No SprintId on Vote.** A Participant is scoped to exactly one Sprint, so counting all votes by a ParticipantId implicitly counts votes in that sprint. No join needed for budget queries.

**Behaviors file:** `src/Services/Reflekt/Reflekt.Domain/Votes/Behaviors/Vote.cs`

Two methods:

**`Create(int cardId, int participantId)`** — static factory:
- Sets `CardId`, `ParticipantId`, `CreatedAt = DateTime.UtcNow`.
- Raises `VoteCastEvent(this)`.
- No guards here — budget check, action-item rejection, and closed-sprint check all happen at the handler level (architecture doc §5.2: "VotesPerParticipant checked at the API/handler level, not the aggregate level").

**`MarkRetracted()`** — instance method:
- No guards, no property changes.
- Raises `VoteRetractedEvent(this)`.
- Called by the endpoint before EF removal so the interceptor can publish the event.

Skill: `create-aggregate`

### 3.2 Domain events

Two events in `src/Services/Reflekt/Reflekt.Domain/Votes/Events/`:

```
VoteCastEvent(Vote Vote) : IDomainEvent
VoteRetractedEvent(Vote Vote) : IDomainEvent
```

Follow CLAUDE.md convention: carry the aggregate reference. At dispatch time (post-`SaveChangesAsync`), `Vote.Id` and `Vote.CardId` are populated. For `VoteRetractedEvent`, the entity is in memory with state `Deleted` — the handler reads `Vote.CardId` before the object is garbage collected.

Skill: `domain-patterns`

## 4. Data model changes

### 4.1 Table: `Votes`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `INTEGER` | PK, auto-increment (from `Entity` base) |
| `CardId` | `INTEGER` | Required, FK to `Cards.Id` |
| `ParticipantId` | `INTEGER` | Required, FK to `Participants.Id` |
| `CreatedAt` | `TEXT` (SQLite datetime) | Required |

FK: `CardId → Cards.Id`, **cascade delete** (when a card is deleted, its votes are deleted).
FK: `ParticipantId → Participants.Id`, **restrict** (same rationale as Cards — prevents multiple cascade paths through Sprint→Participant→Vote; participants are never independently deleted).
Index on `CardId` (used by vote count queries — primary access pattern).
Index on `ParticipantId` (used by budget count and "my votes" queries).

**No unique constraint on (CardId, ParticipantId)** — spec explicitly allows multiple votes on the same card.

### 4.2 EF Configuration — `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/VoteConfiguration.cs`

`EntityConfiguration<Vote>` (extends base from BuildingBlocks):
- `ToTable("Votes")`
- `Property(v => v.CardId).IsRequired()`
- `HasOne<Card>().WithMany().HasForeignKey(v => v.CardId).OnDelete(DeleteBehavior.Cascade)`
- `Property(v => v.ParticipantId).IsRequired()`
- `HasOne<Participant>().WithMany().HasForeignKey(v => v.ParticipantId).OnDelete(DeleteBehavior.Restrict)`
- `HasIndex(v => v.CardId)`
- `HasIndex(v => v.ParticipantId)`
- `Property(v => v.CreatedAt).IsRequired()`

Skill: `persistence-patterns`

## 5. Implementation steps

### Step 1: Vote aggregate + domain events

**What:** Create the entire Vote domain model.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Votes/Vote.cs`
- `src/Services/Reflekt/Reflekt.Domain/Votes/Behaviors/Vote.cs`
- `src/Services/Reflekt/Reflekt.Domain/Votes/Events/VoteCastEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Votes/Events/VoteRetractedEvent.cs`
**Pattern:** Follow `create-aggregate` skill. Reference `Reflekt.Domain/Cards/Card.cs` for the partial-class split. Reference `Reflekt.Domain/Cards/Events/CardCreatedEvent.cs` for event shape.
**Feature-specific inputs:**
- Factory method name: `Create`
- Parameters: `int cardId, int participantId`
- No guards in the factory — validation is at the handler level
- Timestamp: `CreatedAt = DateTime.UtcNow`
- Events raised: `VoteCastEvent(this)` on create, `VoteRetractedEvent(this)` on mark-retracted
**Dependencies:** None.

### Step 2: EF config + DbSet + repository + DI

**What:** Add persistence for the Vote aggregate.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/VoteConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/VoteRepository.cs`
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `public DbSet<Vote> Votes => Set<Vote>();`
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `services.AddScoped<VoteRepository>();`

**VoteRepository** extends `Repository<Vote>`:
- Inherits `AddAsync`, `SaveChangesAsync`, `GetByIdAsync` from base.
- Custom methods:
  - `CountByParticipantAsync(int participantId, CancellationToken ct)` — returns `int`. Counts all votes by this participant (implicitly = votes in their sprint, since Participant is sprint-scoped). Used for budget check.
  - `CountByCardAsync(int cardId, CancellationToken ct)` — returns `int`. Total vote count on a card. Used for SignalR payload `newCount`.
  - `CountByParticipantAndCardAsync(int participantId, int cardId, CancellationToken ct)` — returns `int`. How many of this participant's votes are on this card. Used for `myVoteCount` in response.
  - `GetOneByParticipantAndCardAsync(int participantId, int cardId, CancellationToken ct)` — returns `Vote?`. Finds one vote to retract (any one — order doesn't matter). Used by RetractVote endpoint.
  - `GetVoteDistributionAsync(int participantId, CancellationToken ct)` — returns list of `{ CardId, Count }` pairs. Groups votes by card. Used by GetMyVotes endpoint.
  - `Remove(Vote vote)` — wraps `Db.Votes.Remove(vote)`. Used by RetractVote endpoint.

**Pattern:** Follow `persistence-patterns` skill. Reference `CardRepository.cs` and `CardConfiguration.cs`.
**Details:** See §4.2 for FK cascade/restrict decisions.
**Dependencies:** Step 1.

### Step 3: EF Migration

**What:** Generate the Vote migration.
**Command:**
```bash
dotnet ef migrations add AddVote -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```
**Verify:** The generated migration should only create the `Votes` table with the columns, FKs, and indexes from §4.1. No changes to existing tables.
**Verify:** `dotnet ef migrations list` shows the new migration. `dotnet run` auto-applies in Development.
**Dependencies:** Step 2.

### Step 4: CastVote endpoint

**What:** Create the CastVote endpoint with budget and card-type validation.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Votes/VoteContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/CastVoteEndpoint.cs`

**VoteContracts.cs** — shared records:
- `VoteResponse(int CardId, int VoteCount, int MyVoteCount)` — returned by both CastVote and RetractVote.
- `VoteDistributionEntry(int CardId, int Count)` — used by GetMyVotes.

**CastVoteEndpoint.cs:**
- Auth: Guest token required (same policy as CreateCard).
- `[HttpPost("/api/sprints/{sprintId}/cards/{cardId}/votes")]`, `[Tags("Votes")]`
- Request record: `CastVoteRequest { int SprintId, int CardId }` — both from route. No body needed. Can be defined inline.
- No validator file needed (no body fields to validate).
- Injects: `SprintRepository`, `CardRepository`, `VoteRepository`

**Handler flow:**
1. Read `participantId` and `sprintId` from claims (set by GuestTokenAuthenticationHandler).
2. Verify claim's `sprintId` matches route's `SprintId` → 401 if mismatch.
3. Load card: `cardRepository.GetByIdAsync(request.CardId, ct)` → 404 if null.
4. Verify card belongs to this sprint: `card.SprintId == request.SprintId` → 404 if mismatch.
5. Check `card.CardType == CardType.ActionItem` → 400 (message: "Cannot vote on action items").
6. Load sprint: `sprintRepository.GetByIdAsync(request.SprintId, ct)` → 404 if null.
7. Check `sprint.State == SprintState.Closed` → 409 (message: "Sprint is closed").
8. **Budget check:** `var usedVotes = await voteRepository.CountByParticipantAsync(participantId, ct)`. If `usedVotes >= sprint.VotesPerParticipant` → 409 (message: "Vote budget exhausted").
9. Create aggregate: `Vote.Create(request.CardId, participantId)`.
10. Persist: `voteRepository.AddAsync` + `voteRepository.SaveChangesAsync`.
11. Query counts for response: `voteCount = await voteRepository.CountByCardAsync(request.CardId, ct)`, `myVoteCount = await voteRepository.CountByParticipantAndCardAsync(participantId, request.CardId, ct)`.
12. Respond: 201 with `VoteResponse(request.CardId, voteCount, myVoteCount)`.

**Pattern:** Follow `create-feature` skill. Reference `CreateCardEndpoint.cs` for handler structure with guest token claims.
**Dependencies:** Steps 1–3.

### Step 5: RetractVote endpoint

**What:** Create the RetractVote endpoint.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/RetractVoteEndpoint.cs`

**RetractVoteEndpoint.cs:**
- Auth: Guest token required.
- `[HttpDelete("/api/sprints/{sprintId}/cards/{cardId}/votes")]`, `[Tags("Votes")]`
- Request record: `RetractVoteRequest { int SprintId, int CardId }` — both from route. Can be defined inline.
- Injects: `SprintRepository`, `VoteRepository`

**Handler flow:**
1. Read `participantId` and `sprintId` from claims.
2. Verify claim's `sprintId` matches route's `SprintId` → 401 if mismatch.
3. Load sprint: `sprintRepository.GetByIdAsync(request.SprintId, ct)` → 404 if null.
4. Check `sprint.State == SprintState.Closed` → 409 (message: "Sprint is closed").
5. Find a vote to retract: `var vote = await voteRepository.GetOneByParticipantAndCardAsync(participantId, request.CardId, ct)` → 409 if null (message: "No votes to retract on this card").
6. `vote.MarkRetracted()` — raises `VoteRetractedEvent(this)`.
7. `voteRepository.Remove(vote)` — marks entity for EF deletion.
8. `voteRepository.SaveChangesAsync(ct)` — interceptor publishes VoteRetractedEvent, then EF executes DELETE.
9. Query counts for response: `voteCount = await voteRepository.CountByCardAsync(request.CardId, ct)`, `myVoteCount = await voteRepository.CountByParticipantAndCardAsync(participantId, request.CardId, ct)`.
10. Respond: 200 with `VoteResponse(request.CardId, voteCount, myVoteCount)`.

**Pattern:** Follow `create-feature` skill. Reference `DeleteCardEndpoint.cs` for the MarkDeleted → Remove → Save pattern.
**Dependencies:** Steps 1–3.

### Step 6: GetMyVotes endpoint

**What:** Create the read endpoint for a participant's vote distribution.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Votes/GetMyVotes/GetMyVotesEndpoint.cs`

**GetMyVotesEndpoint.cs:**
- Auth: Guest token required.
- `[HttpGet("/api/sprints/{sprintId}/votes/mine")]`, `[Tags("Votes")]`
- Request record: `GetMyVotesRequest { int SprintId }` — from route. Can be defined inline.
- Injects: `SprintRepository`, `VoteRepository`

**Handler flow:**
1. Read `participantId` and `sprintId` from claims.
2. Verify claim's `sprintId` matches route's `SprintId` → 401 if mismatch.
3. Load sprint: `sprintRepository.GetByIdAsync(request.SprintId, ct)` → 404 if null.
4. Query distribution: `var distribution = await voteRepository.GetVoteDistributionAsync(participantId, ct)`.
5. Respond: 200 with `List<VoteDistributionEntry>`.

**Pattern:** Follow `create-feature` skill. Reference `ListCardsEndpoint.cs` for read pattern.
**Dependencies:** Steps 1–3.

### Step 7: SignalR event handlers

**What:** Create two SignalR broadcasters for vote domain events.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Events/VoteCastEventHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Events/VoteRetractedEventHandler.cs`

Each handler:
- Implements `IEventHandler<VoteXxxEvent>`.
- Injects `IHubContext<SprintHub>` and `VoteRepository`.

**VoteCastEventHandler — HandleAsync:**
1. Read `cardId = ev.Vote.CardId`.
2. Determine `sprintId` — load the card via `CardRepository` to get `card.SprintId`. (The Vote doesn't have SprintId directly.)
3. Query `newCount = await voteRepository.CountByCardAsync(cardId, ct)`.
4. Send to SignalR group `sprint:{sprintId}`: event name `"VoteCast"`, payload `{ cardId, newCount }`.

**VoteRetractedEventHandler — HandleAsync:**
1. Same as above but event name `"VoteRetracted"`.
2. Read `cardId = ev.Vote.CardId`.
3. Determine `sprintId` — load card to get SprintId.
4. Query `newCount = await voteRepository.CountByCardAsync(cardId, ct)`.
5. Send to group `sprint:{sprintId}`: event name `"VoteRetracted"`, payload `{ cardId, newCount }`.

**Important:** The event handlers need `CardRepository` to resolve `SprintId` from `CardId`. Inject it alongside `VoteRepository`.

**Pattern:** Follow `CardCreatedEventHandler.cs` — same structure, different event and payload.
**Dependencies:** Steps 1–2.

### Step 8: Seed data update

**What:** Add seeded votes to the existing seeded sprint.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs`

**Seed logic (after card seeding):**
1. Load seeded participants and cards for the sprint.
2. If `!db.Votes.Any()`: create a few votes using `Vote.Create(...)`, clear domain events (same pattern as card seeding), add, save.
3. Suggested seed data:
   - Participant A: 2 votes on the first WentWell card, 1 vote on a DidntGoWell card (uses full budget of 3).
   - Participant B: 1 vote on the first WentWell card, 1 vote on the first DidntGoWell card (uses 2 of 3).
4. Idempotent.

**Dependencies:** Steps 1–3.

### Step 9: Frontend — vote store + SignalR vote events

**What:** Create the Pinia vote store and wire vote SignalR events.
**Files to create:**
- `web/reflekt-web/src/stores/vote.ts`
**Files to modify:**
- `web/reflekt-web/src/composables/useSprintHub.ts` — add vote event listeners.

**vote.ts — Pinia store:**
```
interface VoteDistribution {
  cardId: number
  count: number
}

state: {
  myVotes: VoteDistribution[]   // from GetMyVotes endpoint
  voteCounts: Map<number, number>  // cardId → total count (from SignalR + initial load)
}

getters: {
  remainingBudget: number    // sprint.votesPerParticipant - sum(myVotes[].count)
  myVotesOnCard(cardId): number  // lookup in myVotes
  voteCountForCard(cardId): number  // lookup in voteCounts
}

actions: {
  fetchMyVotes(sprintId)           // GET /api/sprints/{sprintId}/votes/mine
  castVote(sprintId, cardId)       // POST /api/sprints/{sprintId}/cards/{cardId}/votes
  retractVote(sprintId, cardId)    // DELETE /api/sprints/{sprintId}/cards/{cardId}/votes
  setVoteCount(cardId, count)      // called by SignalR handler
}
```

All API calls include `X-Guest-Token` header from the guest store.

**Initial vote counts:** The vote store needs initial vote counts for all cards. Two options: (a) add a `voteCount` field to the card list endpoint response, or (b) call a separate endpoint. **Recommend (a)** — extend `CardResponse` in `CardContracts.cs` to include `int VoteCount` populated via a count query in `ListCardsEndpoint`. This avoids a separate round-trip. The Developer should modify `CardContracts.cs` and `ListCardsEndpoint.cs` to include this field.

**useSprintHub.ts — add vote event listeners:**
- `VoteCast`: call `voteStore.setVoteCount(payload.cardId, payload.newCount)`.
- `VoteRetracted`: call `voteStore.setVoteCount(payload.cardId, payload.newCount)`.

**Pattern:** Reference `stores/card.ts` for store shape and API call patterns. Reference existing SignalR event handlers in `useSprintHub.ts` for the listener pattern.
**Dependencies:** Steps 4–6 (API endpoints).

### Step 10: Frontend — vote UI on cards

**What:** Add vote buttons and counts to card items in the retro room.
**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue` (or the card component if one was extracted)

**UI additions per Note card:**
1. Vote button with current vote count badge (e.g., "▲ 3").
2. Clicking the vote button calls `voteStore.castVote(sprintId, cardId)`.
3. If the participant has votes on this card, show a retract indicator (e.g., filled/highlighted button, or small "−" next to count). Clicking retract calls `voteStore.retractVote(sprintId, cardId)`.
4. **Budget display:** somewhere visible (header area or sidebar), show remaining votes: "Votes: {remaining}/{total}".
5. **Disable vote button** when `remainingBudget === 0` (grey out, show tooltip "No votes remaining").
6. **ActionItem cards do NOT show vote buttons.** Only Note cards (WentWell, DidntGoWell columns) get the voting UI.

**Optimistic UI:**
- CastVote: increment local vote count and myVoteCount immediately. On failure, revert and show toast.
- RetractVote: decrement local vote count and myVoteCount immediately. On failure, revert and show toast.

**Data loading:** On `enterRoom()`, after `cardStore.fetchCards(sprintId)` completes (which now includes voteCounts), call `voteStore.fetchMyVotes(sprintId)`.

**Styling:** Tailwind. Vote button compact, inline with card content. Subtle highlight when participant has votes on the card.

**Pattern:** Reference existing card UI in `RetroRoomView.vue`.
**Dependencies:** Step 9.

### Step 11: Extend ListCards with vote counts

**What:** Add `voteCount` to the card list response to avoid a separate endpoint for initial state.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs` — add `int VoteCount` to `CardResponse`.
- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs` — query vote counts and populate.

**Implementation:**
1. In `CardContracts.cs`: add `int VoteCount` field to `CardResponse` record. Update `ToResponse` to accept a vote count parameter.
2. In `ListCardsEndpoint.cs`: after loading cards, do a bulk vote count query: `SELECT CardId, COUNT(*) FROM Votes WHERE CardId IN (...) GROUP BY CardId`. Build a dictionary `cardId → count`. Map into `CardResponse` with the count (default 0 for cards with no votes).

**Why here and not in Step 4:** This is a cross-feature concern (modifying Cards code for Voting). Keeping it as a separate step makes the dependency explicit.

**Dependencies:** Steps 2–3 (Votes table must exist for the query).

### Step 12: Unit tests

**What:** Test the Vote aggregate domain logic.
**Files to create:**
- `tests/Reflekt.UnitTests/Votes/VoteTests.cs`

**Test cases:**

*Create factory:*
1. `Create_SetsAllPropertiesCorrectly` — verify CardId, ParticipantId, CreatedAt stamped.
2. `Create_RaisesVoteCastEvent` — verify DomainEvents contains one VoteCastEvent.
3. `Create_EventContainsVoteReference` — verify the event's Vote property is the created vote.

*MarkRetracted method:*
4. `MarkRetracted_RaisesVoteRetractedEvent` — verify DomainEvents contains one VoteRetractedEvent.
5. `MarkRetracted_EventContainsVoteReference`.

**Note:** Budget check, action-item rejection, and closed-sprint check are handler-level concerns — tested in integration tests, not unit tests.

**Pattern:** Reference `tests/Reflekt.UnitTests/Cards/CardTests.cs`.
**Dependencies:** Step 1.

### Step 13: Integration tests

**What:** Test Vote endpoints end-to-end.
**Files to create:**
- `tests/Reflekt.IntegrationTests/VoteEndpointTests.cs`

**Test setup:**
- Same `WebApplicationFactory<Program>` pattern as `CardEndpointTests.cs`.
- Each test creates a team, starts a sprint, joins a participant, and creates a Note card first.
- For guest-token-authenticated requests: capture `guestToken` from JoinSprint response.

**Test cases:**

*CastVote:*
1. `CastVote_Returns201` — valid vote on a Note card → 201, response has cardId + voteCount + myVoteCount.
2. `CastVote_MultipleTimes_SameCard_Succeeds` — place 2 votes on same card → both succeed, myVoteCount=2.
3. `CastVote_BudgetExhausted_Returns409` — cast 3 votes, then try 4th → 409.
4. `CastVote_OnActionItem_Returns400`.
5. `CastVote_ClosedSprint_Returns409`.
6. `CastVote_NonexistentCard_Returns404`.
7. `CastVote_CardInDifferentSprint_Returns404`.
8. `CastVote_NoGuestToken_Returns401`.
9. `CastVote_WrongSprint_Returns401` — token from sprint A, vote in sprint B.

*RetractVote:*
10. `RetractVote_Returns200` — retract one vote → 200, voteCount decremented.
11. `RetractVote_WhenNoVotesOnCard_Returns409`.
12. `RetractVote_ClosedSprint_Returns409`.
13. `RetractVote_ReturnsBudget` — cast 3 votes, retract 1, cast 1 more → succeeds (budget restored).
14. `RetractVote_NoGuestToken_Returns401`.

*GetMyVotes:*
15. `GetMyVotes_ReturnsDistribution` — cast votes on multiple cards → returns correct distribution.
16. `GetMyVotes_Empty_ReturnsEmptyList`.
17. `GetMyVotes_NoGuestToken_Returns401`.
18. `GetMyVotes_NonexistentSprint_Returns404`.

*ListCards with VoteCount:*
19. `ListCards_IncludesVoteCount` — cards have correct vote counts.
20. `ListCards_NoVotes_VoteCountIsZero`.

**Pattern:** Reference `CardEndpointTests.cs`.
**Dependencies:** Steps 4–6, 11.

## 6. Migration notes

Next EF Core migration after Cards. Command:
```bash
dotnet ef migrations add AddVote -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

The migration should only add the `Votes` table. If it includes changes to existing tables, review carefully.

Auto-applied on startup in Development. Delete `data/reflekt.db` before first run if migration conflicts arise.

## 7. Testing strategy

- **Unit tests** (Step 12) cover the factory method and MarkRetracted event raising. Minimal because Vote has no domain guards.
- **Integration tests** (Step 13) cover the full HTTP pipeline: budget enforcement, card-type rejection, sprint-state gating, retraction, and the GetMyVotes query.
- **SignalR is not integration-tested** — event handlers push to groups; verifying they don't throw is sufficient. Frontend manual test covers the round-trip.
- **Optimistic UI** is tested manually in the browser.
- **No frontend tests** — not in scope for v1.

## 8. Verification — acceptance criteria

Voting is done when all of these pass:

1. `dotnet build Reflekt.slnx` — zero errors, zero warnings.
2. `dotnet test` — all tests pass (existing + new unit + new integration).
3. `dotnet run` in Development:
   - `data/reflekt.db` has `Votes` table with correct schema.
   - Seed data creates sample votes.
4. Manual API flow (Scalar or curl):
   - Join a sprint as a participant (get guest token).
   - Create a Note card.
   - `POST /api/sprints/{sprintId}/cards/{cardId}/votes` → 201 with `{ cardId, voteCount: 1, myVoteCount: 1 }`.
   - Vote again on same card → 201 with `{ cardId, voteCount: 2, myVoteCount: 2 }`.
   - Vote a third time → 201 (budget used = 3).
   - Vote a fourth time → 409 "Vote budget exhausted".
   - `DELETE /api/sprints/{sprintId}/cards/{cardId}/votes` → 200 with `{ cardId, voteCount: 1, myVoteCount: 1 }`.
   - Vote on an ActionItem card → 400.
   - Vote on a Closed sprint → 409.
   - `GET /api/sprints/{sprintId}/votes/mine` → returns distribution.
   - `GET /api/sprints/{sprintId}/cards` → cards have `voteCount` field.
5. Frontend flow (`npm run dev`):
   - Navigate to sprint room, join.
   - Note cards show vote button with count.
   - Action item cards do NOT show vote button.
   - Click vote button → count increments, remaining budget decrements.
   - When budget is 0, vote buttons are disabled.
   - Click retract → count decrements, budget restores.
   - Open second browser tab, join as second participant — first tab sees vote count change in real-time (SignalR).
   - Refresh page → vote state (counts + own votes) restored correctly.
6. Existing Card, Participant, Sprint, and Team endpoints still work — no regressions.
7. SignalR events fire correctly: VoteCast and VoteRetracted push `{ cardId, newCount }` to sprint group.

## 9. Out of scope

If tempted to add any of these, stop and check the relevant feature plan:

- `EvidenceLink` entity or URL previews — **EvidenceLinks plan**.
- Report generation or filtered views — **Reports plan**.
- Card sort-order by vote count in the API response — **not in v1 API** (frontend sorts locally).
- Vote visibility toggle (show/hide during voting phase) — **not in v1**.
- Weighted votes or priority ordering — **not in v1**.
- SM vote override (adding/removing other people's votes) — **not in v1**.
- VotesPerParticipant editing after sprint creation — **not in v1**.

## 10. File summary

**New files:**
- `src/Services/Reflekt/Reflekt.Domain/Votes/Vote.cs`
- `src/Services/Reflekt/Reflekt.Domain/Votes/Behaviors/Vote.cs`
- `src/Services/Reflekt/Reflekt.Domain/Votes/Events/VoteCastEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Votes/Events/VoteRetractedEvent.cs`
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/VoteConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/VoteRepository.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Votes/VoteContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/CastVoteEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/RetractVoteEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Votes/GetMyVotes/GetMyVotesEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Events/VoteCastEventHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Events/VoteRetractedEventHandler.cs`
- `web/reflekt-web/src/stores/vote.ts`
- `tests/Reflekt.UnitTests/Votes/VoteTests.cs`
- `tests/Reflekt.IntegrationTests/VoteEndpointTests.cs`

**Modified files:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `DbSet<Vote>`
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `VoteRepository`
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — seed sample votes
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs` — add `VoteCount` to `CardResponse`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs` — populate vote counts
- `web/reflekt-web/src/composables/useSprintHub.ts` — add VoteCast/VoteRetracted listeners
- `web/reflekt-web/src/views/RetroRoomView.vue` — add vote UI to note cards
- `web/reflekt-web/src/stores/card.ts` — read `voteCount` from card response into vote store on fetch

**Unchanged:**
- All Auth files, Team files, Sprint files, Participant domain/persistence files, `Program.cs`, `Policies.cs`, `SprintHub.cs`, `appsettings*.json`.
