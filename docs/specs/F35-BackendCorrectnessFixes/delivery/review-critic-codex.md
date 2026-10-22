# F35-BackendCorrectnessFixes — spec review, round 1 — Codex (primary)

**Model:** codex
Persisted by the architect session from the read-only Codex job, 2026-10-22.

## Verdict: NO-GO

Review target: [docs/specs/F35-BackendCorrectnessFixes/definition/spec.md](docs/specs/F35-BackendCorrectnessFixes/definition/spec.md). Backend source paths below are relative to `src/Services/Reflekt/`. This was a read-only review; no files were modified and no builds or tests were run.

1. **HIGH — spec.md:26,40 — Soft kick does not fully revoke existing hub access.** `Reflekt.API/Hubs/SprintHub.cs:31` authorizes `JoinSprint` from connection claims, and `ToggleReady` at line 98 does likewise. Removing a connection from the sprint group leaves those claims usable: the kicked client can rejoin and restore readiness. Require revocation checks on existing connections and removal from any recipient groups introduced for redaction. F38’s fire-and-forget publishing decision also means eviction cannot depend solely on successful completion of a broadcast handler.

2. **HIGH — spec.md:23 — Writing-phase anonymity remains bypassable through other read endpoints.** `GetGroupsEndpoint.cs:61` returns unredacted card text and evidence; `GetParticipantReportEndpoint.cs:65,106` exposes qualifying groups’ content without a phase restriction. This is reachable with existing data: `Reflekt.Persistence/DbInitializer.cs:52,98` starts a Writing sprint and creates populated groups and votes. Fixing `ListCards` and broadcasts leaves these guest-readable paths exposed. Extend the visibility requirement or explicitly define phase restrictions for them.

3. **HIGH — spec.md:41 — Display-name repair is not guaranteed to produce migratable data.** For existing names `Ana`, `ana`, and `Ana (2)`, appending the prescribed suffix can create another case-insensitive collision. Appending also exceeds the existing 50-character name contract for maximum-length names. The claimed deterministic repair needs a survivor/order rule, collision-safe suffix allocation, and truncation policy. Require an upgrade test containing these cases; clean-database tests cannot establish migration safety.

4. **MEDIUM — spec.md:23,54 — The owner-preservation contract is ambiguous across authentication combinations.** `ListCardsEndpoint.cs:27,52` redacts whenever participant claims exist, including an owner request carrying both cookie and guest credentials. Cookie-only owner requests receive full cards. The hub accepts the guest-token scheme exclusively, while its current broadcasts expose full cards. Specify the intended results for owner cookie-only, cookie-plus-token, and owner participant connections; a blanket owner exemption changes current REST behavior.

5. **MEDIUM — spec.md:23,49 — Evidence-fetch notifications do not have the assumed card payload.** `BroadcastEvidenceLinkFetchedHandler.cs:13` sends an `EvidenceLinkResponse`, while create/edit/attach/detach send `CardResponse`. `web/reflekt-web/src/composables/useSprintHub.ts:90` consumes that link payload directly. Define whether Writing-phase fetch notifications are suppressed for unauthorized viewers or change schema, and specify the permitted author/owner payload. “The same redacted shape” does not resolve this contract.

6. **MEDIUM — spec.md:28 — Closed-sprint protection needs an explicit boundary.** `SetReviewStatusEndpoint.cs:59,75` intentionally modifies a card belonging to a closed previous sprint while the reviewing sprint is active. A guard against the saved card’s sprint would break that feature. Conversely, hub timer controls check phase without checking sprint state, and `JoinSprint` can start a timer in a closed timed phase. Identify the guarded sprint and whether hub mutations are included, preserving the previous-action review behavior explicitly.

7. **MEDIUM — spec.md:29,47 — SK-23 has conflicting ownership and an unassigned remainder.** Scope cites SK-23, while the out-of-scope range includes SK-23 and assigns it to F38. That finding also identifies the team-count race still present in `CreateTeamEndpoint.cs:23,38`. F38 moves the team-limit rule into a service but promises unchanged behavior; it does not require concurrency protection. Mark the finding partial and explicitly assign or defer the remaining race.

8. **MEDIUM — spec.md:63 — The redaction unit-test fallback cannot prove broadcast confidentiality.** A redaction-function test passes even if every existing broadcaster continues sending full payloads to the sprint group. Checking empty text alone also misses evidence and assignee leakage. Require delivery-path checks covering every affected event, author and owner access, other participants, and multiple connections.

