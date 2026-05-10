# Google Auth for Participants (Dual Path)

**Traces to:** `docs/specs/v2.md` §4.3
**Source:** Scratch
**Dependencies:** Team Management (User Entity — v2.md §4.0)
**Status:** Ready

---

## Purpose

V1 participants are ephemeral guests — a browser token and a display name, scoped to a single sprint in a single browser. Clearing the browser or switching devices loses the session. V2 introduces Google sign-in as a second identity path for participants: team members sign in with Google for persistent identity across sprints and devices; external invitees join as guests (unchanged from v1).

The two paths reflect who the participant is, not a per-sprint preference. Google sign-in is the default for team members. Guest join exists for external participants who don't have Google accounts or aren't part of the team.

## Entities

### Participant (modified)

The Participant entity gains an optional link to User.

| Field | Type | Constraints |
|-------|------|-------------|
| UserId | int? | Optional. Links to a User record when the participant signed in with Google. Null for guest participants. Unique per sprint — at most one participant per User per sprint. |

All other Participant fields are unchanged.

**Rules:**
- A Google-authenticated participant is linked to their User record at join time. This link is permanent — it does not change after creation.
- Within a sprint, at most one participant can be linked to a given User.
- A participant linked to a User is recognized across devices — rejoining from another device returns the existing participant rather than creating a new one.

No new entities are introduced.

## User Flows

```
Flow 1: Signed-in user opens a sprint link (not yet joined)
1. User opens the sprint link in their browser.
2. User is already signed in with Google (has an active session).
3. System auto-joins the user as a participant using their Google given name (first name).
4. If the given name is already taken in this sprint, the system appends the lowest
   available numeric suffix ("Ana 2", "Ana 3") until a unique name is found.
5. The participant is linked to the user's identity record. A guest token is
   generated and stored in the browser.
6. User enters the retro room immediately — no join form is shown.
```

```
Flow 2: Non-signed-in user opens a sprint link
1. User opens the sprint link in their browser.
2. User has no active Google session.
3. System shows a join page with two options:
   a. "Sign in with Google" — for team members with a Google account.
   b. "Join as Guest" — name input + join button, for external participants.
4a. If "Sign in with Google": user is redirected to Google sign-in. After
    authentication, user is redirected back to the sprint page. Flow continues
    as Flow 1 (auto-join with given name).
4b. If "Join as Guest": user enters a display name and joins.
    Same as v1 guest flow — no User link.
```

```
Flow 3: Google-linked participant rejoins from another device
1. User opens the same sprint link from a different device or browser.
2. User is signed in with Google on this device.
3. System finds an existing participant linked to this user in this sprint.
4. System returns the existing participant — same identity, same guest token.
5. The guest token is stored in the new browser.
6. Both devices are now connected as the same participant — cards, votes, and
   display name are shared.
```

```
Flow 4: User who already joined as guest signs in with Google in the same sprint
1. User previously joined Sprint X as guest "Ana" (with a guest token in the browser).
2. User returns to the sprint page and is now signed in with Google
   (or clicks "Sign in with Google" on the join page).
3. System creates a new participant linked to the user's identity record.
4. If the Google given name ("Ana") collides with the existing guest display name,
   the system appends a suffix ("Ana 2").
5. The user now has two participant records in this sprint — the original guest
   and the new Google-linked one. Cards and votes from the guest session are
   not transferred.
```

