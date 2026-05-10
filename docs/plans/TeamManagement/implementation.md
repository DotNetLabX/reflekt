# Team Management — Implementation

## Files Created

- `src/Services/Reflekt/Reflekt.Domain/Users/User.cs` — User aggregate with static factory `Create(email, name, givenName)` and `UpdateProfile(name, givenName)` method. No domain events (auth infrastructure concern). No Behaviors/ split per plan.
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/UserConfiguration.cs` — EF config: table "Users", Email max 256 with NOCASE unique index, Name max 256, GivenName max 100.
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/UserRepository.cs` — `GetByEmailAsync` using `EF.Functions.Collate(u.Email, "NOCASE")` for case-insensitive lookup.
- `src/Services/Reflekt/Reflekt.API/Auth/ClaimsPrincipalExtensions.cs` — `GetUserId()` extension: reads `ClaimTypes.NameIdentifier`, parses to int, throws `InvalidOperationException` if missing.
- `src/Services/Reflekt/Reflekt.API/Features/Teams/TeamSettings.cs` — `MaxTeamsPerUser` config POCO, default 10, bound from `TeamSettings` section.
- `src/Services/Reflekt/Reflekt.API/Features/Teams/DeleteTeam/DeleteTeamEndpoint.cs` — DELETE `/api/teams/{teamId}` with guard chain: auth → ownership → confirmation → active sprint → explicit ordered delete.
- `src/Services/Reflekt/Reflekt.API/Features/Teams/DeleteTeam/DeleteTeamCommand.cs` — request record `(int TeamId, string ConfirmationName)` with validator (ConfirmationName not empty).
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260509215050_AddUserAndMultiTeam.cs` — hand-crafted migration with custom SQL (EF-generated ordering was incorrect).
- `web/reflekt-web/src/views/TeamDashboardView.vue` — post-login landing: empty state shows CreateTeamForm inline; has-teams state shows card grid with Create button and delete dialog.
- `web/reflekt-web/src/components/teams/TeamCard.vue` — team card with sprint count tooltip, last sprint date tooltip, three-dot menu with Delete option, click navigates to team sprints.
- `web/reflekt-web/src/components/teams/DeleteTeamDialog.vue` — delete confirmation dialog: blocked state when `hasActiveSprint`, name-typing confirmation otherwise; Confirm disabled until input matches team name exactly.
- `web/reflekt-web/src/views/SprintListView.vue` — thin route wrapper: reads `teamId` from route, fetches sprints, renders existing SprintList component.

## Files Modified

- `src/Services/Reflekt/Reflekt.Domain/Teams/Team.cs` — replaced `CreatorEmail: string` with `OwnerId: int`.
- `src/Services/Reflekt/Reflekt.Domain/Teams/Behaviors/Team.cs` — factory signature `Create(string name, int ownerId)`.
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — added `DbSet<User> Users`.
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/TeamConfiguration.cs` — removed CreatorEmail config and unique index; added OwnerId FK (`OnDelete(Restrict)`), composite unique index `IX_Teams_OwnerId_Name`.
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs` — added `OnDelete(DeleteBehavior.Cascade)` to Team FK.
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — registered `UserRepository`.
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — creates User record from `Seed:SeedUserEmail` config, uses `user.Id` for team creation.
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/TeamRepository.cs` — added `CountByOwnerIdAsync`, `ExistsByOwnerAndNameAsync`, `GetTeamSummariesByOwnerAsync`, `DeleteTeamWithCascadeAsync`; added `TeamSummaryDto` record; removed CreatorEmail-based methods.
- `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs` — `OnGoogleTicketReceived` now resolves `UserRepository`, finds or creates User by email, calls `UpdateProfile` if changed, adds `ClaimTypes.NameIdentifier` claim with `user.Id.ToString()`.
- `src/Services/Reflekt/Reflekt.API/SeedOptions.cs` — renamed `CreatorEmail` → `SeedUserEmail`.
- `src/Services/Reflekt/Reflekt.API/appsettings.json` — added `TeamSettings.MaxTeamsPerUser = 10` and `Seed.SeedUserEmail = "dev@localhost"`.
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — added `services.Configure<TeamSettings>(...)`.
- `src/Services/Reflekt/Reflekt.API/Features/Teams/CreateTeam/CreateTeamEndpoint.cs` — rewritten: userId from claims, team count check, name uniqueness check, 409 with `limit_reached:` / `duplicate_name:` prefix.
- `src/Services/Reflekt/Reflekt.API/Features/Teams/GetMyTeams/GetMyTeamsEndpoint.cs` — rewritten: returns `TeamSummary` list with SprintCount, LastSprintDate, HasActiveSprint.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CreateSprint/CreateSprintEndpoint.cs` — ownership: email → userId via `GetUserId()`.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetTeamSprints/GetTeamSprintsEndpoint.cs` — same pattern.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprint/GetSprintEndpoint.cs` — same pattern.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs` — same pattern.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/StartSprint/StartSprintEndpoint.cs` — same pattern.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/CloseSprint/CloseSprintEndpoint.cs` — same pattern.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` — same pattern; kept `using System.Security.Claims` (dual-auth endpoint also reads `participantId` claim).
- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs` — ownership migrated; kept `using System.Security.Claims` (dual-auth).
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/GetGroups/GetGroupsEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs` — ownership migrated.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/AddCardToGroup/AddCardToGroupEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/RemoveCardFromGroupEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/DissolveGroupEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/FocusGroup/FocusGroupEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ClearFocus/ClearFocusEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ToggleDiscussed/ToggleDiscussedEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/Participants/GetParticipants/GetParticipantsEndpoint.cs` — ownership migrated; kept `using System.Security.Claims` (dual-auth).
- `src/Services/Reflekt/Reflekt.API/Features/Participants/KickParticipant/KickParticipantEndpoint.cs` — ownership migrated.
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/AttachEvidenceLinkEndpoint.cs` — ownership migrated; kept `using System.Security.Claims` (dual-auth).
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/DetachEvidenceLinkEndpoint.cs` — same.
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs` — ownership migrated; kept `using System.Security.Claims` (dual-auth).
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs` — same.
- `web/reflekt-web/src/stores/teams.ts` — full rewrite: `TeamSummary` interface exported, `hasTeams`/`isAtLimit`/`teamById` getters, `fetchMyTeams`/`createTeam`/`deleteTeam` actions with 409 error parsing.
- `web/reflekt-web/src/components/teams/CreateTeamForm.vue` — adapted: emits `created(team: TeamSummary)` and `cancel`; parses `limit_reached:` / `duplicate_name:` prefixes; tooltip on name input.
- `web/reflekt-web/src/router/index.ts` — added `/teams` → TeamDashboardView, `/teams/:teamId/sprints` → SprintListView; navigation guards for auth redirect.
- `web/reflekt-web/src/views/WelcomeView.vue` — simplified to sign-in only page.
- `web/reflekt-web/src/components/AppHeader.vue` — breadcrumb for `team-sprints`, `sprint`, `report` routes: "Reflekt / Teams > {teamName}".

