# Cards Plan — Reflekt v1

**Status:** Ready for implementation
**Prerequisite:** Auth (landed), Teams (landed), Sprints (landed), Participants (landed)
**Prerequisite for:** Votes (needs `CardId`), EvidenceLinks (needs `CardId`), Reports (reads Cards)
**Companion docs:** `docs/features/Cards.md`, `docs/architecture/v1.md`

## 0. Purpose

Introduce the Card aggregate — the core content of a retro. Participants create notes in the Went Well and Didn't Go Well columns, and action items (with assignees) in the Action Items column. The card author and the Scrum Master can edit and delete cards. All mutations push real-time events via SignalR. This feature replaces the "Cards and voting coming soon…" placeholder in the retro room with a working three-column board.

**Non-goals for Cards:**
- No voting — that's the Voting feature.
- No evidence links — that's the EvidenceLinks feature.
- No report generation — that's the Reports feature.
- No drag-and-drop reordering, card grouping, or column configuration.
- No draft text sync while typing — push on save only.

If you find yourself writing a `Vote` entity, an `EvidenceLink` entity, or any endpoint under `/api/.../votes/`, **stop** — that belongs in a different plan.

## 1. Relevant skills

Read each skill file before applying it — this plan only names them.

- **`create-aggregate`** — for the `Card` aggregate root. Follow its partial-class split.
- **`create-feature`** — for each endpoint (CreateCard, EditCard, DeleteCard, ListCards).
- **`persistence-patterns`** — for `CardConfiguration`, `CardRepository`, `DbSet` registration.
- **`domain-patterns`** — for domain event definitions and enum placement.
- **`authorization-patterns`** — reference for auth policy usage on endpoints.

**Pattern references (actual codebase):**
- Aggregate split: `Reflekt.Domain/Participants/Participant.cs` (properties) + `Reflekt.Domain/Participants/Behaviors/Participant.cs` (factory + methods)
- Domain event: `Reflekt.Domain/Participants/Events/ParticipantJoinedEvent.cs`
- Enum: `Reflekt.Domain/Sprints/SprintState.cs`, `Reflekt.Domain/Sprints/SprintPhase.cs`
- Endpoint (create): `Reflekt.API/Features/Participants/JoinSprint/JoinSprintEndpoint.cs`
- Endpoint (read, dual auth): `Reflekt.API/Features/Participants/GetParticipants/GetParticipantsEndpoint.cs`
- Command + validator: `Reflekt.API/Features/Participants/JoinSprint/JoinSprintCommand.cs`
- Shared contracts: `Reflekt.API/Features/Participants/ParticipantContracts.cs`
- Repository: `Reflekt.Persistence/Repositories/ParticipantRepository.cs`
- Entity config: `Reflekt.Persistence/EntityConfigurations/ParticipantConfiguration.cs`
- Event handler (SignalR broadcaster): `Reflekt.API/Events/ParticipantJoinedEventHandler.cs`
- Hub: `Reflekt.API/Hubs/SprintHub.cs`
- Seed data: `Reflekt.Persistence/DbInitializer.cs`
- Unit test: `tests/Reflekt.UnitTests/Participants/ParticipantTests.cs`
- Integration test: `tests/Reflekt.IntegrationTests/ParticipantEndpointTests.cs`
- Frontend store: `web/reflekt-web/src/stores/participant.ts`
- Frontend composable: `web/reflekt-web/src/composables/useSprintHub.ts`
- Frontend view: `web/reflekt-web/src/views/RetroRoomView.vue`

## 2. Judgment calls flagged for the Developer

Small decisions the Developer can make without coming back:

- **`Remove` method placement.** The CardRepository needs a `Remove(Card card)` method. Add it directly to `CardRepository` (e.g., `public void Remove(Card card) => Db.Cards.Remove(card);`). Do not modify the `Repository<T>` or `RepositoryBase` base classes — if a shared `Remove` is needed later, that's a separate refactor.
- **Edit UX.** Inline edit (click text to edit in-place) vs modal — Developer picks. Recommend inline for simplicity.
- **Column layout.** CSS grid vs flexbox for the three-column layout — Developer picks.
- **Card input placement.** Text input at the bottom vs top of each column — Developer picks. Recommend bottom (natural writing position).
- **Empty state.** What to show when a column has no cards — Developer picks (e.g., light placeholder text).

