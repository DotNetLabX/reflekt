# Team Management (User Entity, Multi-Team, Team Deletion)

## Context

V1 stored Scrum Master identity as a plain `CreatorEmail` string on Team, enforced a one-team-per-email constraint, and provided no way to delete teams. V2 introduces a proper User entity (created on first Google sign-in), allows up to 10 teams per user, and adds team deletion with full cascade. This is a single deliverable — the User entity enables multi-team counting, and team deletion is the cleanup valve for multi-team.

**Service impacted:** Reflekt (all three projects: Domain, Persistence, API) + frontend (`web/reflekt-web`).

The ownership check pattern `string.Equals(team.CreatorEmail, email, OrdinalIgnoreCase)` is scattered across 20+ endpoints. All must migrate to User-based ownership (`team.OwnerId == currentUser.Id`).

## Scope

**In scope:**
- User aggregate (Domain + Persistence + API creation on sign-in)
- Team aggregate modification (CreatorEmail → OwnerId FK)
- Multi-team support (up to 10 teams per user, configurable)
- Team name uniqueness per user (case-insensitive)
- Team deletion with cascade (blocked by Active sprints)
- Data migration (existing teams linked to new User records by email)
- GET /api/teams enriched with sprint count + last sprint date
- Frontend: team dashboard (card grid, empty state, create, delete), breadcrumb navigation
- Ownership check migration across all existing endpoints
- Help tooltips wiring per `docs/features/TeamManagement/help.tooltips.md`

**Out of scope:**
- Team member invites, shared ownership, ownership transfer
- Soft delete / restore
- Team renaming
- User profile page, user deletion
- Participant linking to User (separate feature)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | create-aggregate | Follow | User aggregate, EF Core variant, no behavior split needed (plain entity) | |
| 2 | domain-patterns | Follow | Modify Team aggregate: replace CreatorEmail with OwnerId, update factory | |
| 3 | persistence-patterns | Follow | UserConfiguration, TeamConfiguration update, UserRepository | |
| 4 | persistence-patterns | Follow | Data migration for existing teams → User records | |
| 5 | (none) | — | Wire User creation into Google auth callback | |
| 6 | create-feature | Follow | CreateTeam endpoint rewrite (multi-team rules) | |
| 7 | create-feature | Follow | GetMyTeams endpoint rewrite (enriched response) | |
| 8 | create-feature | Follow | DeleteTeam endpoint (new) | |
| 9 | (none) | — | Ownership check migration across 20+ endpoints | |
| 10 | pinia-patterns | Follow | Teams store rewrite for multi-team | |
| 11 | vue-patterns | Follow | Team dashboard view + components | |
| 12 | vue-patterns | Follow | Navigation model (router, breadcrumbs, delete flow) | |

## Domain Model Changes

### New: User aggregate

```
User : AggregateRoot
├── Email: string (required, max 256, unique case-insensitive)
├── Name: string (required, max 256)
├── GivenName: string (required, max 100)
└── CreatedAt: DateTime (UTC)
```

User is a simple identity record. No domain events needed — creation happens in the auth callback, not from domain logic that other aggregates react to. No behavior split (no Behaviors/ partial class) since there's only one mutation: UpdateProfile (update name/givenName if changed).

### Modified: Team aggregate

- **Remove:** `CreatorEmail` property
- **Add:** `OwnerId` (int, required FK → User)
- **Modify factory:** `Team.Create(string name, int ownerId)` replaces `Team.Create(string name, string creatorEmail)`

No `CanDelete` domain method — the active-sprint check is a query-level guard in the DeleteTeam endpoint (the Team aggregate has no knowledge of Sprint state). This avoids coupling Team to Sprint's state enum.

### New: Domain event (none needed)

The TeamCreatedEvent already exists and passes the Team reference. No new events required.

## Data Model Changes

### New table: Users

| Column | Type | Constraints |
|--------|------|-------------|
| Id | int | PK, auto-increment |
| Email | string(256) | NOT NULL, unique index (case-insensitive) |
| Name | string(256) | NOT NULL |
| GivenName | string(100) | NOT NULL |
| CreatedAt | DateTime | NOT NULL |