## Key Decisions

- **User.Name/GivenName as `= default!`:** C# disallows combining `required` with `private set`. Used `= default!` sentinel with factory-only initialization instead.
- **Migration hand-crafted:** EF's auto-generated migration dropped CreatorEmail before creating Users and populating OwnerId, making data migration impossible. Rewrote with correct SQL ordering: Create Users → Add nullable OwnerId → Seed Users → Update OwnerId → Rebuild Teams table without CreatorEmail.
- **`ClaimsPrincipalExtensions.GetUserId()`:** Extracted per plan suggestion to avoid repeating `int.Parse(FindFirstValue(...))` across 20+ endpoints.
- **409 errors as string prefixes:** Used `limit_reached:` and `duplicate_name:` as prefixes in the error message string (rather than a separate `errorCode` field) to stay consistent with FastEndpoints' built-in `AddError()` / `SendErrorsAsync()` pattern. Frontend parses the prefix.
- **TeamSummaryDto in TeamRepository:** Defined the projection DTO in the same file as the repository, consistent with repository pattern in this codebase.
- **Dual-auth endpoints keep `using System.Security.Claims`:** 10 endpoints authenticate both participants (GuestToken) and ScrumMasters (Cookie). These still call `FindFirstValue("participantId")` via the System.Security.Claims namespace even after ownership check migration.

## Review Cycle 1 Fixes

- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/UserConfiguration.cs` — moved `UseCollation("NOCASE")` from index to property builder (EF Core 9 `IndexBuilder` does not expose `UseCollation`; property-level collation propagates to the index column correctly).
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260509220316_AddUserEmailNocaseCollation.cs` — new migration generated to update the snapshot with the NOCASE collation on `Users.Email`.
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/TeamRepository.cs` — wrapped `DeleteTeamWithCascadeAsync` in `BeginTransactionAsync` / `CommitAsync` to make all 7 `ExecuteDeleteAsync` calls atomic.
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260509215050_AddUserAndMultiTeam.cs` — rewrote `Down()` using raw SQL in correct order: drop composite index → rebuild Teams without OwnerId (removing FK reference to Users) → restore `IX_Teams_CreatorEmail` index → drop Users table. Added comment documenting intentional data loss of original email values. Removed `COALESCE(OwnerId, 0)` from `Up()` — NULL OwnerId now fails the NOT NULL constraint visibly rather than silently creating a row with a dangling FK.
- `web/reflekt-web/src/router/index.ts` — router guard now `async`: calls `authStore.fetchMe()` when `!authStore.loaded` before evaluating redirect logic, preventing cold-load flash of protected content. Checked that the auth store action is `fetchMe()` (not `init()`).
- `web/reflekt-web/src/components/teams/DeleteTeamDialog.vue` — added `watch(() => props.team.id, ...)` to reset `confirmationInput` and `busy` when the target team changes while the dialog is open.

## Deviations from Plan

- **SprintListView was listed as "new" in plan but a basic version already existed.** The existing file was rewritten to match the plan's requirements (fetches sprints on teamId change, uses correct route param).
- **TeamSelector.vue:** Plan said "retire or repurpose." File was not modified — it was not used by the new TeamDashboardView. No deletion was done to avoid removing something that may still be referenced elsewhere. Logged for architect attention.