Decisions that should come back if they feel wrong mid-implementation:

- If the `MarkDeleted()` pattern (raising a domain event before EF removal) doesn't work with the interceptor for deleted entities — flag it. The interceptor's `ChangeTracker.Entries()` should include entities with state `Deleted`, but verify.
- If the dual auth check on edit/delete (guest token vs SM cookie) gets complex — flag it rather than inventing a new abstraction.

## 3. Domain model changes

### 3.1 Enums

**`src/Services/Reflekt/Reflekt.Domain/Cards/ColumnType.cs`:**
```
ColumnType { WentWell, DidntGoWell, ActionItems }
```

**`src/Services/Reflekt/Reflekt.Domain/Cards/CardType.cs`:**
```
CardType { Note, ActionItem }
```

Follow the same file-per-enum pattern as `SprintState.cs` and `SprintPhase.cs`.

### 3.2 Card aggregate — partial class split

**Properties file:** `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs`

- Extends `AggregateRoot` (non-generic — int ID, consistent with all existing aggregates).
- Properties:
  - `int SprintId` — FK to Sprint (by ID, no navigation).
  - `int ParticipantId` — FK to Participant (by ID, no navigation). The card author.
  - `ColumnType ColumnType` — which column this card lives in.
  - `CardType CardType` — Note or ActionItem.
  - `string Text` — card content.
  - `string? AssigneeName` — nullable; set only for ActionItems.
  - `DateTime CreatedAt` — UTC, stamped at creation.
  - `DateTime UpdatedAt` — UTC, stamped at creation and on every update.
- Private parameterless constructor for EF Core.

**Behaviors file:** `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs`

Three methods:

**`Create(string text, int sprintId, int participantId, ColumnType columnType, CardType cardType, string? assigneeName)`** — static factory:
- Guard: `text` must not be null/whitespace.
- Guard: if `cardType == ActionItem`, then `assigneeName` must not be null/whitespace.
- Guard: if `cardType == ActionItem`, then `columnType` must be `ActionItems`.
- Guard: if `cardType == Note`, then `columnType` must NOT be `ActionItems`.
- Guard: if `cardType == Note`, then `assigneeName` must be null (reject if provided).
- Sets all properties. `CreatedAt = DateTime.UtcNow`. `UpdatedAt = CreatedAt`.
- Raises `CardCreatedEvent(this)`.

**`Update(string text, string? assigneeName)`** — instance method:
- Guard: `text` must not be null/whitespace.
- Guard: if `CardType == ActionItem`, then `assigneeName` must not be null/whitespace.
- Guard: if `CardType == Note`, then `assigneeName` must be null.
- Sets `Text = text`, `AssigneeName = assigneeName`, `UpdatedAt = DateTime.UtcNow`.
- Raises `CardUpdatedEvent(this)`.

**`MarkDeleted()`** — instance method:
- No guards, no property changes.
- Raises `CardDeletedEvent(this)`.
- Called by the endpoint before EF removal so the interceptor can publish the event.

Skill: `create-aggregate`

### 3.3 Domain events

Three events in `src/Services/Reflekt/Reflekt.Domain/Cards/Events/`:

```
CardCreatedEvent(Card Card) : IDomainEvent
CardUpdatedEvent(Card Card) : IDomainEvent
CardDeletedEvent(Card Card) : IDomainEvent
```

Follow CLAUDE.md convention: carry the aggregate reference. At dispatch time (post-`SaveChangesAsync`), all Card state including `Id` is populated. For `CardDeletedEvent`, the entity is in memory with state `Deleted` — the handler reads `Card.SprintId` and `Card.Id` before the object is garbage collected.

Skill: `domain-patterns`

## 4. Data model changes

### 4.1 Table: `Cards`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `INTEGER` | PK, auto-increment (from `Entity` base) |
| `SprintId` | `INTEGER` | Required, FK to `Sprints.Id` |
| `ParticipantId` | `INTEGER` | Required, FK to `Participants.Id` |
| `ColumnType` | `INTEGER` | Required (enum stored as int) |
| `CardType` | `INTEGER` | Required (enum stored as int) |
| `Text` | `TEXT` | Required, max 2000 |
| `AssigneeName` | `TEXT` | Nullable, max 50 |
| `CreatedAt` | `TEXT` (SQLite datetime) | Required |
| `UpdatedAt` | `TEXT` (SQLite datetime) | Required |

