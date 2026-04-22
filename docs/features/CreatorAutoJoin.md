# Creator Auto-Join

**Traces to:** `docs/backlog.md` item #19
**Dependencies:** Auth, Participants, Sprints
**Status:** Ready

---

## Purpose

When the sprint creator (Scrum Master) opens the retro room, they should not have to pick a display name — they are the first participant and known to the system via Google OAuth. The system auto-joins them using their Google given name. Additionally, the name-picker for later joiners is enhanced to show who is already in the room and to reject duplicate display names.

## Changes to Existing Entities

### Participant (aggregate root) — no schema change

No new fields. The `DisplayName` uniqueness constraint within a sprint is enforced at the endpoint level (not the domain or DB), consistent with how `VotesPerParticipant` budget is checked at the handler level.

## User Flows

```
Flow 1: Creator auto-join (SM opens retro room, no guest token)
1. SM navigates to /sprint/{id}
2. Frontend checks localStorage — no guest token for this sprint
3. Frontend checks auth store — user is signed in (cookie session)
4. Frontend calls /auth/me to get the user's given name
5. Frontend calls POST /api/sprints/{sprintId}/join with DisplayName = givenName (automatic, no prompt)
6. If 201: token saved, room entered — no name-picker shown
7. If 409 (name taken): fall through to name-picker with "Name already taken" error pre-populated
```

```
Flow 2: Creator re-enters (SM returns after someone took their name)
1. SM navigates to /sprint/{id}
2. Frontend checks localStorage — no guest token (or stale token cleared)
3. Frontend checks auth store — user is signed in
4. Auto-join attempted with given name → 409 (duplicate)
5. Name-picker shown with error: "{givenName} is already taken. Choose another."
6. SM picks a different name, joins normally
```

```
Flow 3: Later joiner sees participants and duplicate rejected
1. User navigates to /sprint/{id}
2. No guest token, not signed in (or signed in but already attempted auto-join)
3. Name-picker shown with the list of already-joined participants above the form
4. User enters a name that matches an existing participant → 409
5. UI shows "Name already taken. Choose another." inline under the input
6. User picks a different name, joins successfully
```

```
Flow 4: SM has existing guest token (normal rejoin)
1. SM navigates to /sprint/{id}
2. Frontend finds guest token in localStorage
3. fetchMe succeeds — enters room directly (no auto-join attempted)
4. Same as existing Flow 2 in Participants spec
```

## API Surface Changes

### Modified: POST /api/sprints/{sprintId}/join

| Aspect | Before | After |
|--------|--------|-------|
| Duplicate DisplayName | Allowed (no check) | Returns 409 with message "Name already taken" when DisplayName matches existing participant in the sprint (case-insensitive) |
| Response on conflict | N/A | 409 with `{ "statusCode": 409, "message": "Name already taken." }` |

All other behavior (400 validation, 404 sprint not found, 409 sprint not active) remains unchanged.

### Modified: GET /auth/me

| Aspect | Before | After |
|--------|--------|-------|
| Response | `{ email, displayName }` | `{ email, displayName, givenName }` |

`givenName` is read from `ClaimTypes.GivenName` in the Google cookie claims. Falls back to `displayName` (ClaimTypes.Name) if GivenName claim is absent.

### Existing (no changes): GET /api/sprints/{sprintId}/participants

Already returns the participant list. Frontend will call this on the name-picker screen for later joiners.

## SignalR Events

No new events. The existing `ParticipantJoined` event fires normally when the auto-join succeeds.

## Business Rules

1. **Duplicate display name rejection:** When `JoinSprint` is called, if any existing `Participant` in the same sprint has a `DisplayName` that matches (case-insensitive), return 409 with "Name already taken." This is a new constraint — previously duplicates were allowed.
2. **Auto-join is frontend-only logic.** The backend does not know or care whether the join was automatic or manual. The same `JoinSprint` endpoint is used.
3. **Auto-join uses `givenName` from `/auth/me`.** This is the Google given_name (first name), not the full display name.
4. **Auto-join fallback:** If auto-join returns 409, the frontend shows the name-picker with the error pre-populated. The SM is not stuck.
5. **Guest token takes precedence.** If a guest token exists in localStorage, the normal silent-rejoin flow runs. Auto-join is only attempted when there is no token AND the user is signed in.
6. **Participant list on name-picker:** Later joiners (and SM in fallback) see who is already in the sprint before choosing a name. This uses the existing `GET /api/sprints/{sprintId}/participants` endpoint, called without auth (the endpoint currently requires guest token or SM cookie — the SM case is covered by cookie; for anonymous joiners, the endpoint needs to allow anonymous access OR a new unauthenticated endpoint is needed). Decision: make the participant list available on the unauthenticated sprint summary endpoint instead — add a `participants` array to `GET /api/sprints/{sprintId}/summary`.
7. **Case-insensitive comparison:** "Alice" and "alice" are treated as the same name for duplicate checking.

## Acceptance Criteria

- [ ] `GET /auth/me` returns `givenName` field alongside existing `email` and `displayName`
- [ ] `givenName` falls back to `displayName` when `ClaimTypes.GivenName` is absent
- [ ] `POST /api/sprints/{sprintId}/join` with a display name matching an existing participant (case-insensitive) returns 409
- [ ] 409 response body contains a clear "Name already taken" message
- [ ] Non-duplicate names still join successfully (201)
- [ ] SM opening `/sprint/{id}` with no guest token and signed-in cookie auto-joins with `givenName` — no name-picker shown
- [ ] If auto-join gets 409, name-picker is shown with error message pre-populated
- [ ] If SM has a valid guest token in localStorage, normal silent rejoin occurs (auto-join not attempted)
- [ ] Name-picker for all joiners shows the list of already-joined participants above the form
- [ ] `GET /api/sprints/{sprintId}/summary` includes a `participants` array with `{ displayName }` entries
- [ ] ParticipantJoined SignalR event fires normally on auto-join
- [ ] Build passes with zero errors

## Out of Scope

- **Server-side auto-join logic** — the backend does not auto-create participants. The frontend orchestrates auto-join by calling the existing endpoint.
- **Reserving the creator's name** — if someone joins with the SM's given_name before the SM enters, the SM falls back to the name-picker. No pre-reservation.
- **Changing existing participant display names** — still set-once in v1.
- **Full display name (family name)** — auto-join uses given_name only, per the backlog item.
