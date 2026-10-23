# F35 — Backend Correctness & Security Fixes — Review

## Step 2 — Code Review

Primary: fable — Codex dropped: job task-mvkmsuzx-8n745w failed early (earlyFailure); fresh Fable main per the typed-marker unattended fallback (pair 2).

## Reviewed By
reviewer (primary reader); a Sonnet second reader writes `review-second.md` in parallel.
**Model:** claude-fable-5-1

## Verdict: COMMENT

No CRITICAL or HIGH. One MEDIUM (duplication across the four card-broadcast handlers) and three LOW. Plan conformance holds on every step read; every `Satisfies:` target traced to code. Diff base `1eec0b8`, working tree, 45 changed files plus 4 new.

## Pre-commitment Predictions
- **Interceptor (Step 1): implicit-vs-explicit transaction detection and stale held events.** Found correct: `SavedChangesAsync` sees `CurrentTransaction == null` after EF's own save transaction (committed and disposed before the callback), and only `TransactionCommittedAsync` publishes `_heldEvents`, which every explicit `TransactionStartedAsync` clears first — so a list left by a dispose-without-commit can never be published. One comment overstates when it is *cleared* (LOW below). All transaction callers are async (`grep` for sync `.Commit()`/`BeginTransaction(` → none), so the async-only interceptor members are the ones reached.
- **Hub join/kick race and the `finally` revocation (Step 9).** Found correct. `JoinSprint` registers → joins both groups → re-reads the participant; a kick that commits before that read is undone by the join itself, one that commits after it finds the registry entry and the handler evicts. `BroadcastParticipantKickedHandler` runs each revocation sub-step through `RunAsync`, collects failures, rethrows after revocation — no swallow, no skipped step. `RemoveParticipantState` returns counts with the same semantics `ToggleReady` and `OnDisconnectedAsync` use (`Values.Distinct().Count()`), and the aborted connection's later `OnDisconnectedAsync` finds nothing to remove, so no duplicate `ReadinessChanged`.
- **Card broadcasts (Step 11): recipient correctness, empty `Groups(...)` call.** Found correct: full payload only to `participant:{authorId}`; redacted only to groups of participants `IsRedacted` is true for (never the author); the `Groups(...)` send is guarded by `Count > 0`; outside Writing exactly today's single sprint-group send. Found four verbatim copies of the read/send block (MEDIUM below).
- **SSRF connect callback (Step 13): fail-closed, connect only to checked addresses.** Found correct: empty DNS answer, any rejected address, or an unknown address family refuses; the socket connects to the same `addresses` array that was checked, so no rebinding window; redirects stay on and each hop re-enters the callback; the fetcher's `catch (Exception) when not OCE` maps the refusal to `FetchResult.Failed`. The allowed path is unproven by any run (Gaps).
- **Migration repair SQL (Step 7).** Found correct against the recorded probe and by reading the CTE: candidates are checked against every current active name (old names of rows being renamed included, which is conservative but collision-free) and against the names assigned earlier in the recursion; `Suffixes` is bounded; the `Up` order (repair before `AlterColumn`) is a documented deviation that still repairs before the index exists. `Down` leaves the renames, as planned.

## Findings

### [MEDIUM] `ReadRecipientsAsync`, `CardRecipients` and the Writing-phase send block are copied verbatim into four handlers
**File:** `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/BroadcastCardCreatedHandler.cs:32-50`, `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/BroadcastCardUpdatedHandler.cs:32-50`, `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/BroadcastAndFetchOnEvidenceLinkAttachedHandler.cs:45-63`, `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/BroadcastCardUpdatedOnEvidenceLinkDetachedHandler.cs:33-51`
**Origin:** implementation (the plan's "each handler loads the card's sprint through a scope it creates" was read as "no shared code"; a shared helper that creates the scope per call satisfies the same sentence)
**Issue:** ~30 identical lines per file — the scope creation, the untracked sprint read, the participant read, the redacted-group projection, the private `CardRecipients` record, and the three-way send (`sprint:` group / author group / `Groups(redacted)`) — appear four times, differing only in the message name (`CardCreated` vs `CardUpdated`). The conventions' rule of three (`docs/conventions/csharp.md` § Decomposition Judgment) is exceeded. A future change to the rule (say, the fetched handler also needing recipients, or F38's `ReadinessTracker`) must be made in four places, and the Step 11 reshape after Q4 already had to be.
**Fix:** one static helper in the `Cards` slice (next to `WritingPhaseRedaction`, or a `CardBroadcast` static class) with `ReadRecipientsAsync(IServiceScopeFactory, Card, ct)` and `SendCardAsync(IHubContext<SprintHub>, Card, string message, CardResponse payload, CardRecipients, ct)`; each handler keeps its own `IServiceScopeFactory` and calls the helper, so "a scope it creates" still holds. No behaviour change. If the coordinator prefers to leave structure to F38 (spec Out of scope sends conventions/structure findings there), record it for F38 rather than fixing here.
**Confidence:** 90/100

