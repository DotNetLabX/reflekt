# Auth Plan — Reflekt v1

**Status:** Ready for implementation
**Prerequisite:** Foundation (landed)
**Prerequisite for:** Teams, and every feature that depends on a signed-in user (Sprints, Cards, Votes, EvidenceLinks, Reports, Participants indirectly via SM surface)
**Companion docs:** `docs/specs/v1.md`, `docs/architecture/v1.md`, `CLAUDE.md`

## 0. Purpose

Light up the Google OAuth + cookie session pipeline end-to-end so a user can sign in from the Vue welcome page, see their email in the header, and sign out. Enforce the allowlist at the OAuth callback (primary gate) and expose the session to the SPA via `/auth/me`.

**Non-goals for Auth:**
- No `Team` aggregate, no `CreateTeam` / `GetMyTeams` endpoints — that's the Teams plan (next).
- No routed `/login` page — the "Sign in with Google" button lives on the welcome view.
- No profile settings, no user row in the database — Auth is cookie-session only. The allowlist is the only persistent identity input and it lives in `appsettings.json`.
- No CSRF antiforgery wiring — deferred until a feature actually needs form-style mutations. Logout is POST from same-origin `fetch`.
- No `returnUrl` handling — after sign-in users land on `/`. Revisit if a feature needs deep-linking.

If you find yourself writing an aggregate, a migration, or a `DbSet`, **stop** — that belongs in Teams.

## 1. Relevant skills

Read each skill file before applying it — this plan only names them.

- **`create-feature`** — vertical-slice endpoint scaffolding for FastEndpoints. Use for `LoginEndpoint`, `LogoutEndpoint`, `MeEndpoint`.
- **`authorization-patterns`** — two-layer auth (role gate via policy + resource gate in handler). Relevant only for the `/auth/me` endpoint (role gate = cookie). Resource gate is not used in Auth.

No existing feature slice in this codebase to mirror yet — Auth is the first feature. Later plans will reference `Reflekt.API/Features/Auth/` as the pattern.

## 2. Judgment calls flagged for the Developer

Small decisions the Developer can make without coming back:

- **Single-file vs split Request/Response/Validator per endpoint.** FastEndpoints supports both. For `LoginEndpoint` (no request body) and `LogoutEndpoint` (no body), keep it single-file. For `MeEndpoint` (response record only), single-file. No validators in this feature — nothing to validate.
- **`AllowlistChecker` placement.** Pure static helper taking `(string email, IEnumerable<string> allowed) → bool`. Lives at `Reflekt.API/Auth/AllowlistChecker.cs`. Case-insensitive comparison (`StringComparer.OrdinalIgnoreCase`).
- **Logout response shape.** Return `204 NoContent` after `SignOutAsync`. SPA handles the UI update.
- **`/auth/me` response when unauthenticated.** Return `401`. The SPA treats 401 as "signed out" and shows the sign-in button.

Decisions that should come back if they feel wrong mid-implementation:

- The `OnTicketReceived` rejection shape — if `ctx.HandleResponse()` + `ctx.Response.Redirect("/not-invited")` fights the middleware (double-writing the response), fall back to `ctx.Fail(...)` with `OnRemoteFailure` redirecting to `/not-invited`. Flag either way.

## 3. Backend — endpoint slices

New folder: `src/Services/Reflekt/Reflekt.API/Features/Auth/`.

### 3.1 `LoginEndpoint.cs` — `GET /auth/google/login`

- `public sealed class LoginEndpoint : EndpointWithoutRequest`.
- `Configure()`: `Get("/auth/google/login")`, `AllowAnonymous()`.
- `HandleAsync`: build `AuthenticationProperties { RedirectUri = "/" }`, then `await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props)`. **Do not** call `SendAsync` — the challenge writes the 302.
- Rationale: the SPA's "Sign in with Google" button just does `window.location.href = "/auth/google/login"`. Browser follows the redirect chain to Google → back to `/signin-google` → back to `/`.

### 3.2 `LogoutEndpoint.cs` — `POST /auth/logout`

