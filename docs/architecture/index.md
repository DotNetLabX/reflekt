# Reflekt — Architecture Map

One service (an ASP.NET Core API that also serves the Vue SPA), one SQLite database, one SignalR hub, deployed as one Azure App Service. The full reference is `architecture.md`, imported below. The original proposal and its decisions are in [v1.md](v1.md) (superseded).

## Projects

| Project | Holds |
|---|---|
| `src/Services/Reflekt/Reflekt.API` | 12 feature slices (44 endpoints), `SprintHub`, auth, error middleware, Mapster profiles, OpenGraph fetcher, start-up; serves the built SPA from `wwwroot/` |
| `src/Services/Reflekt/Reflekt.Domain` | 7 aggregates (`Card`, `CardGroup`, `Participant`, `Sprint`, `Team`, `User`, `Vote`), `EvidenceLink`, phase-action policy, 25 domain events |
| `src/Services/Reflekt/Reflekt.Persistence` | `ReflektDbContext`, 9 configurations, 8 repositories, 16 migrations, seeding |
| `src/BuildingBlocks/Blocks.*` | Core (guard), Domain (aggregate base, events), Exceptions (HTTP exceptions), Persistence (event interceptor, repository base) |
| `tests/Reflekt.UnitTests`, `tests/Reflekt.IntegrationTests` | domain unit tests; endpoint tests through `WebApplicationFactory` |
| `web/reflekt-web` | Vue 3 + Pinia + Tailwind SPA |

## Reference graph

```
Reflekt.API ──▶ Reflekt.Domain ──▶ Blocks.Domain
     │ ──▶ Reflekt.Persistence ──▶ Reflekt.Domain, Blocks.Persistence ──▶ Blocks.Domain
     │ ──▶ Blocks.Core ──▶ Blocks.Exceptions
     └ ──▶ Blocks.Exceptions
```

## Request flow for a mutation (example: a guest adds a card)

1. `POST /api/sprints/{sprintId}/cards` with `X-Guest-Token` → `GlobalExceptionMiddleware` wraps everything below.
2. Authentication (`GuestToken` scheme) and the endpoint's policy (`Guest`).
3. FastEndpoints binds the request and runs its FluentValidation validator (400 on failure).
4. The endpoint checks access with `SprintAccess` (the token's sprint, its participant — a kicked participant already failed step 2), the sprint state, and the phase gate (`PhaseActionPolicy.EnsureAllowed`, 409 on refusal).
5. The aggregate's factory (`Card.Create`) builds the card and raises `CardCreatedEvent`; the repository adds and saves it. An edit works the same way through an aggregate method.
6. `DomainEventPublishingInterceptor` publishes the event, awaited inside the save; `BroadcastCardCreatedHandler` sends `CardCreated` to the sprint's hub groups (redacted for other participants during Writing). A handler failure is logged, not rethrown.
7. The endpoint answers `201` with the card; the other clients already have the hub message.

## Where each concern lives

| Concern | Where |
|---|---|
| Endpoints (route, policy, handler) | `Reflekt.API/Features/{Slice}/{Feature}/*Endpoint.cs` + sibling `*Command.cs` / `*Query.cs` |
| Business rules, state and phase machines | `Reflekt.Domain/{Area}/Behaviors/` |
| Which action each phase allows | `PhaseActions` table, seeded in `Reflekt.Persistence/EntityConfigurations/PhaseActionConfiguration.cs` |
| Domain event dispatch | `src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs` |
| Event handlers (broadcasts, fetch queue, timer restart) | next to their feature: `Features/{Slice}/{Feature}/Broadcast…Handler.cs` |
| Real-time hub, readiness, timers | `Reflekt.API/Hubs/` |
| Sign-in, guest tokens, policies, access checks | `Reflekt.API/Auth/` |
| Error status map and body, error codes | `Reflekt.API/Middleware/` |
| Evidence-link metadata fetch | `Reflekt.API/OpenGraph/`, `Features/EvidenceLinks/` |
| Start-up, middleware order, options | `Reflekt.API/Program.cs`, `Reflekt.API/DependencyInjection.cs` |
| Database schema and migrations | `Reflekt.Persistence/EntityConfigurations/`, `Reflekt.Persistence/Migrations/` |
| Package versions | `Directory.Packages.props`; frontend `web/reflekt-web/package.json` |
| SPA API calls and hub client | `web/reflekt-web/src/api/`, `src/composables/useSprintHub.ts` |
| SPA state | `web/reflekt-web/src/stores/` |
| Deployment | `deploy.ps1`, `docs/deployment-azure.md` |
| Coding conventions | `docs/conventions/coding-conventions.md` |

@architecture.md
