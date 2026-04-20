# Auth

**Traces to:** `docs/specs/v1.md` §3.1, §12.6
**Dependencies:** None
**Status:** Ready

---

## Purpose

Google OAuth authentication for the Scrum Master (team creator). V1 serves one team, so authentication is gated by an email allowlist configured per-environment. This feature provides the cookie-based identity every team/sprint-management endpoint depends on; the participant (`GuestToken`) identity tier is a separate feature (see §3.2).

## Entities

None. Identity is held in the ASP.NET auth cookie — no domain entity for users.

Configuration is bound from `appsettings.json`:

| Section | Key | Type | Notes |
|---------|-----|------|-------|
| `Auth` | `AllowedEmails` | `List<string>` | Case-insensitive allowlist; unlisted emails are rejected after Google sign-in |
| `Auth:Google` | `ClientId` | string | OAuth client id |
| `Auth:Google` | `ClientSecret` | string | OAuth client secret |

## User Flows

```
Flow 1: Login (happy path)
1. User hits GET /auth/google/login
2. API issues an OAuth challenge to Google (scopes: openid, email, profile)
3. Google redirects back to /signin-google with the auth code
4. Google handler exchanges the code, builds the ClaimsPrincipal
5. OnTicketReceived checks the email against Auth:AllowedEmails
6. Allowed → auth cookie is issued, user is redirected to /
```

```
Flow 2: Login (not allowlisted)
1. Steps 1–4 as above
2. OnTicketReceived finds the email is NOT in Auth:AllowedEmails
3. Handler calls HandleResponse() and redirects to /not-invited
4. No auth cookie is issued
```

```
Flow 3: Read current user
1. Authenticated user hits GET /auth/me
2. API reads email + displayName from the cookie's claims
3. Response: 200 with { email, displayName }
```

```
Flow 4: Logout
1. Authenticated user hits POST /auth/logout
2. API signs out of the Cookies scheme (auth cookie cleared)
3. Response: 204 No Content
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | /auth/google/login | AllowAnonymous | — | 302 redirect to Google | 302 Found |
| GET | /auth/me | ScrumMaster policy | — | `{ email, displayName }` | 200 OK, 401 Unauthorized |
| POST | /auth/logout | ScrumMaster policy | — | — | 204 No Content, 401 Unauthorized |

**Implicit endpoints** (registered by `AddGoogle`, not written by us):
- `GET /signin-google` — Google OAuth callback. Redirects to `/not-invited` for non-allowlisted emails; otherwise issues the auth cookie and redirects to `/`.

**`/not-invited`** is a frontend SPA route, not a backend endpoint. The backend only redirects to it.

## Business Rules

1. Only emails listed in `Auth:AllowedEmails` may complete sign-in. Comparison is case-insensitive.
2. Non-allowlisted sign-in attempts redirect to `/not-invited` and no cookie is issued.
3. The cookie is `HttpOnly`, `SameSite=Lax`, and `Secure` when the request is HTTPS (`CookieSecurePolicy.SameAsRequest`).
4. Cookie-auth challenges return `401` (not a redirect to a login page), and access-denied returns `403`, so the SPA can handle them as API responses.
5. Google OAuth requests only `openid`, `email`, and `profile` scopes. Tokens are not persisted (`SaveTokens = false`).
6. The `ScrumMaster` authorization policy requires an authenticated identity on the `Cookies` scheme. Per-team ownership checks (e.g. `Team.CreatorEmail == user.email`) are the responsibility of the endpoints that own those resources.

## Acceptance Criteria

- [x] Unauthenticated user is redirected to Google when hitting `/auth/google/login`
- [x] Allowlisted Google user completes sign-in and receives an auth cookie
- [x] Non-allowlisted Google user is redirected to `/not-invited` and no cookie is issued
- [x] `GET /auth/me` returns `{ email, displayName }` for authenticated users
- [x] `GET /auth/me` returns 401 for unauthenticated users
- [x] `POST /auth/logout` clears the auth cookie and returns 204
- [x] Cookie challenges return 401 rather than redirecting to a login page
- [x] Access-denied returns 403 rather than redirecting
- [x] `ScrumMaster` policy resolves for any cookie-authenticated user
- [x] Build passes with zero errors

## Out of Scope

- **Participant (`GuestToken`) authentication** — separate feature (`docs/features/Participants.md` when written). The `GuestTokenAuthenticationHandler`, `Participant` policy, and `OwnerOrScrumMaster` policy are scaffolded in code but their behavior and endpoints belong to the Participants feature.
- **Resource-based authorization** — per-team/per-sprint ownership checks live in the features that own those resources, not in the auth policy.
- **Multi-admin / invite flow / role split** — v1 is single-creator (spec §3.1).
- **Account management UI** — no password, no profile editing, no account deletion; identity is Google-delegated.
- **Token refresh / session sliding** — cookie lifetime uses ASP.NET defaults; no custom sliding expiry in v1.
- **Scopes beyond email + profile** — no photo, no calendar, no Drive (spec §3.1).
