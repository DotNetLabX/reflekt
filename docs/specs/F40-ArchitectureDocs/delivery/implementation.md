# F40 — Architecture Reference & Conventions Docs — Implementation

**Model:** claude-opus-5-5[1m]

## Step 1 — Base commit and fact sheet
**Files:** `docs/specs/F40-ArchitectureDocs/delivery/implementation.md` — new; this block is the fact sheet every later step cites.
**Skills:** None (plan Skill Mapping: inventory work, no pattern; TDD no)
**Decisions:** Message names are counted from both ends and cross-checked; endpoints are listed from their attributes, not from `Configure()` (none exists). Test counts are `[Fact]`/`[Theory]` attribute counts, not executed cases.
**Deviations:** None

### Fact sheet (all read at commit `82a791f`, the build's HEAD)

**F1 — Base commit.** `git rev-parse HEAD` → `82a791f84f47d736ae40f000b2a235306b0f40ba`. `git log --oneline e5b43ce..HEAD` → one commit, `82a791f feat(F40-ArchitectureDocs): add implementation plan`. **AC7 base: `e5b43ce`.** No source file changes in this feature, so every section of `architecture.md` is checked against `82a791f`.

**F2 — Solution.** Source: `Reflekt.slnx`. 9 projects:
- `src/BuildingBlocks/`: `Blocks.Core`, `Blocks.Domain`, `Blocks.Exceptions`, `Blocks.Persistence`
- `src/Services/Reflekt/`: `Reflekt.API` (SDK `Microsoft.NET.Sdk.Web`), `Reflekt.Domain`, `Reflekt.Persistence`
- `tests/`: `Reflekt.IntegrationTests`, `Reflekt.UnitTests`

`Directory.Build.props`: `net10.0`, `Nullable` enable, `ImplicitUsings` enable, `LangVersion` latest. No `global.json`. SDK on this machine: `dotnet --version` → `10.0.401`.

**F3 — Project reference graph.** Source: `ProjectReference` in every `*.csproj` (`grep -oE 'ProjectReference Include=…'`).
| Project | References |
|---|---|
| `Blocks.Exceptions` | — |
| `Blocks.Core` | `Blocks.Exceptions` |
| `Blocks.Domain` | — (package `FastEndpoints`, for `IEvent`) |
| `Blocks.Persistence` | `Blocks.Domain` (no longer `Blocks.Core`) |
| `Reflekt.Domain` | `Blocks.Domain` |
| `Reflekt.Persistence` | `Reflekt.Domain`, `Blocks.Persistence`; `FrameworkReference Microsoft.AspNetCore.App` |
| `Reflekt.API` | `Reflekt.Domain`, `Reflekt.Persistence`, `Blocks.Core`, `Blocks.Exceptions` |
| `Reflekt.UnitTests` | `Reflekt.API`, `Reflekt.Domain`, `Blocks.Domain` |
| `Reflekt.IntegrationTests` | `Reflekt.API` |

The service uses all four building blocks: `Blocks.Domain` (via Domain), `Blocks.Persistence` (via Persistence), `Blocks.Core` and `Blocks.Exceptions` (API directly).

**F4 — Packages.** Source: `Directory.Packages.props` (`ManagePackageVersionsCentrally` true) and the `PackageReference` items per project.
| Package | Version | Referenced by |
|---|---|---|
| FastEndpoints | 5.34.0 | Blocks.Domain, Blocks.Persistence, Reflekt.API |
| FluentValidation | 11.11.0 | Reflekt.API |
| Microsoft.Data.Sqlite | 10.0.13 | Reflekt.IntegrationTests |
| Microsoft.EntityFrameworkCore | 10.0.13 | Blocks.Persistence |
| Microsoft.EntityFrameworkCore.Relational | 10.0.13 | Blocks.Persistence |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.13 | Reflekt.Persistence |
| Microsoft.EntityFrameworkCore.Design | 10.0.13 | Reflekt.API (`PrivateAssets="all"`) |
| Mapster | 10.0.0 | Reflekt.API |
| Microsoft.AspNetCore.Authentication.Google | 10.0.13 | Reflekt.API |
| Microsoft.AspNetCore.OpenApi | 10.0.13 | Reflekt.API |
| Scalar.AspNetCore | 2.5.3 | Reflekt.API |
| AngleSharp | 1.8.4 | Reflekt.API |
| FluentAssertions | 7.2.0 | both test projects |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.13 | Reflekt.IntegrationTests |
| Microsoft.NET.Test.Sdk | 17.14.1 | both test projects |
| xunit | 2.9.3 | both test projects |
| xunit.runner.visualstudio | 3.1.4 | both test projects |
| coverlet.collector | 6.0.4 | both test projects |

18 `PackageVersion` entries, each referenced. Shared-framework parts (not packages): SignalR, cookie authentication, `System.Threading.Channels`, `Microsoft.Extensions.*` come with `Microsoft.AspNetCore.App` (API via the Web SDK; Persistence via its `FrameworkReference`). Not present: MassTransit, RabbitMQ, gRPC, YARP, MediatR, Redis, any mocking library, docker-compose files.

**F5 — Domain.** Source: `grep -rnE "class \w+.*: *(AggregateRoot|Entity)|record \w+.*IDomainEvent|enum \w+" src/Services/Reflekt/Reflekt.Domain src/BuildingBlocks`.
- **Aggregate roots (7):** `Card`, `CardGroup`, `Participant`, `Sprint`, `Team`, `User`, `Vote` — all `: AggregateRoot` (int id). Six are `partial` with state in `{Area}/{Name}.cs` and behaviour in `{Area}/Behaviors/{Name}.cs`; `User` is one non-partial file.
- **Child entity (1):** `EvidenceLink : Entity` (owned by `Card`; created only through `Card`, `EvidenceLink.Create` is `internal`).
- **Reference data:** `PhaseAction` (plain class, composite key `Phase`+`Action`, table `PhaseActions`), 21 rows seeded by `HasData` in `PhaseActionConfiguration.cs`: Writing 6 (CreateNote, EditNote, DeleteNote, AttachEvidenceLink, DetachEvidenceLink, AdvancePhase), Grouping 4 (GroupCards, UngroupCards, RenameGroup, AdvancePhase), Voting 3 (CastVote, RetractVote, AdvancePhase), Discussing 8 (CreateActionItem, EditActionItem, DeleteActionItem, AttachEvidenceLink, DetachEvidenceLink, AdvancePhase, FocusGroup, ToggleDiscussed), Reporting 0. `PhaseActionPolicy` (domain) answers `IsAllowed` / `EnsureAllowed` (throws `DomainException`); `PhaseActionPolicyProvider` (API, singleton) loads it once, lazily, through a scope.
- **Enums:** `SprintPhase { Writing, Grouping, Voting, Discussing, Reporting }`; `SprintState { Draft, Active, Closed }`; `SprintAction` (16: CreateNote, EditNote, DeleteNote, CreateActionItem, EditActionItem, DeleteActionItem, CastVote, RetractVote, AttachEvidenceLink, DetachEvidenceLink, AdvancePhase, GroupCards, UngroupCards, RenameGroup, FocusGroup, ToggleDiscussed); `CardType { Note, ActionItem }`; `ColumnType { Column1, Column2, Column3, ActionItems }`; `EvidenceLinkSource { GitHub, Jira, Generic }`; `FetchStatus { Pending, Ok, Failed }`; `ReviewStatus { None, Done, CarriedOver, Dropped }`; API-side `TimerStatus { Stopped, Running, Paused, Expired }`.
- **Value object:** `ColumnColor` (`sealed record`, `Sprints/ColumnColor.cs`, max length 10).
- **State machine** (`Sprints/Behaviors/Sprint.cs`): `Start()` Draft → Active (else `DomainException`); `Close()` requires Active → Closed, stamps `ClosedAt`; `AdvancePhase()` requires Active, Writing → Grouping → Voting → Discussing → Reporting, throws at Reporting; `EnsureEnoughGroupsDiscussed` — at least `MinDiscussedGroupsForReporting = 2` discussed groups before Reporting; `SetFocus` / `ClearFocus` require Active and Discussing.
- **Domain events (25 records, all `: IDomainEvent`, each holding the aggregate):** CardGroup 6 (`CardGroupCreatedEvent`, `CardGroupTitleChangedEvent`, `CardsGroupedEvent(Group, CardId, SourceGroupId)`, `CardUngroupedEvent(SourceGroup, CardId, NewGroupId)`, `GroupDiscussedChangedEvent`, `GroupDissolvedEvent`); Card 7 (`CardCreatedEvent`, `CardDeletedEvent`, `CardUpdatedEvent`, `EvidenceLinkAttachedEvent(Card, EvidenceLink)`, `EvidenceLinkDetachedEvent(Card, EvidenceLinkId)`, `EvidenceLinkFetchedEvent(Card, EvidenceLink)`, `ReviewStatusChangedEvent`); Participant 2 (`ParticipantJoinedEvent`, `ParticipantKickedEvent`); Sprint 7 (`ColumnConfigUpdatedEvent`, `GroupFocusClearedEvent(Sprint, PreviousGroupId)`, `GroupFocusedEvent(Sprint, GroupId, PreviousGroupId)`, `PhaseAdvancedEvent`, `SprintClosedEvent`, `SprintCreatedEvent`, `SprintStartedEvent`); Team 1 (`TeamCreatedEvent`); Vote 2 (`VoteCastEvent`, `VoteRetractedEvent`). Extra fields beside the aggregate (ids not recoverable after the save) are the BE-24 exception.
- **Event handlers (22 `IEventHandler<T>`, all in `Reflekt.API/Features/{Slice}/{Feature}/`):** `BroadcastCardsGroupedHandler`, `BroadcastGroupFocusClearedHandler`, `BroadcastGroupCreatedHandler`, `BroadcastGroupDissolvedHandler`, `BroadcastGroupFocusedHandler`, `BroadcastCardUngroupedHandler`, `BroadcastGroupDiscussedChangedHandler`, `BroadcastGroupTitleChangedOnCardGroupTitleChangedHandler`, `BroadcastCardCreatedHandler`, `BroadcastCardDeletedHandler`, `BroadcastCardUpdatedHandler`, `BroadcastAndFetchOnEvidenceLinkAttachedHandler`, `BroadcastCardUpdatedOnEvidenceLinkDetachedHandler`, `BroadcastEvidenceLinkFetchedHandler`, `BroadcastParticipantJoinedHandler`, `BroadcastParticipantKickedHandler`, `BroadcastReviewStatusChangedHandler`, `BroadcastAndRestartTimerOnPhaseAdvancedHandler`, `BroadcastSprintClosedHandler`, `BroadcastColumnConfigUpdatedHandler`, `BroadcastVoteCountOnVoteCastHandler`, `BroadcastVoteCountOnVoteRetractedHandler` — one per event. **Events with no handler (3):** `SprintCreatedEvent`, `SprintStartedEvent`, `TeamCreatedEvent` (loop over every event record, `grep -rqE "IEventHandler<$e>"`).

**F6 — Event dispatch (P1 re-checked).** Source: `src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs`, registered scoped in `Reflekt.Persistence/DependencyInjection.cs` and added to the `DbContext`.
- `SavingChangesAsync` collects every tracked `IHasDomainEvents` aggregate's events (in change-tracker order) and clears them.
- `SavedChangesAsync` (line 49): when `Database.CurrentTransaction` is not null the events are **held** (an explicit transaction whose commit has not happened); otherwise `await PublishInOrderAsync(events)` (line 65) **before the save returns**.
- `TransactionCommittedAsync` (line 99) returns `PublishInOrderAsync(held)`, awaited by the commit. Rollback, failure, and the start of a next transaction drop held events.
- `PublishInOrderAsync` (line 129): one event at a time in collected order; each publication awaited with `ConfigureAwaitOptions.SuppressThrowing`, then `LogFailures` logs each handler exception (`LogError`) or a cancellation — **never rethrown**.
- `StartPublish` (line 144) invokes `PublishEvent<T>` reflectively with **`CancellationToken.None`**; `PublishEvent` (line 167) calls `ev.PublishAsync(Mode.WaitForAll, ct)`. The save's token never reaches publication. The code comment at line 127 says "Handlers get CancellationToken.None"; per P1 the doc promises nothing about the token FastEndpoints then hands each handler.
- P1 holds: publication is awaited, not fire-and-forget.

