# Team Management (User Entity, Multi-Team, Team Deletion)

**Traces to:** `docs/specs/v2.md` §4.0, §4.1, §4.2
**Source:** Scratch
**Dependencies:** None (this feature is the prerequisite for Google Auth for Participants)
**Status:** Ready

---

## Purpose

V1 stored Scrum Master identity as a plain email string on Team, enforced a one-team-per-email constraint, and provided no way to delete teams. V2 introduces a proper User entity, allows up to 10 teams per user, and adds team deletion with full cascade. These three capabilities form a single deliverable — the User entity enables multi-team counting, and team deletion is the cleanup valve for multi-team.

This feature also includes a data migration: existing teams get linked to newly created User records derived from their creator email values.

## Entities

### User (new)

The identity record for any Google-authenticated user. Created on first Google sign-in, matched by email on subsequent sign-ins.

| Field | Type | Constraints |
|-------|------|-------------|
| Id | int | Auto-increment primary key |
| Email | string | Required, max 256 characters, unique (case-insensitive) |
| Name | string | Required, max 256 characters, from Google profile (full display name) |
| GivenName | string | Required, max 100 characters, from Google profile (used for creator auto-join) |
| CreatedAt | DateTime | UTC, set on creation |

A User is created once and never deleted in v2. Profile fields (Name, GivenName) are updated from Google on each sign-in if they've changed.

### Team (modified)

