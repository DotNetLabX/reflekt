# F35-BackendCorrectnessFixes — Change notes

Shared files this unit may not edit. The campaign applies each note at merge.

## Note 1 — `docs/backlog.md`: F35 status
- **Target:** `docs/backlog.md`, the F35 entry (line 268 at close: `**Status: Spec Ready** | **Spec:** ...`).
- **Change:** status `Spec Ready` → `Done`, and add the summary link.
- **Exact text:** `**Status: Done** | **Spec:** \`docs/specs/F35-BackendCorrectnessFixes/definition/spec.md\` | **Summary:** \`docs/specs/F35-BackendCorrectnessFixes/delivery/summary.md\``

## Note 2 — `docs/backlog.md`: follow-up row for two review doubts
- **Target:** `docs/backlog.md`, a new row in the bug/follow-up list (next free number in the repo's form).
- **Change:** add one follow-up row, from the F35 code review's open questions (second reader, below its confidence cutoff — not fixed in F35):
- **Exact text:** `F35 follow-up — (1) OpenGraph fetch: with a system proxy configured, \`ConnectCallback\` checks the proxy's address, not the target's; set \`UseProxy = false\` on the fetcher's handler. (2) \`BroadcastAndFetchOnEvidenceLinkAttachedHandler\` reads the database before queueing the fetch job, so a failed read leaves the link \`Pending\`; queue first or catch around the broadcast. Source: docs/specs/F35-BackendCorrectnessFixes/delivery/review-second.md § Open questions.`

## Not needed
- The architect's Q4 follow-up row ("make the integration teardown wait or retry") is **not** owed: the owner allowed the retry (Q5, directive d2) and the fix round made it (review.md § Step 2 — Fix list, cycle 1, row 5).
