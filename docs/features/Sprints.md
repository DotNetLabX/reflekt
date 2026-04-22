# Sprints

**Traces to:** `docs/specs/v1.md` §4 (Sprint entity), §5.2 (phases), §9 (sprint lifecycle)
**Dependencies:** Auth, Teams
**Status:** Complete

---

## Purpose

A Sprint is the time-boxed retro session that participants join, write cards in, vote on, and discuss. The Scrum Master creates sprints under their team, starts them (activating the room link), advances through retro phases, and closes them (making them read-only). Every downstream feature — participants, cards, votes, evidence links, reports — hangs off a sprint.

## Entities

### Sprint (aggregate root)

| Field | Type | Constraints |
|-------|------|-------------|
| Id | int | Auto-increment primary key |
| TeamId | int | Required, FK to Team |
| Name | string | Required, max 100 chars |
| State | enum | Draft, Active, Closed |
| CurrentPhase | enum | Writing, Voting, Discussing, Reporting |
| VotesPerParticipant | int | Required, default 3, minimum 1 |
| CreatedAt | DateTime | UTC, stamped at creation |
| ClosedAt | DateTime? | UTC, stamped when State transitions to Closed |

A Sprint belongs to one Team. A Team can have many Sprints.

Sprints are created only through a guarded factory that enforces required fields, sets initial state to Draft and initial phase to Writing, and raises a domain event.

**State machine:** Draft → Active → Closed. Forward-only, no reopen.

**Phase machine:** Writing → Voting → Discussing → Reporting. Forward-only, one step at a time, no skipping or going back.

State and phase are independent concepts — reaching phase Reporting does not auto-close the sprint state, and closing the sprint does not advance the phase. The sprint state is what enforces read-only; the phase enforces which actions are permitted (see `docs/features/PhaseActionGating.md`).

**Domain events:**
- SprintCreated — raised when a sprint is created
- SprintStarted — raised when a sprint transitions from Draft to Active
- PhaseAdvanced — raised when the current phase moves forward one step
- SprintClosed — raised when a sprint transitions from Active to Closed

## User Flows

```
Flow 1: Create a sprint
1. SM submits a sprint name (and optionally votes-per-participant) for their team
2. Server verifies caller is the team's creator
3. Sprint is created in Draft state, Writing phase
4. Returns the new sprint
```

```
Flow 2: Start a sprint
1. SM starts a draft sprint
2. Server verifies sprint is in Draft state
3. State transitions to Active — room link is now live
4. Returns updated sprint
```

```
Flow 3: Advance phase
1. SM advances the retro to the next phase
2. Server verifies sprint is Active and current phase is not already Reporting
3. Phase transitions forward one step (Writing → Voting → Discussing → Reporting)
4. All connected clients in the sprint receive a real-time phase change notification
5. Returns the new phase
```

```
Flow 4: Close a sprint
1. SM closes the sprint
2. Server verifies sprint is in Active state
3. State transitions to Closed, ClosedAt is stamped
4. Sprint becomes read-only — no new cards, edits, votes, or phase changes
5. Returns updated sprint
```

```
Flow 5: View a sprint
1. SM or authenticated participant requests sprint details
2. Returns full sprint data (state, phase, vote budget, timestamps)
```

