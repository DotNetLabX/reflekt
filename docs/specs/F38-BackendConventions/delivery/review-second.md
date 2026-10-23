# F38-BackendConventions: second reader, Step 2 code review

## Verdict: COMMENT

**Round:** first review (Step 2). Reviewed by: reviewer, as the second reader (Codex reads the same round as primary).
**Model:** claude-sonnet-5-5
**Covers:** the whole working tree against HEAD `9b232fe`, untracked files included (161 tracked files changed, about 55 new files). I read the plan and `implementation.md` in full, the conventions index and the three files it lists, and every area of the diff below. A run of the plan's own accept greps came back clean (list under Evidence).

No CRITICAL or HIGH finding. Two LOW findings stand. I found no status, message, check-order or SignalR-payload regression that the plan did not authorise.

## Pre-commitment Predictions

| Prediction | What I found |
|---|---|
| 1. Detached event publication: a handler racing the request, or a batch that stalls or loses events | Not a defect. `_collectedEvents` and `_heldEvents` are reassigned and never mutated, so the detached batch owns its list. A rollback still clears the held list, and the token is `CancellationToken.None`. The sequential await means a hung handler would delay later events of the same batch. That is plan Decisions row 7, not a finding. |
| 2. A moved check changing its order or status (SprintAccess adoption across 44 endpoints) | Not a defect. I compared old and new for DeleteCard, EditCard, CreateCard, AddCardToGroup, CreateGroup, DissolveGroup, FocusGroup, ClearFocus, GetGroups, KickParticipant, JoinSprint, JoinSprintGoogle, AttachEvidenceLink, SetReviewStatus, both reports, CastVote, RetractVote, AdvancePhase, AdvancePhaseCheck and CreateTeam. Orders and texts match the Access order reference. The only differences are the disclosed ones (below). |
| 3. The `Entity` equality change breaking a container that keys on entities | Not a defect. No `HashSet`, `Dictionary` or `Distinct` over an entity type exists in `src`. A transient hash that changes once the Id is assigned therefore has no consumer. |
| 4. The hub rewrite (static state to instance, per-call scopes) losing the F35 revocation re-check | Not a defect. Both participant checks in `JoinSprint` call `ParticipantRepository.IsActiveAsync` (`AsNoTracking` + `AnyAsync`). The order is check, register, join groups, re-check, undo on removed. `RequireScrumMasterAsync` keeps today's order. |
| 5. Read-only conversions (`AsNoTracking` in place) breaking a caller that mutates | Not a defect. I grepped all callers of the in-place variants (participants by sprint, sprints by team, most-recent-closed sprint, both action-item lists). None mutates what it loads. |

## Findings

### [LOW] `SeedOptions` is bound and validated at start-up but nothing reads it through `IOptions`
**File:** `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs:56-58`
**Origin:** design
**Issue:** `AddOptions<SeedOptions>().Bind(...).ValidateOnStart()` has no predicate and no `IOptions<SeedOptions>` consumer. `Reflekt.Persistence` reads the same "Seed" keys from `IConfiguration` (`MigrateAndSeedAsync`, `DbInitializer`). The registration therefore checks only that the section converts, and the options class is dead weight. The plan listed `SeedOptions` among the options to bind, and the developer disclosed this as a carry-over, so the code follows the plan; the plan row is what produced it.
**Fix:** Either read the keys through the options type (needs the type to sit where Persistence can see it) or drop the registration and keep `SeedOptions.cs` only if something uses it. This is a follow-up for F40 or a later pass, not a fix for this round.
**Confidence:** 85/100

### [LOW] A `git mv` staged one rename in the index against the no-git-write rule
**File:** `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/BroadcastGroupTitleChangedOnCardGroupTitleChangedHandler.cs`
**Origin:** implementation
**Issue:** `git status` shows `RM` for this file and `git diff --cached` lists one rename: the Step 15 rename was done with `git mv`. The campaign rule and the plan's standing rules forbid git writes by the developer. The content is a plain rename (100% similarity), and the developer disclosed it. The effect is limited to the index holding one staged entry while every other change is unstaged.
**Fix:** None needed in code. The coordinator's commit should take the staged rename together with the rest. Recorded so the commit step does not drop it or treat the file as untracked.
**Confidence:** 90/100

