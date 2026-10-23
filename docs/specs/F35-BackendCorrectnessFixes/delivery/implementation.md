# F35 — Backend Correctness & Security Fixes — Implementation

**Model:** claude-opus-5-5[1m]

## Step 1 — Scoped, transaction-aware event publication
**Files:**
- `src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs` — now also implements `IDbTransactionInterceptor`. It keeps two lists: `_collectedEvents` (the save in progress) and `_heldEvents` (waiting for an explicit transaction's commit). `SavedChangesAsync` moves collected to held when `Context.Database.CurrentTransaction` is open, and otherwise publishes at once. `SaveChangesFailedAsync` and `SaveChangesCanceledAsync` clear collected. `TransactionStartedAsync`, `TransactionRolledBackAsync` and `TransactionFailedAsync` clear held, and `TransactionCommittedAsync` publishes it. Publication still uses the reflective runtime-type dispatch with `Mode.WaitForNone`, now in one `PublishAsync` helper that skips an empty list.
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — `AddSingleton` → `AddScoped<DomainEventPublishingInterceptor>()`; it is still resolved through the `sp` of `AddDbContext((sp, options) => …)`, which is the request's scope.
- `src/BuildingBlocks/Blocks.Persistence/Repositories/RepositoryBase.cs` — new `BeginTransactionAsync(CancellationToken)` returning `Db.Database.BeginTransactionAsync(ct)` (`IDbContextTransaction`).
- `src/BuildingBlocks/Blocks.Persistence/Blocks.Persistence.csproj` — one `<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />`.
- `Directory.Packages.props` — one `<PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.13" />` in the EF Core group; no other line changed.

**Skills:** persistence-patterns
**Decisions:**
- `SaveChangesCanceledAsync` also clears collected, beside the plan's `SaveChangesFailedAsync`. EF raises the canceled callback, not the failed one, for a save cut by cancellation, so the same rule (a save that did not complete publishes nothing) needs both.
- Held events are appended (`AddRange`), so several saves inside one explicit transaction all publish at its commit.

**Deviations:** None

**Accept:**
- `grep -rn "AddSingleton<DomainEventPublishingInterceptor>" src` → 0 hits.
- `grep -c "TransactionCommittedAsync\|TransactionRolledBackAsync\|TransactionFailedAsync\|SaveChangesFailedAsync\|TransactionStartedAsync" …/DomainEventPublishingInterceptor.cs` → 6.
- `grep -n "Microsoft.EntityFrameworkCore.Relational"` → `Blocks.Persistence.csproj:9` and `Directory.Packages.props:14`, one each.
- Code trace, spec AC1 (`DomainEventPublishingInterceptor.cs`):
  - (a) a failed save publishes nothing: `SaveChangesFailedAsync` sets `_collectedEvents = []` (line 71), and `SavedChangesAsync` never runs for that save; a later save's `SavingChangesAsync` replaces collected anyway.
  - (b) a rolled-back transaction publishes nothing: its saves' events went to held (line 59), and `TransactionRolledBackAsync` sets `_heldEvents = []` (line 110); `TransactionFailedAsync` does the same (line 119).
  - (c) two concurrent scopes never share a list: `DependencyInjection.cs:18` registers the interceptor `AddScoped`, so each scope's `ReflektDbContext` gets its own instance.
  - (d) a transaction disposed without commit never publishes later: EF raises no callback on that dispose, so held keeps the events until the next `TransactionStartedAsync` (an explicit transaction, or EF's own save transaction), which sets `_heldEvents = []` (line 91). Only `TransactionCommittedAsync` publishes held (line 102).
  - (e) a save with no explicit transaction, which EF wraps in its own transaction: by `SavedChangesAsync` EF's batch transaction has committed and been disposed, so `CurrentTransaction` is null and collected is published once (line 63). That inner transaction's callbacks only touch held, which is empty then (its start cleared it), so nothing is published twice.

**Verify:**
- `verify-run.js --role build` → pass (run `mvkjvmmh-bbc48e35`, 0 `error CS`).
- Step close `verify-run.js --profile fast --tied --changed {the 5 files}` → pass (run `mvkjvz4z-7bec7068`; `tied_fallback`: no installed plugin runs vstest tests, so the whole `roles.unit` ran). `label-tests.js --run mvkjvz4z-7bec7068` → timed no test; nothing to label.
- Runs (`SprintEndpointTests.cs`, `VoteEndpointTests.cs`, `CardEndpointTests.cs`): `verify-run.js --role integration` → pass, whole integration project, 51.7 s (run `mvkjwjbd-cc73d917`). The runner does not print per-test output, so no `ManyServiceProvidersCreatedWarning` text could be read. EF throws on that warning by default, so a green host start in every class is the evidence that it did not occur.

## Step 2 — Multi-save endpoints in one transaction; inline sends after commit
**Files:**
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs` — the transition out of Writing moves into a private `LeaveWritingAsync`. It opens one transaction through `sprintRepository.BeginTransactionAsync`, adds the solo groups, saves, sets the note cards' `GroupId`, saves, then commits. Every other transition keeps its single save.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs` — both saves sit inside one transaction opened after the last validation check, then committed; `GroupCreated` is sent after `CommitAsync`.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/DissolveGroupEndpoint.cs` — the per-card loop with a save in each pass is gone. All solo groups are built at once, added with `AddRangeAsync` and saved once to get their ids. The cards are then pointed at them and the old group is removed in a second save, all in one transaction. `GroupDissolved` is sent after `CommitAsync`.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/RemoveCardFromGroupEndpoint.cs` — both saves sit inside one transaction, committed before the response.

**Skills:** persistence-patterns
**Decisions:**
- `AdvancePhase`'s Writing path is extracted into `LeaveWritingAsync`, because it now opens its own transaction and has eight operations. The method was already near the conventions' 60-line limit.
- Each transaction opens after the endpoint's last early return (`SendNotFoundAsync`, `SendForbiddenAsync`, `ThrowError`). So no response is sent while a transaction is still open.

**Deviations:** None

**Accept:**
- Code trace, `DissolveGroupEndpoint.cs`: there is no `foreach` with a save left. The solo groups are built at line 57, `AddRangeAsync` follows, and a single save runs at line 60. Line 63 points the cards at their groups in a plain `for`, and the second save is at line 66.
- Code trace, every `SaveChangesAsync` on the changed path sits between `BeginTransactionAsync` and `CommitAsync`:
  - `AdvancePhaseEndpoint.cs`: begin 85, saves 93 and 98, commit 99. The non-Writing save at line 78 is the unchanged single save.
  - `CreateGroupEndpoint.cs`: begin 61, saves 66 and 95, commit 96.
  - `DissolveGroupEndpoint.cs`: begin 55, saves 60 and 66, commit 67.
  - `RemoveCardFromGroupEndpoint.cs`: begin 48, saves 53 and 57, commit 58.
- Code trace, `hubContext…SendAsync` after `CommitAsync`: `CreateGroupEndpoint.cs` commit 96, then `GroupCreated` at line 99. `DissolveGroupEndpoint.cs` commit 67, then `GroupDissolved` at line 70. An exception before the commit leaves the method before either send.
- Code trace, leaving Writing: `sprint.AdvancePhase()` raises `PhaseAdvancedEvent` in memory. The first save (line 93) collects it, and because the explicit transaction is open, Step 1's `SavedChangesAsync` moves it to held. It is published only by `TransactionCommittedAsync`, at line 99, after the second save has written every note's `GroupId`. So `PhaseChanged` goes out after the cards are in their groups.

**Verify:**
- `verify-run.js --role build` → pass (run `mvkk05j0-bf128e06`, 0 `error CS`).
- Step close `verify-run.js --profile fast --tied --changed {the 4 files}` → pass (run `mvkk0apx-d4685c30`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Runs (`SprintEndpointTests.cs`, `VoteEndpointTests.cs`, `CardEndpointTests.cs`, including the cases that advance Writing → Grouping): `verify-run.js --role integration` → pass, whole project, 49.9 s, `failed_tests: null` (run `mvkk0lmj-5677056d`).

## Step 3 — Domain rule violations as 409; safe error bodies; missing user claim as 401
**Files:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs` — eight state-rule throws change from `InvalidOperationException` to `DomainException`, keeping their messages: `Start`, `AdvancePhase` (not Active; already Reporting), `Close`, `SetFocus` ×2 and `ClearFocus` ×2. "No timer default for phase" and "Unknown phase" stay `InvalidOperationException`. New `EnsureOpen()` throws `DomainException("Sprint is closed.")` when `State == SprintState.Closed`; Draft and Active pass. Step 4 calls it.
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs` — `SetReviewStatus` on a non-action item throws `DomainException`; both "EvidenceLink … not found" arms stay.
- `tests/Reflekt.UnitTests/Sprints/SprintTests.cs` — the 6 assertions at lines 83, 93, 134, 142, 188 and 198 (numbered as in the original file) change from `Throw<InvalidOperationException>()` to `Throw<DomainException>()`: `Start_FromActive_Throws`, `Start_FromClosed_Throws`, the AdvancePhase-from-Reporting case, `AdvancePhase_OnDraftSprint_Throws`, `Close_FromDraft_Throws` and `Close_FromClosed_Throws`. `using Blocks.Domain;` is added. These are assertion updates the fix changes, not new tests. `EvidenceLinkTests` is unchanged.
- `src/Services/Reflekt/Reflekt.API/Middleware/GlobalExceptionMiddleware.cs` — mapping is now `DomainException` → 409, any `HttpException` → its own `HttpStatusCode`, `ArgumentException` → 400, everything else → 500. `ValidationException` (400) and `OperationCanceledException` (499) keep their own catches. Every body is a private `ErrorResponse` record serialised with `JsonSerializerDefaults.Web` (camelCase), with keys `statusCode`, `message`, `traceId`, `errors[]` (`propertyName`, `errorMessage`) and `details`. `errors` and `details` carry `JsonIgnore(WhenWritingNull)`, so they are left out when null. Outside Development a 5xx body's message is `An unexpected error occurred.` and there is no `details`. In Development the body is today's, message plus stack. The 5xx is still logged with its exception.
- `src/Services/Reflekt/Reflekt.API/Auth/ClaimsPrincipalExtensions.cs` — `GetUserId()` throws `UnauthorizedException("User id claim is missing.")`, which the middleware maps to 401.

**Skills:** error-handling (spec overrides, as the plan's `Departs from` row says: 409 instead of the skill's 400, and a camelCase body with no 5xx details, where the skill shows PascalCase)
**Decisions:**
- `ErrorResponse` and `ValidationError` are private nested records in the middleware, replacing the anonymous objects. An anonymous type cannot carry the `JsonIgnore` attribute needed to leave `details` out.
- The three separate `HttpException` subclass arms (`BadRequestException`, `NotFoundException`, `UnauthorizedException`) are folded into the one `HttpException` arm. `DomainException` is matched first. It does not derive from `HttpException`, so the order is for reading only.

**Deviations:** None. No integration assertion needed updating: the whole integration run passed unchanged (below).

**Accept:**
- `grep -rn "InvalidOperationException" src/Services/Reflekt/Reflekt.Domain --include=*.cs` → 4: `Sprint.cs:64` (timer default), `Sprint.cs:123` (unknown phase), `Card.cs:88` and `Card.cs:97` (EvidenceLink not found). Without `--include=*.cs`, the plan's grep also matches the built `Reflekt.Domain.dll` under `bin/` and `obj/`; those are build output, not source.
- `grep -c "Throw<DomainException>" tests/Reflekt.UnitTests/Sprints/SprintTests.cs` → 6.
- `grep -n "InvalidOperationException" src/Services/Reflekt/Reflekt.API/Auth/ClaimsPrincipalExtensions.cs` → none.
- Code trace, the 5xx body outside Development (`GlobalExceptionMiddleware.cs`):
  - `MapStatusCode` gives 500 for an unmapped exception.
  - `isServerError` (line 51) is true, so the exception is logged with its stack.
  - `message` (line 55) is the constant `An unexpected error occurred.` because `env.IsDevelopment()` is false. No exception text reaches the body.
  - `Details` is set to null outside Development, and `[JsonIgnore(Condition = WhenWritingNull)]` (line 90) leaves the key out of the JSON. `Errors` is null and left out too.
  - The serialised body is exactly `{"statusCode":500,"message":"An unexpected error occurred.","traceId":"…"}` (spec AC3).

**Verify:**
- `verify-run.js --role build` → pass (run `mvkk4agi-8c906848`, 0 `error CS`).
- Step close `verify-run.js --profile fast --tied --changed {the 5 files}` → pass (run `mvkk4enz-60fcb08e`, the whole `roles.unit` through the tied fallback, `SprintTests` included). `label-tests.js` → timed no test.
- Runs (every file in `tests/Reflekt.IntegrationTests/`): `verify-run.js --role integration` → pass, 71.7 s, `failed_tests: null` (run `mvkk4pvn-6ccc3775`). No integration test asserted a body key's casing or a status that this step moves, so none was updated.

## Step 4 — Closed-sprint guard; `SetReviewStatus` 404 on a non-action item
**Files:**
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/AddCardToGroup/AddCardToGroupEndpoint.cs` — `sprint.EnsureOpen()` after the 403 block, before the phase-action check.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs` — the same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/RemoveCardFromGroupEndpoint.cs` — the same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/DissolveGroupEndpoint.cs` — the same.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs` — the same.
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/UpdateColumns/UpdateColumnsEndpoint.cs` — `sprint.EnsureOpen()` after the 403 block, before `UpdateColumnConfig` (the endpoint has no phase-action check).
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` — the private helper `LoadTimedSprintAsync`, which every timer method (`StartTimer`, `PauseTimer`, `ResumeTimer`, `ResetTimer`, `AddTime`) already calls, now calls `sprint.EnsureOpen()` inside `try`/`catch (DomainException)`. The catch throws `HubException("Sprint is closed.")`, so the domain exception never reaches the client. `JoinSprint`'s timer auto-start condition gains `sprint.State != SprintState.Closed`, so a closed sprint skips the start without throwing. `using Blocks.Domain;` is added.
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/SetReviewStatus/SetReviewStatusEndpoint.cs` — the card 404 condition gains `|| card.CardType != CardType.ActionItem`. The existing 409 (not Active, or not Writing) is unchanged, and the previous sprint is never guarded.

**Skills:** domain-patterns
**Decisions:**
- The hub's "one private helper" is the existing `LoadTimedSprintAsync`, not a new method. It already loads the sprint through a created scope for all five timer methods, after `RequireScrumMasterAsync` (the hub's 403). A new helper would load the same sprint a second time. `EnsureOpen()` runs before its timer-phase check, matching the endpoints' order: guard first, then the phase check.

**Deviations:** None

**Accept:**
- `grep -rln "EnsureOpen()" src/Services/Reflekt/Reflekt.API` → exactly the 6 endpoint files plus `Hubs/SprintHub.cs`. In `SprintHub.cs` the one call is at line 233, inside `LoadTimedSprintAsync` (line 224), with `catch (DomainException)` at line 235 throwing `HubException("Sprint is closed.")` at line 237.
- Code trace, guard order: `SendForbiddenAsync`, then `EnsureOpen()`, then `phaseActionCache.IsAllowed` (or `UpdateColumnConfig`), then the first save. Line numbers:
  - `AddCardToGroup` 35 → 39 → 41 → save 71
  - `CreateGroup` 38 → 42 → 44 → save 68
  - `RemoveCardFromGroup` 35 → 39 → 41 → save 55
  - `DissolveGroup` 38 → 42 → 44 → save 62
  - `UpdateGroupTitle` 34 → 38 → 40 → save 48
  - `UpdateColumns` 24 → 28 → `UpdateColumnConfig` 30 → save 35
- Code trace, a closed sprint on `CreateGroup` and `UpdateColumns`: `EnsureOpen()` throws `DomainException("Sprint is closed.")` before any save. Step 3's middleware maps it to 409 with message `Sprint is closed.`, so the data is unchanged.
- Code trace, `SetReviewStatus`: the `CardType` check (line 63) comes before `card.SetReviewStatus` (line 75) and answers 404. The previous sprint is only read (`GetMostRecentClosedByTeamIdAsync`), never guarded, so the previous-sprint action-item path is untouched.
- `CreateCard_ClosedSprint_Returns409` and the other closed-sprint cases stay green (integration run below).

**Verify:**
- `verify-run.js --role build` → pass (run `mvkk8npo-2c6c4664`, 0 `error CS`).
- Step close `verify-run.js --profile fast --tied --changed {the 8 files}` → pass (run `mvkk8rf0-5733b5cc`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Runs (`CardEndpointTests.cs`, `SprintEndpointTests.cs`): `verify-run.js --role integration` → pass, whole project, 73.1 s, `failed_tests: null` (run `mvkk9ofw-36d47eef`).

## Step 5 — Input validation: enums by name, `CardIds`, group title
**Files:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardCommand.cs` — the validator's `ColumnType` and `CardType` rules gain `.Must(IsEnumName<…>)`, with today's messages "Invalid column type." and "Invalid card type.". The new private static `IsEnumName<TEnum>` accepts a value only when it equals a declared enum name exactly (`Enum.GetNames<TEnum>().Contains`): case-sensitive, so a numeric string such as "1" fails. `using Reflekt.Domain.Cards;` is added.
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs` — the two `Enum.TryParse` + `ThrowError` blocks become `Enum.Parse<ColumnType>` and `Enum.Parse<CardType>` of the already-validated values.
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/SetReviewStatus/SetReviewStatusEndpoint.cs` — the inline `Enum.TryParse` + `ThrowError` becomes `Enum.Parse<ReviewStatus>`. Its existing validator already allows only `Done`, `CarriedOver` and `Dropped`, so `None` and numbers never reach it.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs` — new sibling `CreateGroupRequestValidator`: `CardIds` checked with `Cascade(CascadeMode.Stop)`, then `NotEmpty()` (rejects null and empty), then `Must(distinct)` with the message "Card ids must be distinct.". The endpoint's "At least 2 cards" 400 is kept.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs` — new sibling `UpdateGroupTitleRequestValidator`: `Title` at most 100 characters when present.

**Skills:** create-feature (its `workflows/Validator.md`, FastEndpoints `Validator<T>` variant)
**Decisions:**
- The validators use plain FluentValidation (`NotEmpty`, `MaximumLength`, `Must`), as every existing validator in this service does. The skill's `NotEmptyWithMessage` and `MaxLength` helpers do not exist in this repo's `Blocks.Core`. This follows the codebase pattern, not a deviation from the plan.
- `Cascade(CascadeMode.Stop)` on `CardIds`, so the distinct check never runs on null.
- For the reviewer: validation runs before the handler. So a `CreateCard` request with both an invalid enum and a token for the wrong sprint now gets 400 where it used to get 401. This comes from the plan's "one parse per input in the validator", and no existing test covers that combination.

**Deviations:** None

**Accept:**
- `grep -c "Enum.TryParse"` → `CreateCardEndpoint.cs` 0 and `SetReviewStatusEndpoint.cs` 0.
- `grep -c "Validator<CreateGroupRequest>\|Validator<UpdateGroupTitleRequest>"` → 1 in `CreateGroupEndpoint.cs` and 1 in `UpdateGroupTitleEndpoint.cs`, 2 in all.
- Code trace: FastEndpoints runs `CreateCardCommandValidator` before `HandleAsync`. A numeric `ColumnType` "1" fails `IsEnumName<ColumnType>` (`CreateCardCommand.cs:19`), because "1" is not one of `Column1`, `Column2`, `Column3` or `ActionItems`. The answer is 400, and the handler's `AddAsync` and `SaveChangesAsync` never run, so nothing is saved.
- Code trace: a null `CardIds` fails `NotEmpty()` in `CreateGroupRequestValidator` before the handler, so the handler's `req.CardIds.Count` (`CreateGroupEndpoint.cs:60`) is never reached with null. A single card passes the validator and gets the kept "At least 2 cards" 400.

**Verify:**
- `verify-run.js --role build` → pass (run `mvkkdu0a-3a2758fa`, 0 `error CS`).
- Step close `verify-run.js --profile fast --tied --changed {the 5 files}` → pass (run `mvkkdxim-e95589db`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Runs (`CardEndpointTests.cs`, `EvidenceLinkEndpointTests.cs`): `verify-run.js --role integration` → pass, whole project, 53.6 s, `failed_tests: null` (run `mvkke8i6-28029f14`).

## Step 6 — Participant soft removal; readers that skip removed participants
**Files:**
- `src/Services/Reflekt/Reflekt.Domain/Participants/Participant.cs` — new `DateTime? RemovedAt { get; private set; }` and the read `IsRemoved => RemovedAt is not null` (a get-only expression property, which EF does not map).
- `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs` — `Kick()` returns at once when `IsRemoved`, raising no event. Otherwise it sets `RemovedAt = DateTime.UtcNow` and raises the existing `ParticipantKickedEvent` once.
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/ParticipantConfiguration.cs` — `DisplayName` gets `UseCollation("NOCASE")` (the `Users.Email` precedent), `RemovedAt` is `IsRequired(false)`, and a new unique index on `(SprintId, DisplayName)` with filter `"RemovedAt" IS NULL`.
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` — `GetBySprintIdAsync` and `ExistsByDisplayNameAsync` add `p.RemovedAt == null`. `GetByGuestTokenAsync`, `GetByUserAndSprintAsync` and the base `GetByIdAsync` are unchanged, so they still return removed participants. `SaveChangesAsync` is overridden: a `DbUpdateException` whose inner `SqliteException` has extended code 2067 (`SQLITE_CONSTRAINT_UNIQUE`) and a message naming `Participants.SprintId, Participants.DisplayName` becomes `DomainException("Name already taken.", ex)`. Every other exception passes the `when` filter by and propagates unchanged. `Remove` stays until Step 8.

**Skills:** domain-patterns
**Decisions:**
- The unique-violation match uses both the extended error code and the index's column list. So a different unique index on the same table (`GuestToken`, `(SprintId, UserId)`) still surfaces as it does today.
- `ExistsByDisplayNameAsync` keeps its `ToLower()` comparison. With the column now `NOCASE` the result is the same, and the change stays to the one added filter.
- The original `DbUpdateException` is kept as the `DomainException`'s inner exception, so the cause stays in the log.

**Deviations:** None

**Accept:**
- `grep -n "RemovedAt" ParticipantRepository.cs` → lines 19 (`GetBySprintIdAsync`) and 26 (`ExistsByDisplayNameAsync`) only.
- Code trace: these readers all go through `ParticipantRepository.GetBySprintIdAsync`, which now skips removed participants:
  - the advance-phase check's voter count (`AdvancePhaseCheckEndpoint.cs:85`, `nonVotingCount` at 88);
  - the participant list (`GetParticipantsEndpoint.cs:46`);
  - the anonymous summary (`GetSprintSummaryEndpoint.cs:21`);
  - both reports (`GetSprintReportEndpoint.cs:52`, `GetParticipantReportEndpoint.cs:52`).
- Code trace, the 409 mapping (spec AC9, P4): a duplicate active name inserted past the read check fails on the new filtered index. `ParticipantRepository.SaveChangesAsync` (catch at line 39) rethrows it as `DomainException("Name already taken.")` (line 41). Step 3's middleware maps that to 409.

**Verify:**
- `verify-run.js --role build` → pass (run `mvkkjp23-d2f98983`; `Reflekt.Persistence.dll` rebuilt at 06:08:19Z).
- Step close `verify-run.js --profile fast --tied --changed {the 4 files}` → pass (run `mvkkjz75-770de17d`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Runs: none, as the plan says. The EF model now differs from the snapshot until Step 7's migration, so the integration host's `Migrate()` would reject it. Step 7's integration run covers this step.

## Step 7 — Migration with the duplicate-name repair; seed without a double vote
**Files:**
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20261023061022_SoftKickParticipants.cs` — new, generated by `dotnet-ef` 10.0.13 (installed to `out/f35-scratch/tools`) and then edited. `Up` runs: `AddColumn` `RemovedAt` (nullable TEXT, line 87); the three repair `Sql` statements (93–95); `AlterColumn` `DisplayName` → `NOCASE` (97); `CreateIndex` `IX_Participants_SprintId_DisplayName`, unique, filter `"RemovedAt" IS NULL` (108).
  - The repair builds a temp table `ParticipantRenames` from one recursive CTE, applies it with one `UPDATE`, then drops the table.
  - The CTE handles one duplicate per recursion step, in id order. It carries the names assigned so far, per sprint, as a JSON array of `{SprintId}:{lower(name)}` entries. Each duplicate takes the smallest suffix `N ≥ 2` such that `substr(name, 1, 50 − length(' ' || N)) || ' ' || N` is free against every active name in its sprint (`COLLATE NOCASE`) and every name assigned before it in that sprint. A free suffix is always found by `2 · (largest sprint) + 2`, the bound of the `Suffixes` CTE.
  - The rule is stated in the comment above the SQL (Migration Notes).
  - `Down` is as generated: drop the index, drop the column, `AlterColumn` back without the collation. The renames stay.
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/20261023061022_SoftKickParticipants.Designer.cs` — new, generated, unchanged.
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/ReflektDbContextModelSnapshot.cs` — generated: `DisplayName` `.UseCollation("NOCASE")`, the `RemovedAt` property, and the new filtered unique index. The tool also moved the `ProductVersion` annotation from `9.0.4` to `10.0.13`, the EF version F37 already installed. That line is metadata only.
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — the second `Vote.Create(wentWellCards[0].GroupId!.Value, participants[0].Id)` in `votesA` is removed, so the seed no longer votes twice on one group with one participant.

**Skills:** persistence-patterns
**Decisions:**
- `dotnet tool install` ran with `NUGET_PACKAGES`, `NUGET_HTTP_CACHE_PATH`, `TMP` and `TEMP` pointed under `out/f35-scratch/`. So the install wrote nothing outside the worktree. `dotnet-ef` itself ran with `--no-build`, against the build made just before it.
- The repair runs in a temp table, not as a single `WITH … UPDATE`. The CTE reads `Participants`, so it must be fully evaluated before any row changes.
- Names are compared with explicit `COLLATE NOCASE` and `lower()`, both ASCII folding as before. So the repair does not depend on whether the column's collation has been rebuilt yet.

**Deviations:**
- The plan's order in `Up` is: add the column, alter the collation, repair, create the index. In the code, the repair `Sql` comes before the `AlterColumn`. Why: EF's SQLite generator defers a table rebuild to the end of the migration. With `Sql` after the `AlterColumn`, `migrations script` warned (`30200`): "An operation of type 'SqlOperation' will be attempted while a rebuild of table 'Participants' is pending". The scripted SQL put the repair before the rebuild in both orders. After the reorder the warning is gone, and the script runs exactly: `ALTER TABLE … ADD "RemovedAt"`; the repair; the index; the `NOCASE` rebuild (`ef_temp_Participants`), which re-creates every index, the new one included. The plan's requirement holds: duplicates are repaired before the unique index exists.

**Accept:**
- Scratch probe (`out/f35-scratch/probe`):
  - It migrates a fresh SQLite file to `20260519160025_AddReviewStatus` through `IMigrator.MigrateAsync`. It inserts one user, one team and two sprints, then the participants below (sprint 2 is the control). It runs `MigrateAsync()` to the latest, then lists the names.
  - A raw duplicate `INSERT` (`'ANA'`, sprint 1) follows, then a duplicate saved through `ParticipantRepository` (`'ANA 2'`, sprint 1).
  - Before (id | sprint | name):
    ```
    1 | 1 | Ana        2 | 1 | ana        3 | 1 | Ana 2
    4 | 1 | N×50       5 | 1 | N×50       6 | 2 | Ana
    7 | 2 | b   8 | 2 | b   9 | 2 | b   10 | 2 | b 2
    11 | 2 | X×49+A   12 | 2 | X×49+a   13 | 2 | X×49+B   14 | 2 | X×49+B
    ```
  - Output after:
    ```
    APPLIED: 20260519160025_AddReviewStatus, 20261023061022_SoftKickParticipants
      1 | 1 | Ana | NULL
      2 | 1 | ana 3 | NULL
      3 | 1 | Ana 2 | NULL
      4 | 1 | NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN | NULL
      5 | 1 | NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN 2 | NULL
      6 | 2 | Ana | NULL
      7 | 2 | b | NULL
      8 | 2 | b 3 | NULL
      9 | 2 | b 4 | NULL
      10 | 2 | b 2 | NULL
      11 | 2 | XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXA | NULL
      12 | 2 | XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX 2 | NULL
      13 | 2 | XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXB | NULL
      14 | 2 | XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX 3 | NULL
    Name lengths: 1 | 3, 2 | 5, 3 | 5, 4 | 50, 5 | 50, 6 | 3, 7 | 1, 8 | 3, 9 | 3, 10 | 3, 11 | 50, 12 | 50, 13 | 50, 14 | 50
    RAW DUPLICATE INSERT: SqliteException 19/2067: SQLite Error 19: 'UNIQUE constraint failed: Participants.SprintId, Participants.DisplayName'.
    REPOSITORY DUPLICATE SAVE: DomainException "Name already taken." (inner DbUpdateException)
    ```
  - Sprint 1 matches the plan's expected result: `Ana`, `ana 3`, `Ana 2`, the first 50-character name unchanged, the second its first 48 characters + ` 2`. All 14 rows are still there.
  - Sprint 2 (added for coverage) shows:
    - its own `Ana` untouched by sprint 1's repair;
    - a suffix skipped because an earlier rename took it (`b 3` then `b 4`, past the existing `b 2`);
    - two different trimmed bases that meet at the same name, the second taking ` 3` (`X×48 2` then `X×48 3`).
  - The last line shows Step 6's mapping, saved through the repository: the duplicate is rejected with "Name already taken." (spec AC9).
- Scratch stress run (`out/f35-scratch/stress`): 240 fresh databases migrated from empty to the latest, 8 at a time → `ok=240 errors=0`. It was run to look for a start-up fault from the new migration after the integration red below; there was none.
- `verify-run.js --role integration --slug F35-BackendCorrectnessFixes` → pass, 104 tests (runs `mvkktpte-b8327d68` and `mvkkuwi8-29d244cb`, 43.8 s, `failed_tests: null`). Every class starts the host, whose start-up `Migrate()` rejects a pending model change. So the migration and the snapshot match the model, and this covers Step 6's `ParticipantEndpointTests.cs` and `SprintEndpointTests.cs` with the rest of the project.
- `grep -c "participants\[0\].Id" DbInitializer.cs` → 4 (5 at `HEAD`).

**Verify:**
- `verify-run.js --role build` → pass (run `mvkkqn5s-5f90a2f3`, after the reorder).
- Step close `verify-run.js --profile fast --tied --changed {the 4 files}` → pass (run `mvkksfln-94d5f991`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Integration: the first run `mvkksjhg-013fceaf` was red. 1 of 104 failed: `CardEndpointTests.EditCard_ByOtherParticipant_Returns403`, at host start in `CardEndpointTests.InitializeAsync()` (`CardEndpointTests.cs:78`, `_factory.CreateClient()` → `WebApplicationFactory.StartServer()` → `CreateHost`). The two re-runs on the same tree hash (`c4cfd5ee…`) are green. This is the known flaky host start that F37 recorded before this feature (`docs/specs/F37-DependencyUpgrades/delivery/change-notes.md` Note 1: same class, same line, same frames, "exception message not captured"). It did not block the step, so it is noted here, not handed back as a bug. The runner again cut out the exception message. The stress run above rules out the new migration as its cause.

## Step 8 — Removed token rejected; query token only on hubs; soft kick endpoint; Google rejoin 409
**Files:**
- `src/Services/Reflekt/Reflekt.API/Auth/GuestTokenAuthenticationHandler.cs` — the `access_token` query value is read only when `Request.Path.StartsWithSegments("/hubs")` (line 19); otherwise that link of the chain is `null`. The `X-Guest-Token` header and the `Bearer` form are unchanged and keep their order. A token whose participant `IsRemoved` returns `AuthenticateResult.Fail("Participant has been removed")` (line 28).
- `src/Services/Reflekt/Reflekt.API/Features/Participants/KickParticipant/KickParticipantEndpoint.cs` — `participantRepository.Remove(participant)` is gone. The endpoint calls `participant.Kick()`, saves, and returns 204. Its inline 404 (sprint) → 409 (not Active) → 403 (not owner) → 404 (participant) checks are unchanged and in today's order.
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprintGoogle/JoinSprintGoogleEndpoint.cs` — when the existing participant `IsRemoved`, the endpoint answers `ThrowError("You were removed from this sprint.", 409)` (lines 34–35) before today's 200 for an existing participant.
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` — `Remove(Participant)` deleted; its last caller went in this step.

**Skills:** authorization-patterns (departs, as the plan's `Departs from` row says: the kick endpoint keeps its inline checks; no resource-gate handler)
**Decisions:**
- The query-token gate uses `StartsWithSegments("/hubs")`, the plan's form. It matches `/hubs/sprint`, `/hubs/sprint/negotiate` and the WebSocket upgrade, and not `/hubsx` or `/api/...`. The only hub is mapped at `/hubs/sprint` (`DependencyInjection.cs:49`). Neither the SPA nor the tests send `access_token` to a non-hub route (`grep -rn access_token` over `web/reflekt-web/src`, `tests` and `src` finds only the handler).
- The Google removed check sits inside the existing `existing is not null` branch, so a never-joined user's path is unchanged.

**Deviations:** None

**Accept:**
- `grep -n 'Query\["access_token"\]' GuestTokenAuthenticationHandler.cs` → line 19, `?? (Request.Path.StartsWithSegments("/hubs") ? Request.Query["access_token"].FirstOrDefault() : null)`. The read sits under the `/hubs` check on the same line.
- `grep -c "Remove(participant)" KickParticipantEndpoint.cs` → 0.
- `grep -rn "participantRepository.Remove\|public void Remove" src/Services/Reflekt/Reflekt.API src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` → no hits.
- Code traces, `GuestTokenAuthenticationHandler.HandleAuthenticateAsync`:
  - A removed participant's token: `GetByGuestTokenAsync` still returns the row (Step 6 left it unfiltered), and line 28 returns `AuthenticateResult.Fail`. Every guarded route and the hub authorise with this scheme (`SprintHub.cs:14` `[Authorize(AuthenticationSchemes = GuestToken)]`, and the `Participant` policy), so the challenge answers 401.
  - The hub's negotiate request, `/hubs/sprint/negotiate?access_token=…`: the path starts with the `/hubs` segment, the query value is read at line 19, and an active participant authenticates as before.
  - `?access_token=` on `/api/...`: the path check fails, so the query link is `null`. With no header and no bearer, `token is null` gives `AuthenticateResult.NoResult()`, so the request is unauthenticated: 401 on a guarded route.
- Code trace, a second kick of the same participant: the participant is found by `GetByIdAsync` (still returned when removed), and it passes the same checks. `participant.Kick()` (line 43) returns at once because `IsRemoved` is true (Step 6), so nothing changes and no event is raised. `SaveChangesAsync` (44) has nothing to write, and the endpoint returns `SendNoContentAsync` (46): 204.

**Verify:**
- `verify-run.js --role build` → pass (run `mvkl0tok-fb038483`).
- Step close `verify-run.js --profile fast --tied --changed {the 4 files}` → pass (run `mvkl0w5x-abb70736`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Runs (`ParticipantEndpointTests.cs`, `AuthSmokeTests.cs`, `FoundationSmokeTests.cs`): `verify-run.js --role integration` → pass, whole project, 44.6 s, `failed_tests: null` (run `mvkl0znb-241dbf12`). Today's kick tests, now on the soft path, stay green.

## Step 9 — Hub: participant group, removed participants rejected, ordered kick revocation
**Files:**
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`:
  - **New members:**
    - a static `ConnectionRegistry` (connection id → `RegisteredConnection(HubCallerContext Caller, int SprintId, int ParticipantId)`, line 27);
    - the message constant `ParticipantRemovedMessage`;
    - `public static ParticipantGroup(int)` → `participant:{id}` (line 33), which Step 11's handlers reuse;
    - the private helpers `RequireActiveParticipantAsync` / `IsParticipantRemovedAsync` (284/290). They read the participant through a created scope and count a missing or removed participant as removed.
  - **`JoinSprint`**, in order:
    1. sprint claim check (today's);
    2. `participantId` claim, now required ("Missing participant claims.", as `ToggleReady` says);
    3. first removed check (51);
    4. registry entry (53) and `ConnectionState` entry;
    5. `participant:{id}` group (57), then `sprint:{id}` group (58);
    6. second removed check from the database (62). On failure it leaves both groups, drops the `ConnectionState` entry and the registry entry (64–67), and throws `HubException("Participant has been removed.")` (68).

    The readiness and timer snapshots that followed are moved unchanged into a private `SendJoinSnapshotsAsync`, so `JoinSprint` stays under the conventions' 60-line limit.
  - **`LeaveSprint`** is now `async`. It also leaves `participant:{id}` (127), beside today's sprint-group and `ConnectionState` removal. The registry entry stays.
  - **`ToggleReady`** calls `RequireActiveParticipantAsync` after its claim checks (141), before touching readiness.
  - **The five timer methods** reach the removed check through `RequireScrumMasterAsync`, which they all call first. It already loaded the participant; a missing or removed one now throws `HubException("Participant has been removed.")` (229) before the Scrum Master resolution. So the old cookie fallback is reached only for a found, active participant with no `UserId`.
  - **New static seams for the kick handler:**
    - `GetParticipantConnections(sprintId, participantId)` reads the registry;
    - `AbortConnections(IReadOnlyList<HubCallerContext>)` aborts each one, collecting failures into one `AggregateException` after trying all;
    - `RemoveParticipantState(sprintId, participantId)` drops the participant's `ConnectionState` entries and readiness entry and returns `(ReadyCount, TotalCount)`.
  - **`OnDisconnectedAsync`** removes the registry entry first; the rest is unchanged.
- `src/Services/Reflekt/Reflekt.API/Features/Participants/KickParticipant/BroadcastParticipantKickedHandler.cs`:
  1. sends `ParticipantKicked` to `sprint:{id}`, with today's payload and token, inside `try`, its exception collected;
  2. then, in `finally`, `RevokeAsync` runs each revocation sub-step through `RunAsync`, which catches and collects, so each step runs even when an earlier one throws:
     - for every registry connection: remove it from `sprint:{id}`, then from `participant:{id}`;
     - `AbortConnections`;
     - `RemoveParticipantState`;
     - `ReadinessChanged` to `sprint:{id}` with `{ participantId, displayName: null, isReady: false, readyCount, totalCount }`, the payload `OnDisconnectedAsync` sends.

     Every revocation send uses `CancellationToken.None`;
  3. after the revocation it rethrows: one failure with its original stack (`ExceptionDispatchInfo`), several as an `AggregateException`. A failure is never swallowed.

**Skills:** create-domain-event-handler (departs, as the plan's `Departs from` row says: the handler keeps its name while it also evicts and aborts)
**Decisions:**
- `ParticipantGroup(int)` is one public static helper on the hub, not the string written out each time. The hub uses it three times, this handler once, and Step 11's five handlers will use it too.
- The kick handler always sends `ReadinessChanged`, while `OnDisconnectedAsync` sends it only when the leaver was ready. The plan asks for the updated readiness after the state drop, and `totalCount` changes either way. When the aborted connections then disconnect, `OnDisconnectedAsync` finds nothing left to remove, so it sends no second `ReadinessChanged`. It still sends today's `ParticipantLeft`.
- The handler collects failures and rethrows them after revocation. A bare `try`/`finally` would lose the send's exception whenever revocation also failed, and a catch that only logs would break the conventions' no-swallow rule.
- `JoinSprint` now requires the `participantId` claim. The guest-token scheme always issues it (`GuestTokenAuthenticationHandler.cs:33`), and the hub accepts no other scheme, so no working client loses access. Without it the participant's removed check cannot run.

**Deviations:** None

**Accept:**
- `grep -n "participant:" SprintHub.cs` → line 33, `ParticipantGroup(int participantId) => $"participant:{participantId}"`. The group join is line 57 (`Groups.AddToGroupAsync(Context.ConnectionId, ParticipantGroup(participantId))`).
- Code trace, `BroadcastParticipantKickedHandler.HandleAsync`: the `ParticipantKicked` send is in `try` (17–20) with `catch` (22). `finally` (26) calls `RevokeAsync`. Its group removals (43, 45), `AbortConnections` (51), `RemoveParticipantState` (58) and the `ReadinessChanged` send (62) each run through `RunAsync` (72), so a throw in one never skips the next.
- Code trace, every listed hub method reaches the removed check before acting:
  - `JoinSprint`: line 51, before any group join or state change;
  - `ToggleReady`: line 141, before the readiness change;
  - `StartTimer`, `PauseTimer`, `ResumeTimer`, `ResetTimer`, `AddTime`: `RequireScrumMasterAsync(sprintId)` is their first await (172, 180, 188, 196, 206), and it throws at line 229 for a missing or removed participant. `AddTime` validates its `minutes` argument first, as today; that acts on nothing.
- Code trace, `JoinSprint`: the second check (62) comes after both group joins (57, 58). Its failure path removes the connection from `sprint:{id}` and `participant:{id}` (64–65), drops `ConnectionState` and the registry entry (66–67), and throws. `LeaveSprint` removes the participant group (127).
- Code trace, the kick handler takes its connections from `SprintHub.GetParticipantConnections` (handler line 39). That reads `ConnectionRegistry` (hub 320), not `ConnectionState`, so a connection that called `LeaveSprint` (its registry entry stays until disconnect) is still evicted and aborted.

**Verify:**
- `verify-run.js --role build` → pass (run `mvkl6exb-c19b88d0`).
- Step close `verify-run.js --profile fast --tied --changed {the 2 files}` → pass (run `mvkl6n32-bb2ceb26`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Runs (`ParticipantEndpointTests.cs`): `verify-run.js --role integration` → pass, whole project, 33.6 s, `failed_tests: null` (run `mvkl6r5r-9ec06d62`).

## Step 10 — One Writing-phase redaction rule on every read path
**Files:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/WritingPhaseRedaction.cs` — new public static class:
  - `GetViewerParticipantId(ClaimsPrincipal)`: the `participantId` claim, or null.
  - `IsRedacted(Card, Sprint, int?)`: true only when all three hold — `card.SprintId == sprint.Id`, `sprint.CurrentPhase == Writing`, and the viewer has a participant id that is not `card.ParticipantId`. A null viewer is never redacted.
  - The redacted forms, today's `ListCards` shape:
    - `Redact(CardResponse)` → text `""`, `AssigneeName` null, `EvidenceLinks` `[]`;
    - `Redact(CardGroupCardEntry)` → text `""`, `EvidenceLinks` `[]`;
    - `Redact(ReportCardEntry)` → text `""`, `EvidenceLinks` `[]`;
    - `Redact(ReportActionItemEntry)` → text `""`, `AssigneeName` null, `EvidenceLinks` `[]`.
  - `GetReportGroupTitle(string? title, Card? firstCard, Sprint, int?)`: a set title is kept. A derived title is the first card's text, or `""` when there is no card or that card is redacted.
- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs` — the inline `redactOthers` rule is replaced by `GetViewerParticipantId` (line 50) and `IsRedacted` / `Redact` (57–58). `using Reflekt.Domain.Sprints;` is dropped; it was used only by that rule. The auth branches and `VoteCount` are unchanged.
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/GetGroups/GetGroupsEndpoint.cs` — each group card entry goes through `IsRedacted` / `Redact` (65–66). The group's `Title` is the stored `CardGroup.Title` (the mapping does not derive it), so it is not redacted.
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs` and `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs` — the same change in both files:
  - the group title comes from `GetReportGroupTitle` (68);
  - group cards are mapped to `ReportCardEntry` and redacted where the rule says, when each group entry is built (70–71);
  - action items go through the rule (90–91);
  - `BuildColumnSection`'s tuple now carries `List<ReportCardEntry>`, the entries already decided, instead of the card entities;
  - `BuildPreviousActionsSectionAsync` is not passed through the rule (95).

**Skills:** None — plan Skill Mapping row 10 has no skill (gap logged in the plan: response redaction / read-side visibility rule)
**Decisions:**
- The redaction class lives in `Features/Cards` (the plan's path) and refers to the `CardGroups` and `Reports` contracts.
- Callers write the decision and the redacted form as one ternary (`IsRedacted(...) ? Redact(x) : x`). This keeps the rule in one place without adding mapping to the rule class.
- In the reports, cards are mapped to `ReportCardEntry` when each group entry is built, not in `BuildColumnSection`, so the static column builder needs no sprint or viewer parameters.

**Deviations:** None

**Accept:**
- `grep -rln "WritingPhaseRedaction" src/Services/Reflekt/Reflekt.API --include=*.cs` → the new file plus `ListCardsEndpoint.cs`, `GetGroupsEndpoint.cs`, `GetSprintReportEndpoint.cs` and `GetParticipantReportEndpoint.cs` (5). The built `bin/` and `obj/` assemblies also match without `--include`; they are build output. Step 11 adds the handlers.
- `grep -rn "redactOthers" src --include=*.cs` → none.
- Code traces, Writing phase, a viewer with participant id P, another participant's current-sprint card C (spec AC5):
  - `ListCards`: `IsRedacted(C, sprint, P)` is true, so the response is `Redact(response)` (line 58): `Text ""`, no `AssigneeName`, `EvidenceLinks []`. P's own cards fail the `ParticipantId` test and stay full.
  - `GetGroups`: C's `CardGroupCardEntry` is `Redact(...)` (66): `Text ""`, `EvidenceLinks []`. The entry has no assignee field, and the group `Title` is the stored one.
  - Both reports:
    - C's `ReportCardEntry` is redacted (71);
    - a C action item's `ReportActionItemEntry` is redacted, assignee included (91);
    - when the group's `Title` is null and its first card is C, `GetReportGroupTitle` returns `""` (68), so the derived title cannot leak C's text.

    The previous-actions section is built by `BuildPreviousActionsSectionAsync` from the closed previous sprint's cards (95), with no call to the rule, so it is returned as today. `GetPreviousActionsEndpoint.cs` is not touched.
  - No participant identity (the owner signed in with Google only): `GetViewerParticipantId` returns null, so `IsRedacted` is false for every card and the body is full, as today.

**Verify:**
- `verify-run.js --role build` → pass (run `mvklbb69-be0b31c1`).
- Step close `verify-run.js --profile fast --tied --changed {the 5 files}` → pass (run `mvklbl7r-f1ebc73a`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Runs (`CardEndpointTests.cs`, `SprintEndpointTests.cs`): `verify-run.js --role integration` → pass, whole project, 59.7 s, `failed_tests: null` (run `mvklbsya-7a4ec18c`). `ListCards_ReturnsAllCardsInSprint` and `ListCards_OrderedByCreatedAtDescending` (own cards in Writing, Must-NOT-Change) stay green.

## Step 11 — Card broadcasts: full to the author, redacted to the others
**Files:**
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/BroadcastCardCreatedHandler.cs`, `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/BroadcastCardUpdatedHandler.cs` and `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/BroadcastCardUpdatedOnEvidenceLinkDetachedHandler.cs` — each takes `IServiceScopeFactory`, never an injected repository. `HandleAsync` builds today's `CardResponse` payload, then calls the private `ReadRecipientsAsync`:
  - it creates a scope and reads the sprint untracked (`SprintRepository.Query().AsNoTracking().SingleOrDefaultAsync`);
  - when the sprint is missing or `WritingPhaseRedaction.IsWritingPhaseCard` is false, it returns at once, so outside Writing only the sprint is read;
  - otherwise it reads the active participants through `ParticipantRepository.GetBySprintIdAsync` and keeps `participant:{id}` for every participant `WritingPhaseRedaction.IsRedacted(card, sprint, id)` is true for (everyone but the author);
  - its scope is disposed when it returns, before any send.

  Then:
  - outside Writing: today's single send to `sprint:{id}`;
  - in Writing: the full payload to `participant:{authorId}`, and `WritingPhaseRedaction.Redact(payload)` to `Clients.Groups(redactedGroups)`.

  Message names (`CardCreated`, `CardUpdated`) and payloads are unchanged.
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/BroadcastAndFetchOnEvidenceLinkAttachedHandler.cs` — the same broadcast, in a private `BroadcastCardUpdatedAsync`. After it, the fetch job is written to the channel exactly as today (line 24).
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/FetchEvidenceLink/BroadcastEvidenceLinkFetchedHandler.cs` — `IsWritingPhaseCardAsync` reads only the sprint, untracked, in a scope it disposes before the send. In Writing, `EvidenceLinkFetched` goes only to `participant:{authorId}`; outside Writing to `sprint:{id}` as today. The payload is unchanged.
- `src/Services/Reflekt/Reflekt.API/Features/Cards/WritingPhaseRedaction.cs` — new `IsWritingPhaseCard(Card, Sprint)` (the card is the given sprint's and that sprint is in Writing). `IsRedacted` now uses it, so the rule's condition is still written once.

**Skills:** create-domain-event-handler (departs, as the plan's `Departs from` row says: handler names unchanged)
**Decisions:**
- The handlers branch on `WritingPhaseRedaction.IsWritingPhaseCard`, not on `sprint.CurrentPhase` themselves. So the read paths and the broadcasts share one "this card is in Writing" rule. The fetched handler needs that branch and has no redacted form.
- The four card handlers each carry their own `ReadRecipientsAsync` and private `CardRecipients` record, as the plan's "each handler loads … through a scope it creates" asks. No shared broadcaster class was added.
- The participant read uses `ParticipantRepository.GetBySprintIdAsync`, the read path the plan names. Q4's answer accepts the repository's read path in place of `AsNoTracking`. The sprint read uses `Query().AsNoTracking()`, because `GetByIdAsync` is a tracked `FindAsync`.

**Deviations:**
- `WritingPhaseRedaction.cs` (a Step 10 file, not in Step 11's list) gains `IsWritingPhaseCard`. Without it the handlers would restate the rule's sprint and phase condition, or the fetched handler could not reference the rule at all.
- After the first build, Q4's answer (option A) reshaped the handlers: reads first, the sprint untracked, the scope disposed before the first send, only the sprint read outside Writing. The plan's Step 11 accept was amended to match.

**Accept:**
- `grep -rln "WritingPhaseRedaction" src/Services/Reflekt/Reflekt.API/Features --include=*.cs` → 10 files: the rule class, the 4 Step 10 endpoints and these 5 handlers. That is 9 endpoint and handler files, as the plan says.
- Code traces, per handler, in Writing:
  - `BroadcastCardCreatedHandler`: `ReadRecipientsAsync` (line 18, scope at 34, untracked sprint at 36, participants at 42) returns before any send. The full payload goes to `participant:{authorId}` (25–26) and the redacted one to the others' groups (28–29). The only `Group($"sprint:…")` call (21) is on the not-Writing branch, which returns.
  - `BroadcastCardUpdatedHandler`: read at 18 (scope 34, untracked 36, participants 42); author 25–26; others redacted 28–29; sprint group only at 21, on the not-Writing branch.
  - `BroadcastAndFetchOnEvidenceLinkAttachedHandler`: read at 31 (scope 47, untracked 49, participants 55); author 38–39; others redacted 41–42; sprint group only at 34, not Writing; fetch job at 24 after the broadcast.
  - `BroadcastCardUpdatedOnEvidenceLinkDetachedHandler`: read at 19 (scope 35, untracked 37, participants 43); author 26–27; others redacted 29–30; sprint group only at 22, not Writing.
  - `BroadcastEvidenceLinkFetchedHandler`: `IsWritingPhaseCardAsync` (19; scope 28, untracked 30) reads only the sprint and disposes its scope. Then one send (23), to `participant:{authorId}` in Writing (20) or `sprint:{id}` outside it (21).
- Code trace, outside Writing: each `ReadRecipientsAsync` returns `CardRecipients(false, [])` right after the sprint read, so the participants are never read. The handler then makes exactly today's call, the same message and payload to `sprint:{id}`.
- Code trace, no connection gets the other form: the full payload is addressed only to the author's participant group. The redacted one goes only to groups of participants for whom `IsRedacted` is true, which never includes the author. Each hub connection is in exactly one participant group (Step 9).
- **Known teardown flake (Q4):** `IOException … being used by another process` at `File.Delete` in an integration class's `DisposeAsync`. A handler's read after the response races the test's file delete, as `BroadcastGroupCreatedHandler`'s read already did. It is recorded here and not fixed in this run, pending the owner's ruling on Q5.
  - Seen in 2 of the 4 runs before the reshape: `mvklha2p-1e3e7b9f`, `EvidenceLinkEndpointTests.AttachLink_GenericUrl_Returns201`; `mvklm8ji-40ce14b0`, `VoteEndpointTests.CastVote_WrongSprint_Returns401`.
  - Seen in 0 of 4 runs with the handlers at `HEAD`.

**Verify:**
- `verify-run.js --role build` → pass (run `mvkm1gdt-a048fb9d`, the reshaped handlers).
- Step close `verify-run.js --profile fast --tied --changed {the 6 files}` → pass (run `mvkm1j08-3c407607`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Runs (`CardEndpointTests.cs`, `EvidenceLinkEndpointTests.cs`): `verify-run.js --role integration`, whole project, on the reshaped handlers → pass twice (`mvkm1rol-28a1c946`, 95.7 s; `mvkm3twk-2fc45ecc`, 58.1 s), `failed_tests: null`.
- Integration runs before the reshape, kept for the record: `mvklha2p-1e3e7b9f` red (the teardown flake, plus F37's host-start flake in `ParticipantEndpointTests.InitializeAsync`), `mvkljqbb-6d8e9a2e` and `mvklks06-8b4ad9de` green, `mvklm8ji-40ce14b0` red (the teardown flake).
- Probe for Q4, the same tree with only the 5 handlers at `HEAD`, then restored: `mvkloh6i-3ceca317` green, `mvklpq8m-1ce522cd` green, `mvklr7rc-15e19198` red (F37's host-start flake, `SprintEndpointTests.InitializeAsync`), `mvklsjh4-0a6eb13f` green.

## Step 12 — Safe return URL; dev front-end origin from configuration
**Files:**
- `src/Services/Reflekt/Reflekt.API/Features/Auth/Login/LoginEndpoint.cs` — the endpoint takes `IWebHostEnvironment` and `IConfiguration` through its primary constructor (line 10). The `RequestServices` lookup is gone.
  - New `public static bool IsSafeReturnUrl(string?)` (23–26): the value is non-empty, passes `IsSafeAsReceived` and passes `IsSafeWhenDecoded`.
  - `IsSafeAsReceived` (41–45) is the spec's four-part rule on the value as received: it starts with `/`; it is `/` alone or its second character is neither `/` nor `\`; no character is below U+0020 or is U+007F; it has no `://`.
  - `IsSafeWhenDecoded` (47–51) is today's `IsValidReturnUrl`, renamed and unchanged: the `Uri.UnescapeDataString` form starts with `/`, does not start with `//`, and has no `://`.
  - `ResolveRedirectUri` (28–39) is now an instance method. It reads `Auth:DevFrontendOrigin` (31) and keeps today's prefixing with that value in place of the literal (35, 38). There is no branch for a missing key. Outside Development the redirect is the return URL, or `/`.
- `src/Services/Reflekt/Reflekt.API/appsettings.json` — `Auth:DevFrontendOrigin` = `http://localhost:5173`, beside `Auth:Google`. The value is a non-secret origin.

**Skills:** None — plan Skill Mapping row 12 has no skill (gap logged in the plan: safe redirect / return-URL validation)
**Decisions:**
- The public function holds both checks, the received-form rule and today's decoded-form rule. So "the rule is one public static function" covers the whole acceptance, and the probe calls the same function the endpoint uses.
- The function lives on `LoginEndpoint`, in the login slice, rather than in a new file.

**Deviations:** None

**Accept:**
- `grep -rn "localhost:5173" src/Services/Reflekt/Reflekt.API --include=*.cs` → none (exit 1). The origin now appears only in `appsettings.json:10` and its copy under `bin/`.
- Scratch probe `out/f35-scratch/returnurl` (console project referencing `Reflekt.API`, calling `LoginEndpoint.IsSafeReturnUrl`), output pasted:
  ```
  accepted  "/sprint/12"
  rejected  "//evil.com"
  rejected  "/<BACKSLASH>evil.com"
  rejected  "/<TAB>/evil.com"
  rejected  "https://evil.com"
  accepted  "/"
  accepted  "/docs/%09notes"
  rejected  "/%2F/evil.com"
  rejected  "/a<DEL>b"
  rejected  ""
  ```
  The five AC6 cases are the first five lines. `/docs/%09notes` is still accepted, as today; `/%2F/evil.com` is still rejected by the decoded check, as today.
- `Login_RedirectsToGoogle` (`AuthSmokeTests.cs:55`) is green in the integration run below.

**Verify:**
- `verify-run.js --role build` → pass (run `mvkm9xut-10c85053`).
- Step close `verify-run.js --profile fast --tied --changed {the 2 files}` → pass (run `mvkmabwi-221ba67d`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Runs (`AuthSmokeTests.cs`): `verify-run.js --role integration`, whole project:
  - `mvkmaw54-f6725b23` → red, 103/104. The only failure is the known teardown flake (Q4): `VoteEndpointTests.RetractVote_ReturnsBudget`, `IOException` at `File.Delete` in `DisposeAsync` (`VoteEndpointTests.cs:92`).
  - Re-run as Q4 says: `mvkmclfe-a295fec6` → pass, 47.9 s, `failed_tests: null`.

## Step 13 — Connect-time SSRF check on the OpenGraph client
**Files:**
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphAddressPolicy.cs` — new public static class with one public function, `IsAllowed(IPAddress)`.
  - An IPv4-mapped IPv6 address is turned into its IPv4 address first (line 13).
  - IPv4 (`IsRejectedIpv4`) rejects `0/8`, `10/8`, `100.64/10`, `127/8`, `169.254/16`, `172.16/12`, `192.168/16`, `224/4` and `255.255.255.255`.
  - IPv6 (`IsRejectedIpv6`) rejects `::`, `::1`, `fe80::/10`, `fc00::/7` and `ff00::/8`.
  - Any other address family is rejected (19).
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphConfiguration.cs` — the `opengraph` client gets `ConfigurePrimaryHttpMessageHandler` with a `SocketsHttpHandler` (23) whose `ConnectCallback` (25) is the private `ConnectToAllowedAddressAsync`. That callback:
  - resolves the host (38);
  - throws `HttpRequestException` when no address comes back or any address is rejected (39–40);
  - otherwise connects a socket to exactly the addresses it checked (45) and returns a `NetworkStream` that owns the socket. The socket is disposed if the connect fails.

  `AllowAutoRedirect` keeps its default (on), so every redirect hop to a new host and port opens through the same callback. The timeout and the buffer limit are unchanged.
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphFetcher.cs` — `IsPublicHostAsync`, `IsPrivateIpv4` and their pre-check call are removed, along with the `System.Net` and `System.Net.Sockets` usings they alone used. `FetchAsync` keeps its scheme check (24). A refused connect surfaces as `HttpRequestException`, which the existing `catch` (35) turns into `FetchResult.Failed`.

**Skills:** None — plan Skill Mapping row 13 has no skill (gap logged in the plan: SSRF-guarded outbound HttpClient)
**Decisions:**
- The connect callback is a private method of `OpenGraphConfiguration`, beside the client registration, so the policy class holds only the address check.
- The check fails closed: an empty DNS answer, an address family other than IPv4 or IPv6, and a host where any one address is rejected all refuse the connection.
- The ranges are exactly P5's closed list. So `240.0.0.1` (reserved `240/4`, outside `224/4`) and `fec0::1` (deprecated site-local) are allowed; the probe shows both.

**Deviations:** None

**Accept:**
- `grep -c "IsPrivateIpv4\|IsPublicHostAsync" OpenGraphFetcher.cs` → 0.
- `grep -n "ConnectCallback" OpenGraphConfiguration.cs` → 1 hit, line 25.
- Scratch probe `out/f35-scratch/ssrf` (console project referencing `Reflekt.API`), output pasted. The first part calls `OpenGraphAddressPolicy.IsAllowed`:
  ```
  rejected  0.1.2.3
  rejected  10.1.2.3
  rejected  100.64.0.1
  rejected  100.127.255.254
  rejected  127.0.0.1
  rejected  169.254.1.1
  rejected  172.16.0.1
  rejected  172.31.255.254
  rejected  192.168.1.1
  rejected  224.0.0.1
  rejected  239.255.255.255
  rejected  255.255.255.255
  rejected  ::
  rejected  ::1
  rejected  fe80::1
  rejected  febf::1
  rejected  fc00::1
  rejected  fd12:3456::1
  rejected  ff02::1
  rejected  ::ffff:127.0.0.1
  rejected  ::ffff:10.0.0.1
  allowed   ::ffff:93.184.215.14
  allowed   93.184.215.14
  allowed   100.63.255.255
  allowed   100.128.0.1
  allowed   172.15.0.1
  allowed   172.32.0.1
  allowed   240.0.0.1
  allowed   2606:2800:21f:cb07:6820:80da:af6b:8b2c
  allowed   fec0::1
  ```
  The second part builds a `ServiceCollection` with `AddLogging()` and the app's own `AddReflektOpenGraph()`, then resolves `OpenGraphFetcher`. It starts a `TcpListener` on `127.0.0.1` and fetches it three ways. A raw connect at the end is the control:
  ```
  http://127.0.0.1:59565/ -> FetchResult.Failed; listener pending connection: False
  http://localhost:59565/ -> FetchResult.Failed; listener pending connection: False
  http://[::ffff:127.0.0.1]:59565/ -> FetchResult.Failed; listener pending connection: False
  control: raw TcpClient connect; listener pending connection: True
  ```
  The listener accepted no connection from the fetcher, and the control shows it would have recorded one (spec AC7).

**Verify:**
- `verify-run.js --role build` → pass (run `mvkmhtyp-77960e14`).
- Step close `verify-run.js --profile fast --tied --changed {the 3 files}` → pass (run `mvkmi5my-e649549d`, the whole `roles.unit` through the tied fallback). `label-tests.js` → timed no test.
- Runs (`EvidenceLinkEndpointTests.cs`): `verify-run.js --role integration`, whole project → pass (`mvkmjbi2-8b807974`, 47.4 s, `failed_tests: null`).

## Step 14 — Close-out checks
**Files:** None. No source file changed. `out/f35-scratch/` was deleted; it was git-ignored and held the probes, the `dotnet-ef` tool and its NuGet caches, about 205 MB. The empty `out/` folder went with it.
**Skills:** None — plan Skill Mapping row 14 has no skill
**Decisions:** None
**Deviations:**
- `graphify update .` did not run: `graphify` is not on the PATH (`which graphify` and `where.exe graphify` find nothing). The plan names this case: F40's graph refresh covers it, and `graphify-out/` is unchanged.

**Accept:**
- Plan greps on the final tree, with `--include=*.cs` where the folder holds build output. Every one meets the plan's expected value:
  - S1: `AddSingleton<DomainEventPublishingInterceptor>` in `src` → none (exit 1). The 5 callback names in `DomainEventPublishingInterceptor.cs` → 6. `Microsoft.EntityFrameworkCore.Relational` → `Blocks.Persistence.csproj:9` and `Directory.Packages.props:14`, one hit each.
  - S3: `InvalidOperationException` in `Reflekt.Domain` → exactly 4: `Card.cs:88`, `Card.cs:97` (EvidenceLink not found), `Sprint.cs:64` (timer default), `Sprint.cs:123` (unknown phase). `Throw<DomainException>` in `SprintTests.cs` → 6. `InvalidOperationException` in `ClaimsPrincipalExtensions.cs` → none.
  - S4: `EnsureOpen()` → exactly the 6 endpoints (`AddCardToGroup`, `CreateGroup`, `DissolveGroup`, `RemoveCardFromGroup`, `UpdateGroupTitle`, `UpdateColumns`) and `SprintHub.cs`.
  - S5: `Enum.TryParse` → `CreateCardEndpoint.cs:0`, `SetReviewStatusEndpoint.cs:0`. `Validator<CreateGroupRequest>` and `Validator<UpdateGroupTitleRequest>` → 1 each, 2 in all.
  - S8: `Query["access_token"]` → `GuestTokenAuthenticationHandler.cs:19`, inside `Request.Path.StartsWithSegments("/hubs") ? … : null`. `Remove(participant)` in `KickParticipantEndpoint.cs` → 0. `participantRepository.Remove` and `public void Remove` → none.
  - S10/S11: `WritingPhaseRedaction` under `Reflekt.API` → 10 files: the rule class, `ListCards`, `GetGroups`, the two reports and the 5 Step 11 handlers. `redactOthers` in `src` → none.
  - S12: `localhost:5173` in `Reflekt.API` `.cs` files → none.
  - S13: `IsPrivateIpv4` and `IsPublicHostAsync` in `OpenGraphFetcher.cs` → 0. `ConnectCallback` in `OpenGraphConfiguration.cs` → 1, line 25.
- `verify-run.js --role integration --slug F35-BackendCorrectnessFixes` on the final tree → pass (`mvkmmnlm-66f05e6a`, 50.7 s, `failed_tests: null`).
- **Owed, not run here: the complete suite (spec AC10, `roles.full`).** The team lead runs it from the main session at handover and at close (Q3). If its only failures are F37's host-start flake or the Q4 teardown `IOException`, it is re-run (Q4).
- Step 1's `ManyServiceProvidersCreatedWarning` accept: the final run's record has no such text (`grep -c` → 0). The record keeps only an excerpt, and none on a pass (Developer Lessons), so this is weak evidence. The complete run's output is where to read it.
- `git diff 1eec0b8… -- Directory.Packages.props` → one hunk, `+    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.13" />` in the `EF Core` group; `--numstat` → `1 0`, so no line changed or removed.
- File list: `git diff --name-only 1eec0b8… -- . ':!graphify-out'` together with the untracked files from `git status --porcelain --untracked-files=all`. Every entry is allowed:
  - the 47 `src/` and `tests/` paths are all named by Steps 1–13, the new files included (`WritingPhaseRedaction.cs`, `OpenGraphAddressPolicy.cs`, the two `SoftKickParticipants` migration files). The only test file is `tests/Reflekt.UnitTests/Sprints/SprintTests.cs` (Step 3). Step 3 updated no integration test file. `WritingPhaseRedaction.cs` is a Step 10 file that Step 11 also changed (Step 11 `Deviations:`).
  - `Directory.Packages.props` is Step 1's.
  - `.claude/verify.json`: one added line, the `integration` role (Q2).
  - `docs/specs/F35-BackendCorrectnessFixes/`: `definition/spec.md` changes only `**Plan:** None` → `**Plan:** docs/specs/F35-BackendCorrectnessFixes/delivery/plan.md`. The rest are `delivery/` files.
  - `.claude/.personas.json` and five `.claude/tmp/` files from the plan review. This build's `.claude/tmp/*.log` run logs are git-ignored (`.gitignore:107`, `*.log`).
  - Nothing else: no `docs/backlog.md`, no `web/reflekt-web/package*.json`, no other doc.
- `test ! -e out/f35-scratch` → succeeds.

**Verify:** No source changed in this step, so there is no step close. The integration run above is the step's run.

## Fix Round 1
Fix list: `review.md` § Step 2 — Fix list, cycle 1, rows 1–5 (no follow-up line; every row worked). Open questions under the list were read, not fixed.

**Files:**
- Row 1 (MEDIUM, duplicated broadcast block):
  - `src/Services/Reflekt/Reflekt.API/Features/Cards/CardBroadcast.cs` — new static class beside `WritingPhaseRedaction`. `BroadcastAsync(hubContext, scopeFactory, card, message, ct)` holds the block the four handlers carried: today's `CardResponse` payload, then the private `ReadRecipientsAsync` (scope created per call from the caller's factory, sprint read untracked, participants through `GetBySprintIdAsync` only in Writing, scope disposed on return), then the three-way send (sprint group outside Writing; full to `participant:{authorId}`; redacted to `Groups(redacted)` when non-empty). `IsWritingPhaseCardAsync(scopeFactory, card, ct)` is the fetched handler's sprint-only read. Both reads share the private `FindSprintAsync`; `CardRecipients` is now private here.
  - `.../Cards/CreateCard/BroadcastCardCreatedHandler.cs`, `.../Cards/EditCard/BroadcastCardUpdatedHandler.cs`, `.../EvidenceLinks/DetachEvidenceLink/BroadcastCardUpdatedOnEvidenceLinkDetachedHandler.cs` — `HandleAsync` is one call to `CardBroadcast.BroadcastAsync` with its own `IHubContext`, its own `IServiceScopeFactory`, and its message name (`CardCreated` / `CardUpdated` / `CardUpdated`).
  - `.../EvidenceLinks/AttachEvidenceLink/BroadcastAndFetchOnEvidenceLinkAttachedHandler.cs` — the private `BroadcastCardUpdatedAsync`/`ReadRecipientsAsync` are gone; it calls `CardBroadcast.BroadcastAsync(..., "CardUpdated", ct)`, then writes the fetch job exactly as before, in the same order.
  - `.../EvidenceLinks/FetchEvidenceLink/BroadcastEvidenceLinkFetchedHandler.cs` — its private `IsWritingPhaseCardAsync` is replaced by `CardBroadcast.IsWritingPhaseCardAsync`; recipients and payload unchanged.
- Row 2 (LOW, record): no file but this one. Correction to Step 8's `Verify:` line, recorded here rather than by rewriting the closed block: **no test in `tests/` exercises the kick path** (`grep -rni kick tests --include=*.cs` → no hit). Step 8's integration run proves only that nothing else regressed. The kick endpoint (204 soft kick, second kick 204), the removed token's 401, the Google rejoin 409, the hub's removed-participant rejection and the kick handler's eviction/abort have no automated coverage; this is accepted under P1 (no new tests) and listed in `## Carry-Over Findings` below for the summary's accepted gaps.
- Row 3 (LOW): `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardCommand.cs` — `.Cascade(CascadeMode.Stop)` on the `ColumnType` and `CardType` rules, as `CreateGroupRequestValidator`'s rule has it. An empty value now yields only the `NotEmpty` message; the status stays 400.
- Row 4 (LOW): `src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs` — comment only, above `TransactionStartedAsync`. It now says the held list is dropped when the next transaction starts — an explicit one, before any of its saves, or EF's own for a save that opens one — that a save opening no transaction leaves it in place, and that nothing but a commit publishes it. No code changed.
- Row 5 (owner, Q5): the `DisposeAsync` delete line(s) of each of the 8 integration test classes — `AuthSmokeTests.cs`, `CardEndpointTests.cs`, `EvidenceLinkEndpointTests.cs`, `FoundationSmokeTests.cs`, `ParticipantEndpointTests.cs`, `SprintEndpointTests.cs`, `TeamEndpointTests.cs`, `VoteEndpointTests.cs` under `tests/Reflekt.IntegrationTests/`. The same `File.Exists`/`File.Delete` calls now sit in a retry loop: on `IOException`, up to 20 attempts, 100 ms apart (about 2 s), clearing the SQLite pools again before each retry; the last attempt's exception propagates. No check, test case, test file or helper was added; nothing else in the classes changed. `SprintEndpointTests.cs:289` (a test body deleting its own empty-database file) is not a class cleanup and was left as it was.

**Skills:** create-domain-event-handler (row 1, the Step 11 mapping; the skill has no rule on shared helpers, and the handler names, folders and namespaces stay as it says). Rows 2–5: None — record, validator cascade, comment and test-cleanup edits.
**Decisions:**
- Row 1: one entry point `BroadcastAsync` that reads and then sends, instead of the reviewer's two public methods (`ReadRecipientsAsync` + `SendCardAsync`). The Q4 order holds inside it: the scope is created and disposed within the read, before the first send. The payload is built inside it, since all four handlers built it identically (`card.Adapt<CardResponse>()`).
- Row 1: the fetched handler's sprint-only read moved into the same class (the fix list notes it repeats the scope-and-sprint read), so the untracked sprint query is written once.
- Row 5: the retry clears the pools before each new attempt. A handler that runs after the response may open a pooled connection after the first `ClearAllPools()`, and the pool would keep the file open; waiting alone would not release it (reasoned, not measured).
**Deviations:** None — every row was acted on. Row 2 is recorded in this block and the tail rather than by rewording the closed Step 8 block (`implementation-format`: closed step blocks are never rewritten).
**Verify:**
- Tied close `verify-run.js --profile fast --tied --changed {the 16 files above} --slug F35-BackendCorrectnessFixes` → pass (run `mvknhx33-70977364`, 12.3 s, the whole `roles.unit` through the tied fallback). `label-tests.js --run mvknhx33-70977364` → timed no test.
- `verify-run.js --role integration --slug F35-BackendCorrectnessFixes` → pass twice: `mvknie5o-f0a3bca3` (57.8 s) and `mvknk0cn-a4fa2a05` (55.0 s), `failed_tests: null`. The second run is a flake check for row 5, under the step limit.
- Accept greps: `ReadRecipientsAsync` and `CardRecipients` under `src` → only `CardBroadcast.cs`. No scratch was created.
- `graphify update .` not run: not installed (Step 14's fallback).

## Key Decisions
- Step 1: `SaveChangesCanceledAsync` also clears the collected list, beside `SaveChangesFailedAsync`. Held events are appended, so several saves inside one explicit transaction all publish at commit.
- Step 2: `AdvancePhase`'s Writing path is extracted to `LeaveWritingAsync`. Every transaction opens after the endpoint's last early return.
- Step 3: the middleware bodies are private nested records, so `details` can carry `JsonIgnore`. The `HttpException` subclass arms are folded into one `HttpException` arm.
- Step 4: the hub's "one private helper" is the existing `LoadTimedSprintAsync`, so the sprint is not loaded twice.
- Step 5: plain FluentValidation, as in the rest of the service. **A request that is both invalid and wrongly authorised now gets 400 where it used to get 401**, because validation runs first.
- Step 6: the unique-violation match uses the error code and the index's columns. `ExistsByDisplayNameAsync` keeps its `ToLower()`. The `DbUpdateException` is kept as the inner exception.
- Step 7: the duplicate-name repair runs in a temp table, comparing names with explicit `COLLATE NOCASE` and `lower()`. `dotnet-ef` was installed and run entirely inside `out/f35-scratch/`.
- Step 8: the query-token gate is `StartsWithSegments("/hubs")`. The Google removed check sits inside the existing-participant branch.
- Step 9:
  - `SprintHub.ParticipantGroup(int)` is the one group-name helper.
  - The kick handler always sends `ReadinessChanged`.
  - It collects the revocation failures and rethrows them.
  - `JoinSprint` now requires the `participantId` claim.
- Step 10: the redaction class lives in `Features/Cards`, and callers write `IsRedacted(...) ? Redact(x) : x`. The reports map to `ReportCardEntry` when each group entry is built.
- Step 11: the handlers branch on `WritingPhaseRedaction.IsWritingPhaseCard`. Each handler has its own `ReadRecipientsAsync`. The sprint is read untracked; participants come through `GetBySprintIdAsync`.
- Step 12: `LoginEndpoint.IsSafeReturnUrl` holds both the received-form rule and today's decoded-form rule.
- Step 13: the connect callback is private to `OpenGraphConfiguration`, and the check fails closed. The ranges are exactly P5's list, so `240/4` and `fec0::/10` are allowed.
- Fix round 1: the four card handlers' read-and-send block, and the fetched handler's sprint read, live once in `Features/Cards/CardBroadcast.cs`; each handler passes its own scope factory. Each integration class's cleanup retries the file delete for about 2 s, clearing the SQLite pools before each retry.

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | persistence-patterns | departs as planned: `Mode.WaitForNone` kept |
| 2 | persistence-patterns | |
| 3 | error-handling | departs as planned: 409 and a camelCase body, by the spec |
| 4 | domain-patterns | |
| 5 | create-feature | `workflows/Validator.md`, FastEndpoints variant |
| 6 | domain-patterns | |
| 7 | persistence-patterns | |
| 8 | authorization-patterns | departs as planned: inline kick checks kept |
| 9 | create-domain-event-handler | departs as planned: handler name kept |
| 10 | None | plan row 10: no skill (gap logged) |
| 11 | create-domain-event-handler | departs as planned: handler names kept |
| 12 | None | plan row 12: no skill (gap logged) |
| 13 | None | plan row 13: no skill (gap logged) |
| 14 | None | plan row 14: close-out checks, no source |
| Fix round 1 | create-domain-event-handler | row 1 (Step 11's mapping); rows 2–5 need no skill |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| Integration teardown `IOException` at `File.Delete` | medium | reviewer | Step 11: 2 of 4 runs before the reshape. Step 12 run `mvkmaw54-f6725b23` (`VoteEndpointTests.RetractVote_ReturnsBudget`), green on re-run | Known flake (Q4). Not fixed; the fix waits on the owner's Q5. The integration test teardown is untouched. |
| F37's flaky host start in `InitializeAsync` | low | reviewer | Step 7 run `mvkksjhg-013fceaf`; Step 11 runs `mvklha2p-1e3e7b9f`, `mvklr7rc-15e19198` | Older than F35 (F37 `change-notes.md` Note 1). The runner cuts out its message. |
| Validation now runs before the authorisation check in `CreateCard` | low | reviewer | Step 5 Decisions | A request with an invalid enum and a wrong-sprint token gets 400, not 401. No test covers the combination. |
| `JoinSprint` requires the `participantId` claim | low | reviewer | Step 9 Decisions | Every guest token issues it, and the hub accepts only that scheme. |
| Reserved `240/4` and site-local `fec0::/10` are allowed for link fetches | low | architect | Step 13 probe | Follows P5's closed list. Widening it is a spec change. |
| `graphify update` not run | low | reviewer | Step 14 Deviations | `graphify` is not installed; F40's refresh covers it. |
| Kick path has no automated coverage | low | reviewer | `grep -rni kick tests --include=*.cs` → no hit | Corrects Step 8's "today's kick tests" (Fix Round 1, row 2). Soft kick 204, second kick 204, removed token 401, Google rejoin 409, hub rejection and the handler's eviction are proven by code traces only. Accepted under P1; for the summary's accepted gaps. |
| Teardown `IOException` now retried in cleanup | low | reviewer | Fix Round 1, row 5; integration runs `mvknie5o-f0a3bca3`, `mvknk0cn-a4fa2a05` green | Owner's Q5 answer. Two green runs are weak evidence against a flake seen in about half of earlier runs; the complete run at close is the next reading. |

## Deviations from Plan
- Step 7: the repair `Sql` runs before `AlterColumn`. EF's SQLite generator defers the table rebuild, and the plan's order raised warning 30200. The repair still runs before the unique index exists.
- Step 11:
  - `WritingPhaseRedaction.cs`, a Step 10 file, gained `IsWritingPhaseCard`, so the handlers do not restate the rule.
  - The handlers were reshaped after Q4's answer: reads first, scope disposed before any send. The plan's accept was amended to match.
- Step 14: `graphify update .` was not run, because the command is not installed. This is the plan's own fallback.
- Steps 1–13 have no other deviations. The complete suite (spec AC10) is owed to the main session (Q3).

*Status: COMPLETE — developer, 2026-10-23*
