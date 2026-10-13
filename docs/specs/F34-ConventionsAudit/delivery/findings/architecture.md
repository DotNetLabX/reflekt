# F34 research: architecture reference vs code

Repo: the F33 worktree (worktree, HEAD a252cdf). Research only, no repo edits.
Paths are relative to the repo root unless prefixed. `API` = `src/Services/Reflekt/Reflekt.API`, `PER` = `src/Services/Reflekt/Reflekt.Persistence`, `DOM` = `src/Services/Reflekt/Reflekt.Domain`.

Documents checked: `docs/architecture/index.md` (a 1-line `@v1.md` import), `docs/architecture/v1.md`, `CLAUDE.md`, `src/Services/Reflekt/CLAUDE.md`, `docs/product/index.md`, `graphify-out/GRAPH_REPORT.md`, `.claude/README.md`, `README.md`. Also touched (not asked, but they repeat the same claims): `docs/conventions/project-rules.md`, `docs/backlog.md`, `docs/conventions/ef-core.md`.

Severity: High = a reader would build a wrong mental model or a command fails. Med = misleading detail. Low = cosmetic or arguable.
Effort: S = under 15 min of editing, M = a section rewrite, L = regenerate the document.

## 1. Headline facts (verified)

| Fact | Evidence |
|---|---|
| 1 service, 3 projects + 4 BuildingBlocks + 2 test projects, `.slnx` | `Reflekt.slnx` |
| TFM `net10.0`, but all Microsoft.* packages pinned at 9.0.4, FastEndpoints 5.34.0 | `Directory.Build.props:3`, `Directory.Packages.props:7-30` |
| No MassTransit, RabbitMQ, gRPC, YARP, Redis, MediatR, Swashbuckle anywhere | grep over `*.csproj`, `*.props`, `*.cs`, `*.json` returned nothing |
| No Dockerfile, no docker-compose, no CI folder | `ls` at root |
| Program.cs is 13 lines, two `AddReflekt*` plus two `UseReflekt*` calls | `API/Program.cs` |
| 44 endpoint classes in 12 feature folders, 22 `IEventHandler<T>` classes, 24 domain event records | counted from `API/Features/**`, `DOM/**/Events` |
| 7 aggregate roots, 1 child entity, 1 reference-data entity, 9 DbSets, 15 migrations | `DOM/*`, `PER/ReflektDbContext.cs`, `PER/Migrations` |
| Frontend: 6 routes, 6 views, 13 Pinia stores, 7 composables | `web/reflekt-web/src/router/index.ts`, `src/stores`, `src/composables` |

## 2. Drift table

