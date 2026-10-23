# Reflekt — Architecture (as built)

**Status:** Current — describes the code as it is. Replaces [`v1.md`](v1.md), which stays as the decision log.
**Checked against:** commit `82a791f` (F35–F39 merged on the branch). Counts below come from the source, not from v1.
**Map:** [`index.md`](index.md) is the one-page summary; this file is the reference.

Patterns a skill covers are referenced, not restated: each section names its skill and what the skill, not this document, owns. The section list follows the system as built, not the `create-architecture-doc` template (see `docs/conventions/known-deviations.md` § Accepted deviations).

## Skill inventory

Every folder in `.claude/skills/` (37). Disposition: **Referenced** — a section below points to it; **Not used by this app** — a pattern for a stack this app does not have (template skill); **Workflow** — a pipeline or process skill with no architecture area.

| Skill | Covers | Disposition |
|---|---|---|
| add-integration-event | MassTransit integration events | Not used by this app |
| add-pipeline-behavior | MediatR pipeline behaviours | Not used by this app |
| analytics-computation-service | analytics metric services of another app | Not used by this app |
| authorization-patterns | role gate + resource gate over user roles | Not used by this app (no roles; see § 9) |
| boy-scout | small in-file cleanups | Workflow |
| cqrs-patterns | MediatR commands, queries, behaviours | Not used by this app |
| create-aggregate | new aggregate, behaviour split, events, EF config, repository | Referenced (§ 2) |
| create-architecture-doc | this document's inventory and reference rule | Referenced (this file) |
| create-building-blocks-package | new `Blocks.{Name}` package | Referenced (§ 1) |
| create-domain-event-handler | handler per effect, in its feature folder | Referenced (§ 3) |
| create-feature | endpoint + command/query + validator + mapping | Referenced (§ 4) |
| create-feature-spec | feature specs | Workflow |
| create-grpc-contract | gRPC code-first contracts | Not used by this app |
| create-implementation-plan | plans with skill mapping | Workflow |
| create-module | module skeleton | Not used by this app |
| create-module-claude-md | module `CLAUDE.md` | Not used by this app |
| create-service | new service skeleton, Dockerfile | Not used by this app |
| create-service-claude-md | new service `CLAUDE.md` | Not used by this app |
| create-vue-feature | frontend slice: types, API module, store, view, route | Referenced (§ 12) |
| diagnose | debugging protocol | Workflow |
| domain-patterns | aggregates, value objects, events, state machines | Referenced (§§ 2–3) |
| error-handling | exception family, guard, middleware | Referenced (§ 11) |
| extract-endpoint-types | endpoint + sibling `*Command.cs`/`*Query.cs` | Referenced (§ 4) |
| extract-feature-service | focused operation services | Referenced (§ 4) |
| frontend-review | frontend review checklist | Workflow |
| improve-architecture | architecture improvement discovery | Workflow |
| improve-flow | lesson promotion to flow files | Workflow |
| improve-skills | skill creation and fixes | Workflow |
| persistence-patterns | repositories, configurations, DbContext, seed, interceptors | Referenced (§§ 3, 10) |
| pinia-patterns | Pinia setup stores | Referenced (§ 12) |
| redis-patterns | Redis.OM persistence | Not used by this app |
| service-registration | per-layer DI classes, start-up order | Referenced (§ 6) |
| system-design | designing new apps and services, variant axes | Workflow |
| tailwind-theme | Tailwind v4 tokens, dark mode | Referenced (§ 12) |
| tdd | red-green-refactor loop | Workflow |
| vue-component-architecture | component levels and splits | Referenced (§ 12) |
| vue-patterns | Vue 3 Composition API | Referenced (§ 12) |

Totals: 16 Referenced, 11 Not used by this app, 10 Workflow.

## 1. Solution layout and project reference graph

`Reflekt.slnx`, 9 projects, all `net10.0` (`Directory.Build.props`: nullable and implicit usings on):

```
src/
  BuildingBlocks/
    Blocks.Core/          Guard (Guard.NotFound)
    Blocks.Domain/        AggregateRoot, Entity, IDomainEvent, IHasDomainEvents, DomainException
    Blocks.Exceptions/    HttpException + BadRequest/Unauthorized/Forbidden/NotFound/Conflict
    Blocks.Persistence/   ApplicationDbContext, DomainEventPublishingInterceptor, EntityConfiguration, RepositoryBase
  Services/Reflekt/
    Reflekt.API/          Features/ (12 slices), Hubs/, Auth/, Middleware/, Mappings/, OpenGraph/, Program.cs, wwwroot/ (built SPA)
    Reflekt.Domain/       one folder per aggregate: state file + Behaviors/ + Events/
    Reflekt.Persistence/  ReflektDbContext, EntityConfigurations/, Repositories/, Migrations/, DbInitializer, SeedOptions
tests/
  Reflekt.UnitTests/
  Reflekt.IntegrationTests/
web/reflekt-web/          Vue 3 SPA (built into Reflekt.API/wwwroot)
```

