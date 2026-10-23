# CLAUDE.md

Reflekt: a retrospective board. One .NET 10 service (`src/Services/Reflekt/`) serving a Vue 3 SPA, real-time over SignalR, SQLite through EF Core. The full as-built reference is `docs/architecture/architecture.md`; the one-page map is `docs/architecture/index.md`.

## Authority & Spec Files

Spec files (architecture.md, business.md, etc.) describe current state, not rules. I decide changes — flag conflicts, propose options, don't refuse.

## graphify

This project has a graphify knowledge graph at graphify-out/.

Rules:
- Before answering architecture or codebase questions, read graphify-out/GRAPH_REPORT.md for god nodes and community structure
- If graphify-out/wiki/index.md exists, navigate it instead of reading raw files
- After modifying code files in this session, run `graphify update .` to keep the graph current (AST-only, no API cost)

## Domain

### Domain events

Domain events carry a reference to the aggregate object, not individual properties. Since the `DomainEventPublishingInterceptor` dispatches after `SaveChangesAsync` (or after the commit of an explicit transaction), the aggregate's `Id` and all state are fully populated at dispatch time.

```csharp
// Event definition — holds the aggregate reference:
public sealed record CardCreatedEvent(Card Card) : IDomainEvent;

// Raised inside the aggregate factory/method:
card.AddDomainEvent(new CardCreatedEvent(card));

// Handler reads populated state:
var cardId = ev.Card.Id;       // real auto-incremented value
var sprintId = ev.Card.SprintId;
```

**Exception:** an event may carry, beside the aggregate, facts that are not recoverable from the aggregate after the save — a previous value or the id of a removed child. Examples: `CardsGroupedEvent(Group, CardId, SourceGroupId)`, `GroupFocusedEvent(Sprint, GroupId, PreviousGroupId)`, `EvidenceLinkDetachedEvent(Card, EvidenceLinkId)`. Anything the handler can read from the aggregate is read from the aggregate.

How events are published (awaited, in order, failures logged and not rethrown): `docs/architecture/architecture.md` § 3.

### Aggregate creation

Use a static factory method when creation involves business rules (guards) or raises a domain event. Use `required init` properties with direct construction when creation is plain data assignment with no invariants.

### Repositories

Concrete classes (no interfaces — see Guardrails) in `Reflekt.Persistence/Repositories/`: seven, one per aggregate, over `RepositoryBase` (`Blocks.Persistence`), plus the read-only `PhaseActionRepository`, a plain class with no save. The base wraps `SaveChangesAsync` so endpoints never touch `DbContext` directly:

```csharp
public virtual Task<int> SaveChangesAsync(CancellationToken ct)
    => Db.SaveChangesAsync(ct);
```

## Architecture

DDD, Vertical Slice, CQRS, Clean Architecture, Event Driven Design — inside one service: aggregates in `Reflekt.Domain`, one folder per feature slice in `Reflekt.API/Features/`, each slice's request split into a command or a query (`*Command.cs` / `*Query.cs`), domain events handled in-process.

## Repo Structure

```
src/
  BuildingBlocks/                # Shared libraries, no Reflekt knowledge
    Blocks.Core/                 #   Guard
    Blocks.Domain/               #   AggregateRoot, Entity, IDomainEvent, DomainException
    Blocks.Exceptions/           #   HttpException family (400/401/403/404/409)
    Blocks.Persistence/          #   ApplicationDbContext, DomainEventPublishingInterceptor, RepositoryBase
  Services/
    Reflekt/                     # The one service
      Reflekt.API/               #   Features/ (12 slices: endpoint + command/query + validator + event handlers),
                                 #   Hubs/, Auth/, Middleware/, Mappings/, OpenGraph/, Program.cs, wwwroot/ (built SPA)
      Reflekt.Domain/            #   Aggregates (state file + Behaviors/ + Events/), value objects, enums
      Reflekt.Persistence/       #   ReflektDbContext, EntityConfigurations/, Repositories/, Migrations/, seeding
tests/
  Reflekt.UnitTests/             # Aggregate behaviour
  Reflekt.IntegrationTests/      # Endpoints through WebApplicationFactory
web/reflekt-web/                 # Vue 3 SPA, built into Reflekt.API/wwwroot
docs/                            # Architecture, conventions, product, specs, backlog
deploy.ps1                       # Azure App Service deployment
Reflekt.slnx, Directory.Build.props, Directory.Packages.props
```

