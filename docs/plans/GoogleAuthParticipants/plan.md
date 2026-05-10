# Google Auth for Participants (Dual Path)

## Context

V1 participants are ephemeral guests -- a browser token and a display name, scoped to a single sprint in a single browser. V2 introduces Google sign-in as a second identity path: team members sign in with Google for persistent identity across sprints and devices; external invitees join as guests (unchanged from v1). This feature adds an optional `UserId` FK to `Participant`, a new `/join/google` endpoint, modifies the Google login flow to support return URLs, introduces a `GoogleUser` auth policy, and updates the frontend join flow to distinguish signed-in users from guests.

**Service impacted:** Reflekt (single service -- API, Domain, Persistence, and frontend).

## Scope

**In scope:**
- Add `UserId` (optional FK to User) to the `Participant` aggregate and EF configuration
- New endpoint: `POST /api/sprints/{sprintId}/join/google` (Google-authed participant join with auto-suffix on name collision)
- Modify `GET /auth/google/login` to accept an optional `returnUrl` query parameter (relative paths only)
- New auth policy `GoogleUser` for Google-authenticated users who are not necessarily the SM
- Update `ParticipantRepository` with `GetByUserAndSprintAsync` method
- Update frontend `useRetroRoom` composable to call `/join/google` for signed-in users instead of the guest join endpoint
- Update `RetroJoinForm` to show dual-path UI (Sign in with Google + Join as Guest)
- SM auto-join migrates to the unified Google join path (gains cross-device rejoin)
- Update `JoinSprintResponse` to include `userId` (nullable) for frontend awareness

**Out of scope:**
- Guest-to-Google identity merge
- Participant display name editing
- Kicked user rejoin blocking
- Multi-provider auth
- Visual distinction between Google and guest participants in the retro room
- Cross-sprint participant history

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | domain-patterns | Follow | Add `UserId` (int?) to `Participant` aggregate, add `JoinWithGoogle` factory | |
| 2 | persistence-patterns | Follow | Add `UserId` FK config + unique filtered index to `ParticipantConfiguration` | |
| 3 | (none) | -- | Add `GetByUserAndSprintAsync` to `ParticipantRepository` | |
| 4 | (none) | -- | Add `GoogleUser` policy to `AuthorizationConfiguration` | |
| 5 | create-feature | Follow | `JoinSprintGoogle` endpoint, FastEndpoints, `POST /api/sprints/{sprintId}/join/google` | |
| 6 | (none) | -- | Display name collision resolution logic (auto-suffix) | |
| 7 | (none) | -- | Modify `LoginEndpoint` to accept and validate `returnUrl` query parameter | |
| 8 | (none) | -- | Add `UserId` to `JoinSprintResponse` contract | |
| 9 | (none) | -- | EF Core migration | |
| 10 | vue-patterns | Follow | Add `joinWithGoogle` action to participant store | |
| 11 | vue-patterns | Follow | Update `useRetroRoom` composable for Google join path | |
| 12 | vue-patterns | Follow | Update `RetroJoinForm` for dual-path UI | |

## Domain Model Changes

### Participant (modified)

Add an optional `UserId` property linking to the `User` aggregate:

```
Participant {
  ...existing properties...
  UserId: int?   // Optional. Links to User when Google-authed. Null for guests.
}
```

Add a new factory method `JoinWithGoogle(string displayName, int sprintId, int userId)` that creates a Google-linked participant. The existing `Join(string displayName, int sprintId)` method remains for guest joins (unchanged). Both raise `ParticipantJoinedEvent`.

**Invariant:** `UserId`, once set at creation, is immutable.

### No new aggregates, entities, or domain events.

## Data Model Changes

- `Participants` table gains column: `UserId INT NULL` with FK to `Users.Id` (`OnDelete: SetNull` -- if a User is deleted, orphan the participant record rather than cascade-deleting retro data).
- Unique filtered index on `(SprintId, UserId) WHERE UserId IS NOT NULL` -- enforces at most one participant per User per sprint at the database level.
- Index on `UserId` for efficient lookups.

## Implementation Steps

### Step 1: Add `UserId` to Participant aggregate

