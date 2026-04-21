# Fix Bugs Batch 1 — Lessons

## FastEndpoints polymorphic dispatch gotcha

`PublishAsync<TEvent>` resolves handlers by the **compile-time** generic `T`, not the runtime type of the event. When an event list is typed as `IEnumerable<IDomainEvent>` (or any base interface) and each element is passed to `PublishAsync` via an implicit generic argument, `T` binds to `IDomainEvent` for every call — so only `IEventHandler<IDomainEvent>` implementations fire. Concrete handlers (`IEventHandler<CardCreatedEvent>`, etc.) never see the event, and there is no runtime warning: events dispatch silently into the void.

**Where this bit us:** `DomainEventPublishingInterceptor` iterated a `List<IDomainEvent>` and called `ev.PublishAsync(...)`. Every handler downstream (SignalR broadcasters, link-fetch triggers) had been ignored since the day it was written — the bug just stayed hidden behind optimistic UI.

**Fix pattern, for next time:** when you need polymorphic dispatch to concrete handlers, invoke a private `static Task PublishEvent<TEvent>(...)` helper reflectively with `MethodInfo.MakeGenericMethod(ev.GetType())`. Cache the open `MethodInfo` once in a static field so the per-event cost is just `MakeGenericMethod` + `Invoke`. That isolates the whole workaround to one method and leaves the domain events untouched.

**Alternative considered:** CRTP via `IDomainEvent<TSelf> : IDomainEvent` would give compile-time-safe generic binding with zero reflection. Rejected because it forces every concrete event record to carry a self-type parameter, rippling through ~13 files in `Reflekt.Domain/**/Events/`. Not worth it for a one-place dispatch fix.

## Optimistic UI masks broken real-time

When the creator of an action sees their own result through optimistic-local-store push (card appended to the store in the endpoint's success branch), broken SignalR looks identical to working SignalR from a single browser. BUG-002 only surfaced when the user finally opened a second browser and noticed the other participant never saw the card. Every real-time-dependent handler in the codebase had been a no-op for the entire development of Reflekt, unnoticed by single-browser testing.

**For future real-time work:** any feature that relies on `IEventHandler<T>` firing needs at least a two-browser smoke test before being called done. A single-browser "it works" is not evidence that the event bus reached another client.

## Dev-server file locks are not build errors

After a `BuildingBlocks.*` project change, `dotnet build` of the full Reflekt.API solution can report `MSB3021` / `MSB3027` — "cannot copy, file is locked." These are not compile errors; they mean the running `Reflekt.API` process has the old output DLL open and MSBuild can't overwrite it. The C# compilation itself has already succeeded. To deploy the new binary you have to stop the dev server first (or restart it). Don't chase these as real build failures.