```
Flow 5: Returning participant with existing guest token (unchanged from v1)
1. User opens the sprint link in a browser that already has a valid guest token
   for this sprint.
2. System validates the token and silently rejoins as the existing participant.
3. No join form shown — user enters the retro room directly.
4. This applies to both guest and Google-linked participants.
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | /api/sprints/{sprintId}/join | Anonymous | `{ displayName }` | Participant + guest token | 201, 400, 404, 409 |
| POST | /api/sprints/{sprintId}/join/google | Google session | — | Participant + guest token | 200, 201, 404, 409 |
| GET | /auth/google/login | Anonymous | Query: `returnUrl` | Redirect | 302 |

**POST /api/sprints/{sprintId}/join** — unchanged from v1. Guest join with a display name. No User link on the created participant.

**POST /api/sprints/{sprintId}/join/google** — new endpoint for Google-authenticated participants.

- Requires an active Google session. The existing auth policies (`ScrumMaster`, `Participant`, `OwnerOrScrumMaster`) do not semantically fit a non-SM Google user joining as a participant. The implementation plan should introduce a generalized policy or rename the existing one.
- Uses the caller's Google given name as the display name.
- If a participant linked to this user already exists in this sprint: returns the existing participant (200).
- If no existing linked participant: creates a new one linked to the user (201).
- If the given name collides with an existing display name in the sprint, appends the lowest available numeric suffix ("Ana 2", "Ana 3", etc.).
- Response shape matches the guest join endpoint — participant details including the guest token.
- 404 if the sprint does not exist.
- 409 if the sprint is not in Active state.

**GET /auth/google/login** — modified to accept an optional `returnUrl` query parameter.

- After successful authentication, redirects to the specified return URL instead of the default landing page.
- Only relative paths within the application are accepted — external URLs are rejected.
- If no return URL is specified, behavior is unchanged (redirects to the default landing page).

## SignalR Events

No new SignalR events. The existing `ParticipantJoined` event fires for both Google-authed and guest participants — no distinction in the real-time broadcast.

## Business Rules

1. A participant is either Google-linked (has a User association) or a guest (no User association). This is determined at join time and does not change.
2. Within a sprint, at most one participant can be linked to a given User. Joining a sprint where the user already has a linked participant returns the existing participant rather than creating a new one.
3. Google-authenticated participants use their Google given name (first name) as the display name. They do not choose a name. If the given name exceeds the display name length limit (50 characters), it is truncated before joining.
4. If a Google participant's given name collides with an existing display name in the sprint, the system appends the lowest available numeric suffix: "{name} 2", "{name} 3", etc. Truncation accounts for the suffix — the name is shortened enough to fit the suffix within the 50-character limit.
5. Display name uniqueness within a sprint remains enforced (case-insensitive) — same rule as v1, extended to cover auto-suffixed names.
6. A signed-in user opening a sprint link is auto-joined without seeing a join form. The join page is only shown to users without an active Google session.
7. Cross-device rejoin: when a Google-linked participant opens a sprint from a second device, the system returns the existing participant with the same guest token. Both devices operate as the same participant simultaneously.
8. Guest join is unchanged from v1: pick a display name, get a guest token, sprint-scoped. No link to a User.
9. The Scrum Master auto-join behavior is preserved — the sprint creator is auto-joined with their given name, no join form shown. With this feature, SM auto-join migrates to the same Google join path used by all signed-in users, gaining cross-device rejoin. The frontend orchestration from CreatorAutoJoin is replaced by the unified signed-in flow.
10. Kicked participants are not blocked from rejoining. Kicking a participant deletes their participant record (including the UserId link), so the unique-per-sprint constraint is not violated when a kicked Google user reopens the sprint link and is auto-joined as a new participant. This is an accepted gap for v2.
11. The Google sign-in flow supports a return URL so that participants signing in from a sprint page are redirected back to that sprint after authentication.
12. If a user previously joined a sprint as a guest and later joins the same sprint via Google, the two participant records are independent. Cards and votes from the guest session are not transferred to the Google-linked participant.

## Acceptance Criteria

- [ ] A signed-in Google user opening a sprint link is auto-joined with their Google given name — no join form shown.
- [ ] A non-signed-in user opening a sprint link sees a join page with "Sign in with Google" and "Join as Guest" options.
- [ ] Clicking "Sign in with Google" on the join page redirects to Google sign-in and back to the sprint page.
- [ ] After Google sign-in from the join page, the user is auto-joined with their given name.
- [ ] "Join as Guest" works identically to v1 — name input, guest token, no User link.
- [ ] `POST /api/sprints/{sprintId}/join/google` returns 201 and creates a participant linked to the authenticated user.
- [ ] `POST /api/sprints/{sprintId}/join/google` returns 200 with the existing participant when the user already has a linked participant in this sprint (cross-device rejoin).
- [ ] If the Google given name collides with an existing display name, the system auto-suffixes ("Ana 2", "Ana 3") and joins successfully.
- [ ] A Google-linked participant rejoining from a second device receives the same guest token and sees their existing cards and votes.
- [ ] The guest token from cross-device rejoin works for all API calls and SignalR connections.
- [ ] Both devices connected simultaneously receive real-time updates as the same participant.
- [ ] `ParticipantJoined` event fires for Google-authed participants (same as guests).
- [ ] The existing guest join endpoint continues to work unchanged — no User link on the created participant.
- [ ] `GET /auth/google/login?returnUrl=/sprint/42` redirects back to `/sprint/42` after authentication.
- [ ] The return URL parameter rejects absolute URLs and external domains (only relative paths accepted).
- [ ] SM auto-join behavior is preserved — sprint creator is auto-joined with their given name. The SM now uses the Google join path and gains cross-device rejoin.
- [ ] A kicked Google user who reopens the sprint link is auto-joined as a new participant (no rejoin block).

## Out of Scope

- **Guest-to-Google identity merge** — if someone joined as a guest and later signs in with Google in the same sprint, their guest cards and votes are not transferred to the new Google-linked participant. Card ownership migration adds complexity without clear value in v2.
- **Participant display name editing** — once joined, the display name cannot be changed. A future enhancement if needed.
- **Kicked user rejoin blocking** — kicked participants can rejoin. A blocked/banned list is a future enhancement if real usage surfaces this as a problem.
- **Multi-provider auth (Microsoft, GitHub, etc.)** — Google only for v2. Additional identity providers are a v3 decision.
- **Participant profile page** — no UI for viewing or editing participant identity beyond what Google provides.
- **Cross-sprint participant history or analytics** — the data model enables recognizing the same user across sprints (via UserId), but no UI or reporting surfaces this in v2. Deferred to v3.
- **Visual distinction between Google and guest participants** — in the retro room, both participant types appear identically. No badges, icons, or indicators differentiate them. The identity path is invisible to other participants.