- `public sealed class LogoutEndpoint : EndpointWithoutRequest`.
- `Configure()`: `Post("/auth/logout")`. **Requires authentication** — use `Policies.ScrumMaster` (the current "cookie + authenticated" gate, per §4.2 of Foundation done notes). No `AllowAnonymous`.
- `HandleAsync`: `await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)`, then `await SendNoContentAsync(ct)`.

### 3.3 `MeEndpoint.cs` — `GET /auth/me`

- `public sealed class MeEndpoint : EndpointWithoutRequest`.
- **Public response record** in the same file: `public sealed record MeResponse(string Email, string DisplayName);`.
- `Configure()`: `Get("/auth/me")`, `Policies(Policies.ScrumMaster)` (role gate — authenticated cookie user).
- `HandleAsync`:
  - Read `email` from `User.FindFirstValue(ClaimTypes.Email)`.
  - Read `displayName` from `User.FindFirstValue(ClaimTypes.Name)` (Google OAuth maps `profile` to `name` claim).
  - If either claim is missing (shouldn't happen with a valid Google ticket, but be defensive), `await SendUnauthorizedAsync(ct); return;`.
  - Otherwise `await SendOkAsync(new MeResponse(email, displayName), ct);`.
- Rationale: SPA calls `/auth/me` on app mount; a 200 means signed-in, 401 means not. No separate "is authenticated" flag.

### 3.4 Allowlist checker

File: `src/Services/Reflekt/Reflekt.API/Auth/AllowlistChecker.cs`.

```csharp
namespace Reflekt.API.Auth;

internal static class AllowlistChecker
{
    public static bool IsAllowed(string? email, IReadOnlyCollection<string> allowed)
        => !string.IsNullOrWhiteSpace(email)
           && allowed.Contains(email, StringComparer.OrdinalIgnoreCase);
}
```

- Static, pure, unit-testable. No DI.
- Called from the `OnTicketReceived` handler (§3.5) and as defense-in-depth inside the `CreateTeam` handler in the Teams plan.

### 3.5 Wire the allowlist into Google OAuth callback

Modify: `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs`.

Inside the `.AddGoogle(...)` options lambda, add an `Events` handler for `OnTicketReceived`:

- Pull `IOptions<AuthOptions>` from `ctx.HttpContext.RequestServices`.
- Extract the email from `ctx.Principal?.FindFirstValue(ClaimTypes.Email)`.
- If `!AllowlistChecker.IsAllowed(email, options.AllowedEmails)`:
  - Call `ctx.HandleResponse()` to stop the default redirect + cookie issuance.
  - `ctx.Response.Redirect("/not-invited");` — SPA route, no auth required, served by the `index.html` fallback.
- Otherwise: do nothing, let the middleware issue the cookie and redirect to `RedirectUri` (`/`).

**Code shape** — keep the lambda tight. If it grows past ~10 lines, extract a `static Task OnGoogleTicketReceived(TicketReceivedContext ctx)` method in the same file.

**One more Google option tweak:** set `options.SaveTokens = false` (explicit). We don't need the access/refresh token persisted in the cookie — we only need the email + name claims. Smaller cookie, fewer surprises.

**Do not touch the scope list.** Foundation already pinned it to `openid` + `email` + `profile` (spec §3.1: "Google OAuth 2.0 — email + profile name only. No photo, no extended scopes."). When editing `AuthenticationConfiguration.cs`, preserve the existing `options.Scope.Clear()` + three `options.Scope.Add(...)` calls exactly as-is. Do not add `profile.photo`, `phone`, or any other scope.

### 3.6 `AuthOptions` binding check

`AuthOptions` already exists (`Reflekt.API/Auth/AuthOptions.cs`) with `AllowedEmails`, and `AddReflektApi` already binds `configuration.GetSection("Auth")`. **No change needed.** Confirm by reading the file before touching it; if it's drifted, flag it.

## 4. Frontend — store, views, header

### 4.1 `auth` Pinia store

Modify: `web/reflekt-web/src/stores/auth.ts` (currently an empty stub).

```ts
import { defineStore } from 'pinia';

interface User { email: string; displayName: string; }

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as User | null,
    loaded: false,
  }),
  getters: {
    isSignedIn: (s) => s.user !== null,
  },
  actions: {
    async fetchMe() {
      const res = await fetch('/auth/me', { credentials: 'include' });
      this.user = res.ok ? (await res.json()) as User : null;
      this.loaded = true;
    },
    signIn() {
      window.location.href = '/auth/google/login';
    },
    async signOut() {
      await fetch('/auth/logout', { method: 'POST', credentials: 'include' });
      this.user = null;
      window.location.href = '/';
    },
  },
});
```

- `credentials: 'include'` is required so the cookie rides along on fetches from Vite dev server (if used) and in prod.
- No reactive auto-refresh. If the cookie expires mid-session, the next API call will 401 — SPA surfaces this as "signed out" on reload. Fine for v1.

### 4.2 `AppHeader.vue` — new component

File: `web/reflekt-web/src/components/AppHeader.vue`.

- Small top bar. Two states:
  - **Signed in:** show `{user.email}` and a "Sign out" button wired to `authStore.signOut()`.
  - **Signed out:** render nothing (welcome view has its own sign-in button).
- Tailwind styling — plain flex row, minimal. No logo, no nav links (nothing to link to yet).

### 4.3 `WelcomeView.vue` — update

Modify: `web/reflekt-web/src/views/WelcomeView.vue`.

- If `authStore.isSignedIn`: show `"Signed in as {email}. Teams feature coming next."` — literal placeholder text. Teams plan replaces with the My Teams page.
- If not: show the headline + a "Sign in with Google" button wired to `authStore.signIn()`.

### 4.4 `NotInvitedView.vue` — new view

File: `web/reflekt-web/src/views/NotInvitedView.vue`.

- One paragraph: `"That email isn't on the allowlist for this Reflekt instance. Talk to the team admin if you think that's wrong."`
- No sign-in button, no retry button. A refresh reverts to the welcome view.

### 4.5 Router update

Modify: `web/reflekt-web/src/router/index.ts`.

- Keep existing `/` → `WelcomeView`.
- Add `/not-invited` → `NotInvitedView`.
- No auth guards on the router — the server decides via cookie; the SPA reacts to `/auth/me`'s response.

### 4.6 `App.vue` — mount header + fetch session

Modify: `web/reflekt-web/src/App.vue`.

- Import and mount `AppHeader` above `<RouterView />`.
- On mount (`onMounted`), call `authStore.fetchMe()` once. Nothing else — no polling, no visibility listeners.

## 5. Tests

### 5.1 Unit tests — `Reflekt.UnitTests`

New file: `tests/Reflekt.UnitTests/Auth/AllowlistCheckerTests.cs`.

Cover:
- Empty allowlist → any email returns `false`.
- Exact match → `true`.
- Case-insensitive match (`Foo@Bar.com` vs `foo@bar.com`) → `true`.
- `null` or whitespace email → `false`.
- Unlisted email → `false`.

Five test methods, no setup, straight `FluentAssertions`.

### 5.2 Integration tests — `Reflekt.IntegrationTests`

New file: `tests/Reflekt.IntegrationTests/AuthSmokeTests.cs`.

Follow the pattern from `FoundationSmokeTests.cs` — same `WebApplicationFactory<Program>` + unique-per-run SQLite file + `SqliteConnection.ClearAllPools()` cleanup.

Tests:
1. `Me_WithoutCookie_Returns401` — `GET /auth/me`, expect 401.
2. `Logout_WithoutCookie_Returns401` — `POST /auth/logout`, expect 401 (the policy gate — you can't sign out if you're not signed in; a `[Authorize]` rejection here is correct behavior).
3. `Login_RedirectsToGoogle` — `GET /auth/google/login` (disable auto-redirect on the `HttpClient`), expect 302 and `Location` header host is `accounts.google.com`.

**What we're not testing here:** the full OAuth callback path (requires mocking Google's token endpoint). The allowlist logic is unit-covered; the middleware wiring is code-reviewed. Acceptable trade-off for v1 scope.