### Modified table: Teams

- **Remove column:** CreatorEmail
- **Add column:** OwnerId (int, NOT NULL, FK → Users.Id)
- **Remove index:** IX_Teams_CreatorEmail (unique)
- **Add index:** IX_Teams_OwnerId_Name (unique, composite — enforces per-user name uniqueness)

### Migration strategy

Single migration that:
1. Creates Users table
2. Adds OwnerId column (nullable initially)
3. Seeds User records from distinct CreatorEmail values
4. Updates Teams.OwnerId from the newly created Users
5. Makes OwnerId NOT NULL
6. Drops CreatorEmail column
7. Adds composite unique index

## Implementation Steps

### Step 1 — Create User aggregate

Create the User entity in the domain layer. Since User has minimal behavior (just profile update), it does not need a Behaviors/ partial class split. Use a static factory for creation with guards, and a simple method for profile update.

**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Users/User.cs`

**Skill:** Follow `create-aggregate` (EF Core variant)

**Feature-specific inputs:**
- Aggregate: `User` extending `AggregateRoot`
- Properties: `Email` (string, required), `Name` (string, required), `GivenName` (string, required), `CreatedAt` (DateTime, required)
- Factory: `static User Create(string email, string name, string givenName)` — sets `CreatedAt = DateTime.UtcNow`, validates non-empty inputs
- Method: `void UpdateProfile(string name, string givenName)` — updates Name and GivenName if changed; no-op if values are same
- No domain events raised (User creation is an infrastructure concern, not a domain action that other aggregates react to)
- No Behaviors/ split — single file with both state and methods since this is minimal

**Depends on:** Nothing

---

### Step 2 — Modify Team aggregate for User-based ownership

Replace the `CreatorEmail` string property with an `OwnerId` int FK. Update the factory method signature.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Domain/Teams/Team.cs` — replace `CreatorEmail` with `OwnerId`
- `src/Services/Reflekt/Reflekt.Domain/Teams/Behaviors/Team.cs` — update `Create` factory signature to accept `int ownerId` instead of `string creatorEmail`

**Skill:** Follow `domain-patterns`

**Feature-specific inputs:**
- Remove: `public required string CreatorEmail { get; init; }`
- Add: `public required int OwnerId { get; init; }`
- Factory becomes: `public static Team Create(string name, int ownerId)` — validates name non-empty, sets OwnerId, CreatedAt, raises TeamCreatedEvent

**Note:** This step will break compilation across 20+ endpoints. Steps 6-9 fix these references. The developer should batch steps 1-5 before attempting a build.

**Depends on:** Step 1

---

### Step 3 — Persistence layer changes (User config, Team config update, UserRepository)

Create the EF configuration for User, update Team configuration to replace CreatorEmail with OwnerId FK, and create UserRepository.

**Files to create:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/UserConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/UserRepository.cs`

**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `DbSet<User> Users`
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/TeamConfiguration.cs` — remove CreatorEmail config, add OwnerId FK + composite unique index
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `UserRepository`

**Skill:** Follow `persistence-patterns`

**Feature-specific inputs:**

UserConfiguration:
- Table: "Users"
- `Email`: required, max 256, unique index with case-insensitive collation (`NOCASE` for SQLite)
- `Name`: required, max 256
- `GivenName`: required, max 100
- `CreatedAt`: required

TeamConfiguration changes:
- Remove: `builder.Property(t => t.CreatorEmail)...` and `builder.HasIndex(t => t.CreatorEmail).IsUnique()`
- Add: `builder.Property(t => t.OwnerId).IsRequired()`
- Add: `builder.HasOne<User>().WithMany().HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Restrict)` (Restrict on User→Team: we never cascade-delete users)
- Add: `builder.HasIndex(t => new { t.OwnerId, t.Name }).IsUnique()` (SQLite NOCASE collation on Name for case-insensitive uniqueness)

SprintConfiguration change (in same step — same file area):
- Modify: `builder.HasOne<Team>().WithMany().HasForeignKey(s => s.TeamId)` → add `.OnDelete(DeleteBehavior.Cascade)`
- This enables cascade from Team deletion to all Sprints under it