### [LOW] `implementation.md` Step 8 cites "today's kick tests" as evidence, and none exist
**File:** `docs/specs/F35-BackendCorrectnessFixes/delivery/implementation.md:281`
**Origin:** implementation
**Issue:** "Today's kick tests, now on the soft path, stay green." — `grep -rni kick tests --include=*.cs` returns nothing; `ParticipantEndpointTests.cs` has join, current-participant, list and claims tests only. The kick endpoint, the removed-token 401, the hub's removed checks and the kick handler's revocation have no automated coverage at all; the green integration run says nothing about them. Under the owner's no-new-tests rule that is an accepted gap, but the record must not read as if the path were pinned.
**Fix:** reword the Step 8 Verify line to "no existing test exercises the kick path; the run proves only that nothing else regressed", and list the kick path under the summary's accepted gaps.
**Confidence:** 95/100

### [LOW] Enum-name rules emit two messages for an empty value; `Cascade(Stop)` is used on the sibling `CardIds` rule but not here
**File:** `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardCommand.cs:19-20`
**Origin:** implementation
**Issue:** `RuleFor(x => x.ColumnType).NotEmpty().Must(IsEnumName<ColumnType>)` runs both validators on a null or empty value (`IsEnumName` returns false for null), so the 400 body carries "'Column Type' must not be empty." and "Invalid column type." for one defect. `CreateGroupRequestValidator` in the same step uses `.Cascade(CascadeMode.Stop)` for exactly this reason. Response status is unchanged; only the `errors[]` list is noisier.
**Fix:** add `.Cascade(CascadeMode.Stop)` after `RuleFor(...)` on both rules.
**Confidence:** 85/100

### [LOW] Interceptor comment overstates when a stale held list is cleared
**File:** `src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs:83-84`
**Origin:** implementation
**Issue:** "dropped here, at the start of the next transaction (explicit or EF's own save)" — under EF Core's default `AutoTransactionBehavior.WhenNeeded` a single-batch save opens no transaction of its own, so `TransactionStartedAsync` is not raised and the list is not cleared by that save. The outcome is unaffected: the only publisher of `_heldEvents` is `TransactionCommittedAsync`, which is reachable only after an explicit `TransactionStartedAsync` that clears the list first, and the scoped instance dies with the request. The implementation's AC1 (d) trace repeats the stronger claim.
**Fix:** reword to "at the start of the next explicit transaction, before any of its saves; a save with no transaction leaves it in place, and nothing but a commit publishes it".
**Confidence:** 80/100