The service uses the three-project split (API / Domain / Persistence). There is no separate Application project — FastEndpoints endpoint classes own feature slices directly (request + response + validator + handler logic + event handlers all live in `Reflekt.API/Features/`).

### Project reference graph

From each `*.csproj`:

```
Reflekt.API ──▶ Reflekt.Domain ──▶ Blocks.Domain
     │ ──▶ Reflekt.Persistence ──▶ Reflekt.Domain
     │                        └──▶ Blocks.Persistence ──▶ Blocks.Domain
     │ ──▶ Blocks.Core ──▶ Blocks.Exceptions
     └ ──▶ Blocks.Exceptions
Reflekt.UnitTests ──▶ Reflekt.API, Reflekt.Domain, Blocks.Domain
Reflekt.IntegrationTests ──▶ Reflekt.API
```

- `Reflekt.API` references `Reflekt.Domain` and `Reflekt.Persistence` directly. Because we do not use repository interfaces (see Guardrails), endpoints consume concrete repositories from `Persistence` — no abstraction layer to bridge.
- `Reflekt.Persistence` references `Reflekt.Domain` and `Blocks.Persistence`.
- `Reflekt.Domain` references only `Blocks.Domain` — no other project, no framework.

## Tech Stack

Versions are central in `Directory.Packages.props`; the full table is `docs/architecture/architecture.md` § 7.

- .NET 10 / ASP.NET Core — solution uses `.slnx` format (XML-based, .NET 10 default)
- **FastEndpoints** — one class per endpoint; route and policy are attributes on the class (`[HttpPost("…")]`, `[Authorize(Policy = …)]` or `[AllowAnonymous]`). Request, response and validator live in the slice's sibling `*Command.cs` / `*Query.cs`.
- **FluentValidation** — request validators (where a request needs one), auto-discovered and executed by FastEndpoints before the handler runs.
- **FastEndpoints event bus** — in-process publish/subscribe for domain events. Aggregates raise `IDomainEvent` records onto their `DomainEvents` list; `DomainEventPublishingInterceptor` (`Blocks.Persistence`) publishes them after a successful save or commit. `IEventHandler<T>` implementations (SignalR broadcasters, the evidence-link fetch trigger) live alongside their feature slices in `Reflekt.API/Features/`.
- **Mapster** — object mapping with `Adapt<T>()`; `IRegister` profiles in `Reflekt.API/Mappings/`, scanned at start-up.
- **EF Core 10** — SQLite (file-backed, local dev and the single App Service instance). Migrations run on every start.
- **SignalR** — real-time server → browser push: one hub, `SprintHub` at `/hubs/sprint`, groups `sprint:{sprintId}` and `participant:{participantId}`. Hub state lives in memory, so the app runs as one instance.
- **Google OAuth** via ASP.NET Core `AddGoogle` — sign-in (cookie auth; `openid`, `email`, `profile` scopes). Any Google account may sign in; no allowlist exists. Sprint participants (guests and signed-in users alike) also hold a guest token (`GuestToken` scheme).
- **Background fetch** — evidence-link OpenGraph metadata fetched by a hosted service from a bounded `Channel`; HTML parsed with AngleSharp.
- OpenAPI via `Microsoft.AspNetCore.OpenApi` (built into .NET 10) + **Scalar** (`Scalar.AspNetCore`) for the interactive UI. Do not use Swashbuckle. Scalar UI must be gated to `IsDevelopment`.
- **Tests** — xUnit 2.x, FluentAssertions 7.2.0, `Microsoft.AspNetCore.Mvc.Testing`; no mocking library (`docs/conventions/testing.md`).

### Cross-cutting (as it is)

- **Errors:** handlers throw (`DomainException`, the `Blocks.Exceptions` family, `Guard.NotFound`); `GlobalExceptionMiddleware` maps each to a status and one JSON body with an optional `code` (`docs/architecture/architecture.md` § 11).
- **Auth:** schemes `Cookies` + `Google` and `GuestToken`; policies `SignedInUser`, `Guest`, `SignedInUserOrGuest` on each endpoint; `SprintAccess` checks the caller against the sprint or team inside the handler.
- **Events:** the save interceptor above.
- **Mapping:** the Mapster profiles above.
- There are no FastEndpoints pre- or post-processors.

