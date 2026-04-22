# CreatorAutoJoin — Implementation

## Files Created
- None

## Files Modified
- `src/Services/Reflekt/Reflekt.API/Features/Auth/Me/MeEndpoint.cs` — added `GivenName` to `MeResponse` record; reads `ClaimTypes.GivenName` with fallback to `ClaimTypes.Name`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` — added `ExistsByDisplayNameAsync` using `.ToLower()` comparison for case-insensitive SQLite check
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/JoinSprintEndpoint.cs` — added duplicate name check before participant creation; returns 409 with "Name already taken."
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` — added `SprintSummaryParticipant` record; extended `SprintSummaryResponse` with `CurrentPhase` and `Participants`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` — injected `ParticipantRepository`; fetches participants and maps to `SprintSummaryParticipant` list
- `web/reflekt-web/src/stores/auth.ts` — added `givenName` field to `User` interface
- `web/reflekt-web/src/stores/sprint.ts` — added `currentPhase` and `participants` fields to `SprintSummary` interface
- `web/reflekt-web/src/views/RetroRoomView.vue` — auto-join logic in `onMounted`; participant chip list on name-picker; 409 error message update; `summaryParticipants` computed

## Key Decisions
- Used `.ToLower()` on both sides in `ExistsByDisplayNameAsync` rather than `string.Equals(OrdinalIgnoreCase)` — EF Core translates both to SQLite's `lower()` function; functionally equivalent for ASCII names
- `authStore.fetchMe()` is conditionally awaited in `RetroRoomView.onMounted` when `!authStore.loaded` to handle the race with `App.vue`'s fire-and-forget call
- Added `return` after the guest-token path so auto-join is never attempted when a valid token exists
- `summaryParticipants` uses `sprintStore.currentSummary.participants` (from unauthenticated summary endpoint) rather than `participantStore.participants` (requires auth token) — correct for anonymous joiners on name-picker

## Deviations from Plan
- None. All six steps implemented as specified.