- The creator email string field is replaced by a relationship to User (the team's owner). A Team belongs to exactly one User.
- The unique constraint on creator email is dropped — one User can own multiple Teams.
- All existing authorization checks that compared a caller's email against the creator email field migrate to use the User relationship instead.

No other Team fields change.

## User Flows

```
Flow 1: First Google sign-in (User creation)
1. User clicks "Sign in with Google" on the welcome page.
2. Google OAuth flow completes; user is redirected back.
3. System looks up a User record by the Google email (case-insensitive).
4. No match found — system creates a new User with email, name, and given name from the Google profile.
5. User lands on the empty-state team dashboard (see Flow 3a).
```

```
Flow 2: Returning Google sign-in (User update)
1. User clicks "Sign in with Google."
2. Google OAuth flow completes.
3. System finds an existing User record by email.
4. If the Google profile name or given name has changed, system updates the User record.
5. User lands on the team dashboard showing their existing teams.
```

```
Flow 3a: Team dashboard — empty state (no teams)
1. Authenticated user with zero teams lands directly on the "Create Team" form.
2. User enters a team name and submits.
3. System creates the team. User is redirected to the team's sprint list.
```

```
Flow 3b: Team dashboard — with teams
1. Authenticated user with one or more teams sees a card grid.
2. Each card shows: team name, number of sprints, date of the most recent sprint.
3. User clicks a team card to navigate to that team's sprint list.
4. A "Create Team" button is visible below the cards (disabled with tooltip if at the 10-team limit).
```

```
Flow 4: Navigate back to team list
1. User is viewing a team's sprint list.
2. Header breadcrumb shows: Teams > {Team Name}.
3. User clicks "Teams" in the breadcrumb to return to the team dashboard.
```

```
Flow 5: Create additional team
1. User clicks "Create Team" on the team dashboard.
2. A create form appears (inline or modal).
3. User enters a team name.
4. If the name duplicates an existing team owned by the same user, an inline error appears: "You already have a team called '{name}'."
5. On success, the new team card appears on the dashboard.
```

```
Flow 6: Delete team
1. User clicks the three-dot menu on a team card.
2. Menu shows "Delete team."
3. If any sprint under this team is in Active state: dialog shows "This team has an active sprint. Close or delete it before deleting the team." No delete action available.
4. If no Active sprints: confirmation dialog appears — "Delete team '{name}' and all its sprints? This cannot be undone."
5. User must type the team name exactly to enable the confirm button.
6. On confirm: team and all child data are permanently deleted. User returns to the team dashboard.
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | /api/teams | SM | — | List of team summaries | 200 |
| POST | /api/teams | SM | `{ name }` | Created team | 201, 400, 409 |
| DELETE | /api/teams/{teamId} | SM | `{ confirmationName }` | — | 204, 400, 403, 409 |
| GET | /auth/me | SM | — | User profile | 200 |

**GET /auth/me** response is unchanged from v1: email, display name, given name. The User entity's internal Id is not exposed — the backend resolves the current user from auth context on every request, so the frontend never needs to send a userId.

**GET /api/teams** response shape (changed from v1):

Each team summary includes: id, name, createdAt (retained from v1), sprint count, and last sprint date (null if no sprints exist). Existing response fields are retained; sprint count and last sprint date are new additions.

**"Last sprint date" clarification:** v2.md says "last-activity timestamps." This spec deliberately simplifies that to the creation date of the most recently created sprint — not the timestamp of the last action taken in the team. Rationale: avoids cross-entity activity tracking for a dashboard metric. Sprint creation date is a sufficient activity proxy for ≤10 teams.

**POST /api/teams** error responses:

- **400** — name is empty or exceeds 100 characters.
- **409 (duplicate name)** — the user already owns a team with the same name (case-insensitive). Message: "You already have a team called '{name}'."
- **409 (limit reached)** — the user has reached the configured team limit. Message: "Team limit reached. Delete a team to create a new one."

The 409 response body must distinguish between duplicate-name and limit-reached so the frontend can show the appropriate message.

**DELETE /api/teams/{teamId}** error responses:

- **400** — confirmation name doesn't match the team name.
- **403** — caller is not the team's owner.
- **409** — the team has any sprint in Active state. Message: "This team has an active sprint. Close or delete it before deleting the team."

**Modified endpoints (behavior changes, no route/shape changes):**

All existing team-scoped endpoints (sprint creation, sprint listing, sprint management, phase advancement, sprint closure) change their authorization check from email string comparison to User-based ownership verification.

**Navigation model change:** The post-login landing page changes from v1's single-team view (show sprints or create form) to a multi-team dashboard. The frontend route structure will need to accommodate team selection and breadcrumb-based back-navigation. The v1 assumption of "one team, go straight to sprints" no longer holds.

## Business Rules

1. A User record is created on first Google sign-in and matched by email (case-insensitive) on subsequent sign-ins.
2. User profile fields (Name, GivenName) are updated from the Google profile on each sign-in if they differ from the stored values.
3. A user can own up to 10 teams. The limit is configurable per environment.
4. Team names must be unique per user (case-insensitive comparison). Names are not globally unique — two different users can have teams with the same name.
5. Only the team's owner can delete it.
6. Team deletion permanently removes the team and all child data (sprints, participants, card groups, cards, votes, evidence links) in a single transaction.
7. A team with any sprint in Active state cannot be deleted. Draft and Closed sprints do not block deletion. This prevents destroying a team mid-retro while participants are connected.
8. To confirm deletion, the user must type the exact team name. The confirm button is disabled until the input matches.
9. The "Create Team" button is disabled (not hidden) when the user has reached the team limit. A tooltip explains why: "You've reached the maximum of 10 teams. Delete a team to create a new one."
10. The team dashboard shows sprint count and the creation date of the most recent sprint for each team. If a team has no sprints, the date field shows nothing.

## Acceptance Criteria

- [ ] First Google sign-in creates a User record with email, name, and given name from the Google profile.
- [ ] Subsequent sign-ins match the existing User by email (case-insensitive) and update name/given name if changed.
- [ ] `GET /api/teams` returns all teams owned by the authenticated user, each with sprint count and last sprint date.
- [ ] `POST /api/teams` creates a team owned by the authenticated user and returns 201.
- [ ] `POST /api/teams` returns 409 with a distinguishable error when the user already owns a team with the same name (case-insensitive).
- [ ] `POST /api/teams` returns 409 with a distinguishable error when the user has reached the configured team limit (default 10).
- [ ] A user can create 10 teams; the 11th attempt returns 409.
- [ ] `DELETE /api/teams/{teamId}` returns 204 and removes the team and all child data (sprints, participants, card groups, cards, votes, evidence links).
- [ ] `DELETE /api/teams/{teamId}` returns 409 when any sprint under the team is in Active state.
- [ ] `DELETE /api/teams/{teamId}` succeeds when the team has only Draft and/or Closed sprints (Draft sprints are cascade-deleted).
- [ ] `DELETE /api/teams/{teamId}` returns 400 when the confirmation name doesn't match.
- [ ] `DELETE /api/teams/{teamId}` returns 403 when the caller is not the team's owner.
- [ ] All existing team-scoped endpoints continue to work with User-based ownership verification.
- [ ] The team dashboard shows a card grid when the user owns one or more teams.
- [ ] The team dashboard shows the create form directly when the user owns zero teams.
- [ ] The "Create Team" button is disabled with a tooltip when the user owns 10 teams.
- [ ] Header breadcrumb shows "Teams > {Team Name}" when viewing a team's sprint list; clicking "Teams" navigates back to the dashboard.
- [ ] Team card shows a three-dot menu with "Delete team" option.
- [ ] Delete confirmation dialog requires typing the team name to enable the confirm button.
- [ ] Deleting a team removes its card from the dashboard immediately.
- [ ] Existing v1 data is migrated: each unique creator email gets a User record, and existing teams are linked to the corresponding User.

## Out of Scope

- **Team member invites or role hierarchy** — v2 is single-owner per team. No invite flow, no multi-admin, no shared ownership.
- **Team ownership transfer** — no use case identified for v2.
- **Soft delete or restore period** — hard delete is the v2 decision. Lightweight tool, no audit trail requirement.
- **Team renaming** — name is set at creation. Renaming is a future enhancement.
- **User profile page** — no UI for viewing or editing the User record. Profile data comes from Google.
- **User deletion or account management** — out of scope for v2.
- **Participant linking to User** — that is the Google Auth for Participants feature (v2.md §4.3), a separate spec.
