# Participants

**Traces to:** `docs/specs/v1.md` §3.2 (sprint participants), §3.3 (ownership semantics), §4 (Participant entity), §7.1 (SignalR events)
**Dependencies:** Auth, Teams, Sprints
**Status:** Complete

---

## Purpose

Participants are ephemeral guests who join a sprint to contribute to the retro — writing cards, voting, and discussing. They have no accounts; identity is tied to a server-generated guest token scoped to a single sprint, stored in the browser's localStorage. This feature enables the core multi-user retro experience that every downstream feature (cards, votes, reports) depends on.

## Entities

### Participant (aggregate root)

| Field | Type | Constraints |
|-------|------|-------------|
| Id | int | Auto-increment primary key |
| SprintId | int | Required, references Sprint |
| DisplayName | string | Required, max 50 chars |
| GuestToken | string | Required, unique, server-generated GUID |
| JoinedAt | DateTime | UTC, stamped at creation |

A Participant belongs to one Sprint. A Sprint can have many Participants.

Participants are created through a guarded factory that enforces required fields, stamps JoinedAt, generates the guest token, and raises a domain event.

No uniqueness constraint on DisplayName within a sprint — two participants can share the same name.

GuestToken is unique across all participants (globally unique GUID).

**Domain events:**
- ParticipantJoined — raised when a participant joins a sprint
- ParticipantLeft — raised when a participant disconnects (SignalR presence signal, not a deletion — the row persists)

## User Flows

```
Flow 1: Join a sprint (new participant)
1. User navigates to the sprint room link
2. Frontend checks localStorage for an existing guest token for this sprint
3. No token found — user sees a name picker prompt
4. User enters a display name and submits
5. Server verifies the sprint exists and is in Active state
6. Server creates a Participant, generates a guest token, returns both
7. Frontend stores the token in localStorage under the sprint-scoped key
8. Frontend establishes a SignalR connection with the token
9. All connected clients in the sprint receive a real-time ParticipantJoined notification
```

```
Flow 2: Silent rejoin (returning participant)
1. User navigates to the sprint room link (refresh, tab reopen, etc.)
2. Frontend checks localStorage — finds an existing guest token for this sprint
3. Frontend calls the lookup endpoint with the token
4. Server matches token to an existing Participant, returns participant info
5. Frontend reconnects to SignalR — no name prompt, no new participant created
```

```
Flow 3: Rejoin fails (stale token)
1. User navigates to the sprint room link with a stale or invalid token in localStorage
2. Frontend calls the lookup endpoint — server returns 401 (no matching participant)
3. Frontend clears the stale token from localStorage
4. User sees the name picker prompt (falls through to Flow 1)
```