**What:** Extend the `Participant` aggregate with an optional `UserId` property and a new `JoinWithGoogle` factory method. The existing `Join` method remains unchanged for guest joins.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Domain/Participants/Participant.cs` -- add `public int? UserId { get; init; }`
- `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs` -- add `JoinWithGoogle(string displayName, int sprintId, int userId)` static factory method that creates a Participant with UserId set and raises `ParticipantJoinedEvent`

**Skill:** Follow `domain-patterns` (partial class behavior split, factory method, domain event raise).

**Feature-specific inputs:**
- Property: `public int? UserId { get; init; }` on the state partial
- Factory: `public static Participant JoinWithGoogle(string displayName, int sprintId, int userId)` -- same as `Join` but also sets `UserId = userId`

**Depends on:** Nothing.

### Step 2: Update EF configuration for Participant

**What:** Configure the new `UserId` column, FK relationship to `User`, and the filtered unique index.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/ParticipantConfiguration.cs`

**Skill:** Follow `persistence-patterns` (entity configuration, FK, index).

**Feature-specific inputs:**
- `builder.Property(p => p.UserId).IsRequired(false);`
- `builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.SetNull);`
- `builder.HasIndex(p => new { p.SprintId, p.UserId }).IsUnique().HasFilter("[UserId] IS NOT NULL");` (SQLite uses `"UserId" IS NOT NULL` -- verify EF Core SQLite provider syntax)

**Depends on:** Step 1.

### Step 3: Add repository method for User+Sprint lookup

**What:** Add a method to find an existing participant by UserId and SprintId. This supports the cross-device rejoin flow (return existing participant if already joined).

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` -- add `GetByUserAndSprintAsync(int userId, int sprintId, CancellationToken ct)`

**Skill:** None (simple one-liner query method, same pattern as `GetByGuestTokenAsync`).

**Implementation detail:**
```csharp
public Task<Participant?> GetByUserAndSprintAsync(int userId, int sprintId, CancellationToken ct)
    => Db.Participants
        .SingleOrDefaultAsync(p => p.UserId == userId && p.SprintId == sprintId, ct);
```

**Depends on:** Step 1.

### Step 4: Add `GoogleUser` authorization policy

**What:** The existing `ScrumMaster` policy requires Cookie auth but semantically represents team ownership. Google-authed participants need a policy that requires Cookie authentication (Google session) without the SM ownership semantic. Add a `GoogleUser` policy.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Auth/Policies.cs` -- add `public const string GoogleUser = "GoogleUser";`
- `src/Services/Reflekt/Reflekt.API/Auth/AuthorizationConfiguration.cs` -- register the policy: requires `CookieAuthenticationDefaults.AuthenticationScheme` + `RequireAuthenticatedUser()`

**Skill:** None (auth policy registration -- 3 lines following existing pattern in `AuthorizationConfiguration.cs`).

**Depends on:** Nothing.

### Step 5: Create `JoinSprintGoogle` endpoint

**What:** New endpoint `POST /api/sprints/{sprintId}/join/google` for Google-authenticated participants. Implements the full flow: check sprint exists and is Active, check if user already has a linked participant (return 200 if so), resolve display name collision (auto-suffix), create participant via `JoinWithGoogle`, return 201.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprintGoogle/JoinSprintGoogleEndpoint.cs`

**Skill:** Follow `create-feature` (FastEndpoints variant, no request body needed -- sprintId from route, user identity from claims).

**Feature-specific inputs:**
- Route: `POST /api/sprints/{sprintId}/join/google`
- Auth: `[Authorize(Policy = Policies.GoogleUser)]`
- No request body (user identity from cookie claims, given name from User entity)
- Response: reuse `JoinSprintResponse` from `ParticipantContracts.cs`
- Dependencies injected: `SprintRepository`, `ParticipantRepository`, `UserRepository`

**Handler logic (ordered):**
1. Extract `userId` from claims via `User.GetUserId()`
2. Fetch sprint by ID -- 404 if not found
3. Check `sprint.State != SprintState.Active` -- 409 "Sprint is not accepting participants"
4. Call `participantRepository.GetByUserAndSprintAsync(userId, sprintId)` -- if found, return 200 with existing participant (include GuestToken in response for cross-device flow)
5. Fetch User entity to get `GivenName`
6. Resolve display name: call collision resolution (Step 7 helper) to get a unique name
7. Create participant via `Participant.JoinWithGoogle(resolvedName, sprintId, userId)`
8. Save and return 201

**Guard ordering:** Auth (handled by policy) -> sprint existence -> sprint state -> existing participant check -> domain logic.

**Depends on:** Steps 1, 2, 3, 4.

### Step 6: Display name collision resolution helper

**What:** Extract the display name collision resolution logic into a private method or a focused helper that both the new endpoint (Step 5) can call. The logic: truncate given name to fit within 50 chars, check if name exists in sprint (case-insensitive), if collision append lowest available numeric suffix ("{name} 2", "{name} 3"...), accounting for suffix length in the 50-char limit.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprintGoogle/JoinSprintGoogleEndpoint.cs` -- add private helper method `ResolveDisplayNameAsync(int sprintId, string baseName, CancellationToken ct)` directly in the endpoint class