## Carry-Over Findings (from `implementation.md`)
| Title | Disposition | Evidence |
|-------|-------------|----------|
| Integration teardown `IOException` at `File.Delete` | **Confirmed as a test-infrastructure flake, not a product defect.** The mechanism (a `WaitForNone` handler's read after the response vs the class's `DisposeAsync` delete) is real and pre-dates F35 (`BroadcastGroupCreatedHandler` already reads after the response); production never deletes the file. Q4 ruled A; Q5 is the owner's and the teardown is untouched by design. No finding. | Complete run `mvkmqjq8-44234116` passed with `failed_tests: null`; the two red step runs listed in Step 11/12 went green on re-run. |
| F37's flaky host start in `InitializeAsync` | **Confirmed pre-existing by provenance** — F37 `change-notes.md` Note 1 records the same class, line and frames before this feature's base commit. Not attributable to F35; not reproduced here. | Step 7's stress run (240 migrations, 0 errors) rules out the new migration; the complete run passed. |
| Validation now runs before the authorisation check in `CreateCard` | **Confirmed as a status change, and spec-sanctioned.** Spec "What must not change" lists "invalid enum … becomes 400" and the BE-19 row requires one validation per input, in the validator, which FastEndpoints runs pre-handler. No test covers the combination; no fix. | `CreateCardCommand.cs:19-20`; `CreateCardEndpoint.cs:33-34` no longer parses inline. |
| `JoinSprint` requires the `participantId` claim | **Refuted as a defect.** The guest-token scheme always issues the claim (`GuestTokenAuthenticationHandler.cs`, the `claims` array), the hub authorises that scheme only, and `ToggleReady` already threw the same "Missing participant claims." for its absence. No working client loses access. | `SprintHub.cs:47-49`. |
| Reserved `240/4` and site-local `fec0::/10` allowed for link fetches | Noted; addressed to the architect, not a review dimension. The code matches P5's closed list exactly (`OpenGraphAddressPolicy.cs:23-38`). | Step 13 probe output. |
| `graphify update` not run | **Confirmed**; the plan names this fallback (F40's refresh). No finding. | Step 14 Deviations. |

## Positive Observations
- The interceptor's two-list design is small and exact: `SavingChangesAsync` clears the aggregates' events, so a second save in one transaction cannot re-collect them; `SaveChangesCanceledAsync` was added beside `SaveChangesFailedAsync`, which the plan missed.
- `DissolveGroupEndpoint` now builds all solo groups, adds them with one `AddRangeAsync` and saves once; the index-parallel `SetGroupId` loop is order-safe because both lists derive from `groupCards` in order.
- `ParticipantRepository.SaveChangesAsync` narrows the catch with an exception filter on the extended error code *and* the index's column list, so the other unique indexes on the table keep surfacing as today; the `DbUpdateException` is kept as the inner exception. Both join endpoints save through this repository, so the 409 mapping is reachable from both.
- The migration's repair is written for any number of duplicates, bounded, and leaves no row deleted; the probe covers case-folding, a suffix skipped because a later row already holds it, and two trimmed 50-character bases colliding.
- `GlobalExceptionMiddleware` keeps `ValidationException` and `OperationCanceledException` ahead of the generic arm; the 5xx body outside Development is exactly `statusCode`, `message`, `traceId` through `JsonIgnoreCondition.WhenWritingNull`.
- `LoginEndpoint.IsSafeReturnUrl` composes the spec's four-part received-form rule with today's decoded-form predicates unchanged, as R2-02 decided, and `ResolveRedirectUri` keeps the prefixing shape with the literal replaced.
- `OpenGraphAddressPolicy` encodes each range with a mask test, not a string prefix; IPv4-mapped IPv6 is unwrapped first; unknown families fail closed.
- No comment noise: every added comment states a constraint the code cannot show (the interceptor's EF callback facts, the hub registry's lifetime, the join re-check's race argument, the migration's repair rule) — none carries feature ids, review rounds or dates.

## Gaps
- **Kick path has zero automated coverage** (see the LOW above): soft kick 204, second kick 204, removed token → 401, Google rejoin → 409, hub rejection, handler eviction/abort. Accepted under the owner's Q1 rule; code traces are the only proof. Worth a summary note so it is not mistaken for a pinned path.
- **SSRF: the allowed path through `ConnectCallback` has no run behind it.** The probe proves refusal; no test or probe fetches a reachable public host through the new `SocketsHttpHandler` (the dual-mode `Socket(SocketType.Stream, ProtocolType.Tcp)` + `ConnectAsync(IPAddress[], port)` form is the documented one, so I expect it to work, but a silent `FetchResult.Failed` on every link would not fail any test). A local probe cannot cover it — every local address is in the rejected ranges by design.
- **`ManyServiceProvidersCreatedWarning` (Step 1 accept)** is proven only indirectly: EF throws on that warning by default and the 104-test integration suite and the complete run both start the host hundreds of scopes deep without it. The runner keeps no output on a pass, so the plan's "no such text in the output" cannot be read literally.
- Suite flakiness at the close remains (teardown `IOException`, host-start flake) until the owner rules on Q5 — the close gate's re-run ladder covers it; nothing for the developer.

## Open Questions
- None below the cutoff that need the developer's context.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pass | complete run `mvkmqjq8-44234116` (the brief's record; no suite run in its place) | `.claude/audit/verify-verdict.json:73` · role `full`, profile `complete`, `dotnet test Reflekt.slnx && npm ci && npm run build` · verdict `pass` · 65,480 ms · `failed_tests: null`, `timed_out: null` · tree `9b1c27d6…` · 2026-10-23T07:09:36Z |
| Sync transaction callers | none | `grep -rn -E "\.Commit\(\)\|\.Rollback\(\)\|BeginTransaction\(" src tests` | only `CommitAsync`/`BeginTransactionAsync`/`BeginSerializableAsync` (4 endpoints, `CastVote`, `TeamRepository`) — the async interceptor members are the ones reached |
| Aggregates' events cleared on collect | yes | `DomainEventPublishingInterceptor.cs:40-41` | `ClearDomainEvents()` per aggregate in `SavingChangesAsync` |
| 409 mapping reachable from both joins | yes | `grep -rn SaveChangesAsync src/.../Features/Participants/JoinSprint*` | `JoinSprintEndpoint.cs:27`, `JoinSprintGoogleEndpoint.cs:48` both save through `participantRepository` |
| Readers of participants | all through `GetBySprintIdAsync` | `grep -rn "participantRepository\." src/.../Reflekt.API` | `AdvancePhaseCheck:85`, `GetParticipants:46`, `GetSprintSummary:21`, both reports `:53`, the 4 handlers; no raw `Db.Participants` read outside the repository except `DbInitializer` and `TeamRepository`'s cascade delete |
| Kick test coverage | none | `grep -rni kick tests --include=*.cs` | no hit (the LOW finding) |
| Platform-conditional code in the diff | none | `git diff 1eec0b8 -- src tests \| grep "^+" \| grep OperatingSystem\|RuntimeInformation\|...` | no hit; nothing to force the other way |
| History markers in added comments | none | `git diff 1eec0b8 -- src tests \| grep "^+" \| grep -i "F35\|BE-\|SK-\|review\|TODO"` | only code lines (`ReviewStatus`, "review status" message) |
| Named test files run | none | — | no finding needed one |