**Cascade chain analysis for team deletion:**
The full cascade path is: Team → Sprint (Cascade) → Participant (Cascade), Card (Cascade), CardGroup (Cascade). Then: Card → EvidenceLink (Cascade), CardGroup → Vote (Cascade).

**Restrict FK interaction:** Card→Participant and Vote→Participant use `OnDelete(DeleteBehavior.Restrict)`. This could block deletion if Participants are deleted before Cards/Votes. However, SQLite's FK enforcement processes all cascades from the same trigger before checking Restrict constraints — when a Sprint is deleted, Cards, CardGroups, and Participants are all cascade-deleted in the same operation. Cards and Votes are removed via their own cascade paths (Card→Sprint, Vote→CardGroup→Sprint) before the Restrict on Participant is evaluated.

**Safety measure:** Despite SQLite handling this correctly, the DeleteTeam endpoint (Step 8) will use **explicit ordered deletion** rather than relying on cascade from `DbContext.Remove(team)`. This ensures correctness regardless of DB engine behavior: delete Votes → Cards → EvidenceLinks → CardGroups → Participants → Sprints → Team. The cascade config remains as documentation of intent and as a safety net.

**Files to additionally modify in this step:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs` — add `OnDelete(DeleteBehavior.Cascade)` to Team FK

UserRepository:
- Extends `Repository<User>`
- Method: `GetByEmailAsync(string email, CancellationToken ct)` — case-insensitive lookup using `EF.Functions.Collate(u.Email, "NOCASE")`

**Depends on:** Steps 1, 2

---

### Step 4 — Data migration

Create an EF Core migration that introduces the Users table, seeds User records from existing Teams.CreatorEmail values, links teams to users, then drops CreatorEmail.

**Files to create:**
- Migration file (auto-generated via `dotnet ef migrations add`)

**However**, because the migration involves data transformation (creating Users from distinct emails, then updating Team.OwnerId), this requires a custom migration with raw SQL rather than pure schema changes.

**Skill:** Follow `persistence-patterns` (migration section)

**Migration SQL logic (SQLite):**
```sql
-- 1. Create Users table
CREATE TABLE "Users" (...);

-- 2. Add nullable OwnerId to Teams
ALTER TABLE "Teams" ADD COLUMN "OwnerId" INTEGER NULL;

-- 3. Insert distinct users from existing teams
INSERT INTO "Users" ("Email", "Name", "GivenName", "CreatedAt")
SELECT DISTINCT "CreatorEmail", "CreatorEmail", "CreatorEmail", datetime('now')
FROM "Teams";

-- 4. Link teams to users
UPDATE "Teams" SET "OwnerId" = (
    SELECT "Id" FROM "Users" WHERE "Users"."Email" = "Teams"."CreatorEmail" COLLATE NOCASE
);

-- 5. Make OwnerId required (SQLite: recreate table without the column being nullable)
-- Note: SQLite doesn't support ALTER COLUMN. EF Core handles this via table rebuild.

-- 6. Drop CreatorEmail (handled by EF table rebuild)

-- 7. Create indexes
```

**Note:** For the data migration, User.Name and GivenName are populated with the email value as a placeholder — the real Google profile data will be populated on next sign-in (per spec: "Profile fields are updated from Google on each sign-in if they've changed"). This is acceptable because the system has no user-facing profile display that would show stale data.

**Migration command:**
```bash
dotnet ef migrations add AddUserAndMultiTeam -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
```

**Depends on:** Step 3

---

### Step 5 — Wire User creation into Google auth callback

Modify the `OnGoogleTicketReceived` callback in `AuthenticationConfiguration.cs` to find-or-create a User record and store the User.Id in the ClaimsPrincipal. This allows all endpoints to resolve the current user's Id from claims without a DB query.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs` — `OnGoogleTicketReceived` handler: resolve UserRepository from DI, find or create User by email, add a custom claim (`ClaimTypes.NameIdentifier` = User.Id)
- `src/Services/Reflekt/Reflekt.API/Features/Auth/Me/MeEndpoint.cs` — response unchanged (email, displayName, givenName), but internally verify User exists

