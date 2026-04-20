# EvidenceLinks — Lessons

## Developer Lessons

- **CardItem.vue vs RetroRoomView.vue split**: In this codebase, card *rendering* (how a card looks, what actions are on it) lives in `CardItem.vue`. `RetroRoomView.vue` only owns data fetching, handler functions, and wiring props/events to `CardItem`. A plan step that says "implement the card UI" means `CardItem.vue`, not the view.

- **EF backing field + navigation config**: When an aggregate uses a private `List<T>` backing field (e.g. `_evidenceLinks`), the EF navigation config needs two things: `HasMany` on the parent config with `UsePropertyAccessMode(PropertyAccessMode.Field)`, and the `Include` call in every repository query that needs the collection populated. Forgetting either silently gives you an empty collection.

- **Seed idempotency with child entities**: Checking `collection.Any()` in a seed initializer is unreliable when the parent aggregate is loaded without `.Include`. Use a direct `db.ChildSet.AnyAsync(e => e.ParentId == id)` query instead.

- **Background service + scoped DbContext**: A singleton `IHostedService` cannot inject a scoped `DbContext` directly. The standard pattern is to inject `IServiceScopeFactory`, create a scope per work item, then resolve `DbContext` from that scope. Using `IServiceScopeFactory` is the only correct approach; do not try to register `DbContext` as singleton.

- **Domain event with removed entity**: When a `DetachX` method removes an entity from the aggregate's collection before raising the event, the event cannot carry the removed entity reference (it's gone). Carry the entity's `Id` (int) instead — as `EvidenceLinkDetachedEvent(Card Card, int EvidenceLinkId)`.

- **Jira inline display — no fetch needed**: Jira links set `FetchStatus.Ok` immediately in the domain factory because the only display data is the issue key extracted from the URL. Setting Ok at creation avoids a wasted enqueue and lets the frontend render the Jira chip immediately.

## Skill Gaps

- **Missing skill — BackgroundService + Channel pattern**: No skill covers the producer/consumer Channel pattern used in `OpenGraphBackgroundService`. Steps 8–9 required understanding `ChannelWriter<T>`, `ChannelReader<T>`, `IServiceScopeFactory` scope-per-item, and the `IAsyncEnumerable` read loop. Reference files used: `Reflekt.API/OpenGraph/OpenGraphBackgroundService.cs`, `Reflekt.API/OpenGraph/OpenGraphFetcher.cs`. Suggested skill name: `background-service-channel`. Should cover: registering a `Channel<T>` as singleton, injecting `ChannelWriter` into producers and `ChannelReader` into the hosted service, `IServiceScopeFactory` for scoped DB access, and graceful cancellation.

- **Missing skill — dual-auth ownership check**: No skill documents the ownership check pattern used in AttachEvidenceLink and DetachEvidenceLink endpoints (compare participantId claim vs SM email, return 403 vs 404 depending on what was found). Suggested skill name: `ownership-check`. Should cover: extracting participantId from guest-token claims, checking SM auth, the 404-vs-403 decision (hide existence from unauthorized users only when that's the policy).
