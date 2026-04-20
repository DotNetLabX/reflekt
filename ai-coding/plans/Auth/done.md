# Auth — Implementation Log

## Files Created

- `Reflekt.API/Auth/AllowlistChecker.cs` — Pure static helper; made `public` (not `internal` as spec'd) so `Reflekt.UnitTests` can access it without adding a heavy `Reflekt.API` reference; `Reflekt.UnitTests.csproj` gained that reference anyway (see below).
- `Reflekt.API/Features/Auth/LoginEndpoint.cs` — `GET /auth/google/login`; challenges Google OAuth and calls `Response.StartAsync` to flush the 302 before FastEndpoints auto-sends 204.
- `Reflekt.API/Features/Auth/LogoutEndpoint.cs` — `POST /auth/logout`; uses `using AuthPolicies = Reflekt.API.Auth.Policies` alias because `Policies(...)` is ambiguous (method vs class name in same scope).
- `Reflekt.API/Features/Auth/MeEndpoint.cs` — `GET /auth/me`; same alias pattern; returns `MeResponse(Email, DisplayName)` or 401 if claims missing.
- `tests/Reflekt.UnitTests/Auth/AllowlistCheckerTests.cs` — 5 unit tests; all pass.
- `tests/Reflekt.IntegrationTests/AuthSmokeTests.cs` — 3 smoke tests; all pass.
- `web/reflekt-web/src/components/AppHeader.vue` — Signed-in header with email + sign-out button; hidden when signed out.
- `web/reflekt-web/src/views/NotInvitedView.vue` — Static message; no sign-in button.

## Files Modified

- `Reflekt.API/Auth/AuthenticationConfiguration.cs` — Added `OnTicketReceived` allowlist gate + `SaveTokens = false`; added `OnRedirectToLogin` → 401 and `OnRedirectToAccessDenied` → 403 on cookie options (required for SPA to see 401 instead of redirect to `/Account/Login`).
- `tests/Reflekt.UnitTests/Reflekt.UnitTests.csproj` — Added `ProjectReference` to `Reflekt.API` so `AllowlistCheckerTests` can compile.
- `web/reflekt-web/src/stores/auth.ts` — Populated store per plan spec.
- `web/reflekt-web/src/views/WelcomeView.vue` — Signed-in vs signed-out states.
- `web/reflekt-web/src/router/index.ts` — Added `/not-invited` route.
- `web/reflekt-web/src/App.vue` — Mounted `AppHeader`; calls `fetchMe()` on mount.

## Local setup (reproducible)

### Google Cloud Console
1. Go to [Google Cloud Console](https://console.cloud.google.com/) → APIs & Services → Credentials.
2. Create an OAuth 2.0 Client ID (type: Web application).
3. Add Authorized redirect URI: `http://localhost:4001/signin-google`.
4. Copy the Client ID and Client Secret.

### User secrets (run from repo root)
```bash
cd src/Services/Reflekt/Reflekt.API
dotnet user-secrets set "Auth:Google:ClientId" "<your-client-id>"
dotnet user-secrets set "Auth:Google:ClientSecret" "<your-client-secret>"
dotnet user-secrets set "Auth:AllowedEmails:0" "dumitrescu.laurentiu@gmail.com"
```

## Manual verification status (§7)

**§7.1 Build clean** — PASS. `dotnet build Reflekt.slnx` → 0 errors, 0 warnings.

**§7.2 Tests pass** — PASS. All 10 tests pass (6 unit + 4 integration).

**§7.3 Full sign-in/sign-out browser flow** — NOT TESTED by Developer. Requires user secrets + Google OAuth client configured (see Local setup above). Steps to verify manually:
- Load `http://localhost:4001/` → welcome view with "Sign in with Google" button.
- Click → Google consent → approve → redirected to `/`.
- Welcome view shows `"Signed in as {email}. Teams feature coming next."` + header shows email + "Sign out".
- `GET http://localhost:4001/auth/me` → 200 JSON `{ email, displayName }`.
- Click "Sign out" → sign-in button returns. `GET /auth/me` → 401.

**§7.4 Allowlist rejection** — NOT TESTED by Developer. Requires Google OAuth client configured. Steps to verify manually:
- Set `Auth:AllowedEmails:0` to an email that is NOT yours.
- Sign in → should redirect to `/not-invited`.
- Verify in DevTools → Application → Cookies: no `.AspNetCore.Cookies` entry issued.

**§7.5 `Program.cs` ≤ 10 lines** — NOT MODIFIED. No new `Add*`/`Use*` calls added.

**§7.6 No new entities / `DbSet<T>` / EF migration** — CONFIRMED. No persistence layer changes.

## Deviations from Plan

1. **`AllowlistChecker` reverted to `internal` per review** — Initially made `public` to avoid `InternalsVisibleTo` wiring; review established repo convention as `internal` + `InternalsVisibleTo`. Fixed: `AllowlistChecker` is `internal`, `Reflekt.API.csproj` has `<InternalsVisibleTo Include="Reflekt.UnitTests" />`, and `Reflekt.UnitTests.csproj` retains the `ProjectReference` to `Reflekt.API` for compile.
2. **`OnRedirectToLogin` / `OnRedirectToAccessDenied` added to cookie options** — Plan didn't mention this but it's required: without it, unauthenticated requests get a 302 to `/Account/Login` (followed by the test client to a 200 index.html) instead of the 401 the plan and SPA expect.
3. **`await HttpContext.Response.StartAsync(ct)` in `LoginEndpoint`** — Plan didn't mention this. FastEndpoints 5.34.0 auto-sends 204 after `HandleAsync` when no `Send*` method is called. `ChallengeAsync` sets the 302 status + Location header but doesn't flush them (`HasStarted` stays false). Calling `StartAsync` commits the redirect headers before FastEndpoints overwrites with 204.
4. **`using AuthPolicies = Reflekt.API.Auth.Policies` alias** — Plan called `Policies(Policies.ScrumMaster)` but `Policies` is both a method (FastEndpoints) and a class in scope — CS0119 error. Alias resolves it cleanly.