**Implementation approach:**
1. In `OnGoogleTicketReceived`:
   - Extract email, name, givenName from the ticket's principal claims
   - Get `UserRepository` from `ctx.HttpContext.RequestServices`
   - Lookup user by email (case-insensitive)
   - If not found: create User via `User.Create(email, name, givenName)`, save
   - If found: call `user.UpdateProfile(name, givenName)` if changed, save
   - Add `ClaimTypes.NameIdentifier` claim with user.Id.ToString() to the identity
2. In subsequent requests: `User.FindFirstValue(ClaimTypes.NameIdentifier)` gives the int userId without a DB lookup

**Guard ordering:** Auth middleware runs first (cookie validates), then the custom claim is available in every endpoint.

**Depends on:** Steps 3, 4

---

### Step 6 — Rewrite CreateTeam endpoint (multi-team rules)

Replace the current single-team CreateTeam endpoint with multi-team logic: team limit check, per-user name uniqueness, 409 with distinguishable error codes.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Teams/CreateTeam/CreateTeamEndpoint.cs` — full rewrite
- `src/Services/Reflekt/Reflekt.API/Features/Teams/CreateTeam/CreateTeamCommand.cs` — response unchanged

**Skill:** Follow `create-feature` (FastEndpoints variant)

**Feature-specific inputs:**
- Route: `POST /api/teams` (unchanged)
- Auth: `Policies.ScrumMaster`
- Request: `CreateTeamCommand(string Name)` (unchanged)
- Response: `CreateTeamResponse(int Id, string Name, DateTime CreatedAt)` (unchanged)
- Validator: `Name` not empty, max 100 chars (unchanged)

**Handler logic:**
1. Extract userId from `ClaimTypes.NameIdentifier`
2. Get user's teams count via `teamRepository.CountByOwnerIdAsync(userId, ct)`
3. If count >= configured limit (default 10): return 409 with error code "limit_reached" and message "Team limit reached. Delete a team to create a new one."
4. Check name uniqueness: `teamRepository.ExistsByOwnerAndNameAsync(userId, name, ct)` (case-insensitive)
5. If duplicate: return 409 with error code "duplicate_name" and message "You already have a team called '{name}'."
6. Create team: `Team.Create(name, userId)`
7. Save and return 201

**409 response shape** (must distinguish errors for frontend):
```json
{ "errors": { "generalErrors": ["message"] }, "statusCode": 409, "errorCode": "limit_reached" | "duplicate_name" }
```
Use FastEndpoints' `AddError()` + `SendErrorsAsync(409)` pattern. Add a custom header or response property (`X-Error-Code` or inline in the error list) to distinguish. Recommended: use the error message itself as the distinguisher — the frontend already parses error messages. Alternatively, add `errorCode` to the response if FastEndpoints supports custom error shapes.

**Configuration:** Add `TeamSettings:MaxTeamsPerUser` to appsettings.json (default 10). Inject via `IOptions<TeamSettings>` or read from `IConfiguration`.

**Depends on:** Step 5

---

### Step 7 — Rewrite GetMyTeams endpoint (enriched response)

Add sprint count and last sprint date to each team summary.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Features/Teams/GetMyTeams/GetMyTeamsEndpoint.cs` — full rewrite

**Skill:** Follow `create-feature` (FastEndpoints variant, query-only pattern — no request body)

**Feature-specific inputs:**
- Route: `GET /api/teams` (unchanged)
- Auth: `Policies.ScrumMaster`
- Response shape (changed):
  ```
  TeamSummary(int Id, string Name, DateTime CreatedAt, int SprintCount, DateTime? LastSprintDate, bool HasActiveSprint)
  ```
- `HasActiveSprint`: true if any sprint under this team has `State == Active`. Enables the frontend to show the "blocked" state in the delete dialog without attempting a DELETE first (spec Flow 6 pre-check UX).
- No request body or validator needed

**Handler logic:**
1. Extract userId from `ClaimTypes.NameIdentifier`
2. Query teams with sprint aggregation — single query joining Teams + Sprints:
   ```sql
   SELECT t.*,
     COUNT(s.Id) as SprintCount,
     MAX(s.CreatedAt) as LastSprintDate,
     MAX(CASE WHEN s.State = 'Active' THEN 1 ELSE 0 END) as HasActiveSprint
   FROM Teams t LEFT JOIN Sprints s ON s.TeamId = t.Id
   WHERE t.OwnerId = @userId
   GROUP BY t.Id
   ```
