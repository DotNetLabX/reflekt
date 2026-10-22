# F38 — Backend Conventions Conformance

**Kind:** technical refactoring (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` § 3; finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/backend.md` (BE-*), `findings/skills.md` (SK-*) and `findings/deps-tests-conventions.md` (TS-10)
**Dependencies:** F35 (defects fixed first), F37 (packages)
**Rule list:** not applicable — restructuring with no intended behaviour change
**Status:** Ready
**Plan:** None

---

## Purpose

The backend is a teaching example, and today it teaches several conflicting ways to do the same job: four error idioms, two broadcast mechanisms, an ownership check pasted into 30 files, business rules in endpoints instead of aggregates, and building blocks that are compiled in but never used. This feature brings the code onto the repo's own conventions (`CLAUDE.md`, `docs/conventions/`) and pattern skills (`.claude/skills/`), so that each job is done one way. Behaviour stays the same except for the error-body change named in Decisions.

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below picks between its options or narrows it.

| Finding(s) | Theme | Required outcome |
|---|---|---|
| BE-05, SK-02, SK-06, BE-09 | One error convention | Follow the documented rule (`docs/conventions/csharp.md` § Error Handling, the `error-handling` skill): handlers throw the exception family (`NotFoundException`, `ForbiddenException` and `ConflictException` added per the skill's extension point, `BadRequestException`, `UnauthorizedException`) and the middleware maps them; no `Send*Async` status sends, `ThrowError` or `AddError`+`SendErrorsAsync` in handlers. One error body, camelCase: `statusCode`, `message`, optional `code`, `traceId`, optional `errors[]` — FastEndpoints validation failures use the same body. Machine codes travel in `code` (`duplicate_name`, `limit_reached`), never as message prefixes. Building blocks the convention uses are adopted (`Guard`, the `*OrThrow` lookups); the rest is deleted. |
| BE-03, SK-13 | One broadcast mechanism | Domain events fire and their handlers broadcast; endpoints no longer inject `IHubContext<SprintHub>` or call `ClearDomainEvents()`. `RetractVote` calls `MarkRetracted()`. Batch cases (leaving Writing) use one event or a no-event factory, as the plan decides. Events of a transactional save are published after the commit. |
| BE-04, SK-08, SK-09, SK-18, SK-21 | One resource-access check | One focused operation service (`extract-feature-service` skill), e.g. `SprintAccess`, returning the sprint, team and caller role, used by every endpoint and the hub; typed claim accessors (`GetParticipantId()`, `GetSprintId()`) and claim-name constants; one phase-action guard. Check order is the same everywhere: 401 unauthenticated → 404 unknown resource → 403 not yours. A guest token for another sprint → 403 (today 401). |
| BE-06, SK-12, BE-38, BE-15 (rule part) | Rules in aggregates | Single-aggregate rules move into behaviour methods (`Sprint.AdvancePhase`, `Sprint.SetFocus`, group-membership rules on `CardGroup`, sprint-open/active guards); cross-aggregate rules (vote budget, team limit) into focused operation services, one per rule; the phase-action matrix becomes a domain-facing policy fed from a `PhaseActionRepository` (no `DbContext` in the API layer). Endpoints become load → call → save. |
| BE-08, SK-15 | Duplicated handler logic | `SprintReportBuilder` used by both report endpoints; one regroup/cleanup operation for the five grouping endpoints; one group-response builder. |
| BE-10, SK-26, SK-22, BE-27, SK-28 | Dead code and twins | Delete `ISprintAction`, unused `Repository<T,TId>`, unused `IMapper` registration and its package, `GetParticipantCount`, redundant returns after throws, `InternalsVisibleTo` with nothing internal; `Remove` once in the base repository; repository read DTOs returned directly instead of identical API twins; one `*Response` suffix. |
| BE-11, SK-16 | EF in the API layer | No `Query()`/`IQueryable` outside Persistence; `HasActiveSprintAsync`, `PhaseActionRepository.GetAllAsync`; the hub takes repositories by constructor injection. |
| BE-16, BE-33, SK-03 | Hub and timer | `ReadinessTracker` service instead of static hub state; one `TimerSnapshot.ToPayload()`; `Sprint.HasTimer`; timer expiry broadcast through an injected hub context with failures logged. Single-instance deployment stays (documented in F40). |
| BE-17, BE-18, BE-35 | Reads and size | `AsNoTracking` on read-only repository methods; count/exists queries instead of loading lists; methods within ~60 lines; `IReadOnlyList<T>` on public collections and records. |
| BE-19, SK-11 (rest), BE-31, SK-20 | Validation and limits | Remaining mutating endpoints get validators where they take input; limits as constants on the domain types, used by domain, validators and EF configurations. |
| BE-25, SK-27, BE-26, SK-10, BE-29, BE-30, BE-32, BE-34, SK-19, SK-24, SK-25, SK-30 | Naming and layout | Handler names per the naming rule; endpoint file = endpoint class, request/response/validator in the sibling `*Command.cs`/`*Query.cs` (`extract-endpoint-types` skill); redundant usings removed and the `Features/Auth` namespace renamed so `Policies` resolves without an alias (routes unchanged); domain polish items; migrate/seed host extension moved to the API and made async; options bound with validation on start; identical policies merged and named for what they check; `Entity` transient-equality guard; `ColumnColor` as a record; a comment on why team deletion is ordered by hand. |
| TS-10 | Test duplication | One shared integration-test host fixture in the existing helpers folder, used by the 8 endpoint test classes; the stale `Auth:AllowedEmails` test settings removed. |

## Decisions (taken — architect)

| Decision | Why | Rejected alternative | Confidence |
|---|---|---|---|
| Adopt the documented exception convention rather than rewrite the docs to the FastEndpoints idioms. | It is what the repo's conventions and skills teach; the building blocks exist; one central mapper. | Amend the rule and delete the exception family (smaller diff). | medium — larger change, but it is the teaching point |
| The error **body** changes (404/403 get a body; one camelCase shape; `code` field). Status codes do not change, except a guest on the wrong sprint: 401 → 403. F38 updates the one SPA consumer that reads the body — `web/reflekt-web/src/components/teams/CreateTeamForm.vue` (prefix parsing → `code`) — and the integration tests that assert bodies or that 401. | Contract change kept to the minimum the convention needs; the app keeps working between F38 and F39. | Keep three body shapes. | medium |
| Event publishing stays fire-and-forget (`Mode.WaitForNone`); handler failures are logged. The deviation from the `domain-patterns` skill is recorded in `known-deviations.md` (F40). | With wait-for-all, a failed broadcast would fail a request whose data is already committed. | `WaitForAll` per the skill. | medium |
| Routes stay as they are, the two URL schemes included (BE-36 is documented in F40, not changed). | Behaviour-neutral refactor; the SPA and tests depend on them. | Unify routes; make `advance-phase/check` a GET. | high |
| Project skills under `.claude/skills/` are not edited (owner decision 2026-10-12, kept as built); where code and skill still differ after this feature, F40 records it. | Owner kept the agent setup as evidence. | Rewrite the skills to the code. | high |

## Out of scope

- Behaviour changes beyond the Decisions above; defects (F35); packages (F37); frontend beyond the one consumer named; docs (F40).
- New tests beyond updating existing ones and the shared fixture.

## What must not change

- Every route, request field and response field; every status code except the one named in Decisions.
- What each endpoint allows and refuses (the same rules, moved — not changed).
- SignalR event names and payloads.
- The existing test suite passes after its assertions on error bodies and the one status change are updated; every updated assertion is named in `implementation.md`.

## Acceptance (checkable)

1. `grep -rnE "Send(NotFound|Forbidden|Unauthorized|Errors)Async|ThrowError\(|AddError\(" src/Services/Reflekt/Reflekt.API/Features` → no hits.
2. `grep -rn "IHubContext<SprintHub>" src/Services/Reflekt/Reflekt.API/Features --include=*Endpoint.cs` → no hits; `grep -rn "ClearDomainEvents()" src/Services/Reflekt/Reflekt.API` → no hits.
3. `grep -rn "team.OwnerId != userId\|OwnerId != userId" src/Services/Reflekt/Reflekt.API/Features` → no hits (the check lives in one service).
4. `grep -rln "Microsoft.EntityFrameworkCore\|\.Query()" src/Services/Reflekt/Reflekt.API` → no hits.
5. `grep -rn "FindFirstValue(\"participantId\"\|FindFirstValue(\"sprintId\"" src` → only the claim-accessor file.
6. No endpoint file declares a request, response or validator type (`grep -lE "record .*Request|class .*Validator" src/Services/Reflekt/Reflekt.API/Features/**/*Endpoint.cs` → no hits).
7. `dotnet build Reflekt.slnx` → 0 warnings; the declared full role in `.claude/verify.json` passes.