| ID | Sev | Doc location | Code reality (file:line) | Suggested doc fix | Effort |
|---|---|---|---|---|---|
| AR-01 | High | `CLAUDE.md` "Repo Structure" (tree lines 52-65); same tree duplicated in `docs/conventions/project-rules.md:33-45` | `src/` holds only `BuildingBlocks/` and `Services/Reflekt/`. No `Modules/`, no `ApiGateway/`, no `docker-compose`, no Dockerfile. BuildingBlocks has no "app-specific (gRPC/integration event contracts)" part. Real dirs: `Blocks.Core`, `Blocks.Domain`, `Blocks.Exceptions`, `Blocks.Persistence`. Plus `tests/`, `web/reflekt-web/`, `ai-coding/`, `deploy.ps1`. | Replace the tree with the real one (section 3.1 below). Mark Modules / ApiGateway / compose as "template options, not present". Same fix in project-rules.md. | S |
| AR-02 | High | `CLAUDE.md` "Tech Stack" lines 90-91 (MassTransit + RabbitMQ, gRPC code-first listed as stack) | Neither is referenced by any csproj or `Directory.Packages.props`. v1.md itself says "Skipped for v1" (C5). Root CLAUDE.md is the only place that presents them as stack. | Drop both bullets or move them to "Not used (template options)". | S |
| AR-03 | High | `CLAUDE.md` "Project reference graph" lines 72-80 ("Domain references nothing") | `DOM/Reflekt.Domain.csproj:3` references `Blocks.Domain`. `PER/Reflekt.Persistence.csproj:7-8` references Domain + `Blocks.Persistence`. `Blocks.Persistence.csproj` references `Blocks.Domain` + `Blocks.Core`; `Blocks.Core` references `Blocks.Exceptions`. API has no direct Blocks reference and gets them transitively (`API/Middleware/GlobalExceptionMiddleware.cs:2-3` uses `Blocks.*`). Tests: UnitTests reference API + Domain + Blocks.Domain; IntegrationTests reference API. | Redraw the graph with the BuildingBlocks nodes (section 3.2). v1.md section 4 already has most of it but omits `Blocks.Core` and `Blocks.Exceptions`. | S |
| AR-04 | High | `v1.md:3` ("Status: Proposed, pending review") and section 14 review checklist (all boxes unchecked) | The app is built and shipped (`docs/product/index.md:3` says v1 Complete). The doc is a pre-implementation proposal never promoted to as-built. Sections 4-8 and 10-13 are largely not as-built (rows below). | Change status to "Accepted, partially superseded", or regenerate as an as-built doc. See verdict. | L |
| AR-05 | High | `v1.md` section 4 solution tree (lines 57-100) | (a) `BuildingBlocks.Domain/Persistence` are actually `Blocks.Core/Domain/Exceptions/Persistence`. (b) No `API/Events/` folder: all 22 handlers sit in `API/Features/<Slice>/<Action>/Broadcast*Handler.cs`. (c) No `DOM/Events/`: events are `DOM/<Aggregate>/Events/`. (d) `PER/Configurations/` is really `PER/EntityConfigurations/`. (e) `Reflekt.sln` is `Reflekt.slnx`. (f) `docker-compose.yml` does not exist. (g) Undocumented dirs/files: `API/Middleware/`, `API/Mappings/` (5 files), `API/SeedOptions.cs`, `PER/DbInitializer.cs`, `PER/Repositories/Repository.cs`. | Rewrite the tree from section 3.1. | M |
| AR-06 | Med | `v1.md:104,120,127` ("BuildingBlocks.Domain depends only on FastEndpoints.Abstractions") | `Blocks.Domain.csproj:3` references the full `FastEndpoints` package (for `IEvent`, `Blocks.Domain/IDomainEvent.cs:3`). `Blocks.Persistence.csproj` also pulls `FastEndpoints` and `Microsoft.EntityFrameworkCore`. | Correct the dependency claim. | S |
| AR-07 | High | `v1.md` section 5.1 aggregate table (lines 137-143) | 7 ARs, not 5: `User`, `Team`, `Sprint`, `Participant`, `Card`, `CardGroup`, `Vote`. `Vote` is scoped by `(GroupId, ParticipantId)` (`DOM/Votes/Vote.cs:9-10`), not `(SprintId, CardId, ParticipantId)`; votes target card groups. `Team` has `OwnerId` (user FK, `DOM/Teams/Team.cs:10`). `EvidenceLink` is a child `Entity` of `Card` (confirmed, `DOM/Cards/EvidenceLink.cs:5`). `PhaseAction` is a keyless-by-Id reference entity, not an AR (`DOM/Sprints/PhaseAction.cs`). `docs/backlog.md` ("Already agreed" item 1) contradicts both ("Vote becomes CardVote value object inside Card"). | New aggregate table from section 3.3. Fix or retire backlog.md item 1. | M |
| AR-08 | High | `v1.md` section 5.2 invariants (lines 149-153) | Phase order is `Writing, Grouping, Voting, Discussing, Reporting` (`DOM/Sprints/SprintPhase.cs`); sprint lifecycle is the separate `SprintState` `Draft, Active, Closed` (`SprintState.cs`). Doc says `Writing → Voting → Discussing → Closed`. Transitions in `Sprint.Start/AdvancePhase/Close` (`DOM/Sprints/Behaviors/Sprint.cs`). `Vote`-rejects-ActionItem invariant no longer exists (votes target groups). Card/column coherence is in `Card.Create` (`Card.Create`, `DOM/Cards/Behaviors/Card.cs`) and does hold. | Replace with the real phase and state machine; keep the Card rule. | S |
| AR-09 | High | `v1.md` (whole), `CLAUDE.md`: no mention of the phase-action gating matrix | Central mechanism undocumented: `PhaseAction(Phase, Action)` table seeded with `HasData` (`PER/EntityConfigurations/PhaseActionConfiguration.cs`), cached once at first use in singleton `PhaseActionCache` (`API/Features/PhaseActions/PhaseActionCache.cs`), enforced inside endpoints (`CreateCardEndpoint`, `AdvancePhaseEndpoint`...), exposed at `GET /api/phase-actions` and mirrored by frontend `stores/policy.ts`. | Add a "Phase-action gating" subsection. | S |
| AR-10 | Med | `v1.md` section 5.3 (lines 155-167) | Mechanism matches (reflection dispatch in `Blocks.Persistence/DomainEventPublishingInterceptor.cs:44-66`), but: publish mode is `Mode.WaitForNone` (line 66), so handlers are fire-and-forget; events are cleared in `SavingChangesAsync` before the save, not after publish; event list is 24 types, not the 12 named; 3 events have no handler (`TeamCreatedEvent`, `SprintCreatedEvent`, `SprintStartedEvent`). Interceptor is registered **singleton** with instance field `_pendingEvents` (`PER/DependencyInjection.cs:18`, interceptor line 17), so concurrent saves share one list. Flag as an observation for the code audit, not a doc error. | Update the event list and dispatch description; note fire-and-forget. | S |
| AR-11 | High | `v1.md` section 6 slice table (lines 180-188) | 12 slices / 44 endpoints, doc shows 7 slices / 19 endpoints. Renames: `UpdateCard` -> `EditCard`, `GetSprintCards` -> `ListCards`. New slices: `Auth` (3), `CardGroups` (9), `Health`, `PhaseActions`, `PreviousActions` (2). New endpoints in old slices: Sprints `AdvancePhaseCheck`, `GetSprintSummary`, `UpdateColumns`; Participants `GetParticipants`, `JoinSprintGoogle`, `KickParticipant`; Teams `DeleteTeam`; Votes `GetMyVotes`; Reports `GetParticipantReport`. Vote routes are under `/card-groups/{groupId}/votes`. Request/response/validator live in `*Command.cs` / `*Contracts.cs` siblings, not "one folder with Validator class"; only 9 validator classes exist. Doc's "logging post-processor registered globally in Program.cs" (line 190) does not exist: no `PostProcessor` anywhere; Program.cs has no registrations. | Regenerate the table from the endpoint list in section 3.4. | M |
| AR-12 | High | `v1.md:210` ("Email allowlist `Auth:AllowedEmails`, not-invited page"), `docs/backlog.md` (item 7 Teams) | Nothing in `src/` reads `Auth:AllowedEmails`. `appsettings.json` has no such key. The only hits are leftover config lines in 6 integration tests (`tests/.../CardEndpointTests.cs:40` etc.). Sign-in auto-provisions any Google user into `Users` (`API/Auth/AuthenticationConfiguration.cs:51-71`). GRAPH_REPORT still lists "Auth Allowlist" / "AuthOptions" communities. | Delete the allowlist paragraph; state "any Google account may sign in". Clean the stale test config keys in the code audit. | S |
| AR-13 | High | `v1.md` section 7.1 policies (lines 205-208) | Policies are scheme gates only: `ScrumMaster` = cookie scheme + authenticated user, `Participant` = GuestToken scheme + authenticated user, `OwnerOrScrumMaster` = either scheme (`API/Auth/AuthorizationConfiguration.cs:10-22`). The `Team.CreatorEmail == email` check does not exist (`Team` has `OwnerId`). Ownership is checked inside each endpoint, e.g. `team.OwnerId != userId` (`API/Features/Sprints/CreateSprint/CreateSprintEndpoint.cs:22`), with the user id from the `NameIdentifier` claim that `OnGoogleTicketReceived` overwrites with the DB id (`AuthenticationConfiguration.cs:73-77`). Sprint match for guests is a claim check in each endpoint (`CreateCardEndpoint.cs`). | Rewrite the policies paragraph: "policy = who may authenticate; resource ownership = endpoint code". | M |
| AR-14 | Med | `v1.md` section 7.1 table; `CLAUDE.md` Tech Stack bullet "Google OAuth ... cookie auth" | Undocumented: fourth policy `GoogleUser` (`Policies.cs:8`, used by `JoinSprintGoogle`); participants can join with Google (`Participant.JoinWithGoogle`, `Participant.UserId`, `DOM/Participants/Behaviors/Participant.cs`); `User` aggregate with case-insensitive email (migration `AddUserEmailNocaseCollation`); `Auth:Google:ClientId/Secret` must be non-empty or the host fails (README documents this; v1.md does not). Guest token accepted from `X-Guest-Token`, `access_token` query, or `Authorization: Bearer` (`GuestTokenAuthenticationHandler.cs:18-20`), token = raw GUID stored in `Participants.GuestToken`. Scopes are `openid email profile` (`AuthenticationConfiguration.cs:39-41`); CLAUDE.md says "email + profile". Root CLAUDE.md never mentions the GuestToken scheme (only the service CLAUDE.md does). | Add User, Google participants, GoogleUser policy, token transports. | M |
| AR-15 | Med | `v1.md` section 7.2 SignalR (lines 212-218) | Hub is `[Authorize(GuestToken)]` (`API/Hubs/SprintHub.cs`). Hub methods: `JoinSprint`, `LeaveSprint`, `ToggleReady`, `StartTimer`, `PauseTimer`, `ResumeTimer`, `ResetTimer`, `AddTime` (doc: only `JoinSprint`). Server-to-client names: 26 distinct (`CardCreated`, `CardUpdated`, `CardDeleted`, `CardsGrouped`, `CardUngrouped`, `ColumnConfigUpdated`, `EvidenceLinkFetched`, `GroupCreated`, `GroupDiscussedChanged`, `GroupDissolved`, `GroupFocusCleared`, `GroupFocused`, `GroupTitleChanged`, `ParticipantJoined`, `ParticipantKicked`, `ParticipantLeft`, `PhaseChanged`, `ReadinessChanged`, `ReadinessReset`, `ReadinessSnapshot`, `ReviewStatusChanged`, `SprintClosed`, `TimerSnapshot`, `TimerUpdated`, `VoteCast`, `VoteRetracted`); doc lists 9. Doc claim "every push goes to a group" is false for `ReadinessSnapshot` / `TimerSnapshot` (`Clients.Caller` in `SprintHub.JoinSprint`). Hub keeps **static in-memory state** (`ReadinessState`, `ConnectionState`, hub lines 104-107) and `TimerService` is a singleton with `System.Threading.Timer`s: the app only works as a single instance. Timer expiry callback is wired in `UseReflektApi` (`API/DependencyInjection.cs` (`UseReflektApi`)). SM-only hub calls re-resolve identity by `AuthenticateAsync(Cookies)` (hub `RequireScrumMasterAsync`). | Replace the event list; add readiness, timer, and the single-instance constraint. | M |
| AR-16 | Low | `v1.md` section 7.3 OpenGraph | Substantially accurate: bounded `Channel<FetchJob>` (100), 5 s client timeout, 2 MB cap, SSRF guard, AngleSharp (`API/OpenGraph/OpenGraphConfiguration.cs`, `OpenGraphFetcher.cs:55`). Differences: handler is `BroadcastAndFetchOnEvidenceLinkAttachedHandler` (broadcast and enqueue in one); Jira-detected links skip the fetch and are created `FetchStatus.Ok` (`DOM/Cards/EvidenceLink.cs:35-44`); `EvidenceLinkSourceDetector` (GitHub / Jira / Generic) and config key `EvidenceLinks:JiraDomain` are undocumented. | Add source detection and the Jira short-circuit. | S |
| AR-17 | High | `v1.md` section 7.4 line 249 ("Auto-apply on startup in Development only ... Production requires explicit `dotnet ef database update`") | `Database.Migrate()` runs on every start in every environment (`PER/DependencyInjection.cs:47`); only the seed is Development-only **and** needs `Seed:Enabled=true` (line 49; default false in `appsettings.json`). README states the correct behaviour. Also creates the `data/` directory (lines 41-43). | Fix the sentence; this is a deploy-relevant fact (Azure runs `/home/data/reflekt.db`, `docs/deployment-azure.md:47`). | S |
| AR-18 | Med | `v1.md` sections 7.5 and 12 ("No custom exception hierarchy yet", "Exception hierarchy + middleware (premature)") | Exists: `Blocks.Exceptions` (`HttpException`, `NotFoundException`, `BadRequestException`, `UnauthorizedException`), `Blocks.Core.Guard`, `Blocks.Domain.DomainException`, and `API/Middleware/GlobalExceptionMiddleware.cs` registered first in `UseReflektApi` (`API/DependencyInjection.cs`, first `Use` in `UseReflektApi`). But the hierarchy is effectively dead: nothing outside the middleware's `switch` references the exception types (grep over `src` excluding BuildingBlocks), and `Guard` has no callers. Domain throws plain `InvalidOperationException`/`ArgumentException`; the middleware maps `InvalidOperationException` to 500 (default arm). | Document middleware + status mapping. Note unused building blocks (candidate for the code audit, not a doc fix). | S |
| AR-19 | Med | `v1.md` section 8 NuGet table | Wrong placement/extras: `Microsoft.AspNetCore.Authentication.Cookies` and `Microsoft.AspNetCore.SignalR` are shared-framework, not package references. `Microsoft.EntityFrameworkCore.Design` is in **API** (`PrivateAssets=all`, `Reflekt.API.csproj:18`), not Persistence. Missing: `Microsoft.EntityFrameworkCore` (Blocks.Persistence), `Microsoft.Data.Sqlite`, test stack (xUnit 2.9.3, FluentAssertions 7.2.0, `Mvc.Testing`, coverlet). All Microsoft.* at **9.0.4 on net10.0**, FastEndpoints 5.34.0, FluentValidation 11.11.0, Mapster 10.0.0, Scalar 2.5.3, AngleSharp 1.2.0 (`Directory.Packages.props`). Stack claim ".NET 10" is true only for the TFM. | Regenerate table from `Directory.Packages.props`; call out the 9.0.4 / net10.0 mismatch. | S |
| AR-20 | Low | `v1.md` section 9 / `CLAUDE.md` "Port Convention" | Only the HTTP dev port is real: `4001` (`API/Properties/launchSettings.json:8`, `vite.config.ts` proxy). No HTTPS profile, no container (8080/8081/8082), no compose. CLAUDE.md's table is a template with `TBD` prefix and `TBD` service index (lines 108, 127); the real prefix/index (40 / 1) live in v1.md section 9 and `src/Services/Reflekt/CLAUDE.md:5`. Production is Azure App Service (`deploy.ps1`, `docs/deployment-azure.md`). | Fill the CLAUDE.md prefix/index; mark container ports and compose as not applicable; mention App Service. | S |
| AR-21 | Med | `v1.md` section 10 Frontend | 3 stores named; real 13: `auth`, `card`, `cardGroup`, `guest`, `participant`, `policy`, `previousActions`, `readiness`, `report`, `sprint`, `teams`, `timer`, `vote`. Undocumented: 6 routes with a `beforeEach` guard that awaits `/auth/me` (`router/index.ts`), `useSprintHub` as the single SignalR client (registers 21 handlers), `useTheme` (dark/light, F31/F32), `components/ui/Base*` primitives, `lucide-vue-next`, Tailwind v4 via `@tailwindcss/vite`. No shadcn-vue is used. Accurate parts: Vite `outDir`/`emptyOutDir` (`vite.config.ts`), SPA fallback (`API/DependencyInjection.cs` (`MapFallbackToFile`)), localStorage key `reflekt:sprint:{id}:token` (`stores/guest.ts:4`). Dev proxy (`/api`, `/auth`, `/signin-google`, `/hubs` ws) is undocumented in v1.md (only in the web README). | Rewrite section 10 from section 3.7 below. | M |
| AR-22 | Med | `v1.md` section 11 testing | `FastEndpoints.Testing` is not used. No "endpoint logic unit tests": `tests/Reflekt.UnitTests` holds only domain tests (7 files) plus the evidence-link source detector. Integration tests use `WebApplicationFactory<Program>`, file-backed SQLite and a `TestAuthHandler` that replaces the cookie scheme (`tests/Reflekt.IntegrationTests/CardEndpointTests.cs:25-60`). No SignalR test (grep for `HubConnection`/`SignalR` in `tests/` empty), no tests for CardGroups, PreviousActions, Reports, timer. No Vitest (not in `package.json`), though `docs/conventions/project-rules.md` still tells agents to run `npx vitest run`. About 163 `[Fact]` (59 unit, 104 integration). | Rewrite the table from reality; state the gaps. | S |
| AR-23 | Low | `v1.md` sections 12-13 (deferred items, open questions) | Stale: "exception hierarchy + middleware" and "docker compose" listed as deferred; open question 4 (guest token transport) and 5 (Swagger) are answered by code. | Delete or mark resolved. | S |
| AR-24 | High | `v1.md` (scope) vs features 23-32 (`docs/backlog.md:188-246`) | The architecture doc stops at v1. Not reflected anywhere in it: `User` aggregate and team ownership (F23, migration `AddUserAndMultiTeam`), multi-team and `TeamSettings:MaxTeamsPerUser` (F24), `DeleteTeam` (F25), Google participants (F26), configurable columns `Sprint.Column1..3Name/Color` + `ColumnColor` VO (F27, `AddConfigurableColumns`), `AdvancePhaseCheck` inline controls (F28), timers `Sprint.*TimerDefault`, `TimerService`, hub timer methods (F29, `AddTimerDefaults`), `ReviewStatus`/`PreviousActions` (F30, `AddReviewStatus`), dark/light theme (F31/F32, frontend only). Earlier post-v1 additions also missing: card grouping, discussion flow, kick participant, focus. | Cover these in the new "as-built" doc (section 3). | L |
| AR-25 | Med | `docs/architecture/index.md` | Is just `@v1.md`. Anything that loads the index gets a "Proposed" v1 proposal as "the architecture". There is no map, no "current state" doc. | Make index.md the one-page map; keep v1.md as the decision log. | S |
| AR-26 | High | `graphify-out/GRAPH_REPORT.md` (header line 1: dated 2026-04-21; 1004 nodes) | Generated 3+ weeks before the latest migration (2026-05-19). God nodes are minified JS from the old Vite bundle: `error()`, `push()`, `F()`, `send()` ... (God Nodes section); 8 of the first 17 communities are minified bundle or SignalR client runtime. Graph includes deleted Vue scaffold (`HelloWorld`, `TheWelcome`, `Icon*`, `counter`, `AboutView`, `HomeView`), the removed allowlist, and old `BuildingBlocks.Domain` assembly names. Misses `User`, `CardGroup`, timers, previous actions. `graph.json` is 1.29 MB, all three files tracked. Cause: `wwwroot/` build output was inside the corpus (it is git-ignored now: `.gitignore:46`). | Regenerate with `graphify update .` after excluding `wwwroot/`, `bin/`, `obj/`; or drop the tracked output. Cannot be fixed by edit. | M |
| AR-27 | Med | `.claude/README.md:34,45,54` and `CLAUDE.md` line 12-13 | "36 skills" but 37 skill folders exist (`.claude/skills`). `docs/kb/` and `docs/kb/index.md` are referenced as the domain knowledge map but do not exist (README.md:105 already admits this; `.claude/README.md` still presents it as live). CLAUDE.md references `graphify-out/wiki/index.md`, absent (conditional, harmless). About 10 template skills describe features this app never had (gRPC, Redis, MediatR, module/service scaffolding; admitted in `README.md:104`). `.claude/hooks/audit-logger.js`, `.claude/verify.json`, `.claude/settings.local.json` are not described. Agent table matches the 8 agent files. | Fix the count; drop or footnote the kb lines; list hooks/verify.json. | S |
| AR-28 | Med | `CLAUDE.md` "EF Core Migrations" (lines 153-161) and `docs/conventions/project-rules.md` | The paths `Services/{Svc}/{Svc}.Persistence` only resolve from `src/`, not the repo root; the doc never says so. There is no `dotnet-tools.json` (no `.config/` dir), so `dotnet-ef` must be installed globally; the README does not mention it. There is no `IDesignTimeDbContextFactory` (grep), so `dotnet ef` runs the real host: `UseReflektPersistence` would call `Migrate()` and the Google handler needs non-empty client values (see `tests/.../TestGoogleCredentials.cs`). The last point is unverified at runtime; I read the code but did not run `dotnet ef`. | Give root-relative commands (`-p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API`), say how to install `dotnet-ef`, note the user-secrets prerequisite. Verify the host-boot claim before writing it. | S |
| AR-29 | Med | `CLAUDE.md` Tech Stack line 85 ("pre/post processors handle cross-cutting (logging, auth enrichment)"; "validator is a sibling class") | No processors exist. Cross-cutting is `GlobalExceptionMiddleware`, scheme/policy auth, and the save interceptor. Validators live inside `*Command.cs` files, not separate classes per endpoint; only 9 of 44 endpoints have one. Mapping is `Adapt<T>()` via Mapster static config scanned from the assembly (`API/DependencyInjection.cs`) with profiles in `API/Mappings/`. | Reword to what the code does. | S |
| AR-30 | Low | `README.md` | Already corrected in F33 and consistent with code: stack, ports, migrate-on-start, seed flags, commands, user-secrets keys. Gaps only: no mention of `GuestToken` scheme names or SignalR hub path, no pointer to an architecture map (none exists). `.claude/README.md` and `README.md:98` say only F31 and F32 have `definition/`; not re-verified (outside this dimension). | Add one line linking to the new architecture map. | S |
| AR-31 | Low | `docs/product/index.md` | Matches reality: v1 "Complete (shipped)", v2 "Draft". But `docs/product/v2.md` status "Draft" while backlog items 23-32 (v2 features) are Done, so v2 is partly shipped. Not an architecture claim. | Optionally change v2 status to "Partially shipped (F23-F32)". Product scope, not mine to decide. | S |
| AR-32 | Low | `docs/conventions/ef-core.md` (also `known-deviations.md`) | Contains examples from a different product (`Ticket`, `IssueType`, `BugRatioTarget`, `SprintIssueSyncService`, `Memberships`) that do not exist in this codebase. Teaching-repo hazard: reads as Reflekt guidance. | Mention to the conventions audit owner (outside architecture reference scope). | S |
| AR-33 | Low | `docs/backlog.md` section A (lines 1-40) | An "Architect Backlog" of pre-Foundation ACKs that repeats stale decisions (4 ARs with CardVote VO; allowlist; "Foundation writes launchSettings"). Contradicts v1.md section 5.1 and the code. | Mark section A as historical, or fold into the new map. | S |