One service, one deployable. No modules, API gateway, message broker or gRPC.

Project references (from each `*.csproj`):

```
Reflekt.API ──▶ Reflekt.Domain ──▶ Blocks.Domain
     │ ──▶ Reflekt.Persistence ──▶ Reflekt.Domain
     │                        └──▶ Blocks.Persistence ──▶ Blocks.Domain
     │ ──▶ Blocks.Core ──▶ Blocks.Exceptions
     └ ──▶ Blocks.Exceptions
Reflekt.UnitTests ──▶ Reflekt.API, Reflekt.Domain, Blocks.Domain
Reflekt.IntegrationTests ──▶ Reflekt.API
```

`Blocks.Domain` references the `FastEndpoints` package for `IEvent`; `Reflekt.Persistence` takes the ASP.NET Core shared framework (`FrameworkReference Microsoft.AspNetCore.App`).

**Skill reference:** create-building-blocks-package. **Not specified here:** how a new building-block package is scaffolded and registered.

## 2. Domain: aggregates, phase and state machines, phase-action gating

**7 aggregate roots** (`: AggregateRoot`, int ids): `Card`, `CardGroup`, `Participant`, `Sprint`, `Team`, `User`, `Vote`. Six are partial classes — state in `{Area}/{Name}.cs`, behaviour in `{Area}/Behaviors/{Name}.cs`; `User` is one file.
**Child entity:** `EvidenceLink` (owned by `Card`, created only through it).
**Value object:** `ColumnColor`.
**Reference data:** `PhaseAction` (table `PhaseActions`, key `Phase` + `Action`).
**Enums:** `SprintPhase` (Writing, Grouping, Voting, Discussing, Reporting), `SprintState` (Draft, Active, Closed), `SprintAction` (16 actions), `CardType`, `ColumnType`, `EvidenceLinkSource`, `FetchStatus`, `ReviewStatus`.

**State machine** (`Sprint`): `Start` Draft → Active; `Close` Active → Closed (stamps `ClosedAt`). The other operations check the state per operation, before acting:
- **Active only** — `EnsureActive`: close, advance phase and its check, focus and clear focus, toggle discussed, join (both join endpoints), kick. `SetReviewStatus` checks Active and the Writing phase itself.
- **Not Closed (Draft allowed)** — `EnsureOpen`: cards (create, edit, delete), evidence links (attach, detach), groups (create, add a card, remove a card, rename, dissolve), votes (cast, retract), columns (`UpdateColumns`; `Sprint.UpdateColumnConfig` itself has no state check), and the hub's timer methods. The phase-action gating below still applies on top.
**Phase machine** (`Sprint.AdvancePhase`): Writing → Grouping → Voting → Discussing → Reporting, no way back; advancing past Reporting throws. Reaching Reporting needs at least 2 discussed groups (`EnsureEnoughGroupsDiscussed`). Focus (`SetFocus`, `ClearFocus`) is Discussing-only.

**Phase-action gating:** which `SprintAction` each phase allows is data — 21 rows seeded by `HasData` in `PhaseActionConfiguration`:

| Phase | Allowed actions |
|---|---|
| Writing (6) | CreateNote, EditNote, DeleteNote, AttachEvidenceLink, DetachEvidenceLink, AdvancePhase |
| Grouping (4) | GroupCards, UngroupCards, RenameGroup, AdvancePhase |
| Voting (3) | CastVote, RetractVote, AdvancePhase |
| Discussing (8) | CreateActionItem, EditActionItem, DeleteActionItem, AttachEvidenceLink, DetachEvidenceLink, AdvancePhase, FocusGroup, ToggleDiscussed |
| Reporting (0) | — |

`PhaseActionPolicy` (domain) answers `IsAllowed` and `EnsureAllowed` (throws `DomainException` → 409). `PhaseActionPolicyProvider` (API singleton) loads the rows once and caches the policy; the 16 phase-gated endpoints (cards, groups, votes, evidence links, advance phase) call `EnsureAllowed` before acting, and `GET /api/phase-actions` serves the table to the SPA.

**Skill reference:** domain-patterns, create-aggregate. **Not specified here:** the aggregate base, the state/behaviour split, factory versus `required init` creation, value-object shape — owned by the skills (and the root `CLAUDE.md` Domain section).

## 3. Domain events and their dispatch

**25 domain events** (records, `: IDomainEvent`). What an event carries — the rule and its one exception — is stated in the root `CLAUDE.md` § Domain events. Per aggregate: CardGroup 6, Card 7, Participant 2, Sprint 7, Team 1, Vote 2.