FK: `SprintId → Sprints.Id`, **cascade delete** (spec §4 — all FKs cascade on Sprint removal).
FK: `ParticipantId → Participants.Id`, **restrict** (no cascade — prevents issues with multiple cascade paths through Sprint→Participant→Card. Participants are never independently deleted, so Restrict is safe).
Index on `SprintId` (used by ListCards query — primary access pattern).

### 4.2 EF Configuration — `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs`

`EntityConfiguration<Card>` (extends base from BuildingBlocks):
- `ToTable("Cards")`
- `Property(c => c.SprintId).IsRequired()`
- `HasOne<Sprint>().WithMany().HasForeignKey(c => c.SprintId).OnDelete(DeleteBehavior.Cascade)`
- `Property(c => c.ParticipantId).IsRequired()`
- `HasOne<Participant>().WithMany().HasForeignKey(c => c.ParticipantId).OnDelete(DeleteBehavior.Restrict)`
- `Property(c => c.ColumnType).IsRequired()`
- `Property(c => c.CardType).IsRequired()`
- `Property(c => c.Text).IsRequired().HasMaxLength(2000)`
- `Property(c => c.AssigneeName).HasMaxLength(50)`
- `HasIndex(c => c.SprintId)`
- `Property(c => c.CreatedAt).IsRequired()`
- `Property(c => c.UpdatedAt).IsRequired()`

Skill: `persistence-patterns`

## 5. Implementation steps

### Step 1: Card aggregate + enums + domain events

**What:** Create the entire Card domain model.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Cards/ColumnType.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/CardType.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/CardCreatedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/CardUpdatedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/CardDeletedEvent.cs`
**Pattern:** Follow `create-aggregate` skill. Reference `Reflekt.Domain/Participants/` for the partial-class split and event shape. Reference `Reflekt.Domain/Sprints/SprintState.cs` for enum file pattern.
**Feature-specific inputs:**
- Factory method name: `Create`
- Parameters: `string text, int sprintId, int participantId, ColumnType columnType, CardType cardType, string? assigneeName`
- Guards: see §3.2 — text not empty, column/type combination, assigneeName conditional on CardType
- Timestamps: `CreatedAt = DateTime.UtcNow`, `UpdatedAt = CreatedAt`
- Events raised: `CardCreatedEvent(this)` on create, `CardUpdatedEvent(this)` on update, `CardDeletedEvent(this)` on mark-deleted
**Dependencies:** None.

### Step 2: EF config + DbSet + repository + DI

**What:** Add persistence for the Card aggregate.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/CardRepository.cs`
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `public DbSet<Card> Cards => Set<Card>();`
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `services.AddScoped<CardRepository>();`

**CardRepository** extends `Repository<Card>`:
- Inherits `AddAsync`, `SaveChangesAsync`, `GetByIdAsync` from base.
- Custom methods:
  - `GetBySprintIdAsync(int sprintId, CancellationToken ct)` — returns `List<Card>` ordered by `CreatedAt` descending. Used by ListCards endpoint.
  - `Remove(Card card)` — wraps `Db.Cards.Remove(card)`. Used by DeleteCard endpoint.

**Pattern:** Follow `persistence-patterns` skill. Reference `ParticipantRepository.cs` and `ParticipantConfiguration.cs`.
**Details:** See §4.2 for FK cascade/restrict decisions. SprintId cascades, ParticipantId restricts.
**Dependencies:** Step 1.

### Step 3: EF Migration

**What:** Generate the Card migration.
**Command:**
```bash
dotnet ef migrations add AddCard -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```
**Verify:** The generated migration should only create the `Cards` table with the columns, FKs, and indexes from §4.1. No changes to existing tables.
**Verify:** `dotnet ef migrations list` shows the new migration. `dotnet run` auto-applies in Development.
**Dependencies:** Step 2.

### Step 4: Shared contracts + CreateCard endpoint