Counts: 33 drift rows. High 14 (AR-01, 02, 03, 04, 05, 07, 08, 09, 11, 12, 13, 17, 24, 26), Med 12 (AR-06, 10, 14, 15, 18, 19, 21, 22, 25, 27, 28, 29), Low 7 (AR-16, 20, 23, 30, 31, 32, 33).

Things verified as accurate (no drift): FastEndpoints + FluentValidation + Mapster + EF Core SQLite + SignalR; no repository interfaces (concrete repositories, `PER/Repositories/*`); no MediatR/Swashbuckle; Scalar and `MapOpenApi` gated to Development (`API/DependencyInjection.cs` (`UseReflektApi`)); SQLite path `./data/reflekt.db` (`appsettings.json:3`); single hub at `/hubs/sprint` with `sprint:{id}` groups; Vite builds into `API/wwwroot`; solution is `.slnx`; service index 1 / port 4001; guest token localStorage key; OpenGraph pipeline shape; EvidenceLink as child entity; `Program.cs` shape (orchestration only).

## 3. Architecture-map inventory (what a one-page map needs, with real names)

### 3.1 Solution layout

```
Reflekt.slnx
Directory.Build.props        net10.0, Nullable, ImplicitUsings
Directory.Packages.props     central package management
deploy.ps1                   frontend build + dotnet publish + zip + az webapp deploy (Azure App Service, Linux)
src/BuildingBlocks/
  Blocks.Exceptions   HttpException, NotFound/BadRequest/UnauthorizedException (practically unused)
  Blocks.Core         Guard, GuardExtensions (no callers)           -> Blocks.Exceptions
  Blocks.Domain       Entity<TId>, AggregateRoot<TId>, IDomainEvent (: FastEndpoints IEvent), IHasDomainEvents, DomainException   (+FastEndpoints pkg)
  Blocks.Persistence  ApplicationDbContext, RepositoryBase<,,>, EntityConfiguration<T>, DomainEventPublishingInterceptor   -> Blocks.Domain, Blocks.Core (+FastEndpoints, EF Core)
src/Services/Reflekt/
  Reflekt.Domain       aggregates, enums, domain events                       -> Blocks.Domain
  Reflekt.Persistence  ReflektDbContext, EntityConfigurations, Repositories, Migrations (15), DbInitializer, DependencyInjection   -> Reflekt.Domain, Blocks.Persistence (+EF Core Sqlite)
  Reflekt.API          Features/, Auth/, Hubs/, OpenGraph/, Middleware/, Mappings/, DependencyInjection, Program, wwwroot (Vite output, git-ignored)   -> Domain, Persistence
  CLAUDE.md            service axes (index 1, auth schemes, hub)
tests/Reflekt.UnitTests         -> API, Domain, Blocks.Domain      (domain tests)
tests/Reflekt.IntegrationTests  -> API                              (WebApplicationFactory<Program>, SQLite file, TestAuthHandler)
web/reflekt-web                 Vue 3 SPA, Vite
ai-coding/, docs/, .claude/, graphify-out/   process artifacts (not runtime)
```

