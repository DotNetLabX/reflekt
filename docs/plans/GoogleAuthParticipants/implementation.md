# GoogleAuthParticipants — Implementation

## Files Created

- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprintGoogle/JoinSprintGoogleEndpoint.cs` — New endpoint `POST /api/sprints/{sprintId}/join/google` with `GoogleUser` policy, existing-participant check (200), display name collision resolution (auto-suffix up to 100), `JoinWithGoogle` factory call, returns 201. Includes private `ResolveDisplayNameAsync` helper (Step 6 implemented in same file as noted in plan).
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260510*_AddParticipantUserId.cs` — EF Core migration adding `UserId` nullable column, FK to `Users` with `SetNull` delete behavior, unique filtered index on `(SprintId, UserId) WHERE UserId IS NOT NULL`, and index on `UserId`.

## Files Modified

- `src/Services/Reflekt/Reflekt.Domain/Participants/Participant.cs` — Added `public int? UserId { get; init; }` property.
- `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs` — Added `JoinWithGoogle(string displayName, int sprintId, int userId)` static factory that sets `UserId` and raises `ParticipantJoinedEvent`.
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/ParticipantConfiguration.cs` — Added `UserId` property config, FK to `User` with `SetNull`, unique filtered index on `(SprintId, UserId)`, index on `UserId`. Added `using Reflekt.Domain.Users`.
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` — Added `GetByUserAndSprintAsync(int userId, int sprintId, CancellationToken ct)`.
- `src/Services/Reflekt/Reflekt.API/Auth/Policies.cs` — Added `public const string GoogleUser = "GoogleUser"`.
- `src/Services/Reflekt/Reflekt.API/Auth/AuthorizationConfiguration.cs` — Registered `GoogleUser` policy: Cookie auth scheme + `RequireAuthenticatedUser()`.
- `src/Services/Reflekt/Reflekt.API/Features/Auth/Login/LoginEndpoint.cs` — Added `returnUrl` query parameter support. Validates relative paths only (starts with `/`, no `://`, no `//` prefix). In development prepends `http://localhost:5173`. Falls back to default if absent or invalid.
- `src/Services/Reflekt/Reflekt.API/Features/Participants/ParticipantContracts.cs` — Added `int? UserId` to `JoinSprintResponse`. Guest joins map null automatically via Mapster.
- `web/reflekt-web/src/stores/participant.ts` — Added `userId?: number | null` to `JoinSprintResponse` interface. Added `joinWithGoogle(sprintId)` action: POST with `credentials: 'include'`, no body.
- `web/reflekt-web/src/composables/useRetroRoom.ts` — Replaced SM auto-join (guest endpoint with givenName) with `participantStore.joinWithGoogle(sprintId)` for all signed-in users. Error messages: 409 → "Sprint is not accepting participants.", 404 → "Sprint not found.", other errors fall through to show join form. Added `showGoogleOption` computed (true when `!authStore.isSignedIn`) to returned context.
- `web/reflekt-web/src/components/retro/RetroJoinForm.vue` — Added `sprintId: number` and `showGoogleSignIn: boolean` props. When `showGoogleSignIn` is true, shows "Sign in with Google" anchor link (`/auth/google/login?returnUrl=/sprint/{sprintId}`) above a divider, then the guest form below. Guest join button label changed from "Join" to "Join as Guest".
- `web/reflekt-web/src/views/RetroRoomView.vue` — Added `:sprint-id` and `:show-google-sign-in="showGoogleOption"` props to `RetroJoinForm`. Destructures `showGoogleOption` from `useRetroRoom`.

## Key Decisions

- Steps 5 and 6 implemented together in one file as explicitly noted in the plan.
- EF note: EF Core emitted a debug message that the standalone `SprintId` index was subsumed by the composite `(SprintId, UserId)` index — this is correct behavior, no action needed.
- `JoinSprintGoogleEndpoint` uses `using Reflekt.API.Auth;` explicitly (not just global usings) for `GetUserId()` extension method access — consistent with other endpoints that use the same pattern.
- The `ResolveDisplayNameAsync` helper truncates the base name to 50 chars, then for each suffix computes the max allowable base length as `50 - suffixLen - 1` (the -1 is for the space separator), preventing names exceeding the 50-char limit after suffixing.

## Deviations from Plan

None. All 12 steps implemented as specified. Steps 5 and 6 were implemented together as directed.

## Review Fixes (Cycle 1)

- `JoinSprintGoogleEndpoint.cs` — Replaced fallback `return truncated` after exhausting 100 suffix attempts with `ThrowError(409)`. Returning the conflicting name violated Business Rule 5 (display name uniqueness within a sprint).
- `LoginEndpoint.cs` — Added `Uri.UnescapeDataString` before validation in `IsValidReturnUrl`. Blocks percent-encoded `//` bypasses (`/%2F%2Fevil.com`) that previously passed all guards.
