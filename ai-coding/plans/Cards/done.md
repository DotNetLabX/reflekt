# Cards — Done

## Files Created

### Domain
- `Reflekt.Domain/Cards/ColumnType.cs` — enum: WentWell, DidntGoWell, ActionItems
- `Reflekt.Domain/Cards/CardType.cs` — enum: Note, ActionItem
- `Reflekt.Domain/Cards/Card.cs` — aggregate properties partial class
- `Reflekt.Domain/Cards/Behaviors/Card.cs` — Create, Update, MarkDeleted; guards enforce column/type invariants
- `Reflekt.Domain/Cards/Events/CardCreatedEvent.cs`
- `Reflekt.Domain/Cards/Events/CardUpdatedEvent.cs`
- `Reflekt.Domain/Cards/Events/CardDeletedEvent.cs`

### Persistence
- `Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs` — SprintId cascades, ParticipantId restricts
- `Reflekt.Persistence/Repositories/CardRepository.cs` — GetBySprintIdAsync (desc), Remove
- `Reflekt.Persistence/Migrations/20260420144939_AddCard.cs` — Cards table only, correct FKs and indexes

### Modified Persistence
- `ReflektDbContext.cs` — added `DbSet<Card> Cards`
- `DependencyInjection.cs` — registered `CardRepository`
- `DbInitializer.cs` — seeds 5 cards across all three columns, idempotent

### API Endpoints
- `Features/Cards/CardContracts.cs` — CardResponse record + CardMappings.ToResponse
- `Features/Cards/CreateCard/CreateCardCommand.cs` + `CreateCardEndpoint.cs` — Participant policy; sprint/claim cross-check; ArgumentException from domain → 400
- `Features/Cards/EditCard/EditCardCommand.cs` + `EditCardEndpoint.cs` — OwnerOrScrumMaster; dual-auth ownership check
- `Features/Cards/DeleteCard/DeleteCardEndpoint.cs` — MarkDeleted → Remove → SaveChanges sequence; 204
- `Features/Cards/ListCards/ListCardsEndpoint.cs` — dual-auth read

### SignalR Handlers
- `Events/CardCreatedEventHandler.cs` — sends full CardResponse
- `Events/CardUpdatedEventHandler.cs` — sends full CardResponse
- `Events/CardDeletedEventHandler.cs` — sends `{ id }` only

### Frontend
- `stores/card.ts` — Pinia store with state, getters (wentWell/didntGoWell/actionItems), CRUD actions
- `composables/useSprintHub.ts` — wired CardCreated, CardUpdated, CardDeleted listeners
- `views/RetroRoomView.vue` — replaced placeholder with three-column grid; optimistic UI; CardItem integration
- `components/CardItem.vue` — inline edit, edit/delete buttons gated by canEdit prop

### Tests
- `tests/Reflekt.UnitTests/Cards/CardTests.cs` — 13 tests covering Create, Update, MarkDeleted
- `tests/Reflekt.IntegrationTests/CardEndpointTests.cs` — 26 tests covering full HTTP pipeline

## Review Corrections

- **BUG fixed**: Removed `this.cards.unshift(card)` from `createCard` store action — the component owns the optimistic lifecycle, so the action now only calls the API and returns the card. Prevented duplicate card on create.
- **MINOR fixed**: Renamed test `CreateCard_NonexistentSprint_Returns404` → `CreateCard_NonexistentSprint_Returns401` to match actual behavior.
- **MINOR fixed**: Save button in `CardItem.vue` disabled when assignee is empty for ActionItem cards.

## Key Decisions

- `CreateCard_NonexistentSprint_Returns404` → actually returns 401 because sprint ID claim check fires before DB lookup; test updated accordingly.
- `ListCards_NoAuth_Returns401` and `CreateCard_NoGuestToken_Returns401` use `_anonFactory`/`_anonClient` (no TestAuthHandler), same pattern as ParticipantEndpointTests.
- `CardItem.vue` created as new component (not in plan file list but required by RetroRoomView).
- Domain guards throw ArgumentException on invalid column/type combos; endpoints catch and convert to 400.
- `dotnet build -q` flag produces misleading "Question build FAILED" output on this machine; use full output instead.