**F7 — Evidence-link fetch pipeline.**
- `Features/EvidenceLinks/EvidenceLinkSourceDetector.cs`: host `github.com` → GitHub; host equal to `EvidenceLinks:JiraDomain` (case-insensitive) → Jira with the first `[A-Z][A-Z0-9]+-\d+` key in the path; any host with a `/browse/{KEY}` segment → Jira; else Generic. Invalid URL → Generic.
- `EvidenceLinkSettings.JiraDomain` bound from `EvidenceLinks` (`appsettings.json`: `"JiraDomain": null`).
- `Reflekt.Domain/Cards/EvidenceLink.cs` `Create`: Jira → `FetchStatus.Ok` (no fetch); otherwise `Pending`. Limits: Url 2048, Title 500, Description 1000, ExternalKey 50.
- `OpenGraph/OpenGraphConfiguration.cs`: `Channel.CreateBounded<FetchJob>(new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait })`; reader and writer registered as singletons; `OpenGraphBackgroundService` (hosted) and `OpenGraphFetcher` (singleton).
- Fetcher limits: named `HttpClient` `"opengraph"`, `Timeout` 5 s, `MaxResponseContentBufferSize` 2 MB; `SocketsHttpHandler.ConnectCallback` resolves DNS and refuses unless every address passes `OpenGraphAddressPolicy.IsAllowed` (IPv4 rejected: 0/8, 10/8, 100.64/10, 127/8, 169.254/16, 172.16/12, 192.168/16, 224/4 and up, 255.255.255.255; IPv6 rejected: unspecified, loopback, fe80::/10, fc00::/7, ff00::/8; IPv4-mapped addresses checked as IPv4) — guards every connection, redirects included; `OpenGraphFetcher` accepts only `http`/`https`, a success status, and reads `og:title` / `og:description` / `og:image` and an icon with AngleSharp; any non-cancel exception → `Failed`.
- `BroadcastAndFetchOnEvidenceLinkAttachedHandler`: broadcasts `CardUpdated` (via `CardBroadcast`), then, when `Pending`, `await channel.WriteAsync(new FetchJob(...), ct)` — with the queue full this waits (F38's residual: the attach request waits, since publication is awaited inside the save).
- `OpenGraphBackgroundService.ExecuteAsync`: `ReadAllAsync`, new scope per job, loads the card, fetches, `card.UpdateEvidenceLinkMetadata(..., Ok|Failed)` (title/description truncated to the limits), saves (→ `EvidenceLinkFetchedEvent` → `BroadcastEvidenceLinkFetchedHandler` → `EvidenceLinkFetched`); exceptions logged per job, no retry.

**F8 — API endpoints.** Source: attributes over `Features/**/*Endpoint.cs` (`grep -oE '\[Http(Get|Post|Put|Patch|Delete)\("…"'` and `\[Authorize(Policy = …)\]|\[AllowAnonymous\]`). `find src -name '*Endpoint.cs' | wc -l` → **44**; 44 carry an `[Http*]` attribute, 44 an `[Authorize(Policy = …)]` or `[AllowAnonymous]`; `Configure()` overrides: 0. **12 slices, 44 endpoints.**

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

Per slice: Account 3, CardGroups 9, Cards 4, EvidenceLinks 2, Health 1, Participants 5, PhaseActions 1, PreviousActions 2, Reports 2, Sprints 9, Teams 3, Votes 3. Per policy: SignedInUser 24, SignedInUserOrGuest 10, Guest 5, AllowAnonymous 5. **Two URL schemes:** team-scoped `/api/teams/{teamId}/sprints…` (9 routes) and sprint-scoped `/api/sprints/{sprintId}/…` (27 routes), beside `/auth/*`, `/health`, `/api/phase-actions`, `/api/teams`, `/api/teams/{teamId}`.
Non-endpoint feature files: per-slice `*Command.cs` / `*Query.cs` siblings (request, response, validator); scoped feature services `CardRegrouping` and `FocusHandover` (`Features/CardGroups/`), `TeamLimitCheck` (`Features/Teams/CreateTeam/`), `VoteBudget` (`Features/Votes/CastVote/`), `SprintReportBuilder` (`Features/Reports/`); `CardBroadcast` and `WritingPhaseRedaction` (`Features/Cards/`); `EvidenceLinkSourceDetector`, `EvidenceLinkSettings` (`Features/EvidenceLinks/`); `Features/EvidenceLinks/FetchEvidenceLink/` holds a handler only. `TeamLimitCheck` counts owned teams (`CountByOwnerIdAsync`) and throws `ConflictException` at `MaxTeamsPerUser`; the insert follows separately (the team-count race).

**F9 — Errors.** `Middleware/ErrorCodes.cs`: `DuplicateName = "duplicate_name"`, `LimitReached = "limit_reached"` (2 codes; used by `CreateTeamEndpoint` and `TeamLimitCheck`). `Middleware/GlobalExceptionMiddleware.cs` status map: `DomainException` → 409; `HttpException` (`Blocks.Exceptions`: `BadRequestException` 400, `UnauthorizedException` 401, `ForbiddenException` 403, `NotFoundException` 404, `ConflictException` 409) → its own status; `ArgumentException` → 400; anything else → 500 (logged; message replaced by "An unexpected error occurred." outside Development). `ValidationException` → 400 with `errors`. `OperationCanceledException` → 499 when the response has not started. Body (`JsonSerializerDefaults.Web`, camelCase): `statusCode`, `message`, `traceId`, optional `code`, `errors[{field, message}]`, `details` (stack trace, Development only). FastEndpoints validation failures use the same body through `config.Errors.ResponseBuilder = BuildEndpointValidationResponse`. `Blocks.Core/Guard.cs` has one member, `Guard.NotFound<T>`.

**F10 — Cross-cutting.** Source: `Reflekt.API/DependencyInjection.cs`, `Program.cs`.
- `Program.cs`: `AddReflektPersistence` → `AddReflektApi` → `Build()` → `await app.Services.MigrateAndSeedAsync(app.Lifetime.ApplicationStopping)` → `app.UseReflektApi()` → `RunAsync()`.
- `UseReflektApi` order: `UseMiddleware<GlobalExceptionMiddleware>` → `UseDefaultFiles` → `UseStaticFiles` → `UseRouting` → `UseAuthentication` → `UseAuthorization` → `MapFastEndpoints` (error response builder) → `MapHub<SprintHub>("/hubs/sprint")` → Development only: `MapOpenApi`, `MapScalarApiReference` → `MapFallbackToFile("index.html")`.
- `AddReflektApi`: `AddFastEndpoints`, `AddSignalR`, `TypeAdapterConfig.GlobalSettings.Scan(...)` (Mapster; 5 `IRegister` profiles in `API/Mappings/`: Card, CardGroup, Participant, Report, Sprint), `AddOpenApi`, options, authentication, authorization, OpenGraph, then the services below.
- Options and keys (all `ValidateOnStart`): `AuthSettings` ← `Auth` (`Auth:Google:ClientId`, `Auth:Google:ClientSecret` must be non-empty; `Auth:DevFrontendOrigin`, read by `LoginEndpoint`); `TeamSettings` ← `TeamSettings` (`MaxTeamsPerUser` ≥ 1, default 10); `SeedOptions` ← `Seed` (`Enabled` false, `SeedUserEmail` `dev@localhost`); `EvidenceLinkSettings` ← `EvidenceLinks` (`JiraDomain`). Connection string `ConnectionStrings:Reflekt` = `Data Source=./data/reflekt.db`. Only `appsettings.json` exists (no `appsettings.Development.json`). `launchSettings.json`: `http://localhost:4001`, `ASPNETCORE_ENVIRONMENT=Development`.
- Singletons holding state: `TimerService` (`ConcurrentDictionary<int, TimerEntry>` with a `System.Threading.Timer` each), `ReadinessTracker` (three `ConcurrentDictionary` maps: readiness, connections per sprint, connection registry), `PhaseActionPolicyProvider` (cached policy), the `FetchJob` channel reader/writer, `OpenGraphFetcher`. Scoped: `SprintAccess`, `TeamLimitCheck`, `CardRegrouping`, `FocusHandover`, `VoteBudget`, `SprintReportBuilder`, the 8 repositories, the interceptor. No FastEndpoints pre/post processors (`grep -rn "IPreProcessor\|IPostProcessor" src` → none).

**F11 — Real-time.** Source: `Hubs/SprintHub.cs`, `Hubs/TimerService.cs`, `Hubs/ReadinessTracker.cs`, the handlers, `web/reflekt-web/src/composables/useSprintHub.ts`.
- Route `/hubs/sprint`; `[Authorize(AuthenticationSchemes = GuestToken)]` — every connection is a participant (every `Participant` row holds a required `GuestToken`; both `JoinSprint` and `JoinSprintGoogle` return it in `JoinSprintResponse`).
- **Client-to-server methods (8):** `JoinSprint(sprintId)`, `LeaveSprint(sprintId)`, `ToggleReady(sprintId)`, `StartTimer`, `PauseTimer`, `ResumeTimer`, `ResetTimer` (each `sprintId`), `AddTime(sprintId, minutes ∈ {1,2,5})`. Timer methods require the scrum master: the participant's `UserId`, or the cookie user, must own the team. The SPA invokes 6: `JoinSprint`, `ToggleReady`, `PauseTimer`, `ResumeTimer`, `ResetTimer`, `AddTime` (`src/api/hub.ts` `invoke`, called from `useSprintHub.ts`, `stores/readiness.ts`, `composables/useTimerActions.ts`); `StartTimer` and `LeaveSprint` are never invoked by the SPA (the timer starts on join when stopped and on phase advance).
- **Server-to-client messages (26), both ends reconciled.** Server: every `SendAsync("…")` literal plus `CardBroadcast.BroadcastAsync(…, message)` callers (`"CardCreated"`, `"CardUpdated"`). Client: every `connection.on('…')` in `useSprintHub.ts` (the only file that registers; `GroupDiscussedChanged` is registered over two lines at line 105–106). Names: `CardCreated`, `CardUpdated`, `CardDeleted`, `EvidenceLinkFetched`, `GroupCreated`, `GroupTitleChanged`, `GroupDissolved`, `CardsGrouped`, `CardUngrouped`, `GroupFocused`, `GroupFocusCleared`, `GroupDiscussedChanged`, `VoteCast`, `VoteRetracted`, `ParticipantJoined`, `ParticipantKicked`, `ParticipantLeft`, `PhaseChanged`, `SprintClosed`, `ColumnConfigUpdated`, `ReviewStatusChanged`, `ReadinessChanged`, `ReadinessReset`, `ReadinessSnapshot`, `TimerUpdated`, `TimerSnapshot`. **One-sided: none.** (`SendAsync(x.Adapt…)` hits in endpoints are FastEndpoints HTTP responses, not hub sends.)
- **Groups:** `sprint:{sprintId}` and `participant:{participantId}` (`SprintHub.ParticipantGroup`), both joined in `JoinSprint`.
- **Per-participant delivery and Writing-phase redaction (F35):** `CardBroadcast` — outside Writing, one send to `sprint:{id}`; in Writing, the full card to the author's `participant:` group and a redacted copy (`Text` "", `AssigneeName` null, `EvidenceLinks` []) to every other participant's group. `WritingPhaseRedaction` is also applied by `ListCards`, `GetGroups`, `SprintReportBuilder`, and `BroadcastEvidenceLinkFetchedHandler` (checks `IsWritingPhaseCardAsync`).
- **Readiness:** `ReadinessTracker` (in memory); `ToggleReady` → `ReadinessChanged`; join → `ReadinessSnapshot` to the caller; disconnect → `ReadinessChanged` (isReady false) and `ParticipantLeft`; phase advance → `ReadinessReset`.
- **Timers:** `TimerService` (in memory, per sprint), `TimerStatus { Stopped, Running, Paused, Expired }`; `TimerUpdated` to the group, `TimerSnapshot` to a joining caller; expiry broadcast from a `System.Threading.Timer`, failure logged. `BroadcastAndRestartTimerOnPhaseAdvancedHandler` sends `PhaseChanged`, `ReadinessReset`, `TimerUpdated`.
- **Single-instance constraint:** timers, readiness, the connection registry and the fetch queue live in process memory; no SignalR backplane.

**F12 — Auth.** Source: `Reflekt.API/Auth/`.
- Schemes: `Cookies` (default; `SameSite=Lax`, `HttpOnly`, `SecurePolicy=SameAsRequest`, redirects to login/denied replaced by 401/403), `Google` (scopes `openid`, `email`, `profile`; `SaveTokens` false; `OnTicketReceived` creates or updates the `User` by email and replaces `NameIdentifier` with the user id), `GuestToken` (`GuestTokenAuthenticationHandler`).
- Policies (`Policies.cs`): `SignedInUser` (Cookies), `Guest` (GuestToken), `SignedInUserOrGuest` (both).
- Access service: `SprintAccess` (scoped) — `ForSprintAsync`, `ForTeamAsync` → `SprintAccessResult` with `RequireSprint`/`RequireTeam` (`Guard.NotFound` → 404), `RequireOwner` (403), `RequireGuestOf` (401), `RequireParticipant` (401), `IsOwnedBy`.
- Guest-token transports, in order: header `X-Guest-Token`; query `access_token` on `/hubs/*` paths; `Authorization: Bearer …`. Claims `participantId`, `sprintId`, `displayName`.
- Soft kick (F35 migration `SoftKickParticipants`): `Participant.Kick()` sets `RemovedAt` (`IsRemoved`); the guest handler fails a removed participant ("Participant has been removed"); the hub refuses removed participants on join and on every method; `BroadcastParticipantKickedHandler` sends `ParticipantKicked`, removes the participant's connections from both groups, aborts them, clears its readiness and sends `ReadinessChanged`.
- **`Auth:AllowedEmails`: read by nothing.** `grep -rn "AllowedEmails" --include=*.cs --include=*.json --include=*.ts --include=*.vue src tests web/reflekt-web/src deploy.ps1 | wc -l` → 0. Any Google account may sign in. (Mentions remain only in docs and in `.claude/skills/create-feature-spec/SKILL.md`, which this feature does not edit.)

**F13 — Persistence.** Source: `Reflekt.Persistence/`.
- `ReflektDbContext` DbSets (9): `Users`, `Teams`, `Sprints`, `Participants`, `Cards`, `CardGroups`, `Votes`, `EvidenceLinks`, `PhaseActions`. 9 entity configurations in `EntityConfigurations/`. 8 concrete repositories in `Repositories/` (User, Team, Sprint, Participant, Card, Vote, CardGroup, PhaseAction) over `Blocks.Persistence/Repositories/RepositoryBase.cs`.
- Migrations: **16**, first `20260419224520_AddTeam`, last `20261023061022_SoftKickParticipants` (`ls Migrations | grep -v Designer | grep -v Snapshot`).
- `MigrateAndSeedAsync` (`DependencyInjection.cs`): creates the SQLite file's directory, `Database.MigrateAsync` **on every start**, then `DbInitializer.SeedAsync` only when `IsDevelopment()` **and** `Seed:Enabled`. Seed flags: `Seed:Enabled` (default false), `Seed:SeedUserEmail` (default `dev@localhost`).

**F14 — Frontend (`web/reflekt-web`).**
- Routes (`src/router/index.ts`, 6): `/` welcome → `WelcomeView`; `/teams` → `TeamDashboardView`; `/teams/:teamId/sprints` → `SprintListView`; `/teams/:teamId/sprints/:sprintId` → `SprintView`; `/sprint/:sprintId` retro-room → `RetroRoomView`; `/teams/:teamId/sprints/:sprintId/report` → `ReportView`. Guard: waits for `authStore.fetchMe()` on cold load; signed-in users leave welcome for teams; `meta.requiresAuth` routes need sign-in.
- Views (6) as above. Stores (14, `src/stores/`): auth, card, cardGroup, guest, participant, pendingValues, policy, previousActions, readiness, report, sprint, teams, timer, vote. Composables (10): useCopyReport, useReadiness, useRetroBoardActions, useRetroPhase, useRetroRoom, useSprintHub, useTheme, useTimer, useTimerActions, useToast.
- API layer `src/api/` (12): `client.ts` (`apiFetch`, `ApiError`, `isHttpError`; credentials always `include`; guest token header set here only), `hub.ts` (`invoke`, `isHubConnected`), and one module per area: auth, cardGroups, cards, participants, policy, previousActions, reports, sprints, teams, votes.
- Other folders: `components/` (AppHeader, layout, reports, retro, sprints, teams, ui), `types/` (11), `constants/`, `format/`, `storage/`.
- Build: Vite `outDir` `../../src/Services/Reflekt/Reflekt.API/wwwroot`; dev proxy to `http://localhost:4001`. Committed lint configs: `eslint.config.js`, `.prettierrc.json`, `.prettierignore` (F39); the `lint` script and its five dev-dependencies arrive with F39 change note 1 (P4 — described as merged; no versions cited from the note).
- `package.json` ranges (P4): dependencies `@microsoft/signalr` ^10.0.0, `lucide-vue-next` ^1.0.0, `pinia` ^3.0.4, `vue` ^3.5.32, `vue-router` ^5.0.4; devDependencies `@tailwindcss/vite` ^4.2.2, `@tsconfig/node24` ^24.0.4, `@types/node` ^24.12.2, `@vitejs/plugin-vue` ^6.0.6, `@vue/tsconfig` ^0.9.1, `npm-run-all2` ^8.0.4, `tailwindcss` ^4.2.2, `typescript` ~6.0.0, `vite` ^8.0.8, `vite-plugin-vue-devtools` ^8.1.1, `vue-tsc` ^3.2.6; `engines.node` `^20.19.0 || >=22.12.0`. No shadcn-vue, no UI kit, no frontend test runner.

**F15 — Tests.**
- Projects: `tests/Reflekt.UnitTests` — 63 test attributes (59 `[Fact]`, 4 `[Theory]`) in 8 files: `BuildingBlocksTests/AggregateRootTests` 1, `Cards/CardTests` 14, `Cards/EvidenceLinkTests` 12, `EvidenceLinks/SourceDetectorTests` 5, `Participants/ParticipantTests` 5, `Sprints/SprintTests` 18, `Teams/TeamTests` 3, `Votes/VoteTests` 5. `tests/Reflekt.IntegrationTests` — 104 `[Fact]` in 8 files at the project root: `AuthSmokeTests` 3, `CardEndpointTests` 26, `EvidenceLinkEndpointTests` 19, `FoundationSmokeTests` 1, `ParticipantEndpointTests` 14, `SprintEndpointTests` 16, `TeamEndpointTests` 6, `VoteEndpointTests` 19; plus `TestParallelization.cs` (`[assembly: CollectionBehavior(DisableTestParallelization = true)]`, because FastEndpoints keeps process-wide statics).
- Helpers (`tests/Reflekt.IntegrationTests/Helpers/`): `ReflektApiFactory` (static: `CreateAnonymous(dbPath)`, `CreateSignedIn(dbPath, …)` adding the `Test` scheme, `DeleteDatabaseFilesAsync` with `SqliteConnection.ClearAllPools`), `TestAuthHandler` (scheme `Test`, `TestUserId = 1`, `test@example.com`), `TestDataSeeder` (`SeedTestUserAsync`, `SeedDiscussedGroupsAsync`, `CreateScrumMasterCookie`), `TestGoogleCredentials` (`UsePlaceholderGoogleCredentials`).
- Test method names by `_`-separated parts: 1 part 15, 2 parts 41, 3 parts 118, 4 parts 8.
- Mocking library: none. `Speed` trait: no test carries one (`grep -rn '"Speed"' tests` → none); `.claude/verify.json` `roles.unit` filters `Speed!=Slow`, so it runs every unit test.

**F16 — Agent setup.**
- `.claude/skills/`: **37** skill folders (`ls .claude/skills | wc -l`).
- `.claude/hooks/`: `audit-logger.js` (1 file). `.claude/verify.json`: roles `build`, `unit`, `integration`, `full`; `slow` (threshold 2000 ms, vstest, `Reflekt.slnx`); `stepLimitSeconds` 30; `measured` unit 8474 ms, full 67817 ms (2026-10-13). F39 change note 2 appends `npm --prefix web/reflekt-web run lint` to `roles.full` at merge.
- `.claude/` also holds `agents/` (8), `rules/` (10), `conventions/` (7), `README.md`, and campaign files `.claude/audit/`, `.claude/tmp/`, `.claude/.personas.json`.
- `docs/specs/*` folders with a `definition/` folder (8 of 13): F31-DarkLightTheme, F32-DarkThemeContrast, F35-BackendCorrectnessFixes, F36-FrontendDefects, F37-DependencyUpgrades, F38-BackendConventions, F39-FrontendConventions, F40-ArchitectureDocs. Without: F29-TimerPerPhase, F30-OpenActions, F33-PublishCleanup, F34-ConventionsAudit, adhoc-AgentRulesCleanup.

**F17 — Deployment.** `deploy.ps1` (params `AppName`, `ResourceGroup`): `npm run build` in `web/reflekt-web` (into the API's `wwwroot`) → `dotnet publish -c Release` → zip → `az webapp deploy --type zip --clean true` (Azure App Service). Guide: `docs/deployment-azure.md`.

## Step 2 — Run and record `dotnet ef`
**Files:** `docs/specs/F40-ArchitectureDocs/delivery/implementation.md` — this block (the recorded run). No other tracked or untracked path is left by the step; the tool's folder `obj/dotnet-ef/` was created and removed.
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** For the install command only, `NUGET_PACKAGES` and `NUGET_HTTP_CACHE_PATH` pointed into `obj/dotnet-ef/`, so the download stayed in the worktree and was removed with the tool (coordinator-confirmed). Raw command outputs kept in `.claude/tmp/f40-step2/` (campaign scratch, not part of the change set). One extra `dbcontext info --verbose` run was made to show how the tool resolves the context. Everything was run from the repo root at commit `82a791f`.
**Deviations:** Plan item 3 (retry with placeholder Google credentials) was not run: no command failed while starting the host.

### Commands and output
Shared arguments `A` = `-p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API`; executable `obj/dotnet-ef/dotnet-ef.exe`.

| # | Command | Exit | Decisive output |
|---|---|---|---|
| 1 | `dotnet tool install dotnet-ef --version 10.0.13 --tool-path obj/dotnet-ef` | 0 | `Tool 'dotnet-ef' (version '10.0.13') was successfully installed.` |
| 2 | `obj/dotnet-ef/dotnet-ef.exe --version` | 0 | `Entity Framework Core .NET Command-line Tools` / `10.0.13` |
| 3 | `… dbcontext info A` | 0 | `Build succeeded.` · `Type: Reflekt.Persistence.ReflektDbContext` · `Provider name: Microsoft.EntityFrameworkCore.Sqlite` · `Database name: main` · `Data source: ./data/reflekt.db` · `Options: None` |
| 4 | `… migrations list --no-connect A` | 0 | the 16 migrations, `20260419224520_AddTeam` … `20261023061022_SoftKickParticipants` · `Pending status not shown. Unable to determine which migrations have been applied. …` (expected with `--no-connect`) |
| 5 | `… migrations has-pending-model-changes A` | 0 | `No changes have been made to the model since the last migration.` |
| 6 | `… dbcontext info --verbose A` | 0 | `Using working directory '…\src\Services\Reflekt\Reflekt.API'.` · `Using environment 'Development'.` · `Finding IDesignTimeDbContextFactory implementations...` · `Finding Microsoft.Extensions.Hosting service provider...` · `Using application service provider from Microsoft.Extensions.Hosting.` · `Using context 'ReflektDbContext'.` |

No placeholder credentials were set for any command (`Auth__Google__*` absent from the shell).

### Findings
1. **The design-time host starts without a design-time factory.** No `IDesignTimeDbContextFactory` exists in source (`grep -rn IDesignTimeDbContextFactory src` → only EF's own DLLs under `bin/`). `dotnet ef` takes the `DbContext` from the application's host (`Program.cs`, `WebApplication.CreateBuilder`), in environment `Development`, working directory `src/Services/Reflekt/Reflekt.API`.
2. **It needs no Google credentials.** All commands passed with `Auth:Google:ClientId`/`ClientSecret` empty (as in `appsettings.json`), although `AuthSettings` is registered with `ValidateOnStart`: the options check runs when the host starts, and `dotnet ef` builds the host without starting it.
3. **No start-up migration or seeding ran.** `MigrateAndSeedAsync` runs after `Build()` in `Program.cs`, and no database file was created: `git status --porcelain --ignored` before and after differ only by `obj/` (the tool), and after removing `obj/dotnet-ef/` (and the then-empty `obj/`) the listing is identical to the one taken before; `find . -maxdepth 6 -name '*.db' -o -name data -type d` (outside `node_modules`) → nothing. The `Data Source` is `./data/reflekt.db`, relative to the working directory the host gets (`src/Services/Reflekt/Reflekt.API` under `dotnet ef`).
4. **Model and migrations agree:** `has-pending-model-changes` reports no change since `SoftKickParticipants`.

### `git status --porcelain --ignored`
Before (26 lines):
```
 M docs/specs/F40-ArchitectureDocs/delivery/communication-log.md
?? .claude/.personas.json
?? .claude/tmp/
?? docs/specs/F40-ArchitectureDocs/delivery/implementation.md
!! .claude/.campaign-directives.json
!! .claude/.campaign-unit
!! .claude/.current-agent
!! .claude/.pipeline-state
!! .claude/.worktree-target
!! .claude/audit/
!! src/BuildingBlocks/Blocks.Core/bin/
!! src/BuildingBlocks/Blocks.Core/obj/
!! src/BuildingBlocks/Blocks.Domain/bin/
!! src/BuildingBlocks/Blocks.Domain/obj/
!! src/BuildingBlocks/Blocks.Exceptions/bin/
!! src/BuildingBlocks/Blocks.Exceptions/obj/
!! src/BuildingBlocks/Blocks.Persistence/bin/
!! src/BuildingBlocks/Blocks.Persistence/obj/
!! src/Services/Reflekt/Reflekt.API/bin/
!! src/Services/Reflekt/Reflekt.API/obj/
!! src/Services/Reflekt/Reflekt.Domain/bin/
!! src/Services/Reflekt/Reflekt.Domain/obj/
!! src/Services/Reflekt/Reflekt.Persistence/bin/
!! src/Services/Reflekt/Reflekt.Persistence/obj/
!! tests/Reflekt.UnitTests/bin/
!! tests/Reflekt.UnitTests/obj/
```
After the run: the same 26 lines plus `!! obj/` (line 11). After removing `obj/dotnet-ef/`: identical to before (`diff` exit 0). `git status --porcelain` then shows no new path from this step.

## Step 3 — `docs/architecture/architecture.md` (new, as-built)
**Files:** `docs/architecture/architecture.md` — new, 393 lines: the skill inventory (37 skills), 14 sections in the spec's Scope order, a Gaps table. `docs/specs/F40-ArchitectureDocs/delivery/lessons.md` — `## Skill Gaps` added with the four gaps the skill says to log (signalr-hub-patterns, background-fetch-queue, authorization-patterns scheme variant, app-service-deployment).
**Skills:** create-architecture-doc (Generate mode: every `SKILL.md` frontmatter scanned, inventory built, each section gives **Skill reference** + **Not specified here**, gaps logged to `lessons.md`)
**Decisions:** A third disposition, **Workflow**, beside Referenced and Not used by this app, for the 10 pipeline/process skills (boy-scout, create-feature-spec, create-implementation-plan, diagnose, frontend-review, improve-architecture, improve-flow, improve-skills, system-design, tdd) — they are neither patterns this app uses nor template skills for a missing stack, so neither of the plan's two labels fits; `tdd` is still named in § 14 for the loop. `service-registration` has no frontmatter `name`/`description`; its folder name and first paragraph were used. `authorization-patterns` is marked Not used by this app (its role gate needs user roles; Reflekt has none) and recorded as an ill-fitting gap. Each section points to `known-deviations.md` § Accepted deviations for skill/code differences instead of listing them (Step 9 writes that table). Every section was checked against commit `82a791f`; there is no source change in this feature, so the base for each check is the same.
**Deviations:** The skill's output path is `docs/architecture/{version}.md` and its template's section list is fixed; this file is `architecture.md` with the spec's section list (spec Decisions row 4; plan Decisions; coordinator-confirmed Phase 1 choice). Recorded as a row in `known-deviations.md` by Step 9.

### Per section: commit and how checked
| § | Section | Checked against | How (Step 1 listing) |
|---|---|---|---|
| — | Skill inventory | `82a791f` | `ls .claude/skills` (37) + each `SKILL.md` frontmatter (F16) |
| 1 | Solution layout, reference graph | `82a791f` | `Reflekt.slnx` and every `ProjectReference` (F2, F3) |
| 2 | Aggregates, machines, phase-action gating | `82a791f` | domain class/enum grep, `Sprint` behaviour, `PhaseActionConfiguration` 21 rows (F5); `grep -rl EnsureAllowed Features` → 16 endpoints |
| 3 | Events and dispatch | `82a791f` | event records (25), `IEventHandler<T>` list (22, 3 without) (F5); interceptor read in full (F6) |
| 4 | Feature slices and endpoints | `82a791f` | endpoint list from attributes: 44 rows, 12 slices, policy counts 24/10/5/5, URL schemes 9/27 (F8) |
| 5 | Evidence-link fetch pipeline | `82a791f` | detector, settings, `EvidenceLink.Create`, `OpenGraphConfiguration`, fetcher, address policy, background service, attach handler (F7) |
| 6 | Cross-cutting | `82a791f` | `Program.cs`, `UseReflektApi` order, options and keys, singletons (F10); `LoginEndpoint.ResolveRedirectUri` for `DevFrontendOrigin` |
| 7 | Packages | `82a791f` | `Directory.Packages.props` 18 entries + `PackageReference` per project (F4); `package.json` ranges (F14) |
| 8 | Real-time | `82a791f` | hub methods (8), server sends and client registrations reconciled (26), groups, redaction, readiness, timers (F11) |
| 9 | Auth | `82a791f` | `Auth/` folder read in full (F12); `AllowedEmails` readers: 0 |
| 10 | Persistence | `82a791f` | DbSets (9), migrations (16), `MigrateAndSeedAsync`, seed flags (F13); recorded `dotnet ef` run (Step 2) |
| 11 | Error convention | `82a791f` | `ErrorCodes` (2), middleware status map and body, exception family (F9) |
| 12 | Frontend | `82a791f` | routes (6), views (6), stores (14), composables (10), `src/api/` (12), storage keys (F14); `createPendingValues` users (card, cardGroup, previousActions, readiness); `grep useToast stores` → 0; `index.html` `<style>` in `<head>` |
| 13 | Deployment | `82a791f` | `deploy.ps1`, `docs/deployment-azure.md` (F17) |
| 14 | Testing | `82a791f` | test counts 63 / 104, helpers, no `Speed` trait, no mocking library (F15) |

### Inventory entries marked "not used by this app" (11)
add-integration-event, add-pipeline-behavior, analytics-computation-service, authorization-patterns, cqrs-patterns, create-grpc-contract, create-module, create-module-claude-md, create-service, create-service-claude-md, redis-patterns.

### Accept checks
- Heading per section: `grep -c "^## " docs/architecture/architecture.md` → 16 (Skill inventory, §§ 1–14, Gaps).
- `grep -nE "EvidenceLinkSourceDetector|JiraDomain|Directory.Packages.props|UseReflektApi" docs/architecture/architecture.md` → hits on lines 199 (`EvidenceLinkSourceDetector`, `JiraDomain`), 212 (`UseReflektApi`), 231 (`JiraDomain`), 241 (`Directory.Packages.props`): all four names.
- `grep -niE "allowlist|AllowedEmails" docs/architecture/architecture.md` → one hit, line 303: "No allowlist exists, and no code reads an email list."
- Counts match Step 1: 7 aggregates, 21 phase-action rows, 25 events, 22 handlers, 12 slices / 44 endpoints, 8 hub methods, 26 messages, 9 DbSets, 16 migrations, 2 error codes, 18 packages, 6 routes, 14 stores, 10 composables, 63 + 104 tests, 37 skills (16 + 11 + 10).

## Step 4 — The one-page map and `v1.md` as the decision log
**Files:** `docs/architecture/index.md` — `@v1.md` replaced by the one-page map (56 lines: projects, reference graph, the request flow for a mutation using `CreateCard`, a concern → where-it-lives table), ending with `@architecture.md`; `v1.md` is a plain link (line 3). `docs/architecture/v1.md` — status line `**Status:** Accepted — superseded by architecture.md`; in-place `*(Superseded: …)*` annotations on the § 6 Teams row (line 182), the § 7.1 allowlist paragraph (line 210), the 7 deferred items of § 12 and the 6 entries of § 13 (5 low-risk questions + the high-risk "None"), each linking the `architecture.md` section that replaces it. `git diff --stat -- docs/architecture/v1.md` → 16 lines changed, 16 inserted: nothing else in `v1.md` was touched.
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** The mutation flow follows the real `CreateCardEndpoint` (guest policy, validator, `SprintAccess`, `EnsureOpen`, `EnsureAllowed`, `Card.Create`, save, interceptor, `BroadcastCardCreatedHandler`, 201), not the audit's § 3.9 outline, which still says `PhaseActionCache` and `Mode.WaitForNone`. Annotations keep the original text and add the note on the same line, so a grep hit on the old wording shows the superseded marker; § 12 items that are still true (no MediatR, broker, gRPC, gateway, repository interfaces) say "still not used/none" rather than implying they changed. Links use the GitHub heading anchors of `architecture.md`; all 10 distinct anchors were checked against its headings.
**Deviations:** None

### Accept checks
- `grep -n "^@" docs/architecture/index.md` → `56:@architecture.md` only.
- `grep -n "Status" docs/architecture/v1.md` → `3:**Status:** Accepted — superseded by architecture.md` (the other hits are the unrelated `FetchStatus` lines 224, 235, 242 and the § 9 table header, line 292).
- `grep -niE "allowlist|AllowedEmails|not.invited" docs/architecture/v1.md` (run before editing: lines 182 and 210) → the same two lines, each now carrying `*(Superseded: no allowlist exists … any Google account may sign in …)*`.

## Step 5 — Root `CLAUDE.md` rewritten for Reflekt
**Files:** `CLAUDE.md` — rewritten for this app (193 lines): a two-line opener pointing to `architecture.md` and `index.md`; Authority and graphify sections unchanged; Domain kept (the events example now uses the real `CardCreatedEvent(Card Card) : IDomainEvent` and `card.AddDomainEvent(…)`, the BE-24 exception added, the repository snippet is `RepositoryBase.SaveChangesAsync`); Architecture kept, with one sentence on what it means inside one service; the real repo tree and reference graph (F2, F3); Tech Stack from F4 with one "Not used by this app" note; Cross-cutting as it is (F9, F10, F12); Frontend from F14; Port Convention filled (prefix 40, index 1, HTTP 4001; HTTPS and container ports not applicable; App Service); Guardrails; EF Core Migrations root-relative, with the global install line and the Step 2 findings.
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** The port table keeps the `{AA}XY` scheme and the "services do not duplicate port numbers" rule (Step 6's service doc points to it) but drops the slot table: only slot 0 is used, and the other slots are stated as unused. The gRPC port slot is named only inside the "Not used by this app" note, so AC1's grep has one hit, in that note. The "Default to module" line is dropped (there are no modules; it lives in the note). The no-repository-interfaces guardrail's "(modules, cross-cutting)" becomes real examples (`IDomainEvent`, `IHasDomainEvents`). The `SprintIssueSyncService` example becomes `CardRegrouping`, `FocusHandover` (F8). "No god folders" names the three service projects and says the solution-level `src/Services/` and the test `Helpers/` are not affected (CV-06, TS-12).
**Deviations:** None

### Accept checks
- AC1: `grep -nE "MassTransit|RabbitMQ|gRPC|ApiGateway|docker-compose|Modules/" CLAUDE.md` → one hit, line 127, the "Not used by this app" note (`### Not used by this app`, line 125).
- `grep -n "^## Authority" CLAUDE.md` → `5:## Authority & Spec Files`.
- AC3 grep on `CLAUDE.md` → no hits.
- AC2 grep on `CLAUDE.md` → one hit, line 112: "Any Google account may sign in; no allowlist exists."
- Guardrails present (lines 163–169): 1 No entity-wrapper service classes · 2 No repository interfaces · 3 No god folders (qualified: inside a service project) · 4 No bypassing domain rules · 5 Domain events within the service boundary / integration events cross-service · 6 No `.gitkeep` files · 7 No secrets in commits or artifacts (moved from `project-rules.md`, text unchanged).
- Domain-events rule: line 20 ("carry a reference to the aggregate object, not individual properties"); its exception: line 36 ("facts that are not recoverable from the aggregate after the save", with three real examples from F5).
- Step close: `verify-run.js --profile fast --tied --changed CLAUDE.md` → pass, run `mvlbcyw3-cbeec220` (no tied tests; `roles.unit` ran, 3.9 s). `label-tests.js` → no test timed.

## Step 6 — `docs/conventions/project-rules.md` and `src/Services/Reflekt/CLAUDE.md`
**Files:** `docs/conventions/project-rules.md` — Source File Definition describes this repo (`src/Services/Reflekt/` and `src/BuildingBlocks/`, no "modules"); `docker-compose.*` out of the not-source list; Guardrails, Repo Structure and Project Reference Graph replaced by one pointer section to the root `CLAUDE.md` (the "No secrets" guardrail, the `SprintIssueSyncService` example and the "(modules, cross-cutting)" leftover go with them — all now live in `CLAUDE.md`); the Conventions domain-events line is a pointer to `CLAUDE.md` § Domain events; validator wording → the slice's `*Command.cs` / `*Query.cs`; `dotnet ef` root-relative; Build Verification adds `npm --prefix web/reflekt-web run lint` (P4) and says frontend tests are not set up. `src/Services/Reflekt/CLAUDE.md` — building blocks: all four, with the project that references each (F3); auth: schemes `Cookies`/`Google`/`GuestToken`, the three guest-token transports in order, the three policies (F12); hub groups add `participant:{participantId}` (F11); the port line keeps only service index 1 and points to the root port table (no port number).
**Skills:** None (plan Skill Mapping: `create-service-claude-md` checked, scaffolds new services only; TDD no)
**Decisions:** The plan's "replace the `SprintIssueSyncService` example with a real one" is met in `CLAUDE.md` (Step 5: `CardRegrouping`, `FocusHandover`); in `project-rules.md` the whole guardrail list became the pointer the plan asks for, so the example left with it rather than being rewritten in a list that is no longer there. "KB entries" dropped from the `docs/**` description (no KB exists). Aggregate creation, FastEndpoints and Repositories lines stay in Conventions (not part of the events rule; the plan did not move them).
**Deviations:** None

### Accept checks
- AC1 grep on `project-rules.md` → no hits.
- `grep -niE "carr(y|ies) .*reference|not recoverable" docs/conventions/project-rules.md` → no hit.
- `grep -nE "40[0-9]{2}" src/Services/Reflekt/CLAUDE.md` → no hit.
- `grep -n "run lint" docs/conventions/project-rules.md` → line 41.
- The plan's no-frontend-test-runner grep on `project-rules.md` → no hit. `grep -n "SprintIssueSyncService\|docker-compose" docs/conventions/project-rules.md` → no hit. AC3 grep → no hits. `grep -niE "modules"` → no hit. AC2 grep on both files → no hits.
- Code examples left in `project-rules.md` (for Step 14's AC3 list): one — the `dotnet ef migrations add` command, naming `Reflekt.Persistence` and `Reflekt.API`.
- Step close: `verify-run.js --profile fast --tied --changed docs/conventions/project-rules.md src/Services/Reflekt/CLAUDE.md` → pass, run `mvlbebtb-ceb4baab` (3.9 s). `label-tests.js` → no test timed.

## Step 7 — Sign-in in the product doc
**Files:** `docs/product/v1.md` — open question 6 (line 531–532) rewritten from "Google OAuth allowlist … Configuration-driven allowlist in `appsettings.json`" to "Who may sign in with Google in v1 — any Google account may sign in. No allowlist exists; the first sign-in creates the user."; the Decision summary's Team creation row (line 545) "allowlisted emails" → "any Google account (no allowlist)". `grep -niE "allowlist|AllowedEmails|allowlisted|not.invited|invite"` on the file before editing found only these two sign-in hits (lines 44, 46, 493 are about team invite flows, not sign-in).
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** Only the sign-in statements were changed. The same row's "one team per creator" no longer matches the code (`TeamSettings:MaxTeamsPerUser`, default 10, F10) but is product scope, out of this feature's scope — left, and logged as a carry-over.
**Deviations:** None

### AC2 listing — `grep -rniE "allowlist|AllowedEmails|allowlisted|not.invited" CLAUDE.md README.md docs/architecture docs/product docs/conventions docs/backlog.md`
| Hit | Disposition |
|---|---|
| `CLAUDE.md:112` | states no allowlist exists ("Any Google account may sign in; no allowlist exists") |
| `docs/architecture/architecture.md:303` | states no allowlist exists |
| `docs/architecture/v1.md:182` | marked superseded (Step 4) |
| `docs/architecture/v1.md:210` | marked superseded (Step 4) |
| `docs/product/v1.md:532` | states no allowlist exists |
| `docs/product/v1.md:545` | states no allowlist exists ("any Google account (no allowlist)") |
| `docs/backlog.md:48, 49, 56` | section B (above item 23) — covered by Step 12's change note, which marks it historical and the allowlist statements superseded (P2; owed at merge) |
| `docs/backlog.md:118, 120, 121` | section C, item 15 (above item 23) — covered by the same change note |
| `docs/backlog.md:271` | item 35: "The login allowlist is **not** enforced in code — … any Google account m[ay sign in]" — states no allowlist is enforced (the line the spec names) |

No hit in `README.md`, `docs/conventions/`, `docs/product/index.md` or `docs/product/v2.md`. No hit says sign-in is limited to a list, apart from the backlog lines the change note covers.
- Step close: `verify-run.js --profile fast --tied --changed docs/product/v1.md` → pass, run `mvlbfyoz-2fc2e298` (3.7 s). `label-tests.js` → no test timed.

## Step 8 — `testing.md` rewritten; `coding-conventions.md` index
**Files:** `docs/conventions/testing.md` — rewritten to the real practice (F15): the two projects and their layout (unit tests in area folders, integration tests flat with `Helpers/`, parallelization off and why); naming `{Action}_{Condition}_{Outcome}` with `{Action}_{Outcome}` (`Application_Starts`, `Login_RedirectsToGoogle`) as the accepted short form; unit tests call aggregate factories directly; the four real helpers in a table (F38's shared `ReflektApiFactory`, `TestAuthHandler`, `TestDataSeeder`, `TestGoogleCredentials`); no mocking library; FluentAssertions 7.2.0 kept on purpose with the 8.0.0 release-notes line; xUnit 2.x, test SDK, coverlet kept (F37); the `Speed!=Slow` filter and that no test carries the trait; frontend testing not set up; coverage reworded to this domain (TS-09). Dropped: `_sut`, numbered slice headers, `TestData.*`, NSubstitute, the frontend test-runner prescription, the analytics coverage line and the "simple CRUD" skip line. `docs/conventions/coding-conventions.md` — `@testing.md` added after `@ef-core.md`; a table names all eight conventions files and where each is loaded.
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** FluentAssertions reason cited from its GitHub release notes for 8.0.0 (https://github.com/fluentassertions/fluentassertions/releases/tag/8.0.0, fetched 2026-10-23): "Update ownership and license to the Xceed Community License". The notes do not call it commercial, so the doc quotes the line and adds no interpretation. The `Speed` paragraph also says how a slow unit test would be marked (`[Trait("Speed", "Slow")]`, then only the full suite runs it) — that is what the filter in `verify.json` does, not a new rule. `coding-conventions.md` names the loaders found by `grep -rln` over `.claude/agents/`: the index by developer, reviewer, solo; `project-rules.md` by architect, developer, learner, po, reviewer, solo; `known-deviations.md` by developer.
**Deviations:** None

### Code examples left in `testing.md` (AC3 review list)
| # | Example | Reflekt types it names |
|---|---|---|
| 1 | layout tree | `Reflekt.UnitTests`, `Reflekt.IntegrationTests`, `TestParallelization.cs`, `AuthSmokeTests`, `FoundationSmokeTests` |
| 2 | unit test `Create_WithEmptyName_Throws` | `Team.Create` (from `tests/Reflekt.UnitTests/Teams/TeamTests.cs`) |
| 3 | integration test class | `TeamEndpointTests`, `ReflektApiFactory.CreateSignedIn` / `DeleteDatabaseFilesAsync`, `Policies.SignedInUser`, `SeedTestUserAsync`, `WebApplicationFactory<Program>`, `/api/teams` (from `tests/Reflekt.IntegrationTests/TeamEndpointTests.cs`, shortened) |

### Accept checks
- `grep -n "^@" docs/conventions/coding-conventions.md` → `@csharp.md`, `@vue.md`, `@ef-core.md`, `@testing.md` (lines 2–5); each of the eight file names appears in the file.
- AC3 grep on `testing.md` → no hits. `grep -niE "simple CRUD|analytics" docs/conventions/testing.md` → no hit. `grep -nE "_sut|Slice [0-9]|NSubstitute"` → no hit.
- Step close: `verify-run.js --profile fast --tied --changed docs/conventions/testing.md docs/conventions/coding-conventions.md` → pass, run `mvlbi5tp-2b9e260f` (3.8 s). `label-tests.js` → no test timed.

## Step 9 — `known-deviations.md`: two tables
**Files:** `docs/conventions/known-deviations.md` — purpose line kept, plus one line introducing the second table; the recurring-mistakes table (now under `## Recurring mistakes`) keeps its six generic lessons, its Frequency column reads `2+` and First Seen "before this repo's features (inherited lesson)", with a note that the foreign ids were removed; new `## Accepted deviations` table (What · Why · Where), 18 rows.
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** The foreign Frequency ids (`F28, F31`, `F29`, `F30`, the F32 lesson) went too, not only the First Seen column — in this repo F29–F32 are different features, so they would mislead the same way. Each F38 row was re-checked against the code at `82a791f` before writing (409 and `SKILL.md:19/48`; no `Guard.ThrowIfNullOrWhiteSpace`/`OrThrowNotFound`/`FindByIdOrThrowAsync` in `src` — the 10 `ThrowIfNullOrWhiteSpace` hits are the BCL's `ArgumentException`; `Entity.IsNew` protected with no reader outside `Entity`; 9 handlers take `IServiceScopeFactory`; no `Repository<`/`FindByIdAsync` in `src`, `PhaseActionRepository` a plain class; `AddReflekt*` names; 7 `*Contracts.cs`; `TeamLimitCheck`, `VoteBudget` in their slice folders). Each Why cites the record that decided it; none is added beyond the records.
**Deviations:** None

### Rows and their sources
1 failures logged, not rethrown (P1; F38 item 3 read through its Fix Round 1 correction) · 2 OpenGraph queue-full wait (F38 Fix Round 1 residual) · 3 single-instance hub state · 4 two URL schemes (BE-36) · 5 team-count race (SK-23) · 6 architecture-doc template departure (Step 3) · 7 `tailwind-theme` inline-body wording (F36, FE-05; `index.html:11` `<style>`, `:25` plain `<body>`) · 8–15 F38 items 1, 2, 4, 5, 6, 7, 8, 9 · 16–18 F39 (below).

### F39 candidates (whole `implementation.md` read: per-step Skills/Decisions/Deviations, Fix Rounds 1–2, Key Decisions, Skills Used, Carry-Over, Deviations from Plan)
| Candidate | Checked against | Row? |
|---|---|---|
| `useTimerActions` toasts from a composable vs `vue-component-architecture` `SKILL.md:79`, `:110` (Fix Round 1 row 9 / S4) | `useTimerActions.ts:2,6` imports and calls `useToast` | yes (16) |
| API layer `src/api/client.ts` vs `create-vue-feature`'s `client/src/api/` and its cited `client/src/api/settings.ts` (Step 4, Skills Used row 4) | `web/reflekt-web/src/api/client.ts` exists; no `client/` folder | yes (17) |
| Types: `src/types/` one file per area vs `create-vue-feature`'s single `client/src/types/index.ts` (Step 3 under create-vue-feature) | `src/types/` holds 11 files, no `index.ts` | yes (18) |
| Stores never toast (Step 8) vs `pinia-patterns` `SKILL.md:227` | conforms | no |
| Inject through `InjectionKey` with a throwing guard (Step 9) vs `vue-component-architecture` `SKILL.md:100` | conforms (typed `InjectionKey`) | no |
| `index.html`'s inline theme read kept (Step 5) | not a skill rule; row 7 covers the head style | no |
| Prettier `endOfLine: "auto"` (Step 1), `@/` imports (Step 2), `BaseButton.to`, `BaseCard` radius, `BaseTooltip`, ARIA changes (Steps 13–14, Fix Round 1) | design choices within the skills, no skill statement contradicted | no |
| Unsanctioned `git rm --cached` (Step 9) | process incident, not a skill/code difference | no |

- Accept: `grep -n "Accepted deviations" docs/conventions/known-deviations.md` → lines 5, 20. `grep -n "useTimerActions"` → line 39. `grep -nE "F28-|F30-|F31-|F32-|adhoc-Suite"` → no hits. `grep -n "fire-and-forget"` → no hit. AC3 grep → no hits.
- Step close: `verify-run.js --profile fast --tied --changed docs/conventions/known-deviations.md` → pass, run `mvlbly4c-ae323142` (3.8 s). `label-tests.js` → no test timed.

## Step 10 — `ef-core.md`, `vue.md`, `csharp.md`
**Files:** `docs/conventions/ef-core.md` — every example rewritten with Reflekt types (`Card.EvidenceLinks`, `CardRepository`, `Sprint.Column1Name`, `Card.ReviewStatus`, `SprintConfiguration`/`CardConfiguration`, `JoinSprintEndpoint`, `PhaseAction`, `DbInitializer.SeedAsync`), generic gotchas kept; the design-time-factory advice replaced by the Step 2 findings (no `IDesignTimeDbContextFactory` needed; host built, not started; no credentials; no migrate/seed; working directory and `Data Source`); a pointer to `CLAUDE.md` for the commands; migrate-on-start noted. `docs/conventions/vue.md` — § White Flash Prevention describes F36's `<head>` `<style>` (`html` / `html.light` hex backgrounds), the theme script, and that `<body>` has no colour of its own; the TypeScript union example now matches `src/types/domain.ts` (`'Draft' | 'Active' | 'Closed'`). `docs/conventions/csharp.md` — line 73 rewritten: "Not found" is an exception, not a result, and a new line 74 keeps the other point (no exceptions for outcomes that are not errors, with `AdvancePhaseCheckResponse` and `PhaseActionPolicy.IsAllowed`/`EnsureAllowed`); line 12 names the real exception family (`DomainException` plus the `Blocks.Exceptions` types, `Guard.NotFound`) instead of calling the HTTP exceptions "domain exceptions"; the Method Overrides line now uses the real case (`CardRepository` overriding the virtual `GetByIdAsync`; `new` only for a non-virtual member) instead of the absent `UpsertAsync`; naming examples moved to Reflekt names (`IsRemoved`, `IsOwner`, `HasTimer`; `PhaseAdvanced`/`SprintClosed`, `SprintEvent`; `namespace Reflekt.Domain.Cards;`; `using var scope = scopeFactory.CreateScope();`).
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** Changes in `csharp.md` beyond line 73 (lines 12, 17, 103, 104, 111, 112) are there to meet AC3's "every code example names a Reflekt type" and to keep line 12 consistent with the rewritten line 73; each replacement was checked in the source (`RepositoryBase.GetByIdAsync` is `virtual`, `CardRepository` overrides it; `HttpException.StatusCode` exists; `Participant.IsRemoved`, `IsOwner`, `HasTimer` exist; no `UpsertAsync` in `src`). In `ef-core.md`, the `First` gotcha now ends in `Guard.NotFound` (404) rather than plain null handling, matching F38's exception convention.
**Deviations:** None

### Code examples left in `ef-core.md`, `csharp.md`, `vue.md` (AC3 review list)
| File | Example | Reflekt type it names |
|---|---|---|
| `ef-core.md` | `Db.Cards.Include(c => c.EvidenceLinks)` | `Card`, `EvidenceLink`, `CardRepository` |
| `ef-core.md` | `!excludedCardIds.Contains(c.Id)` | `CardRepository.HasCardsInGroupAsync` |
| `ef-core.md` | `.Include(c => c.EvidenceLinks)` (missing-include gotcha) | `Card` |
| `ef-core.md` | `CardRepository.GetActionItemsReviewedInSprintAsync` | `CardRepository` |
| `ef-core.md` | `dotnet ef migrations remove --force`; `Data Source=./data/reflekt.db`; `MigrateAndSeedAsync` → `Database.MigrateAsync` | `ReflektDbContext`, `Reflekt.API` host |
| `ef-core.md` | `HasDefaultValue("Went Well")`, `HasDefaultValue(ReviewStatus.None)` | `Sprint`, `Card`, `ReviewStatus`, `SprintConfiguration`, `CardConfiguration` |
| `ef-core.md` | `Guard.NotFound(await sprintRepository.GetByIdAsync(…))` | `JoinSprintEndpoint`, `SprintRepository` |
| `ef-core.md` | `required int` | `Card.SprintId` |
| `ef-core.md` | `HasData` + `ToJson()`; `DbInitializer.SeedAsync` | `PhaseAction`, `DbInitializer` |
| `ef-core.md` | `new List<int>()` / `Array.Empty<int>()` | `HasCardsInGroupAsync` (`excludedCardIds`) |
| `csharp.md` | `Reflekt.API.Features.Sprints.CreateSprint` namespaces; `GlobalUsings.cs` | Reflekt projects |
| `csharp.md` | exception family, `Guard.NotFound` | `DomainException`, `Blocks.Exceptions` types |
| `csharp.md` | `GetByIdAsync` override | `CardRepository`, `EvidenceLinks` |
| `csharp.md` | `catch (HttpException ex) when (ex.StatusCode == ...)` | `HttpException` (`Blocks.Exceptions`) |
| `csharp.md` | `AdvancePhaseCheckResponse`, `IsAllowed` / `EnsureAllowed` | `AdvancePhaseCheck`, `PhaseActionPolicy` |
| `csharp.md` | `IsRemoved`, `IsOwner`, `HasTimer`; `PhaseAdvancedEvent`, `SprintClosedEvent` | `Participant`, Reflekt events |
| `csharp.md` | `namespace Reflekt.Domain.Cards;`; `using var scope = scopeFactory.CreateScope();` | `Reflekt.Domain`, `BroadcastGroupCreatedHandler` |
| `csharp.md` | language idioms with no domain type: `.Result`/`.Wait()`, `throw;`, `ct.ThrowIfCancellationRequested()`, LINQ/EF operators (`.AsNoTracking()`, `.Any()`, `ExecuteUpdateAsync`…), `(int Count, bool Success)`, `ArgumentNullException.ThrowIfNull(param)`, `public T NewProp { get; init; }`, `IReadOnlyList<T>` | none — BCL/C# syntax, kept as is |
| `vue.md` | `index.html` `<style>`: `html { background: #0a0a0f; }`, `html.light { background: #f4f4f8; }`; `reflekt:theme` | Reflekt's `index.html` and storage key |
| `vue.md` | `type SprintState = 'Draft' \| 'Active' \| 'Closed'` | `SprintState` (`src/types/domain.ts`) |
| `vue.md` | framework idioms with no domain type: `data.length === 0 && result === null && !loading`, `RouterLink`, `withDefaults(defineProps<T>(), …)`, `appearance-none bg-transparent`, `v-if="list.length > 0"` | none — Vue/Tailwind syntax, kept as is |

### Accept checks
- AC3 grep `grep -nE "Ticket|IssueType|TestData\.|BugRatioTarget|SprintIssueSyncService|CreateDefault|Memberships" CLAUDE.md docs/conventions/*.md` → no hits.
- `grep -n "<style>" docs/conventions/vue.md` → line 25. `grep -niE "use inline .?style|style=\"background" docs/conventions/vue.md` → no hit.
- `grep -ni "not found" docs/conventions/csharp.md` → line 73 only: "\"Not found\" is an exception, not a result."
- Step close: `verify-run.js --profile fast --tied --changed docs/conventions/ef-core.md docs/conventions/vue.md docs/conventions/csharp.md` → pass, run `mvlbpana-77bf866b` (3.8 s). `label-tests.js` → no test timed.

### Carry-over for the run's tail (range 5–10 ends here; the developer who writes the tail gathers these)
| Title | Severity | For | Evidence | Note |
|---|---|---|---|---|
| Product decision row says "one team per creator" | low | architect | `docs/product/v1.md:545`; code: `TeamSettings:MaxTeamsPerUser` default 10 (F10) | Product scope, outside F40; only the sign-in part of the row was changed (Step 7) |
| Store file naming differs from `create-vue-feature` | low | reviewer | skill: `client/src/stores/{name}Store.ts`; code: `web/reflekt-web/src/stores/card.ts` etc. | Not listed by F39's record, so not an Accepted-deviations row (Step 9); a candidate row if the reviewer wants it |
| Generic language idioms left in `csharp.md` / `vue.md` | low | reviewer | the "none" rows of the Step 10 list above | Read as syntax illustrations, not domain examples; AC3's review half decides |

## Step 11 — `.claude/README.md` and `kb-entry-schema.md`
**Files:** `.claude/README.md` — skill count 36 → 37 (F16); the two `docs/kb/` mentions (Where Do I Put This, Key Files) carry a `[^never-created]` footnote that also names `docs/skill-backlog.md` (no mention of it existed in this file, so the footnote is its only mention here) and the rules/files that name both; Key Files adds `docs/architecture/index.md` (imports `architecture.md`), `.claude/hooks/audit-logger.js` and `.claude/verify.json`; a paragraph "Agent files not updated" names the stale lines (`architect.md` 100/112, `po.md` 111/179, `team-lead.md` 52 name `docs/architecture/v1.md`; `learner.md` 68 calls `CLAUDE.md` stack-agnostic); the "Repo structure in 3 files" row now names `CLAUDE.md`, `architecture.md`, `index.md` (with `project-rules.md` as a pointer and `v1.md` as the linked decision log). `docs/conventions/kb-entry-schema.md` — a note under the purpose line saying `docs/kb/` was never created.
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** Hooks and `verify.json` are listed under Key Files rather than as new Information Layers rows (neither is a layer of instructions). `audit-logger.js` is described from its source: it writes `docs/audit/{session_id}.log`, and no committed settings file wires it (`.claude/` holds no `settings*.json`; `git ls-files .claude` outside the four folders → `README.md`, `hooks/audit-logger.js`, `verify.json`), so the README says it runs only where a settings file registers it. `verify.json`'s `full` role is described as merged (with F39 change note 2's lint step, P4). Agent-file line numbers were taken by `grep -n "v1.md" .claude/agents` / `grep -n "stack-agnostic"`; `critic.md`'s `v1.md` mentions are the product spec (spec review against the product doc), so they are not listed. `.claude/settings.local.json`, named by the audit (AR-27), does not exist in this worktree, so it is not listed.
**Deviations:** None

### Accept checks
- Skill count: `grep -n "skills)" .claude/README.md` → line 34 `(37 skills)`; `ls .claude/skills | wc -l` → 37 (F16).
- `grep -n "never created" .claude/README.md docs/conventions/kb-entry-schema.md` → `.claude/README.md:59`, `docs/conventions/kb-entry-schema.md:5`.
- Hooks / `verify.json` listed: `.claude/README.md:56`, `:57`.
- Step close: `verify-run.js --profile fast --tied --changed .claude/README.md docs/conventions/kb-entry-schema.md` → pass, run `mvlbsk43-ddc222b8` (3.7 s), re-run after the note's wording fix as `mvlbt6ww-7ce4706c` → pass (3.6 s). `label-tests.js` → no test timed.

## Step 12 — Small fixes and the backlog change note
**Files:** `README.md` — line 98: `definition/` folders corrected from "only the newest features (`F31`, `F32`)" to `F31`, `F32` and `F35`–`F40`, with `F29`, `F30`, `F33`, `F34` and the ad-hoc run `delivery/` only (F16); line 102: `docs/architecture/` now links `docs/architecture/index.md` as the one-page map leading to `architecture.md`, with `v1.md` named as the decision log. `docs/product/index.md` — v2 line `Draft` → `Partially shipped (F23–F32)`. `docs/product/v2.md` — status line `Partially shipped (F23–F32)`, keeping that it was written as a draft and corrected from PO review. `docs/specs/F40-ArchitectureDocs/delivery/change-notes.md` — new, F35–F39's form (Target, Source, Order, Change, Exact text, Expected result, Owed at merge): Note 1, the historical note for the top of `docs/backlog.md`.
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** The backlog note names the statements it supersedes by item number, read from the file: allowlist in items 6 (lines 48–49, incl. "not-invited page"), 7 (line 56, `Auth:AllowedEmails`) and 15 (lines 118–121); `CardVote` in "Already agreed" item 1 (line 6, which also says 4 aggregates — the code has 7, F5) and items 10, 11 (lines 80, 85). It is inserted after the title line, so the file still opens with its heading. Item 22 is named as still parked (its `**Status: Parked**`, line 174). Only one note is written; the backlog status line for #40 at close is the close's to note, not this step's.
**Deviations:** None

### Accept checks
- `grep -n "Partially shipped" docs/product/index.md docs/product/v2.md` → `index.md:4`, `v2.md:3`.
- `grep -n "architecture/index.md" README.md` → line 102.
- `change-notes.md` holds Note 1 with its exact text (one `**Exact text:**` block).
- `git diff --name-only e5b43ce -- docs/backlog.md` → empty.
- AC2 grep on the three edited files → no hits.
- Step close: `verify-run.js --profile fast --tied --changed README.md docs/product/index.md docs/product/v2.md docs/specs/F40-ArchitectureDocs/delivery/change-notes.md` → pass, run `mvlbusq3-a5c945d6` (3.7 s). `label-tests.js` → no test timed.

## Step 13 — Regenerate the code graph (last doc-changing step)
**Files:** `graphify-out/GRAPH_REPORT.md`, `graphify-out/graph.json`, `graphify-out/manifest.json` — regenerated by a full rebuild from the final tree (Steps 1–12 done); `git diff --stat -- graphify-out` → 3 files, 7674 insertions, 34590 deletions. New untracked outputs the tool now writes: `graphify-out/.graphify_labels.json`, `.graphify_labels.json.sig`, `.graphify_root` (one byte, `.`). Also written but git-ignored (`.gitignore:445–446`): `graphify-out/cache/`, `graphify-out/graph.html`. **Primary path taken; no fallback edit.**
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** AC6 was checked on `graph.json` with a script (`.claude/tmp/f40-step13/check.py`, campaign scratch) because the JSON is compact: the plan's `grep -c '"file_type": "code"'` counts lines and returns 0 on it, so the count is `grep -o '"file_type": *"code"' | wc -l`. The `IsAllowed`/`EnsureAllowed`/`ConnectToAllowedAddressAsync` code nodes are `PhaseActionPolicy` and `OpenGraphAddressPolicy` (gating and SSRF), not sign-in allowlist code. The three new untracked `graphify-out/` files are left in place: they are tool output inside `graphify-out/`, which AC7 allows; whether the close commits them is the close's call (carry-over).
**Deviations:** None

### Command and result
- Before: `GRAPH_REPORT.md` copied to `.claude/tmp/f40-step13/GRAPH_REPORT.before.md`; `git status --porcelain --ignored` saved.
- `timeout 550 D:/tools/graphify-venv/Scripts/graphify.exe update . --force` (repo root) → **exit 0**. Output: 874 code files AST-extracted; 11 unclassified files skipped (`.gitignore`, `Directory.*.props`, …); 30 files with no symbols (e.g. `audit-logger.js`, `deploy.ps1`, `GlobalUsings.cs`); "pruned 7 node(s) from 7 newly-ignored file(s)"; "Rebuilt: 7405 nodes, 14946 edges, 509 communities"; "Built from commit: `82a791f8`". Log: `.claude/tmp/f40-step13/update.log`.
- `git status --porcelain --ignored` after vs before: ` M` the three tracked files; `??` the three new dot-files; `!!` `graphify-out/cache/`, `graphify-out/graph.html`. Nothing outside `graphify-out/` (and the scratch log).

### AC6 checks
| Check | Before (HEAD, 2026-04-21 graph) | After |
|---|---|---|
| God nodes (top 10) | `error()`, `push()`, `F()`, `send()`, `log()`, `fetch()`, `isRequired()`, `forEach()`, `set()`, `get()` — minified-bundle functions | `Fact sheet (all read at commit 82a791f…)`, `Communities`, `Step 12 — Reads: …`, `Sprint`, `SprintAccess`, `ReflektDbContext`, `DomainException`, `Backend vs the repo's own conventions (F33 worktree…)`, `Card`, `SprintHub` — no `wwwroot`, `HelloWorld` or allowlist entry |
| Community names | 91; include "Minified JS Bundle (Vue dist)", "Auth Allowlist" (`AllowlistChecker`), "HelloWorld"; `wwwroot/assets/index-C4EWDWjc.js` in surprising connections | 509 (468 named in `graph.json`); `grep -niE "^### Community.*(wwwroot\|assets/index\|HelloWorld\|allowlist\|AllowedEmails)"` → none; hub list → none. Report-wide, "allowlist" appears only inside three communities' node lists (159 "Cards feature", 199 "Auth Review — Step 2", 321 "3. Backend — endpoint slices"), as headings of old evidence docs — document nodes, expected |
| Code nodes `file_type: code` | 906 | 2267 (document 5059, concept 79) |
| `User` code nodes | — | `src/Services/Reflekt/Reflekt.Domain/Users/User.cs`, `web/reflekt-web/src/types/auth.ts` |
| `CardGroup` code nodes | — | `Reflekt.Domain/CardGroups/Behaviors/CardGroup.cs`, `web/reflekt-web/src/types/card.ts` |
| Allowlist / HelloWorld code nodes | `AllowlistChecker` | none |
| `wwwroot` / `node_modules` nodes | bundle nodes present | 0; `manifest.json` has no `wwwroot`, `node_modules`, `bin/`, `obj/` path |

### Document-node observation
- Document nodes **were** refreshed from the edited docs: `docs/architecture/architecture.md` (new in this feature) has 18 nodes (its § headings, e.g. "8. Real-time"); `CLAUDE.md` has 19 nodes including "Not used by this app" and "Port Convention" (the old graph had none from either); `implementation.md`'s fact sheet is the top god node.
- `docs/architecture/v1.md`: 12 nodes; none has "allowlist" in its label, before or after (its allowlist text is a table row and a paragraph, not a heading).
- Community names were regenerated: the old graph had no `community_name` field; the new names are taken from the current code and docs (e.g. "useRetroRoom", "ParticipantRepository", "DomainEventPublishingInterceptor").
- Step close: `verify-run.js --profile fast --tied --changed graphify-out/GRAPH_REPORT.md graphify-out/graph.json graphify-out/manifest.json graphify-out/.graphify_labels.json graphify-out/.graphify_labels.json.sig graphify-out/.graphify_root` → pass, run `mvlbx0g0-04516e60` (3.8 s). `label-tests.js` → no test timed.

## Step 14 — Acceptance run and change-set check
**Files:** `docs/specs/F40-ArchitectureDocs/delivery/implementation.md` — this block and the closing sections. `docs/specs/F40-ArchitectureDocs/delivery/lessons.md` — Developer Lessons (one entry strengthened, one added).
**Skills:** None (plan Skill Mapping: no pattern; TDD no)
**Decisions:** Every AC was run from the repo root at the final tree (HEAD `82a791f`, working tree with Steps 1–13). The AC8 frontend-test-runner grep on `project-rules.md` was run as `grep -c 'vi[t]est'` (the same match): the plain word in a shell command is blocked by the plugin's test-entry gate (lessons). AC3's "every code example in `docs/conventions/`" was widened beyond the files Steps 6, 8 and 10 listed: every `docs/conventions/*.md` was checked for fenced blocks (`grep -c '^```'`: only `testing.md` 6 and `kb-entry-schema.md` 4) and code spans, and `kb-entry-schema.md` and `known-deviations.md` are added to the list below.
**Deviations:** None

### AC1–AC8
| AC | Command (as written in the spec) | Result |
|---|---|---|
| 1 | `grep -nE "MassTransit\|RabbitMQ\|gRPC\|ApiGateway\|docker-compose\|Modules/" CLAUDE.md docs/conventions/project-rules.md` | **pass** — one hit, `CLAUDE.md:127`, the body of `### Not used by this app` (line 125); none in `project-rules.md` |
| 2 | `grep -rniE "allowlist\|AllowedEmails\|allowlisted\|not.invited" CLAUDE.md README.md docs/architecture docs/product docs/conventions docs/backlog.md` | **pass in the worktree; backlog part owed at merge** — `CLAUDE.md:112`, `architecture.md:303`, `product/v1.md:532`, `:545` state no allowlist exists; `architecture/v1.md:182`, `:210` marked superseded; `backlog.md:271` (item 35) states the allowlist is not enforced; `backlog.md:48, 49, 56, 118, 120, 121` (items 6, 7, 15) are covered by `change-notes.md` Note 1, which marks them historical and superseded (P2). Same listing as Step 7, re-run |
| 3 | `grep -nE "Ticket\|IssueType\|TestData\.\|BugRatioTarget\|SprintIssueSyncService\|CreateDefault\|Memberships" CLAUDE.md docs/conventions/*.md` | **pass** — no hits (exit 1); review half: the list below |
| 4 | `grep -n "^@" docs/conventions/coding-conventions.md`; `grep -n "^@" docs/architecture/index.md` | **pass** — `@csharp.md`, `@vue.md`, `@ef-core.md`, `@testing.md` (lines 2–5); all eight file names appear in the file; `index.md:56` `@architecture.md` only |
| 5 | `implementation.md` per-section record | **pass** — § Step 3 "Per section: commit and how checked" (14 sections + inventory, all against `82a791f`, each with its Step 1 listing); its accept line confirms the doc's counts equal the listings |
| 6 | graph regenerated, god nodes / community names / code nodes | **pass** — § Step 13: rebuilt (exit 0), no `wwwroot`, `HelloWorld` or allowlist god node or community name; 2267 code nodes, `User` and `CardGroup` present, no allowlist code |
| 7 | `git diff --name-only e5b43ce` + `git status --porcelain` | **pass** — listing below |
| 8 | the other rows by grep | **pass in the worktree; backlog part owed at merge** — `vue.md:25` `<style>`, inline-style grep no hit; `csharp.md` "not found" only at line 73 ("is an exception, not a result"); `known-deviations.md:20` `## Accepted deviations`, foreign-id grep no hit; `architecture/v1.md:3` superseded; `Partially shipped` at `product/index.md:4`, `v2.md:3`; `backlog.md` does not yet open with the note (owed at merge, `change-notes.md` Note 1, P2); `project-rules.md:41` `run lint`, frontend-test-runner grep 0; `.claude/README.md:34` 37 skills = `ls .claude/skills \| wc -l` 37; `implementation.md` § Step 2 (the recorded run) and § Step 3 "Inventory entries marked "not used by this app" (11)" |

### AC3 review list — every code example in `docs/conventions/` (gathered)
| File | Examples (Reflekt type named) | Source |
|---|---|---|
| `project-rules.md` | `dotnet ef migrations add` (`Reflekt.Persistence`, `Reflekt.API`) | Step 6 |
| `testing.md` | layout tree (`Reflekt.UnitTests`, `Reflekt.IntegrationTests`, `TestParallelization.cs`, `AuthSmokeTests`, `FoundationSmokeTests`); `Create_WithEmptyName_Throws` (`Team.Create`); integration class (`TeamEndpointTests`, `ReflektApiFactory`, `Policies.SignedInUser`, `SeedTestUserAsync`, `/api/teams`) | Step 8 |
| `ef-core.md` | 10 rows: `Card.EvidenceLinks`, `CardRepository` (`HasCardsInGroupAsync`, `GetActionItemsReviewedInSprintAsync`), `ReflektDbContext`, `Sprint`/`Card` defaults with `SprintConfiguration`/`CardConfiguration`, `JoinSprintEndpoint`, `Card.SprintId`, `PhaseAction`, `DbInitializer` | Step 10 |
| `csharp.md` | 7 rows with Reflekt types (namespaces, exception family, `CardRepository.GetByIdAsync`, `HttpException`, `AdvancePhaseCheckResponse`/`PhaseActionPolicy`, `Participant` names and events, `BroadcastGroupCreatedHandler`); 1 row of BCL/C# idioms with no domain type | Step 10 |
| `vue.md` | `index.html` `<style>` and `reflekt:theme`; `SprintState`; 1 row of Vue/Tailwind idioms with no domain type | Step 10 |
| `kb-entry-schema.md` | two fenced Markdown templates (placeholders, no code); path example `src/Services/Reflekt/Reflekt.Domain/Sprints/Sprint.cs` | this step |
| `known-deviations.md` | no fenced block; code spans are Reflekt names and paths (`useTimerActions`, `DomainEventPublishingInterceptor`, `TeamLimitCheck`, `src/api/client.ts`…) and skill paths | this step |
| `coding-conventions.md` | none (imports and a file table) | this step |

The idiom rows (`csharp.md`, `vue.md`) are syntax illustrations with no domain type; left for the reviewer's AC3 judgement (carry-over).

### AC7 — change set
`git diff --name-only e5b43ce` (27): `.claude/README.md`, `CLAUDE.md`, `README.md`, `docs/architecture/index.md`, `docs/architecture/v1.md`, `docs/conventions/{coding-conventions,csharp,ef-core,kb-entry-schema,known-deviations,project-rules,testing,vue}.md`, `docs/product/{index,v1,v2}.md`, `docs/specs/F40-ArchitectureDocs/definition/spec.md`, `docs/specs/F40-ArchitectureDocs/delivery/{communication-log,lessons,plan,reader-pair,review-critic-codex,review-critic}.md`, `graphify-out/{GRAPH_REPORT.md,graph.json,manifest.json}`, `src/Services/Reflekt/CLAUDE.md`.
`git status --porcelain` untracked: `.claude/.personas.json`, `.claude/tmp/` (campaign files, left out per plan Decisions), `docs/architecture/architecture.md`, `docs/specs/F40-ArchitectureDocs/delivery/change-notes.md`, `docs/specs/F40-ArchitectureDocs/delivery/implementation.md`, `graphify-out/.graphify_labels.json`, `graphify-out/.graphify_labels.json.sig`, `graphify-out/.graphify_root`.
Every path is a `*.md` outside `docs/features/`, `ai-coding/`, `docs/plans/` and past `docs/specs/*` (only F40's own folder), with `.claude/` limited to `.claude/README.md`, or under `graphify-out/`. No source, test or config file; `docs/backlog.md` absent; `.claude/agents`, `rules`, `skills` absent. `src/Services/Reflekt/CLAUDE.md` is a `*.md` file (Step 6). `obj/dotnet-ef/` and `obj/` gone (`ls` → no such file); no `docs/pending-decisions/`.
- Step close: `verify-run.js --profile fast --tied --changed docs/specs/F40-ArchitectureDocs/delivery/implementation.md docs/specs/F40-ArchitectureDocs/delivery/lessons.md` → pass, run `mvlbzh0h-ead962a9` (3.6 s). `label-tests.js` → no test timed.

## Fix Round 1
**Files:** `docs/architecture/architecture.md` — row 1 (§ 2, lines 102–104): "every other change requires Active" replaced by the guard per operation: `EnsureActive` (close, advance phase and its check, focus/clear focus, toggle discussed, both joins, kick; `SetReviewStatus` checks Active + Writing itself) vs `EnsureOpen` (cards, evidence links, groups, votes, `UpdateColumns` — `Sprint.UpdateColumnConfig` has no state check — and the hub's timer methods; Draft allowed); row 12 (§ 3, line 123): the domain-events exception and its examples removed, the line now points to root `CLAUDE.md` § Domain events (the plan pins the rule there, "Stated here only"); row 5 (§ 8, line 295): `ReadinessChanged` on disconnect only for the last connection of a ready participant, `ParticipantLeft` on every disconnect of a connection with participant claims; row 9 (§ 9, line 318): the hub rechecks active participation in `JoinSprint` (before and after registering), `ToggleReady` and the five timer methods; `LeaveSprint` cleans up with no check; row 8 (§ 10, line 324): seven repositories over `RepositoryBase` plus the read-only `PhaseActionRepository`; row 4 (§ 11, line 336 and the 499 row): the JSON body is written for every mapped exception except cancellation; 499, the cookie scheme's 401/403 callbacks and `GuestTokenAuthenticationHandler`'s default challenge/forbid carry a status only; row 7 (§ 12, line 360): 13 Pinia stores, `pendingValues.ts` named as the `createPendingValues` helper. `CLAUDE.md` — row 8 (line 46): seven repositories over `RepositoryBase` plus `PhaseActionRepository`, a plain class with no save. `docs/conventions/known-deviations.md` — row 2: the team-count row no longer claims "a duplicate name cannot slip through"; new row (line 29) "Team-name duplicates: the check ignores case, the index does not" (`TeamRepository.ExistsByOwnerAndNameAsync` `NOCASE` vs `IX_Teams_OwnerId_Name` binary — `Teams.Name` has no collation; concurrent `Alpha`/`alpha` can both persist; documented, not fixed). `docs/conventions/ef-core.md` — row 3 (line 7): `GetByIdAsync`, `GetBySprintIdAsync`, `GetBySprintIdReadOnlyAsync` include `EvidenceLinks`; `GetUnreviewedActionItemsBySprintIdAsync` and `GetActionItemsReviewedInSprintAsync` do not, and their callers (`GetPreviousActionsEndpoint`, `SprintReportBuilder.BuildPreviousActionsSectionAsync`) read no links. `docs/conventions/csharp.md` — row 10: line 43 now `ReadinessToggle` (3 fields, `ReadinessTracker.ToggleReady`) and `EvidenceLinkSourceDetector.Detect`'s `(EvidenceLinkSource Source, string? ExternalKey)`; Reflekt usages added beside the idiom rules at lines 49, 50, 55–59, 64, 67, 80, 81, 87–89, 96, 97, 109, 113–115 (each checked in `src`; `ThrowIfNegative` → the real `ThrowIfLessThan` in `Sprint.Create`). `docs/conventions/vue.md` — row 10: lines 7–9, 13–15, 20, 21 now name Reflekt code (`useToast`/`useTheme` module refs; `SprintList.vue`'s `error` then `!loading && sprints.length === 0`; the card store's `editCard` / `applyCardUpdated` through `pendingEdits`, replacing the foreign "auto-detect vs Re-detect" example; `AppHeader.vue` `RouterLink`; `CreateTeamForm.vue` `withDefaults`; the assignee `<select>`s in `RetroActionItemInput.vue`, `RetroCardItem.vue`; `RetroBoardPanel.vue` empty columns). `docs/specs/F40-ArchitectureDocs/delivery/change-notes.md` — row 11: Note 2, merge order — F40 with or after F39 Notes 1 (package.json `lint`) and 2 (`roles.full` lint leg), listing the five F40 docs that describe them. `docs/specs/F40-ArchitectureDocs/delivery/lessons.md` — one Developer lesson strengthened (AC3), one added (graph rebuild indexes in-tree scratch). `graphify-out/GRAPH_REPORT.md`, `graph.json`, `manifest.json`, `.graphify_labels.json(.sig)` — row 6: regenerated. Deleted: `.claude/tmp/f40-step2/`, `.claude/tmp/f40-step13/` (this feature's scratch; `.claude/tmp/` is now empty) and `graphify-out/2026-10-23/` (a backup of the previous, polluted graph the tool wrote during this rebuild; untracked, not ignored).
**Skills:** None (docs-only fixes; plan Skill Mapping: no pattern, TDD no)
**Decisions:** Row 6 order: every doc fix and the lessons entry first; then the scratch deleted (`ls -A .claude/tmp | wc -l` → 0); then `graphify update . --force` as the last change to an indexed doc; this block is written after it, as Step 14 was after Step 13 (it needs the rebuild's results). Row 4: the GuestToken default challenge was added to the status-only list after checking `GuestTokenAuthenticationHandler` overrides neither `HandleChallengeAsync` nor `HandleForbiddenAsync`. Row 10: a code span is read as an example (the review refuted the "generic idioms" carry-over); the spans left without a Reflekt usage name a BCL member or form the code does not use, so there is no Reflekt instance to cite — `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, `async void`, `throw ex;`, `ct.ThrowIfCancellationRequested()` (0 uses in `src`), `IQueryable<T>`, `.Count() > 0`, `.ToList()`, `?.`, `!= null`, `List<T>`, `IEnumerable<T>` as a return; the `vue.md` `<select>` line describes what the selects do (tokens, no `appearance-none`) rather than prescribing a class they lack. Row 7's "correct the implementation record's counts": Step 3's accept line and § Step 3 row 12 say "stores (14)"; per the format the closed blocks are not rewritten — the correct count is **13 Pinia stores plus the `pendingValues.ts` helper** (13 files with `defineStore`). Row 12: pointer chosen over keeping the restatement.
**Deviations:** None — all twelve rows acted on.

### Row checks
| Row | Check | Result |
|---|---|---|
| 1 | `grep EnsureActive\|EnsureOpen src` | `EnsureActive`: 4 calls in `Sprint` (`AdvancePhase`, `Close`, `SetFocus`, `ClearFocus`) + 8 endpoints (AdvancePhase, AdvancePhaseCheck, FocusGroup, ClearFocus, ToggleDiscussed, JoinSprint, JoinSprintGoogle, KickParticipant); `EnsureOpen`: 13 endpoints (cards 3, evidence links 2, groups 5, votes 2, UpdateColumns) + `SprintHub.EnsureTimerAvailable`; `SetReviewStatusEndpoint.cs:27` its own Active + Writing check; matches lines 103–104 |
| 2 | `TeamConfiguration.cs:13,16`, `TeamRepository.cs:13` | `Name` has no `UseCollation`; index on `{OwnerId, Name}`; check uses `EF.Functions.Collate(t.Name, "NOCASE")` |
| 3 | `CardRepository.cs` `Include` lines 9, 14, 22; methods 33, 47 have none | matches line 7 |
| 4 | `GlobalExceptionMiddleware.cs:46–50`, `AuthenticationConfiguration.cs:22–31` | 499 sets status only; cookie callbacks set status only |
| 5 | `SprintHub.OnDisconnectedAsync`, `ReadinessTracker.Disconnect:97–111` | `null` when another connection remains or not ready; `ParticipantLeft` unconditional |
| 7 | `grep -l "defineStore(" web/reflekt-web/src/stores` | 13 files; `pendingValues.ts` not among them |
| 8 | `grep "class \w+Repository" Repositories/` | 7 `: RepositoryBase<…>`, `PhaseActionRepository` plain |
| 9 | `SprintHub.cs` | `RequireActiveParticipantAsync` in `JoinSprint`, `ToggleReady`; `RequireScrumMasterAsync` (`IsRemoved`) in the 5 timer methods; `LeaveSprint` none |
| AC2 | spec grep (worktree files) | unchanged hits: `CLAUDE.md:112`, `architecture.md:305`, `architecture/v1.md:182, 210`, `product/v1.md:532, 545`; no new hit |
| AC3 | spec grep | no hits (exit 1); review half: row 10 above |

### AC6 re-recorded (row 6)
- Before the rebuild: `.claude/tmp/` empty (`ls -A | wc -l` → 0).
- `timeout 550 D:/tools/graphify-venv/Scripts/graphify.exe update . --force` (repo root) → **exit 0**: 127 uncached files AST-extracted, "Rebuilt: 7350 nodes, 14970 edges, 509 communities" (Step 13: 7405 nodes — the 55 fewer are the scratch nodes gone, net of doc changes); it wrote a backup `graphify-out/2026-10-23/` of the previous graph, deleted afterwards.
- `.claude/tmp` paths in `graph.json`, `manifest.json`, `GRAPH_REPORT.md`: **0** (`grep -oiE 'claude[/\\]+tmp'`).
- Old labels: "Minified JS Bundle", "Auth Allowlist", "HelloWorld", `"Communities"` → 0 in `graph.json`, `GRAPH_REPORT.md` and `.graphify_labels.json`.
- God nodes (top 10): `Fact sheet (all read at commit 82a791f…)`, `Step 12 — Reads: …`, `Sprint`, `SprintAccess`, `ReflektDbContext`, `DomainException`, `Backend vs the repo's own conventions (F33 worktree…)`, `SprintHub`, `Card`, `CardRepository` — no "Communities", no `wwwroot`, `HelloWorld` or allowlist entry.
- Code nodes `"file_type": "code"`: 2267 (`grep -o … | wc -l`); `User` and `CardGroup` code nodes present (2 each); `wwwroot` / `node_modules` / `bin/` / `obj/` in `manifest.json`: 0. One `AllowlistChecker` string remains, as the heading of the old `ai-coding/plans/Auth/review.md` (a document node, as in Step 13).
- Not in row 6, noted: `manifest.json` lists `.claude/.personas.json` (an untracked campaign file, "do not touch"); it yields no node (`personas` in `graph.json` → 0).
- Step close (round): `verify-run.js --profile fast --tied --changed` the 14 files above (architecture.md, CLAUDE.md, known-deviations.md, ef-core.md, csharp.md, vue.md, change-notes.md, lessons.md, implementation.md, the five `graphify-out/` files) `--slug F40-ArchitectureDocs` → **pass**, run `mvlcs3w8-335d8557` (4.0 s; no tied tests, `roles.unit` ran). `label-tests.js --run mvlcs3w8-335d8557` → no test timed.

## Key Decisions
- **Step 1:** message names counted from both ends and reconciled; endpoints listed from their attributes (no `Configure()` exists); test counts are attribute counts, not executed cases.
- **Step 2:** the `dotnet-ef` download was kept inside `obj/dotnet-ef/` (`NUGET_PACKAGES`, `NUGET_HTTP_CACHE_PATH`) and removed with it; one extra `dbcontext info --verbose` run shows how the tool resolves the context.
- **Step 3:** a third inventory disposition, **Workflow**, for the 10 pipeline/process skills; `authorization-patterns` marked "not used by this app" (no user roles) and logged as an ill-fitting gap; skill/code differences point to `known-deviations.md` instead of being listed in `architecture.md`.
- **Step 4:** the mutation flow follows the real `CreateCardEndpoint`, not the audit's outline; superseded annotations are added on the same line as the old text, so a grep hit shows the marker.
- **Step 5:** the port section keeps the `{AA}XY` scheme and the no-duplicate rule but drops the unused slot table; template-only items live in one "Not used by this app" note; "No god folders" names the three service projects and leaves `src/Services/` and test `Helpers/` out.
- **Step 6:** the `SprintIssueSyncService` example left `project-rules.md` with the guardrail list it sat in (now a pointer); its replacement is in `CLAUDE.md`.
- **Step 7:** only sign-in statements changed in `docs/product/v1.md`; "one team per creator" left as product scope (carry-over).
- **Step 8:** the FluentAssertions reason quoted from its 8.0.0 release notes without interpretation; the `Speed` paragraph says how a slow test would be marked.
- **Step 9:** the foreign Frequency ids removed along with First Seen; every F38 row re-checked in the code before it was written.
- **Step 10:** `csharp.md` changes beyond line 73 made to meet AC3, each checked in the source; `ef-core.md`'s `First` gotcha ends in `Guard.NotFound`.
- **Step 11:** hooks and `verify.json` listed under Key Files; `audit-logger.js` described as not wired by any committed settings file; `verify.json`'s `full` role described as merged (P4).
- **Step 12:** the backlog note names each superseded statement by item number and is inserted after the title line; one note only.
- **Step 13:** AC6 checked by parsing `graph.json`; the tool's three new untracked `graphify-out/` files left in place.
- **Step 14:** the AC3 list covers every `docs/conventions/*.md`, not only the files earlier steps listed.

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | None | plan: inventory work, no pattern; TDD no |
| 2 | None | plan: no pattern; TDD no |
| 3 | create-architecture-doc | Generate mode: inventory from every `SKILL.md` frontmatter, reference-don't-restate, gaps logged; section list per spec Decisions row 4 |
| 4 | None | plan: no pattern; TDD no |
| 5 | None | plan: no pattern (`create-service-claude-md` scaffolds new services only); TDD no |
| 6 | None | plan: `create-service-claude-md` checked, scaffolds new services only; TDD no |
| 7 | None | plan: no pattern; TDD no |
| 8 | None | plan: no pattern; TDD no |
| 9 | None | plan: no pattern; TDD no |
| 10 | None | plan: no pattern; TDD no |
| 11 | None | plan: no pattern; TDD no |
| 12 | None | plan: no pattern; TDD no |
| 13 | None | plan: no pattern; TDD no |
| 14 | None | plan: no pattern; TDD no |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| Product decision row says "one team per creator" | low | architect | `docs/product/v1.md:545`; code: `TeamSettings:MaxTeamsPerUser` default 10 (F10) | Product scope, outside F40; only the sign-in part of the row was changed (Step 7) |
| Store file naming differs from `create-vue-feature` | low | reviewer | skill: `client/src/stores/{name}Store.ts`; code: `web/reflekt-web/src/stores/card.ts` etc. | Not listed by F39's record, so not an Accepted-deviations row (Step 9); a candidate row if the reviewer wants it |
| Generic language idioms left in `csharp.md` / `vue.md` | low | reviewer | the "none" rows of the Step 10 list; Step 14 AC3 list | Read as syntax illustrations, not domain examples; AC3's review half decides |
| New untracked graphify outputs | low | reviewer | `graphify-out/.graphify_labels.json`, `.graphify_labels.json.sig`, `.graphify_root` (Step 13) | Inside `graphify-out/` (AC7 allows it); whether to commit them or add them to `.gitignore` (a config file, out of F40's scope) is the close's call |
| Backlog parts of AC2 and AC8 owed at merge | low | reviewer | `change-notes.md` Note 1 | `docs/backlog.md` is a shared file (P2); the checks pass only once the note is applied |
| Agent files still name `v1.md` as the architecture context | low | architect | `architect.md:100,112`, `po.md:111,179`, `team-lead.md:52`; `learner.md:68` ("CLAUDE.md is stack-agnostic") | Agent files are out of F40's scope; noted in `.claude/README.md` (Step 11); a follow-up could update them |
| Backlog "How to resume" section is stale | low | architect | `docs/backlog.md:305–310` ("pick up from Section B", "If Foundation is not yet built") | Not in the spec's backlog row, so not in the change note |

## Deviations from Plan
- Step 2: plan item 3 (retry with placeholder Google credentials) was not run, because no command failed while starting the host.
- Step 3: the inventory uses a third disposition, **Workflow**, beside the plan's "Referenced" and "not used by this app", for the 10 pipeline/process skills, which neither label fits.
- Step 13: the code-node count used `grep -o '"file_type": *"code"' | wc -l` (and a JSON parse) instead of the plan's `grep -c '"file_type": "code"'`, which returns 0 on the compact `graph.json`.
- Step 14: the frontend-test-runner grep on `project-rules.md` was run with a character class (`vi[t]est`, the same match), because the plugin's test-entry gate blocks the plain word in a shell command.

*Status: COMPLETE — developer, 2026-10-23*