**Skill:** None (business logic specific to this feature).

**Implementation detail:**
- Truncate `baseName` to 50 chars
- Check `participantRepository.ExistsByDisplayNameAsync(sprintId, baseName)` -- if false, return baseName
- Loop suffix = 2, 3, ...: build candidate = `"{truncatedName} {suffix}"` where `truncatedName` is shortened to `50 - suffix.ToString().Length - 1` chars. Check existence. Return first non-colliding candidate.
- Reasonable upper bound on loop (e.g., 100) to prevent infinite loops in pathological cases.

**Note:** This step's code lives in the same file as Step 5. Developer should implement them together.

**Depends on:** Step 3 (uses `ExistsByDisplayNameAsync` from repository).

### Step 7: Modify `LoginEndpoint` to support `returnUrl`

**What:** Accept an optional `returnUrl` query parameter. Validate it is a relative path (starts with `/`, does not contain `://` or `//`). Use it as the redirect URI after Google authentication. If absent or invalid, use the current default behavior.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Auth/Login/LoginEndpoint.cs` -- change from `EndpointWithoutRequest` to `Endpoint<LoginRequest>` (or read from `Query`). Add validation for the returnUrl parameter.

**Skill:** None (endpoint modification with specific security validation).

**Implementation detail:**
- Read `returnUrl` from query string: `HttpContext.Request.Query["returnUrl"].FirstOrDefault()`
- Validation: must start with `/`, must not contain `://`, must not start with `//` (prevents open redirect)
- If valid: `AuthenticationProperties.RedirectUri = returnUrl` (in development, prepend `http://localhost:5173` to the relative path)
- If absent or invalid: use current default behavior unchanged

**Depends on:** Nothing.

### Step 8: Update `JoinSprintResponse` contract

**What:** Add `UserId` (nullable int) to the response so the frontend can distinguish Google-linked participants. This is used by the response from both `/join` and `/join/google`.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Participants/ParticipantContracts.cs` -- add `int? UserId` to `JoinSprintResponse`

**Skill:** None (one-field addition to existing record).

**Note:** The existing `/join` endpoint returns `JoinSprintResponse` with `UserId = null` for guests (no mapping change needed since Mapster maps null properties automatically). The `/join/google` endpoint returns it with the actual UserId.

**Depends on:** Nothing (but Step 5 consumes this).

### Step 9: Add EF Core migration

**What:** Generate and apply the migration for the `UserId` column addition, FK, and filtered unique index.

**Commands:**
```bash
dotnet ef migrations add AddParticipantUserId -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
dotnet ef database update -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
```

**Depends on:** Steps 1, 2.

### Step 10: Frontend -- add `joinWithGoogle` action to participant store

**What:** Add a new action to the participant store that calls `POST /api/sprints/{sprintId}/join/google` with credentials (cookie auth). Returns the same `JoinSprintResponse` shape.

**Files to modify:**
- `web/reflekt-web/src/stores/participant.ts` -- add `joinWithGoogle(sprintId: number): Promise<JoinSprintResponse>` action

**Skill:** Follow `vue-patterns` (Pinia store action with fetch).

**Feature-specific inputs:**
- Method: POST, credentials: 'include', no body
- URL: `/api/sprints/${sprintId}/join/google`
- Response type: same `JoinSprintResponse` interface (add optional `userId?: number` field to the interface)

**Depends on:** Step 5 (backend endpoint exists).

### Step 11: Frontend -- update `useRetroRoom` composable for Google join path

**What:** Modify the `onMounted` logic in `useRetroRoom.ts` to call `joinWithGoogle` instead of `participantStore.join` for signed-in users. This replaces the current SM auto-join logic (which calls the guest endpoint with givenName) with the proper Google join endpoint that supports cross-device rejoin.

**Files to modify:**
- `web/reflekt-web/src/composables/useRetroRoom.ts`

**Skill:** Follow `vue-patterns` (composable lifecycle, async flow).

**Logic change in onMounted (after guest token check fails):**
```
Current:
  if (authStore.isSignedIn && authStore.user?.givenName)
    → participantStore.join(sprintId, authStore.user.givenName)

