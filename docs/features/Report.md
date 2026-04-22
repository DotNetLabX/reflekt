# Report

**Traces to:** `docs/specs/v1.md` §10 (report endpoint, content, storage policy, export)
**Dependencies:** Auth, Teams, Sprints, Participants, Cards, Voting, EvidenceLinks
**Status:** Complete

---

## Purpose

The report is the output artifact of a retro — a structured summary of what the team surfaced and committed to. It filters noise (zero-vote notes), preserves signal (voted notes ranked by importance, all action items with evidence), and provides a one-click "copy as markdown" for pasting into Confluence, Slack, or team wikis.

## Entities

None. The report is computed on demand from existing sprint data. No stored entity, no snapshot, no cache.

## User Flows

```
Flow 1: View sprint report
1. User (participant or Scrum Master) navigates to the report view for a sprint
2. Client requests the report endpoint
3. Server loads the sprint, verifies it exists and the caller has access
4. Server assembles the report: sprint metadata, team metadata, participant list, voted notes sorted by vote count, all action items with their evidence links
5. Returns the assembled report as JSON
6. Frontend renders the report as a structured HTML view
```

```
Flow 2: Copy report as markdown
1. User clicks "Copy as markdown" on the report view
2. Frontend serializes the current report JSON into a markdown document
3. Markdown is placed in the system clipboard
4. User receives a confirmation toast
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | `/api/teams/{teamId}/sprints/{sprintId}/report` | Guest token OR SM cookie | — | Report JSON (see below) | 200, 401, 404 |

**Response shape:**

```json
{
  "sprint": { "id": 0, "name": "", "createdAt": "", "closedAt": null },
  "team": { "id": 0, "name": "" },
  "participants": [{ "displayName": "" }],
  "notes": {
    "wentWell": [{ "text": "", "evidenceLinks": [] }],
    "didntGoWell": [{ "text": "", "evidenceLinks": [] }]
  },
  "actionItems": [
    { "text": "", "assigneeName": "", "evidenceLinks": [] }
  ]
}
```

**Evidence link shape within cards:**

```json
{ "url": "", "source": "", "title": null, "externalKey": null }
```

**What is excluded from the response (by design):**
- Vote counts per card — ordering already conveys importance; showing counts adds noise
- Card author names — anonymous model
- Card timestamps — ordering already conveys importance
- Zero-vote notes — filtered out entirely

Error responses:
- **Sprint not found or not in the specified team:** 404
- **No valid guest token or SM cookie:** 401

## Business Rules

1. Notes (CardType = Note) appear in the report only if they have at least one vote. Zero-vote notes are excluded.
2. Notes are sorted by vote count descending within each column (wentWell, didntGoWell). Vote counts drive ordering but are not exposed in the response.
3. All action items are included regardless of votes. Action items are never filtered.
4. Action items include their evidence links (URLs, titles, external keys from enriched metadata).
5. Notes also include their evidence links.
6. The report is accessible during any sprint state — Draft, Active, or Closed. It is not gated to closed sprints.
7. Both participants (via guest token) and the Scrum Master (via cookie auth) can access the report.
8. The report is computed fresh on every request. No caching, no snapshot storage.
9. Participant list includes all participants who have joined the sprint, regardless of whether they contributed cards or votes.
10. The markdown export is a frontend-only operation — no additional backend endpoint. The client serializes the JSON response into markdown format and copies to clipboard.

## Acceptance Criteria

- [ ] `GET /api/teams/{teamId}/sprints/{sprintId}/report` returns 200 with the report JSON for a valid sprint
- [ ] Notes with zero votes are excluded from the response
- [ ] Notes are sorted by vote count descending within each column
- [ ] All action items are included regardless of vote count
- [ ] Evidence links are embedded on their parent cards (both notes and action items)
- [ ] Participant list includes all sprint participants
- [ ] Report is accessible with a guest token (participant access)
- [ ] Report is accessible with SM cookie auth
- [ ] Report returns 404 for nonexistent sprint or sprint not belonging to the specified team
- [ ] Report returns 401 without valid authentication
- [ ] Report works on Active sprints (not only Closed)
- [ ] Report works on Draft sprints (empty but valid)
- [ ] Response excludes card author names
- [ ] Response excludes card timestamps
- [ ] Frontend "Copy as markdown" places formatted markdown in the system clipboard
- [ ] Markdown output includes sprint name, team name, notes grouped by column, and action items with assignees
- [ ] Build passes with zero errors

## Out of Scope

- **PDF or CSV export** — wrong ROI for v1; markdown copy covers the paste-into-tools workflow
- **Stored report snapshots** — computed on demand is sufficient; closed sprints are immutable so no drift risk
- **Jira/Slack/email push** — integration features deferred to v2+
- **Historical report diffing** — no cross-sprint comparison in v1
- **Report access control beyond auth** — no "only SM can see the report" restriction; all sprint participants can view it
- **Vote count display prominence** — counts are in the data for sorting but the UI does not emphasize them (ordering conveys importance)