### 3.2 Project reference graph (real)

```
Reflekt.API ──▶ Reflekt.Domain ──▶ Blocks.Domain
     │
     └──▶ Reflekt.Persistence ──▶ Reflekt.Domain
                    │
                    └──▶ Blocks.Persistence ──▶ Blocks.Domain
                                │
                                └──▶ Blocks.Core ──▶ Blocks.Exceptions
API reaches Blocks.* transitively only.
```

### 3.3 Domain model

| Aggregate root | Key state | Factory / behaviours | Events raised |
|---|---|---|---|
| `User` | Email (NOCASE), Name, GivenName | `Create`, `UpdateProfile` | none |
| `Team` | Name, OwnerId (User) | `Create` | `TeamCreatedEvent` |
| `Sprint` | TeamId, State (Draft/Active/Closed), CurrentPhase, VotesPerParticipant, Column1..3 Name/Color (`ColumnColor` VO), Writing/Voting/DiscussingTimerDefault, FocusedGroupId | `Create`, `Start`, `AdvancePhase`, `Close`, `UpdateColumnConfig`, `SetFocus`, `ClearFocus`, `GetTimerDefaultForPhase` | `SprintCreated`, `SprintStarted`, `PhaseAdvanced`, `SprintClosed`, `ColumnConfigUpdated`, `GroupFocused`, `GroupFocusCleared` |
| `Participant` | SprintId, DisplayName, GuestToken (GUID), UserId? | `Join`, `JoinWithGoogle`, `Kick` | `ParticipantJoined`, `ParticipantKicked` |
| `Card` | SprintId, ParticipantId, ColumnType (Column1-3, ActionItems), CardType (Note, ActionItem), Text, AssigneeName, GroupId?, ReviewStatus (None, Done, CarriedOver, Dropped), ReviewedInSprintId; child `EvidenceLink` list | `Create`, `Update`, `SetGroupId`, `MarkDeleted`, `AttachEvidenceLink`, `UpdateEvidenceLinkMetadata`, `DetachEvidenceLink`, `SetReviewStatus` | `CardCreated`, `CardUpdated`, `CardDeleted`, `EvidenceLinkAttached/Fetched/Detached`, `ReviewStatusChanged` |
| `CardGroup` | SprintId, ColumnType, Title?, DiscussedAt? | `Create`, `SetTitle`, `RecordCardAdded/Removed`, `MarkDiscussed`, `UnmarkDiscussed` | `CardGroupCreated`, `CardGroupTitleChanged`, `CardsGrouped`, `CardUngrouped`, `GroupDiscussedChanged` |
| `Vote` | GroupId, ParticipantId | `Create`, `MarkRetracted` | `VoteCast`, `VoteRetracted` |
| `EvidenceLink` (child entity of Card) | Url, Source (GitHub/Jira/Generic), ExternalKey, Title/Description/IconUrl/ImageUrl, FetchStatus, FetchedAt | `Create`, internal `UpdateMetadata` | via Card |
| `PhaseAction` (reference data) | composite key (Phase, Action) | seeded `HasData`, 21 rows | none |

