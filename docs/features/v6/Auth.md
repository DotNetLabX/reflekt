# Auth

**Traces to:** `docs/specs/v1.md` §3, §12.4, §12.6
**Dependencies:** None (foundational — other features wire into Auth's schemes and policies)
**Status:** Ready

---

## Purpose

Reflekt has two identity tiers — Google-authenticated Scrum Masters and ephemeral guest participants — that never blur. Auth provides the authentication schemes, authorization policies, session endpoints, and email allowlist that all other features build on. See v1 spec §3 for the full identity model.

## Entities

Auth introduces no entities of its own. It reads from:

- `Team.CreatorEmail` — to verify Scrum Master ownership (defined by Teams feature)
- `Participant.GuestToken` — to authenticate guest participants (defined by Participants feature)

## User Flows

```
Flow 1: Scrum Master login
1. User navigates to GET /auth/google/login
2. Server redirects to Google consent screen (openid + email + profile scopes)
3. Google redirects back with auth code
4. Server exchanges code, extracts email claim
5. Server checks email against Auth:AllowedEmails allowlist
6a. Allowed → cookie is issued, redirect to /
6b. Not allowed → redirect to /not-invited (no cookie, no account created)
```

```
Flow 2: Scrum Master session check
1. Authenticated user calls GET /auth/me
2. Server reads email and display name from cookie claims
3. Returns { email, displayName }
4. If cookie is missing or expired → 401 (no redirect)
```

```
Flow 3: Scrum Master logout
1. Authenticated user calls POST /auth/logout
2. Server clears the authentication cookie
3. Returns 204
```

```
Flow 4: Guest participant authentication
1. Participant sends X-Guest-Token header (GUID) with every API request
2. GuestTokenAuthenticationHandler extracts token from header
   (or from access_token query parameter for SignalR handshake)
3. Handler looks up Participant by GuestToken
4a. Found → creates ClaimsPrincipal with ParticipantId and SprintId claims
4b. Not found → AuthenticateResult.NoResult() (no error, just unauthenticated)
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | `/auth/google/login` | Anonymous | — | Redirect to Google | 302 |
| POST | `/auth/logout` | ScrumMaster | — | — | 204, 401 |
| GET | `/auth/me` | ScrumMaster | — | `{ email, displayName }` | 200, 401 |

Error responses:
- **Login with non-allowlisted email:** 302 redirect to `/not-invited`
- **Me/Logout without valid cookie:** 401 status code (not a login-page redirect — SPA model)

## Business Rules

1. Only emails listed in `Auth:AllowedEmails` configuration can complete Google sign-in. All others are redirected to `/not-invited`.
2. The allowlist check runs in the `OnTicketReceived` Google OAuth event — before a cookie is ever issued.
3. Cookie auth endpoints return 401/403 status codes, not login-page redirects (SPA-compatible).
4. Google OAuth requests `openid`, `email`, and `profile` scopes only. No extended access.
5. Guest tokens are accepted from either the `X-Guest-Token` header or the `access_token` query parameter (SignalR handshake compatibility).
6. The **ScrumMaster** policy requires cookie authentication. Team-ownership verification (`Team.CreatorEmail == user.email`) is enforced per-endpoint where a team context exists — not in the policy itself.
7. The **Participant** policy requires guest token authentication with a valid, lookup-matched token.
8. The **OwnerOrScrumMaster** policy accepts either authentication scheme. Resource-level ownership checks (e.g., card author == caller) are enforced per-endpoint.
9. Cookie options: `SameSite=Lax`, `HttpOnly=true`, `SecurePolicy=SameAsRequest`.

## Acceptance Criteria

- [ ] `GET /auth/google/login` redirects to Google consent screen
- [ ] Successful Google callback with allowlisted email issues a cookie and redirects to `/`
- [ ] Non-allowlisted email redirects to `/not-invited` without issuing a cookie
- [ ] `GET /auth/me` returns `{ email, displayName }` from cookie claims
- [ ] `GET /auth/me` returns 401 when no cookie is present (not a redirect)
- [ ] `POST /auth/logout` clears the authentication cookie and returns 204
- [ ] `POST /auth/logout` returns 401 when no cookie is present
- [ ] GuestTokenAuthenticationHandler reads token from `X-Guest-Token` header
- [ ] GuestTokenAuthenticationHandler reads token from `access_token` query parameter (SignalR fallback)
- [ ] GuestToken handler returns `NoResult` (not `Fail`) when no token is present
- [ ] Three authorization policies registered: ScrumMaster, Participant, OwnerOrScrumMaster
- [ ] ScrumMaster policy requires cookie authentication scheme
- [ ] Participant policy requires guest token authentication scheme
- [ ] OwnerOrScrumMaster policy accepts either scheme

## Out of Scope

- **Multi-admin / team invite flow** — v2; v1 serves one team (spec §3.1)
- **Role hierarchy beyond SM / Participant** — not needed for one-team v1
- **JWT-based auth** — cookie auth is simpler for SPA + server model (spec §12.4)
- **Participant entity and CRUD** — owned by the Participants feature; Auth only provides the GuestToken authentication handler
- **Token refresh / expiration** — guest tokens are ephemeral per-sprint with no lifecycle management
- **OAuth token storage** — `SaveTokens = false`; we use Google only for identity, not API access