**What:** Create shared Card contracts and the CreateCard endpoint.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs`

**CardContracts.cs** — shared records:
- `CardResponse(int Id, int SprintId, int ParticipantId, string ColumnType, string CardType, string Text, string? AssigneeName, DateTime CreatedAt, DateTime UpdatedAt)` — enum values serialized as strings for the frontend.
- Static mapping helper `ToResponse(Card)` — converts enum values to their string names.

**CreateCardCommand.cs:**
- Request record: `CreateCardCommand { int SprintId, string Text, string ColumnType, string CardType, string? AssigneeName }` — `SprintId` from route, rest from body. Enum values sent as strings from the frontend, parsed in the endpoint.
- Validator: `Text` required (not empty/whitespace), max 2000 chars. `ColumnType` and `CardType` required, not empty. `AssigneeName` max 50 chars when present.

**CreateCardEndpoint.cs:**
- Auth: `[Authorize(Policy = Policies.Participant)]` — guest token required.
- `[HttpPost("/api/sprints/{sprintId}/cards")]`, `[Tags("Cards")]`
- `Endpoint<CreateCardCommand, CardResponse>`
- Injects: `SprintRepository`, `CardRepository`

**Handler flow:**
1. Read `participantId` and `sprintId` from claims (set by GuestTokenAuthenticationHandler).
2. Verify claim's `sprintId` matches route's `SprintId` → 401 if mismatch.
3. Parse `ColumnType` and `CardType` from string to enum → 400 if invalid.
4. Load sprint: `sprintRepository.GetByIdAsync(command.SprintId, ct)` → 404 if null.
5. Check `sprint.State == SprintState.Closed` → 409 if closed (message: "Sprint is closed").
6. Create aggregate: `Card.Create(command.Text, command.SprintId, participantId, columnType, cardType, command.AssigneeName)` — factory guards handle column/type validation, will throw on invalid combination.
7. Persist: `cardRepository.AddAsync` + `cardRepository.SaveChangesAsync`.
8. Respond: 201 with `CardResponse`.

**Pattern:** Follow `create-feature` skill. Reference `JoinSprintEndpoint.cs` for handler structure with guest token claims.
**Dependencies:** Steps 1–3.

### Step 5: EditCard endpoint

**What:** Create the EditCard endpoint with ownership check.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs`

**EditCardCommand.cs:**
- Request record: `EditCardCommand { int SprintId, int CardId, string Text, string? AssigneeName }` — `SprintId` and `CardId` from route, rest from body.
- Validator: `Text` required, max 2000 chars. `AssigneeName` max 50 chars when present.

**EditCardEndpoint.cs:**
- Auth: `[Authorize(Policy = Policies.OwnerOrScrumMaster)]` — guest token or SM cookie.
- `[HttpPut("/api/sprints/{sprintId}/cards/{cardId}")]`, `[Tags("Cards")]`
- `Endpoint<EditCardCommand, CardResponse>`
- Injects: `SprintRepository`, `TeamRepository`, `CardRepository`

**Handler flow:**
1. Load card: `cardRepository.GetByIdAsync(command.CardId, ct)` → 404 if null.
2. Verify card belongs to this sprint: `card.SprintId == command.SprintId` → 404 if mismatch.
3. Load sprint: `sprintRepository.GetByIdAsync(command.SprintId, ct)` → 404 if null.
4. Check `sprint.State == SprintState.Closed` → 409 if closed.
5. **Ownership check** — follow the same dual-auth pattern as `GetParticipantsEndpoint`:
   - If caller has a `participantId` claim (guest token): check `card.ParticipantId == claimParticipantId` → 403 if not the author.
   - Else (cookie auth): load team via `sprint.TeamId`, check `team.CreatorEmail` matches caller's email (case-insensitive) → 403 if not SM.
6. `card.Update(command.Text, command.AssigneeName)` — domain method handles validation and raises event.
7. `cardRepository.SaveChangesAsync(ct)`.
8. Respond: 200 with `CardResponse`.

**Pattern:** Follow `create-feature` skill. Reference `GetParticipantsEndpoint.cs` for the dual-auth ownership check pattern.
**Dependencies:** Steps 1–3.

### Step 6: DeleteCard endpoint

