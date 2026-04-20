# Teams

**Traces to:** `docs/specs/v1.md` §3.1 (team creator identity), §4 (Team entity), §12.6 (email allowlist)
**Dependencies:** Auth
**Status:** Ready

---

## Purpose

A Team is the tenancy root of Reflekt — sprints, participants, cards, votes, and evidence links all hang off a single team. V1 scopes teams tightly: the Scrum Master creates one team under their Google account, views only their own teams, and owns every sprint underneath. There is no membership model, no invite flow, and no cross-user discovery.

## Entities

### Team (aggregate root)

| Field | Type | Constraints |
|-------|------|-------------|
| Id | int | Auto-increment primary key. Spec §4 shows `Guid`; the v1 implementation uses `int` for SQLite ergonomics. |
| Name | string | Required, max 100 chars |
| CreatorEmail | string | Required, max 256 chars, **unique index** |
| CreatedAt | DateTime | UTC; stamped by the factory |

Teams are constructed only through the `Team.Create(name, creatorEmail)` static factory, which guards against null/whitespace input, sets `CreatedAt = DateTime.UtcNow`, and raises a domain event. The parameterless constructor is private to block direct instantiation.

**Domain events:**
- `TeamCreatedEvent(Team)` (implements `IDomainEvent`) — raised inside `Team.Create`. No subscribers in v1; the event is available for future features.

## User Flows

```
Flow 1: Create a team
1. Cookie-authenticated user POSTs /api/teams with { name }
2. CreateTeamCommandValidator rejects empty name or name > 100 chars (400)
3. Handler reads the caller's email from the cookie claims
4. Handler checks the email against Auth:AllowedEmails (403 if not allowlisted)
5. Handler checks TeamRepository.ExistsByCreatorEmailAsync (409 if the caller already owns a team)
6. Team.Create builds the aggregate and raises TeamCreatedEvent
7. TeamRepository.AddAsync + SaveChangesAsync persist the team
8. Response: 201 with { id, name, createdAt }
```

```
Flow 2: List my teams
1. Cookie-authenticated user GETs /api/teams
2. Handler reads the caller's email from the cookie claims
3. TeamRepository.GetByCreatorEmailAsync returns teams matching the email (case-insensitive)
4. Handler projects each Team into { id, name, createdAt }
5. Response: 200 with the list (0 or 1 element in v1)
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | /api/teams | ScrumMaster policy | `{ name: string }` | `{ id, name, createdAt }` | 201 Created, 400 Validation, 401 Unauthorized, 403 Not allowlisted, 409 Caller already owns a team |
| GET | /api/teams | ScrumMaster policy | — | `[ { id, name, createdAt } ]` | 200 OK, 401 Unauthorized |

## Business Rules

1. Both endpoints are gated by the `ScrumMaster` policy — cookie-authenticated identity required. Anonymous callers get 401.
2. `CreateTeam` re-checks `Auth:AllowedEmails` inside the handler, on top of the OAuth-callback gate in Auth. Non-allowlisted callers get 403; no team row is created.
3. One team per creator email. Enforced twice: a pre-insert `ExistsByCreatorEmailAsync` probe (returns 409 with `"You already have a team"`) and a unique DB index on `CreatorEmail` as a concurrency backstop.
4. Email matching is **case-insensitive** wherever `CreatorEmail` is compared — the allowlist check uses `StringComparer.OrdinalIgnoreCase`; EF queries use SQLite `NOCASE` collation.
5. `Name` is required and ≤ 100 chars. Enforced by `CreateTeamCommandValidator` (FluentValidation, auto-discovered and run pre-handler by FastEndpoints).
6. `Team` instances must go through `Team.Create`. The private parameterless constructor and `required init` properties make direct construction impractical; the factory is the only path that raises `TeamCreatedEvent`.
7. `GET /api/teams` is scoped to the caller's email. There is no endpoint to list other users' teams.

## Acceptance Criteria

- [x] Allowlisted cookie-authenticated user can create a team with a valid name → 201 with `{ id, name, createdAt }`
- [x] Anonymous request to either endpoint → 401
- [x] Cookie-authenticated but non-allowlisted user → 403 on POST, no row inserted
- [x] Empty or over-length name → 400 from validator
- [x] Duplicate creation by the same email → 409 with `"You already have a team"`
- [x] Duplicate check is case-insensitive (`Foo@x.com` cannot create a second team as `foo@x.com`)
- [x] `GET /api/teams` returns only teams whose `CreatorEmail` matches the caller, matched case-insensitively
- [x] `TeamCreatedEvent` is raised on every successful creation
- [x] `Teams` table exists with a unique index on `CreatorEmail` (migration `AddTeam`)
- [x] Build passes with zero errors

## Out of Scope

- **Team renaming / editing** — `Name` is immutable in v1; no `PATCH /api/teams/{id}`.
- **Team deletion** — not in v1. Teams persist indefinitely alongside their sprints.
- **Multiple teams per creator** — one-team-per-email is a deliberate v1 constraint; avoids a team-selector UI.
- **Member management / invites / multi-admin** — spec §3.1 is explicit: v1 is single-creator, no roles beyond Scrum Master.
- **Cross-user team discovery** — no public listing endpoint; the caller sees only their own teams.
- **Team-level settings (votes-per-participant default, retro template, etc.)** — configuration lives on the `Sprint`, not the `Team`, in v1.
- **Domain-event handlers** — `TeamCreatedEvent` has no subscriber in v1. Future features can subscribe via `IEventHandler<TeamCreatedEvent>` in their own slice.
