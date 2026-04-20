# Evidence Links Plan — Reflekt v1

**Status:** Ready for implementation
**Prerequisite:** Auth (landed), Teams (landed), Sprints (landed), Participants (landed), Cards (landed), Voting (landed)
**Prerequisite for:** Reports (reads evidence links embedded on cards)
**Companion docs:** `docs/features/EvidenceLinks.md`, `docs/architecture/v1.md`

## 0. Purpose

Attach URL previews (GitHub, Jira, generic) to retro cards. The differentiating feature of Reflekt. Participants paste a URL, the system detects the source, and enriches it asynchronously via server-side OpenGraph scraping. Jira links skip the fetch — the issue key extracted from the URL is the display. The existing OpenGraph pipeline (background service, channel, SSRF-guarded fetcher, AngleSharp parser) is already scaffolded; this plan wires it into the domain model and completes the feature end-to-end.

**Non-goals for Evidence Links:**
- No Jira REST API calls — issue key extraction from URL only.
- No GitHub REST API calls — OpenGraph scrape only.
- No full preview card mode — inline only (favicon + title + issue key chip).
- No editing evidence links — attach or detach, not modify.
- No automatic retry on fetch failure.
- No report generation — that's the Reports feature.

If you find yourself calling any external API with authentication, or building a card-mode preview layout, **stop** — that belongs in a different plan or v2.

## 1. Relevant skills

Read each skill file before applying it — this plan only names them.

- **`domain-patterns`** — for EvidenceLink entity, new enums, new domain event definitions.
- **`create-feature`** — for each endpoint (AttachEvidenceLink, DetachEvidenceLink).
- **`persistence-patterns`** — for `EvidenceLinkConfiguration`, repository updates.
- **`authorization-patterns`** — reference for auth policy usage on endpoints (OwnerOrScrumMaster).

**No `create-aggregate` skill** — EvidenceLink is a child entity of Card, not an aggregate root.

**Missing skill:** No skill for BackgroundService/Channel pipeline patterns. The OpenGraph pipeline is already scaffolded, so this plan modifies existing code. Log in lessons.md.

**Pattern references (actual codebase):**
- Card aggregate split: `Reflekt.Domain/Cards/Card.cs` (properties) + `Reflekt.Domain/Cards/Behaviors/Card.cs` (factory + methods)
- Domain event: `Reflekt.Domain/Cards/Events/CardCreatedEvent.cs`
- Enum: `Reflekt.Domain/Cards/CardType.cs`
- Endpoint (create, guest auth): `Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs`
- Endpoint (mutation, dual auth + ownership): `Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs`
- Shared contracts: `Reflekt.API/Features/Cards/CardContracts.cs`
- Repository: `Reflekt.Persistence/Repositories/CardRepository.cs`
- Entity config: `Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs`
- Event handler (SignalR broadcaster): `Reflekt.API/Events/CardCreatedEventHandler.cs`
- OpenGraph pipeline: `Reflekt.API/OpenGraph/` (all four files)
- Frontend store: `web/reflekt-web/src/stores/card.ts`
- Frontend SignalR: `web/reflekt-web/src/composables/useSprintHub.ts`
- Frontend view: `web/reflekt-web/src/views/RetroRoomView.vue`
- Unit test: `tests/Reflekt.UnitTests/Cards/CardTests.cs`
- Integration test: `tests/Reflekt.IntegrationTests/CardEndpointTests.cs`

## 2. Judgment calls flagged for the Developer

Small decisions the Developer can make without coming back:

- **Evidence link UI placement.** How to surface the "add link" action on a card — a link icon button, a small "Add evidence" text link, or an input always visible. Developer picks. Recommend a small link icon that expands an input on click.
- **Favicon URL construction for pending/failed links.** The spec says "domain favicon." Constructing `https://{domain}/favicon.ico` from the URL is a reasonable heuristic. If a more reliable approach exists (e.g., Google's favicon service), Developer can use it.
- **OpenGraphParser as separate class vs inline.** The existing `OpenGraphFetcher` already inlines the parsing. Keep it there.

Decisions that should come back if they feel wrong mid-implementation:

- If loading the Card aggregate (with `.Include(c => c.EvidenceLinks)`) in the background service creates issues with the DomainEventPublishingInterceptor or concurrency — flag it.
- If the `FetchJob` record needs additional fields beyond what's specified — flag it rather than silently adding them.

## 3. Domain model changes

### 3.1 Enums

**`src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLinkSource.cs`:**
```
EvidenceLinkSource { GitHub, Jira, Generic }
```

**`src/Services/Reflekt/Reflekt.Domain/Cards/FetchStatus.cs`:**
```
FetchStatus { Pending, Ok, Failed }
```

Follow the same file-per-enum pattern as `CardType.cs`.

### 3.2 EvidenceLink entity — child entity of Card

**`src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLink.cs`:**

- Extends `Entity` (not `AggregateRoot` — it has no domain events of its own).
- Private parameterless constructor for EF Core.
- Properties (all `private set`; Card aggregate manages mutations):
  - `int CardId` — FK to parent Card.
  - `string Url` — the original URL. Max 2048 chars.
  - `EvidenceLinkSource Source` — auto-detected.
  - `string? Title` — max 500 chars, populated by fetch.
  - `string? Description` — max 1000 chars, populated by fetch.
  - `string? IconUrl` — populated by fetch.
  - `string? ImageUrl` — populated by fetch.
  - `string? ExternalKey` — max 50 chars, e.g. `PROJ-123` for Jira.
  - `DateTime? FetchedAt` — UTC, stamped on fetch completion.
  - `FetchStatus FetchStatus` — initial state depends on source.

**Static factory:** `Create(int cardId, string url, EvidenceLinkSource source, string? externalKey)`:
- Sets all properties.
- For `source == Jira`: `FetchStatus = Ok`, `FetchedAt = DateTime.UtcNow` (nothing to fetch).
- For `source == GitHub` or `Generic`: `FetchStatus = Pending`, `FetchedAt = null`.

**Internal method:** `UpdateMetadata(string? title, string? description, string? iconUrl, string? imageUrl, FetchStatus status)`:
- Sets metadata fields. Sets `FetchedAt = DateTime.UtcNow`.
- Internal visibility — only callable from within the Domain project (by the Card aggregate).

Skill: `domain-patterns`

### 3.3 Card aggregate extensions

**Properties file — `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs`:**

Add the evidence links collection:
```
private readonly List<EvidenceLink> _evidenceLinks = [];
public IReadOnlyList<EvidenceLink> EvidenceLinks => _evidenceLinks.AsReadOnly();
```

**Behaviors file — `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs`:**

Three new methods:

**`AttachEvidenceLink(string url, EvidenceLinkSource source, string? externalKey)`** — instance method:
- Guard: `url` must not be null/whitespace.
- Creates `EvidenceLink.Create(this.Id, url, source, externalKey)`.
- Adds to `_evidenceLinks`.
- Raises `EvidenceLinkAttachedEvent(this, evidenceLink)`.
- Returns the created `EvidenceLink` (endpoint needs its ID for the response).

**`UpdateEvidenceLinkMetadata(int evidenceLinkId, string? title, string? description, string? iconUrl, string? imageUrl, FetchStatus status)`** — instance method:
- Finds the evidence link in `_evidenceLinks` by ID. Throws `InvalidOperationException` if not found.
- Calls `evidenceLink.UpdateMetadata(title, description, iconUrl, imageUrl, status)`.
- Raises `EvidenceLinkFetchedEvent(this, evidenceLink)`.

**`DetachEvidenceLink(int evidenceLinkId)`** — instance method:
- Finds the evidence link in `_evidenceLinks` by ID. Throws `InvalidOperationException` if not found.
- Removes from `_evidenceLinks`.
- Raises `EvidenceLinkDetachedEvent(this, evidenceLinkId)`.

### 3.4 Domain events

Three events in `src/Services/Reflekt/Reflekt.Domain/Cards/Events/`:

```
EvidenceLinkAttachedEvent(Card Card, EvidenceLink EvidenceLink) : IDomainEvent
EvidenceLinkFetchedEvent(Card Card, EvidenceLink EvidenceLink) : IDomainEvent
EvidenceLinkDetachedEvent(Card Card, int EvidenceLinkId) : IDomainEvent
```

Follow `CardCreatedEvent.cs` pattern. Events carry the aggregate reference (per CLAUDE.md convention). `EvidenceLinkAttachedEvent` also carries the specific link (needed by the fetch pipeline to build the job). `EvidenceLinkDetachedEvent` carries just the ID (the link is removed from the collection by the time the event fires).

Skill: `domain-patterns`

## 4. Data model changes

### 4.1 Table: `EvidenceLinks`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `INTEGER` | PK, auto-increment (from `Entity` base) |
| `CardId` | `INTEGER` | Required, FK to `Cards.Id` |
| `Url` | `TEXT` | Required, max 2048 |
| `Source` | `INTEGER` | Required (enum stored as int) |
| `Title` | `TEXT` | Nullable, max 500 |
| `Description` | `TEXT` | Nullable, max 1000 |
| `IconUrl` | `TEXT` | Nullable |
| `ImageUrl` | `TEXT` | Nullable |
| `ExternalKey` | `TEXT` | Nullable, max 50 |
| `FetchedAt` | `TEXT` (SQLite datetime) | Nullable |
| `FetchStatus` | `INTEGER` | Required (enum stored as int) |

FK: `CardId → Cards.Id`, **cascade delete** (spec: deleting a card deletes all its evidence links).
Index on `CardId` (primary access pattern — load links for a card).

### 4.2 EF Configuration

**`src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/EvidenceLinkConfiguration.cs`:**

`EntityConfiguration<EvidenceLink>`:
- `ToTable("EvidenceLinks")`
- `Property(e => e.CardId).IsRequired()`
- `Property(e => e.Url).IsRequired().HasMaxLength(2048)`
- `Property(e => e.Source).IsRequired()`
- `Property(e => e.Title).HasMaxLength(500)`
- `Property(e => e.Description).HasMaxLength(1000)`
- `Property(e => e.ExternalKey).HasMaxLength(50)`
- `Property(e => e.FetchStatus).IsRequired()`
- `HasIndex(e => e.CardId)`

**`src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs` — modify:**

Add the Card → EvidenceLinks relationship via backing field:
```
builder.HasMany(c => c.EvidenceLinks)
    .WithOne()
    .HasForeignKey(e => e.CardId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Navigation(c => c.EvidenceLinks)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

The `UsePropertyAccessMode(PropertyAccessMode.Field)` tells EF Core to populate the `_evidenceLinks` backing field directly, which the `IReadOnlyList` property exposes.

Skill: `persistence-patterns`

## 5. Implementation steps

### Step 1: EvidenceLink entity + enums + domain events

**What:** Create the EvidenceLink child entity with its supporting types.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLinkSource.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/FetchStatus.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLink.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/EvidenceLinkAttachedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/EvidenceLinkFetchedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/EvidenceLinkDetachedEvent.cs`
**Pattern:** Reference `CardType.cs` for enums. Reference `Entity` base class in `BuildingBlocks.Domain/Entity.cs`. Reference `CardCreatedEvent.cs` for event shape.
**Feature-specific inputs:**
- EvidenceLink extends `Entity` (not `AggregateRoot`). Private parameterless constructor.
- Factory: `Create(int cardId, string url, EvidenceLinkSource source, string? externalKey)` — sets FetchStatus based on source (Jira=Ok, others=Pending).
- Internal method: `UpdateMetadata(...)` — sets metadata + FetchedAt + status.
- All properties have `private set` (Card aggregate manages mutations, EF Core hydrates via backing fields).
**Dependencies:** None.

### Step 2: Extend Card aggregate

**What:** Add the evidence links collection and domain methods to the Card aggregate.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs` — add `_evidenceLinks` backing field + `EvidenceLinks` read-only property.
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs` — add `AttachEvidenceLink`, `UpdateEvidenceLinkMetadata`, `DetachEvidenceLink` methods per §3.3.
**Pattern:** Follow existing Card partial-class split. Reference `Card.Update()` for the method pattern.
**Dependencies:** Step 1.

### Step 3: EF configuration + repository update + migration

**What:** Add persistence for EvidenceLink and update Card queries to include evidence links.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/EvidenceLinkConfiguration.cs`
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs` — add `HasMany(c => c.EvidenceLinks)` relationship + backing field access mode per §4.2.
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/CardRepository.cs` — two changes:
  1. Override `GetByIdAsync`: use `Db.Cards.Include(c => c.EvidenceLinks).FirstOrDefaultAsync(c => c.Id == id, ct)` instead of base `FindAsync` (which doesn't support Include).
  2. Update `GetBySprintIdAsync`: add `.Include(c => c.EvidenceLinks)` to the existing query.
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `public DbSet<EvidenceLink> EvidenceLinks => Set<EvidenceLink>();` (needed for the background service to query by EvidenceLinkId if needed, and for EF to discover the entity type).

**Migration command:**
```bash
dotnet ef migrations add AddEvidenceLink -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

**Verify:** The generated migration should only create the `EvidenceLinks` table. No changes to the `Cards` table (the relationship is tracked via the FK on EvidenceLinks, not a column on Cards).

**Pattern:** Reference `CardConfiguration.cs` for entity config. Reference `CardRepository.cs` for repository pattern.
Skill: `persistence-patterns`
**Dependencies:** Steps 1–2.

### Step 4: Update CardContracts

**What:** Add EvidenceLinkResponse and embed evidence links in CardResponse.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs`

**Add `EvidenceLinkResponse` record:**
```
EvidenceLinkResponse(
    int Id, int CardId, string Url, string Source,
    string? Title, string? Description, string? IconUrl, string? ImageUrl,
    string? ExternalKey, DateTime? FetchedAt, string FetchStatus)
```

Enum values serialized as strings (same pattern as `ColumnType`/`CardType` in `CardResponse`).

**Update `CardResponse`:** Add a `List<EvidenceLinkResponse> EvidenceLinks` parameter (with default `[]` to avoid breaking existing code that doesn't pass it).

**Update `CardMappings.ToResponse`:** Map `card.EvidenceLinks` to `List<EvidenceLinkResponse>`. Handle the case where `EvidenceLinks` is empty (newly created cards, events where Include wasn't used). Add a static `ToResponse(EvidenceLink)` helper.

**Impact on existing code:** The event handlers (`CardCreatedEventHandler`, `CardUpdatedEventHandler`) call `CardMappings.ToResponse(ev.Card)`. After this change, they'll include the evidence links list (empty for new cards, populated for cards loaded with Include). Existing tests that check response shape may need updating — this is expected.

**Dependencies:** Steps 1–3.

### Step 5: Source detection utility + config

**What:** Create a utility to detect EvidenceLinkSource from a URL and extract the Jira issue key.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/EvidenceLinkSourceDetector.cs`
**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/appsettings.json` — add `"EvidenceLinks": { "JiraDomain": null }` section.
- `src/Services/Reflekt/Reflekt.API/appsettings.Development.json` — add example: `"EvidenceLinks": { "JiraDomain": "your-org.atlassian.net" }`.

**EvidenceLinkSourceDetector — static class:**

```
static (EvidenceLinkSource Source, string? ExternalKey) Detect(string url, string? jiraDomain)
```

Detection rules (from spec §6.1):
1. Parse URL. If host is `github.com` → `(GitHub, null)`.
2. If `jiraDomain` is configured and host matches → `(Jira, extractedKey)`. Extract key from the last path segment matching the pattern `{PROJECT}-{number}` (e.g., `/browse/PROJ-123` → `PROJ-123`).
3. If URL path matches `*/browse/{KEY}` regardless of domain → `(Jira, extractedKey)`. This is the fallback when no Jira domain is configured.
4. Otherwise → `(Generic, null)`.

Jira key regex pattern: `[A-Z][A-Z0-9]+-\d+`

The endpoint reads `jiraDomain` from `IConfiguration["EvidenceLinks:JiraDomain"]` and passes it to the detector. No injected service needed.

**Dependencies:** None (can be implemented in parallel with domain steps).

### Step 6: AttachEvidenceLink endpoint

**What:** Create the endpoint to attach an evidence link to a card.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkEndpoint.cs`

**AttachEvidenceLinkCommand.cs:**
- Request record: `AttachEvidenceLinkCommand { int SprintId, int CardId, string Url }` — `SprintId` and `CardId` from route, `Url` from body.
- Validator: `Url` required, not empty/whitespace, max 2048 chars. Must be a valid absolute URI with HTTP or HTTPS scheme.

**AttachEvidenceLinkEndpoint.cs:**
- Auth: `[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]` — guest token or SM cookie.
- `[HttpPost("/api/sprints/{sprintId}/cards/{cardId}/evidence-links")]`, `[Tags("EvidenceLinks")]`
- `Endpoint<AttachEvidenceLinkCommand, EvidenceLinkResponse>`
- Injects: `SprintRepository`, `TeamRepository`, `CardRepository`, `IConfiguration`

**Handler flow:**
1. Load card: `cardRepository.GetByIdAsync(command.CardId, ct)` → 404 if null.
2. Verify card belongs to this sprint: `card.SprintId == command.SprintId` → 404 if mismatch.
3. Load sprint: `sprintRepository.GetByIdAsync(command.SprintId, ct)` → 404 if null.
4. Check `sprint.State == SprintState.Closed` → 409 if closed.
5. **Ownership check** — same dual-auth pattern as `EditCardEndpoint.cs`:
   - If `participantId` claim exists: check `card.ParticipantId == claimParticipantId` → 403 if not the author.
   - Else (cookie auth): verify SM via team creator email → 403 if not SM.
6. Detect source: `EvidenceLinkSourceDetector.Detect(command.Url, configuration["EvidenceLinks:JiraDomain"])`.
7. `var link = card.AttachEvidenceLink(command.Url, source, externalKey)` — domain method handles creation and raises event.
8. `cardRepository.SaveChangesAsync(ct)` — interceptor publishes `EvidenceLinkAttachedEvent` → handler enqueues fetch job (if Pending) + pushes SignalR.
9. Respond: 201 with `EvidenceLinkResponse`.

**Pattern:** Follow `create-feature` skill. Reference `EditCardEndpoint.cs` for the ownership check pattern.
**Dependencies:** Steps 1–5.

### Step 7: DetachEvidenceLink endpoint

**What:** Create the endpoint to remove an evidence link from a card.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/DetachEvidenceLinkEndpoint.cs`

**DetachEvidenceLinkEndpoint.cs:**
- Auth: `[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]`
- `[HttpDelete("/api/sprints/{sprintId}/cards/{cardId}/evidence-links/{evidenceLinkId}")]`, `[Tags("EvidenceLinks")]`
- Request record: `DetachEvidenceLinkRequest { int SprintId, int CardId, int EvidenceLinkId }` — all from route. Defined inline (no separate command file — no body, no validator beyond route params).
- Injects: `SprintRepository`, `TeamRepository`, `CardRepository`

**Handler flow:**
1. Load card: `cardRepository.GetByIdAsync(command.CardId, ct)` → 404 if null (Include loads evidence links).
2. Verify card belongs to this sprint: `card.SprintId == command.SprintId` → 404 if mismatch.
3. Verify evidence link exists on this card: `card.EvidenceLinks.Any(e => e.Id == command.EvidenceLinkId)` → 404 if not found.
4. Load sprint: `sprintRepository.GetByIdAsync(command.SprintId, ct)` → 404 if null.
5. Check `sprint.State == SprintState.Closed` → 409 if closed.
6. **Ownership check** — same dual-auth pattern as AttachEvidenceLink (Step 6).
7. `card.DetachEvidenceLink(command.EvidenceLinkId)` — domain method removes from collection + raises event.
8. `cardRepository.SaveChangesAsync(ct)` — interceptor publishes `EvidenceLinkDetachedEvent` → handler pushes SignalR.
9. Respond: 204 No Content.

**Pattern:** Follow `create-feature` skill. Reference `DeleteCardEndpoint` for the 204 pattern.
**Dependencies:** Steps 1–5.

### Step 8: Complete OpenGraph background service

**What:** Replace the TODO log in the existing background service with actual DB update via the Card aggregate.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/OpenGraph/FetchJob.cs` — add `CardId` field: `FetchJob(int CardId, int EvidenceLinkId, string Url, int SprintId)`.
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphBackgroundService.cs` — replace the log-only handler with DB update logic.

**Updated BackgroundService logic:**

Inject `IServiceScopeFactory` (instead of just the fetcher). For each job:
1. Create a new scope: `using var scope = scopeFactory.CreateScope();`
2. Get `CardRepository` from the scope.
3. `var card = await cardRepository.GetByIdAsync(job.CardId, ct)` — loaded with evidence links via the updated repository.
4. If card is null (deleted since job was enqueued), skip silently.
5. `var result = await fetcher.FetchAsync(job.Url, ct)` — existing method, returns `FetchResult`.
6. If `result.Ok`: `card.UpdateEvidenceLinkMetadata(job.EvidenceLinkId, result.Title, result.Description, result.IconUrl, result.ImageUrl, FetchStatus.Ok)`.
7. If `!result.Ok`: `card.UpdateEvidenceLinkMetadata(job.EvidenceLinkId, null, null, null, null, FetchStatus.Failed)`.
8. `await cardRepository.SaveChangesAsync(ct)` — interceptor publishes `EvidenceLinkFetchedEvent` → handler pushes SignalR.
9. Wrap in try/catch — log errors but don't crash the service. A failed fetch should not stop the pipeline.

**Keep `OpenGraphFetcher` as-is** — it already handles SSRF, timeouts, and AngleSharp parsing correctly.
**Keep `OpenGraphConfiguration` as-is** — DI registration is already correct. Just update the `OpenGraphBackgroundService` constructor to take `IServiceScopeFactory`.

**Dependencies:** Steps 1–3 (needs Card aggregate with evidence link methods + repository with Include).

### Step 9: Event handlers

**What:** Create three event handlers that wire domain events to the fetch pipeline and SignalR.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Events/EvidenceLinkAttachedEventHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Events/EvidenceLinkFetchedEventHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Events/EvidenceLinkDetachedEventHandler.cs`

**EvidenceLinkAttachedEventHandler:**
- Implements `IEventHandler<EvidenceLinkAttachedEvent>`.
- Injects `ChannelWriter<FetchJob>`, `IHubContext<SprintHub>`.
- `HandleAsync`:
  1. Push `CardUpdated` to SignalR group `sprint:{ev.Card.SprintId}` with the full card response (so other clients see the new Pending link immediately).
  2. If `ev.EvidenceLink.FetchStatus == FetchStatus.Pending`: enqueue `new FetchJob(ev.Card.Id, ev.EvidenceLink.Id, ev.EvidenceLink.Url, ev.Card.SprintId)` to the channel.
  3. For Jira links (`FetchStatus == Ok`), skip enqueueing — they're already enriched.

**EvidenceLinkFetchedEventHandler:**
- Implements `IEventHandler<EvidenceLinkFetchedEvent>`.
- Injects `IHubContext<SprintHub>`.
- `HandleAsync`: push `EvidenceLinkFetched` (the dedicated SignalR event from the spec) to group `sprint:{ev.Card.SprintId}` with the full evidence link response object.

**EvidenceLinkDetachedEventHandler:**
- Implements `IEventHandler<EvidenceLinkDetachedEvent>`.
- Injects `IHubContext<SprintHub>`.
- `HandleAsync`: push `CardUpdated` to SignalR group `sprint:{ev.Card.SprintId}` with the full card response (evidence link is already removed from the collection).

**Pattern:** Follow `CardCreatedEventHandler.cs` exactly — same structure, different event and payload.
**Dependencies:** Steps 1–4 (needs domain events + CardMappings with evidence links).

### Step 10: Seed data

**What:** Add seeded evidence links to existing seeded cards.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs`

**Seed logic (after card seeding):**
1. Load seeded cards for the sprint.
2. If the first WentWell card has no evidence links: attach a GitHub link (e.g., `https://github.com/dotnet/aspnetcore/pull/12345`) and a generic link (e.g., `https://learn.microsoft.com/en-us/aspnet/core`).
3. Use `Card.AttachEvidenceLink(url, source, externalKey)` to create via the aggregate. Clear domain events after each (same pattern as existing seeding).
4. Save.
5. Idempotent — skip if evidence links already exist on the card.

**Note:** The seeded links will have `FetchStatus = Pending` (for GitHub/Generic). The background service will attempt to fetch them on startup. This is expected and demonstrates the pipeline working. If fetch fails (e.g., fake URLs), the links stay with `FetchStatus = Failed` — also expected.

**Dependencies:** Steps 1–3.

### Step 11: Frontend — card type update + evidence link API + SignalR

**What:** Update the card types, add evidence link API calls, and wire the new SignalR event.
**Files to modify:**
- `web/reflekt-web/src/stores/card.ts`
- `web/reflekt-web/src/composables/useSprintHub.ts`

**card.ts — update Card interface:**
```typescript
export interface EvidenceLink {
  id: number
  cardId: number
  url: string
  source: string          // "GitHub" | "Jira" | "Generic"
  title: string | null
  description: string | null
  iconUrl: string | null
  imageUrl: string | null
  externalKey: string | null
  fetchedAt: string | null
  fetchStatus: string     // "Pending" | "Ok" | "Failed"
}

// Update Card interface:
export interface Card {
  // ... existing fields ...
  evidenceLinks: EvidenceLink[]
}
```

**card.ts — add evidence link actions:**
```typescript
async attachEvidenceLink(sprintId: number, cardId: number, url: string): Promise<EvidenceLink>
async detachEvidenceLink(sprintId: number, cardId: number, evidenceLinkId: number): Promise<void>
```

Both use the same auth header pattern as existing card actions (guest token + credentials include).

**useSprintHub.ts — add `EvidenceLinkFetched` listener:**
```typescript
connection.on('EvidenceLinkFetched', (link: EvidenceLink) => {
  const card = cardStore.cards.find(c => c.id === link.cardId)
  if (card) {
    const idx = card.evidenceLinks.findIndex(e => e.id === link.id)
    if (idx !== -1) card.evidenceLinks[idx] = link
    else card.evidenceLinks.push(link)
  }
})
```

The existing `CardUpdated` listener already replaces the full card object, which now includes evidence links. No additional change needed for attach/detach — they push `CardUpdated`.

**Dependencies:** Steps 4, 6, 7, 9 (API endpoints and event handlers).

### Step 12: Frontend — evidence link UI on cards

**What:** Display evidence links on cards and provide attach/detach UI.
**Files to modify:**
- `web/reflekt-web/src/components/CardItem.vue` — evidence link display + attach/detach UI
- `web/reflekt-web/src/views/RetroRoomView.vue` — new event handlers + optimistic update logic

**CardItem.vue — evidence link display:**
1. Below the card text, render each evidence link as a compact inline preview.
2. Display logic per evidence link:
   - **`FetchStatus = Ok`, Source = GitHub/Generic:** Favicon (from `iconUrl` or constructed `https://{domain}/favicon.ico`) + title text. Truncate title if too long.
   - **`FetchStatus = Ok`, Source = Jira:** Favicon + issue key chip (styled badge with `externalKey`). If title is present, show it too.
   - **`FetchStatus = Pending`:** Bare URL text with a small loading spinner or dimmed style.
   - **`FetchStatus = Failed`:** Bare URL text with domain favicon. Dimmed or subtle error indicator.
3. Clicking any evidence link opens the URL in a new tab (`target="_blank"`, `rel="noopener noreferrer"`).
4. Detach button (small "×" icon) visible only if `canEdit` prop is true (same ownership gating as edit/delete buttons).

**CardItem.vue — attach UI:**
1. A small link icon button (visible only when `canEdit` is true).
2. Clicking it reveals a text input below the card.
3. User pastes/types a URL and presses Enter or clicks "Add."
4. Emits `@attach-evidence-link(card.id, url)` — the view handles the API call.

**CardItem.vue — detach:**
- Detach × button emits `@detach-evidence-link(card.id, evidenceLinkId)`.

**RetroRoomView.vue — event handlers:**
- Wire `@attach-evidence-link="attachEvidenceLink"` and `@detach-evidence-link="detachEvidenceLink"` on each `<CardItem>`.
- `attachEvidenceLink(cardId, url)`: optimistic add (evidence link with `FetchStatus = Pending` and bare URL) → call `cardStore.attachEvidenceLink(sprintId, cardId, url)` → on failure, remove and show toast. The `EvidenceLinkFetched` SignalR event updates the link with metadata when the background fetch completes.
- `detachEvidenceLink(cardId, evidenceLinkId)`: optimistic remove → call `cardStore.detachEvidenceLink(sprintId, cardId, evidenceLinkId)` → on failure, restore and show toast.

**Styling:** Tailwind. Evidence links render as small pill-like elements below the card text. Jira issue key chips use a distinct color (e.g., blue badge). Keep it compact per spec §6.4 ("inline mode only").

**Pattern:** Reference existing `@edit`, `@delete`, `@vote` event wiring in `RetroRoomView.vue` and the corresponding UI in `CardItem.vue`.
**Dependencies:** Step 11.

### Step 13: Unit tests

**What:** Test the Card aggregate's evidence link methods and the source detector.
**Files to create:**
- `tests/Reflekt.UnitTests/Cards/EvidenceLinkTests.cs`
- `tests/Reflekt.UnitTests/EvidenceLinks/SourceDetectorTests.cs`

**EvidenceLinkTests.cs — Card aggregate evidence link tests:**

*AttachEvidenceLink:*
1. `AttachEvidenceLink_GitHub_CreatesWithPendingStatus` — attach a github.com URL, verify FetchStatus = Pending.
2. `AttachEvidenceLink_Jira_CreatesWithOkStatus` — attach a Jira URL, verify FetchStatus = Ok, ExternalKey populated.
3. `AttachEvidenceLink_Generic_CreatesWithPendingStatus`.
4. `AttachEvidenceLink_RaisesEvidenceLinkAttachedEvent`.
5. `AttachEvidenceLink_AddsToCollection` — `card.EvidenceLinks.Count` increases.
6. `AttachEvidenceLink_EmptyUrl_Throws`.

*UpdateEvidenceLinkMetadata:*
7. `UpdateMetadata_SetsAllFieldsAndRaisesEvent` — attach a link, then update metadata. Verify Title/Desc/Icon/Image/FetchedAt set, FetchStatus = Ok, event raised.
8. `UpdateMetadata_Failed_SetsFetchedAtAndStatus` — update with Failed status, verify no metadata stored.
9. `UpdateMetadata_NonexistentLink_Throws`.

*DetachEvidenceLink:*
10. `DetachEvidenceLink_RemovesFromCollection` — attach then detach, verify count.
11. `DetachEvidenceLink_RaisesDetachedEvent`.
12. `DetachEvidenceLink_NonexistentLink_Throws`.

**SourceDetectorTests.cs:**

13. `Detect_GitHubUrl_ReturnsGitHub` — `https://github.com/org/repo/pull/1` → (GitHub, null).
14. `Detect_JiraBrowseUrl_ReturnsJiraWithKey` — `https://anything.com/browse/PROJ-123` → (Jira, "PROJ-123").
15. `Detect_ConfiguredJiraDomain_ReturnsJira` — URL with configured domain host → (Jira, key if present).
16. `Detect_GenericUrl_ReturnsGeneric` — `https://example.com/page` → (Generic, null).
17. `Detect_JiraDomainNull_FallsBackToPattern` — no configured domain, `/browse/KEY` still works.

**Pattern:** Reference `CardTests.cs`.
**Dependencies:** Steps 1–2, 5.

### Step 14: Integration tests

**What:** Test evidence link endpoints end-to-end.
**Files to create:**
- `tests/Reflekt.IntegrationTests/EvidenceLinkEndpointTests.cs`

**Test setup:** Same `WebApplicationFactory<Program>` pattern as `CardEndpointTests.cs`. Each test creates a team, starts a sprint, joins a participant, and creates a card first.

**Test cases:**

*AttachEvidenceLink:*
1. `AttachLink_GitHubUrl_Returns201` — valid GitHub URL → 201, Source = "GitHub", FetchStatus = "Pending".
2. `AttachLink_JiraUrl_Returns201_WithExternalKey` — Jira pattern URL → 201, Source = "Jira", ExternalKey populated, FetchStatus = "Ok".
3. `AttachLink_GenericUrl_Returns201` → Source = "Generic", FetchStatus = "Pending".
4. `AttachLink_EmptyUrl_Returns400`.
5. `AttachLink_UrlExceeds2048_Returns400` — URL longer than 2048 chars.
6. `AttachLink_NonHttpScheme_Returns400` — `ftp://example.com`.
7. `AttachLink_NoAuth_Returns401`.
8. `AttachLink_NotCardOwnerOrSm_Returns403` — participant B on participant A's card.
9. `AttachLink_NonexistentCard_Returns404`.
10. `AttachLink_ClosedSprint_Returns409`.
11. `AttachLink_ByScrumMaster_Returns201` — SM attaches to another's card.

*DetachEvidenceLink:*
12. `DetachLink_ByAuthor_Returns204` — owner detaches.
13. `DetachLink_ByScrumMaster_Returns204`.
14. `DetachLink_NotOwnerOrSm_Returns403`.
15. `DetachLink_NonexistentLink_Returns404`.
16. `DetachLink_ClosedSprint_Returns409`.

*Card endpoints return evidence links:*
17. `ListCards_IncludesEvidenceLinks` — create card, attach link, GET cards → response includes evidence links array.
18. `CreateCard_ReturnsEmptyEvidenceLinks` — new card response has `evidenceLinks: []`.
19. `DeleteCard_CascadeDeletesEvidenceLinks` — create card, attach link, delete card, verify evidence links are gone (GET cards returns no orphaned links).

**Pattern:** Reference `CardEndpointTests.cs`.
**Dependencies:** Steps 6–7 and all prior steps.

## 6. Migration notes

Next EF Core migration after Voting. Command:
```bash
dotnet ef migrations add AddEvidenceLink -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

The migration should only add the `EvidenceLinks` table and its FK/index. It should NOT modify the `Cards` table (the relationship is defined via the FK on EvidenceLinks).

If existing Card queries break after adding `.Include(c => c.EvidenceLinks)`, delete `data/reflekt.db` and re-run — auto-migration will recreate.

## 7. Testing strategy

- **Unit tests** (Step 13) cover the Card aggregate's evidence link methods (attach/update/detach), domain event raising, guard validation, and the source detection utility.
- **Integration tests** (Step 14) cover the full HTTP pipeline: attach/detach with auth checks, ownership enforcement, sprint-state gating, source detection from real URLs, and evidence links embedded in card responses.
- **Background service is not integration-tested** — it requires an actual HTTP call to an external URL. The unit tests cover the aggregate methods it calls. Manual testing covers the round-trip.
- **SignalR push is not integration-tested** — verified manually in the browser.
- **Optimistic UI** is tested manually in the browser.

## 8. Verification — acceptance criteria

Evidence Links is done when all of these pass:

1. `dotnet build Reflekt.slnx` — zero errors, zero warnings.
2. `dotnet test` — all tests pass (existing + new unit + new integration).
3. `dotnet run` in Development:
   - `data/reflekt.db` has `EvidenceLinks` table with correct schema.
   - Seed data creates sample evidence links on cards.
4. Manual API flow (Scalar or curl):
   - Join a sprint as a participant (get guest token).
   - Create a card.
   - `POST /api/sprints/{sprintId}/cards/{cardId}/evidence-links` with `{ "url": "https://github.com/dotnet/aspnetcore" }` → 201, Source = "GitHub", FetchStatus = "Pending".
   - Wait a few seconds — background fetch enriches the link. Check via `GET /api/sprints/{sprintId}/cards` — evidence link now has Title, IconUrl, FetchStatus = "Ok".
   - `POST` with a Jira URL (`https://your-org.atlassian.net/browse/PROJ-123`) → 201, Source = "Jira", ExternalKey = "PROJ-123", FetchStatus = "Ok", no fetch queued.
   - `POST` with a generic URL → 201, Source = "Generic", background fetch runs.
   - `POST` with `ftp://example.com` → 400.
   - Attach to a Closed sprint → 409.
   - Attach to someone else's card as participant → 403.
   - Attach to someone else's card as SM → 201.
   - `DELETE /api/sprints/{sprintId}/cards/{cardId}/evidence-links/{id}` → 204.
   - Delete a card with evidence links → card and links both gone.
5. Frontend flow (`npm run dev`):
   - Navigate to `/sprint/{sprintId}`, join.
   - Open a card's link attachment UI, paste a GitHub URL.
   - Evidence link appears immediately with "Pending" state (bare URL).
   - After a moment, the link updates with a title and favicon (SignalR EvidenceLinkFetched event).
   - Attach a Jira URL — issue key chip appears immediately (no Pending state).
   - Click an evidence link — opens in a new tab.
   - Detach an evidence link — it disappears.
   - Open second browser tab, join as second participant — evidence link changes appear in real-time.
   - Second participant cannot see attach/detach buttons on first participant's cards.
6. Existing Card, Vote, Participant, Sprint, and Team endpoints still work — no regressions.
7. Evidence links are embedded in card responses from all card endpoints.

## 9. Out of scope

If tempted to add any of these, stop:

- Jira REST API calls — **not in v1**. URL key extraction only.
- GitHub REST API calls — **not in v1**. OpenGraph only.
- Full preview card mode — **not in v1**. Inline only.
- Evidence link editing — **not in v1**. Detach and re-attach.
- Automatic fetch retry — **not in v1**. Failed stays failed.
- Report generation with evidence links — **Reports plan**.
- Card text URL auto-detection — **not in v1**. Frontend sends one URL at a time.
- OpenGraph library — **not needed**. AngleSharp + HttpClient already scaffolded.

## 10. File summary

**New files:**
- `src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLinkSource.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/FetchStatus.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLink.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/EvidenceLinkAttachedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/EvidenceLinkFetchedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Events/EvidenceLinkDetachedEvent.cs`
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/EvidenceLinkConfiguration.cs`
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/EvidenceLinkSourceDetector.cs`
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/DetachEvidenceLinkEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Events/EvidenceLinkAttachedEventHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Events/EvidenceLinkFetchedEventHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Events/EvidenceLinkDetachedEventHandler.cs`
- `tests/Reflekt.UnitTests/Cards/EvidenceLinkTests.cs`
- `tests/Reflekt.UnitTests/EvidenceLinks/SourceDetectorTests.cs`
- `tests/Reflekt.IntegrationTests/EvidenceLinkEndpointTests.cs`

**Modified files:**
- `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs` — add `_evidenceLinks` field + `EvidenceLinks` property
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs` — add Attach/Update/Detach methods
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs` — add HasMany + backing field config
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/CardRepository.cs` — override GetByIdAsync with Include, add Include to GetBySprintIdAsync
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `DbSet<EvidenceLink>`
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — seed evidence links
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs` — add EvidenceLinkResponse, update CardResponse + mappings
- `src/Services/Reflekt/Reflekt.API/OpenGraph/FetchJob.cs` — add CardId field
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphBackgroundService.cs` — replace log with DB update logic
- `src/Services/Reflekt/Reflekt.API/appsettings.json` — add EvidenceLinks:JiraDomain
- `src/Services/Reflekt/Reflekt.API/appsettings.Development.json` — add EvidenceLinks:JiraDomain example
- `web/reflekt-web/src/stores/card.ts` — update Card interface, add evidence link actions
- `web/reflekt-web/src/composables/useSprintHub.ts` — add EvidenceLinkFetched listener
- `web/reflekt-web/src/components/CardItem.vue` — evidence link display + attach/detach UI
- `web/reflekt-web/src/views/RetroRoomView.vue` — new event handlers for attach/detach

**Unchanged:**
- All Auth files, Team files, Sprint files, Participant files, Vote files
- `Reflekt.API/DependencyInjection.cs` (AddReflektOpenGraph already called)
- `Reflekt.API/OpenGraph/OpenGraphFetcher.cs` (already complete)
- `Reflekt.API/OpenGraph/OpenGraphConfiguration.cs` (already complete)
- `Reflekt.API/Hubs/SprintHub.cs`, `Program.cs`, `Policies.cs`