**What:** Create the DeleteCard endpoint with ownership check.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs`

**DeleteCardEndpoint.cs:**
- Auth: `[Authorize(Policy = Policies.OwnerOrScrumMaster)]`
- `[HttpDelete("/api/sprints/{sprintId}/cards/{cardId}")]`, `[Tags("Cards")]`
- `Endpoint<DeleteCardRequest, EmptyResponse>` (or `EndpointWithoutResponse` — Developer picks the FastEndpoints pattern for 204).
- Request record: `DeleteCardRequest { int SprintId, int CardId }` — both from route. Can be defined inline in this file (no separate command file needed — no body, no validator).
- Injects: `SprintRepository`, `TeamRepository`, `CardRepository`

**Handler flow:**
1. Load card: `cardRepository.GetByIdAsync(command.CardId, ct)` → 404 if null.
2. Verify card belongs to this sprint: `card.SprintId == command.SprintId` → 404 if mismatch.
3. Load sprint: `sprintRepository.GetByIdAsync(command.SprintId, ct)` → 404 if null.
4. Check `sprint.State == SprintState.Closed` → 409 if closed.
5. **Ownership check** — same dual-auth pattern as EditCard (Step 5).
6. `card.MarkDeleted()` — raises `CardDeletedEvent(this)`.
7. `cardRepository.Remove(card)` — marks entity for EF deletion.
8. `cardRepository.SaveChangesAsync(ct)` — interceptor publishes CardDeletedEvent, then EF executes DELETE.
9. Respond: 204 No Content.

**Important:** The `MarkDeleted()` → `Remove()` → `SaveChangesAsync()` sequence ensures the domain event is raised before EF removes the entity. The interceptor collects events from all tracked entities (including those with state `Deleted`), clears them, saves, then publishes. If this doesn't work (interceptor skips deleted entities), flag it — see §2 judgment call.

**Dependencies:** Steps 1–3.

### Step 7: ListCards endpoint

**What:** Create the ListCards read endpoint.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs`

**ListCardsEndpoint.cs:**
- Auth: `[Authorize(Policy = Policies.OwnerOrScrumMaster)]` — guest token or SM cookie.
- `[HttpGet("/api/sprints/{sprintId}/cards")]`, `[Tags("Cards")]`
- Request record: `ListCardsRequest { int SprintId }` — from route. Can be defined inline.
- `Endpoint<ListCardsRequest, List<CardResponse>>`
- Injects: `SprintRepository`, `TeamRepository`, `CardRepository`

**Handler flow:**
1. Load sprint: `sprintRepository.GetByIdAsync(request.SprintId, ct)` → 404 if null.
2. **Auth check** — same dual-auth pattern as `GetParticipantsEndpoint`:
   - If guest token: verify claim's `sprintId` matches route `SprintId` → 401 if mismatch.
   - If cookie: verify caller is SM of the sprint's team → 403 if not.
3. Load cards: `cardRepository.GetBySprintIdAsync(request.SprintId, ct)`.
4. Respond: 200 with `List<CardResponse>` (mapped via `CardMappings.ToResponse`).

**Pattern:** Follow `create-feature` skill. Reference `GetParticipantsEndpoint.cs` for the dual-auth read pattern.
**Dependencies:** Steps 1–3.

### Step 8: SignalR event handlers

**What:** Create three SignalR broadcasters for card domain events.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Events/CardCreatedEventHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Events/CardUpdatedEventHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Events/CardDeletedEventHandler.cs`

Each handler:
- Implements `IEventHandler<CardXxxEvent>`.
- Injects `IHubContext<SprintHub>`.
- `HandleAsync`: sends the named event to SignalR group `sprint:{ev.Card.SprintId}`.

**Payloads:**
- `CardCreated` → full card response object (use `CardMappings.ToResponse(ev.Card)`).
- `CardUpdated` → full card response object.
- `CardDeleted` → `new { id = ev.Card.Id }` (just the card ID — clients remove by ID).

**Pattern:** Follow `ParticipantJoinedEventHandler.cs` exactly — same structure, different event and payload.
**Dependencies:** Steps 1, 4.

### Step 9: Seed data update

**What:** Add seeded cards to the existing seeded sprint.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs`

**Seed logic (after participant seeding):**
1. Load seeded participants for the sprint.
2. If `!db.Cards.Any(c => c.SprintId == sprint.Id)`: create a handful of cards using `Card.Create(...)`, clear domain events (same pattern as participant seeding), add, save.
3. Suggested seed data:
   - 2 notes in WentWell (by different participants)
   - 2 notes in DidntGoWell (by different participants)
   - 1 action item in ActionItems (with an assignee name from the participants)