## 6. Config

### 6.1 `appsettings.json`

Already has `Auth:Google:ClientId`, `Auth:Google:ClientSecret`, `Auth:AllowedEmails: []`. **No change to the file.**

### 6.2 User secrets

Developer must set real values to test locally:

```bash
cd src/Services/Reflekt/Reflekt.API
dotnet user-secrets set "Auth:Google:ClientId" "..."
dotnet user-secrets set "Auth:Google:ClientSecret" "..."
dotnet user-secrets set "Auth:AllowedEmails:0" "you@example.com"
```

The Google OAuth app needs redirect URI `http://localhost:4001/signin-google` registered in Google Cloud Console. Document this in `done.md` so future setup is reproducible; no code change needed here — `/signin-google` is the Google middleware default `CallbackPath`.

## 7. Verification — acceptance criteria

Auth is done when all of these pass:

1. `dotnet build Reflekt.slnx` — zero errors, zero warnings.
2. `dotnet test` — all tests pass (existing + new unit + new integration).
3. Manual flow (Developer runs `dotnet run` + `npm run dev`-or-build):
   - Load `http://localhost:4001/` — welcome view with "Sign in with Google" button.
   - Click button → redirected to Google consent → approve → redirected back to `/`.
   - Welcome view now reads `"Signed in as {email}. Teams feature coming next."` + header shows email + "Sign out".
   - `GET http://localhost:4001/auth/me` in a browser tab with the cookie → 200 JSON `{ email, displayName }`.
   - Click "Sign out" → back to the sign-in button. `GET /auth/me` → 401.