Enums: `SprintPhase` {Writing, Grouping, Voting, Discussing, Reporting}; `SprintState` {Draft, Active, Closed}; `SprintAction` (16 values).
Aggregates are referenced by int ID only. Events carry the aggregate reference (`record XEvent(Aggregate A) : IDomainEvent`).

### 3.4 Feature slices (API/Features), 12 slices, 44 endpoints

| Slice | Endpoints | Policy |
|---|---|---|
| Auth | `LoginEndpoint` GET `/auth/google/login` (anon), `LogoutEndpoint` POST `/auth/logout`, `MeEndpoint` GET `/auth/me` | ScrumMaster (cookie) except Login |
| Teams | `CreateTeam`, `GetMyTeams`, `DeleteTeam` under `/api/teams` | ScrumMaster |
| Sprints | `CreateSprint`, `GetSprint`, `GetTeamSprints`, `StartSprint`, `AdvancePhase`, `AdvancePhaseCheck`, `CloseSprint` under `/api/teams/{teamId}/sprints[...]`; `UpdateColumns` PUT `/api/sprints/{id}/columns`; `GetSprintSummary` GET `/api/sprints/{id}/summary` (anon) | ScrumMaster; summary anon |
| Participants | `JoinSprint` (anon), `JoinSprintGoogle` (GoogleUser), `GetCurrentParticipant`, `GetParticipants`, `KickParticipant` | mixed |
| Cards | `CreateCard`, `EditCard`, `DeleteCard`, `ListCards` under `/api/sprints/{id}/cards` | Participant / OwnerOrScrumMaster |
| CardGroups | `CreateGroup`, `GetGroups`, `UpdateGroupTitle`, `DissolveGroup`, `AddCardToGroup`, `RemoveCardFromGroup`, `FocusGroup`, `ClearFocus`, `ToggleDiscussed` under `/api/sprints/{id}/card-groups`, `/focus` | ScrumMaster (GetGroups: OwnerOrScrumMaster) |
| Votes | `CastVote`, `RetractVote` (`/card-groups/{groupId}/votes`), `GetMyVotes` (`/votes/mine`) | Participant |
| EvidenceLinks | `AttachEvidenceLink`, `DetachEvidenceLink`, plus `FetchEvidenceLink/BroadcastEvidenceLinkFetchedHandler` (handler only) | OwnerOrScrumMaster |
| PreviousActions | `GetPreviousActions`, `SetReviewStatus` | OwnerOrScrumMaster / ScrumMaster |
| Reports | `GetSprintReport` (`/api/teams/{t}/sprints/{s}/report`), `GetParticipantReport` (`/api/sprints/{s}/report`) | OwnerOrScrumMaster |
| PhaseActions | `GetPhaseActions` GET `/api/phase-actions` (anon) + `PhaseActionCache` | anon |
| Health | `HealthEndpoint` GET `/health` (anon) | anon |