4. Idempotent.

**Dependencies:** Steps 1–3.

### Step 10: Frontend — card store + SignalR card events

**What:** Create the Pinia card store and wire card SignalR events.
**Files to create:**
- `web/reflekt-web/src/stores/card.ts`
**Files to modify:**
- `web/reflekt-web/src/composables/useSprintHub.ts` — add card event listeners.

**card.ts — Pinia store:**
```
interface Card {
  id: number
  sprintId: number
  participantId: number
  columnType: string   // "WentWell" | "DidntGoWell" | "ActionItems"
  cardType: string     // "Note" | "ActionItem"
  text: string
  assigneeName: string | null
  createdAt: string
  updatedAt: string
}

state: {
  cards: Card[]
}

getters: {
  wentWell: Card[]        // filter by columnType
  didntGoWell: Card[]
  actionItems: Card[]
}

actions: {
  fetchCards(sprintId)           // GET  /api/sprints/{sprintId}/cards
  createCard(sprintId, payload)  // POST /api/sprints/{sprintId}/cards
  editCard(sprintId, cardId, payload)  // PUT  /api/sprints/{sprintId}/cards/{cardId}
  deleteCard(sprintId, cardId)   // DELETE /api/sprints/{sprintId}/cards/{cardId}
}
```

All API calls include `X-Guest-Token` header from the guest store. Edit and delete also include `credentials: 'include'` for SM cookie fallback.

**useSprintHub.ts — add card event listeners:**
- `CardCreated`: push card to `cardStore.cards` (deduplicate by ID — the creator already has it from their optimistic API response).
- `CardUpdated`: find card in `cardStore.cards` by ID, replace with updated data.
- `CardDeleted`: remove card from `cardStore.cards` by ID.

**Pattern:** Reference `stores/participant.ts` for store shape and API call patterns. Reference existing SignalR event handlers in `useSprintHub.ts` for the listener pattern.
**Dependencies:** Steps 4–7 (API endpoints).

### Step 11: Frontend — RetroRoomView card columns UI

