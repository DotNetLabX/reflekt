# F38 — Backend Conventions Conformance

**Kind:** technical refactoring (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` § 3; finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/backend.md` (BE-*), `findings/skills.md` (SK-*) and `findings/deps-tests-conventions.md` (TS-10)
**Dependencies:** F35 (defects fixed first, incl. transaction-aware event publication and the camelCase error body), F37 (packages)
**Rule list:** not applicable — restructuring with no intended behaviour change
**Review:** round 1 — `delivery/review-critic.md`
**Status:** Ready
**Plan:** `docs/specs/F38-BackendConventions/delivery/plan.md`

---

## Purpose

The backend is a teaching example, and today it teaches several conflicting ways to do the same job: four error idioms, two broadcast mechanisms, an ownership check pasted into 30 files, business rules in endpoints instead of aggregates, and building blocks that are compiled in but never used. This feature brings the code onto the repo's own conventions (`CLAUDE.md`, `docs/conventions/`) and pattern skills (`.claude/skills/`), so each job is done one way. Every status code and every SignalR message stays as it is; the two behaviour changes are listed in Decisions.

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below picks between its options or narrows it.

| Finding(s) | Theme | Required outcome |
|---|---|---|
| BE-05, SK-02, SK-06, BE-09 | One error convention | Handlers throw the exception family — `NotFoundException`, `BadRequestException`, `UnauthorizedException`, plus `ForbiddenException` and `ConflictException` added per the `error-handling` skill's extension point — and the middleware maps them; handlers no longer use `Send*Async` status sends, `ThrowError` or `AddError`+`SendErrorsAsync`. **One error body** for every error the middleware or FastEndpoints validation produces: `statusCode`, `message`, `traceId`, optional `code`, optional `errors` — an array of `{ "field": string, "message": string }` (validation failures put one item per failed rule). `code` is a closed set, listed in the plan and in F40's architecture doc; today it holds `duplicate_name` and `limit_reached`, which stop travelling as message prefixes. Policy-level 401/403 written by the authentication setup (`OnRedirectToLogin`, `OnRedirectToAccessDenied`) stay bodyless. Building blocks the convention uses are adopted (`Guard`, the `*OrThrow` lookups); the rest is deleted. |
| BE-03, SK-13 | One broadcast mechanism | Domain events fire and their handlers broadcast; endpoints no longer inject `IHubContext<SprintHub>` or call `ClearDomainEvents()`. **Every client receives exactly the messages and payloads it receives today**, array order included: the solo groups created by `DissolveGroup` and `RemoveCardFromGroup` and the groups created when leaving Writing use a factory that raises no event (or one batch event) so no new `GroupCreated` message appears; `GroupDissolved`, sent inline today, gets its own domain event and handler; `RetractVote` calls `MarkRetracted()`. Publication after commit uses F35's transaction-aware interceptor — F38 adds only `CastVote`'s serializable transaction to it. |
| BE-04, SK-08, SK-09, SK-21 | One resource-access check | One focused operation service (`extract-feature-service` skill), e.g. `SprintAccess`, used by every endpoint that addresses a sprint or a team and by the hub; anonymous endpoints (`JoinSprint`, `GetSprintSummary`, `GetPhaseActions`) do not use it. It returns a result (found sprint, team, caller identity) and never decides the order of failures: **each endpoint keeps the status it returns today for every input**, so where an endpoint checks ownership before existence today it still does. Identity precedence stays as today: a participant claim takes the participant branch even when an owner cookie is also present. The card-author check stays where it is. The hub translates the result into `HubException` with today's messages. Typed claim accessors (`GetParticipantId()`, `GetSprintId()`) and claim-name constants replace the string reads; one phase-action guard replaces the 16 copies. |
| BE-06, SK-12, BE-38 | Rules in aggregates | Single-aggregate rules move into behaviour methods (`Sprint.AdvancePhase`, `Sprint.SetFocus`, group-membership rules on `CardGroup`); cross-aggregate rules (vote budget, team limit) into focused operation services, one per rule; the phase-action matrix becomes a domain-facing policy fed from a `PhaseActionRepository` (no `DbContext` in the API layer). **A moved rule keeps its status:** rules answered with 400 today throw `ArgumentException` (→ 400), rules answered with 409 throw `DomainException` (→ 409, per F35), with today's messages. F35's `Sprint.EnsureOpen()` message and placement stay. Endpoints become load → call → save. |
| BE-08, SK-15 | Duplicated handler logic | `SprintReportBuilder` used by both report endpoints (keeping F35's redaction); one regroup/cleanup operation for the five grouping endpoints; one group-response builder. |
| BE-10, SK-26, SK-22, BE-27, SK-28 | Dead code and twins | Delete `ISprintAction`, the unused `Repository<T,TId>`, the unused `IMapper` registration and its package, `GetParticipantCount`, redundant returns after throws, and `InternalsVisibleTo` (nothing internal is tested — F35's new tests use public functions); `Remove` once in the base repository; repository read DTOs returned directly instead of identical API twins; one `*Response` suffix. |
| BE-11, SK-16 | EF in the API layer | No EF type or `Query()`/`IQueryable` in API source; `HasActiveSprintAsync`, `PhaseActionRepository.GetAllAsync`; the hub takes repositories by constructor injection; the migrate/seed start-up is one Persistence method the API host calls (so the API itself stays free of EF usings). |
| BE-16, BE-33, SK-03 | Hub, timer, events | `ReadinessTracker` service instead of static hub state (F35's kick handler uses it); one `TimerSnapshot.ToPayload()`; `Sprint.HasTimer`; timer expiry broadcast through an injected hub context; the save awaits event publication, so every broadcast completes before the request's response as the inline sends did, and the interceptor logs every handler failure without failing the save or the request (amended 2026-10-23, fix round 1, `delivery/questions.md` Q2 — was "event publishing stays fire-and-forget"). Single-instance deployment stays (documented in F40). |
| BE-17, BE-18, BE-35 | Reads and size | New `AsNoTracking` read methods for the read endpoints; every caller that changes the loaded entities keeps a tracked load; count/exists queries instead of loading lists; methods within ~60 lines; `IReadOnlyList<T>` on public collections and records. |
| BE-19, SK-11 (rest), BE-31, SK-20, BE-23 (truncation) | Validation and limits | A validator for every request whose body input is not yet validated after F35 (the plan lists them); route ids are not validated, so 404 paths stay 404. Limits become constants on the domain types used by domain, validators and EF configurations, the OpenGraph title/description truncation lengths included. |
| BE-25, SK-27, BE-26, SK-10, BE-29, BE-30, BE-32, BE-34, SK-19, SK-24, SK-25, SK-30 | Naming and layout | Handler names per the naming rule; endpoint file = endpoint class only, every request, response, command, query, DTO and validator in the sibling `*Command.cs`/`*Query.cs` (`extract-endpoint-types` skill); redundant usings removed and the `Features/Auth` namespace renamed so `Policies` resolves without an alias (routes unchanged); domain polish items, the guards they add included; options bound with validation on start (tests keep their placeholder Google credentials); identical policies merged and named for what they check; `Entity` transient-equality guard; `ColumnColor` as a record; a comment on why team deletion is ordered by hand. |
| TS-10 | Test duplication | One shared integration-test host fixture in the existing helpers folder, used by the six `*EndpointTests` classes; `AuthSmokeTests` and `FoundationSmokeTests` keep their own simple hosts; each class keeps its own database file and authentication setup; the stale `Auth:AllowedEmails` test settings are removed. |

## Decisions (taken — architect)

| Decision | Why | Rejected alternative | Confidence |
|---|---|---|---|
| Adopt the documented exception convention rather than rewrite the docs to the FastEndpoints idioms. `docs/conventions/csharp.md` contradicts itself (line 12 says throw `NotFoundException`; line 73 says "not found" is a result, not an exception): this feature follows line 12, and F40 rewrites line 73 to match. | It is what the repo's conventions and skills teach; the building blocks exist; one central mapper. | Amend the rule and delete the exception family. | medium |
| **Behaviour change 1 — error bodies.** 404/403 answered by handlers gain the body; every error body is camelCase in one shape with `code`. The one SPA consumer that reads a body — `web/reflekt-web/src/components/teams/CreateTeamForm.vue` (prefix parsing → `code`) — is updated by F39, which owns every SPA error consumer and merges before F38 (amended 2026-10-23, shared answer AR-2; F38 edits no frontend file); F38 adds body assertions to existing integration test classes (one per status family, plus the two codes). | The convention needs one body; F39's API client reads it. | Keep three body shapes. | medium |
| **Behaviour change 2 — a broadcast failure no longer fails a committed request.** Today four endpoints await their SignalR sends, so a send that throws after the save returns 500 although the data is saved; as handlers, a failed send is logged and the request returns its normal status. | Reporting failure for committed data is the worse behaviour. | Fail the request when a handler fails (amended 2026-10-23, fix round 1, Q2 — was "Wait for every handler": the save now waits, but never fails). | medium |
| No other status code changes. Apart from behaviour change 2, each endpoint keeps today's status for every input (order of checks included); a guest on another sprint keeps today's 401. | Behaviour-neutral refactor; the frontend clears a stored token only on 401. | Unify the check order 401 → 404 → 403. | high |
| Routes stay as they are, the two URL schemes included (BE-36 is documented in F40). | The SPA and tests depend on them. | Unify routes. | high |
| Project skills under `.claude/skills/` are not edited (owner decision 2026-10-12); where code and skill still differ after this feature, the plan lists it and F40 records it. | Owner kept the agent setup as evidence. | Rewrite the skills. | high |

## Out of scope

- Defects (F35); packages (F37); every frontend file, the one consumer named in Decisions included (F39, per AR-2); docs (F40).
- New test projects, frameworks or helper libraries.

## What must not change

- Every route, request field and response field; every status code except behaviour change 2 (Decisions); every SignalR method, message name, payload and recipient set (array order included).
- What each endpoint allows and refuses, and the message it refuses with.
- F35's outcomes: redaction, soft kick, transaction-aware publication, the closed-sprint guard and message.
- The existing suite passes; every updated assertion is named in `implementation.md`.

## Acceptance (checkable)

Greps run from the repository root over source files only.

1. `grep -rnE --include=*.cs "Send(NotFound|Forbidden|Unauthorized|Errors)Async|ThrowError\(|AddError\(" src/Services/Reflekt/Reflekt.API` → no hits.
2. `grep -rn --include=*.cs "IHubContext<SprintHub>" src/Services/Reflekt/Reflekt.API/Features | grep -v "Handler.cs"` → no hits; `grep -rn --include=*.cs "ClearDomainEvents()" src/Services/Reflekt/Reflekt.API` → no hits.
3. `grep -rnE --include=*.cs "OwnerId != " src/Services/Reflekt/Reflekt.API` → only inside the access service.
4. `grep -rlE --include=*.cs "Microsoft.EntityFrameworkCore|\.Query\(\)|IQueryable" src/Services/Reflekt/Reflekt.API` → no hits.
5. `grep -rn --include=*.cs "FindFirstValue(\"participantId\"\|FindFirstValue(\"sprintId\"" src` → only the claim-accessor file.
6. `find src/Services/Reflekt/Reflekt.API/Features -name "*Endpoint.cs" -exec grep -lE "(record|class) [A-Za-z]+(Request|Response|Command|Query|Validator|Dto|Summary|Entry)\b" {} +` → no hits.
7. Integration body assertions (existing classes): a 404, a 409 with `code` = `duplicate_name`, a 409 with `limit_reached`, and a validation 400 with `errors[0].field`, each in the one camelCase shape.
8. Handler and integration tests (existing classes; hub-context fakes as in F35): each broadcast handler that replaced an inline send sends the same message and payload the endpoint sent before; a handler whose send throws is logged and the request still returns its normal status; F35's rollback tests still pass for the moved broadcasts.
9. `dotnet build Reflekt.slnx` → 0 warnings; the integration suite starts clean (the API's start-up `Migrate()` throws on a pending model change, so no schema change slipped in); the declared full role in `.claude/verify.json` passes.
10. One manual walkthrough recorded in `implementation.md` with two browsers: create, group, dissolve and ungroup cards, vote and retract, advance phases — the second browser sees the same updates as before, and no extra `GroupCreated` arrives on dissolve or ungroup (browser devtools, WebSocket frames).
