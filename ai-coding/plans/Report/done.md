# Report — Done

## Files Created
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs` — Response records for the report shape (ReportResponse, ReportSprintInfo, ReportTeamInfo, ReportParticipantInfo, ReportNotesSection, ReportCardEntry, ReportActionItemEntry, ReportEvidenceLinkEntry)
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs` — GET endpoint at /api/teams/{teamId}/sprints/{sprintId}/report with dual-auth (guest token + SM cookie), assembles report from cards/votes/participants
- `web/reflekt-web/src/stores/report.ts` — Pinia store with fetchReport and copyAsMarkdown actions, typed interfaces for the response shape
- `web/reflekt-web/src/views/ReportView.vue` — Report view with structured sections (Went Well, Didn't Go Well, Action Items), evidence link pills, copy-to-clipboard button with toast

## Files Modified
- `web/reflekt-web/src/router/index.ts` — Added import for ReportView and route at /teams/:teamId/sprints/:sprintId/report

## Key Decisions
- Hand-mapped response records in the endpoint (no Mapster) as specified in the plan
- Evidence link pills colored to match their section (green/red/blue) following RetroRoomView's color scheme
- URL truncation in evidence links uses hostname + first 20 chars of path for readability
- Toast for copy confirmation is green (success action) vs RetroRoomView's red (error) toast

## Build Verification (Step 5)
- **Backend:** `dotnet build src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj` — Build succeeded, 0 warnings, 0 errors
- **Frontend:** `npx vue-tsc --noEmit` — Exit code 0, no type errors

## Review Cycle 1 Fixes
- **[HIGH] Auth check ordering:** Reordered endpoint to load sprint first, then auth check (participant path returns early; SM path loads team inside the else block), then data loads. Team is loaded once more after auth for response assembly. Matches ListCardsEndpoint pattern: sprint → auth → data.
- **[MEDIUM] Mapping helpers:** Moved `ToCardEntry`, `ToActionItemEntry`, `ToLinkEntry` from private endpoint methods into `public static class ReportMappings` in `ReportContracts.cs`, following the `CardMappings` pattern in `CardContracts.cs`.

## Deviations from Plan
- None
