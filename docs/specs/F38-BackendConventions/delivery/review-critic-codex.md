# F38-BackendConventions — spec review, round 1 — Codex (primary)

**Model:** codex
Persisted by the architect session from the read-only Codex job, 2026-10-22.

## Verdict: NO-GO

1. **HIGH — [spec.md:24](docs/specs/F38-BackendConventions/definition/spec.md:24): Access ordering contradicts status preservation.** `GetSprintEndpoint` checks team ownership before sprint existence: a foreign team plus nonexistent sprint currently returns 403; the proposed ordering implies 404. `CardEndpointTests.CreateCard_NonexistentSprint_Returns401` would similarly become 404, although Decisions permits only 401 → 403. Specify precedence for overlapping failures and enumerate the additional status changes.

2. **HIGH — [spec.md:24](docs/specs/F38-BackendConventions/definition/spec.md:24): The shared access service lacks an applicable permission contract.** “Every endpoint” includes anonymous `JoinSprint`, `GetSprintSummary`, and `GetPhaseActions`, plus team endpoints without a sprint. Identity precedence also needs a decision: `EditCardEndpoint` and `DeleteCardEndpoint` select the participant branch whenever `participantId` exists, bypassing the owner branch even when an owner cookie accompanies the token. F36’s FE-31 instead describes “author or team owner.” Centralization cannot both preserve that live behavior and implement F36’s stated rule without clarification.

3. **HIGH — [spec.md:41](docs/specs/F38-BackendConventions/definition/spec.md:41): Fire-and-forget conversion introduces another status change.** The four endpoints with inline broadcasts currently await SignalR sends. If a send throws after persistence, middleware returns 500. Moving those sends into `WaitForNone` handlers with logged failures allows the request to succeed instead. That conflicts with the status-code invariant and the claim that only wrong-sprint authorization changes status. Explicitly resolve broadcast-failure behavior.

4. **HIGH — [spec.md:22](docs/specs/F38-BackendConventions/definition/spec.md:22): The error contract is incomplete and its acceptance checks cannot establish conformity.** `errors[]` has no defined element schema. Authentication failures also bypass exception mapping: `AuthenticationConfiguration` writes empty 401/403 responses directly. State whether these responses must use the envelope. Existing integration tests do not assert error-envelope fields or machine codes; updating existing body assertions therefore supplies no meaningful coverage for the new contract.

5. **HIGH — [spec.md:60](docs/specs/F38-BackendConventions/definition/spec.md:60): Removing broadcast code would satisfy acceptance without replacing its behavior.** No existing tests observe SignalR delivery, publication after commit, rollback suppression, duplicate publication, or handler-failure logging. Entity event tests only establish that an event was raised. The checks need evidence that broadcasts still happen with the required timing and payloads. The same acceptance section leaves major structural outcomes—aggregate enforcement, shared report/group builders, read-query tracking, readiness extraction, and options validation—unverified.

6. **MEDIUM — [spec.md:23](docs/specs/F38-BackendConventions/definition/spec.md:23): The cited broadcast implementations are not interchangeable.** `CreateGroupEndpoint` emits `cardIds` in the repository’s descending creation order; `BroadcastGroupCreatedHandler` explicitly sorts ascending. Activating that handler changes the payload. `DissolveGroup` additionally emits `GroupDissolved`, for which no domain event currently exists. Define the preserved event inventory, array ordering, and suppression of incidental solo-group events; deleting inline sends alone does not preserve the invariant at line 54.

7. **MEDIUM — [spec.md:31](docs/specs/F38-BackendConventions/definition/spec.md:31): “Remaining” validation work is undefined after F35.** F35 already owns enum validation, `CreateGroup` collection validation, and `UpdateGroupTitle` length validation—the concrete missing validations in SK-11. F38 does not identify the remaining endpoints or rules. Most remaining requests contain route identifiers; adding positive-ID validators would turn existing 404 paths into pre-handler 400 responses. Enumerate the residual work and its permitted behavior changes.