9. **MEDIUM — spec.md:22,27,60 — Event isolation and transaction ordering lack effective acceptance checks.** Removing the singleton registration does not prove scoped resolution or failed-save cleanup. No acceptance case verifies the four operations’ rollback behavior or publication after commit. The current interceptor publishes in `SavedChangesAsync`, before an enclosing explicit transaction commits. Require checks for separate scopes, failed saves, rollback without published events, and committed group membership being readable when the phase notification arrives.

10. **MEDIUM — spec.md:63 — The closed-group check can pass before the defect is fixed.** Closing during Writing and calling `CreateGroup` already returns 409 through the phase-action guard. Focus/discussion endpoints also already reject closed sprints with different messages. Exercise otherwise-valid operations in phases where they are permitted, assert `Sprint is closed.`, and verify unchanged data. One unspecified group endpoint cannot prove the “every mutating endpoint” requirement.

11. **MEDIUM — spec.md:62 — Error acceptance can miss both the mapping defect and information leakage.** Existing endpoint checks, such as `StartSprintEndpoint.cs:31`, return 409 before invoking the aggregate, so an endpoint-status assertion can pass with `DomainException` still mapped incorrectly. Require a test that actually exercises that middleware mapping and a separate non-action-item review → 404 case. The 5xx check must also exclude the development stack trace currently emitted through `Details`, and verify camelCase and `HttpException.StatusCode`.

12. **MEDIUM — spec.md:64 — The EF command is incomplete, and its result does not prove the migration works.** The repository root contains no project file. Supply `--project src/Services/Reflekt/Reflekt.Persistence --startup-project src/Services/Reflekt/Reflekt.API` and the tool/configuration prerequisites. “No pending model changes” compares the model with its snapshot; it does not execute duplicate repair or prove database enforcement. Require migration application over duplicate existing data, active-name case-insensitive uniqueness, removed-name reuse, and constraint-error → 409 checks.

13. **MEDIUM — spec.md:65 — “Rejects” does not define the OAuth acceptance outcome.** The existing auth test only checks a 302 to Google; the vulnerable implementation passes that check even for malicious return URLs. Specify safe fallback versus HTTP rejection, then inspect the stored `AuthenticationProperties.RedirectUri` through OAuth state or a test capture. Cover production, the configured development origin, encoded malicious prefixes, and the legitimate `/sprint/{id}` return URL used by `RetroJoinForm.vue`.

14. **MEDIUM — spec.md:33,66 — SSRF acceptance never observes the outbound connection behavior.** Existing evidence-link tests exercise attachment responses; fetching happens later in `OpenGraphBackgroundService`. Successful attachment therefore proves neither blocking nor safe resolution. Require deterministic checks for the listed address ranges, mapped IPv6, mixed DNS answers, redirect handling, DNS rebinding, and an allowed public connection.

15. **MEDIUM — spec.md:30,31,34,66 — Several required outcomes have no concrete acceptance cases.** The current suite does not cover query-token transport restrictions, missing-user-claim mapping, numeric/undefined enum inputs, or group-input validation. Specify cases proving query tokens fail outside hubs while hub/header/bearer forms work; missing claims return 401; invalid inputs return 400 without persistence. Preserve the existing minimum of two cards for group creation and rejection of `ReviewStatus.None` explicitly.

---

# Round 2 — 2026-10-23 — Codex (primary, alone)

## Verdict: NO-GO

Line numbers refer to [F35’s spec](docs/specs/F35-BackendCorrectnessFixes/definition/spec.md).

