# Technical Debts & Known Bugs

Log of findings that aren't critical enough to block work but should be revisited.

Format: one entry per finding. Include file:line, severity, and what to do.

---

## Open

### TD-001 — Anonymous sprint summary endpoint is over-exposed
**Severity:** Low
**Source:** Codex review, 2026-04-21
**File:** `src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs`

`GET /api/sprints/{id}/summary` is `[AllowAnonymous]` and returns `{id, name}` for any sprint by ID. An unauthenticated caller who guesses sequential IDs can enumerate sprint names (including Draft/Closed sprints that are otherwise guarded by scrum-master/guest policies).

**Why it exists:** Needed by the pre-join name picker in `RetroRoomView.vue` to show the sprint name before a participant has a guest token.

**Why it's acceptable for now:** The app uses a "Google Docs" invite model — anyone with the sprint ID can attempt to join anyway. Sprint names are not highly sensitive in this retro-tool context.

**Suggested fix when revisited:** Either (a) gate the endpoint to `State == Active` and return 404 otherwise, or (b) require an auth scheme (cookie OR guest token matching the sprint), so draft/closed sprints don't leak.

---

## Resolved

_(nothing yet)_