Slice layout convention: `Features/<Slice>/<Action>/<Action>Endpoint.cs`, optional `<Action>Command.cs` (request + validator), `Broadcast*Handler.cs` for the SignalR push; shared DTOs in `<Slice>Contracts.cs`; Mapster profiles in `API/Mappings/*`.

### 3.5 Real-time

- One hub `SprintHub`, route `/hubs/sprint`, `[Authorize(GuestToken)]`, group `sprint:{sprintId}`; token via `accessTokenFactory` (query `access_token`).
- Client to server: `JoinSprint`, `LeaveSprint`, `ToggleReady`, `StartTimer`, `PauseTimer`, `ResumeTimer`, `ResetTimer`, `AddTime`.
- Server to client (26 names, listed in AR-15). Broadcast path: aggregate event -> interceptor -> `IEventHandler<T>` -> `IHubContext<SprintHub>`.
- In-process state (single instance only): `SprintHub.ReadinessState`, `ConnectionState` (static), `TimerService` (singleton, `System.Threading.Timer` per sprint).

### 3.6 Auth

| Scheme | Handler | Credential | Used by |
|---|---|---|---|
| `Cookies` (default) | ASP.NET cookie, 401/403 instead of redirects | auth cookie, SameSite Lax, HttpOnly | Scrum Master (team owner) |
| `Google` | `AddGoogle`, scopes openid/email/profile, `OnTicketReceived` upserts `User` and rewrites `NameIdentifier` to DB id | OAuth | sign-in, `JoinSprintGoogle` |
| `GuestToken` | `GuestTokenAuthenticationHandler` looks up `Participant.GuestToken` | `X-Guest-Token` header, `access_token` query, or Bearer | participants and SignalR |

