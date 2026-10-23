# F38 — Backend Conventions Conformance

**Feature Spec:** `docs/specs/F38-BackendConventions/definition/spec.md`
**Model:** claude-opus-5-5[1m]
**Build slices:** 1–4 · 5–7 · 8–10 · 11–14 · 15–17; why: shared mechanisms (errors, claims, access, publication) · write paths with moved rules and moved broadcasts · the remaining endpoint areas and the hub · persistence, reads, limits and type layout · polish, the test fixture and verification
**Size:** 17 developer-built steps · 198 files · fullChecks: none — no fullChecks list declared
**Lane:** full — normal gear (as today, by size: 17 developer-built steps, 198 files)
**Build lane:** thorough — full team — normal gear
**Gear:** normal — campaign

## Context

Base commit: `1228e292cab7766891f9b08a42633200cbace18a` (F37 and F35 are on this branch). One service is affected, `src/Services/Reflekt`, plus `src/BuildingBlocks` and `tests/`. The spec is a restructuring: each job is done one way, every status code and every SignalR message stays as it is, and the two behaviour changes the spec lists are the only ones (spec § Decisions).

These standing rules bind every step, the owner's as owner answers:
- No new test cases, test classes or test files. An existing test is updated only where a fix changes what it asserts, or where a renamed symbol in its setup stops it compiling (plan P1, Decisions row 2).
- No business-rule registries.
- Login stays open to any Google account; `Auth:AllowedEmails` is not enforced (Step 16 removes the stale test setting).
- No package version change. Step 15 removes one package reference (`Mapster.DependencyInjection`, spec row BE-10); nothing else in `Directory.Packages.props` moves.
- Never push, never rewrite history, no git writes by the developer.
- Never edit `docs/backlog.md`, `.claude/skills/`, `web/reflekt-web/package.json` or `web/reflekt-web/package-lock.json`. F38 edits no frontend file at all: shared answer AR-2 moved the `CreateTeamForm.vue` edit to F39, which owns every SPA error consumer (Decisions row "AR-2"; the spec's Decisions row 2 is amended to match).
- Scratch files live under `.claude/tmp/f38-scratch/` and are deleted in Step 17.

Related:
- **F35** (done on this branch) supplies the transaction-aware interceptor, `Sprint.EnsureOpen()`, the redaction, the soft kick and the static connection registry that Step 10 moves into `ReadinessTracker`.
- **F39** (Ready) moves the SPA to the `code` field and the new body; F38 only produces it. **F39 merges before F38** (AR-2): Step 5 removes the `limit_reached: ` / `duplicate_name: ` message prefixes that `CreateTeamForm.vue` parses today, so F38 merged alone would lose the form's two specific messages.
- **F40** (Ready) records every code/skill difference this plan leaves (Step 17's list), the error-code set and the single-instance deployment.

## Scope

**In scope:** every row of the spec's Scope table — BE-03, BE-04, BE-05, BE-06, BE-08, BE-09, BE-10, BE-11, BE-16, BE-17, BE-18, BE-19 (rest), BE-23 (truncation), BE-25, BE-26, BE-27, BE-29, BE-30, BE-31, BE-32, BE-33, BE-34, BE-35, BE-38, SK-02, SK-03, SK-06, SK-08, SK-09, SK-10, SK-11 (rest), SK-12, SK-13, SK-15, SK-16, SK-19, SK-20, SK-21, SK-22, SK-24, SK-25, SK-26, SK-27, SK-28 (dead `IMapper`, twins, options), SK-30, TS-10. Where a row's "suggested fix" leaves a choice, the spec's "Required outcome" column picks it, and this plan picks the rest in `## Decisions`.

**Out of scope:** the spec's Out of scope; every new test; every frontend file (F39); every doc outside this feature folder (F40); registration method names (`AddReflekt*`, SK-28's naming part — the spec's row narrows SK-28 to the dead `IMapper`, the twins and the options); SK-29.

**Residual validators (spec row "Validation and limits", critic R2-03 handed to the plan):** none are added. After F35 every request with a body has a validator — 11 validators over the 11 body requests (census 2026-10-23). The only body inputs not fully checked by a validator are the ones whose limit is enforced in the domain after the access checks today (for example `CreateGroup`'s title over 100 characters). A validator runs before the handler, so adding one would answer such an input 400 before a 404 or 403 that today comes first, which changes the status for multi-fault inputs. Step 13 makes the validators and the domain read the same limit constants instead.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | error-handling | Follow | no | `ForbiddenException`, `ConflictException` with `Code`; one body with `code` and `errors[{field, message}]`; FastEndpoints response builder; spec overrides the skill's status for `DomainException` and the body shape | ill-fitting skill, logged |
| 2 | authorization-patterns | Follow | no | `GuestClaims`, typed accessors, merged and renamed policies, `Features/Account` | — |
| 3 | extract-feature-service | Follow | no | `SprintAccess` result (never orders failures); `PhaseActionPolicy` in Domain, `PhaseActionRepository`, singleton provider | — |
| 4 | persistence-patterns | Follow | no | interceptor logs each handler failure while the request never waits, handlers get `CancellationToken.None`; four handlers read through a scope | departures, logged (Decisions: the publisher row, the handler-scope row) |
| 5 | domain-patterns | Follow | no | `Sprint` guards carrying today's texts, discussed-count rule, `CardGroup.CreateSolo`, `TeamLimitCheck`, `HasActiveSprintAsync` | — |
| 6 | create-domain-event-handler | Follow | no | `GroupDissolvedEvent` + handler; `GroupCreated` through the event, newest-first order; `FocusHandover`, `CardRegrouping`, `CardGroupResponseBuilder` | — |
| 7 | extract-feature-service | Follow | no | `VoteBudget`; vote broadcasts through handlers; `MarkRetracted()` | — |
| 8 | error-handling | Follow | no | Cards, EvidenceLinks, Participants, PreviousActions: exceptions, access, claims; author check stays | — |
| 9 | extract-feature-service | Follow | no | `SprintReportBuilder`, F35 redaction kept | — |
| 10 | create-domain-event-handler | Follow | no | `ReadinessTracker`, hub constructor injection, `TimerSnapshot.ToPayload()`, `Sprint.HasTimer`, `TimerService` with injected hub context, handler rename | — |
| 11 | persistence-patterns | Follow | no | no EF in API; `MigrateAndSeedAsync`; `Query()` protected; `DbInitializer` split | — |
| 12 | persistence-patterns | Follow | no | `AsNoTracking` read variants, count/exists queries, `IReadOnlyList<T>`, `Remove` in the base, DTOs returned directly, `*Response` suffix | — |
| 13 | domain-patterns | Follow | no | limit constants on domain types, read by domain, validators, EF configs and the OpenGraph truncation | — |
| 14 | extract-endpoint-types | Follow | no | 28 endpoint files emptied into sibling `*Command.cs` / `*Query.cs`; shared types stay in `*Contracts.cs` | — |
| 15 | service-registration | Follow | no | options with `ValidateOnStart`; dead code; `Entity` transient guard; `ColumnColor` record; the BE-30 list item by item; Mapster map; naming and usings | — |
| 16 | (none) | — | no | one shared integration-test host in `tests/Reflekt.IntegrationTests/Helpers/` | gap: integration-test host fixture |
| 17 | (none) | — | no | acceptance greps, AC7 evidence run, AC8 traces, build, skill/code difference list, walkthrough (developer part) | — |
| 18 | (none) | — | no | `Owner: operator` — the rest of the AC10 walkthrough | — |

TDD is `no` on every step because the owner's rule forbids new tests (Decisions row 1).

## Domain Model Changes

- `Sprint`: guard methods that carry today's endpoint texts (`EnsureActive(string message)`), `EnsureEnoughGroupsDiscussed(int discussedCount)` with today's text "At least 2 groups must be discussed before advancing to Reporting.", `HasTimer` (the timed-phase check written once), `SetFocus` / `ClearFocus` unchanged in behaviour; `AdvancePhase()` stays parameterless so the existing `SprintTests` compile unchanged.
- `CardGroup`: `CreateSolo(int sprintId, ColumnType columnType)` raises no event; `Create` keeps raising `CardGroupCreatedEvent`; group-membership rules move in as behaviour methods throwing `ArgumentException` with no `paramName` and today's texts; `Dissolve()` raises the new `GroupDissolvedEvent(CardGroup Group)`.
- `Vote`: `MarkRetracted()` is now called by `RetractVote` (it exists today and raises `VoteRetractedEvent`).
- `PhaseActionPolicy` (new, `Reflekt.Domain/Sprints/PhaseActionPolicy.cs`): built from the `PhaseAction` rows; `IsAllowed(phase, action)`; `EnsureAllowed(phase, action)` throws `DomainException("{action} is not allowed in phase {phase}.")`.
- Limit constants on `Card`, `CardGroup`, `Sprint`, `Team`, `Participant`, `User`, `EvidenceLink`, `ColumnColor` (Step 13).
- `Entity` (`Blocks.Domain`): two transient entities (default `Id`) are equal only when they are the same reference; the hash code of a transient entity is its reference hash.
- `ColumnColor` becomes a `sealed record` with the same validation and the same EF conversion.
- `Participant.Join` / `JoinWithGoogle` share one private builder; `EvidenceLink.Create` becomes `internal`; `User.UpdateProfile` assigns directly. No new guard on `UpdateProfile` or `Card.SetGroupId` (Decisions row "BE-30 guards").
- Deleted: `ISprintAction`.

## Data Model Changes

None. No migration. The integration host's start-up `Migrate()` throws on a pending model change (EF Core 10), so the per-step integration runs catch any slip (spec AC9).

## Access order reference

Census of today's check order, 2026-10-23 (44 endpoints). Each endpoint keeps its order; **the code wins on any mismatch with this table**, and the developer records each endpoint's order before and after in its step block.

| Shape | Endpoints | Order today |
|---|---|---|
| Team-first | `DeleteTeam`, `AdvancePhaseCheck`, `AdvancePhase`, `CloseSprint`, `CreateSprint`, `GetSprint`, `GetTeamSprints`, `StartSprint`, `SetReviewStatus` | team missing → 404; not owner → 403; sprint missing or in another team → 404; then state or phase 409 |
| Sprint-first | `UpdateColumns`, every `CardGroups` endpoint, `KickParticipant`, the owner-or-guest list and report endpoints | sprint missing → 404; team missing or not owner → 403 (guest branch: `sprintId` claim mismatch → 401). The report endpoints also answer 404 for a sprint in another team (`sprint.TeamId != req.TeamId`), checked with the sprint-missing 404 |
| Card-first | `DeleteCard`, `EditCard`, `AttachEvidenceLink`, `DetachEvidenceLink` | card missing or in another sprint → 404; sprint missing → 404; closed → 409; author (participant claim) or owner → 403; phase → 409; then, for `EditCard` and `DeleteCard` on an ActionItem in Discussing, no `NameIdentifier` claim → 403 — except `Attach`/`Detach`, which run the phase 409 before the 403 |
| Group-first | `CastVote` | `sprintId` claim mismatch → 401; no `participantId` → 401; group missing or in another sprint → 404; sprint missing → 404; closed → 409; phase → 409; then the vote rules (budget, already voted) inside the serializable transaction |
| Guest, sprint-first | `RetractVote` | `sprintId` claim mismatch → 401; no `participantId` → 401; sprint missing → 404; closed → 409; phase → 409; no vote on the group → 409. **No group lookup** — none is added |
| Endpoint-specific | `CreateCard` | guest 401s; sprint missing → 404; closed → 409; phase → 409; then an ActionItem in Discussing whose cookie (`HttpContext.AuthenticateAsync` with the cookie scheme) carries no email claim → 403. The cookie check stays in the endpoint; `SprintAccess`'s cookie user id does not replace it |
| Special | `KickParticipant` | `State != Active` 409 before the owner 403 |
| Guest | guest-only endpoints | `sprintId` claim mismatch → 401; `participantId` missing → 401 |
| Anonymous (no `SprintAccess`) | `JoinSprint`, `GetSprintSummary`, `GetPhaseActions`, `Health`, `Login` | unchanged |

Identity precedence: a participant claim takes the participant branch even when an owner cookie is also present.

Hub texts that stay exactly: "Not authorized to join this sprint.", "Missing participant claims.", "Participant has been removed.", "Not authorized for this sprint.", "Only the Scrum Master can control the timer.", "Sprint not found.", "Sprint is closed.", "Timer is not available in phase {phase}.", "Invalid minutes value. Allowed: 1, 2, 5."; `RequireScrumMasterAsync` order: claims → participant → resolved user (`participant.UserId`, else the cookie) → sprint → team owner.

## Error codes (closed set)

| `code` | Status | Thrown by |
|---|---|---|
| `duplicate_name` | 409 | `CreateTeam` — a team with that name already exists for the user |
| `limit_reached` | 409 | `TeamLimitCheck` — the user owns `TeamSettings.MaxTeamsPerUser` teams |

The set lives in `Reflekt.API/Middleware/ErrorCodes.cs`; F40 copies it into the architecture doc.

## Implementation Steps

Every step closes with `dotnet build Reflekt.slnx` (0 `error CS`) and the `unit` role through `verify-run.js`. Each step with a **Runs:** line also runs the integration project through the declared `integration` role: `verify-run.js --role integration --slug F38-BackendConventions` (the role runs the whole project; the **Runs:** line names the files whose results the step block reports). The `full` role is the main session's: the team lead runs the complete suite at handover and at close.

**Code-trace accepts:** an accept marked *code trace* names the path the done check and the reviewer read. The developer writes the trace (file, method, the line that does it) into the step block.

**Per-endpoint record:** every step that rewrites an endpoint lists, per endpoint, its check order before and after (one line each, from the Access order reference) and every status and message it can return. A changed status or message other than the one-body change is a defect, not a deviation.

**Moved texts:** every message that moves from an endpoint into a domain method, a service or an exception keeps its exact text; the step block lists each moved text with its old and new location.

### Step 1 — One error convention and one error body
Follow error-handling. Files:
- `src/BuildingBlocks/Blocks.Exceptions/HttpException.cs`
- `src/BuildingBlocks/Blocks.Exceptions/ForbiddenException.cs` (new)
- `src/BuildingBlocks/Blocks.Exceptions/ConflictException.cs` (new)
- `src/BuildingBlocks/Blocks.Exceptions/Extensions.cs`
- `src/BuildingBlocks/Blocks.Core/Guard.cs`
- `src/Services/Reflekt/Reflekt.API/Middleware/GlobalExceptionMiddleware.cs`
- `src/Services/Reflekt/Reflekt.API/Middleware/ErrorCodes.cs` (new)
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs`
- `tests/Reflekt.IntegrationTests/TeamEndpointTests.cs`

What to build:
- `HttpException` gains an optional `Code` (string, null by default). `ForbiddenException` (403) and `ConflictException` (409, takes an optional code) are added at the skill's extension point.
- The middleware writes **one body** for every exception it maps: `statusCode`, `message`, `traceId`, `code` (only when set), `errors` (only for validation failures; an array of `{ "field", "message" }`), and `details` in Development only, as F35 left it. Keep F35's serializer options (camelCase, null members omitted) so absent members stay absent. `DomainException` → 409 and `ArgumentException` → 400 stay as F35 mapped them.
- FastEndpoints' own validation failures write the same body: set `Errors.ResponseBuilder` in the `MapFastEndpoints` configuration (`UseReflektApi`, `DependencyInjection.cs`) to build it — `message` = the text FastEndpoints writes today ("One or more errors occurred!"; the middleware's own `ValidationException` branch keeps its different text, "One or more validation errors occurred." — each path keeps the text it writes today), `traceId` from the same source the middleware uses, one `errors` item per failed rule, `field` = the key FastEndpoints' default body uses today for that failure (camelCase property name, the property path for nested members). The status stays the one FastEndpoints sends (400).
- `ErrorCodes`: `DuplicateName = "duplicate_name"`, `LimitReached = "limit_reached"`.
- `Guard` and the `*OrThrow` lookups in `Blocks.Exceptions/Extensions.cs` are the adopted helpers; `Guard.NotFound<T>` keeps its "{Type} not found" text, which is the 404 body message from here on.
- New handler-side texts (today's 403/404/401 sends carry no body, so these are body additions, not changed messages): 404 "{Type} not found"; 403 "You do not own this team.", "You are not the author of this card.", "Only a signed-in user can create action items while discussing." (`CreateCard`), "Only a signed-in user can edit action items while discussing." (`EditCard`) and "Only a signed-in user can delete action items while discussing." (`DeleteCard`); 401 "Not authorized for this sprint.", "Missing participant claims." and "Missing user claims." (`MeEndpoint`, Step 2). They are used from Step 2 on. Any other bodyless send a step converts takes the nearest of these texts and is named in its step block.
- `TeamEndpointTests.CreateTeam_EmptyName_ReturnsValidationError` gains the body assertion: status 400, `errors[0].field == "name"`, `errors[0].message` non-empty, `message`, `statusCode`, `traceId` present (plan P2).
- Policy-level 401/403 written by `OnRedirectToLogin` / `OnRedirectToAccessDenied` stay bodyless; no edit there.

Accept:
- `grep -n "ForbiddenException\|ConflictException" src/BuildingBlocks/Blocks.Exceptions/*.cs` lists both classes.
- `grep -n "ResponseBuilder" src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` gives 1 hit.
- `grep -rn "propertyName\|errorMessage\|PropertyName =\|ErrorMessage =" src/Services/Reflekt/Reflekt.API/Middleware` gives no hit on the written body (the record members are `Field` / `Message`).
- *Code trace*: a `ConflictException` with a code reaches the body's `code`; a `BadRequestException` has no `code` and no `errors` member.
- The updated `TeamEndpointTests` assertion passes in the integration run; any other existing assertion on the validation body shape is updated and named in the step block.

Runs: `TeamEndpointTests.cs`, `SprintEndpointTests.cs`.

Satisfies: spec BE-05/SK-02/SK-06/BE-09 (mechanism), AC7 (validation 400). Risk: risky — every error response passes through it.

### Step 2 — Claim names, typed claim accessors, merged policies, `Features/Account`
Follow authorization-patterns. Files:
- `src/Services/Reflekt/Reflekt.API/Auth/GuestClaims.cs` (new)
- `src/Services/Reflekt/Reflekt.API/Auth/ClaimsPrincipalExtensions.cs`
- `src/Services/Reflekt/Reflekt.API/Auth/GuestTokenAuthenticationHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Auth/Policies.cs`
- `src/Services/Reflekt/Reflekt.API/Auth/AuthorizationConfiguration.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Auth/**` → `Features/Account/**` (Login, Logout, Me; folder and namespace)
- `src/Services/Reflekt/Reflekt.API/GlobalUsings.cs`
- the 39 files with the `AuthPolicies` alias, and every file reading `"participantId"`, `"sprintId"` or `"displayName"` as a raw string (`Hubs/SprintHub.cs` and the endpoints)
- the six `*EndpointTests.cs` files whose policy overrides name the renamed constants (setup only), including the inline host in `SprintEndpointTests.GetTeamSprints_EmptyTeam_ReturnsEmptyArray`, which overrides `Policies.ScrumMaster` itself

What to build:
- `GuestClaims`: `ParticipantId`, `SprintId`, `DisplayName` constants with today's claim names. `ClaimsPrincipalExtensions` gains `GetParticipantId()` and `GetSprintId()` returning `int?`, parsing exactly as the endpoints parse today; each caller keeps its own reaction to `null` (today's 401).
- Policies: `ScrumMaster` and `GoogleUser` are identical cookie policies and merge into `SignedInUser`; `Participant` becomes `Guest`; `OwnerOrScrumMaster` (cookie or guest token, no ownership check) becomes `SignedInUserOrGuest`. Policy definitions keep their schemes and requirements exactly.
- `Features/Auth` becomes `Features/Account` (namespace too; routes unchanged), so `Policies` resolves without the `AuthPolicies` alias; the alias goes from every file.
- `MeEndpoint`'s `SendUnauthorizedAsync` (missing email or name claim) becomes `throw new UnauthorizedException("Missing user claims.")`; same status, the body is the one-body addition (Step 1).
- The integration tests' policy overrides are renamed to the new constants; no assertion changes (Decisions row 2).

Accept:
- `grep -rn "AuthPolicies" src tests` → no hits.
- `grep -rn "Features.Auth\b" src tests` → no hits; `Features/Account/` holds the three endpoints; `grep -rn "Routes\|Get(\"\|Post(\"" src/Services/Reflekt/Reflekt.API/Features/Account` shows the same route strings as before (pasted).
- `grep -rnE "\"(participantId|sprintId|displayName)\"" src/Services/Reflekt/Reflekt.API` → only `GuestClaims.cs` (and the token issuer if it builds claims from the constants, which it must).
- `grep -rn "ScrumMaster\b\|GoogleUser\|OwnerOrScrumMaster\|\"Participant\"" src/Services/Reflekt/Reflekt.API/Auth/Policies.cs` → no hits.
- `grep -rnE "Send(NotFound|Forbidden|Unauthorized|Errors)Async|ThrowError\(|AddError\(" src/Services/Reflekt/Reflekt.API/Features/Account` → no hits.

Runs: all six `*EndpointTests.cs`, `AuthSmokeTests.cs`.

Satisfies: spec BE-04/SK-09 (claims), SK-19, BE-25 part (namespace), BE-05 (`MeEndpoint`), AC1 part, AC5. Risk: risky — every endpoint's policy attribute changes name.

### Step 3 — `SprintAccess` and one phase-action guard
Follow extract-feature-service (and domain-patterns for the policy). Files:
- `src/Services/Reflekt/Reflekt.API/Auth/SprintAccess.cs` (new; the service and its result type)
- `src/Services/Reflekt/Reflekt.Domain/Sprints/PhaseActionPolicy.cs` (new)
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/PhaseActionRepository.cs` (new)
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs`
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/PhaseActionPolicyProvider.cs` (new; replaces `PhaseActionCache.cs`, deleted)
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/GetPhaseActions/GetPhaseActionsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs`
- the 16 endpoints that call `phaseActionCache.IsAllowed(` today (8 in `CardGroups`, 3 in `Cards`, 2 in `EvidenceLinks`, 2 in `Votes`, `AdvancePhase`). The hub makes no phase-action check (its timed-phase check is Step 10's `Sprint.HasTimer`), and `OpenGraph/OpenGraphAddressPolicy.IsAllowed` is unrelated and untouched

What to build:
- `SprintAccess` (scoped, concrete, no interface) loads what an endpoint addresses and returns a result; it never throws while building it and never decides an order. Entry points: by sprint id; by team id (and sprint id) for the team-first endpoints. The result carries the sprint (or null), its team (or null), the caller's user id from the cookie (read without throwing), participant id and `sprintId` claim, `IsGuest` (participant claim present), and `IsOwnedBy(int? userId)` plus `IsOwner` — the one place an `OwnerId` comparison lives. It offers throwing checks the endpoint calls in its own order: sprint found (404, `Guard` text), team found (404), owner (403 "You do not own this team."), guest of this sprint (401 "Not authorized for this sprint."). Entities are loaded tracked, as the endpoints load them today.
- Adoption is per area in Steps 5–10; this step only builds it and registers it.
- `PhaseActionRepository.GetAllAsync` reads the matrix `AsNoTracking`. `PhaseActionPolicyProvider` is a singleton with one awaited accessor, `Task<PhaseActionPolicy> GetAsync()`: the first call loads the rows through `IServiceScopeFactory` and `PhaseActionRepository.GetAllAsync(CancellationToken.None)` (so a cancelled request never leaves a failed load behind); concurrent first callers share that one load; a successful load is kept for the process and never reloaded — today's cache also loads once; a failed load is not kept, so the next call tries again, as a failed singleton constructor is retried on the next resolution today. No blocking wait anywhere (Step 11's `GetAwaiter().GetResult()` grep covers it). No `ReflektDbContext` in the API.
- Every `phaseActionCache.IsAllowed(` site in the 16 endpoints becomes `(await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(phase, action)` at the same place in the check order; the status (409) and the text stay. The hub is not touched by this step.
- `GetPhaseActionsEndpoint` returns the same response from the policy.

Accept:
- `test ! -e src/Services/Reflekt/Reflekt.API/Features/PhaseActions/PhaseActionCache.cs`.
- `grep -rn "ReflektDbContext" src/Services/Reflekt/Reflekt.API` → no hits.
- `grep -rn "is not allowed in phase" src/Services/Reflekt` → only `PhaseActionPolicy.cs`.
- `grep -rn "PhaseActionCache\|phaseActionCache" src` → no hits; `grep -rnE "GetAwaiter|\.Result\b|\.Wait\(" src/Services/Reflekt/Reflekt.API/Features/PhaseActions` → no hits.
- `grep -rn "OwnerId" src/Services/Reflekt/Reflekt.API/Auth/SprintAccess.cs` → the one comparison.
- *Code trace*: `GetPhaseActions` response built from the same rows in the same order as before.

Runs: `CardEndpointTests.cs`, `SprintEndpointTests.cs`, `EvidenceLinkEndpointTests.cs`.

Satisfies: spec BE-04/SK-08/SK-21 (service), BE-06/BE-38 (phase-action policy), BE-11 part. Risk: risky — the phase guard sits on 16 write paths.

### Step 4 — Event publication logs every handler failure; handlers read through a scope
Follow persistence-patterns (and create-domain-event-handler for the handlers). Files:
- `src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/BroadcastGroupCreatedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/SetReviewStatus/BroadcastReviewStatusChangedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/BroadcastVoteCountOnVoteCastHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/BroadcastVoteCountOnVoteRetractedHandler.cs`

What to build:
- The interceptor keeps F35's collected/held lists and its publish points (after a save with no explicit transaction; on commit). What changes is how a batch is published: the events of one batch are published in today's order, inside one task the request never awaits; each event is published with `Mode.WaitForAll` inside that task so its handlers' failures are observed; every failed handler is logged (`ILogger<DomainEventPublishingInterceptor>`, error level, event type and exception — one entry per failure, inner exceptions of a combined failure included); nothing is rethrown. **The detached task passes `CancellationToken.None` to the handlers**, never the save's token: that token is the endpoint's `ct`, derived from the request's `RequestAborted`, and the task outlives the request, so a moved broadcast must not hang on what happens to the request token after the response (Decisions row "Detached publication token"). The commit and save callbacks still receive and honour their own token for everything they do before the task starts. See plan P5, the publisher `Departs from` row and the handler-scope `Departs from` row.
- FastEndpoints event handlers are singletons ("DO NOT maintain state in them", `FastEndpoints.xml`, 5.34.0). The four handlers that take scoped repositories in their constructor read through `IServiceScopeFactory` per call instead, as F35's `CardBroadcast` callers do. Their payloads do not change in this step.

Accept:
- `grep -n "Mode.WaitForNone" src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs` → no hits; `grep -n "LogError" …` → at least 1.
- `grep -rln "CardRepository\|VoteRepository\|SprintRepository\|CardGroupRepository" src/Services/Reflekt/Reflekt.API/Features --include=*Handler.cs | xargs grep -L "IServiceScopeFactory"` → no hits.
- *Code trace*: (a) a handler that throws is logged and the request's response is unaffected (the request never awaits the task); (b) two events of one save are published in the order of the collected list; (c) a rolled-back transaction still publishes nothing (F35 path unchanged); (d) the token every handler receives is `CancellationToken.None` (the line that passes it).
- The step block records, from the FastEndpoints 5.34.0 package (XML docs or decompiled source), what `PublishAsync(Mode.WaitForAll, …)` returns when one or several handlers throw (P5 rests on it).

Runs: `VoteEndpointTests.cs`, `CardEndpointTests.cs`, `SprintEndpointTests.cs`.

Satisfies: spec BE-16 (logging, fire-and-forget), BE-03 (prerequisite), AC8 (logged failure, by trace). Risk: risky — every write path publishes through it.

### Step 5 — Teams and Sprints: exceptions, access, rules in `Sprint`, team limit
Follow domain-patterns. Files:
- `src/Services/Reflekt/Reflekt.API/Features/Teams/**` (CreateTeam, DeleteTeam, GetMyTeams)
- `src/Services/Reflekt/Reflekt.API/Features/Teams/CreateTeam/TeamLimitCheck.cs` (new)
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/**` except `Timer/` (AdvancePhase, AdvancePhaseCheck, CloseSprint, CreateSprint, GetSprint, GetSprintSummary, GetTeamSprints, StartSprint, UpdateColumns)
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Behaviors/CardGroup.cs` (`CreateSolo` only)
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/SprintRepository.cs`, `TeamRepository.cs`
- `tests/Reflekt.IntegrationTests/SprintEndpointTests.cs`

What to build:
- Every `Send*Async` status send, `ThrowError` and `AddError` in these endpoints becomes a throw from the exception family, a `SprintAccess` check, or a domain guard, in today's order (Access order reference, team-first and sprint-first rows).
- State guards: where an endpoint checks state before calling `Sprint`, it calls a `Sprint` guard that carries the endpoint's text (`EnsureActive("Sprint must be Active to advance phase.")` and the other variants listed in the census) **at the endpoint check's current position**. An endpoint state check goes only where the domain method throws the same text **and** no other check sits between the two today; otherwise it stays as a guard call, so a multi-fault input keeps today's message. `AdvancePhase` is such a case: its Active check runs before the phase-action check today, so `EnsureActive("Sprint must be Active to advance phase.")` stays ahead of the phase-action check (a closed sprint in Reporting keeps answering "Sprint must be Active to advance phase."). `Start`, `Close` and `AdvancePhase` keep F35's texts. Neither the endpoint texts nor the domain texts change.
- `AdvancePhase`: the discussed-count rule moves to `Sprint.EnsureEnoughGroupsDiscussed(int)` called before `AdvancePhase()` when leaving Discuss. **The count is taken in memory from the loaded, tracked groups after the focused group is marked discussed** (`MarkDiscussed()` then `ClearFocus()`, as today), not by a database count: the focus mark is not saved yet, so a database count would miss it and refuse a valid advance. Step 12's count queries do not apply here. Leaving Writing creates solo groups with `CardGroup.CreateSolo` (no event) instead of `Create` + `ClearDomainEvents()`. `AdvancePhaseCheck` and `AdvancePhase` `HandleAsync` (BE-18) are split into small private methods (≤ ~60 lines each) with the same answers.
- `CreateTeam`: `TeamLimitCheck` (focused operation service) throws `ConflictException(…, ErrorCodes.LimitReached)` with today's message text minus the `limit_reached: ` prefix; the duplicate-name check throws `ConflictException(…, ErrorCodes.DuplicateName)` likewise, through a repository exists query. Check order as today. No `AddError` prefixes remain.
- `DeleteTeam`: `SprintRepository.HasActiveSprintAsync(teamId)` replaces the `Query().AnyAsync` read; the 409 text stays.
- `GetSprintSummary` (anonymous): exceptions only; no `SprintAccess`.
- `SprintEndpointTests.CreateSprint_WrongTeam_Returns404` gains the one-body assertion (404, `message`, `statusCode`, `traceId`, no `code`); `SprintEndpointTests.StartSprint_FromActive_Returns409` gains it for 409 with `message` = "Sprint must be in Draft state to start." (plan P2).

Accept:
- `grep -rnE "Send(NotFound|Forbidden|Unauthorized|Errors)Async|ThrowError\(|AddError\(" src/Services/Reflekt/Reflekt.API/Features/Teams src/Services/Reflekt/Reflekt.API/Features/Sprints` → no hits.
- `grep -rn "limit_reached:\|duplicate_name:" src` → no hits; `grep -rn "ErrorCodes\." src/Services/Reflekt/Reflekt.API/Features/Teams` → 2 sites.
- `grep -rn "ClearDomainEvents()" src/Services/Reflekt/Reflekt.API/Features/Sprints` → no hits.
- *Code trace*: `AdvancePhase` out of Writing raises no `CardGroupCreatedEvent` (only `CreateSolo`); out of Discuss with fewer than 2 discussed groups returns 409 with today's text; the discussed count is read from the in-memory groups after the focus mark (one saved discussed group plus an undiscussed focused group still advances); the Active check precedes the phase-action check.
- The two updated `SprintEndpointTests` pass; the existing `SprintTests` unit tests pass unchanged.

Runs: `TeamEndpointTests.cs`, `SprintEndpointTests.cs`.

Satisfies: spec BE-05/SK-02, BE-04 (adoption), BE-06/SK-12 (Sprint rules, team limit), BE-11 (`HasActiveSprintAsync`), BE-35 (`AdvancePhaseCheck`), AC1 part, AC3 part, AC7 (404, 409). Risk: risky — phase advance and team creation paths.

### Step 6 — CardGroups: group rules in `CardGroup`, broadcasts through events, one regroup operation
Follow create-domain-event-handler (and domain-patterns for the rules). Files:
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/**` (AddCardToGroup, ClearFocus, CreateGroup, DissolveGroup, FocusGroup, GetGroups, RemoveCardFromGroup, ToggleDiscussed, UpdateGroupTitle, `CardGroupContracts.cs`)
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/BroadcastGroupDissolvedHandler.cs` (new)
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CardRegrouping.cs` (new)
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/FocusHandover.cs` (new)
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CardGroupResponseBuilder.cs` (new)
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Behaviors/CardGroup.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/GroupDissolvedEvent.cs` (new)
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` (solo groups via `CreateSolo`)

What to build:
- Group-membership rules become `CardGroup` behaviour methods throwing `ArgumentException` without `paramName`, so the middleware's 400 body carries exactly today's text: "At least 2 cards are required to create a group.", "Only Note cards can be grouped.", "All cards must be in the same column.", "Card must be in the same column as the group.", "Cannot dissolve a solo group.". "One or more cards not found in this sprint." stays a lookup failure (400, same text) in the regroup operation.
- `CardRegrouping` (focused operation service) moves cards between groups and removes emptied source groups for the grouping endpoints. Its emptiness check counts the cards that stay in a source group with the moved cards accounted for (today's code relies on tracked, unsaved `SetGroupId` changes). Each endpoint keeps its saves and its transaction exactly as today (`CreateGroup` keeps its explicit transaction; no transaction is added or removed).
- `CreateGroup`: drop `ClearDomainEvents()` and the inline `GroupCreated` send. `BroadcastGroupCreatedHandler` then sends the message; its payload must equal the inline one field for field (`group { id, sprintId, columnType, title, cardIds }`), with `cardIds` in the repository's order — `CardRepository.GetBySprintIdAsync` orders by `CreatedAt` descending, which is today's inline order — so the handler's `.OrderBy(c => c.CreatedAt)` goes. The event is published on the transaction's commit (F35).
- `DissolveGroup`: solo groups via `CreateSolo`; `CardGroup.Dissolve()` raises `GroupDissolvedEvent(this)`; the inline send goes; `BroadcastGroupDissolvedHandler` sends `"GroupDissolved"` with `{ groupId }` to `sprint:{sprintId}` — today's name, payload and recipients. Confirm by code trace that the interceptor collects events from an entity removed in the same save (its entries are still tracked as `Deleted` while saving); if it does not, raise the event on an entity that is saved instead and record why.
- `RemoveCardFromGroup`: the solo group via `CreateSolo`; no `ClearDomainEvents()`.
- `FocusHandover` (focused operation service) holds the shared focus logic of `FocusGroup` and `ClearFocus`, both marking the previous group discussed unconditionally as today; `AdvancePhase` keeps its own conditional mark (Step 5).
- `CardGroupResponseBuilder` builds `CardGroupResponse` (cards and `IsFocused`) for every endpoint that returns it today, with the same values.
- `CreateGroup`'s `HandleAsync` (BE-18, 86 lines today) ends within ~60 lines once the rules, the regroup and the response builder move out.
- Every endpoint here uses `SprintAccess`, the exception family and the phase guard (sprint-first order), and no longer injects `IHubContext<SprintHub>`.
- `DbInitializer`'s seeded solo groups use `CreateSolo`; its `ClearDomainEvents()` on that group goes.

Accept:
- `grep -rn "IHubContext<SprintHub>\|ClearDomainEvents()" src/Services/Reflekt/Reflekt.API/Features/CardGroups | grep -v "Handler.cs"` → no hits.
- `grep -rnE "Send(NotFound|Forbidden|Unauthorized|Errors)Async|ThrowError\(|AddError\(" src/Services/Reflekt/Reflekt.API/Features/CardGroups` → no hits.
- `grep -rn "CardGroup.Create(" src` → only `CreateGroupEndpoint` (or the regroup operation it calls); `grep -rn "CreateSolo(" src` → Dissolve, RemoveCardFromGroup, AdvancePhase, DbInitializer.
- `grep -n "OrderBy" src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/BroadcastGroupCreatedHandler.cs` → no hits.
- *Code trace*, per moved broadcast: the inline payload before (file:line at the base commit) and the handler payload after, side by side, for `GroupCreated` and `GroupDissolved`; no `GroupCreated` is raised by Dissolve, RemoveCardFromGroup or leaving Writing.

Runs: `CardEndpointTests.cs`, `SprintEndpointTests.cs`.

Satisfies: spec BE-03/SK-13 (CardGroups), BE-06/SK-12 (group rules), BE-08/SK-15 (regroup, response builder), AC1 part, AC2 part, AC8 (payload traces). Risk: risky — five write paths and two broadcasts move.

### Step 7 — Votes: `VoteBudget`, broadcasts through handlers, `MarkRetracted()`
Follow extract-feature-service. Files:
- `src/Services/Reflekt/Reflekt.API/Features/Votes/**` (CastVote, RetractVote, GetMyVotes, `VoteContracts.cs`)
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/VoteBudget.cs` (new)
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/VoteRepository.cs`

What to build:
- `VoteBudget` (focused operation service) answers "may this participant cast another vote in this sprint" and throws `DomainException("Vote budget exhausted.")` (409) as today. "Already voted on this group." and "No votes to retract on this group." keep their status and text where they are checked today.
- `CastVote`: keeps its serializable transaction; drops `vote.ClearDomainEvents()` and the inline `VoteCast` send; `BroadcastVoteCountOnVoteCastHandler` sends `"VoteCast"` `{ groupId, newCount }` after commit.
- `RetractVote`: calls `vote.MarkRetracted()` before removing it; drops the inline `VoteRetracted` send; `BroadcastVoteCountOnVoteRetractedHandler` sends `"VoteRetracted"` `{ groupId, newCount }`.
- `newCount` in both handlers is the group's vote count read after commit through a scope, the same query the endpoints use today.
- Each endpoint keeps its own order from the Access order reference: `CastVote` group-first; `RetractVote` guest then sprint-first, with **no group lookup** — adding one would add a new 404.

Accept:
- `grep -rn "IHubContext<SprintHub>\|ClearDomainEvents()" src/Services/Reflekt/Reflekt.API/Features/Votes | grep -v "Handler.cs"` → no hits.
- `grep -n "MarkRetracted()" src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/*.cs` → 1 hit, before the remove.
- *Code trace*: inline payload before vs handler payload after for both messages (names, members, recipients); `VoteCast` is published only on the serializable transaction's commit.

Runs: `VoteEndpointTests.cs`.

Satisfies: spec BE-03/SK-13 (votes), BE-06 (vote budget), AC1 part, AC2 part, AC8 (payload traces). Risk: risky — concurrency path with a serializable transaction.

### Step 8 — Cards, EvidenceLinks, Participants, PreviousActions
Follow error-handling. Files:
- `src/Services/Reflekt/Reflekt.API/Features/Cards/**` (`CardBroadcast.cs` included; `WritingPhaseRedaction.cs` unchanged)
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/**`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/**`
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/**`

What to build:
- Exceptions, `SprintAccess` and the typed claims in every endpoint here, card-first order for card endpoints (`Attach`/`Detach` keep the phase 409 before the 403), the guest 401 texts, `KickParticipant`'s `State != Active` 409 before the owner 403. The card-author check stays in the card endpoints and throws `ForbiddenException("You are not the author of this card.")`.
- `CardBroadcast` takes `IHubClients` (the caller passes `hubContext.Clients`) instead of `IHubContext<SprintHub>`; its recipients and payloads do not change. Its EF read moves in Step 11.
- `JoinSprint` (anonymous) does not use `SprintAccess`; exceptions only.

Accept:
- `grep -rnE "Send(NotFound|Forbidden|Unauthorized|Errors)Async|ThrowError\(|AddError\(" src/Services/Reflekt/Reflekt.API/Features/{Cards,EvidenceLinks,Participants,PreviousActions}` → no hits.
- `grep -rn "IHubContext<SprintHub>" src/Services/Reflekt/Reflekt.API/Features/Cards/CardBroadcast.cs` → no hits.
- *Code trace*: F35's redaction path for each card broadcast unchanged (recipient groups and payloads).

Runs: `CardEndpointTests.cs`, `EvidenceLinkEndpointTests.cs`, `ParticipantEndpointTests.cs`, `SprintEndpointTests.cs`.

Satisfies: spec BE-05/SK-02, BE-04/SK-08 (adoption), AC1 part, AC2 part, AC3 part. Risk: risky — author and guest branches.

### Step 9 — Reports: one `SprintReportBuilder`
Follow extract-feature-service. Files:
- `src/Services/Reflekt/Reflekt.API/Features/Reports/**`
- `src/Services/Reflekt/Reflekt.API/Features/Reports/SprintReportBuilder.cs` (new)
- `src/Services/Reflekt/Reflekt.API/Mappings/ReportMappings.cs` (if the builder moves mapping calls)

What to build:
- `SprintReportBuilder` builds what `GetSprintReport` and `GetParticipantReport` share, keeping F35's Writing-phase redaction exactly where it applies today. Both endpoints use exceptions and `SprintAccess` in today's order. Both `HandleAsync` methods (BE-18, 73 lines each today) and the builder's methods end within ~60 lines.

Accept:
- The two endpoint files no longer hold the duplicated building code (step block shows the before/after line counts).
- `grep -rn "WritingPhaseRedaction" src/Services/Reflekt/Reflekt.API/Features/Reports` → the same redaction call sites as the base commit, now in the builder.
- `grep -rnE "Send(NotFound|Forbidden|Unauthorized|Errors)Async|ThrowError\(|AddError\(" src/Services/Reflekt/Reflekt.API/Features/Reports` → no hits.

Runs: `SprintEndpointTests.cs`, `CardEndpointTests.cs`.

Satisfies: spec BE-08/SK-15 (reports), AC1 part. Risk: low.

### Step 10 — Hub, readiness and timer
Follow create-domain-event-handler (handler rename) and service-registration (lifetimes). Files:
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`
- `src/Services/Reflekt/Reflekt.API/Hubs/ReadinessTracker.cs` (new)
- `src/Services/Reflekt/Reflekt.API/Hubs/TimerService.cs` (moved from `Features/Sprints/Timer/TimerService.cs`)
- `src/Services/Reflekt/Reflekt.API/Hubs/TimerSnapshot.cs` (the snapshot type with `ToPayload()`, wherever it lives today, moved or extended)
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/BroadcastPhaseChangedHandler.cs` → `BroadcastAndRestartTimerOnPhaseAdvancedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/KickParticipant/BroadcastParticipantKickedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs`
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` (`HasTimer`)
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` (`IsActiveAsync`)

What to build:
- `ReadinessTracker` (singleton) owns what the hub keeps in static fields today: readiness, connection state and F35's connection registry; the hub, the kick handler and the phase handler use it. Same data, same lifetime (process).
- The hub takes its repositories, `SprintAccess`, `ReadinessTracker` and `TimerService` by constructor injection (it makes no phase-action check, so it takes no policy provider). Its owner check uses `SprintAccess` (`IsOwnedBy` with the resolved user) and translates every failure into `HubException` with today's texts, in `RequireScrumMasterAsync`'s order. `GetParticipantCount` (a static method, not a hub method; no callers) is deleted.
- **F35's revocation re-check must keep reading fresh.** Today `JoinSprint` checks the participant before registering the connection and again after it (`IsParticipantRemovedAsync`, "A kick committed before this read is undone here; one committed after it finds this connection in the registry"), each read in its own scope, so the second read sees a kick committed in between. With constructor-injected repositories both reads share one `DbContext`, and `GetByIdAsync` (`FindAsync`) would return the entity tracked by the first read without querying. So both reads go through a new `ParticipantRepository.IsActiveAsync(int participantId, CancellationToken)` that always queries the database `AsNoTracking` (an exists query: the participant exists and `RemovedAt` is null); "removed" stays "missing or removed", as today. The order stays: check → register → join groups → re-check → on removed, leave the groups, unregister and throw `HubException("Participant has been removed.")`. No other hub read may serve this check from the change tracker.
- `TimerSnapshot.ToPayload()` builds `{ status, remainingSeconds, endTimeUtc }` and `Stopped` / `Expired` snapshots replace the `"stopped"` / `"expired"` literals; all six payload sites use it with identical output. `Sprint.HasTimer` replaces the three timed-phase checks.
- `TimerService` moves to `Hubs/` (it is hub infrastructure), injects `IHubContext<SprintHub>`, broadcasts expiry itself (same message `"TimerUpdated"`, same group, same payload) and logs a failed expiry broadcast instead of discarding its task. `RegisterExpiryCallback` and the callback in `UseReflektApi` go.
- Rename `BroadcastPhaseChangedHandler` to `BroadcastAndRestartTimerOnPhaseAdvancedHandler` (SK-27); it uses `ReadinessTracker` instead of `SprintHub.ClearReadiness` and the snapshot payload. Behaviour unchanged.

Accept:
- `grep -n "static " src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` → only `const` lines and the pure helper `ParticipantGroup(int)`, which stays; no static field.
- `grep -rn "RegisterExpiryCallback\|GetParticipantCount\|SprintHub.ClearReadiness" src` → no hits.
- `grep -rn "\"expired\"\|\"stopped\"" src/Services/Reflekt/Reflekt.API` → only `TimerSnapshot.cs`.
- `grep -rn "IHubContext<SprintHub>" src/Services/Reflekt/Reflekt.API/Features | grep -v "Handler.cs"` → no hits (spec AC2 first half; `TimerService` now sits in `Hubs/`).
- *Code trace*: every `HubException` text in the hub, before and after, pasted side by side; the six timer payloads produce the same members.
- *Code trace*: `JoinSprint`'s order "check, register, join groups, re-check, undo on removed", with both checks calling `ParticipantRepository.IsActiveAsync` and that method's query (`AsNoTracking`, no `FindAsync`); `grep -n "GetByIdAsync" src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` shows no participant read used for the removed-check.

Runs: `SprintEndpointTests.cs`, `ParticipantEndpointTests.cs`.

Satisfies: spec BE-16/SK-03 (tracker, snapshot, `HasTimer`), BE-11 (hub repositories injected), BE-33 (injected hub context), BE-25/SK-27 (phase handler), BE-10 part, AC2, spec "What must not change" (F35's soft kick). Risk: risky — the hub's join and kick paths share the moved state, and the revocation re-check is a security property no test pins.

### Step 11 — No EF in the API layer; one async migrate/seed method
Follow persistence-patterns. Files:
- `src/BuildingBlocks/Blocks.Persistence/Repositories/RepositoryBase.cs` (`Query()` protected)
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/SprintRepository.cs`
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs`
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Reflekt.Persistence.csproj`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardBroadcast.cs`
- `src/Services/Reflekt/Reflekt.API/Program.cs`
- every other API file with an EF using

What to build:
- `CardBroadcast`'s read becomes a `SprintRepository` method returning the same data `AsNoTracking`; `RepositoryBase.Query()` becomes protected.
- `UseReflektPersistence(WebApplication)` (blocking `GetAwaiter().GetResult()`) becomes one async Persistence method, `MigrateAndSeedAsync(IServiceProvider, CancellationToken)`, that `Program` awaits before the app runs; same order (migrate, then seed when enabled), same options. `Reflekt.Persistence` drops its `Microsoft.AspNetCore.App` framework reference if nothing else needs it.
- `DbInitializer`'s seed is split into methods of ≤ ~60 lines with the same data and order.

Accept:
- `grep -rlE "Microsoft.EntityFrameworkCore|\.Query\(\)|IQueryable" src/Services/Reflekt/Reflekt.API --include=*.cs` → no hits (spec AC4).
- `grep -rn "GetAwaiter().GetResult()" src/Services/Reflekt` → no hits.
- The integration run starts clean (start-up migrate and seed work).

Runs: all integration files.

Satisfies: spec BE-11/SK-16, BE-32, AC4. Risk: risky — start-up path.

### Step 12 — Reads: no-tracking reads, count queries, read-only collections, one `*Response` suffix
Follow persistence-patterns. Files:
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/*.cs`
- `src/BuildingBlocks/Blocks.Persistence/Repositories/RepositoryBase.cs` (`Remove`)
- the read endpoints and their contracts under `src/Services/Reflekt/Reflekt.API/Features/**`
- the aggregates' public collection members under `src/Services/Reflekt/Reflekt.Domain/**`

What to build:
- New `AsNoTracking` read methods for the read-only endpoints (lists, reports, summaries, `GetMyVotes`, `GetPreviousActions`); every caller that changes the loaded entities keeps a tracked load (`GetBySprintIdAsync` stays tracked for `CreateGroup`, `DissolveGroup`, `AdvancePhase`).
- Count/exists queries replace loading a list only to count or test it; `CardRegrouping`'s emptiness check (Step 6) becomes a count that excludes the moved cards.
- `Remove` lives once in `RepositoryBase`; the copies in the card, group and vote repositories go. `CardGroupRepository.GetByIdsAsync` (no callers) goes.
- `IReadOnlyList<T>` (BE-35) in three places, JSON output identical: (a) repository methods returning a materialized list (`Task<List<T>>` → `Task<IReadOnlyList<T>>`); (b) `List<T>` properties of response and contract records; (c) endpoint response types `Endpoint<…, List<…>>` → `Endpoint<…, IReadOnlyList<…>>`. Aggregate collections already expose `IReadOnlyList<T>` (`Card.EvidenceLinks`) and stay; a public aggregate member still typed `List<T>` changes the same way, EF backing as configured. A caller that adds to a returned list keeps its own copy (`.ToList()`), named in the step block.
- `SprintAccess` keeps its tracked loads (Step 3) on read endpoints too: it loads at most one sprint and one team per request, so no read-only variant is added for it.
- Repository read DTOs are returned directly: `TeamSummaryDto` and the API's `TeamSummary` twin become one `TeamSummaryResponse`; the two `VoteDistributionEntry` records become one `VoteDistributionResponse`; `Report*Entry` / `*Info` and similar names take the `*Response` suffix. Members and JSON names do not change.
- Method size (BE-18): the methods BE-18 names are split in their own steps (`AdvancePhaseCheck` and `AdvancePhase` in Step 5, `CreateGroup` in Step 6, the two report endpoints in Step 9, `DbInitializer` in Step 11). This step only checks them; no other method is in scope for size.

Accept:
- The line count of each BE-18 method after Steps 5, 6, 9 and 11 (`HandleAsync` of `AdvancePhaseCheckEndpoint`, `AdvancePhaseEndpoint`, `CreateGroupEndpoint`, `GetSprintReportEndpoint`, `GetParticipantReportEndpoint`; every method of `DbInitializer`), pasted: each ≤ ~60 lines.
- `grep -rn "Task<List<" src/Services/Reflekt/Reflekt.Persistence/Repositories` → no hits; `grep -rnE "Endpoint<[^>]*, *List<" src/Services/Reflekt/Reflekt.API/Features` → no hits; `grep -rnE "\bList<" src/Services/Reflekt/Reflekt.API/Features --include=*Contracts.cs --include=*Response.cs` → no hits (`IReadOnlyList<` does not match `\bList<`); record members still declared inside endpoint files (they move in Step 14) are listed in the step block with their new type.
- `grep -rn "record TeamSummary\b\|TeamSummaryDto\|record VoteDistributionEntry" src` → no hits.
- `grep -rn "public void Remove" src/Services/Reflekt/Reflekt.Persistence/Repositories` → no hits.
- `grep -rn "AsNoTracking" src/Services/Reflekt/Reflekt.Persistence/Repositories | wc -l` → greater than at the base commit (0); the step block lists each new method and its callers.
- *Code trace*: each read endpoint's response members unchanged (a list of the renamed types with their members).

Runs: all integration files.

Satisfies: spec BE-17, BE-18, BE-35, BE-27/SK-22 (twins and suffix). Risk: risky — tracked vs untracked loads on write paths.

### Step 13 — Limits as constants on the domain types
Follow domain-patterns. Files:
- `src/Services/Reflekt/Reflekt.Domain/**` (`Card`, `CardGroup`, `Sprint`, `Team`, `Participant`, `User`, `EvidenceLink`, `ColumnColor`)
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/*.cs`
- the validators under `src/Services/Reflekt/Reflekt.API/Features/**`
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphBackgroundService.cs`

What to build:
- One constant per limit on the type it belongs to, read by the domain guard, the validator and the EF configuration that use it today: text 2000, assignee 50, display name 50, group title 100, sprint and team name 100, column name 30, colour length 10, URL 2048, OpenGraph title 500 and description 1000 (`OpenGraphBackgroundService.cs:30-31`), external key 50, user email and name 256, given name 100, votes per participant minimum 1, the three timers 1–60 (each timer its own constant, BE-30). Values unchanged.

Accept:
- `grep -rnE "HasMaxLength\([0-9]+\)|MaximumLength\([0-9]+\)|Length\([0-9]+, *[0-9]+\)" src/Services/Reflekt` → no literal numbers.
- `grep -nE "\b(500|1000)\b" src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphBackgroundService.cs` → no hits.
- The integration run starts clean (no model change).

Runs: all integration files.

Satisfies: spec BE-19 (rest)/SK-11 (rest), BE-31, SK-20, BE-23 (truncation), BE-30 part. Risk: low.

### Step 14 — Endpoint type layout
Follow extract-endpoint-types. Files:
- the 28 endpoint files under `src/Services/Reflekt/Reflekt.API/Features/**` that hold a request, response, command, query, DTO or validator type
- their sibling `*Command.cs` (write slices) and `*Query.cs` (read slices), new or existing
- `src/Services/Reflekt/Reflekt.API/Features/PhaseActions/GetPhaseActions/PhaseActionsResponse.cs` → into the slice's `GetPhaseActionsQuery.cs`

What to build:
- Each endpoint file holds the endpoint class only. A type used by one slice moves to that slice's sibling file; a type shared by several slices stays in the feature's `*Contracts.cs` (Decisions row on shared contracts). Type names and namespaces are kept, so no consumer changes beyond a using where a namespace really moves.

Accept:
- `find src/Services/Reflekt/Reflekt.API/Features -name "*Endpoint.cs" -exec grep -lE "(record|class) [A-Za-z]+(Request|Response|Command|Query|Validator|Dto|Summary|Entry)\b" {} +` → no hits (spec AC6).

Runs: all integration files.

Satisfies: spec BE-26/SK-10, AC6. Risk: low — moves only.

### Step 15 — Options, dead code, domain polish, naming
Follow service-registration (and domain-patterns for `Entity` and `ColumnColor`). Files:
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs`, `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs`, `src/Services/Reflekt/Reflekt.API/SeedOptions.cs`, `src/Services/Reflekt/Reflekt.API/Features/Teams/TeamSettings.cs`
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/EvidenceLinkSettings.cs` (new; `EvidenceLinks:JiraDomain`) and its reader `AttachEvidenceLinkEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj`, `Directory.Packages.props` (remove `Mapster.DependencyInjection`)
- `src/Services/Reflekt/Reflekt.Domain/Sprints/ISprintAction.cs` (delete), `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardCommand.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/Repository.cs` (delete), `TeamRepository.cs` (comment)
- `src/BuildingBlocks/Blocks.Domain/Entity.cs`, `src/Services/Reflekt/Reflekt.Domain/Sprints/ColumnColor.cs`, `Sprint.cs`, `CardGroup.cs`, `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs`, `src/Services/Reflekt/Reflekt.Domain/Users/User.cs`, `src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLink.cs` (BE-30 items)
- `src/BuildingBlocks/Blocks.Core/GuardExtensions.cs`, `src/BuildingBlocks/Blocks.Persistence/Extensions/RepositoryExtensions.cs`, `Blocks.Exceptions/Extensions.cs` (unused helpers)
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs`, `src/Services/Reflekt/Reflekt.API/Mappings/SprintMappings.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/BroadcastGroupTitleChangedHandler.cs` → `BroadcastGroupTitleChangedOnCardGroupTitleChangedHandler.cs`
- files with redundant usings or fully-qualified names (BE-29), across Domain, Persistence and API

What to build:
- Options bound with `AddOptions<T>().Bind(…).Validate…().ValidateOnStart()`: `TeamSettings` (`MaxTeamsPerUser` ≥ 1), `SeedOptions`, the new `EvidenceLinkSettings` (`JiraDomain` optional, as today), and the Google options (`ClientId` and `ClientSecret` non-empty). For Google, first check what happens today with empty values: `TestGoogleCredentials.cs` says "The Google handler validates its options at startup". If the host already refuses to start without them, no Google binding is added (three `ValidateOnStart` bindings) and the step block records the evidence; if it fails only at the first Google sign-in, the binding is added (four). The integration hosts already set placeholder Google credentials (`TestGoogleCredentials`, all eight test classes), so they start. An endpoint no longer reads `IConfiguration`.
- Dead code: `ISprintAction` and `CreateCardCommand.ActionType`; `Repository<T,TId>`; the `IMapper` registration and the `Mapster.DependencyInjection` reference (props line and csproj); `InternalsVisibleTo` (the only internal type, `TimerEntry`, is not tested); `return;` / `return default!` after a throw; the building-block helpers no code calls after Steps 1–14 (each deletion confirmed by a grep in the step block).
- `Entity`: transient entities are never equal unless the same reference (SK-24). `ColumnColor` is a `sealed record` with the same validation and the same EF value conversion (SK-25).
- BE-30, item by item (values, texts and statuses unchanged throughout):
  - `Sprint`: drop the redundant default-enum assignments; `name?.Trim() ?? ""` on a non-nullable parameter becomes `name.Trim()` only if every caller passes non-null (else left, and recorded); timer constants per timer (Step 13).
  - `CardGroup`: the title-length rule written twice in `Create` and `SetTitle` becomes one private check reading Step 13's constant; its text and its `nameof(title)` stay exactly.
  - `Participant`: `Join` and `JoinWithGoogle` keep their public signatures and fold their shared body into one private builder (`userId` null for `Join`); the `ParticipantTests` unit tests pass unchanged.
  - `ColumnColor` as a record (SK-25, above).
  - `EvidenceLink.Create` becomes `internal` (its one caller is `Card.AttachEvidenceLink` in the same assembly; no test calls it).
  - `User.UpdateProfile`: the redundant compare-then-assign becomes a plain assignment (EF's snapshot tracking marks nothing modified when the value is equal, so the saved row is the same). **No guard is added** — deferred (Decisions row "BE-30 guards").
  - `Card.SetGroupId`: **no guard is added** — deferred (same row). `Card.MarkDeleted` changes nothing: it holds no state to change before its event (the repository removes the card), so the finding's ordering point has nothing to reorder; recorded in the step block.
- `GetSprintSummary`: the hand-written 17-argument construction becomes a Mapster mapping in `SprintMappings.cs` with the same output (BE-34).
- Rename the title handler per the naming rule (SK-27).
- Redundant usings and fully-qualified names removed (BE-29).
- `TeamRepository`'s hand-ordered deletion gets a comment on why EF cascade is not enough (the `Restrict` foreign keys) (SK-30).

Accept:
- `grep -rn "ISprintAction\|ActionType\|class Repository<\|IMapper\|ServiceMapper\|InternalsVisibleTo\|Mapster.DependencyInjection" src Directory.Packages.props` → no hits.
- `grep -rn "ValidateOnStart" src/Services/Reflekt/Reflekt.API` → 3 or 4 bindings, as the Google check above decided (recorded).
- `grep -n "public static EvidenceLink Create" src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLink.cs` → no hits; `grep -n "internal static EvidenceLink Create" …` → 1 hit.
- `grep -c "new Participant" src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs` → 1; `grep -c "characters or fewer" src/Services/Reflekt/Reflekt.Domain/CardGroups/Behaviors/CardGroup.cs` → 1 (the rendered text stays "Title must be 100 characters or fewer.").
- `grep -rn "IConfiguration" src/Services/Reflekt/Reflekt.API/Features` → no hits.
- `git diff 1228e292cab7766891f9b08a42633200cbace18a -- Directory.Packages.props` shows only the removed `Mapster.DependencyInjection` line.
- *Code trace*: `GetSprintSummary` output members before and after.

Runs: all integration files, `AuthSmokeTests.cs`, `FoundationSmokeTests.cs`.

Satisfies: spec BE-10/SK-26, SK-28 (dead `IMapper`, options), BE-29, BE-30, BE-34, SK-24, SK-25, SK-27, SK-30, BE-25 part. Risk: risky — start-up validation and entity equality.

### Step 16 — One shared integration-test host
None (no matching skill). Files:
- `tests/Reflekt.IntegrationTests/Helpers/ReflektApiFactory.cs` (new)
- `tests/Reflekt.IntegrationTests/{Card,EvidenceLink,Participant,Sprint,Team,Vote}EndpointTests.cs`

What to build:
- The duplicated host setup of the six `*EndpointTests` classes (two `WebApplicationFactory` instances per class, policy overrides, Google placeholders) moves into one helper in the existing `Helpers/` folder. The helper can also build a host on a fresh database file of the caller's choosing, which the third, inline host in `SprintEndpointTests.GetTeamSprints_EmptyTeam_ReturnsEmptyArray` (its own empty database, seed off) uses instead of its inline `new WebApplicationFactory<Program>()` — a setup-only edit, its assertions unchanged. Each class keeps its own database file, its own authentication setup and its per-test lifetime. `AuthSmokeTests` and `FoundationSmokeTests` keep their own simple hosts. The stale `Auth:AllowedEmails` settings are removed. No test case, assertion or name changes.
- Pattern to follow: the current setup in `VoteEndpointTests.cs`.

Accept:
- `grep -rn "AllowedEmails" tests` → no hits.
- `grep -c "\[Fact\]\|\[Theory\]" tests/Reflekt.IntegrationTests/*.cs` → the same per-file counts as at the base commit (both pasted).
- `grep -rln "new WebApplicationFactory<Program>" tests/Reflekt.IntegrationTests/*EndpointTests.cs` → no hits.

Runs: all integration files.

Satisfies: spec TS-10. Risk: low.

### Step 17 — Verification and close-out
None. No source file.

Accept:
- Spec AC1–AC6 greps run on the final tree, outputs pasted (AC2's first half now passes literally; AC5's grep has no hit at all, since the accessor reads through `GuestClaims`).
- **AC7**: the three updated assertions (Steps 1 and 5) pass in the integration run. The two codes are shown by one evidence run: a scratch console project under `.claude/tmp/f38-scratch/` that hosts the API with the integration tests' test authentication handler, posts the same team name twice and posts past `MaxTeamsPerUser`, and prints both 409 bodies; input and output are pasted and the project is deleted. Where the scratch host cannot start, the code trace of both throw sites and the middleware's `code` path stands, recorded as such (plan P2).
- **AC8**: the payload traces of Steps 6 and 7 and the logged-failure trace of Step 4, collected in one table; F35's existing integration tests still pass.
- **AC9**: `dotnet build Reflekt.slnx` → 0 warnings (pasted summary line); the integration run starts clean; the full role is the close gate's complete run, recorded as owed to the team lead.
- **Skill/code differences**: a list in `implementation.md` of every place where the code and a project skill still differ after this feature (at least: `error-handling`'s 400 for `DomainException` and its body; `domain-patterns`' awaited publisher; `create-domain-event-handler`'s constructor-injected handler dependencies; `service-registration`'s method names; `extract-endpoint-types`' shared contracts), for F40. Under it, the findings left open on purpose: the BE-30 guards on `User.UpdateProfile` and `Card.SetGroupId` (Decisions row "BE-30 guards").
- `graphify update .` runs; where the command is not found, that is recorded and F40's graph refresh covers it.
- **AC10, developer part**: the walkthrough is run against the running app as far as the session can (the `run` skill or browser tooling); what was seen is recorded, and everything the session cannot observe is listed under `OPERATOR ACTION REQUIRED` for Step 18 (plan P3).
- `git diff --name-only 1228e292cab7766891f9b08a42633200cbace18a -- . ':!graphify-out'` plus `git status --porcelain -- . ':!graphify-out'` list only the files Steps 1–16 name, `docs/specs/F38-BackendConventions/` (its `definition/spec.md` changing only the `**Plan:**` line and the AR-2 amendment of its Decisions row 2 and Out of scope, both made at plan time), `docs/pending-decisions/` (the carrier's deletion), and the campaign's `.claude/.personas.json` and `.claude/tmp/` — no file under `web/`.
- `test ! -e .claude/tmp/f38-scratch` succeeds.

Satisfies: spec AC1–AC9, AC10 (developer part). Risk: low.

### Step 18 — The AC10 two-browser walkthrough (operator)
Owner: operator. None.

The operator runs the walkthrough the spec's AC10 names with two browsers (create, group, dissolve and ungroup cards, vote and retract, advance phases), checks that the second browser sees the same updates as before and that no extra `GroupCreated` frame arrives on dissolve or ungroup (devtools, WebSocket frames), and records the result in `implementation.md` under the Step 17 `OPERATOR ACTION REQUIRED` list. Done check: `N/A (operator-owned)`; AC10 stays open in `summary.md` until recorded.

Satisfies: spec AC10 (operator part).

## Migration Notes

None — no schema change and no migration.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|---|---|---|
| Routes, request and response field names | the existing integration suite (`integration` role per step; the close gate's complete run); Step 2 route paste; Step 12 member list | pinned for covered routes; accepted-gap for routes no test calls — the reviewer's diff read |
| Each endpoint's status for every input, check order included (except behaviour change 2) | integration suite; the per-endpoint record in each step | pinned for covered paths; accepted-gap elsewhere — per-endpoint record and review |
| Every refusal message text | moved-text lists per step; Step 10 hub text paste | accepted-gap — no new test (P1) |
| SignalR message names, payloads, recipients, array order | Steps 6, 7, 10 payload traces; AC10 walkthrough | accepted-gap — no new test (P1) |
| F35 outcomes: redaction, soft kick, transaction-aware publication, closed-sprint guard and text | existing tests; Steps 4, 8, 9 traces; Step 10's `JoinSprint` re-check trace (fresh read after registration) | pinned where tested; the hub's revocation re-check is accepted-gap — no test (P1), Step 10 trace and review |
| No schema change | start-up `Migrate()` in every integration run | pinned |
| No package version change; one reference removed | Step 15 `git diff` on `Directory.Packages.props` | pinned |
| Existing tests pass; only the named assertions change | integration and unit roles per step; Step 16 test counts | pinned |

## Testing Strategy

No new test cases (owner rule, P1). Test edits are: the three AC7 body assertions (Steps 1 and 5), any existing assertion on the validation body shape that the one body changes (named in the step block), the policy-constant renames in test setup (Step 2) and the shared host move (Step 16). Proof comes from:
- the existing unit and integration suites: the `unit` role per step, the `integration` role on every step with a **Runs:** line, and the complete suite the team lead runs at handover and at close;
- plan-time greps re-run in Step 17;
- code traces (per-endpoint order records, moved-text lists, broadcast payload traces) and one scratch evidence run for the two error codes, mapped to the spec's acceptance in `## Decisions`;
- the AC10 walkthrough, developer part and operator part.

## KB Impact

None — the repo has no `docs/kb/`. The architecture doc and the conventions are F40's.

## Decisions

| Decision | Why | Rejected alternative | Confidence | Status |
|---|---|---|---|---|
| No step writes a test; TDD is `no` on every step; only the named assertions change. | The owner's directive binds; see plan P1. | The spec's new handler and integration cases. | high — owner directive | decided |
| Test setup that names a renamed symbol (the policy constants, Step 2) is edited so it compiles; no assertion changes with it. | A rename the spec requires cannot leave the suite uncompiled; the owner's rule is about what a test asserts. | Keeping the old policy names as aliases. | high | decided |
| AC7's body assertions go into three existing tests; the two `code` values are shown by a scratch evidence run, with a code trace as the fallback. | P2. | Two new `CreateTeam` cases. | high | decided |
| AC8's handler and rollback cases become payload traces (Steps 6, 7), the logged-failure trace (Step 4) and the existing F35 tests. | P1; F35 wrote no handler tests to extend. | New handler tests with hub-context fakes. | high | decided |
| AC10 is split: the developer runs what the session can observe, an `Owner: operator` step does the rest. | P3. | Code trace only; blocking the close. | medium — depends on the session's browser tooling | decided |
| No residual validator is added (R2-03). | Every body request already has one; a new validator would answer some multi-fault inputs 400 before today's 404/403. | Validators for the domain-checked limits. | high — census of 11 validators | decided |
| `SprintAccess` returns a result with throwing checks the endpoint calls in its own order, and one `IsOwnedBy` comparison; see plan P4. | The spec forbids the service from ordering failures, and the census found four different orders. | Named per-order methods on the service; a resource-gate authorization handler. | medium | decided |
| Event batches are published in a task the request never awaits, each event with `Mode.WaitForAll` inside it so failures are logged; see plan P5. | `Mode.WaitForNone` leaves a failed handler unobserved, and the spec asks the interceptor to log every failure while staying fire-and-forget. | Per-handler try/catch; `WaitForNone` unchanged with no logging. | medium — rests on FastEndpoints' `WaitForAll` surfacing every handler's failure on the returned task | decided |
| Events of one batch are published one after another, in the collected order. | Keeps today's order of sends between events; a parallel start could reorder them. | One task per event. | medium | decided |
| `TimerService` moves from `Features/Sprints/Timer/` to `Hubs/`; see plan P6. | BE-33 requires the injected hub context, AC2 forbids it in `Features` outside handlers, and the service is hub infrastructure. | A second AC2 exception. | high | decided |
| `CardBroadcast` takes `IHubClients` instead of `IHubContext<SprintHub>`. | AC2; the handlers already hold the context. | Moving `CardBroadcast` out of `Features`. | high | decided |
| The discussed-count rule is a `Sprint` guard called before the parameterless `AdvancePhase()`, the count passed in. | The count spans another aggregate, and the existing `SprintTests` call `AdvancePhase()` without arguments. | `AdvancePhase(int discussedCount)` changing the unit tests. | medium | decided |
| Endpoint state checks whose text differs from the domain's call a `Sprint` guard carrying the endpoint's text. | Both texts are visible today (one to REST clients, one in unit tests); neither may change. | One text per rule, which changes a message. | high | decided |
| The team limit is one focused service (`TeamLimitCheck`); the duplicate name is a repository exists query in the endpoint. | The spec names one service per cross-aggregate rule and lists the vote budget and the team limit. | One `TeamCreationCheck` for both rules. | medium | decided |
| Grouping endpoints keep their save count and their transactions exactly; only the rules and the cleanup move. | New group ids need a save before cards move; changing transactions is outside a convention pass. | One save per endpoint. | high | decided |
| The four moved broadcasts — `GroupCreated`, `GroupDissolved`, `VoteCast`, `VoteRetracted` — may now reach clients after the HTTP response instead of before it. | Payload and recipients are identical; SignalR and HTTP never guaranteed an order across two connections; spec behaviour change 2 accepts handler dispatch. | Keeping the inline sends. | medium — F39 owns the SPA's handling | decided |
| **AR-2:** F38 edits no frontend file; the `CreateTeamForm.vue` change (prefix parsing → `code`) is F39's, and F39 merges before F38. The spec's Decisions row 2 and Out of scope are amended to say so. | Shared answer AR-2 (shared architect, binding on the campaign): F39 owns every SPA error consumer, and one owner per file avoids a cross-branch conflict. | F38 edits the form as the spec first said. | high — a binding shared answer | decided |
| Detached publication token: the task that publishes a batch passes `CancellationToken.None` to the handlers. | The save's token is the endpoint's `ct` (from `RequestAborted`) and the task outlives the request; with `None`, no broadcast depends on whether the server signals that token after a normal completion, which this plan does not need to settle. Handlers that run at shutdown fail and are logged (Step 4). | The request token (the open question); an application-stopping token injected into the BuildingBlocks interceptor (a hosting dependency the block does not have). | medium — presumed (proceed-default): the server's behaviour was not measured; the choice is safe either way | decided |
| `PhaseActionPolicyProvider` exposes one awaited `GetAsync()`, loaded once on success with `CancellationToken.None`; each of the 16 sites awaits it. | An async repository read cannot sit behind a synchronous singleton without blocking, which Step 11 bans; loading in the migrate/seed start-up would tie the Persistence start-up to an API service. | Blocking load in the constructor (today's shape); preloading at start-up. | high | decided |
| Departs from create-domain-event-handler, required by spec row "One broadcast mechanism" (the four handlers become the only senders) and FastEndpoints 5.34.0's singleton event handlers: the four handlers read through `IServiceScopeFactory` per call instead of taking scoped repositories in their constructor. | A singleton holding a scoped repository keeps one `DbContext` for the process; F35's `CardBroadcast` callers already read this way. | Constructor-injected repositories, as the skill's template shows. | high | decided |
| The hub's two participant checks in `JoinSprint` read through a new `ParticipantRepository.IsActiveAsync` (`AsNoTracking` exists query), not `GetByIdAsync`. | Constructor injection (spec BE-11) puts both reads on one `DbContext`; `FindAsync` would serve the second from the tracker and miss a kick committed in between, reopening the race F35 closed. | A scope per read inside the hub (today's shape, against the spec's constructor injection); `AsNoTracking` on `GetByIdAsync` for every caller (changes write paths that need tracking). | high — read in `SprintHub.cs` and `RepositoryBase.GetByIdAsync` | decided |
| BE-30 guards: no guard is added to `User.UpdateProfile` or `Card.SetGroupId`; the other BE-30 items are built (Step 15). | The owner's standing rule allows no new product behaviour: a guard on `UpdateProfile` would refuse a sign-in whose name claim is blank (today it is stored), and a guard on `SetGroupId` could refuse a call path that passes an unsaved group. The spec's "the guards they add included" yields to the owner's rule. Listed for F40 (Step 17). | Adding the guards (changes a status for an edge input). | medium — presumed (proceed-default): the owner's rule read against the spec row | deferred |
| Shared request/response types stay in the feature's `*Contracts.cs`; single-slice types move to the sibling file. | A type used by several slices has no single sibling; AC6 checks only endpoint files. | Duplicating shared types per slice. | medium — a reading of the spec's layout row | decided |
| Renamed DTO types (`*Response`) change OpenAPI schema names, not JSON members. | The spec fixes field names, not type names. | Keeping the mixed suffixes. | high | decided |
| Registration method names (`AddReflekt*`, `UseReflekt*`) stay. | The spec's row narrows SK-28 to the dead `IMapper`, the twins and the options; listed for F40. | Renaming to the skill's names. | high | decided |
| Departs from domain-patterns, required by spec row "Hub, timer, events" ("event publishing stays fire-and-forget"): the save does not await the publication, where the skill's publisher awaits `Mode.WaitForAll`. | Awaiting would change request timing and turn handler failures into request failures, the opposite of behaviour change 2. | Awaiting the publication. | high | decided |
| Departs from error-handling, required by spec row "One error convention" and F35: `DomainException` maps to 409, and the body is the spec's camelCase shape with `code` and `errors[{field, message}]`, where the skill maps it to 400 with its own body. | The spec sets both; the skill stays unedited (owner decision 2026-10-12) and the difference goes to F40. | Following the skill. | high | decided |

## Decisions and why

### P1 — No new tests; acceptance lines that need a new test become code traces or evidence runs
- **By:** owner
- **Decided:** no new test case, class or file. Existing tests are changed only where F38 changes what they assert (status bodies, the shared fixture move under TS-10). Each spec acceptance line that can only be met by a new test is rewritten in the plan as an existing-test check, a code trace or a scratch evidence run, each listed in `## Decisions`.
- **Why:** "Write no new tests; update an existing test only where a fix changes what it asserts." (the owner's standing campaign directive, `.claude/.campaign-directives.json`; applied the same way to F35, `docs/specs/F35-BackendCorrectnessFixes/delivery/questions.md` Q1).
- **Turned down:** new handler tests and new integration cases as spec AC7/AC8 word them; the backlog's older "extended with a regression case" wording.
- **Rests on:** the campaign directive; if the owner later allows regression cases, AC7's two `code` cases and AC8's handler cases come back as tests.
- **Confidence:** high — the owner's own rule, already ruled on for F35.
- **Source:** campaign directive; F35 `questions.md` Q1
- **State:** current

### P2 — AC7 body assertions go into existing tests; the two `code` values are proven by an evidence run
- **By:** architect
- **Decided:** the one-body assertions are added to existing tests that already hit each status family — a 404 (`SprintEndpointTests.CreateSprint_WrongTeam_Returns404`), a 409 (`SprintEndpointTests.StartSprint_FromActive_Returns409`), a validation 400 (`TeamEndpointTests.CreateTeam_EmptyName_ReturnsValidationError`, `errors[0].field`). No existing test reaches the duplicate-name or team-limit 409, so `code = duplicate_name` and `code = limit_reached` are proven by a scratch evidence run against the built API (output pasted into `implementation.md`, scratch deleted) plus a code trace of the throw sites. AC8's handler and rollback cases (no such tests exist, F35 wrote none) become per-handler payload code traces plus the AC10 walkthrough.
- **Why:** shared answer AR-2 keeps "the integration body assertions"; P1 forbids new test cases. Putting the assertions in tests that already exist honours both; only the two cases with no existing test move to evidence runs, the route F35 took under the same rule.
- **Turned down:** two new `CreateTeam` test cases (breaks P1); dropping the `code` check entirely (leaves F39's contract unproven).
- **Rests on:** no existing integration test posting a duplicate team name or exceeding the team limit (grep of `tests/Reflekt.IntegrationTests/TeamEndpointTests.cs`, 2026-10-23).
- **Confidence:** high — follows the owner's rule and the F35 precedent.
- **Source:** Phase 1 analysis, 2026-10-23
- **State:** current

### P3 — The AC10 two-browser walkthrough: developer first, operator for what an agent cannot observe
- **By:** architect
- **Decided:** a verification step runs the AC10 walkthrough against the running app (the `run` skill / browser tooling where available) and records what was seen; every item the session cannot observe is listed under `OPERATOR ACTION REQUIRED` in `implementation.md`, and a final `Owner: operator` step completes it (done check: `N/A (operator-owned)`); AC10 stays open in `summary.md` until recorded.
- **Why:** the run is unattended and two live browser sessions may be out of an agent's reach; F36 used the same split under the same campaign.
- **Turned down:** marking AC10 met by code trace alone; blocking the close on it.
- **Rests on:** no agent being able to drive two browser sessions in this environment; if one can, the operator step is empty.
- **Confidence:** medium — depends on what browser tooling the build session has.
- **Source:** Phase 1 analysis, 2026-10-23; precedent `docs/specs/F36-FrontendDefects/delivery/plan.md` Steps 13–14
- **State:** current

### P4 — `SprintAccess` answers; each endpoint orders
- **By:** architect
- **Decided:** `SprintAccess` loads and returns a result (sprint, team, caller identity, `IsGuest`, `IsOwnedBy`) and offers throwing checks; each endpoint calls them in its own order from the Access order reference. The only `OwnerId` comparison lives in it.
- **Why:** the spec says the service "never decides the order of failures" and "each endpoint keeps the status it returns today for every input"; the census found four orders (team-first, sprint-first, card-first, kick's state-first). Per-order methods would put the orders back inside the service.
- **Turned down:** one uniform order (changes statuses); per-order service methods; an authorization handler.
- **Rests on:** the census of 44 endpoints, 2026-10-23; the code wins on a mismatch and the per-endpoint record exposes it.
- **Confidence:** medium — the endpoints keep a few lines of ordering each.
- **Source:** Phase 2 planning, 2026-10-23
- **State:** current

### P5 — Fire-and-forget publication that still observes every failure
- **By:** architect
- **Decided:** the interceptor publishes a batch inside a task the request never awaits; inside it each event is awaited with `Mode.WaitForAll`, failures are logged one per handler, and nothing is rethrown. The handlers receive `CancellationToken.None`, not the request's token (plan review round 1, R1-01).
- **Why:** `Mode.WaitForNone` returns a completed task, so a handler that fails later is never observed; the spec asks for logging while keeping fire-and-forget. Awaiting each event in order keeps today's order of sends between events.
- **Turned down:** a try/catch in every handler (the spec names the interceptor); awaiting the publication in the save (changes request timing and failure, against behaviour change 2).
- **Rests on:** FastEndpoints 5.34.0's `WaitForAll` returning a task that faults with every handler's failure; the developer confirms it from the package and records it.
- **Confidence:** medium
- **Source:** Phase 2 planning, 2026-10-23; spec row "Hub, timer, events"
- **State:** current

### P6 — `TimerService` moves to `Hubs/`
- **By:** architect
- **Decided:** `TimerService` moves to `Reflekt.API/Hubs/` with `ReadinessTracker` and `TimerSnapshot`, and injects `IHubContext<SprintHub>` there.
- **Why:** BE-33 requires the injected hub context; AC2 forbids `IHubContext<SprintHub>` in `Features` outside handler files; the timer is hub infrastructure, not a feature slice. The move meets both without an exception to AC2.
- **Turned down:** keeping it under `Features/Sprints/Timer/` with an AC2 exception.
- **Rests on:** AC2's grep scoping to `Features`.
- **Confidence:** high
- **Source:** Phase 2 planning, 2026-10-23
- **State:** current

## Open Questions

None.

## Plan Review

Gear review mode: critic, Codex + Sonnet (`reader-pair.md`), run by the team lead.

**Round 1 (2026-10-23)** — one reader (Sonnet main under the late rule; Codex and the Sonnet second reader lost). REVISE: 0 CRITICAL, 0 HIGH, 5 MEDIUM, 5 LOW (R1-01..R1-10). All ten folded `fixed-real`; record in `review-critic.md` § Fold — round 1 (plan). The two open questions are settled in `## Decisions`: the request token no longer reaches the detached handlers (row "Detached publication token"), and the BE-30 guards are deferred under the owner's no-new-behaviour rule (row "BE-30 guards"). Contract changes: Step 3's provider accessor, Step 4's handler token, Step 15's BE-30 scope, the Access order reference (RetractVote, CreateCard), and the spec's Decisions row 2 / Out of scope (AR-2). Round 2 owed (MEDIUM in the round-1 merge).

**Round 2 (2026-10-23)** — one reader (same Sonnet primary), full read. All R1 folds verified closed. REVISE: 0 CRITICAL, 1 HIGH, 1 MEDIUM, 3 LOW (R2-01..R2-05). Folded: R2-01 (Step 10 — the `JoinSprint` revocation re-check reads fresh through `ParticipantRepository.IsActiveAsync`; Decisions row; Must-NOT-Change row), R2-02 (Step 5 — Active check kept ahead of the phase check; discussed count in memory after the focus mark), R2-03 (header count), R2-05 (Sprint-first census row) `fixed-real`; R2-04 (F39's dependency line) `waived` for F38 — no F38 change; the coordinator sent it to the campaign's shared architect as a notice, and F38 edits nothing in F39. Record in `review-critic.md` § Fold — round 2. No CRITICAL in round 2, so no round 3: the plan is approved. Build slices unchanged.
