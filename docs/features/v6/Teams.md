# Teams

**Traces to:** `docs/specs/v1.md` §3.1, §4 (Team entity), §12.6
**Dependencies:** Auth (Google OAuth, ScrumMaster policy, AllowlistChecker)
**Status:** Ready

---

## Purpose

Teams are the top-level organizational unit in Reflekt. A Google-authenticated user creates a team and becomes its Scrum Master — the only identity that can create sprints, advance phases, and close retros under that team. v1 supports one team per creator, no invites, no multi-admin. See v1 spec §3.1.

## Entities

### Team (aggregate root)

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | PK, auto-increment | From AggregateRoot base |
| Name | string | Required, max 100 | |
| CreatorEmail | string | Required, max 256, unique index | Google-authed email |
| CreatedAt | DateTime | Required | UTC |

- Static factory method `Team.Create(name, creatorEmail)` — guards on null/whitespace, raises `TeamCreatedEvent`.
- `TeamCreatedEvent(Team)` carries the aggregate reference per CLAUDE.md domain event convention.

## User Flows

```
Flow 1: Create a team
1. Signed-in user submits team name via POST /api/teams
2. Server verifies email is on the allowlist (defense-in-depth — primary gate is at OAuth callback)
3. Server checks no existing team for this CreatorEmail
4a. No existing team → Team.Create(), persist, return 201 with team data
4b. Existing team → 409 "You already have a team"
```

```
Flow 2: View my teams
1. Signed-in user calls GET /api/teams
2. Server queries teams where CreatorEmail matches the caller's email (case-insensitive)
3. Returns list of team summaries (id, name, createdAt)
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | `/api/teams` | ScrumMaster | `{ name }` | `{ id, name, createdAt }` | 201, 400, 401, 403, 409 |
| GET | `/api/teams` | ScrumMaster | — | `[{ id, name, createdAt }]` | 200, 401 |

Error responses:
- **CreateTeam with empty/missing name:** 400 (FluentValidation — name required, max 100 chars)
- **CreateTeam when caller already has a team:** 409 "You already have a team"
- **CreateTeam with non-allowlisted email:** 403 (defense-in-depth)
- **Any endpoint without valid cookie:** 401

## Business Rules

1. One team per creator email — enforced by unique index on `CreatorEmail` and explicit `ExistsByCreatorEmail` check before creation.
2. Team creation requires a Google-authenticated session (ScrumMaster policy).
3. Allowlist check in CreateTeam is defense-in-depth — the primary allowlist gate is at OAuth sign-in (Auth feature).
4. Team name is required, max 100 characters.
5. `GetMyTeams` returns only teams where `CreatorEmail` matches the caller — case-insensitive comparison (SQLite NOCASE collation).
6. The team creator is implicitly the Scrum Master for all sprints under that team. No separate role assignment in v1.
7. `TeamCreatedEvent` is raised on creation. No consumer in v1 — the event exists for forward compatibility.

## Acceptance Criteria

- [ ] `POST /api/teams` with valid name creates a team and returns 201
- [ ] `POST /api/teams` with empty name returns 400
- [ ] `POST /api/teams` when caller already has a team returns 409
- [ ] `POST /api/teams` with non-allowlisted email returns 403
- [ ] `POST /api/teams` without cookie returns 401
- [ ] `GET /api/teams` returns teams created by the calling user
- [ ] `GET /api/teams` email comparison is case-insensitive
- [ ] `GET /api/teams` without cookie returns 401
- [ ] `Team.Create()` guards against null/whitespace name and email
- [ ] `Team.Create()` raises `TeamCreatedEvent`
- [ ] `CreatorEmail` has a unique index in the database
- [ ] EF migration exists and applies cleanly

## Out of Scope

- **Team update / rename** — not needed for v1 one-team model
- **Team delete** — not needed for v1
- **Team invite flow / multi-admin** — v2 (spec §3.1)
- **Role hierarchy** — team creator = SM, no separation in v1
- **Team-level settings** (e.g., default votes per participant) — deferred
- **Sprint CRUD under a team** — owned by the Sprints feature