Policies (`Policies.cs`): `ScrumMaster`, `Participant`, `OwnerOrScrumMaster`, `GoogleUser`; all scheme gates. Resource ownership is endpoint code (`team.OwnerId == User.GetUserId()`, sprint/participant claims `sprintId`, `participantId`, `displayName`). No email allowlist.

### 3.7 Persistence

- `ReflektDbContext : ApplicationDbContext`, 9 DbSets (`Users, Teams, Sprints, Participants, Cards, CardGroups, Votes, EvidenceLinks, PhaseActions`); `ApplyConfigurationsFromAssembly`; configs in `PER/EntityConfigurations/*` (internal sealed, inherit `EntityConfiguration<T>`).
- Repositories: concrete, `Repository<T> : RepositoryBase<ReflektDbContext,T>`, one per aggregate (User, Team, Sprint, Participant, Card, Vote, CardGroup), registered scoped.
- `DomainEventPublishingInterceptor` (singleton) registered on the context.
- SQLite `ConnectionStrings:Reflekt` = `Data Source=./data/reflekt.db`. `UseReflektPersistence`: create dir, `Migrate()` always, `DbInitializer.SeedAsync` only in Development with `Seed:Enabled`.
- 15 migrations from `AddTeam` (2026-04-19) to `AddReviewStatus` (2026-05-19).

