# Voting — Implementation Log

## Step 1: Vote aggregate + domain events
- Created `Reflekt.Domain/Votes/Vote.cs` — properties file: `CardId`, `ParticipantId`, `CreatedAt`, private EF ctor.
- Created `Reflekt.Domain/Votes/Behaviors/Vote.cs` — `Create` factory (sets properties, raises `VoteCastEvent`), `MarkRetracted` (raises `VoteRetractedEvent`). No guards per plan.
- Created `Reflekt.Domain/Votes/Events/VoteCastEvent.cs` — `IDomainEvent` record carrying `Vote`.
- Created `Reflekt.Domain/Votes/Events/VoteRetractedEvent.cs` — same shape.

## Step 2: EF config + DbSet + repository + DI
- Created `Reflekt.Persistence/EntityConfigurations/VoteConfiguration.cs` — FK Cascade on Card, Restrict on Participant, two indexes.
- Created `Reflekt.Persistence/Repositories/VoteRepository.cs` — all six methods from plan plus `GetVoteCountsByCardIdsAsync` (bulk dictionary, added for ListCards use case). Also defined `VoteDistributionEntry` record in same file.
- Modified `ReflektDbContext.cs` — added `DbSet<Vote> Votes`.
- Modified `DependencyInjection.cs` — registered `VoteRepository`.

## Step 3: EF migration
- Generated `20260420170934_AddVote.cs` — only creates `Votes` table with correct columns, FKs, and indexes.

## Step 4: CastVote endpoint
- Created `Reflekt.API/Features/Votes/VoteContracts.cs` — `VoteResponse` and `VoteDistributionEntry` records.
- Created `Reflekt.API/Features/Votes/CastVote/CastVoteEndpoint.cs` — full handler flow per plan: sprintId claim check, card null+sprint check, ActionItem guard (400), sprint closed (409), budget exhausted (409), create+save, return counts.

## Step 5: RetractVote endpoint
- Created `Reflekt.API/Features/Votes/RetractVote/RetractVoteEndpoint.cs` — MarkRetracted → Remove → SaveChanges → return counts.

## Step 6: GetMyVotes endpoint
- Created `Reflekt.API/Features/Votes/GetMyVotes/GetMyVotesEndpoint.cs` — reads distribution from VoteRepository, maps to API contract type.

## Step 7: SignalR event handlers
- Created `Reflekt.API/Events/VoteCastEventHandler.cs` — loads card for SprintId, queries newCount, sends `VoteCast` to sprint group.
- Created `Reflekt.API/Events/VoteRetractedEventHandler.cs` — same pattern, sends `VoteRetracted`.

## Step 8: Seed data
- Modified `DbInitializer.cs` — added vote seeding block; Participant A: 2×wentWell[0] + 1×didntGoWell[0]; Participant B: 1×wentWell[0] + 1×didntGoWell[0]. Idempotent via `!db.Votes.Any()`.

## Step 9: Frontend vote store + SignalR
- Created `web/reflekt-web/src/stores/vote.ts` — Pinia store with `myVotes`, `voteCounts` (Map), `votesPerParticipant`; getters `remainingBudget`, `myVotesOnCard`, `voteCountForCard`; actions with optimistic updates + revert on failure.
- Added `initVoteCounts` action to seed vote counts from card list response.
- Modified `useSprintHub.ts` — added `VoteCast` and `VoteRetracted` listeners calling `voteStore.setVoteCount`.

## Step 10: Frontend vote UI
- Modified `CardItem.vue` — added optional `showVoting`, `voteCount`, `myVoteCount`, `remainingBudget` props; vote button (▲ count), retract button (−), "mine" indicator; disabled when budget=0 and no votes.
- Modified `RetroRoomView.vue` — imports vote store, passes vote props to WentWell/DidntGoWell columns, budget display in header, `castVote`/`retractVote` handlers with toast on failure, `fetchMyVotes` in `enterRoom`.
- Modified `card.ts` — added `voteCount` field to Card interface, `fetchCards` calls `voteStore.initVoteCounts` after fetch.

## Step 11: Extend ListCards with vote counts
- Modified `CardContracts.cs` — added `int VoteCount = 0` to `CardResponse`, updated `ToResponse` to accept optional voteCount param.
- Added `GetVoteCountsByCardIdsAsync` to `VoteRepository` — bulk group-by query returning `Dictionary<int,int>`.
- Modified `ListCardsEndpoint.cs` — injects `VoteRepository`, bulk-fetches counts, passes per-card count to `ToResponse`.

## Step 12: Unit tests
- Created `tests/Reflekt.UnitTests/Votes/VoteTests.cs` — 5 tests: property correctness, VoteCastEvent raised, event reference, VoteRetractedEvent raised, retracted event reference.

## Step 13: Integration tests
- Created `tests/Reflekt.IntegrationTests/VoteEndpointTests.cs` — 20 tests covering all plan cases: CastVote (9), RetractVote (5), GetMyVotes (4), ListCards voteCount (2).

## Test fixes (post-first-run)
- `PostVote` helper initially sent no body → FastEndpoints returned 415 on all CastVote tests. Fixed by adding `Content = JsonContent.Create(new { })` — POST endpoints require `Content-Type: application/json` even with route-only params.
- `GetMyVotes_NonexistentSprint_Returns404` — auth check (sprint claim mismatch) fires before sprint lookup, so 404 is unreachable. Renamed to `GetMyVotes_WrongSprint_Returns401` with 401 assertion.

## Decisions / deviations
- `VoteDistributionEntry` is defined in both `VoteRepository.cs` (persistence return type) and `VoteContracts.cs` (API contract). They are in different namespaces; the endpoint maps between them. No ambiguity because parent namespace takes precedence over `using` imports.
- `GetVoteCountsByCardIdsAsync` was added to `VoteRepository` beyond the plan's listed methods — needed for the bulk ListCards use case (plan §11 described the requirement but didn't list it in §2's method list).
- `initVoteCounts` action added to vote store — plan §9 said `card.ts` should "read `voteCount` from card response into vote store on fetch" but didn't name the action.
- `votesPerParticipant` in the vote store uses default value 3 (matches sprint default). Fetching the actual sprint value is not in this plan's scope.
