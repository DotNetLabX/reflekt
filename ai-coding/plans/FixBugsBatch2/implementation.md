# FixBugsBatch2 — Implementation

## Files Created
_(none)_

## Files Deleted
- `src/Services/Reflekt/Reflekt.API/Auth/AllowlistChecker.cs` — dead code after removing allowlist gate
- `src/Services/Reflekt/Reflekt.API/Auth/AuthOptions.cs` — only held AllowedEmails, no longer needed
- `tests/Reflekt.UnitTests/Auth/AllowlistCheckerTests.cs` — tests for removed code
- `web/reflekt-web/src/views/NotInvitedView.vue` — "not invited" page, no longer reachable

## Files Modified
- `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs` — removed allowlist check from `OnGoogleTicketReceived`; any Google-authenticated user now gets a session
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — removed `Configure<AuthOptions>` registration
- `src/Services/Reflekt/Reflekt.API/Features/Teams/CreateTeam/CreateTeamEndpoint.cs` — removed allowlist check; team creation now gated only by `[Authorize(Policy = ScrumMaster)]`
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — seed creator email sourced from `Seed:CreatorEmail` config instead of `Auth:AllowedEmails`
- `src/Services/Reflekt/Reflekt.API/SeedOptions.cs` — added `CreatorEmail` property
- `src/Services/Reflekt/Reflekt.API/appsettings.json` — removed `AllowedEmails` from Auth section
- `src/Services/Reflekt/Reflekt.API/appsettings.Development.json` — moved creator email to `Seed:CreatorEmail`, removed `AllowedEmails`
- `web/reflekt-web/src/router/index.ts` — removed `/not-invited` route and import
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` — added `VotesPerParticipant` to `SprintSummaryResponse`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` — passes `sprint.VotesPerParticipant` to response
- `web/reflekt-web/src/stores/sprint.ts` — added `votesPerParticipant` to `SprintSummary` interface
- `web/reflekt-web/src/stores/vote.ts` — `votesPerParticipant` still defaults to 3, but is now overwritten by actual value at room entry
- `web/reflekt-web/src/views/RetroRoomView.vue` — three changes: (1) `enterRoom` fetches sprint summary and sets vote budget from it; (2) `isReportingPhase` watcher guarded by `joined.value` to prevent race condition; (3) report fetch triggered at end of `enterRoom` if already in Reporting phase

## Key Decisions
- Removed `AuthOptions` entirely rather than keeping an empty class — no remaining consumers
- Used `Seed:CreatorEmail` config for the DB seeder rather than hardcoding — keeps it configurable per environment
- Fixed report race condition by guarding the watcher + triggering from `enterRoom`, rather than adding a retry or moving the fetch elsewhere

## Deviations from Plan
_(no formal plan — solo conversation-driven fixes)_
