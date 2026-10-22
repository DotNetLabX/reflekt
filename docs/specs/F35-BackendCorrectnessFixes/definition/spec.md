# F35 — Backend Correctness & Security Fixes

**Kind:** technical (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` § 1; finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/backend.md` (BE-*) and `findings/skills.md` (SK-*)
**Dependencies:** F33 (done), F37 (packages settle first — same backend lane)
**Rule list:** not applicable — defect fixes against existing behaviour; the rules they restore are stated below
**Status:** Ready
**Plan:** None

---

## Purpose

The backend has real defects a student cloning the repo would hit or copy: domain events can be lost, other participants' card text leaks during the Writing phase, rule violations come back as 500s with raw exception text, a closed sprint can still be edited, and kicking an active participant crashes. This feature fixes those defects and the two security holes the audit found. It does not restructure the code — that is F38.

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below narrows it or picks between its options.

| Finding(s) | Defect | Required outcome |
|---|---|---|
| BE-01, SK-01 | Event interceptor is a singleton holding per-save state | Interceptor registered scoped and resolved from the context's provider; a failed save clears its pending events. |
| BE-02 | Writing-phase card text broadcast to the whole sprint over SignalR | One redaction function, used by `ListCards` and by every card broadcast (create, edit, evidence attached/detached/fetched). During Writing, the author's own connections get the full card; everyone else gets the same redacted shape the REST list returns today (text empty, no assignee, no evidence links). The team owner's view is unchanged. |
| BE-07, SK-04, SK-05 | Domain rule violations surface as 500 | Aggregates throw `DomainException` (exists in `Blocks.Domain`) for state-rule violations instead of `InvalidOperationException`; the middleware maps it to 409. `ArgumentException` stays for bad arguments and maps to 400. `SetReviewStatus` on a non-action-item card returns 404. |
| BE-20, SK-07 | Raw exception text returned for 5xx | A 5xx response carries a generic message and the trace id only; the real exception is logged (already is). Status taken from `HttpException.StatusCode`; response serialised camelCase. |
| BE-13, SK-17 | Kicking a participant who wrote a card or voted fails on a restrict foreign key | **Owner decision 2026-10-22 — soft kick** (see Decisions): the participant row is kept and marked removed, their guest token stops authenticating, their cards and votes stay, their open SignalR connections are evicted from the sprint group, and they drop out of participant lists and readiness counts. |
| BE-12, SK-14 | Multi-step changes saved in several non-transactional saves | `AdvancePhase` (leaving Writing), `CreateGroup`, `DissolveGroup` (no save per card in a loop), `RemoveCardFromGroup`: each persists in one save or one explicit transaction, and its domain events are published only after the final commit, so clients never see a phase change before the cards carry their groups. |
| BE-14 | A closed sprint can still be grouped, renamed and re-columned | Every mutating endpoint rejects a closed sprint with 409 and one message, `Sprint is closed.` — the group endpoints and `UpdateColumns` included. |
| BE-15, SK-23 | "One vote per group" and "display name unique per sprint" enforced only by read-then-insert | Unique index on votes `(GroupId, ParticipantId)`; unique index on participants `(SprintId, DisplayName)` case-insensitive, **filtered to participants not removed**; a unique-constraint violation returns 409. The migration first repairs existing data (see Decisions). The dev seed no longer casts two votes by one participant on one group. `docs/features/Voting.md` stale lines are out of scope (feature docs are kept as evidence). |
| BE-19, SK-11 (part) | Out-of-range enum values persisted; null `CardIds` crashes `CreateGroup` | `ColumnType`, `CardType` and review status rejected unless they name a defined value (no numeric strings); `CreateGroup` validates `CardIds` not empty and distinct; `UpdateGroupTitle` validates length ≤ 100. One validation per input — the second inline parse is removed. |
| SK-09 (part) | `GetUserId()` throws `InvalidOperationException` (→ 500) when the claim is missing | Returns 401 via `UnauthorizedException`. |
| BE-21 | Open redirect after sign-in (`/\evil.com` accepted) | Return URL accepted only when it is a local path: starts with a single `/`, no second `/` or `\` after it, no scheme. The dev front-end origin comes from configuration, not a literal in the endpoint. |
| BE-23 | SSRF guard misses IPv6, mapped addresses, 0.0.0.0, 100.64/10; redirects unchecked | Every resolved address is checked (IPv4 and IPv6, IPv4-mapped IPv6, unspecified, CGNAT, link-local, private, loopback) at connect time, so redirects and DNS rebinding are covered; auto-redirect off or each hop re-checked. |
| BE-28 (part) | Guest token accepted from the `access_token` query string on every route | Query-string token accepted only on `/hubs/*` (the SignalR client needs it); header and bearer forms unchanged. |

## Decisions (taken — owner or architect)

| Decision | Why | Rejected alternative | Confidence |
|---|---|---|---|
| **Soft kick** (owner, 2026-10-22): add a removed marker to `Participant` (the `Kick()` behaviour sets it and raises the existing event); a removed participant's token fails authentication; a removed Google participant cannot rejoin the same sprint (409); a guest can join again only as a new participant. | Keeps the team's retro content; matches "retire by marking, never delete". | Delete their votes and cards; refuse the kick while they have content. | high — owner's pick |
| Migration repairs data before the unique indexes: duplicate votes keep the lowest id; duplicate active display names in one sprint get ` (2)`, ` (3)`… appended. | An index creation over existing duplicates fails on a deployed database. | Fail the migration and ask the operator to clean up. | medium — rewrites data on a live database; deterministic and minimal |
| A fix that changes behaviour gets its regression case **in the existing test class for that area** (kick → `ParticipantEndpointTests`, closed sprint → the relevant endpoint tests, redaction → `CardEndpointTests`, error mapping → an existing endpoint test). No new test project, framework or helper library. | Owner's scope rule allows tests "we have already"; an untested security fix is not a fix. | No new test cases at all. | medium — reading of the owner's "no tests unless we have them already" |
| Domain rule violations map to 409 now; the full one-error-shape convention is F38's. | Keep F35 a defect fix; F38 unifies idioms. | Do the whole error convention here. | high |

## Out of scope

- Every conventions/structure finding (BE-03…06, BE-08…11, BE-16…18, BE-24…38 except the parts named above, SK-02, SK-06, SK-08, SK-10, SK-12, SK-13, SK-15, SK-16, SK-18…SK-30) — F38.
- The login allowlist: **not** enforced in code (owner, 2026-10-13); the docs are corrected in F40.
- Frontend changes, except where a fix changes a payload the SPA reads — then the one consumer is updated in the same step and named in `implementation.md`.

## What must not change

- Routes, request and response field names, status codes of every path not named above.
- Writing-phase behaviour for the author and for the team owner.
- Existing tests keep passing; a test that asserted a defect (e.g. a 500) is updated and named in `implementation.md`.
- No package version changes (F37 owns them).

## Acceptance (checkable)

1. `grep -n "AddSingleton<DomainEventPublishingInterceptor>" -r src` → no hits.
2. `grep -rn "InvalidOperationException" src/Services/Reflekt/Reflekt.Domain` → no hits (state-rule violations use `DomainException`).
3. Middleware: a 5xx body contains no `exception.Message` (inspect `GlobalExceptionMiddleware`); an integration test asserts a domain-rule violation returns 409.
4. Integration tests (existing classes) cover: kick of a participant with a card and a vote → 204/200, their card still listed, their token → 401; closed sprint → 409 on a group endpoint and `UpdateColumns`; a Writing-phase card broadcast to a non-author carries an empty text (or the redaction function's unit test, if broadcast payloads are not reachable from the existing harness — the plan decides and says which).
5. A new migration adds the two unique indexes and the participant removed marker; `dotnet ef migrations has-pending-model-changes` → none after it.
6. `LoginEndpoint` rejects `/\evil.com` and `//evil.com` (test in the existing auth test class).
7. The declared full role in `.claude/verify.json` passes.
