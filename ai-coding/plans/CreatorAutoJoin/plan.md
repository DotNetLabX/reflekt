# Creator Auto-Join

## Context

The sprint creator (Scrum Master) currently has to manually pick a display name when entering the retro room, even though they are already authenticated via Google OAuth. This feature auto-joins the creator using their Google given name and enhances the name-picker with duplicate detection and a participant list.

## Scope

**In scope:**
- Expose `givenName` from `/auth/me`
- Duplicate display name check in `JoinSprint` endpoint (case-insensitive, 409 on conflict)
- Add participants to the sprint summary response (unauthenticated)
- Frontend: auto-join logic for signed-in SM with no guest token
- Frontend: show participant list on name-picker
- Frontend: handle 409 duplicate name inline error

**Out of scope:**
- Server-side auto-join (backend stays stateless about who the creator is)
- Name reservation for the creator
- Display name editing after join

## Domain Model Changes

None. No new aggregates, entities, value objects, or domain events.

## Data Model Changes

None. No schema changes, no migrations.

## Implementation Steps

### Step 1: Add `givenName` to `/auth/me` response

**What:** Extend the `MeResponse` record to include `GivenName`. Read it from `ClaimTypes.GivenName` in the Google cookie claims. Fall back to `ClaimTypes.Name` if absent.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Auth/Me/MeEndpoint.cs` — add `GivenName` to `MeResponse` record, read `ClaimTypes.GivenName` claim with fallback

**Pattern:** Follow existing `MeEndpoint.cs` — same structure, one more claim read.

### Step 2: Add duplicate display name check to `JoinSprint`

**What:** Before creating the participant, query for an existing participant in the same sprint with a matching display name (case-insensitive). If found, return 409 with "Name already taken."

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` — add `ExistsByDisplayNameAsync(int sprintId, string displayName, CancellationToken ct)` method. Use case-insensitive comparison via `EF.Functions.Collate` or `StringComparison.OrdinalIgnoreCase` (SQLite is case-insensitive by default for ASCII with `NOCASE`, but use `string.Equals` with `OrdinalIgnoreCase` in the LINQ query to be explicit — EF Core translates this to a SQL `LIKE` or `=` that SQLite handles case-insensitively for ASCII).
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/JoinSprintEndpoint.cs` — after sprint validation, before creating the participant, call `ExistsByDisplayNameAsync`. If true, return 409 with "Name already taken."

**Pattern:** Follow existing `JoinSprintEndpoint.cs` for error handling — use `ThrowError("Name already taken.", StatusCodes.Status409Conflict)`.

### Step 3: Add participants to sprint summary response

**What:** The name-picker needs to show already-joined participants, but the joiner has no auth token yet. Extend the unauthenticated `GET /api/sprints/{sprintId}/summary` to include a participants array.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` — add a `Participants` property (list of display names) to `SprintSummaryResponse`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` — inject `ParticipantRepository`, fetch participants for the sprint, map to display names, include in response

**Pattern:** Follow existing `GetSprintSummaryEndpoint.cs` — add one more repository dependency, one more query.

### Step 4: Frontend — update auth store with `givenName`

**What:** The auth store's `User` interface and `fetchMe` action need to expose `givenName`.

**Files to modify:**
- `web/reflekt-web/src/stores/auth.ts` — add `givenName` to the `User` interface

**Pattern:** Follow existing `auth.ts` — same shape, one more field.

### Step 5: Frontend — update sprint store to carry participant list from summary

**What:** The sprint store's `SprintSummary` interface needs to include the participant names from the summary endpoint, so the name-picker can display them.

**Files to modify:**
- `web/reflekt-web/src/stores/sprint.ts` — add `participants: { displayName: string }[]` to `SprintSummary` interface (or equivalent shape matching the API response)

**Pattern:** Follow existing `sprint.ts` — same fetch action, updated type.

### Step 6: Frontend — auto-join logic and name-picker enhancements in RetroRoomView

**What:** This is the main frontend change. Modify `RetroRoomView.vue` to:

1. **Auto-join for SM:** In `onMounted`, after checking for a guest token and finding none, check if the user is signed in (`authStore.isSignedIn`). If yes, attempt `participantStore.join(sprintId, authStore.user.givenName)`. On success, save token and enter room. On 409, fall through to name-picker with error message pre-populated.
2. **Participant list on name-picker:** Before showing the name-picker, fetch the sprint summary (already done) which now includes participants. Display the list of already-joined participants above the form.
3. **409 handling in manual join:** Update `handleJoin` to detect 409 and show "Name already taken. Choose another." inline.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue` — modify `onMounted` for auto-join logic, add participant list UI to name-picker section, update 409 error handling in `handleJoin`

**Pattern:** Follow existing `RetroRoomView.vue` — same store usage, same error handling style.

**Detailed logic for onMounted:**
```
onMounted:
  1. policyStore.fetch()
  2. sprintStore.fetchSprintSummary(sprintId)
  3. guestStore.loadToken(sprintId)
  4. IF token exists:
       try fetchMe → enterRoom (existing flow)
       catch 401 → clearToken, fall through
  5. IF no token AND authStore.isSignedIn AND authStore.user.givenName:
       try join(sprintId, givenName)
         → saveToken, enterRoom
       catch 409 → set errorMessage = "{givenName} is already taken. Choose another."
       catch other → set errorMessage = generic error
  6. Show name-picker (reached if no token, not signed in, or auto-join failed)
```

**Name-picker enhancements:**
- Above the form, show "Already in this retro:" followed by participant chips (from `sprintStore.currentSummary.participants`)
- Only show the list if there are participants
- 409 error in `handleJoin` shows "Name already taken. Choose another." instead of the current generic "Sprint is not accepting participants."

## Cross-Service Changes

None. Single service.

## Migration Notes

None. No schema changes.

## Testing Strategy

**Backend:**
- `POST /api/sprints/{sprintId}/join` with duplicate name returns 409
- `POST /api/sprints/{sprintId}/join` with unique name returns 201 (regression)
- Case-insensitive duplicate: "Alice" then "alice" returns 409
- `GET /auth/me` includes `givenName` field
- `GET /api/sprints/{sprintId}/summary` includes `participants` array

**Frontend (manual):**
- SM opens retro room with no guest token → auto-joined, no name-picker
- SM opens retro room but name is taken → name-picker with error
- SM opens retro room with existing token → silent rejoin (no auto-join)
- Later joiner sees participant list on name-picker
- Later joiner submits duplicate name → inline error
- Later joiner submits unique name → joins successfully

## Open Questions

None. The collision fallback decision (show name-picker with error) is documented in the backlog and confirmed in the spec.
