# GoogleAuthParticipants — Review

## Reviewed By
`reviewer` (Sonnet agent). Cycle 1: REQUEST CHANGES. Cycle 2: APPROVE.

## Verdict: APPROVE

All cycle 1 findings resolved. No new issues introduced by the fixes.

---

## Pre-commitment Predictions

| Prediction | Actual |
|---|---|
| Open redirect bypass via URL encoding in `IsValidReturnUrl` | Not a genuine bypass — server-side relative redirect keeps same origin. LOW at most. |
| Off-by-one in 50-char truncation + suffix math | Math is correct: `maxBase = 50 - suffixLen - 1`. |
| Missing `ExistsByDisplayNameAsync` on repository | Already existed in `ParticipantRepository.cs` line 18. |
| Frontend `showGoogleOption` → `showGoogleSignIn` prop wiring | Correctly wired in `RetroRoomView.vue` line 19. |
| Mapster null mapping for `JoinSprintResponse.UserId` | Convention mapping works; `int?` null → null automatically. |

---

## Findings

### [HIGH] `ResolveDisplayNameAsync` fallback returns a name known to be taken

**File:** `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprintGoogle/JoinSprintGoogleEndpoint.cs:68-70`

**Issue:** After exhausting 100 suffix attempts the method returns `truncated` — which is the base name that was already checked and found to be taken (line 55). This will produce a `Participant` with a duplicate display name in the sprint, violating Spec Business Rule 5: "Display name uniqueness within a sprint remains enforced (case-insensitive) — same rule as v1."

The plan says "Reasonable upper bound (e.g., 100) to prevent infinite loops in pathological cases." It does not say what to do on overflow; returning a taken name is the wrong choice.

**Fix:** Replace the fallback return with a thrown error:
```csharp
throw new InvalidOperationException(
    $"Could not find a unique display name for '{baseName}' in sprint {sprintId} after 100 attempts.");
```
Or return an `ErrorOr` / use `ThrowError` so the endpoint returns 409 with a human-readable message ("Too many participants share this name. Please join as a guest with a custom name."). Either is better than silently creating a duplicate. The exact error surface is a developer decision; the fix is to stop returning a conflicting name.

**Confidence:** HIGH. The code path is reachable (100 participants with names "X", "X 2" … "X 100" in one sprint), and the fallback at line 69 demonstrably returns a name that `ExistsByDisplayNameAsync` returned `true` for at line 55.

---

### [LOW] `IsValidReturnUrl` does not normalise URL encoding

**File:** `src/Services/Reflekt/Reflekt.API/Features/Auth/Login/LoginEndpoint.cs:34-35`

**Issue:** A caller could supply `returnUrl=/%2F%2Fevil.com`. This passes all three guards (starts with `/`, does not start with `//`, does not contain `://`). In production the `RedirectUri` becomes `/%2F%2Fevil.com` — a relative path — so the browser stays on the same origin. Not a genuine open redirect under standard HTTP. However, some proxies decode percent-encoded slashes before forwarding, and the intent of the guard is clearly to block `//`-style redirects regardless of encoding.

**Fix:** Add `!url.Contains("%2F%2F", StringComparison.OrdinalIgnoreCase)` to the validation, or URL-decode the value before validating:
```csharp
private static bool IsValidReturnUrl(string url)
{
    var decoded = Uri.UnescapeDataString(url);
    return decoded.StartsWith('/') && !decoded.StartsWith("//") && !decoded.Contains("://");
}
```

---

## Positive Observations

- **Domain model is clean.** `Participant.JoinWithGoogle` follows the existing `Join` factory pattern exactly — same guard, same event, adds only `UserId`. No duplication, no deviation from the partial-class behavior split.
- **EF configuration is precise.** `ParticipantConfiguration.cs` uses the correct SQLite filter syntax (`"UserId" IS NOT NULL`), `DeleteBehavior.SetNull`, and both the composite filtered index and the standalone lookup index — all exactly as the plan specified.
- **Guard ordering in the endpoint is correct.** Auth → sprint existence (404) → sprint state (409) → existing participant check (200) → domain logic. This matches the plan's ordered guard list and is consistent with `JoinSprintEndpoint`.
- **Cross-device rejoin returns the existing `GuestToken`.** Because the existing `Participant` entity is returned and adapted, the guest token is included in the 200 response — enabling the cross-device rejoin scenario described in spec Flow 3 without any special-casing.
- **`ResolveDisplayNameAsync` suffix math is correct.** `maxBase = 50 - suffixStr.Length - 1` correctly accounts for the space separator and keeps every suffixed candidate within the 50-char limit.
- **Frontend dual-path is cleanly separated.** `showGoogleSignIn` is a prop (not internal state), the Google link is a plain anchor (not an API call), and the guest form is unchanged — separation of concerns is correct.
- **`loginEndpoint` stays `EndpointWithoutRequest`.** Reading `returnUrl` from `HttpContext.Request.Query` directly keeps the endpoint anonymous-compatible and avoids introducing a request model for a single optional query param. Sensible choice consistent with the existing pattern.
- **Build is clean.** Zero warnings, zero errors.