*Status: COMPLETE — reviewer, 2026-10-23*


## Step 2 — Fix list, cycle 1

Merged by team-lead from: done check 1 (PASS, no items), the primary review above (Fable — Codex dropped early), and the second reader (`review-second.md`, Sonnet). Round 1 bar: every item.

**From the code review (primary + second reader):**
1. [MEDIUM] `ReadRecipientsAsync`, `CardRecipients` and the Writing-phase full/redacted send block are copied into four handlers (`BroadcastCardCreatedHandler.cs:32-50`, `BroadcastCardUpdatedHandler.cs:32-50`, `BroadcastAndFetchOnEvidenceLinkAttachedHandler.cs:45-63`, `BroadcastCardUpdatedOnEvidenceLinkDetachedHandler.cs:33-51`); the fetched handler repeats the scope-and-sprint read. Extract one shared helper beside `WritingPhaseRedaction` in the `Cards` slice; each handler keeps its own scope and the Q4 order (reads first, scope disposed before the first send). Refactor only — no behaviour change. (primary + second reader)
2. [LOW] `implementation.md` Step 8 cites "today's kick tests"; none exist in `tests/`. Correct the record: the kick path has no automated coverage (P1 — no new tests). (primary)
3. [LOW] `CreateCardCommand.cs:19-20` — the enum-name rules lack `Cascade(CascadeMode.Stop)`, so an empty value yields two messages; match the sibling `CardIds` rule. (primary)
4. [LOW] `DomainEventPublishingInterceptor.cs:83-84` — the comment claims EF's own save clears a stale held list; under `AutoTransactionBehavior.WhenNeeded` a single-batch save raises no `TransactionStarted`. Correct the comment only. (primary)

**From the owner (Q5, answered 2026-10-23 — campaign directive d2):**
5. Each integration test class's cleanup (`DisposeAsync`) retries deleting its SQLite file briefly (the known teardown `IOException`, Q4). Only the cleanup line of each test class changes — no new check, test case or test file.

Follow-up review: none due — no row at HIGH or above, no done-check item; each row the round reports fixed counts as closed (fixed, not re-verified).

Under the bar: none (round 1 carries every item).

Open questions (not fixed this round, recorded for the summary): the attach handler reads the DB before queueing the fetch job, so a failed read leaves the link `Pending` (second reader, 50/100); with a system proxy configured, `ConnectCallback` checks the proxy's address, not the target — `UseProxy = false` would close it (second reader, 40/100).