**What:** Replace the "coming soon" placeholder with the three-column card board.
**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue`

**UI structure:**
1. Three-column layout: **Went Well** | **Didn't Go Well** | **Action Items**
2. Each column header shows the column name.
3. Each column has a card input area:
   - WentWell / DidntGoWell: text input + "Add" button. Creates a Note.
   - ActionItems: text input + assignee dropdown (populated from `participantStore.participants`) + "Add" button. Creates an ActionItem.
4. Cards displayed within each column, ordered by creation time (newest first — already ordered by API).
5. Each card shows:
   - Text content.
   - For action items: assignee name as a badge/chip.
   - Edit button (pencil icon) and delete button (trash icon) — **only visible** if `card.participantId === participantStore.currentParticipant?.id` (own card) OR `authStore.user !== null` (SM is logged in).
6. Edit flow: clicking edit makes the text editable inline (or opens a small form). Save/cancel buttons. For action items, assignee is also editable.
7. Delete flow: clicking delete removes the card (with confirmation if desired — Developer picks).

**Optimistic UI:**
- CreateCard: add the card to the store immediately, fire the API call. On failure, remove and show a toast.
- EditCard: update in store immediately, fire API call. On failure, revert and show a toast.
- DeleteCard: remove from store immediately, fire API call. On failure, re-add and show a toast.

**Data loading:** On `enterRoom()` (after join/rejoin), call `cardStore.fetchCards(sprintId)` alongside `participantStore.fetchParticipants`.

**Styling:** Tailwind. Match existing aesthetic (white cards, subtle borders, blue accents). Cards as small boxes within each column.

**Pattern:** Reference existing `RetroRoomView.vue` structure. The participant list section stays; the "coming soon" div is replaced.
**Dependencies:** Step 10.

### Step 12: Unit tests

**What:** Test the Card aggregate domain logic.
**Files to create:**
- `tests/Reflekt.UnitTests/Cards/CardTests.cs`

**Test cases:**

*Create factory:*
1. `Create_Note_SetsAllPropertiesCorrectly` — verify SprintId, ParticipantId, ColumnType, CardType=Note, Text, AssigneeName=null, timestamps.
2. `Create_ActionItem_SetsAllPropertiesCorrectly` — verify CardType=ActionItem, AssigneeName set.
3. `Create_Note_RaisesCardCreatedEvent` — verify DomainEvents contains one CardCreatedEvent.
4. `Create_WithEmptyText_Throws` — ArgumentException.
5. `Create_ActionItem_WithMissingAssigneeName_Throws`.
6. `Create_ActionItem_InWrongColumn_Throws` — ActionItem in WentWell → throws.
7. `Create_Note_InActionItemsColumn_Throws` — Note in ActionItems → throws.
8. `Create_Note_WithAssigneeName_Throws` — Note shouldn't have an assignee.

*Update method:*
9. `Update_ChangesTextAndTimestamp` — verify text updated, UpdatedAt changed, CreatedAt unchanged.
10. `Update_ActionItem_ChangesAssigneeName`.
11. `Update_RaisesCardUpdatedEvent`.
12. `Update_WithEmptyText_Throws`.

*MarkDeleted method:*
13. `MarkDeleted_RaisesCardDeletedEvent`.

**Pattern:** Reference `tests/Reflekt.UnitTests/Participants/ParticipantTests.cs`.
**Dependencies:** Step 1.

### Step 13: Integration tests

**What:** Test Card endpoints end-to-end.
**Files to create:**
- `tests/Reflekt.IntegrationTests/CardEndpointTests.cs`

**Test setup:**
- Same `WebApplicationFactory<Program>` pattern as `ParticipantEndpointTests.cs`.
- Each test creates a team, starts a sprint, and joins a participant first (via API calls).
- For guest-token-authenticated requests: capture `guestToken` from JoinSprint response, include as `X-Guest-Token` header.
- For SM requests: use TestAuthHandler cookie scheme.

**Test cases:**

*CreateCard:*
1. `CreateCard_Note_Returns201` — valid note in WentWell → 201, response has all fields.
2. `CreateCard_ActionItem_Returns201` — valid action item with assigneeName → 201.
3. `CreateCard_EmptyText_Returns400`.
4. `CreateCard_TextTooLong_Returns400` — 2001+ chars.
5. `CreateCard_ActionItem_NoAssignee_Returns400`.
6. `CreateCard_ActionItem_WrongColumn_Returns400` — ActionItem in WentWell.
7. `CreateCard_Note_InActionItemsColumn_Returns400`.
8. `CreateCard_NoGuestToken_Returns401`.
9. `CreateCard_WrongSprint_Returns401` — token from sprint A, create in sprint B.
10. `CreateCard_ClosedSprint_Returns409`.
11. `CreateCard_NonexistentSprint_Returns404`.

*EditCard:*
12. `EditCard_ByAuthor_Returns200` — author edits own card.
13. `EditCard_ByScrumMaster_Returns200` — SM edits another's card.
14. `EditCard_ByOtherParticipant_Returns403` — participant B edits A's card.
15. `EditCard_ClosedSprint_Returns409`.
16. `EditCard_NonexistentCard_Returns404`.
17. `EditCard_UpdatesTimestamp` — verify UpdatedAt changed.

*DeleteCard:*
18. `DeleteCard_ByAuthor_Returns204`.
19. `DeleteCard_ByScrumMaster_Returns204`.
20. `DeleteCard_ByOtherParticipant_Returns403`.
21. `DeleteCard_ClosedSprint_Returns409`.
22. `DeleteCard_NonexistentCard_Returns404`.

*ListCards:*
23. `ListCards_ReturnsAllCardsInSprint` — create 3 cards, list returns 3.
24. `ListCards_OrderedByCreatedAtDescending`.
25. `ListCards_NoAuth_Returns401`.
26. `ListCards_NonexistentSprint_Returns404`.

**Pattern:** Reference `ParticipantEndpointTests.cs`.
**Dependencies:** Steps 4–7 and all prior steps.

## 6. Migration notes

Next EF Core migration after Participants. Command:
```bash
dotnet ef migrations add AddCard -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

The migration should only add the `Cards` table. If it includes changes to existing tables, review carefully.

Auto-applied on startup in Development. Delete `data/reflekt.db` before first run if migration conflicts arise.

## 7. Testing strategy