### 3.8 Background and cross-cutting

- `OpenGraphBackgroundService` (hosted) reads `Channel<FetchJob>` (bounded 100) fed by `BroadcastAndFetchOnEvidenceLinkAttachedHandler`; `OpenGraphFetcher` (5 s, 2 MB, SSRF guard, AngleSharp); writes back via `Card.UpdateEvidenceLinkMetadata`, which raises `EvidenceLinkFetchedEvent` and a SignalR push.
- `GlobalExceptionMiddleware` (first in pipeline). Pipeline order in `UseReflektApi`: exception middleware, default files, static files, routing, authentication, authorization, FastEndpoints, hub, timer callback wiring, Development-only OpenAPI + Scalar, SPA fallback `index.html`.
- Options: `Seed` (`Enabled`, `SeedUserEmail`), `TeamSettings:MaxTeamsPerUser`, `EvidenceLinks:JiraDomain`, `Auth:Google:ClientId/ClientSecret` (user-secrets; must be non-empty).
- Singletons with state: `PhaseActionCache`, `TimerService`.

### 3.9 Request flow (mutation) for the map

`Browser fetch/SignalR` -> `GlobalExceptionMiddleware` -> auth (cookie or GuestToken) + policy -> FastEndpoints binds request, runs FluentValidation if a validator exists -> endpoint loads aggregate via concrete repository, checks ownership/phase gate (`PhaseActionCache`) -> aggregate method raises `IDomainEvent` -> `repository.SaveChangesAsync` -> interceptor `SavingChanges` collects events, `SavedChanges` publishes each (reflection, `Mode.WaitForNone`) -> `IEventHandler<T>` pushes to `sprint:{id}` group via `IHubContext` (and for evidence links enqueues a `FetchJob`) -> response -> other clients update Pinia stores from hub events.

### 3.10 Frontend

- `web/reflekt-web`: Vue 3.5, vue-router 5, Pinia 3, `@microsoft/signalr` 10, Tailwind 4 (`@tailwindcss/vite`), `lucide-vue-next`, Vite 8, TypeScript 6, vue-tsc. No test runner.
- Routes (`router/index.ts`): `/` welcome, `/teams` (auth), `/teams/:teamId/sprints` (auth), `/teams/:teamId/sprints/:sprintId` (SM sprint view), `/sprint/:sprintId` (retro room, guest or SM), `/teams/:teamId/sprints/:sprintId/report`. `beforeEach` awaits `authStore.fetchMe()`; signed-in users on `/` go to `/teams`.
- Views (6): Welcome, TeamDashboard, SprintList, Sprint, RetroRoom, Report.
- Stores (13): auth, card, cardGroup, guest, participant, policy, previousActions, readiness, report, sprint, teams, timer, vote. Stores call `fetch` directly (no shared api module).
- Composables (7): `useSprintHub` (single module-level SignalR connection, 21 handlers), `useRetroRoom`, `useRetroBoardActions`, `useReadiness`, `useTimer`, `useTheme`, `useToast`.
- Components by folder: `layout/`, `retro/` (board, columns, cards, groups, discussion, timer, participant list, mobile tab bar, report), `sprints/`, `teams/`, `reports/`, `ui/` (Base* primitives).
- Dev: `npm run dev` on 5173, proxy `/api`, `/auth`, `/signin-google`, `/hubs` (ws) to 4001. Build: `vue-tsc` then Vite into `API/wwwroot`.

### 3.11 Deployment and tooling

Azure App Service (Linux) via `deploy.ps1 -AppName -ResourceGroup`; SQLite at `/home/data/reflekt.db`; no container. `.claude/verify.json` declares build/unit/full roles. No CI.

## 4. Verdict on fixability

| Document | Verdict | Why |
|---|---|---|
| `docs/architecture/v1.md` | Regenerate (or rewrite as as-built, keep the conflict log) | 14 High rows trace to it; sections 4, 5, 6, 7.1, 7.2, 7.4, 7.5, 8, 10, 11 are wrong or incomplete and F23-F32 are absent. Editing in place would touch most paragraphs. Keep section 2 (conflict log) as the decision record. |
| `docs/architecture/index.md` | Rewrite (S) | Should hold the one-page map from section 3. |
| `CLAUDE.md` | Fixable by edit | AR-01, 02, 03, 20, 28, 29: prune template-only parts, correct the graph, fill the port prefix. Note it is also an instruction file for agents, so owner approval is needed per its own "Authority" rule. |
| `docs/conventions/project-rules.md` | Fixable by edit | Same repo-structure and graph fixes as CLAUDE.md. |
| `.claude/README.md` | Fixable by edit | AR-27. |
| `README.md` | Fixable by edit (one line) | AR-30. |
| `graphify-out/*` | Regenerate | AR-26; or untrack. |
| `docs/product/index.md` | No change needed | AR-31 optional. |

## 5. Open items I could not verify

- Whether `dotnet ef migrations add` boots the full host and runs `Migrate()` (AR-28). Needs a run.
- Whether `Microsoft.AspNetCore.OpenApi` lists the FastEndpoints routes at `/openapi/v1.json` (the docs claim Scalar is the UI; I did not start the app).
- Resolved package versions at restore time (I read only the declared central versions).