3. Return list of TeamSummary

**Repository method to add:**
- `TeamRepository.GetTeamSummariesByOwnerAsync(int ownerId, CancellationToken ct)` — returns projected DTOs directly or teams + aggregation data. Includes the HasActiveSprint flag in the projection.

**Depends on:** Step 5

---

### Step 8 — Create DeleteTeam endpoint

New endpoint for team deletion with cascade, confirmation check, and active sprint guard.

**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Teams/DeleteTeam/DeleteTeamEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Teams/DeleteTeam/DeleteTeamCommand.cs`

**Skill:** Follow `create-feature` (FastEndpoints variant)

**Feature-specific inputs:**
- Route: `DELETE /api/teams/{teamId}`
- Auth: `Policies.ScrumMaster`
- Request: `DeleteTeamCommand(int TeamId, string ConfirmationName)` — TeamId from route, ConfirmationName from body
- Response: none (204 No Content)
- Validator: ConfirmationName not empty

**Handler logic (guard ordering: auth → ownership → confirmation → state → delete):**
1. Extract userId from `ClaimTypes.NameIdentifier`
2. Find team by id — 404 if not found
3. Ownership check: `team.OwnerId == userId` — 403 if not owner
4. Confirmation check: `team.Name == command.ConfirmationName` (exact match, case-sensitive) — 400 if mismatch
5. Active sprint check: query sprints for this team where `State == Active` — 409 if any exist, message: "This team has an active sprint. Close or delete it before deleting the team."
6. **Explicit ordered deletion** to avoid Restrict FK conflicts (see Step 3 cascade analysis):
   - Load all sprint IDs for the team
   - Delete Votes (by GroupId via CardGroups belonging to those sprints)
   - Delete EvidenceLinks (by CardId via Cards belonging to those sprints)
   - Delete Cards (by SprintId)
   - Delete CardGroups (by SprintId)
   - Delete Participants (by SprintId)
   - Delete Sprints (by TeamId)
   - Delete the Team
   - All in a single `SaveChangesAsync` call (single transaction)
7. Return 204

**Why explicit deletion:** The Restrict FKs (Card→Participant, Vote→Participant) mean deletion order matters. While SQLite handles cascades correctly in practice, explicit ordering guarantees correctness regardless of engine and makes the intent clear. Use `ExecuteDeleteAsync()` (EF Core bulk delete) for each entity type in order — avoids loading all entities into memory.

**Repository method to add:** `TeamRepository.DeleteTeamWithCascadeAsync(int teamId, CancellationToken ct)` — encapsulates the ordered bulk deletion.

**Depends on:** Step 5

---

### Step 9 — Migrate ownership checks across all existing endpoints

Replace `string.Equals(team.CreatorEmail, email, OrdinalIgnoreCase)` with `team.OwnerId == userId` across all team-scoped endpoints. The userId comes from `User.FindFirstValue(ClaimTypes.NameIdentifier)` parsed to int.

**Files to modify (all contain CreatorEmail ownership checks):**

Sprint endpoints:
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetTeamSprints/GetTeamSprintsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprint/GetSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/StartSprint/StartSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CloseSprint/CloseSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs`

Card endpoints:
- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs`

CardGroup endpoints:
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/GetGroups/GetGroupsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/AddCardToGroup/AddCardToGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/RemoveCardFromGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/DissolveGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/FocusGroup/FocusGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ClearFocus/ClearFocusEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ToggleDiscussed/ToggleDiscussedEndpoint.cs`

Participant endpoints:
- `src/Services/Reflekt/Reflekt.API/Features/Participants/GetParticipants/GetParticipantsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/KickParticipant/KickParticipantEndpoint.cs`

EvidenceLink endpoints:
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/DetachEvidenceLinkEndpoint.cs`

Report endpoints:
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs`

Other:
- `src/Services/Reflekt/Reflekt.API/SeedOptions.cs` — update seed config if it uses CreatorEmail

