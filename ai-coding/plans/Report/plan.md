# Report

## Context

The report is the output artifact of a retrospective — a structured summary of what the team surfaced and committed to. It is computed on demand from existing sprint data (cards, votes, participants, evidence links). No new domain entities, no stored snapshots, no caching.

This feature adds one GET endpoint and a frontend view with "Copy as markdown" functionality.

**Service impacted:** Reflekt (single service).

## Scope

**In scope:**
- Backend: single GET endpoint at `/api/teams/{teamId}/sprints/{sprintId}/report`
- Backend: response contracts (records) for the report shape
- Frontend: Pinia store for report data
- Frontend: report view (new route `/teams/:teamId/sprints/:sprintId/report`)
- Frontend: "Copy as markdown" (client-side clipboard)

**Out of scope:**
- PDF/CSV export
- Stored report snapshots
- Jira/Slack push integrations
- SignalR real-time updates for the report (it's a snapshot read)
- Vote count display in the UI (ordering conveys importance)

## Domain Model Changes

None. The report is a read-only projection over existing aggregates (Sprint, Team, Card, Vote, Participant). No new entities, value objects, or domain events.

## Data Model Changes

None. No new tables, columns, or migrations.

## Implementation Steps

### Step 1: Backend — Report response contracts

Create the report-specific response records.

**Create:** `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs`

**Records to define:**

```
ReportResponse
  - Sprint: ReportSprintInfo (id, name, createdAt, closedAt)
  - Team: ReportTeamInfo (id, name)
  - Participants: List<ReportParticipantInfo> (displayName)
  - Notes: ReportNotesSection
      - WentWell: List<ReportCardEntry>
      - DidntGoWell: List<ReportCardEntry>
  - ActionItems: List<ReportActionItemEntry>

ReportCardEntry (text, evidenceLinks: List<ReportEvidenceLinkEntry>)
ReportActionItemEntry (text, assigneeName, evidenceLinks: List<ReportEvidenceLinkEntry>)
ReportEvidenceLinkEntry (url, source, title, externalKey)
```

These are flat projection records — no mapping library needed, hand-map in the endpoint.

**Pattern to follow:** `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs` — same style of sealed records with a static mappings class.

### Step 2: Backend — GetSprintReport endpoint

Create the endpoint that assembles and returns the report.

**Create:** `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs`

**Behavior:**

1. Route: `GET /api/teams/{teamId}/sprints/{sprintId}/report`
2. Auth: `[Authorize(Policy = Policies.OwnerOrScrumMaster)]` — this policy accepts both guest token and SM cookie auth. The endpoint must also verify access:
   - If caller is a participant (has `participantId` claim): verify their `sprintId` claim matches the route's `sprintId`
   - If caller is SM (has email claim): verify `team.CreatorEmail` matches the email
3. Load team by `teamId` — 404 if not found
4. Load sprint by `sprintId` — 404 if not found or `sprint.TeamId != teamId`
5. Load all cards for the sprint: `cardRepository.GetBySprintIdAsync(sprintId)` (already includes EvidenceLinks)
6. Load vote counts in bulk: `voteRepository.GetVoteCountsByCardIdsAsync(cardIds)`
7. Load participants: `participantRepository.GetBySprintIdAsync(sprintId)`
8. Assemble the response:
   - **Notes:** filter to `CardType.Note`, exclude cards with zero votes, sort by vote count descending, split into WentWell and DidntGoWell by `ColumnType`
   - **Action items:** filter to `CardType.ActionItem`, include all regardless of votes
   - Map evidence links to the slim `ReportEvidenceLinkEntry` shape (url, source, title, externalKey only)
9. Return 200 with `ReportResponse`

**Pattern to follow:** `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs` — same dual-auth check pattern, same bulk data loading approach (cards + vote counts).

**Performance approach:** All data loaded in minimal queries — `GetBySprintIdAsync` is one query with Include, `GetVoteCountsByCardIdsAsync` is one grouped query, `GetBySprintIdAsync` (participants) is one query. Three queries total, no N+1.

**Request record:** Define `GetSprintReportRequest(int TeamId, int SprintId)` inside the endpoint file (same pattern as `SprintRouteRequest` in `SprintContracts.cs`).

### Step 3: Frontend — Report store

Create a Pinia store for fetching the report.

**Create:** `web/reflekt-web/src/stores/report.ts`

**Behavior:**
- State: `report` (the response JSON, nullable)
- Action: `fetchReport(teamId, sprintId)` — GET to `/api/teams/{teamId}/sprints/{sprintId}/report` with guest token header and `credentials: 'include'`
- Action: `copyAsMarkdown()` — serializes current `report` state into markdown and calls `navigator.clipboard.writeText()`

**Auth header pattern:** Same as `card.ts` — read token from `useGuestStore()`, set `X-Guest-Token` header if present, always include `credentials: 'include'` for cookie auth fallback.

**Markdown format:**

```markdown
# {sprintName} — Sprint Report

**Team:** {teamName}
**Date:** {createdAt formatted}
**Participants:** {comma-separated display names}

## Went Well

- {text}
  - [{link title or URL}]({url})

## Didn't Go Well

- {text}
  - [{link title or URL}]({url})

## Action Items

- [ ] {text} — @{assigneeName}
  - [{link title or URL}]({url})
```

### Step 4: Frontend — Report view + route

Create the report view and register the route.

**Create:** `web/reflekt-web/src/views/ReportView.vue`

**Modify:** `web/reflekt-web/src/router/index.ts` — add route:
```
{ path: '/teams/:teamId/sprints/:sprintId/report', name: 'report', component: ReportView }
```

**View behavior:**
1. On mount: extract `teamId` and `sprintId` from route params, call `reportStore.fetchReport(teamId, sprintId)`
2. Loading state while fetching
3. Render structured report:
   - Header: sprint name, team name, date, participant list
   - "Went Well" section: list of note cards with evidence links
   - "Didn't Go Well" section: list of note cards with evidence links
   - "Action Items" section: list with assignee names and evidence links
4. "Copy as markdown" button in the header area — calls `reportStore.copyAsMarkdown()`, shows a confirmation toast
5. Error states: 404 → "Sprint not found", 401 → "Not authorized"

**Styling pattern:** Follow `RetroRoomView.vue` — Tailwind utility classes, `bg-white rounded shadow` card containers, same color scheme (green for WentWell, red for DidntGoWell, blue for ActionItems).

**Evidence link rendering:** Show as a small pill/chip below the card text — display `title` if available, fall back to truncated URL. Link is clickable (`<a>` with `target="_blank" rel="noopener"`).

### Step 5: Build verification

Run `dotnet build` for the solution and `npm run build` (or `npx vue-tsc --noEmit`) for the frontend to confirm zero errors.

**Commands:**
```bash
dotnet build D:/src/reflekt/Reflekt.sln
cd D:/src/reflekt/web/reflekt-web && npx vue-tsc --noEmit
```

## Cross-Service Changes

None. Single service, no gRPC, no integration events.

## Migration Notes

None. No data model changes.

## Testing Strategy

**Key scenarios to verify:**
1. Report returns 200 with correct structure for a sprint with mixed cards (notes + action items + evidence links)
2. Zero-vote notes are excluded
3. Notes are sorted by vote count descending within each column
4. All action items are included regardless of votes
5. Evidence links are embedded on both notes and action items
6. Participant list includes all sprint participants
7. Report accessible with guest token auth
8. Report accessible with SM cookie auth
9. Returns 404 for nonexistent sprint or sprint not in the specified team
10. Returns 401 without valid auth
11. Report works on Draft sprints (empty but valid — returns empty arrays)
12. Report works on Active sprints
13. "Copy as markdown" places correct markdown in clipboard
14. Build passes with zero errors

## Open Questions

None. The spec is unambiguous and all patterns are established in the codebase.