### Not used by this app

The starter template this repo came from also described MassTransit + RabbitMQ integration events, gRPC code-first service-to-service calls (and its gRPC h2c port slot), a YARP `ApiGateway/`, a modular monolith under `Modules/`, and docker-compose containers. None of them exists here: no package, project, folder or compose file. Their skills stay in `.claude/skills/` and are marked "not used by this app" in the architecture doc's skill inventory.

## Frontend

- **Vue 3** + **TypeScript** — SPA framework; **vue-router** for the 6 routes.
- **Pinia** — state management (setup stores).
- **Tailwind CSS v4** — styling, with light and dark themes. Icons from `lucide-vue-next`. No UI kit (no Vuetify, no PrimeVue, no shadcn-vue).
- **`@microsoft/signalr`** — the hub client.
- Built with **Vite** into the API's `wwwroot/` and served as static files — single-solution monorepo. The dev server proxies to `http://localhost:4001`.
- All HTTP goes through `src/api/client.ts` (`apiFetch`); components and stores never call `fetch` themselves.
- Optimistic UI for user actions (`createPendingValues`); reconcile or revert on API response.
- ESLint + Prettier: `npm --prefix web/reflekt-web run lint`. No frontend test runner is set up.

## Port Convention

Host ports use an `{AA}XY` scheme:

- `{AA}` — 2-digit app prefix: **40**.
- `X` — protocol slot (tens digit): **0** = HTTP, the only slot used.
- `Y` — service index (units digit).

Host port is derived mechanically: `<app prefix> + <protocol slot> + <service index>`.

### Service indices

| Index | Service | HTTP |
|-------|---------|------|
| 1     | Reflekt | `4001` |

- Locally the app runs on `http://localhost:4001` (`launchSettings.json`, Vite proxy).
- HTTPS and container ports are not applicable: there is no HTTPS profile and no container. Slots 1–9 stay unused.
- Production runs on Azure App Service (`deploy.ps1`, `docs/deployment-azure.md`), which assigns its own ports.
- **Services do not duplicate port numbers in their own `CLAUDE.md`.** Each service records only its service index; the ports derive from this table.

## Guardrails — DO NOT

- **No entity-wrapper service classes** (e.g. `ArticleService`, `SprintService`) — classes that accumulate business logic around a single entity. Focused operation services scoped to a feature area (e.g. `CardRegrouping`, `FocusHandover`) are allowed.
- **No repository interfaces** — single implementation, interfaces add zero value. Other components (cross-cutting contracts such as `IDomainEvent`, `IHasDomainEvents`) do use interfaces where contracts are needed.
- **No god folders inside a service project** (`Services/`, `Helpers/`, `Utils/` in `Reflekt.API`, `Reflekt.Domain` or `Reflekt.Persistence`). The solution-level `src/Services/` and the test project's `Helpers/` are not service-project folders.
- **No bypassing domain rules** via EF configs or endpoints.
- **Domain events** = within service boundary. **Integration events** = cross-service.
- **No `.gitkeep` files** — don't track empty directories. Directories are created at runtime or when files are added.
- **No secrets in commits or artifacts.** No agent may commit, include, or reference files containing secrets (`.env`, `credentials.json`, API keys, connection strings, tokens). If a file appears to contain secrets, warn the user instead of proceeding.

## EF Core Migrations

Install the tool once (its version matches EF Core in `Directory.Packages.props`):

```bash
dotnet tool install --global dotnet-ef --version 10.0.13
```

From the repository root:

```bash
# Add migration
dotnet ef migrations add Name -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API

# Apply migration (rarely needed: the app applies pending migrations on every start)
dotnet ef database update -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

What `dotnet ef` needs at design time (recorded run: `docs/specs/F40-ArchitectureDocs/delivery/implementation.md` § Step 2):

- There is no `IDesignTimeDbContextFactory`; `dotnet ef` takes `ReflektDbContext` from the app's own host (`Program.cs`), in environment `Development`, with working directory `src/Services/Reflekt/Reflekt.API`.
- It builds the host without starting it, so no Google credentials are needed and no start-up migration or seeding runs.
- The connection string `Data Source=./data/reflekt.db` is relative to that working directory.