4. Allowlist rejection: add an email that **isn't** yours to `Auth:AllowedEmails`; remove yours. Sign in → redirected to `/not-invited`. No cookie issued (verify via DevTools → Application → Cookies: no `.AspNetCore.Cookies` entry).
5. `Program.cs` still ≤ 10 lines — no new `Add*`/`Use*` calls there.
6. No new entities, no new `DbSet<T>`, no EF migration generated.

## 8. Out of scope (reminder)

If tempted to add any of these, stop and check the feature plan:

- `Team` aggregate or `CreateTeam`/`GetMyTeams` endpoints — **Teams plan**.
- Seed data — **Teams plan** (needs the `Team` aggregate).
- Routed `/login` page — not in v1.
- `returnUrl` deep-linking — not in v1.
- Refresh-token handling / silent re-auth — cookie lifetime is the default; re-sign-in on expiry is acceptable.
- Role hierarchy beyond "Scrum Master = team creator" — deferred to v2.
- Email verification beyond Google's `email_verified` claim — Google already verifies.
- CSRF antiforgery middleware — defer until a feature requires form posts.
- `auth_logs` table / audit trail — not in v1.

## 9. File summary (for quick scan)

**New files:**
- `src/Services/Reflekt/Reflekt.API/Features/Auth/LoginEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Auth/LogoutEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Auth/MeEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Auth/AllowlistChecker.cs`
- `tests/Reflekt.UnitTests/Auth/AllowlistCheckerTests.cs`
- `tests/Reflekt.IntegrationTests/AuthSmokeTests.cs`
- `web/reflekt-web/src/components/AppHeader.vue`
- `web/reflekt-web/src/views/NotInvitedView.vue`

**Modified files:**
- `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs` — add `OnTicketReceived` allowlist rejection + `SaveTokens = false`.
- `web/reflekt-web/src/stores/auth.ts` — populate store.
- `web/reflekt-web/src/views/WelcomeView.vue` — signed-in vs signed-out states.
- `web/reflekt-web/src/router/index.ts` — add `/not-invited` route.
- `web/reflekt-web/src/App.vue` — mount `AppHeader`, call `fetchMe()` on mount.

**Unchanged:**
- `Program.cs`, `DependencyInjection.cs` (both), `Policies.cs`, `AuthOptions.cs`, `AuthorizationConfiguration.cs`, `appsettings*.json`, persistence layer entirely.
