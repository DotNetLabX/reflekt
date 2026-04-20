# Participants — Done

## Step 1: Generic ID support in BuildingBlocks

- `src/BuildingBlocks/BuildingBlocks.Domain/IHasDomainEvents.cs` — created; interface with `DomainEvents` + `ClearDomainEvents`
- `src/BuildingBlocks/BuildingBlocks.Domain/Entity.cs` — made generic (`Entity<TId>`); non-generic alias added
- `src/BuildingBlocks/BuildingBlocks.Domain/AggregateRoot.cs` — made generic (`AggregateRoot<TId>` implements `IHasDomainEvents`); non-generic alias added
- `src/BuildingBlocks/BuildingBlocks.Persistence/EntityConfigurations/EntityConfiguration.cs` — made generic (`EntityConfiguration<T, TId>`); non-generic alias added
- `src/BuildingBlocks/BuildingBlocks.Persistence/Repositories/RepositoryBase.cs` — made generic (`RepositoryBase<TContext, TEntity, TId>`); non-generic alias constraint changed to `Entity<int>` (not `Entity`) — required because Sprint/Team inherit via `AggregateRoot<int>` not through the non-generic `Entity` alias class
- `src/BuildingBlocks/BuildingBlocks.Persistence/DomainEventPublishingInterceptor.cs` — changed `Entries<AggregateRoot>()` to `Entries().OfType<IHasDomainEvents>()`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/Repository.cs` — added `Repository<TEntity, TId>` overload; changed non-generic constraint to `Entity<int>`

**Key deviation:** Non-generic aliases constrain to `Entity<int>` (not `Entity` class) because existing aggregates (Sprint, Team) inherit through `AggregateRoot<int> : Entity<int>`, not through the non-generic `Entity` alias class.

## Step 2: Participant aggregate + domain events

- `src/Services/Reflekt/Reflekt.Domain/Participants/Participant.cs` — properties partial class; private setter properties; EF private parameterless constructor
- `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs` — `Join` static factory; guards displayName; sets GuestToken via `Guid.NewGuid().ToString()`; raises `ParticipantJoinedEvent`
- `src/Services/Reflekt/Reflekt.Domain/Participants/Events/ParticipantJoinedEvent.cs` — sealed record carrying `Participant` reference

## Step 3: EF config + DbSet + repository + DI

- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/ParticipantConfiguration.cs` — cascade delete on SprintId FK; unique index on GuestToken; index on SprintId
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` — `GetByGuestTokenAsync` (single by token); `GetBySprintIdAsync` (list ordered by JoinedAt)
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — added `DbSet<Participant> Participants`
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — registered `ParticipantRepository` as scoped

## Step 4: EF Migration

- Migration `20260420135139_AddParticipant` created; only creates `Participants` table — no phantom changes to Teams/Sprints

## Step 5: Wire GuestTokenAuthenticationHandler

- `src/Services/Reflekt/Reflekt.API/Auth/GuestTokenAuthenticationHandler.cs` — constructor-injected `ParticipantRepository`; issues `participantId`, `sprintId`, `displayName` claims on successful token lookup

## Step 6: Shared contracts + JoinSprint endpoint

- `src/Services/Reflekt/Reflekt.API/Features/Participants/ParticipantContracts.cs` — `JoinSprintResponse` (with token), `ParticipantResponse` (no token), `ParticipantMappings` static helpers
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/JoinSprintCommand.cs` — request record + FluentValidation (NotEmpty, MaxLength 50)
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/JoinSprintEndpoint.cs` — `[AllowAnonymous]`; 409 via `ThrowError` for non-Active sprint

## Step 7: GetCurrentParticipant + GetParticipants endpoints

- `src/Services/Reflekt/Reflekt.API/Features/Participants/GetCurrentParticipant/GetCurrentParticipantEndpoint.cs` — validates claim's `sprintId` matches route; returns 401 on mismatch
- `src/Services/Reflekt/Reflekt.API/Features/Participants/GetParticipants/GetParticipantsEndpoint.cs` — detects auth type by presence of `participantId` claim; guest path checks sprint scoping; SM path checks team.CreatorEmail

## Step 8: SprintHub participant validation + disconnect handling

- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` — `JoinSprint` validates claim `sprintId` matches param, throws `HubException` on mismatch; `OnDisconnectedAsync` pushes `ParticipantLeft` directly to sprint group with `{ participantId }`

## Step 9: ParticipantJoined event handler

- `src/Services/Reflekt/Reflekt.API/Events/ParticipantJoinedEventHandler.cs` — follows `PhaseAdvancedEventHandler` pattern; sends `"ParticipantJoined"` with `{ id, displayName }` to `sprint:{sprintId}` group

## Step 10: Seed data update

- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — sprint now started (Active) before participant seeding; 3 participants ("Ana", "Mihai", "Elena"); idempotent
- **Deviation:** Sprint is started before seeding participants to produce consistent seed data (Active sprint with participants matches business rule)

## Step 11: Frontend — guest store + participant store + SignalR connection

- `web/reflekt-web/src/stores/guest.ts` — fleshed out from stub; `loadToken`, `saveToken`, `clearToken` localStorage actions
- `web/reflekt-web/src/stores/participant.ts` — created; `join`, `fetchMe`, `fetchParticipants` actions; includes X-Guest-Token header
- `web/reflekt-web/src/composables/useSprintHub.ts` — created; `HubConnectionBuilder` with `accessTokenFactory`; `connect`/`disconnect`; listens for `ParticipantJoined`, `ParticipantLeft`, `PhaseChanged`

## Step 12: Frontend — join flow UI

- `web/reflekt-web/src/views/RetroRoomView.vue` — created; join flow with silent rejoin; retro room shell with participant chips; Tailwind styling
- `web/reflekt-web/src/router/index.ts` — added `/sprint/:sprintId` route → `RetroRoomView`

## Step 13: Unit tests

- `tests/Reflekt.UnitTests/Participants/ParticipantTests.cs` — 5 tests: properties, unique tokens, domain event, empty/whitespace name guards

## Step 14: Integration tests

- `tests/Reflekt.IntegrationTests/ParticipantEndpointTests.cs` — 14 tests covering join flow, guest token auth, sprint-state gating, cross-sprint token rejection, SM list access
- Factory setup: TestAuthHandler for SM; `OwnerOrScrumMaster` policy overridden to include TestAuthHandler scheme; `GuestTokenAuthenticationHandler` kept from app registration

## Final verification

- `dotnet build Reflekt.slnx`: 0 errors, 0 warnings
- `dotnet test`: 79 total (38 unit + 41 integration), all pass
- `npm run type-check`: clean