```
Flow 6: List team sprints
1. SM requests all sprints for their team
2. Returns a list of sprint summaries ordered by creation date descending
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | `/api/teams/{teamId}/sprints` | SM (team owner) | `{ name, votesPerParticipant? }` | `{ id, teamId, name, state, currentPhase, votesPerParticipant, createdAt }` | 201, 400, 401, 403 |
| POST | `/api/teams/{teamId}/sprints/{sprintId}/start` | SM (team owner) | — | `{ id, state }` | 200, 401, 403, 404, 409 |
| POST | `/api/teams/{teamId}/sprints/{sprintId}/advance-phase` | SM (team owner) | — | `{ id, currentPhase }` | 200, 401, 403, 404, 409 |
| POST | `/api/teams/{teamId}/sprints/{sprintId}/close` | SM (team owner) | — | `{ id, state, closedAt }` | 200, 401, 403, 404, 409 |
| GET | `/api/teams/{teamId}/sprints/{sprintId}` | SM or participant | — | full sprint object | 200, 401, 403, 404 |
| GET | `/api/teams/{teamId}/sprints` | SM (team owner) | — | `[{ id, name, state, currentPhase, createdAt, closedAt }]` | 200, 401 |

Error responses:
- **CreateSprint with empty/missing name:** 400
- **CreateSprint with votesPerParticipant < 1:** 400
- **CreateSprint by non-team-owner:** 403
- **StartSprint on non-Draft sprint:** 409
- **AdvancePhase on non-Active sprint or already at phase Reporting:** 409
- **CloseSprint on non-Active sprint:** 409
- **Any mutation on a Closed sprint:** 409
- **Sprint not found:** 404
- **Any endpoint without valid auth:** 401

## SignalR Events

| Event | Payload | Trigger |
|-------|---------|---------|
| PhaseChanged | New phase value | SM advances phase via AdvancePhase endpoint |

## Business Rules

1. Only the team creator (Scrum Master) can create, start, advance phase, or close sprints under their team.
2. Sprint state transitions are strictly forward: Draft → Active → Closed. No reopen, no backward transitions.
3. Phase transitions are strictly forward: Writing → Voting → Discussing → Reporting. No going back, no skipping.
4. Phases are enforced. See `docs/features/PhaseActionGating.md` for the action matrix.
5. A closed sprint is read-only. Mutation endpoints reject requests against closed sprints with 409.
6. Sprint state and phase are independent. Reaching phase Closed does not auto-close the sprint. The SM must close the sprint explicitly.
7. ClosedAt is stamped when the sprint transitions to Closed. It is null for Draft and Active sprints.
8. VotesPerParticipant defaults to 3 if not specified at creation. Minimum value is 1.
9. Sprint name is required, max 100 characters.
10. GetSprint is accessible to the Scrum Master of the owning team or any authenticated participant in that sprint.
11. GetTeamSprints returns all sprints for the team, regardless of state (Draft, Active, and Closed all appear).

## Acceptance Criteria

- [ ] `POST /api/teams/{teamId}/sprints` creates a sprint in Draft state, Writing phase, returns 201
- [ ] CreateSprint defaults votesPerParticipant to 3 when not provided
- [ ] CreateSprint with missing/empty name returns 400
- [ ] CreateSprint with votesPerParticipant < 1 returns 400
- [ ] CreateSprint by non-team-owner returns 403
- [ ] CreateSprint without cookie returns 401
- [ ] StartSprint on Draft sprint transitions to Active, returns 200
- [ ] StartSprint on non-Draft sprint returns 409
- [ ] AdvancePhase moves Writing → Voting → Discussing → Reporting, one step per call
- [ ] AdvancePhase on phase Reporting returns 409
- [ ] AdvancePhase on non-Active sprint returns 409
- [ ] AdvancePhase triggers PhaseChanged SignalR event to all clients in the sprint
- [ ] CloseSprint on Active sprint transitions to Closed, stamps ClosedAt, returns 200
- [ ] CloseSprint on non-Active sprint returns 409
- [ ] Mutation endpoints reject requests against Closed sprints with 409
- [ ] GetSprint returns full sprint data for the team's SM
- [ ] GetSprint returns full sprint data for an authenticated participant in that sprint
- [ ] GetSprint for nonexistent sprint returns 404
- [ ] GetTeamSprints returns all sprints for the calling SM's team
- [ ] Domain events raised: SprintCreated on create, SprintStarted on start, PhaseAdvanced on advance, SprintClosed on close
- [ ] Build passes with zero errors

## Out of Scope

- **Create-and-start in one action** — spec §9 mentions this as optional. Separate endpoints for v1; frontend can call both in sequence if desired.
- **Sprint delete or archive** — closed sprints persist indefinitely in the team's list (spec §9).
- **Sprint editing after creation** — no renaming, no changing vote budget after creation in v1.
- **Auto-close on phase Reporting** — spec §9 is explicit: state and phase are independent. SM must close manually.
- **Sprint-scoped SignalR group membership** — group join/leave is owned by the Participants feature. The PhaseChanged handler pushes to the group; Participants wires up who's in it.
- **Participant CRUD** — owned by the Participants feature. Sprints only defines the container participants join.