**22 handlers** (`IEventHandler<T>`), one per event, each in the feature folder it serves (`Features/{Slice}/{Feature}/Broadcast…Handler.cs`); nearly all broadcast over SignalR (§ 8), and two do more: `BroadcastAndFetchOnEvidenceLinkAttachedHandler` also queues the fetch (§ 5), `BroadcastAndRestartTimerOnPhaseAdvancedHandler` also restarts the timer. **No handler:** `SprintCreatedEvent`, `SprintStartedEvent`, `TeamCreatedEvent`.

**Dispatch** — `DomainEventPublishingInterceptor` (`Blocks.Persistence`, scoped, added to the `DbContext`):
- **Collect:** before the save, it takes the events of every tracked aggregate and clears them.
- **Transaction-aware hold:** after the save, if an explicit transaction is open, the events are held; they are published when that transaction commits, and dropped on rollback, on failure, or when the next transaction starts.
- **Awaited:** with no open transaction, publication runs inside `SavedChangesAsync` and is awaited before the save returns; on commit, inside `TransactionCommittedAsync`, awaited by the commit. Events go out one at a time in collected order, each with `PublishAsync(Mode.WaitForAll)`, so the endpoint's response follows its broadcasts.
- **Failures logged, never rethrown:** a failing handler (or a cancelled publication) is logged with `LogError`; the save, the commit and the request still succeed.
- **Token:** each publication is started with `CancellationToken.None`, so the save's token never reaches it — a client that aborts after the commit does not stop the broadcast. What token a handler then receives is FastEndpoints' own and is not promised here.

Handlers that read data take `IServiceScopeFactory` and open a scope per call (FastEndpoints resolves handlers as singletons).

**Skill reference:** create-domain-event-handler, domain-patterns, persistence-patterns (interceptors). **Not specified here:** handler naming and placement, the event-on-aggregate pattern. Where the dispatch differs from `domain-patterns`, it is listed in `known-deviations.md` § Accepted deviations.

## 4. Feature slices and endpoints

FastEndpoints, one class per endpoint, route and policy declared by attributes on the class (`[HttpGet("…")]` …, `[Authorize(Policy = …)]` or `[AllowAnonymous]`). Request, response and validator live in the slice's sibling `*Command.cs` / `*Query.cs`; types shared by several slices sit in the area's `*Contracts.cs`.

**12 slices, 44 endpoints.** Policies: SignedInUser 24, SignedInUserOrGuest 10, Guest 5, AllowAnonymous 5.