| Round-1 id | Verification | Line / remaining gap | Collateral |
|---|---|---|---|
| R1-01 | partial | 29 — revocation specified; broadcast failure can prevent cleanup | R2-01 |
| R1-02 | closed | 25, 69 — previously omitted endpoints included | R2-03, R2-04 |
| R1-03 | partial | 24, 42, 65 — mechanism assigned; acceptance permits failed **or** rolled-back saves and omits scope isolation | R2-05 |
| R1-04 | closed | 33, 70 — rule, fallback and test seam defined | none |
| R1-05 | partial | 30, 43, 73 — destructive repair removed; fixture lacks duplicate maximum-length names and constraint-enforcement checks | none |
| R1-06 | closed | 25, 44 — identity precedence explicit | none |
| R1-07 | closed | 25 — fetched-link notification restricted to author connections | none |
| R1-08 | partial | 28 — timer controls covered; `JoinSprint` timer auto-start remains unaddressed | none |
| R1-09 | closed | 30, 51 — team-count race explicitly assigned to F40 | none |
| R1-10 | not closed | 46, 69 — absence of a mapping expression does not prove delivery confidentiality | R2-02 |
| R1-11 | closed | 67 — permitted phase, exact message and unchanged data required | none |
| R1-12 | closed | 26, 27, 28, 67 — reachable mapping check and production-detail exclusion specified | none |
| R1-13 | closed | 73 — unrunnable command replaced with startup and upgrade checks | none |
| R1-14 | partial | 34, 71 — address classification tested; actual connection, redirect and rebinding behavior untested | none |
| R1-15 | partial | 32, 35, 72 — missing-claim mapping and preserved token transports lack concrete cases; invalid-input coverage remains incomplete | none |
| R1-16 | closed | 29 — repository filtering, rejoin and repeated-kick behavior explicit | none |
| R1-17 | closed | 26, 66 — defensive throws and affected assertions identified | none |
| R1-18 | partial | 9 — plan obligations remain deferred; no plan exists yet | none |

1. **R2-01 — HIGH — spec.md:29 — Kick cleanup still depends on successful notification. Tag: `fold`.**  
   The new ordered handler broadcasts first, then evicts and aborts connections. If that awaited broadcast throws or its cancellation token is cancelled, ordinary sequential execution skips revocation. Existing connections remain subscribed despite their tokens subsequently failing authentication. The current interceptor publishes with `Mode.WaitForNone` (`src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs:66`); F38 preserves that behavior and logs failures. Require eviction, abort and state cleanup to execute despite notification failure, and exercise that failure path.

2. **R2-02 — MEDIUM — spec.md:46,69 — The replacement grep cannot establish broadcaster correctness. Tag: `fold`.**  
   Removing `Adapt<CardResponse>` proves neither use of the redaction rule nor correct recipients. A helper can return full content to the sprint group while satisfying the grep. `BroadcastEvidenceLinkFetchedHandler.cs:13` already uses `Adapt<EvidenceLinkResponse>`, so its disclosure path is entirely invisible to this check. Require handler-level assertions capturing recipients and payloads for every affected event, including multiple author connections. These can use handwritten SignalR interface fakes without adding a client package.

3. **R2-03 — MEDIUM — spec.md:25,59 — The expanded redaction rule lacks a previous-sprint boundary. Tag: `fold`.**  
   “Every path” during Writing and “own cards only” also describe previous-action content, yet that content must remain visible to participants. `GetPreviousActionsEndpoint.cs:45` loads cards from the closed previous sprint; both reports also return their text and assignee (`GetParticipantReportEndpoint.cs:111`, and its sibling). Those cards have previous-sprint participant IDs, so current participants cannot match their authors. Specify that the card’s source sprint controls Writing redaction, or explicitly exempt previous-action content, and verify that preservation.

4. **R2-04 — MEDIUM — spec.md:25,69 — Report redaction overlooks text copied into group titles. Tag: `round-1 miss: report-derived fields`.**  
   Both report endpoints derive `groupTitle` from `groupCards.FirstOrDefault()?.Text` before mapping card entries (`GetSprintReportEndpoint.cs:65`; `GetParticipantReportEndpoint.cs:65`). Redacting card text and evidence can therefore leave the same private text in `groupTitle`. This is reachable in the existing Writing-phase seed, whose untitled groups have sufficient votes for report inclusion—even after removing the duplicate vote. Explicitly cover derived titles and assert that another author’s text appears nowhere in the response.

5. **R2-05 — MEDIUM — spec.md:24,42 — Transaction-aware interception does not cover existing inline notifications. Tag: `fold`.**  
   `CreateGroupEndpoint.cs:95` and `DissolveGroupEndpoint.cs:67` send SignalR messages directly; their group creation events are explicitly cleared. Holding intercepted domain events therefore cannot defer these sends. Wrapping the existing bodies in transactions can still notify clients before commit, and the SPA immediately refetches on both messages (`useSprintHub.ts:107,119`). Require commit before these inline sends in F35, with rollback producing no notification; F38’s later broadcast refactor cannot supply that guarantee retroactively.

Read-only review completed. No files were modified; no builds or tests were run.
