# EvidenceLinks — Implementation Log

## Files Created

### Domain
- `Reflekt.Domain/Cards/EvidenceLinkSource.cs` — enum: GitHub, Jira, Generic
- `Reflekt.Domain/Cards/FetchStatus.cs` — enum: Pending, Ok, Failed
- `Reflekt.Domain/Cards/EvidenceLink.cs` — entity with private ctor + static Create factory; Jira→Ok immediately, others→Pending
- `Reflekt.Domain/Cards/Events/EvidenceLinkAttachedEvent.cs` — carries Card + EvidenceLink references
- `Reflekt.Domain/Cards/Events/EvidenceLinkFetchedEvent.cs` — carries Card + EvidenceLink references
- `Reflekt.Domain/Cards/Events/EvidenceLinkDetachedEvent.cs` — carries Card + EvidenceLinkId (int, not reference — entity removed at event time)

### Domain (modified)
- `Reflekt.Domain/Cards/Card.cs` — added `_evidenceLinks` backing field + `EvidenceLinks` read-only property
- `Reflekt.Domain/Cards/Behaviors/Card.cs` — added `AttachEvidenceLink`, `UpdateEvidenceLinkMetadata`, `DetachEvidenceLink`; guards: empty URL throws, nonexistent link throws

### Persistence
- `Reflekt.Persistence/EntityConfigurations/EvidenceLinkConfiguration.cs` — table name, FK, max lengths, index on CardId
- `Reflekt.Persistence/Migrations/20260420185204_AddEvidenceLink.cs` — creates EvidenceLinks table
- `Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs` — added HasMany→WithOne cascade + PropertyAccessMode.Field for backing field
- `Reflekt.Persistence/Repositories/CardRepository.cs` — overrode `GetByIdAsync` with `.Include(c => c.EvidenceLinks)`; added Include to `GetBySprintIdAsync`
- `Reflekt.Persistence/ReflektDbContext.cs` — added `DbSet<EvidenceLink>` property
- `Reflekt.Persistence/DbInitializer.cs` — seeding: GitHub + Generic evidence links on first WentWell card; idempotency check via `db.EvidenceLinks.AnyAsync`

### API
- `Reflekt.API/Features/EvidenceLinks/EvidenceLinkSourceDetector.cs` — static class; GitHub by host, Jira by /browse/{KEY} pattern (GeneratedRegex), Generic fallback; accepts optional configured JiraDomain
- `Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkCommand.cs` — request record + FluentValidation: required, max 2048, absolute HTTP/HTTPS URI
- `Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkEndpoint.cs` — POST `/api/sprints/{sprintId}/cards/{cardId}/evidence-links`; dual-auth ownership check; returns 201 EvidenceLinkResponse
- `Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/DetachEvidenceLinkEndpoint.cs` — DELETE same path + `/{evidenceLinkId}`; ownership check; 204
- `Reflekt.API/Events/EvidenceLinkAttachedEventHandler.cs` — pushes CardUpdated to SignalR; enqueues FetchJob if Pending
- `Reflekt.API/Events/EvidenceLinkFetchedEventHandler.cs` — pushes EvidenceLinkFetched with EvidenceLinkResponse
- `Reflekt.API/Events/EvidenceLinkDetachedEventHandler.cs` — pushes CardUpdated to SignalR
- `Reflekt.API/Features/Cards/CardContracts.cs` — added EvidenceLinkResponse record (11 fields); added `List<EvidenceLinkResponse>?` to CardResponse; added ToResponse(EvidenceLink) helper
- `Reflekt.API/OpenGraph/FetchJob.cs` — added CardId field to existing record
- `Reflekt.API/OpenGraph/OpenGraphBackgroundService.cs` — replaced stub with scoped DB update via IServiceScopeFactory; calls `card.UpdateEvidenceLinkMetadata`; try/catch sets Failed on error
- `Reflekt.API/appsettings.json` — added `"EvidenceLinks": { "JiraDomain": null }`
- `Reflekt.API/appsettings.Development.json` — added `"EvidenceLinks": { "JiraDomain": "your-org.atlassian.net" }`

### Frontend
- `web/reflekt-web/src/stores/card.ts` — added `EvidenceLink` interface; added `evidenceLinks: EvidenceLink[]` to `Card`; added `attachEvidenceLink` and `detachEvidenceLink` actions
- `web/reflekt-web/src/composables/useSprintHub.ts` — added `EvidenceLinkFetched` listener that patches card's evidence links array in-place
- `web/reflekt-web/src/components/CardItem.vue` — evidence link display (Pending/Failed/Ok states, Jira chip, favicon); attach UI (🔗 → input → Add); detach (× button gated by canEdit)
- `web/reflekt-web/src/views/RetroRoomView.vue` — `attachEvidenceLink` + `detachEvidenceLink` handlers with optimistic UI; wired on all 3 CardItem usages; optimistic card objects include `voteCount: 0, evidenceLinks: []`

### Tests
- `tests/Reflekt.UnitTests/Cards/EvidenceLinkTests.cs` — 12 tests: Attach (6), UpdateMetadata (3), Detach (3)
- `tests/Reflekt.UnitTests/EvidenceLinks/SourceDetectorTests.cs` — 5 tests for source detector
- `tests/Reflekt.IntegrationTests/EvidenceLinkEndpointTests.cs` — 19 integration tests: Attach (9), Detach (5), card endpoint inclusion (3); all passing

## Key Decisions

- `DetachEvidenceLinkDetachedEvent` carries `int EvidenceLinkId` (not the entity) because the entity is removed from the collection before the event is dispatched.
- `EvidenceLink.Create` sets `FetchStatus.Ok` + `FetchedAt` immediately for Jira (issue key is the data; no fetch needed).
- Seed idempotency uses `db.EvidenceLinks.AnyAsync(e => e.CardId == ...)` — not an in-memory check — because EvidenceLinks are not eager-loaded in seed context.
- `PropertyAccessMode.Field` on the EvidenceLinks navigation is required for EF Core to populate the private `_evidenceLinks` backing field.
- Background service uses `IServiceScopeFactory` to create a scoped lifetime for `ReflektDbContext` (registered as scoped, service is singleton).

## Review Fixes

- `CardItem.vue` line 47-51: Made Pending evidence links clickable — changed `:href` from conditional to always `link.url`, removed dynamic `cursor-default/pointer` in favor of static `cursor-pointer`. Pending visual treatment (dimmed text, animated dots) unchanged.
- `OpenGraphBackgroundService.cs` line 29: Truncate `result.Title` (max 500) and `result.Description` (max 1000) before calling `UpdateEvidenceLinkMetadata` — prevents EF max-length constraint throws that would leave links stuck in Pending.
- `RetroRoomView.vue` `attachEvidenceLink`: Replace optimistic placeholder with real API response after `attachEvidenceLink` succeeds — prevents stale negative-ID links if SignalR `CardUpdated` event is missed. Pattern matches existing `addNote` reconciliation.
- `CardItem.vue` `faviconUrl`: Resolve `link.iconUrl` against `link.url` via `new URL(iconUrl, baseUrl).href` — handles relative icon paths returned by AngleSharp so favicons resolve against the target site's origin, not the app's.

## Deviations from Plan

- **Step 12 file**: Plan said to implement card rendering in `RetroRoomView.vue`, but all card rendering lives in `CardItem.vue`. Raised via `questions.md`; Architect confirmed `CardItem.vue` is correct. `RetroRoomView.vue` only gets event wiring.