```
Flow 4: View participants
1. Any authenticated participant or the Scrum Master requests the participant list
2. Server returns all participants in the sprint ordered by join time
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | `/api/sprints/{sprintId}/join` | None (public) | `{ displayName }` | `{ id, sprintId, displayName, guestToken, joinedAt }` | 201, 400, 404, 409 |
| GET | `/api/sprints/{sprintId}/participants/me` | Guest token | — | `{ id, sprintId, displayName, joinedAt }` | 200, 401, 404 |
| GET | `/api/sprints/{sprintId}/participants` | Guest token or SM cookie | — | `[{ id, displayName, joinedAt }]` | 200, 401, 404 |

**Route rationale:** Participant endpoints use flat `/api/sprints/{sprintId}/...` routes (not nested under `/api/teams/{teamId}/...`) because participants enter via a room link and only know the sprint ID. SM-facing endpoints remain nested under teams.

Error responses:
- **JoinSprint with empty/missing displayName:** 400
- **JoinSprint with displayName exceeding 50 chars:** 400
- **JoinSprint targeting a nonexistent sprint:** 404
- **JoinSprint targeting a Draft or Closed sprint:** 409 — only Active sprints accept new participants
- **GetCurrentParticipant without a valid guest token:** 401
- **GetCurrentParticipant with a token that doesn't belong to this sprint:** 401
- **GetParticipants without valid auth (guest token or SM cookie):** 401
- **GetParticipants targeting a nonexistent sprint:** 404

## SignalR Events

| Event | Payload | Trigger |
|-------|---------|---------|
| ParticipantJoined | `{ id, displayName }` | New participant joins via the join endpoint |
| ParticipantLeft | `{ participantId }` | Participant's SignalR connection drops |

## Business Rules

1. Only Active sprints accept new participants. Draft sprints are not yet live; Closed sprints are read-only.
2. The guest token is a server-generated GUID, returned in the join response. The client stores it in localStorage under `reflekt:sprint:{sprintId}:token` and sends it as an `X-Guest-Token` header on every subsequent API call and SignalR connection. **Note:** the business spec (§3.2) says "generated client-side" — server-generated is preferred because it prevents token forgery/collision and follows standard auth patterns, with no downside for v1's flow.
3. A guest token is scoped to one sprint. A participant joining multiple sprints gets a separate token and Participant record per sprint.
4. Silent rejoin: if a valid token exists in localStorage on page load, the frontend calls the lookup endpoint and resumes as the existing participant without prompting for a name.
5. Switching devices mid-retro creates a new participant — the token lives in the browser, not the person. Acceptable tradeoff for v1.
6. Display name is required, max 50 characters. Display names must be unique within a sprint (case-insensitive comparison). Duplicate names are rejected with 409. See `docs/features/CreatorAutoJoin.md`.
7. Participant rows are permanent within their sprint. Disconnecting fires a presence event but does not delete the row. The participant can rejoin at any time with the same token.
8. The Scrum Master participates in the retro by also joining as a participant. Their SM powers (via cookie auth) and their participant identity (via guest token) are independent.
9. The participant list is visible to any authenticated participant in the sprint and to the Scrum Master.
10. Every Card and Vote created in downstream features references a ParticipantId, establishing ownership per spec §3.3.
11. Email comparison for Scrum Master identification is case-insensitive.

## Acceptance Criteria

- [ ] `POST /api/sprints/{sprintId}/join` with a valid display name creates a participant, returns 201 with participant data including guest token
- [ ] Join generates a unique server-side guest token in GUID format
- [ ] Join with empty/missing displayName returns 400
- [ ] Join with displayName exceeding 50 characters returns 400
- [ ] Join targeting a Draft sprint returns 409
- [ ] Join targeting a Closed sprint returns 409
- [ ] Join targeting a nonexistent sprint returns 404
- [ ] `GET /api/sprints/{sprintId}/participants/me` with a valid guest token returns the participant, 200
- [ ] GetCurrentParticipant without a guest token returns 401
- [ ] GetCurrentParticipant with a token not matching this sprint returns 401
- [ ] `GET /api/sprints/{sprintId}/participants` returns all participants in the sprint
- [ ] GetParticipants requires valid auth (guest token or SM cookie), returns 401 otherwise
- [ ] Guest token authentication resolves the `X-Guest-Token` header to a valid participant identity with participant ID and sprint ID claims
- [ ] SignalR hub rejects group join if the participant does not belong to the target sprint
- [ ] ParticipantJoined SignalR event fires to sprint group when a new participant joins
- [ ] ParticipantLeft SignalR event fires to sprint group when a participant's connection drops
- [ ] Existing participants can rejoin via lookup without creating a duplicate
- [ ] Domain event raised: ParticipantJoined on join
- [ ] Build passes with zero errors

## Out of Scope

- **~~Participant deletion or kick~~** — Implemented. SM can kick participants from the sprint management page. See backlog item #18.
- **Display name editing after join** — name is set once. Changing requires creating a new participant (new token, new row).
- **Online/offline presence indicators** — ParticipantLeft fires on disconnect but the frontend does not display real-time connection status. The participant list shows everyone who has joined, not who is currently connected.
- **Cross-sprint identity linking** — each sprint has independent participants. No "same person across sprints" concept in v1.
- **Rate limiting on join** — not needed for v1's single-team trust model.
- **Room link security (slugs, invite codes)** — sprint IDs are sequential integers. Anyone with the ID can join an Active sprint. Acceptable for internal team use.
