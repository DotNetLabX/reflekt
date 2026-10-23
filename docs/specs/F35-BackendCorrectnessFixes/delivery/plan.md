# F35 — Backend Correctness & Security Fixes

**Feature Spec:** `docs/specs/F35-BackendCorrectnessFixes/definition/spec.md`
**Model:** claude-opus-5-5[1m]
**Build slices:** 1–5 · 6–11 · 12–14; why: events, transactions, errors and input checks · participants, kick, hub and card hiding (Steps 9 and 11 share the hub's participant group) · sign-in, link fetch and close-out
**Size:** 14 developer-built steps · 48 files · fullChecks: none — no fullChecks list declared
**Lane:** full — normal gear (as today, by size: 14 developer-built steps, 48 files)
**Build lane:** thorough — full team — normal gear
**Gear:** normal — campaign

## Context

Base commit: `1eec0b8bfb0ad76895a147416b51d6dbfa5c6bb0` (F37 is already on this branch). One service is affected, `src/Services/Reflekt`, plus `BuildingBlocks/Blocks.Persistence`. The spec fixes defects the F34 audit found: lost or early domain events, Writing-phase card text reaching other participants, rule violations surfacing as 500, a closed sprint still being editable, a kick that crashes on a foreign key, and five security holes.

These standing rules bind every step, the owner's as owner answers:
- No new test cases, test classes or test files. An existing test is updated only where a fix changes what it asserts (`questions.md` Q1, plan P1).
- No business-rule registries.
- Login stays open to any Google account.
- No package version change. Step 1 adds one direct package reference, at the version every project already resolves; no existing version moves (plan P7).
- Never push, never rewrite history.
- Never edit `docs/backlog.md`, `web/reflekt-web/package.json` or `web/reflekt-web/package-lock.json`.

Related:
- **F38-BackendConventions** (Ready) builds on this feature: it reuses the transaction-aware interceptor, the closed-sprint message and the redaction. It also replaces the hub's static state with a `ReadinessTracker` and removes `InternalsVisibleTo`.
- **F40-ArchitectureDocs** (Ready) records the team-count race as a known deviation and rewrites the docs, `docs/architecture/v1.md` § 5.3 / § 7.5 included.
- **F37** is done.

## Scope

**In scope:** the spec's Scope table rows, without exception — BE-01, BE-02, BE-07, BE-12, BE-13, BE-14, BE-15, BE-19, BE-20, BE-21, BE-23, BE-28 (query-token part), SK-01, SK-04, SK-05, SK-07, SK-09 (`GetUserId`), SK-11 (part), SK-14, SK-17 and SK-23 (display-name part).

**Out of scope:** everything in the spec's Out of scope, every new test, every frontend file, and every doc outside this feature folder (F40). The `error-handling` and `domain-patterns` skills that now differ from the code are logged in `lessons.md` § Skill Gaps; they are not edited here.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | persistence-patterns | Follow | no | `DomainEventPublishingInterceptor` scoped and transaction-aware; `RepositoryBase.BeginTransactionAsync`; EF Core Relational reference in `Blocks.Persistence` | gap: transaction-aware variant, logged |
| 2 | persistence-patterns | Follow | no | four endpoints in one explicit transaction; inline sends after commit | — |
| 3 | error-handling | Follow | no | `DomainException` → 409, camelCase body, generic 5xx, `HttpException.StatusCode`; spec overrides the skill's 400 and PascalCase | ill-fitting skill, logged |
| 4 | domain-patterns | Follow | no | `Sprint.EnsureOpen()`; guard placement in 6 endpoints and the hub; `SetReviewStatus` 404 | — |
| 5 | create-feature | Follow | no | validators for `CreateCardCommand`, `CreateGroupRequest`, `UpdateGroupTitleRequest`; one parse per input | — |
| 6 | domain-patterns | Follow | no | `Participant.RemovedAt`, idempotent `Kick()`; repository filters; unique index config | — |
| 7 | persistence-patterns | Follow | no | migration with name repair, model snapshot, seed double vote | — |
| 8 | authorization-patterns | Follow | no | removed token → 401; query token only on `/hubs`; soft-kick endpoint; Google rejoin 409 | — |
| 9 | create-domain-event-handler | Follow | no | ordered kick handler; hub participant group, removed check, connection registry | — |
| 10 | (none) | — | no | one Writing-phase redaction rule on 4 read paths | gap: response redaction / read-side visibility rule |
| 11 | create-domain-event-handler | Follow | no | 5 card broadcast handlers, author vs others | — |
| 12 | (none) | — | no | safe return-URL rule; dev origin from configuration | gap: safe redirect / return-URL validation |
| 13 | (none) | — | no | connect-time address check on the OpenGraph client | gap: SSRF-guarded outbound HttpClient |
| 14 | (none) | — | no | close-out checks | — |

TDD is `no` on every step because the owner's rule forbids new tests (Decisions row 1). Step 3 updates existing assertions only.

## Domain Model Changes

- `Sprint`: new behaviour method `EnsureOpen()` throws `DomainException("Sprint is closed.")` when `State == Closed`. Draft and Active pass. The state-rule `InvalidOperationException`s at `Start`, `AdvancePhase` (not Active; already Reporting), `Close`, `SetFocus` ×2 and `ClearFocus` ×2 become `DomainException`, keeping their messages. "No timer default for phase" and "Unknown phase" stay `InvalidOperationException`.
- `Card.SetReviewStatus` on a non-action item throws `DomainException`. Both "EvidenceLink not found" arms stay `InvalidOperationException`.
- `Participant`: `DateTime? RemovedAt` (private set) and an `IsRemoved` read. `Kick()` sets `RemovedAt` and raises the existing `ParticipantKickedEvent` once. On an already removed participant it does nothing and raises no event.

## Data Model Changes

- `Participants.RemovedAt` (nullable TEXT, default NULL).
- `Participants.DisplayName` gets collation `NOCASE`; SQLite rebuilds the table, the same move `20260509220316_AddUserEmailNocaseCollation` made.
- New unique index on `(SprintId, DisplayName)` with filter `"RemovedAt" IS NULL`.
- No vote index (spec Decisions row 3).

## Implementation Steps

Every step closes with `dotnet build Reflekt.slnx` (0 `error CS`) and the `unit` role through `verify-run.js`. The `unit` role runs only `Reflekt.UnitTests`. So each step also runs, by name, every existing integration test file its **Runs:** line names: `verify-run.js --profile fast --files tests/Reflekt.IntegrationTests/{File}.cs`. Steps 7 and 14 also run the `full` role.

**Scratch rule:** a scratch probe is a throwaway console project under `out/f35-scratch/` (git-ignored, `.gitignore:33`). Its input and output are pasted into the step block, and it is deleted in Step 14. It is never a test.

**Code-trace accepts:** an accept marked *code trace* names the path the done check and the reviewer read. The developer writes the trace (file, method, the line that does it) into the step block.

### Step 1 — Scoped, transaction-aware event publication
Follow persistence-patterns. Files:
- `src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs`
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs`
- `src/BuildingBlocks/Blocks.Persistence/Repositories/RepositoryBase.cs`
- `src/BuildingBlocks/Blocks.Persistence/Blocks.Persistence.csproj`
- `Directory.Packages.props`

What to build:
- `IDbTransactionInterceptor` lives in `Microsoft.EntityFrameworkCore.Relational`, and `Blocks.Persistence` references base EF Core only. Add `<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />` to its csproj, and `<PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.13" />` to the `EF Core` group of `Directory.Packages.props`. This is the version `Microsoft.EntityFrameworkCore.Sqlite` 10.0.13 already brings in; no existing line changes (P7).
- The interceptor is registered `AddScoped`. It is resolved through the `sp` of `AddDbContext((sp, options) => …)`, which is the request's scope because options are built per scope.
- It also implements `IDbTransactionInterceptor`.
- It keeps two lists:
  - **collected** — the events of the save in progress, gathered in `SavingChangesAsync`;
  - **held** — events waiting for an explicit transaction's commit.
- In `SavedChangesAsync`:
  - with `Context.Database.CurrentTransaction` open, an explicit transaction is running, so collected moves to held;
  - with none open, collected is published at once, as today. This covers EF's own transaction for a multi-statement save: that transaction has already committed and closed by `SavedChangesAsync`.
- `SaveChangesFailedAsync` clears collected only.
- The transaction callbacks touch held only:
  - `TransactionCommittedAsync` publishes held;
  - `TransactionRolledBackAsync` and `TransactionFailedAsync` clear it;
  - `TransactionStartedAsync` clears it.
- EF's own save transaction also raises these callbacks. It opens only when no explicit transaction is open, and its start clears held first, so it never publishes collected events and never publishes stale ones.
- Held events left by an explicit transaction disposed without commit are therefore never published: the next transaction start drops them, whether it is an explicit transaction or EF's own save transaction.
- Publication keeps the reflective runtime-type dispatch and `Mode.WaitForNone`.
- `RepositoryBase` gains `BeginTransactionAsync(CancellationToken)` returning the context's transaction. This is the one way endpoints open one; `VoteRepository.BeginSerializableAsync` stays.

Accept:
- `grep -rn "AddSingleton<DomainEventPublishingInterceptor>" src` gives no hits; it gives 1 today, at `Reflekt.Persistence/DependencyInjection.cs:18`.
- `grep -c "TransactionCommittedAsync\|TransactionRolledBackAsync\|TransactionFailedAsync\|SaveChangesFailedAsync\|TransactionStartedAsync" src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs` gives 5 or more.
- `grep -n "Microsoft.EntityFrameworkCore.Relational" src/BuildingBlocks/Blocks.Persistence/Blocks.Persistence.csproj Directory.Packages.props` gives 1 hit in each.
- *Code trace*, one line per spec AC1 case:
  - (a) a failed save publishes nothing: `SaveChangesFailedAsync` clears collected;
  - (b) a rolled-back transaction publishes nothing;
  - (c) two concurrent scopes never share a list: one interceptor instance per scope;
  - (d) a transaction disposed without commit never publishes later;
  - (e) a save with no explicit transaction, which EF wraps in its own transaction, publishes its events once, after the save.
- The full suite in Step 7 runs with no EF `ManyServiceProvidersCreatedWarning` in its output.

Runs: `SprintEndpointTests.cs`, `VoteEndpointTests.cs`, `CardEndpointTests.cs`.

Satisfies: spec BE-01/SK-01, BE-12 (mechanism), AC1. Risk: risky — every write path publishes through it; dependency kind (`Directory.Packages.props`).

### Step 2 — Multi-save endpoints in one transaction; inline sends after commit
Follow persistence-patterns. Files:
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/DissolveGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/RemoveCardFromGroupEndpoint.cs`

What to build:
- Each endpoint's saves run inside one transaction from Step 1's `BeginTransactionAsync`, committed once before the response.
- In `AdvancePhase` the transaction wraps only the transition out of Writing, as the spec names it. The other transitions keep their single save, as today.
- `DissolveGroup` adds all solo groups at once and saves once to get their ids; no save inside the per-card loop.
- `CreateGroup`'s `GroupCreated` and `DissolveGroup`'s `GroupDissolved` are sent only after `CommitAsync` returns, so an exception before commit sends nothing.
- Messages, payloads, status codes and response bodies stay as today.

Accept:
- *Code trace:* in `DissolveGroupEndpoint` no `SaveChangesAsync` call sits inside the per-card `foreach`; the solo groups are added in one call before a single save.
- *Code trace:* in each of the four files, every `SaveChangesAsync` on the changed path sits between the `BeginTransactionAsync` and the `CommitAsync` (for `AdvancePhase`, the path leaving Writing).
- *Code trace:* in `CreateGroupEndpoint` and `DissolveGroupEndpoint` the `hubContext…SendAsync` line comes after `CommitAsync`.
- *Code trace:* leaving Writing, `PhaseAdvancedEvent` is held until commit, so `PhaseChanged` goes out after every note has its `GroupId`.
- The existing `VoteEndpointTests` and `SprintEndpointTests` cases that advance Writing → Grouping stay green.

Runs: `SprintEndpointTests.cs`, `VoteEndpointTests.cs`, `CardEndpointTests.cs`.

Satisfies: spec BE-12/SK-14, AC1.

### Step 3 — Domain rule violations as 409; safe error bodies; missing user claim as 401
Follow error-handling, with the spec's overrides. Files:
- `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/Sprint.cs`
- `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs`
- `tests/Reflekt.UnitTests/Sprints/SprintTests.cs`
- `src/Services/Reflekt/Reflekt.API/Middleware/GlobalExceptionMiddleware.cs`
- `src/Services/Reflekt/Reflekt.API/Auth/ClaimsPrincipalExtensions.cs`

What to build:
- The domain conversions listed under Domain Model Changes. `EnsureOpen()` is added here; Step 4 calls it.
- Middleware mapping:
  - `DomainException` → 409;
  - any `HttpException` → its `StatusCode`;
  - `ArgumentException` and `ValidationException` → 400;
  - everything else → 500.
- Every body is serialised camelCase (`JsonSerializerDefaults.Web`): `statusCode`, `message`, `traceId`, `details` where present, and `errors[]` with `propertyName` and `errorMessage`.
- Outside Development a 5xx body has exactly `statusCode`, `message: "An unexpected error occurred."` and `traceId`. The `details` key is absent, not null: `JsonSerializerDefaults.Web` writes nulls, so the body type leaves the property out (for example `JsonIgnoreCondition.WhenWritingNull` on `details`). The real exception is still logged.
- In Development the body is today's: message plus stack.
- Run every integration test file. An assertion that changes only because a rule violation moves from 400 or 500 to 409, or because a body key changes case, is updated and named in `implementation.md`. Any other failure is a defect to fix, not a test to change.
- `GetUserId()` throws `UnauthorizedException("User id claim is missing.")`.
- The 6 `SprintTests` assertions change from `Throw<InvalidOperationException>` to `Throw<DomainException>` (lines 83, 93, 134, 142, 188, 198). They are named in `implementation.md`; `EvidenceLinkTests` is unchanged (its two assertions are on the kept defensive arms).

Accept:
- `grep -rn "InvalidOperationException" src/Services/Reflekt/Reflekt.Domain` gives exactly 4 hits: `Sprint.cs` timer-default and unknown-phase, `Card.cs` two "EvidenceLink … not found". It gives 13 today.
- `grep -c "Throw<DomainException>" tests/Reflekt.UnitTests/Sprints/SprintTests.cs` gives 6.
- `grep -n "InvalidOperationException" src/Services/Reflekt/Reflekt.API/Auth/ClaimsPrincipalExtensions.cs` gives none.
- *Code trace* of the middleware for the 5xx body outside Development: no exception message, and no `details` key in the serialised JSON (spec AC3).

Runs: every file in `tests/Reflekt.IntegrationTests/` (`AuthSmokeTests.cs`, `CardEndpointTests.cs`, `EvidenceLinkEndpointTests.cs`, `FoundationSmokeTests.cs`, `ParticipantEndpointTests.cs`, `SprintEndpointTests.cs`, `TeamEndpointTests.cs`, `VoteEndpointTests.cs`).

Satisfies: spec BE-07/SK-04/SK-05, BE-20/SK-07, SK-09 (part), AC2, AC3, AC8 (missing claim). Risk: risky — security kind (`ClaimsPrincipalExtensions.cs`), and every error response changes shape.

### Step 4 — Closed-sprint guard; `SetReviewStatus` 404 on a non-action item
Follow domain-patterns.

Files:
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/AddCardToGroup/AddCardToGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/RemoveCardFromGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/DissolveGroup/DissolveGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/UpdateColumns/UpdateColumnsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/SetReviewStatus/SetReviewStatusEndpoint.cs`

What to build:
- `sprint.EnsureOpen()` runs in the 5 group endpoints and `UpdateColumns`, after the 404/403 checks and before the phase-action check. `UpdateColumns` has no phase-action check, so there it runs before `UpdateColumnConfig`.
- In the hub, `StartTimer`, `PauseTimer`, `ResumeTimer`, `ResetTimer` and `AddTime` reject a closed sprint through one private hub helper. It loads the sprint through a created scope (as `JoinSprint` does), calls `sprint.EnsureOpen()`, and rethrows its `DomainException` as `HubException("Sprint is closed.")`. A client sees only a `HubException`'s text, so the domain exception never reaches the transport.
- `JoinSprint` never auto-starts a timer for a closed sprint. It checks `sprint.State` directly in its existing auto-start condition, skips the start and does not throw.
- `SetReviewStatus` returns 404 when the found card's `CardType` is not `ActionItem`. Its existing 409 (not Active, or not Writing) stays as it is, and it never guards the previous sprint.

Accept:
- `grep -rln "EnsureOpen()" src/Services/Reflekt/Reflekt.API` lists exactly the 6 endpoint files and `SprintHub.cs`. In `SprintHub.cs` the one call sits in the helper, inside a `catch (DomainException)` that throws `HubException("Sprint is closed.")`.
- *Code trace:* in each endpoint the call sits after `SendForbiddenAsync` and before `phaseActionCache.IsAllowed`.
- *Code trace:* a closed sprint answers `CreateGroup` and `UpdateColumns` with 409 `Sprint is closed.` through Step 3's mapping, before any save, so the data is unchanged.
- *Code trace:* `SetReviewStatus` checks `CardType` before `card.SetReviewStatus`; the previous-sprint action-item path is untouched.
- The existing `CreateCard_ClosedSprint_Returns409` and the other closed-sprint cases stay green.

Runs: `CardEndpointTests.cs`, `SprintEndpointTests.cs`.

Satisfies: spec BE-14, BE-07/SK-05 (404), AC3.

### Step 5 — Input validation: enums by name, `CardIds`, group title
Follow create-feature (validator).

Files:
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/SetReviewStatus/SetReviewStatusEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs`

What to build:
- `ColumnType` and `CardType` are valid only when they equal a declared enum name: case-sensitive, as today's `TryParse`, and no numeric string. Each fails with 400 through the validator.
- The endpoint's inline `TryParse` + `ThrowError` pair becomes a plain parse of the already-validated value. `SetReviewStatus` loses its inline `TryParse` the same way; its validator's set already rejects `None` and numbers.
- `CreateGroupRequest` gets a validator: `CardIds` not null, not empty, distinct. The endpoint keeps its "At least 2 cards" 400.
- `UpdateGroupTitleRequest` gets a validator: `Title` at most 100 characters when present.
- Both validators are sibling classes in their endpoint files.

Accept:
- `grep -c "Enum.TryParse" CreateCardEndpoint.cs SetReviewStatusEndpoint.cs` gives 0 in each.
- `grep -c "Validator<CreateGroupRequest>\|Validator<UpdateGroupTitleRequest>"` over the two endpoint files gives 2.
- *Code trace:* a numeric `ColumnType` ("1") fails the validator before the handler, so nothing is saved; a null `CardIds` fails before the handler's `.Count`.

Runs: `CardEndpointTests.cs`, `EvidenceLinkEndpointTests.cs`.

Satisfies: spec BE-19, SK-11 (part), AC8.

### Step 6 — Participant soft removal; readers that skip removed participants
Follow domain-patterns.

Files:
- `src/Services/Reflekt/Reflekt.Domain/Participants/Participant.cs`
- `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs`
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/ParticipantConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs`

What to build:
- The `Participant` changes listed under Domain Model Changes.
- `ParticipantConfiguration`: `DisplayName` collation `NOCASE`; `RemovedAt` optional; the unique `(SprintId, DisplayName)` index filtered `"RemovedAt" IS NULL`.
- `ParticipantRepository`:
  - `GetBySprintIdAsync` and `ExistsByDisplayNameAsync` skip removed participants;
  - `GetByGuestTokenAsync`, `GetByUserAndSprintAsync` and `GetByIdAsync` still return them;
  - `SaveChangesAsync` is overridden: a `DbUpdateException` whose SQLite error is the unique violation on `Participants.SprintId, Participants.DisplayName` becomes `DomainException("Name already taken.")` (→ 409); every other exception is rethrown unchanged;
  - `Remove` stays for now: the kick endpoint still calls it until Step 8, which deletes it.

Accept:
- `grep -n "RemovedAt" ParticipantRepository.cs` shows the two filtered readers only.
- *Code trace:* the advance-phase check's voter count, the participant list, the anonymous summary and both reports read through `GetBySprintIdAsync`.

Runs: none by name. Step 6 changes the EF model, and its migration is Step 7's, so the integration host's start-up `Migrate()` rejects the pending model change until Step 7 is built. Steps 6 and 7 form one verification boundary: Step 6 closes on the build and the `unit` role, and Step 7 runs Step 6's integration files with its own. Both are in build slice 2, so one developer builds them in turn.

Satisfies: spec BE-13/SK-17 (domain and readers), BE-15/SK-23 (index, 409), AC4.

### Step 7 — Migration with the duplicate-name repair; seed without a double vote
Follow persistence-patterns. Files:
- the new migration under `src/Services/Reflekt/Reflekt.Persistence/Migrations/` (`{timestamp}_SoftKickParticipants.cs` and `.Designer.cs`)
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/ReflektDbContextModelSnapshot.cs`
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs`

Generating the migration:
- Use the command in Migration Notes.
- `dotnet-ef` is not installed: install it with `--tool-path out/f35-scratch/tools`, version 10.0.13, matching `Microsoft.EntityFrameworkCore.Design`.
- If the install cannot run, write the migration and the snapshot by hand. The `full` suite's start-up `Migrate()` then proves there is no pending model change.

Order inside `Up`:
1. add `RemovedAt`;
2. alter `DisplayName` to `NOCASE`;
3. one or more `migrationBuilder.Sql` statements that rename duplicates as the spec's Decisions row 3 says:
   - keep the lowest-id participant's name;
   - give each later active duplicate, compared case-insensitively within its sprint, the first suffix ` 2`, ` 3`, … that is free against every active name in that sprint and every name already assigned;
   - trim the base so the result fits in 50 characters;
   - delete nothing;
4. create the index.

SQLite has no loops, so the repair must be correct for any number of duplicates, for example as a recursive CTE over the duplicates in id order. `Down` drops the index, the collation and the column, and leaves the renames.

`DbInitializer` drops the second `Vote.Create(wentWellCards[0]…, participants[0].Id)`, so no participant votes twice on one group.

Accept:
- A scratch probe builds a scratch database at the previous migration (`20260519160025_AddReviewStatus`, through `IMigrator.Migrate`). It inserts into one sprint the active names `Ana`, `ana`, `Ana 2` and two identical 50-character names, plus one removed-looking control in another sprint. It then migrates to the latest and lists the names after. Expected: `Ana`, `ana 3`, `Ana 2`, the first 50-character name unchanged, the second its first 48 characters + ` 2`; no row deleted.
- A raw `INSERT` of a duplicate active name into that sprint then fails with SQLite's unique-constraint error. That output is pasted; the 409 mapping is Step 6's code trace (spec AC9).
- `verify-run.js --role full` passes, which proves the start-up `Migrate()` sees no pending model change.
- `grep -c "participants\[0\].Id" DbInitializer.cs` drops by 1.

Runs: `ParticipantEndpointTests.cs` and `SprintEndpointTests.cs` (Step 6's), then the `full` role, as above.

Satisfies: spec BE-15, AC9. Risk: risky — migration kind; renames users' display names on an existing database.

### Step 8 — Removed token rejected; query token only on hubs; soft kick endpoint; Google rejoin 409
Follow authorization-patterns. Files:
- `src/Services/Reflekt/Reflekt.API/Auth/GuestTokenAuthenticationHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/KickParticipant/KickParticipantEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprintGoogle/JoinSprintGoogleEndpoint.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs`

What to build:
- The handler reads `access_token` from the query only when `Request.Path.StartsWithSegments("/hubs")`; the header and bearer forms are unchanged.
- A token whose participant `IsRemoved` fails authentication (401).
- The kick endpoint calls `participant.Kick()` and saves, with no `Remove`, and keeps its inline 404/409/403 checks in today's order and its 204. Kicking an already removed participant is a no-op that saves nothing new and returns 204.
- `ParticipantRepository.Remove` is deleted in this step, the same step that removes its last caller.
- `JoinSprintGoogle` returns 409 `You were removed from this sprint.` when the existing participant is removed. A guest can join again only as a new participant, which today's anonymous join already does.

Accept:
- `grep -n 'Query\["access_token"\]' GuestTokenAuthenticationHandler.cs` sits under a `/hubs` path check.
- `grep -c "Remove(participant)" KickParticipantEndpoint.cs` gives 0.
- `grep -rn "participantRepository.Remove\|public void Remove" src/Services/Reflekt/Reflekt.API src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` gives no hits.
- *Code trace:* a removed participant's token returns `AuthenticateResult.Fail` (401); the hub's negotiate request (`/hubs/sprint/negotiate?access_token=`) still authenticates; `?access_token=` on `/api/...` is ignored, so the request is unauthenticated (401 on a guarded route).
- *Code trace:* a second kick of the same participant reaches `Kick()` as a no-op and returns 204.

Runs: `ParticipantEndpointTests.cs`, `AuthSmokeTests.cs`, `FoundationSmokeTests.cs`.

Satisfies: spec BE-13, BE-28 (part), AC4, AC8. Risk: risky — security kind (authentication handler).

### Step 9 — Hub: participant group, removed participants rejected, ordered kick revocation
Follow create-domain-event-handler for the kick handler. Files:
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/KickParticipant/BroadcastParticipantKickedHandler.cs`

Hub changes:
- `JoinSprint`, `ToggleReady` and the five timer methods load the caller's participant and throw `HubException("Participant has been removed.")` when it is missing or removed. `JoinSprint` does this before any group is joined.
- The hub keeps a static registry from connection id to its `HubCallerContext`, sprint id and participant id. Entries are added in `JoinSprint` and removed only in `OnDisconnectedAsync`. Revocation finds a participant's connections here, so a connection that called `LeaveSprint` is still found and aborted.
- `JoinSprint`, in this order, so a join cannot slip past a kick:
  1. the removed check above;
  2. registry entry and `ConnectionState` entry;
  3. `participant:{participantId}` group, then `sprint:{sprintId}` group;
  4. **a second removed check, read from the database after the group joins.** If the participant is now removed, the method takes the connection out of both groups and the state, drops its registry entry, and throws the same `HubException`.

  Why this closes the race: a kick commits before its handler runs. If the second check reads before the commit, the registry entry already exists when the handler looks, so the handler evicts and aborts the connection. If it reads after the commit, the join undoes itself.
- `LeaveSprint` also removes the connection from `participant:{participantId}`, beside today's sprint-group and `ConnectionState` removal.
- The hub exposes the static seams the kick handler needs:
  - a participant's connections in a sprint, from the registry;
  - abort those connections;
  - drop the participant from connection and readiness state, returning the counts.

  The names are the developer's, and F38 later moves them into its `ReadinessTracker`.

The kick handler, in this order:
1. Send `ParticipantKicked` to the sprint group, payload unchanged.
2. Then, in a `finally` so an exception or cancellation in the send never skips it:
   - remove the participant's connections from `sprint:{id}` and `participant:{id}`;
   - abort them;
   - drop them from state;
   - send the updated readiness as `ReadinessChanged`, in the payload `OnDisconnectedAsync` sends today (`displayName: null`, `isReady: false`).
3. Each revocation sub-step runs even if an earlier one throws, and the revocation sends use `CancellationToken.None`.

Accept:
- `grep -n "participant:" SprintHub.cs` shows the group join.
- *Code trace:* `finally` (or equivalent) wraps group removal, `Abort()` and state removal after the `ParticipantKicked` send.
- *Code trace:* every hub method named above reaches the removed check before acting.
- *Code trace:* in `JoinSprint` the second removed check comes after both group joins, and its failure path removes the connection from both groups; `LeaveSprint` removes the participant group.
- *Code trace:* the kick handler takes the connections from the registry, not from `ConnectionState`.

Runs: `ParticipantEndpointTests.cs`.

Satisfies: spec BE-13 (hub and handler), AC4. Risk: risky — access revocation.

### Step 10 — One Writing-phase redaction rule on every read path
None. Create `src/Services/Reflekt/Reflekt.API/Features/Cards/WritingPhaseRedaction.cs` and modify:
- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/GetGroups/GetGroupsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetSprintReport/GetSprintReportEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs`

The rule, as one public static class:
- A card is shown in full unless all three hold: its sprint is the given sprint, that sprint's `CurrentPhase` is `Writing`, and the viewer has a participant id that is not the card's `ParticipantId`.
- A viewer with no participant identity sees everything.
- Redacted means today's `ListCards` shape: text `""`, no assignee, no evidence links.
- The class offers the decision and the redacted forms each caller needs:
  - `CardResponse` for `ListCards` and the broadcasts;
  - `CardGroupCardEntry` for `GetGroups`;
  - `ReportCardEntry` and `ReportActionItemEntry` for the reports.
- A report group title derived from its first card's text, when `Title` is null, is `""` when that card is redacted. A title the Scrum Master set is kept.
- The previous-actions sections (`GetPreviousActions`, the reports' `PreviousActionsReview`) are not passed through the rule.
- The viewer id is the request's `participantId` claim, or null.

Accept:
- `grep -rln "WritingPhaseRedaction" src/Services/Reflekt/Reflekt.API` lists the new file and these 4 endpoints. Step 11 adds the handlers.
- `grep -rn "redactOthers" src` gives none.
- *Code trace* per endpoint: in Writing, another participant's current-sprint card has no text, assignee or evidence anywhere in the body, the derived group title included; the previous actions are still returned (spec AC5).

Runs: `CardEndpointTests.cs`, `SprintEndpointTests.cs`.

Satisfies: spec BE-02 (read paths), AC5. Risk: risky — security (data leak).

### Step 11 — Card broadcasts: full to the author, redacted to the others
Follow create-domain-event-handler. Files:
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/BroadcastCardCreatedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/BroadcastCardUpdatedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/BroadcastAndFetchOnEvidenceLinkAttachedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/BroadcastCardUpdatedOnEvidenceLinkDetachedHandler.cs`
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/FetchEvidenceLink/BroadcastEvidenceLinkFetchedHandler.cs`

What to build:
- Each handler loads the card's sprint through a scope it creates (`IServiceScopeFactory`, as `SprintHub` does), never a constructor-injected repository.
- In Writing, every recipient is addressed through its own participant group, never through the sprint group:
  - the full payload goes to `participant:{authorId}`;
  - the redacted payload goes to `Clients.Groups(…)` over `participant:{id}` for every other active participant of the sprint, the ids read through `ParticipantRepository.GetBySprintIdAsync` in the same scope.

  Every hub connection carries a participant id (the guest-token scheme always issues the `participantId` claim, `GuestTokenAuthenticationHandler.cs:31`), and each connection is in exactly one participant group. So no connection can receive the other form, whenever it joins. A connection that joins after the send misses that one message, as any late joiner does today.
- `EvidenceLinkFetched` goes in Writing only to `participant:{authorId}`, payload unchanged.
- Outside Writing every handler sends exactly what it sends today.
- Message names and payload shapes do not change.
- The attach handler still writes the fetch job.

Accept:
- `grep -rln "WritingPhaseRedaction" src/Services/Reflekt/Reflekt.API/Features` adds these 5 files, 9 endpoint and handler files in all.
- *Code trace* per handler: in Writing no `Group("sprint:…")` call is made, the full card goes only to the author's participant group, and the others' groups get the redacted form; outside Writing the call is today's.

Runs: `CardEndpointTests.cs`, `EvidenceLinkEndpointTests.cs`.

Satisfies: spec BE-02 (broadcasts), AC5. Risk: risky — security (data leak).

### Step 12 — Safe return URL; dev front-end origin from configuration
None. Files:
- `src/Services/Reflekt/Reflekt.API/Features/Auth/Login/LoginEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/appsettings.json`

What to build:
- One public static function in the login slice accepts a return URL only when all of these hold:
  - it starts with exactly one `/`;
  - the next character is neither `/` nor `\`;
  - it has no character below U+0020 and no U+007F;
  - it has no `://`.

  The spec's four-part rule is applied to the value as received. Today's check of the `Uri.UnescapeDataString` form (`LoginEndpoint.cs:35`) stays exactly as it is: the decoded value starts with `/`, does not start with `//`, and has no `://`. The spec's other parts are not applied to the decoded form, so a value such as `/docs/%09notes`, accepted today, is still accepted. Both checks must pass. Otherwise the redirect is `/`, with no error.
- `IWebHostEnvironment` and `IConfiguration` come in through the constructor.
- In Development the origin prefix is the value of `Auth:DevFrontendOrigin`, added to `appsettings.json` as `http://localhost:5173`. Today's prefixing stays as it is, with the literal replaced by the configured value; no separate branch is written for a missing key.

Accept:
- `grep -n "localhost:5173" src/Services/Reflekt/Reflekt.API --include=*.cs -r` gives none; it gives 2 today, both in `LoginEndpoint.cs`.
- A scratch probe calls the public function and records: `/sprint/12` accepted; `//evil.com`, `/\evil.com`, `/` + TAB + `/evil.com` and `https://evil.com` rejected (spec AC6).
- The existing `Login_RedirectsToGoogle` stays green.

Runs: `AuthSmokeTests.cs`.

Satisfies: spec BE-21, AC6. Risk: risky — security kind (login endpoint) and the `secrets` kind (`appsettings.json`); only a non-secret origin is added.

### Step 13 — Connect-time SSRF check on the OpenGraph client
None. Create `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphAddressPolicy.cs` and modify:
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphConfiguration.cs`
- `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphFetcher.cs`

The address check is one public static function on the new class. It rejects:
- IPv4: `0.0.0.0/8`, `10/8`, `100.64/10`, `127/8`, `169.254/16`, `172.16/12`, `192.168/16`, `224/4` and `255.255.255.255`.
- IPv6: `::`, `::1`, `fe80::/10`, `fc00::/7` and `ff00::/8`.
- IPv4-mapped IPv6: the embedded IPv4 is checked.

Wiring:
- The `opengraph` client's primary handler is a `SocketsHttpHandler` whose `ConnectCallback` resolves the host and refuses when any resolved address is rejected. It connects only to a checked address, so DNS rebinding cannot swap one in.
- Redirects stay on: every hop opens its connection through the same callback.
- `FetchAsync` keeps its scheme check and drops `IsPublicHostAsync` / `IsPrivateIpv4`, so the connect-time check is the one guard.

Accept:
- `grep -c "IsPrivateIpv4\|IsPublicHostAsync" OpenGraphFetcher.cs` gives 0.
- `grep -n "ConnectCallback" OpenGraphConfiguration.cs` gives 1 hit.
- A scratch probe records the function's answer for one address per listed range, in IPv4 and IPv6, for `::ffff:127.0.0.1`, and for `93.184.215.14` (allowed).
- A scratch probe resolves `OpenGraphFetcher` from the app's service registration and fetches `http://127.0.0.1:{port}/` of a `TcpListener` it starts. Expected: `FetchResult.Failed`, with the listener accepting no connection (spec AC7).

Runs: `EvidenceLinkEndpointTests.cs`.

Satisfies: spec BE-23, AC7. Risk: risky — security (SSRF).

### Step 14 — Close-out checks
None. No source file.

Accept:
- The greps of Steps 1, 3, 4, 5, 8, 10, 11, 12 and 13 run once more on the final tree, outputs pasted.
- `verify-run.js --role full` passes (spec AC10).
- `git diff 1eec0b8bfb0ad76895a147416b51d6dbfa5c6bb0 -- Directory.Packages.props` shows exactly one added line, the Relational `PackageVersion` at 10.0.13, and no changed or removed line.
- `graphify update .` runs. Its `graphify-out/` output is generated, so it is committed but not reviewed and kept out of the file-list check below. Where the `graphify` command is not found, that is recorded and F40's graph refresh covers it.
- `git diff --name-only 1eec0b8… -- . ':!graphify-out'` plus `git status --porcelain -- . ':!graphify-out'` list only:
  - the files Steps 1–13 name, and any integration test file whose assertion Step 3 updates (named in `implementation.md`);
  - `docs/specs/F35-BackendCorrectnessFixes/` (its `definition/spec.md` changing only the `**Plan:**` line);
  - the campaign's `.claude/.personas.json` and `.claude/tmp/`.
- `test ! -e out/f35-scratch` succeeds.

## Migration Notes

```bash
dotnet ef migrations add SoftKickParticipants -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

Run with `out/f35-scratch/tools/dotnet-ef` when the tool is installed there (Step 7). Production applies the migration at start-up through `UseReflektPersistence` → `Migrate()`, as every migration does today. The renames are recorded in the migration file's comment.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|---|---|---|
| Routes, request and response field names, and status codes, except the changes the spec lists | the existing integration suite (`full` role) | pinned for covered routes; accepted-gap for routes no test calls — the reviewer's diff read |
| The author sees their own cards in full during Writing | `ListCards_ReturnsAllCardsInSprint` and `ListCards_OrderedByCreatedAtDescending` (own cards, participant token) | pinned |
| The owner's REST view (no participant identity) is full | Step 10 code trace | accepted-gap — no new test (P1) |
| The previous-action review of a closed sprint's action items keeps working | Step 4 and Step 10 code traces | accepted-gap — no new test (P1) |
| SignalR message names and payload shapes | Steps 2, 9 and 11 code traces; the frontend is unchanged | accepted-gap — no new test (P1) |
| No package version change (one added reference at the already resolved version, P7) | Step 14 `git diff` on `Directory.Packages.props` | pinned |
| Existing tests pass; only assertions a fix changes are updated (the 6 `SprintTests` throw types, plus any integration assertion Step 3 names) | `full` role; Step 3 grep | pinned |

## Testing Strategy

No new test cases (owner rule, P1). The only test edits are Step 3's: the six `SprintTests` throw types, plus any existing integration assertion whose status code or body-key casing the spec changes, each named in `implementation.md`. Proof comes from three sources:
- the existing unit and integration suites: the `unit` role per step, each step's **Runs:** integration files by name, and the `full` role at Steps 7 and 14;
- plan-time greps re-run at close;
- code traces and scratch probes that leave no file, mapped to the spec's acceptance in Decisions rows 2–8.

## KB Impact

None — the repo has no `docs/kb/`. The architecture doc and the conventions are F40's.

## Decisions

| Decision | Why | Rejected alternative | Confidence | Status |
|---|---|---|---|---|
| No step writes a test; TDD is `no` on every step; only assertions a fix changes are updated (Step 3). | The owner's directive binds (`questions.md` Q1); see plan P1. | The spec's new test cases. | high — owner directive | decided |
| AC1's test cases become Step 1's greps, Step 1's five code traces and Step 2's ordering traces, plus the existing phase-advance tests. | A new test is forbidden (P1). | New interceptor tests. | high | decided |
| AC3's integration cases become Step 4's guard-placement traces, Step 3's middleware trace and the existing closed-sprint tests. | P1. | New integration tests. | high | decided |
| AC4's kick cases become Step 8's and Step 9's code traces. | P1. | A kick test and a hub-fake handler test. | high | decided |
| AC5's unit, integration and handler cases become the `WritingPhaseRedaction` grep over the 9 files plus per-file code traces. | P1. | Redaction tests with hub-context fakes. | high | decided |
| AC6 and AC7 become scratch-probe evidence runs in `out/f35-scratch/` (git-ignored), pasted into `implementation.md` and deleted at Step 14. | A throwaway run of the public functions is evidence, not a test; F37 used the same route under the same rule. | Code trace only, a weaker proof for pure functions. | medium — reading of the owner's "no new tests" | decided |
| AC8's integration cases become Steps 3, 5 and 8 code traces. | P1. | New integration tests. | high | decided |
| AC9's "duplicate insert → 409" becomes a raw `INSERT` failing in the scratch database plus Step 6's mapping trace; the recorded migration run stays as the spec says. | P1; the spec itself names the recorded run. | A test. | high | decided |
| The interceptor keeps two lists, collected (this save) and held (an explicit transaction's). Transaction callbacks touch held only, and `TransactionStartedAsync` clears held, so events from a transaction disposed without commit are dropped at the next transaction start (explicit, or EF's own save transaction). | EF raises no event for a dispose without commit, and EF's own save transaction raises the same callbacks as an explicit one; one list would let that implicit commit publish or drop the wrong events (R1-03). | One list; clearing at the next save with no transaction. | medium — EF has no dispose hook, so this rests on reading the interface | decided |
| The sync `SaveChanges` paths are not overridden. | `grep -rn "SaveChanges()" src` gives no caller. | Overriding them too. | high | decided |
| `CastVote`'s serializable transaction also gets post-commit publication; F38's "adds only `CastVote`" is already met. | The spec's mechanism is generic over any explicit transaction. | Excluding `CastVote`. | high | decided |
| `BeginTransactionAsync` lives on `RepositoryBase`; endpoints never touch `DbContext`. | One way to open a transaction, inside the repository rule. | A per-repository method, as `VoteRepository` has. | medium — F38 may move EF types out of the API | decided |
| `AdvancePhase`'s transaction wraps only the transition out of Writing. | The spec names that transition; the others are one save each and stay as today (R1-10). | Every branch in a transaction. | high | decided |
| The hub's timer methods reject a closed sprint through one private helper that calls `EnsureOpen()` and rethrows its `DomainException` as `HubException("Sprint is closed.")`. `JoinSprint` checks `sprint.State` in its auto-start condition, skips the start and does not throw. | Clients see only a `HubException`'s text, and a participant may still open a closed sprint's board (R1-08). | Throwing in `JoinSprint`; letting the `DomainException` reach the client. | medium | decided |
| The hub re-checks the participant in the database on every listed method. | The connection's claims were fixed at connect. | An in-memory removed set. | high | decided |
| Abort and revocation use a static connection registry on the hub (connection id → `HubCallerContext`, sprint id, participant id), cleared only on disconnect; F38's `ReadinessTracker` takes it over. | `IHubContext` cannot abort a connection, the hub state is static today, and a connection that left the sprint must still be found (R1-05). | A new tracker service now, which is F38's work; enumerating `ConnectionState`. | medium | decided |
| `JoinSprint` registers the connection, joins both groups, then re-reads the participant and undoes the join if it is now removed. | A kick commits before its handler runs, so either the handler sees the registered connection or the second read sees the removal (R1-02). | A lock shared by join and kick; one check before the join only. | high — the order argument needs no timing assumption | decided |
| `LeaveSprint` also leaves the participant group. | Otherwise a left connection keeps receiving the author's full notifications (R1-05). | Leaving the participant group to disconnect. | high | decided |
| The removed marker is `RemovedAt` (nullable `DateTime`). | It records when, which a flag does not. | `bool IsRemoved` column. | high | decided |
| A removed Google participant gets 409 `You were removed from this sprint.` | The spec fixes the status, not the text. | Reusing "Name already taken." | high | decided |
| The display-name unique violation becomes `DomainException("Name already taken.")` inside `ParticipantRepository.SaveChangesAsync`; see plan P4. | It keeps EF types out of the API and matches the endpoint's own message. | A catch in each endpoint; a middleware `DbUpdateException` case. | high | decided |
| Case-insensitivity comes from `DisplayName` collation `NOCASE` (ASCII folding, as today's `lower()`). | The repo's own precedent for `Users.Email`. | A raw-SQL expression index unknown to the EF model. | high | decided |
| `ParticipantRepository.Remove` is deleted in Step 8, with its last caller. | Deleting it earlier breaks the build at the step boundary (R1-07). | Deleting it in Step 6; leaving dead code for F38. | high | decided |
| The return-URL rule applies the spec's four parts to the value as received, and keeps today's three decoded-form predicates exactly (`LoginEndpoint.cs:35`: starts with `/`, not `//`, no `://`). The dev origin is the value of `Auth:DevFrontendOrigin` in `appsettings.json`, prefixed as today, with no branch for a missing key. | Dropping today's decoded check would accept values today rejects, and applying the spec's full rule to the decoded form would reject values today accepts (R2-02); either way behaviour changes beyond the spec. `appsettings.Development.json` is git-ignored (`.gitignore:426`) (R1-10). | The received value only; the spec's rule on both forms; a Development-only file; a designed fallback for a missing key. | medium | decided |
| Redirects stay on for the OpenGraph client; the DNS pre-check is removed; see plan P5. | `ConnectCallback` checks every hop; the pre-check would make the `127.0.0.1` run fail before connect. | Redirects off. | high | decided |
| The 5xx body outside Development is `statusCode`, `message: "An unexpected error occurred."` and `traceId`, with no `details` key; Development keeps message and stack. | The spec fixes the rule, not the text, and requires no `details` (R1-06). | Dropping `message`; `details: null`. | high | decided |
| The new validators are sibling classes in their endpoint files. | The request records live there. | New files. | high | decided |
| `dotnet-ef` is installed to `out/f35-scratch/tools`; when it cannot be installed, the migration is written by hand. | No global or repo change; the start-up `Migrate()` check catches a wrong snapshot. | A global tool install. | medium — needs network | decided |
| In Writing, card broadcasts go to participant groups only: full to the author's, redacted to each other active participant's; see plan P8. | Each connection is in exactly one participant group, so no exclusion list can go stale (R1-04). | The sprint group with the author's connections excluded (P2, superseded). | high | decided |
| `Blocks.Persistence` takes a direct `Microsoft.EntityFrameworkCore.Relational` reference at 10.0.13; see plan P7. | `IDbTransactionInterceptor` lives there (R1-01). | A wrapper transaction from `RepositoryBase` that flushes on commit, which misses `VoteRepository`'s transaction. | high | decided |
| Departs from domain-patterns, required by spec BE-12 ("with no transaction, events publish after the save as today"): publication keeps `Mode.WaitForNone`, where the skill's FastEndpoints publisher uses `Mode.WaitForAll`. | Waiting for every handler would change request timing, which this cleanup must not do (R1-11). | `Mode.WaitForAll`. | high | decided |
| Departs from error-handling, required by spec BE-07/SK-05 and BE-20/SK-07: `DomainException` maps to 409 and the body is camelCase with no 5xx details, where the skill maps it to 400 with a PascalCase body. | The spec sets both; the skill is logged as ill-fitting in `lessons.md` (R1-11). | Following the skill. | high | decided |
| Departs from authorization-patterns, required by the spec's Out of scope (conventions and structure findings are F38's): the kick endpoint keeps its inline 404/409/403 checks instead of a resource-gate handler. | Moving the checks is structure work outside F35 (R1-11). | A resource-gate authorization handler. | high | decided |
| Departs from create-domain-event-handler, required by the spec's Out of scope (handler naming is F38's BE-25): `BroadcastParticipantKickedHandler` keeps its name while it also evicts and aborts. | Renaming is F38's naming pass; the spec asks for one ordered handler (R1-11). | Renaming it, or splitting it into several handlers, which would lose the order. | high | decided |

## Decisions and why

### P1 — No new tests; the spec's test acceptance becomes code traces and evidence runs
- **By:** owner
- **Decided:** no new test case, class or file. Only existing assertions a fix changes are updated (the 6 `SprintTests` throw types, and any integration assertion whose status or body-key casing the spec changes). Each spec acceptance that asked for a test is replaced as Decisions rows 2–8 say.
- **Why:** "write no new tests, update an existing test only where a fix changes what it asserts" (the owner's standing campaign directive, relayed by the campaign coordinator 2026-10-23).
- **Turned down:**
  - regression cases plus three new test classes, as the spec said (the architect's recommendation);
  - regression cases in existing classes only.
- **Rests on:** the campaign directive; if the owner later allows regression cases, the spec's acceptance 1 and 3–9 come back as tests.
- **Confidence:** high — the owner's own rule.
- **Source:** `questions.md` Q1
- **State:** current

### P2 — The author's connections cannot receive a redacted copy of their own card, and others never a full one
- **By:** architect
- **Decided:** in Writing, the full card goes only to `participant:{authorId}`, joined in `JoinSprint`. The redacted copy goes to the sprint group, excluding the author's tracked connections. Joining registers the connection and the participant group before the sprint group.
- **Why:** this answers the spec review's handed-over "author-connection window". No ordering can send a full payload to another participant. The worst window gives the author a redacted copy of their own card, which is not a leak.
- **Turned down:** sending the redacted copy to the whole sprint group and the full copy after it — the client keeps the first `CardCreated` and would hold the blank one.
- **Rests on:** `useSprintHub.ts` ignoring a second `CardCreated` for a card it holds.
- **Confidence:** high
- **Source:** `review-critic.md` R1-18 (plan-must-resolve)
- **State:** superseded by P8 — plan review round 1, R1-04: a connection joining between the two sends can be left with only the redacted copy.

### P3 — `EvidenceLinkFetched` keeps its payload and goes only to the author during Writing
- **By:** architect
- **Decided:** the payload is the evidence link, unchanged. During Writing it is sent only to `participant:{authorId}`; outside Writing it goes to the sprint group, as today.
- **Why:** this answers R1-18's "`EvidenceLinkFetched` payload". The payload is not a card, so it has no redacted form; the spec sends it to the author only.
- **Turned down:** a redacted link payload to the others — it has no shape the client uses.
- **Rests on:** the spec's BE-02 row.
- **Confidence:** high
- **Source:** `review-critic.md` R1-18
- **State:** current

### P4 — The display-name unique violation maps to 409 in the repository
- **By:** architect
- **Decided:** `ParticipantRepository.SaveChangesAsync` turns that one index's violation into `DomainException("Name already taken.")`, which the middleware sends as 409.
- **Why:** this answers R1-18's "unique-violation → 409 mapping". Persistence already references Domain, and F38 removes EF types from the API.
- **Turned down:** a catch in each join endpoint; a middleware `DbUpdateException` case.
- **Rests on:** SQLite's constraint message naming `Participants.SprintId, Participants.DisplayName`.
- **Confidence:** high
- **Source:** `review-critic.md` R1-18
- **State:** current

### P5 — The SSRF range list is closed at the spec's list
- **By:** architect
- **Decided:** exactly the ranges in Step 13, the spec's eight kinds in IPv4 and IPv6 plus IPv4-mapped. NAT64 and 6to4 prefixes are not added.
- **Why:** this answers R1-18's "open-ended SSRF list". The spec names the kinds, and a closed list is checkable.
- **Turned down:** an open-ended "every special-purpose range" list.
- **Rests on:** the spec's BE-23 row.
- **Confidence:** medium — NAT64 reaches loopback only on a network that runs a NAT64 gateway.
- **Source:** `review-critic.md` R1-18
- **State:** current

### P6 — No `InternalsVisibleTo` reliance
- **By:** architect
- **Decided:** every new rule this feature adds is public (the redaction class, the return-URL function, the address check), and no probe or test reads an internal member.
- **Why:** this answers R1-18's "`InternalsVisibleTo`". F38 deletes it.
- **Turned down:** internal helpers read through `InternalsVisibleTo`.
- **Rests on:** F38's BE-10 row.
- **Confidence:** high
- **Source:** `review-critic.md` R1-18
- **State:** current

### P7 — `Blocks.Persistence` takes a direct EF Core Relational reference
- **By:** architect
- **Decided:** add `Microsoft.EntityFrameworkCore.Relational` to `Blocks.Persistence.csproj`, and one `PackageVersion` line at 10.0.13 to `Directory.Packages.props`. No existing line changes.
- **Why:** `IDbTransactionInterceptor` is in the Relational package, and a project cannot compile against a reference only its consumer has (R1-01). `Microsoft.EntityFrameworkCore.Sqlite` 10.0.13 already brings Relational 10.0.13 into every project that uses it, so the resolved version does not move and the spec's "no package version changes" holds.
- **Turned down:** a wrapper transaction returned by `RepositoryBase` that publishes on commit. It needs no new reference, but misses `VoteRepository.BeginSerializableAsync` and any transaction opened another way.
- **Rests on:** EF Core providers depending on the Relational package of their own version.
- **Confidence:** high
- **Source:** `review-critic.md` R1-01
- **State:** current

### P8 — Writing-phase card broadcasts address participant groups only
- **By:** architect
- **Decided:** in Writing, the full card goes to `participant:{authorId}` and the redacted card to `participant:{id}` of each other active participant of the sprint. No Writing-phase card broadcast uses the sprint group.
- **Why:** this closes the author-connection window R1-18 handed to the plan, which P2 left open (R1-04). Every hub connection carries a participant id (`GuestTokenAuthenticationHandler.cs:31`) and sits in exactly one participant group, so no connection can get the other form, whenever it joins. A connection that joins after the send misses that one message, as a late joiner does today.
- **Turned down:**
  - the sprint group with the author's connections excluded (P2) — the exclusion list can go stale between the two sends;
  - redacted first, full second — the client keeps the first `CardCreated` (`useSprintHub.ts:75–78`).
- **Rests on:** every hub connection authenticating with the guest-token scheme (`SprintHub` is `[Authorize]`d with that scheme only).
- **Confidence:** high
- **Source:** `review-critic.md` R1-18, R1-04
- **State:** current

## Open Questions

None.

## Plan Review

- **Round 1 (2026-10-23, Codex primary NO-GO + Sonnet second reader ACCEPT):** 13 merged findings, R1-01 to R1-13 (2 HIGH, 9 MEDIUM, 2 LOW), all folded. The record, merge and fold are in `review-critic.md` § Round 1 — 2026-10-23 — plan.
  - R1-01: the Relational reference (Step 1, P7).
  - R1-02: the join re-check (Step 9).
  - R1-03: the collected and held lists (Step 1).
  - R1-04: participant-group fan-out (Step 11, P8; P2 superseded).
  - R1-05: `LeaveSprint` and the registry (Step 9).
  - R1-06: no `details` key (Step 3).
  - R1-07: `Remove` moved to Step 8.
  - R1-08: the hub helper (Step 4).
  - R1-09: **Runs:** lines.
  - R1-10: `AdvancePhase` narrowed to the spec; return URL and dev origin reworded as today's behaviour.
  - R1-11: four `Departs from` rows.
  - R1-12: the dissolve code trace.
  - R1-13: `graphify-out` kept out of the file-list check.
- Reconciled: Context (package rule), Size line, Skill Mapping row 1, Steps 1–14 (Runs lines; Step 9 seams vs Step 11; Step 6/8 `Remove`; Step 14 package and file-list checks), Must-NOT-Change, Testing Strategy, Decisions, P1, P2, P7, P8.
- **Round 2 (2026-10-23, Codex primary, full sampled read, NO-GO):** R1-01 to R1-09 and R1-11 to R1-13 verified closed, R1-10 partial. Two MEDIUM, both tagged `fold`, folded:
  - R2-01: Steps 6 and 7 form one verification boundary, with Step 6's integration files run in Step 7.
  - R2-02: the decoded return-URL check keeps today's three predicates exactly (Step 12, Decisions).
  No round 3 by schedule (no CRITICAL). Fold record: `review-critic.md` § Round 2 — plan, `### Fold — round 2`.
- **Plan departures:** the four `Departs from` rows (domain-patterns, error-handling, authorization-patterns, create-domain-event-handler) are each required by a source and are **kept**. This is an unattended run, so the away answerer took the recommendation (keep), relayed by the campaign coordinator 2026-10-23; not owner-confirmed. No `Adds:` rows.
- **Approved** 2026-10-23 for the build. Lane: thorough — full team — normal gear.