## Carry-Over Findings (from `implementation.md`)

| Carry-over | Disposition |
|---|---|
| One integration host-start failure in Step 1 (`SprintEndpointTests.AdvancePhase_FullCycle_Returns200`, `CreateClient()`) | **Not reproduced, not attributable.** I found nothing in the host start-up path that can fail intermittently: `ValidateOnStart` reads the in-memory configuration the test host sets, and `MigrateAndSeedAsync` is the old migrate-then-seed order. I could not confirm or refute a cause (the record's excerpt cut the exception text). It stays an unexplained single failure. |
| (from the brief) earlier complete run failed `EvidenceLinkEndpointTests.DeleteCard_CascadeDeletesEvidenceLinks`, green on the unchanged re-run | **Not reproduced.** I ran the whole `EvidenceLinkEndpointTests` class six times with `--no-build` against the current tree (the build output is newer than every source file): 19/19 each time. The test attaches a link and deletes the card at once, and the OpenGraph background service meanwhile reads the same card. That race exists in the base code too: the fetch job is queued in both versions, and the service catches its own exceptions. I cannot show that F38 changed the timing materially. I record it as a flake with no measured cause; a repeat in the close run should be re-run solo by name. |
| `GetSprintSummary` compares `OwnerId` outside `SprintAccess` | **Confirmed, and consistent with the spec.** `GetSprintSummaryEndpoint.cs:26` holds the one comparison outside the access service. The spec says anonymous endpoints do not use the service, and AC3's grep (`OwnerId != `) returns no hit. Plan P4's wording ("the only comparison") is slightly broader than the spec; no change needed. |
| Step 13's limit grep matches generated migration files | **Confirmed.** With `Migrations/` excluded, my re-run of the grep returns no hit. Generated history, rightly untouched. |
| `SeedOptions` bound but not read through `IOptions` | **Confirmed**, see the first finding. |
| `Blocks.Persistence` keeps an unused project reference to `Blocks.Core` | **Accepted as disclosed.** Not in the plan's file list; harmless. |
| One staged rename in the index | **Confirmed**, see the second finding. |
| AC10 needs the operator | **Confirmed open** (Step 18, operator-owned). The developer part ran as two SignalR guest clients against the in-process host. The recorded frames match the payload traces for `GroupCreated`, `GroupDissolved`, `VoteCast` and `VoteRetracted`. |

## Disclosed deviations I checked and accept

- 401 to 403 shift for a cookie without `NameIdentifier` on the owner branches (`SprintAccess` reads the id without throwing). Reachable only for a cookie no issuer produces: `OnGoogleTicketReceived` always sets an integer `NameIdentifier`, and the test handler sets one.
- A present but non-integer `participantId` claim now reads as absent (guest branch no longer entered). No issuer writes one (`GuestTokenAuthenticationHandler` writes `int.ToString()`).
- `CardRegrouping`'s emptiness check is an exists query (`HasCardsInGroupAsync`) where the plan said a count. The predicate matches the old in-memory check: same sprint, same source group, moved cards excluded by id.
- A start-up failure now occurs when `Auth:Google:ClientId`/`ClientSecret` are empty. The developer measured that the base host started and then answered every request 400, so this is the plan's conditional branch (four `ValidateOnStart` bindings) and not new behaviour for a working setup.

## Positive Observations

- The mechanical claims hold when checked independently. `TimerService` moved with exactly the described edits (my `diff` against `git show HEAD:` shows only the namespace, the injected hub context, the removed callback and the expiry send). The two report endpoints differed at base only in their route/team lookup, so merging them into `SprintReportBuilder` is faithful.
- `DbInitializer`'s split keeps the original data, order, existence checks and save points (compared method by method against base).
- The plan accept greps I re-ran came back as the plan wants them (list under Evidence); I did not re-run the ones that only count lines or list file moves, and rely on the step blocks for those.
- Tests: the `[Fact]`/`[Theory]` count per integration file equals base (checked with `git show HEAD:`). The only assertion lines added are the three named ones. The shared host reproduces each class's old policy overrides (I read `VoteEndpointTests`'s base setup against `ReflektApiFactory.CreateSignedIn`; I did not diff the other five classes' setup line by line).
- The one comment added in `Blocks.Persistence`, `ParticipantRepository.IsActiveAsync` and `TeamRepository` are constraint comments the code cannot show. There is no feature id, step or review history in any source comment.

## Gaps

- Not covered by any test, by the owner's no-new-tests rule (plan P1): the hub's join and kick revocation re-check, the four moved broadcasts, `Entity` transient equality, `ColumnColor` as a record, the two `code` values, and the `GetSprintSummary` Mapster map (an evidence run covers the last two once). The plan names these accepted gaps and replaces them with traces, which I read and found consistent with the code.
- The declared `full` role is `dotnet test Reflekt.slnx` (plus the web build), which covers both test projects; no endpoint suite is left outside it.
- `FocusHandover`, `VoteBudget` and `TeamLimitCheck` are not separately tested, same rule.

## Open Questions

- None below the report cutoff that I could not settle. The flake in the carry-over table is an open observation rather than a suspected defect.

## Evidence

| Check | Result | Command | Output |
|---|---|---|---|
| Run record | pass | the brief's `Run record:` line, complete run `mvl2pwti-bc55eeb1` | pass for the reviewed tree; the earlier run `mvl2ohcm-afc6e6b6` failed one integration test, recorded flaky and green on the unchanged re-run. I ran no whole suite. |
| `EvidenceLinkEndpointTests` (flake probe, class by name, six runs) | pass x6 | `dotnet test tests/Reflekt.IntegrationTests --no-build --filter "FullyQualifiedName~EvidenceLinkEndpointTests"` | `Passed! Failed: 0, Passed: 19, Total: 19` each run (18 to 28 s) |
| Plan accept: `Send(NotFound\|Forbidden\|Unauthorized\|Errors)Async\|ThrowError(\|AddError(\|GetAwaiter()\|.Wait()` over `src` | no hits | `grep -rnE` | empty |
| Plan accept: handlers naming a repository without `IServiceScopeFactory` | no hits | `grep -rln "Repository" Features --include=*Handler.cs \| xargs grep -L IServiceScopeFactory` | empty |
| Plan accept: `IHubContext<SprintHub>` in `Features` outside `*Handler.cs` | no hits | `grep -rn` | empty |
| Plan accept: static members on `SprintHub` | one | `grep -n "static " Hubs/SprintHub.cs` | only `ParticipantGroup(int)` |
| Plan accept: raw claim names and old policy names in `Reflekt.API` | no hits outside `GuestClaims.cs` | `grep -rnE` | empty |
| Plan accept: literal length numbers in `HasMaxLength`/`MaximumLength`/`Length(a, b)` outside `Migrations/` | no hits | `grep -rnE` | empty |
| Plan accept: a non-endpoint type declared in a `*Endpoint.cs` file | none | `find ... -name "*Endpoint.cs"` + type count | none (each file declares one type) |
| Verbatim-move check: `TimerService` against `git show HEAD:` | as described | `diff` | namespace, injected hub context and logger, callback removal, `TimerSnapshot.Stopped`, expiry send only |
| Verbatim-merge check: base `GetSprintReportEndpoint` vs `GetParticipantReportEndpoint` | differ only by route and team lookup | `diff` | 8 hunks, all route/team/namespace |
| Test case counts per integration file against base | equal | `git show HEAD:{file} \| grep -c "\[Fact\]\|\[Theory\]"` vs the tree | 3/26/19/1/14/16/6/19, identical for all eight files |
| Platform-conditional code in the diff | none | `grep` for `OperatingSystem`, `RuntimeInformation`, path separators in added lines | empty |

*Status: COMPLETE — reviewer (second reader), 2026-10-23*

## Verdict: COMMENT (re-review, cycle 1)

Covers: the round's changes; steps built or redone: Step 4.
**Model:** claude-sonnet-5-5
Reviewed by: reviewer, as the second reader (Codex reads the same follow-up as primary). Scope: the five fix-list rows against the amended plan (Step 4, Decisions rows, P7, P8) and questions.md Q2/Q3; the changed files `DomainEventPublishingInterceptor.cs`, `Reflekt.Persistence/{SeedOptions,DependencyInjection,DbInitializer}.cs`, both csproj files, and the three handlers beside row 2. I did not redo the first review.

No CRITICAL or HIGH finding. Row 1 (HIGH) and row 2 (MEDIUM) are resolved as the plan now specifies; rows 3, 4 and 5 are resolved. One LOW stands.

### Plan conformance (Step 4 as amended, P7, P8)
- `SavedChangesAsync` is `async` and awaits `PublishInOrderAsync(events)` (interceptor line 65); `TransactionCommittedAsync` returns the same task (line 106), which EF awaits inside `CommitAsync`. `Task.Run` and `PublishDetached` are absent from the interceptor (the one `Task.Run` left in `src` is `PhaseActionPolicyProvider`, Step 2). `Mode.WaitForNone` is absent; `Mode.WaitForAll` is on line 167; the token is `CancellationToken.None` (line 154); failures are logged one per handler through `LogFailures` and never rethrown (`SuppressThrowing`). The held/collected lists, the order and the rollback clearing are unchanged. Trace (e) holds.
- Row 1 is closed for the stated sequence (vote 1, then retract 0): on both paths the broadcast finishes before the endpoint continues, so a request sent after the first response cannot be overtaken. Explicit-transaction endpoints (`CastVote`, `CreateGroup`, `DissolveGroup`) publish from the commit callback, with the transaction already committed at the `DbTransaction` level, so the handlers' fresh-scope reads see committed data. The interceptor is registered scoped (`Reflekt.Persistence/DependencyInjection.cs:16`), so the lists are per-request state and a handler's own scope has its own interceptor: no re-entrancy on the shared lists.
- Row 2 matches P8: `BroadcastGroupCreatedHandler` and the vote handlers are unchanged, read through `IServiceScopeFactory`, and now run between commit and response. The events still carry only the aggregate (root CLAUDE.md rule kept). The concurrent-commit residual and the OpenGraph channel residual are recorded in the Fix Round 1 block as the architect asked.
- Row 3: `SeedOptions` moved verbatim (`git show HEAD:` of the API copy against the new file: only the namespace differs; same `init` properties). `MigrateAndSeedAsync` and `DbInitializer.SeedAsync` read `IOptions<SeedOptions>`; no `IConfiguration` seed read remains; the API's `AddOptions<SeedOptions>().Bind("Seed")` is the one binding and still resolves through `global using Reflekt.Persistence`. Behaviour kept: absent `Enabled` is `false`, absent email falls back to `dev@localhost`.
- Row 4: `Blocks.Persistence` no longer references `Blocks.Core`; the API references `Blocks.Core` and `Blocks.Exceptions` directly (`Blocks.Core` already references `Blocks.Exceptions`). The direct `Blocks.Exceptions` reference is justified by the API's own use. Both runs built the tree.
- Row 5: 16 `*.Designer.cs` files in `Migrations/` (counted); the host-start, `SeedOptions` and `Blocks.Core` rows read as corrected or fixed.

### Carry-over rows touched this round
- `SeedOptions` bound but unread: confirmed fixed.
- `Blocks.Persistence` unused `Blocks.Core` reference: confirmed fixed.
- Host-start flake and designer count: wording corrected. The flake stays an open observation, not a defect (my six-run probe of `EvidenceLinkEndpointTests` in the first review passed every time; this round's integration run passed on the first attempt).

## Findings

### [LOW] Step 17's F40 list item 3 now states the opposite of the code
**File:** `docs/specs/F38-BackendConventions/delivery/implementation.md:456` (also `:57`, `:64`, `:66`, the Step 4 block)
**Origin:** implementation
**Issue:** Item 3 of the F40 hand-off list says the interceptor "publishes in a task the request never awaits (plan Decisions row, P5)". P5 is superseded; the save now awaits. The Fix Round 1 Deviations line admits the item is out of date but leaves it, so F40 would be handed a departure from `domain-patterns` that no longer exists (the only one left is that failures are logged, not rethrown). The Step 4 block (`:57`, `:64`, `:66`) still describes `PublishDetached` and `Task.Run` as the build; the Fix Round 1 block supersedes it, but a reader of the Step 4 block alone sees code that is gone.
**Fix:** Reword item 3 to the P7 state (the publisher awaits `WaitForAll` as the skill does; the only difference is that handler failures are logged, not rethrown). Optionally mark the Step 4 block "superseded by Fix Round 1, P7".
**Confidence:** 85/100

## Positive Observations
- The fix is the smallest change that closes the HIGH: one file, about ten lines, and every property the plan guards (order, `WaitForAll`, per-handler log, no rethrow, token) is kept.
- The Fix Round 1 code trace follows both publish paths separately and names the accepted residuals instead of hiding them.
- The `SeedOptions` move keeps the API's `ValidateOnStart` count at four and adds no second options type.

## Gaps
- No test pins the new property (a broadcast completes before the response; a failing handler leaves the status unchanged). The plan asks for run records only (owner's no-new-tests rule, P1) and the trace stands in; the integration run proves the awaited path breaks no vote, card or group flow, not that the order holds. Not a finding. If a test is ever allowed: a handler that blocks until released, asserting the endpoint call has not returned.
- The OpenGraph channel residual (a full 100-slot channel makes an attach-link request wait) is unreachable at this app's scale and is already listed for F40.

## Open Questions
- None below the report cutoff.

## Evidence
| Check | Result | Command | Output |
|---|---|---|---|
| Run record (integration) | pass | `verify-run.js --role integration`, run `mvl3fprl-da6b27e6` | `.claude/audit/verify-verdict.json:65`, pass, 45.7 s, tree hash `12f8eaf0…`, after rows 1-4. The whole integration project, so the files Q2 names are included. I ran no suite. |
| Run record (fast, tied) | pass | `verify-run.js --profile fast`, run `mvl3gt0s-8bb944ee` | `verify-verdict.json:66`, pass, 7.3 s, same tree hash, tied over the seven changed files (fell back to `roles.unit`; the integration run above is what exercises the interceptor) |
| Plan accept: `Task.Run`, `PublishDetached` in the interceptor | no hits | `grep -n` | empty |
| Plan accept: `Mode.WaitForNone` in the interceptor | no hits | `grep -n` | only `Mode.WaitForAll` (line 167) |
| Seed settings read from configuration | none left | `grep -rn` for `"Seed:` and `GetValue<bool>("Seed` over `src` | empty |
| `SeedOptions` verbatim move | namespace only | `git show HEAD:…/Reflekt.API/SeedOptions.cs` against the new file | identical members |
| Project references | as described | `grep -rn "Blocks.Core\|Blocks.Exceptions" src --include=*.csproj` | `Blocks.Core` to `Blocks.Exceptions`; `Reflekt.API` to both; none from `Blocks.Persistence` |
| Migration designer count | 16 | `ls Migrations \| grep -c Designer.cs` | 16 |
| Platform-conditional code in the changed files | none | read of the changed files | none |

*Status: COMPLETE — reviewer (second reader), 2026-10-23*
