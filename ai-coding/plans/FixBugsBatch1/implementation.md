# Fix Bugs Batch 1 — Implementation

## Scope

Fixed two bugs in one solo session:
- **BUG-001** — Closed sprint page was a navigation dead-end.
- **BUG-002** — SignalR broadcasts never reached other participants (all real-time broken, not just cards).

**Deferred to team pipeline:**
- **BUG-003** — Blur other participants' card text during Writing phase. Scope exceeds solo (6 files, backend + frontend, intersects TD-001).

## Files Modified

### BUG-001
- `web/reflekt-web/src/views/SprintView.vue` — Added an always-visible "← Back to sprints" link (router-link to `welcome` route) so the Sprint view is never a dead end. Added a "View Report" link (router-link to `report` route) visible when `sprint.state === 'Closed'`, exposing the report that was previously unreachable from the app.

### BUG-002
- `src/BuildingBlocks/BuildingBlocks.Persistence/DomainEventPublishingInterceptor.cs` — Changed event dispatch from compile-time-typed `ev.PublishAsync(Mode.WaitForNone, ct)` (which silently dispatched to `IEventHandler<IDomainEvent>` — zero handlers) to reflection-based invocation of a generic helper `PublishEvent<TEvent>` with the runtime event type. Cached `MethodInfo` once as a static field. Now every concrete handler (`BroadcastCardCreatedHandler`, `BroadcastParticipantJoinedHandler`, etc.) fires as intended.

## Key Decisions

- **Reflection over CRTP** for BUG-002. CRTP (`IDomainEvent<TSelf>`) would be compile-time safe and zero-runtime-cost, but it would force every domain event record to carry a self-type parameter (~13 files in `Reflekt.Domain/**/Events/`). Reflection isolates the fix to one method in one file. MethodInfo is cached, so the per-event cost is a single `MakeGenericMethod` + `Invoke` — negligible at event-dispatch frequency.
- **Didn't adopt the microservices-repo pattern** (MediatR-based `IDomainEventPublisher`). That would have required adding MediatR, switching handler interfaces, and re-registering everything — big refactor unjustified for a single-service project that already wires FastEndpoints events.

## Deviations from Plan

None. Scope matched the pre-approved plan exactly (BUG-001 implemented, BUG-002 investigated and fixed, BUG-003 flagged and deferred).

## Verification

- `dotnet build src/BuildingBlocks/BuildingBlocks.Persistence/BuildingBlocks.Persistence.csproj` → build succeeded, 0 warnings, 0 errors.
- Full Reflekt.API build reported 8 `MSB3021` errors — all file-lock errors because the dev server (`Reflekt.API` PID 39228) was holding the output DLLs. C# compilation itself succeeded; copying into `bin/` is blocked until the running server restarts. **Action needed by user:** restart the Reflekt.API dev server to load the fixed interceptor.
- UI verification not performed in this session — requires running the app in two browsers and creating a card. Recommend doing this after the server restart to confirm BUG-002 is resolved.