**Pattern replacement:**

Before:
```csharp
var email = User.FindFirstValue(ClaimTypes.Email);
// ...
if (!string.Equals(team.CreatorEmail, email, StringComparison.OrdinalIgnoreCase))
{
    await SendForbiddenAsync(ct);
    return;
}
```

After:
```csharp
var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
// ...
if (team.OwnerId != userId)
{
    await SendForbiddenAsync(ct);
    return;
}
```

**Note:** Consider extracting a helper method or extension (`HttpContext.GetUserId()`) to avoid repeating the parse logic in every endpoint. Place in `src/Services/Reflekt/Reflekt.API/Auth/ClaimsPrincipalExtensions.cs`.

**Depends on:** Steps 2, 5

---

### Step 10 — Rewrite teams store for multi-team support

Replace the current single-team store with multi-team support: list of teams with summaries, team CRUD actions, selected team tracking.

**Files to modify:**
- `web/reflekt-web/src/stores/teams.ts` — full rewrite

**Skill:** Follow `pinia-patterns`

**Feature-specific inputs:**

State:
- `teams: TeamSummary[]` — full list from API
- `loaded: boolean`

Interface:
```typescript
interface TeamSummary {
  id: number
  name: string
  createdAt: string
  sprintCount: number
  lastSprintDate: string | null
  hasActiveSprint: boolean
}
```

Getters:
- `hasTeams` — teams.length > 0
- `isAtLimit` — teams.length >= 10
- `teamById(id: number)` — find specific team

Actions:
- `fetchMyTeams()` — GET /api/teams
- `createTeam(name: string): Promise<TeamSummary>` — POST /api/teams, push to list on success, throw parsed error on 409
- `deleteTeam(teamId: number, confirmationName: string): Promise<void>` — DELETE /api/teams/{teamId}, remove from list on success, throw parsed error on 400/403/409

**Error handling for 409:**
Parse the error response to distinguish `duplicate_name` vs `limit_reached` and surface the appropriate message to the UI.

**Depends on:** Steps 6, 7, 8

---

### Step 11 — Team dashboard view + components

Build the team dashboard: card grid when teams exist, inline create form when no teams, create button with limit tooltip, three-dot menu with delete option, delete confirmation dialog.

**Files to create:**
- `web/reflekt-web/src/views/TeamDashboardView.vue` — new view (replaces WelcomeView as post-login landing)
- `web/reflekt-web/src/components/teams/TeamCard.vue` — individual team card
- `web/reflekt-web/src/components/teams/DeleteTeamDialog.vue` — confirmation dialog with name-typing requirement

**Files to modify:**
- `web/reflekt-web/src/components/teams/CreateTeamForm.vue` — adapt for dashboard context (not just first-team flow)
- `web/reflekt-web/src/components/teams/TeamSelector.vue` — retire or repurpose (currently the WelcomeView wrapper)

**Skill:** Follow `vue-patterns`

**Feature-specific inputs:**

TeamDashboardView:
- If not signed in: redirect to welcome/sign-in (or show sign-in button)
- If signed in + no teams: show CreateTeamForm directly (Flow 3a)
- If signed in + has teams: show card grid + "Create Team" button below

TeamCard:
- Props: `team: TeamSummary`
- Displays: team name, sprint count (with tooltip from help.tooltips.md), last sprint date (with tooltip)
- Three-dot menu (with tooltip): "Delete team" option
- Click navigates to team sprints: `/teams/{teamId}/sprints`
- Emits: `delete(teamId: number)`

CreateTeamForm (modified):
- Shows inline error for 409 responses (duplicate name or limit reached)
- Tooltip on team name input per help.tooltips.md

DeleteTeamDialog:
- Props: `team: TeamSummary`, `open: boolean`
- If `team.hasActiveSprint` is true: show blocked message "This team has an active sprint. Close or delete it before deleting the team." (tooltip text from help.tooltips.md). No confirmation input shown, only a close button.
- Otherwise: show "Delete team '{name}' and all its sprints? This cannot be undone."
- Input for confirmation name typing (with tooltip from help.tooltips.md)
- Confirm button disabled until input matches team name exactly
- Emits: `confirm(teamId: number, confirmationName: string)`, `close`