| Slice | Endpoint | Verb | Route | Policy |
|---|---|---|---|---|
| Account | Login | GET | /auth/google/login | AllowAnonymous |
| Account | Logout | POST | /auth/logout | SignedInUser |
| Account | Me | GET | /auth/me | SignedInUser |
| CardGroups | AddCardToGroup | POST | /api/sprints/{sprintId}/card-groups/{groupId}/cards | SignedInUser |
| CardGroups | ClearFocus | DELETE | /api/sprints/{sprintId}/focus | SignedInUser |
| CardGroups | CreateGroup | POST | /api/sprints/{sprintId}/card-groups | SignedInUser |
| CardGroups | DissolveGroup | DELETE | /api/sprints/{sprintId}/card-groups/{groupId} | SignedInUser |
| CardGroups | FocusGroup | POST | /api/sprints/{sprintId}/card-groups/{groupId}/focus | SignedInUser |
| CardGroups | GetGroups | GET | /api/sprints/{sprintId}/card-groups | SignedInUserOrGuest |
| CardGroups | RemoveCardFromGroup | DELETE | /api/sprints/{sprintId}/card-groups/{groupId}/cards/{cardId} | SignedInUser |
| CardGroups | ToggleDiscussed | POST | /api/sprints/{sprintId}/card-groups/{groupId}/toggle-discussed | SignedInUser |
| CardGroups | UpdateGroupTitle | PATCH | /api/sprints/{sprintId}/card-groups/{groupId} | SignedInUser |
| Cards | CreateCard | POST | /api/sprints/{sprintId}/cards | Guest |
| Cards | DeleteCard | DELETE | /api/sprints/{sprintId}/cards/{cardId} | SignedInUserOrGuest |
| Cards | EditCard | PUT | /api/sprints/{sprintId}/cards/{cardId} | SignedInUserOrGuest |
| Cards | ListCards | GET | /api/sprints/{sprintId}/cards | SignedInUserOrGuest |
| EvidenceLinks | AttachEvidenceLink | POST | /api/sprints/{sprintId}/cards/{cardId}/evidence-links | SignedInUserOrGuest |
| EvidenceLinks | DetachEvidenceLink | DELETE | /api/sprints/{sprintId}/cards/{cardId}/evidence-links/{evidenceLinkId} | SignedInUserOrGuest |
| Health | Health | GET | /health | AllowAnonymous |
| Participants | GetCurrentParticipant | GET | /api/sprints/{sprintId}/participants/me | Guest |
| Participants | GetParticipants | GET | /api/sprints/{sprintId}/participants | SignedInUserOrGuest |
| Participants | JoinSprint | POST | /api/sprints/{sprintId}/join | AllowAnonymous |
| Participants | JoinSprintGoogle | POST | /api/sprints/{sprintId}/join/google | SignedInUser |
| Participants | KickParticipant | DELETE | /api/sprints/{sprintId}/participants/{participantId} | SignedInUser |
| PhaseActions | GetPhaseActions | GET | /api/phase-actions | AllowAnonymous |
| PreviousActions | GetPreviousActions | GET | /api/sprints/{sprintId}/previous-actions | SignedInUserOrGuest |
| PreviousActions | SetReviewStatus | PATCH | /api/teams/{teamId}/sprints/{sprintId}/previous-actions/{cardId} | SignedInUser |
| Reports | GetParticipantReport | GET | /api/sprints/{sprintId}/report | SignedInUserOrGuest |
| Reports | GetSprintReport | GET | /api/teams/{teamId}/sprints/{sprintId}/report | SignedInUserOrGuest |
| Sprints | AdvancePhaseCheck | POST | /api/teams/{teamId}/sprints/{sprintId}/advance-phase/check | SignedInUser |
| Sprints | AdvancePhase | POST | /api/teams/{teamId}/sprints/{sprintId}/advance-phase | SignedInUser |
| Sprints | CloseSprint | POST | /api/teams/{teamId}/sprints/{sprintId}/close | SignedInUser |
| Sprints | CreateSprint | POST | /api/teams/{teamId}/sprints | SignedInUser |
| Sprints | GetSprint | GET | /api/teams/{teamId}/sprints/{sprintId} | SignedInUser |
| Sprints | GetSprintSummary | GET | /api/sprints/{sprintId}/summary | AllowAnonymous |
| Sprints | GetTeamSprints | GET | /api/teams/{teamId}/sprints | SignedInUser |
| Sprints | StartSprint | POST | /api/teams/{teamId}/sprints/{sprintId}/start | SignedInUser |
| Sprints | UpdateColumns | PUT | /api/sprints/{sprintId}/columns | SignedInUser |
| Teams | CreateTeam | POST | /api/teams | SignedInUser |
| Teams | DeleteTeam | DELETE | /api/teams/{teamId} | SignedInUser |
| Teams | GetMyTeams | GET | /api/teams | SignedInUser |
| Votes | CastVote | POST | /api/sprints/{sprintId}/card-groups/{groupId}/votes | Guest |
| Votes | GetMyVotes | GET | /api/sprints/{sprintId}/votes/mine | Guest |
| Votes | RetractVote | DELETE | /api/sprints/{sprintId}/card-groups/{groupId}/votes | Guest |