---

## Gaps

- **No DB-level uniqueness on `(SprintId, DisplayName)`.** Display name uniqueness is enforced only in application code (`ExistsByDisplayNameAsync`). Concurrent requests from two clients with the same name could both pass the check and both insert. This pre-existed before this feature and is out of scope for this plan, but worth noting for v2 hardening.
- **`ResolveDisplayNameAsync` does not handle the case where `user.GivenName` is empty after truncation.** If somehow `GivenName` is an empty string (theoretically guarded by `User.Create`'s `ThrowIfNullOrWhiteSpace`), `Participant.JoinWithGoogle` would throw `ArgumentException`. This is defended at the domain layer so not a bug here, but the endpoint does not surface a clean 4xx — it would throw unhandled.
- **No test for the fallback code path** (100+ collisions). The testing strategy in the plan does not cover the overflow path. Since the fallback is the finding above, once fixed the test gap should also be closed.

---

## Open Questions

None. All CRITICAL/HIGH findings have HIGH confidence.

---

## Evidence

| Check | Result | Command | Output |
|---|---|---|---|
| Build | PASS | `dotnet build Reflekt.API.csproj --no-incremental` | 0 Warnings, 0 Errors, 4.28s |
| Domain step 1 | PASS | Read `Reflekt.Domain/Participants/Participant.cs` + `Behaviors/Participant.cs` | `UserId` property added; `JoinWithGoogle` factory present and raises `ParticipantJoinedEvent` |
| Persistence step 2 | PASS | Read `ParticipantConfiguration.cs` | FK, filtered unique index, UserId index all present |
| Repository step 3 | PASS | Read `ParticipantRepository.cs` | `GetByUserAndSprintAsync` present; `ExistsByDisplayNameAsync` pre-existing |
| Auth step 4 | PASS | Read `Policies.cs`, `AuthorizationConfiguration.cs` | `GoogleUser` constant and policy registration present |
| Endpoint step 5 | PASS (with HIGH finding) | Read `JoinSprintGoogleEndpoint.cs` | Guard ordering correct; fallback at line 69 is the HIGH finding |
| Collision resolution step 6 | PARTIAL (HIGH finding) | Read `JoinSprintGoogleEndpoint.cs:50-71` | Suffix math correct; fallback returns taken name |
| LoginEndpoint step 7 | PASS | Read `LoginEndpoint.cs` | returnUrl accepted, validated, applied; LOW encoding note raised |
| Contract step 8 | PASS | Read `ParticipantContracts.cs` | `int? UserId` added to `JoinSprintResponse` |
| Migration step 9 | PASS | Read `20260510090004_AddParticipantUserId.cs` | Column, FK, both indexes present; Down() fully reverts |
| Frontend store step 10 | PASS | Read `participant.ts` | `joinWithGoogle` action present; `userId?: number \| null` on interface |
| useRetroRoom step 11 | PASS | Read `useRetroRoom.ts` | `joinWithGoogle` replaces SM guest-join; error handling matches plan; `showGoogleOption` returned |
| RetroJoinForm step 12 | PASS | Read `RetroJoinForm.vue`, `RetroRoomView.vue` | Props wired correctly; Google anchor and divider present; guest button labelled "Join as Guest" |

---

## Cycle 2 Fix Verification

### [HIGH] Fallback fix — VERIFIED
**File:** `JoinSprintGoogleEndpoint.cs:69-70`

`return truncated` replaced with `ThrowError("Unable to assign a unique display name. Too many participants with the same given name.", StatusCodes.Status409Conflict)` followed by `return default!;` (unreachable, required for compiler). `ThrowError` is the correct FastEndpoints pattern (used identically in `JoinSprintEndpoint` for name collisions). The uniqueness invariant is now upheld — the overflow path surfaces a 409 rather than silently creating a duplicate.

### [LOW] `IsValidReturnUrl` decode fix — VERIFIED
**File:** `LoginEndpoint.cs:34-38`

`Uri.UnescapeDataString(url)` is now called before all three guards. The decoded value is checked, so `/%2F%2Fevil.com` decodes to `//evil.com` and fails the `!decoded.StartsWith("//")` guard. Implementation matches the suggested fix exactly.

### Build — PASS
`dotnet build --no-incremental`: 0 Warnings, 0 Errors, 4.02s.