"Create Team" button:
- Disabled with tooltip when `isAtLimit` (tooltip text from help.tooltips.md: "You've reached the maximum of 10 teams. Delete a team to create a new one.")
- Enabled tooltip (from help.tooltips.md: "Create a new team. You can manage up to 10 teams.")

**Depends on:** Step 10

---

### Step 12 — Navigation model (router, breadcrumbs, delete flow)

Update the router to support the multi-team navigation model: team dashboard as post-login landing, team-scoped sprint list, breadcrumb navigation.

**Files to create:**
- `web/reflekt-web/src/views/SprintListView.vue` — thin view wrapper that renders the existing `SprintList` component within a layout that includes the breadcrumb header ("Teams > {Team Name}"). Fetches sprints for the team from route param `:teamId`. Reuses existing `SprintList` and `CreateSprintForm` components.

**Files to modify:**
- `web/reflekt-web/src/router/index.ts` — add TeamDashboardView route, add team sprints route pointing to SprintListView
- `web/reflekt-web/src/views/WelcomeView.vue` — simplify to sign-in only (not team selector)
- `web/reflekt-web/src/components/AppHeader.vue` — add breadcrumb: "Teams > {Team Name}" when viewing a team's sprint list
- `web/reflekt-web/src/App.vue` — may need layout adjustment for breadcrumb

**Skill:** Follow `vue-patterns`

**Feature-specific inputs:**

Router changes:
- `/` — WelcomeView (sign-in page for unauthenticated)
- `/teams` — TeamDashboardView (authenticated, team list or create form)
- `/teams/:teamId/sprints` — SprintListView (team's sprint list, shows breadcrumb)
- `/teams/:teamId/sprints/:sprintId` — SprintView (existing, unchanged)
- `/sprint/:sprintId` — RetroRoomView (existing, unchanged)

Navigation guards:
- Redirect authenticated users from `/` to `/teams`
- Redirect unauthenticated users from `/teams` to `/`

AppHeader breadcrumb:
- When route matches `/teams/:teamId/sprints*`: show "Teams > {teamName}" breadcrumb
- "Teams" link navigates to `/teams`
- Team name is resolved from the teams store

Post-delete behavior:
- After successful team deletion, team card removed optimistically, user stays on dashboard

**Depends on:** Step 11

---

## Cross-Service Changes

None. Single-service project.

## Migration Notes

**Command:**
```bash
dotnet ef migrations add AddUserAndMultiTeam -p Services/Reflekt/Reflekt.Persistence -s Services/Reflekt/Reflekt.API
```

**Data migration notes:**
- The migration must handle existing data: create User records from distinct CreatorEmail values
- User.Name and GivenName are set to the email as placeholder — updated on next Google sign-in
- SQLite limitation: ALTER COLUMN not supported. EF Core handles this via table rebuild (drop + recreate with new schema)
- Verify cascade delete configuration works for the full chain: Team → Sprint → Participant/Card/CardGroup/Vote/EvidenceLink

**Seed data (dev):**
- If `DbInitializer` / `SeedOptions` uses `CreatorEmail`, update to create a User record first and use OwnerId
- `src/Services/Reflekt/Reflekt.API/SeedOptions.cs` references `CreatorEmail` — update

## Testing Strategy

**Backend:**
- User creation on first sign-in (mock Google callback)
- User profile update on subsequent sign-in
- Create team: success (201), duplicate name (409), limit reached (409), validation (400)
- Delete team: success (204), not owner (403), active sprint (409), wrong confirmation (400), not found (404)
- GET /api/teams: returns enriched summaries with sprint count + last sprint date
- All existing endpoints work with new ownership model (regression)
- Data migration: existing teams are properly linked to User records

**Frontend:**
- Team dashboard shows cards when teams exist
- Empty state shows create form
- Create team button disabled with tooltip at limit
- Delete flow: dialog, name confirmation, success removes card
- Breadcrumb navigation works
- Sign-in creates user and redirects to dashboard

## Open Questions

None. All decisions are captured in the spec.