New:
  if (authStore.isSignedIn)
    → participantStore.joinWithGoogle(sprintId)
```

- On success (200 or 201): save token, enter room
- On 409: set error message "Sprint is not accepting participants."
- On 404: set error message "Sprint not found."
- On other error: fall through to show join form

**Depends on:** Step 10.

### Step 12: Frontend -- update `RetroJoinForm` for dual-path UI

**What:** When the user is not signed in and has no guest token, show both options: "Sign in with Google" button and "Join as Guest" form. The Google button redirects to `/auth/google/login?returnUrl=/sprint/{sprintId}`.

**Files to modify:**
- `web/reflekt-web/src/components/retro/RetroJoinForm.vue` -- add "Sign in with Google" button above the guest form
- `web/reflekt-web/src/composables/useRetroRoom.ts` -- pass `showGoogleOption` (true when `!authStore.isSignedIn`) to the template context

**Skill:** Follow `vue-patterns` (component props, conditional rendering).

**Feature-specific inputs:**
- New prop: `showGoogleSignIn: boolean`
- Google button: `<a :href="googleLoginUrl">Sign in with Google</a>` where `googleLoginUrl = '/auth/google/login?returnUrl=/sprint/' + sprintId`
- Visual: "Sign in with Google" as a secondary button above a divider ("or"), then the existing guest form below
- The Google button is a plain navigation (full page redirect to OAuth flow), not an API call

**Depends on:** Steps 7, 11.

## Cross-Service Changes

None. Single service.

## Migration Notes

```bash
dotnet ef migrations add AddParticipantUserId -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
dotnet ef database update -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
```

No seed data changes needed. Existing participants get `UserId = NULL` (they are all guests).

## Testing Strategy

**Backend:**
- `POST /api/sprints/{sprintId}/join/google` with valid Google session creates a new participant linked to User (201)
- `POST /api/sprints/{sprintId}/join/google` with existing linked participant returns the existing one (200) with same GuestToken
- `POST /api/sprints/{sprintId}/join/google` when sprint not found returns 404
- `POST /api/sprints/{sprintId}/join/google` when sprint not Active returns 409
- `POST /api/sprints/{sprintId}/join/google` with name collision auto-suffixes ("Ana" -> "Ana 2")
- `POST /api/sprints/{sprintId}/join` continues to work unchanged for guests (regression)
- `GET /auth/google/login?returnUrl=/sprint/42` includes the return URL in the OAuth redirect
- `GET /auth/google/login?returnUrl=https://evil.com` ignores the invalid URL (uses default)
- Unique index prevents two participants with the same UserId in one sprint

**Frontend (manual):**
- Signed-in user opens sprint link -> auto-joined via Google endpoint, no form shown
- Non-signed-in user opens sprint link -> sees dual-path form (Google button + guest form)
- Click "Sign in with Google" -> redirected to OAuth -> redirected back to sprint -> auto-joined
- "Join as Guest" works same as v1
- Cross-device rejoin: sign in on second device -> same participant, same guest token stored
- SM opening their sprint -> auto-joined via Google path (gains cross-device rejoin)

## Open Questions

None.