8. **MEDIUM — [spec.md:62](docs/specs/F38-BackendConventions/definition/spec.md:62): Acceptance 4 rejects legitimate dependencies and generated artifacts.** Running the command finds `Reflekt.API.csproj` because it references `Microsoft.EntityFrameworkCore.Design`, plus numerous `bin/` and `obj/` files. Those matches survive removal of EF queries from handlers. Furthermore, moving the migrate/seed host extension into API requires an explicit startup exception or a specified persistence boundary. Restrict the check to the intended source locations and responsibilities.

9. **MEDIUM — [spec.md:64](docs/specs/F38-BackendConventions/definition/spec.md:64): Acceptance 6 already reports no hits despite existing violations.** With Bash’s default `globstar` setting disabled, the glob reaches only `HealthEndpoint.cs` in this tree. Even with recursion enabled, its regex misses inline responses such as `MeResponse` and `AdvancePhaseCheckResponse`, commands such as `SetReviewStatusCommand`, and `TeamSummary`. Use recursive file selection and detection covering every prohibited declaration.

10. **MEDIUM — [spec.md:40](docs/specs/F38-BackendConventions/definition/spec.md:40): The frontend compatibility scope misses a status consumer.** `useRetroRoom.ts:107` clears a rejected stored guest token only on 401; `participant.fetchMe()` propagates the HTTP status. Returning 403 for a wrong-sprint token leaves that token stored. Decide the intended recovery behavior and include this consumer in F38’s migration scope, or explicitly accept the change until F39.

11. **MEDIUM — [spec.md:39](docs/specs/F38-BackendConventions/definition/spec.md:39): “The documented exception convention” is internally contradictory.** `docs/conventions/csharp.md:12` requires throwing `NotFoundException`, while line 73 explicitly prohibits exceptions for expected validation and “not found” results. F38 excludes documentation changes, and F40 does not explicitly assign resolution of this contradiction. Identify which rule takes precedence and where the conflicting live instruction will be reconciled.

12. **LOW — [spec.md:33](docs/specs/F38-BackendConventions/definition/spec.md:33): The fixture target count is inaccurate.** There are six `*EndpointTests` classes, plus `AuthSmokeTests` and `FoundationSmokeTests`. The smoke classes use different host setups. Name the intended consumers and preserve their authentication and database isolation requirements rather than referring to eight endpoint classes.

Read-only review completed. Build and full verification were not run because they write artifacts.

---

# Round 2 — 2026-10-23 — Codex (primary, alone)

## Verdict: NO-GO

Lines below refer to [F38’s spec](docs/specs/F38-BackendConventions/definition/spec.md).

| Round-1 id | Verification | Line | Collateral |
|---|---|---|---|
| R1-01 | closed | 25, 43 | F2-01 |
| R1-02 | closed | 25 | none |
| R1-03 | partial | 42 discloses the change; 43 and 54 still prohibit it | F2-01 |
| R1-04 | closed | 23, 41, 69 | none |
| R1-05 | partial | 24 pins payload preservation; 71 checks successful delivery but omits commit, rollback and handler-failure verification | none |
| R1-06 | partial | 32 excludes route validation; residual endpoints and rules remain deferred to the plan | none |
| R1-07 | closed | 29, 61, 66 | none |
| R1-08 | closed | 68 | none |
| R1-09 | closed | 43 | none |
| R1-10 | closed | 40; F40:31 | none |
| R1-11 | closed | 26 | none |
| R1-12 | closed | 31 | none |
| R1-13 | closed | 24; F35:24 | none |
| R1-14 | closed | 25 | none |
| R1-15 | closed | 26, 32 | none |
| R1-16 | closed | 28, 34 | none |
| R1-17 | closed | 30 | none |

- **F2-01 — MEDIUM** — **File:** [spec.md:43](docs/specs/F38-BackendConventions/definition/spec.md:43). **Issue:** The fold introduces mutually exclusive status requirements. Line 42 explicitly changes a committed request from 500 to its normal status when broadcasting fails, while line 43 requires today’s status “for every input.” Line 54 also preserves every status code without an exception. The disclosed change therefore still breaks the blanket preservation guarantee. **Tag:** `fold`.

No additional contradictions were found in the targeted F35, F36, F39 or F40 clauses.