- **Unit tests** (Step 12) cover the factory method guards (column/type validation, text/assignee enforcement), update method, mark-deleted event, and all edge cases.
- **Integration tests** (Step 13) cover the full HTTP pipeline: CRUD operations, ownership enforcement (author vs SM vs other participant), sprint-state gating, and auth requirements.
- **SignalR is not integration-tested** — event handlers push to groups; verifying they don't throw is sufficient. The frontend manual test covers the round-trip.
- **Optimistic UI** is tested manually in the browser.
- **No frontend tests** — not in scope for v1.

## 8. Verification — acceptance criteria

Cards is done when all of these pass:

1. `dotnet build Reflekt.slnx` — zero errors, zero warnings.
2. `dotnet test` — all tests pass (existing + new unit + new integration).
3. `dotnet run` in Development:
   - `data/reflekt.db` has `Cards` table with correct schema.
   - Seed data creates sample cards across all three columns.
4. Manual API flow (Scalar or curl):
   - Join a sprint as a participant (get guest token).
   - `POST /api/sprints/{sprintId}/cards` with `{ "text": "Great teamwork", "columnType": "WentWell", "cardType": "Note" }` → 201.
   - `POST /api/sprints/{sprintId}/cards` with `{ "text": "Update docs", "columnType": "ActionItems", "cardType": "ActionItem", "assigneeName": "Ana" }` → 201.
   - `GET /api/sprints/{sprintId}/cards` → lists both cards, newest first.
   - `PUT /api/sprints/{sprintId}/cards/{cardId}` with `{ "text": "Updated text" }` → 200 (as author).
   - `DELETE /api/sprints/{sprintId}/cards/{cardId}` → 204 (as author).
   - Create card on Closed sprint → 409.
   - Edit someone else's card as participant → 403.
   - Edit someone else's card as SM → 200.
   - ActionItem in WentWell → 400.
   - Note in ActionItems → 400.
5. Frontend flow (`npm run dev`):
   - Navigate to `/sprint/{sprintId}`, join.
   - See three-column board: Went Well, Didn't Go Well, Action Items.
   - Add a note to Went Well — appears immediately (optimistic).
   - Add an action item with assignee — appears in Action Items column with assignee badge.
   - Edit a card inline — text updates, UpdatedAt changes.
   - Delete a card — disappears.
   - Open second browser tab, join as second participant — first tab sees new cards appear in real-time (SignalR).
   - Second participant cannot see edit/delete buttons on first participant's cards.
6. Existing Participant, Sprint, and Team endpoints still work — no regressions.
7. SignalR events fire correctly: CardCreated, CardUpdated, CardDeleted all push to sprint group.

## 9. Out of scope

If tempted to add any of these, stop and check the relevant feature plan:

- `Vote` entity or vote CRUD — **Votes plan**.
- `EvidenceLink` entity or URL preview fetching — **EvidenceLinks plan**.
- Report generation — **Reports plan**.
- Card drag-and-drop reordering — **not in v1**.
- Moving cards between columns — **not in v1** (column immutable after creation).
- Converting notes to action items — **not in v1**.
- Card grouping or clustering — **not in v1**.
- Vote-based card sorting in the API — **Votes plan** (frontend display concern).

## 10. File summary

**New files:**
- `src/Services/Reflekt/Reflekt.Domain/Cards/ColumnType.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/CardType.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/CardCreatedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/CardUpdatedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/CardDeletedEvent.cs`
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/CardRepository.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Events/CardCreatedEventHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Events/CardUpdatedEventHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Events/CardDeletedEventHandler.cs`
- `web/reflekt-web/src/stores/card.ts`
- `tests/Reflekt.UnitTests/Cards/CardTests.cs`
- `tests/Reflekt.IntegrationTests/CardEndpointTests.cs`

**Modified files:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `DbSet<Card>`
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `CardRepository`
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — seed sample cards
- `web/reflekt-web/src/composables/useSprintHub.ts` — add CardCreated/Updated/Deleted listeners
- `web/reflekt-web/src/views/RetroRoomView.vue` — replace placeholder with three-column card board

**Unchanged:**
- All Auth files, Team files, Sprint files, Participant files, `Program.cs`, `Policies.cs`, `SprintHub.cs`, `appsettings*.json`.