**Two URL schemes** live side by side: team-scoped `/api/teams/{teamId}/sprints…` (9 routes, the scrum master's sprint management) and sprint-scoped `/api/sprints/{sprintId}/…` (27 routes, the retro room). Both stay (accepted deviation).

**Feature services** (scoped, named for the operation): `CardRegrouping`, `FocusHandover` (CardGroups), `TeamLimitCheck` (Teams/CreateTeam), `VoteBudget` (Votes/CastVote), `SprintReportBuilder` (Reports). Single-caller services sit in their feature's folder. `CardBroadcast` and `WritingPhaseRedaction` (Cards) are shared static helpers. `TeamLimitCheck` counts the owner's teams, then the endpoint inserts — two concurrent creates can pass the count (accepted deviation).

**Skill reference:** create-feature (FastEndpoints variant), extract-endpoint-types, extract-feature-service. **Not specified here:** the endpoint file layout, validator wiring, the result-record pattern of a feature service.

## 5. Evidence-link fetch pipeline

A card link is classified, saved, broadcast and — unless it is Jira — enriched with OpenGraph metadata in the background.

1. **Source detection** — `EvidenceLinkSourceDetector` (`Features/EvidenceLinks/`): host `github.com` → GitHub; host equal to the configured `EvidenceLinks:JiraDomain` → Jira, with the first `ABC-123` key in the path; any host with a `/browse/{KEY}` path → Jira; anything else (or an invalid URL) → Generic. `EvidenceLinks:JiraDomain` is unset by default.
2. **Jira short-circuit** — `EvidenceLink.Create` gives a Jira link `FetchStatus.Ok` at once: it is never fetched. Every other link starts `Pending`.
3. **Broadcast and queue** — after the save, `BroadcastAndFetchOnEvidenceLinkAttachedHandler` sends `CardUpdated` and, for a `Pending` link, writes a `FetchJob` to the bounded channel built in `OpenGraphConfiguration`: `Channel.CreateBounded<FetchJob>(100)` with `FullMode = Wait`.
4. **Queue full** — the write then waits for space. Because publication is awaited inside the save (§ 3), the attach request itself waits while the queue holds 100 jobs (accepted deviation).
5. **Background fetch** — `OpenGraphBackgroundService` (hosted) reads the channel one job at a time, in a new scope per job, calls `OpenGraphFetcher`, stores title (≤ 500), description (≤ 1000), image and icon with `Ok`, or `Failed` with no metadata, and saves — which raises `EvidenceLinkFetchedEvent` → `EvidenceLinkFetched` to clients. A failed job is logged; there is no retry.
6. **Fetcher limits** — `HttpClient` `"opengraph"`: 5-second timeout, 2 MB response buffer; `http`/`https` only; success status only; HTML parsed with AngleSharp (`og:title`, `og:description`, `og:image`, icon). **SSRF guard:** the handler's `ConnectCallback` resolves the host and connects only if every address passes `OpenGraphAddressPolicy` (private, loopback, link-local, carrier-grade NAT, multicast, broadcast and unspecified addresses refused, IPv4-mapped IPv6 checked as IPv4) — applied to every connection, redirects included.

**Skill reference:** None (see Gaps).

## 6. Cross-cutting

**Start-up** (`Program.cs`): `AddReflektPersistence` → `AddReflektApi` → `Build()` → `MigrateAndSeedAsync` (§ 10) → `UseReflektApi` → `RunAsync`.

**Middleware pipeline** — `UseReflektApi` (`Reflekt.API/DependencyInjection.cs`), in order:
1. `GlobalExceptionMiddleware`
2. `UseDefaultFiles`, `UseStaticFiles` (the SPA in `wwwroot`)
3. `UseRouting`
4. `UseAuthentication`, `UseAuthorization`
5. `MapFastEndpoints` (validation failures use the middleware's error body)
6. `MapHub<SprintHub>("/hubs/sprint")`
7. Development only: `MapOpenApi`, `MapScalarApiReference`
8. `MapFallbackToFile("index.html")`

**Registration** — `AddReflektApi`: FastEndpoints, SignalR, Mapster (`TypeAdapterConfig.GlobalSettings.Scan`; 5 `IRegister` profiles in `API/Mappings/`), OpenAPI, options, authentication, authorization, OpenGraph, then the services. No FastEndpoints pre- or post-processors. `AddReflektPersistence`: the interceptor, the `DbContext` and 8 repositories (all scoped).

**Options and config keys** (all `ValidateOnStart`):

| Options class | Section | Keys | Rule |
|---|---|---|---|
| `AuthSettings` | `Auth` | `Auth:Google:ClientId`, `Auth:Google:ClientSecret`, `Auth:DevFrontendOrigin` | both Google values non-empty |
| `TeamSettings` | `TeamSettings` | `TeamSettings:MaxTeamsPerUser` (10) | ≥ 1 |
| `SeedOptions` | `Seed` | `Seed:Enabled` (false), `Seed:SeedUserEmail` (`dev@localhost`) | — |
| `EvidenceLinkSettings` | `EvidenceLinks` | `EvidenceLinks:JiraDomain` (null) | — |

Connection string: `ConnectionStrings:Reflekt` = `Data Source=./data/reflekt.db`. There is one `appsettings.json`; secrets come from user secrets or environment variables (`Auth__Google__ClientId`, `Auth__Google__ClientSecret`).

**Singletons that hold state** (in process memory — see the single-instance constraint, § 8): `TimerService` (per-sprint timers), `ReadinessTracker` (readiness, connections per sprint, connection registry), `PhaseActionPolicyProvider` (cached policy), the `FetchJob` channel reader and writer, `OpenGraphFetcher`.

**Skill reference:** service-registration. **Not specified here:** which layer registers what, lifetimes, the `DependencyInjection` class shape.

## 7. Packages

Versions are central in `Directory.Packages.props` (`ManagePackageVersionsCentrally`); no project names a version.

| Package | Version | Used by |
|---|---|---|
| FastEndpoints | 5.34.0 | Blocks.Domain, Blocks.Persistence, Reflekt.API |
| FluentValidation | 11.11.0 | Reflekt.API |
| Mapster | 10.0.0 | Reflekt.API |
| Microsoft.AspNetCore.Authentication.Google | 10.0.13 | Reflekt.API |
| Microsoft.AspNetCore.OpenApi | 10.0.13 | Reflekt.API |
| Scalar.AspNetCore | 2.5.3 | Reflekt.API |
| AngleSharp | 1.8.4 | Reflekt.API |
| Microsoft.EntityFrameworkCore | 10.0.13 | Blocks.Persistence |
| Microsoft.EntityFrameworkCore.Relational | 10.0.13 | Blocks.Persistence |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.13 | Reflekt.Persistence |
| Microsoft.EntityFrameworkCore.Design | 10.0.13 | Reflekt.API (private assets) |
| Microsoft.Data.Sqlite | 10.0.13 | Reflekt.IntegrationTests |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.13 | Reflekt.IntegrationTests |
| FluentAssertions | 7.2.0 | both test projects |
| Microsoft.NET.Test.Sdk | 17.14.1 | both test projects |
| xunit | 2.9.3 | both test projects |
| xunit.runner.visualstudio | 3.1.4 | both test projects |
| coverlet.collector | 6.0.4 | both test projects |

**Shared framework, not packages:** SignalR, cookie authentication, `System.Threading.Channels`, hosting and options come with `Microsoft.AspNetCore.App` (the API's Web SDK; `Reflekt.Persistence`'s framework reference).

**Frontend** — `web/reflekt-web/package.json` ranges:
- dependencies: `@microsoft/signalr` ^10.0.0, `lucide-vue-next` ^1.0.0, `pinia` ^3.0.4, `vue` ^3.5.32, `vue-router` ^5.0.4;
- devDependencies: `@tailwindcss/vite` ^4.2.2, `tailwindcss` ^4.2.2, `vite` ^8.0.8, `@vitejs/plugin-vue` ^6.0.6, `vite-plugin-vue-devtools` ^8.1.1, `typescript` ~6.0.0, `vue-tsc` ^3.2.6, `@vue/tsconfig` ^0.9.1, `@tsconfig/node24` ^24.0.4, `@types/node` ^24.12.2, `npm-run-all2` ^8.0.4; plus the ESLint and Prettier dev-dependencies behind the `lint` script (`eslint.config.js`, `.prettierrc.json`);
- `engines.node`: `^20.19.0 || >=22.12.0`.

No UI kit, no frontend test runner.

## 8. Real-time

One hub, `SprintHub`, at `/hubs/sprint`, authorized by the `GuestToken` scheme only: every connection is a sprint participant (each participant, guest or Google, holds a guest token, sent as `access_token` by the SPA's `accessTokenFactory`).

**Client → server (8 methods):** `JoinSprint`, `LeaveSprint`, `ToggleReady` (each `sprintId`), `StartTimer`, `PauseTimer`, `ResumeTimer`, `ResetTimer` (each `sprintId`), `AddTime(sprintId, minutes)` with minutes 1, 2 or 5. Timer methods are for the scrum master only: the participant's user (or the signed-in cookie user) must own the team. The SPA calls six; `StartTimer` and `LeaveSprint` exist but are not called (a stopped timer starts on join and on phase advance).

**Server → client (26 messages)**, the same set on both ends (server sends, `useSprintHub.ts` registrations):

| Area | Messages |
|---|---|
| Cards | `CardCreated`, `CardUpdated`, `CardDeleted`, `EvidenceLinkFetched` |
| Groups | `GroupCreated`, `GroupTitleChanged`, `GroupDissolved`, `CardsGrouped`, `CardUngrouped`, `GroupFocused`, `GroupFocusCleared`, `GroupDiscussedChanged` |
| Votes | `VoteCast`, `VoteRetracted` |
| Participants | `ParticipantJoined`, `ParticipantKicked`, `ParticipantLeft` |
| Sprint | `PhaseChanged`, `SprintClosed`, `ColumnConfigUpdated`, `ReviewStatusChanged` |
| Readiness | `ReadinessChanged`, `ReadinessReset`, `ReadinessSnapshot` |
| Timer | `TimerUpdated`, `TimerSnapshot` |

**Groups and per-participant delivery:** `JoinSprint` adds the connection to `sprint:{sprintId}` and `participant:{participantId}`. Most messages go to the sprint group. During Writing, card messages go per participant (`CardBroadcast`): the author's group gets the full card, every other participant's group a redacted copy (text, assignee and links blanked). The same `WritingPhaseRedaction` rule applies to `ListCards`, `GetGroups`, the report, and `EvidenceLinkFetched`.

**Readiness:** `ReadinessTracker` keeps who is ready per sprint. `ToggleReady` → `ReadinessChanged`; a joining client gets `ReadinessSnapshot`; a phase advance sends `ReadinessReset`. On a disconnect, `ReadinessTracker.Disconnect` drops the connection, and `ReadinessChanged` (not ready) is sent only when it was the participant's last connection to the sprint and the participant was ready; `ParticipantLeft` is sent for every disconnect of a connection that carries participant claims, even when another connection of the same participant remains.

**Timers:** `TimerService` keeps one timer per sprint (`Stopped`, `Running`, `Paused`, `Expired`), defaulting to the sprint's per-phase minutes. Changes go out as `TimerUpdated`; a joining client gets `TimerSnapshot`; expiry is broadcast from a `System.Threading.Timer` (a failed send is logged).

**Single-instance constraint:** timers, readiness, the connection registry and the fetch queue live in one process's memory, and there is no SignalR backplane. The app must run as one instance (accepted deviation).

**Skill reference:** create-domain-event-handler (the broadcasting handlers). **Not specified here:** handler placement. The hub, groups, redaction and in-memory state have no skill (see Gaps).

## 9. Auth

**Sign-in:** any Google account may sign in. No allowlist exists, and no code reads an email list.

**Schemes:**
- `Cookies` — the default; `SameSite=Lax`, `HttpOnly`; an unauthenticated API call gets 401 and a forbidden one 403 instead of a redirect.
- `Google` — scopes `openid`, `email`, `profile`; on the ticket, the `User` is created or updated by email and the cookie's `NameIdentifier` becomes the user id. In Development, `Login` redirects back to `Auth:DevFrontendOrigin` (the Vite dev server); elsewhere to a same-site return path.
- `GuestToken` — `GuestTokenAuthenticationHandler` finds the participant by token and issues claims `participantId`, `sprintId`, `displayName`.

**Policies** (`Auth/Policies.cs`): `SignedInUser` (Cookies), `Guest` (GuestToken), `SignedInUserOrGuest` (either). There are no roles.

**Access service:** `SprintAccess` (scoped) loads the sprint and team for the caller and returns a `SprintAccessResult` the handler checks: `RequireSprint` / `RequireTeam` (404), `RequireOwner` (403 — the scrum master owns the team), `RequireGuestOf(sprintId)` (401), `RequireParticipant` (401). The policy is the endpoint-level gate; `SprintAccess` is the resource-level gate, called inside the handler.

**Guest-token transports**, tried in this order: header `X-Guest-Token`; query `access_token` on `/hubs/*`; `Authorization: Bearer …`. The SPA's API client sets the header; the hub client sends the query value.

**Soft kick:** `KickParticipant` sets the participant's `RemovedAt`; the row stays. A removed participant's token fails authentication; on an open connection, the hub rechecks active participation in `JoinSprint` (before and after registering the connection), `ToggleReady` and the five timer methods (through the scrum-master check), while `LeaveSprint` only cleans up, with no check; and the kick handler sends `ParticipantKicked`, removes the participant's connections from both groups, aborts them and updates readiness.

**Skill reference:** None for this shape — `authorization-patterns` describes a role-based gate this app does not have (see Gaps). **Not specified here:** nothing beyond the above.

## 10. Persistence

SQLite through EF Core 10. `ReflektDbContext` (over `ApplicationDbContext`) has 9 DbSets: `Users`, `Teams`, `Sprints`, `Participants`, `Cards`, `CardGroups`, `Votes`, `EvidenceLinks`, `PhaseActions`; one configuration class per entity (9); 8 concrete repositories (no interfaces): seven, one per aggregate, over `RepositoryBase`, which wraps `SaveChangesAsync`, and the read-only `PhaseActionRepository`, a plain class that only loads the phase-action rows.

**Migrations:** 16, from `20260419224520_AddTeam` to `20261023061022_SoftKickParticipants`. They run **on every start**: `MigrateAndSeedAsync` creates the SQLite file's folder, then `Database.MigrateAsync`.

**Seeding:** only when the environment is Development **and** `Seed:Enabled` is true (default false); it seeds for `Seed:SeedUserEmail`. The phase-action rows are model data (`HasData`), applied by migrations in every environment.

**Design time:** `dotnet ef` builds the app's host without starting it — no design-time factory and no Google credentials are needed, and no migration or seeding runs (recorded run: `docs/specs/F40-ArchitectureDocs/delivery/implementation.md` § Step 2).

**Skill reference:** persistence-patterns. **Not specified here:** repository and configuration shapes, the interceptor pattern, seed-data conventions.

## 11. Error convention

Handlers throw; `GlobalExceptionMiddleware` maps the exception to a status and, for every row of the table below except cancellation, writes one JSON body. Two kinds of response carry a status only, with no body: a cancelled request (499), and the authentication challenge and forbid responses (401, 403) — the cookie scheme's `OnRedirectToLogin` and `OnRedirectToAccessDenied` callbacks set the status instead of redirecting and write nothing, and `GuestTokenAuthenticationHandler` keeps the framework's default challenge and forbid, which write no body either.

| Exception | Status |
|---|---|
| `DomainException` (a business rule refused the change) | 409 |
| `HttpException` family — `BadRequestException` 400, `UnauthorizedException` 401, `ForbiddenException` 403, `NotFoundException` 404, `ConflictException` 409 | its own |
| `ValidationException`, and FastEndpoints validation failures | 400, with `errors` |
| `ArgumentException` | 400 |
| `OperationCanceledException` | 499 (if the response has not started), no body |
| anything else | 500, logged; the message is generic outside Development |

**Body** (camelCase): `statusCode`, `message`, `traceId`; `code` when the exception carries one; `errors` as `[{ field, message }]`; `details` (stack trace) in Development only.

**`code` set** (`Middleware/ErrorCodes.cs`): `duplicate_name`, `limit_reached` — both from creating a team.

"Not found" is an exception (`Guard.NotFound`, `NotFoundException`), not a result value.

**Skill reference:** error-handling. **Not specified here:** the exception hierarchy pattern and guard style. Where the code differs from the skill (409 for domain rules, the body shape), see `known-deviations.md` § Accepted deviations.

## 12. Frontend

Vue 3 + TypeScript SPA in `web/reflekt-web`, built by Vite into `Reflekt.API/wwwroot` and served by the API; the dev server proxies to `http://localhost:4001`.

- **Routes (6):** `/` welcome · `/teams` team dashboard · `/teams/:teamId/sprints` sprint list · `/teams/:teamId/sprints/:sprintId` sprint · `/sprint/:sprintId` retro room (guests) · `/teams/:teamId/sprints/:sprintId/report` report. The guard waits for `fetchMe()` on first load and sends routes marked `requiresAuth` to welcome.
- **Views (6)**, **Pinia stores (13):** auth, card, cardGroup, guest, participant, policy, previousActions, readiness, report, sprint, teams, timer, vote; beside them in `src/stores/`, the `pendingValues.ts` helper (`createPendingValues`, no `defineStore`). **Composables (10):** useCopyReport, useReadiness, useRetroBoardActions, useRetroPhase, useRetroRoom, useSprintHub, useTheme, useTimer, useTimerActions, useToast.
- **API layer** (`src/api/`): `client.ts` holds `apiFetch` (credentials always included; sets `X-Guest-Token` — the only place that does), `ApiError`, `isHttpError` and `describeApiError`; `hub.ts` holds the hub `invoke`; one module per area (auth, cardGroups, cards, participants, policy, previousActions, reports, sprints, teams, votes). Components and stores never call `fetch` or build the guest header themselves.
- **Local storage** (`src/storage/`): the guest token per sprint (`reflekt:sprint:{sprintId}:token`), the theme, the timer mute.
- **Optimistic rules (F39):** a user change is applied at once through `createPendingValues` (card, cardGroup, previousActions, readiness stores) — one pending value per key, oldest first. Success confirms the change and every older one, and the server's value becomes the base; failure undoes only that change, so a newer change keeps its value; an authoritative hub payload settles the key. Stores never toast: they revert and reject, and the view or composable reports the error through `describeApiError` and the toast.
- **Styling:** Tailwind v4 tokens (light and dark themes); `index.html` sets the background in a `<head>` style block before first paint.
- **Lint:** ESLint + Prettier, `npm --prefix web/reflekt-web run lint`.

**Skill reference:** create-vue-feature, pinia-patterns, vue-patterns, vue-component-architecture, tailwind-theme. **Not specified here:** slice structure, store shape, component levels, token naming. Where the code differs from these skills, see `known-deviations.md` § Accepted deviations.

## 13. Deployment

Production is one Azure App Service (Linux). `deploy.ps1 -AppName … -ResourceGroup …` builds the SPA into `wwwroot`, runs `dotnet publish -c Release`, zips the output and deploys it with `az webapp deploy --type zip --clean true`. The app needs WebSockets on (SignalR), the connection string pointing at a persistent path (`/home/data/reflekt.db`), and `Auth__Google__ClientId` / `Auth__Google__ClientSecret` as app settings. Locally it runs on `http://localhost:4001` (port convention in the root `CLAUDE.md`). Full guide: `docs/deployment-azure.md`.

**Skill reference:** None (see Gaps).

## 14. Testing

Practice and conventions: `docs/conventions/testing.md`. As it stands:
- `tests/Reflekt.UnitTests` — 63 tests (59 `[Fact]`, 4 `[Theory]`) over the aggregates, the source detector and the aggregate base.
- `tests/Reflekt.IntegrationTests` — 104 `[Fact]` through `WebApplicationFactory<Program>` on a SQLite file per test, with the shared helpers `ReflektApiFactory`, `TestAuthHandler`, `TestDataSeeder` and `TestGoogleCredentials`; parallelization is off (FastEndpoints keeps process-wide state).
- xUnit 2.x and FluentAssertions 7.2.0; no mocking library.

**Gaps:** no frontend tests; no test drives the SignalR hub end to end; no test carries a `Speed` trait, so the `Speed!=Slow` filter in `.claude/verify.json` runs every unit test.

**Skill reference:** tdd (the loop only). **Not specified here:** the red-green-refactor workflow.

## Gaps

Areas with no project skill; the inline detail above is the reference until one exists.

| Area | Section | What a skill would own |
|---|---|---|
| SignalR hub, groups, per-participant redaction, in-memory hub state | § 8 | hub method guards, group naming, redacted fan-out |
| Bounded background queue and outbound fetch with SSRF guard | § 5 | channel + hosted service, `HttpClient` limits, address policy |
| Scheme-based auth without roles (cookie + guest token, access service) | § 9 | policies per scheme, the resource-gate service |
| Single App Service deployment | § 13 | publish and deploy steps |
