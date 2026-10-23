# F38 — Backend Conventions Conformance — Done Check

## Step 1 — Done-Check

### Check 1 — 2026-10-23 — after the build

**Model:** claude-opus-5-5[1m]

Pre-commitment predictions: (1) a skill slip on the steps that name a second skill (3, 4, 10, 15); (2) edits outside the step file lists (Steps 5–15 add services and delete helpers); (3) a developer git write. Found: (1) none — every mapped skill is in the log; (2) yes, each disclosed with a reason; (3) yes, the one staged rename the dispatch sanctions.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — One error convention and one error body | Implemented | `ForbiddenException`/`ConflictException` present; `ResponseBuilder` 1 hit (`DependencyInjection.cs:79`); body assertion added to `CreateTeam_EmptyName_ReturnsValidationError` (verified in diff). `Guard.cs` and `Extensions.cs` needed no edit here (already the adopted helpers). |
| 2 — Claim names, typed claim accessors, merged policies, `Features/Account` | Implemented | All accept greps re-run empty; raw claim names only in `GuestClaims.cs`. `Auth.Policies.X` form is the plan's own text (Q1). |
| 3 — `SprintAccess` and one phase-action guard | Implemented | `PhaseActionCache.cs` gone; no `ReflektDbContext` in API; "is not allowed in phase" only `PhaseActionPolicy.cs:23`; 16 `EnsureAllowed(` sites; one `OwnerId` comparison in `SprintAccess.cs:22`. See plan-hygiene note on `GetSprintSummary`. |
| 4 — Event publication logs every handler failure | Implemented | No `WaitForNone`; `LogError` ×2; `CancellationToken.None` at line 153; all `*Handler.cs` naming a repository use `IServiceScopeFactory`; FastEndpoints `WaitForAll` behaviour recorded from a probe (P5's premise confirmed). |
| 5 — Teams and Sprints | Deviated (valid) | `DependencyInjection.cs` registers `TeamLimitCheck` outside the file list (skill's registration step). Accept greps empty; `ErrorCodes.` 2 sites; `CreateSolo` on leaving Writing; in-memory discussed count and Active-before-phase order traced. |
| 6 — CardGroups | Deviated (valid) | `DependencyInjection.cs` registers `CardRegrouping`/`FocusHandover` outside the file list. `CardGroup.Create(` only in `CreateGroupEndpoint`; `CreateSolo(` at Dissolve, RemoveCardFromGroup, AdvancePhase, DbInitializer; no `OrderBy` in the created handler; payload traces for `GroupCreated`/`GroupDissolved` present. |
| 7 — Votes | Deviated (valid) | `SprintAccess.RequireParticipant()` and the `VoteBudget` registration are outside the file list (one text owner; registration). `MarkRetracted()` precedes `Remove` (now lines 33/34; later steps shifted the lines); payload traces present; no group lookup added to `RetractVote`. |
| 8 — Cards, EvidenceLinks, Participants, PreviousActions | Implemented | Accept greps empty; `CardBroadcast` takes `IHubClients`; redaction trace present. |
| 9 — Reports | Deviated (valid) | `SprintReportBuilder` registration outside the file list. Endpoint files 138/139 → 26 lines; six redaction calls all in the builder. |
| 10 — Hub, readiness and timer | Implemented | Only `ParticipantGroup` static on the hub; removed names gone; `Features/Sprints/Timer/` gone, `Hubs/` holds the four files; both `JoinSprint` checks call `IsActiveAsync` (hub lines 51, 214); `GetByIdAsync` only at 75 (timer sprint) and 166 (`RequireScrumMasterAsync`). Text and payload tables present. |
| 11 — No EF in the API layer; async migrate/seed | Implemented | AC4 grep empty; no `GetAwaiter().GetResult()`; `Query()` protected; `Program.cs:8` awaits `MigrateAndSeedAsync`. The csproj framework reference stays under the plan's own "if nothing else needs it" condition (reason recorded). |
| 12 — Reads | Deviated (valid) | `Hubs/ReadinessTracker.cs` edited outside the file list (BE-35 rule on a public record); emptiness check is an exists query rather than a count (conventions' `Any()`). All accept greps empty; `AsNoTracking` 10 lines vs 0 at base; BE-18 sizes pasted, each ≤ 60. |
| 13 — Limits as constants | Deviated (valid) | First accept grep is empty only with generated `Migrations/` excluded (EF history, not editable under "no migration"). OpenGraph grep empty. |
| 14 — Endpoint type layout | Implemented | AC6 grep empty; 27 files vs the plan's 28 explained (Step 12 already emptied `GetMyTeamsEndpoint.cs`). |
| 15 — Options, dead code, domain polish, naming | Deviated (valid) | Out-of-list edits (seven repositories' base clause, `RepositoryBase`/`Guard` dead members, `LoginEndpoint`/`AuthSettings` for the missed `IConfiguration` read) all under the step's own rules. The `git mv` staged rename is a developer git write against the campaign rule — disclosed and sanctioned by the dispatch; left for the coordinator's commit. Re-run: every accept grep holds; `ValidateOnStart` 4 (Google check evidence recorded); `Directory.Packages.props` diff is the one removed line. |
| 16 — One shared integration-test host | Deviated (valid) | Base-commit counts not read at the step; read in Step 17 and re-verified here: all eight integration files match the base (3/26/19/1/14/16/6/19). No `AllowedEmails`; no inline `WebApplicationFactory` in `*EndpointTests.cs`. Test diff adds only the three P2 assertion blocks; no assertion removed. |
| 17 — Verification and close-out | Implemented | AC1–AC6 re-run on the final tree: all hold. AC7 evidence run pasted (both codes). AC8 table present. AC9 build 0/0. Skill/code difference list (9 items) and the BE-30 open items present. `graphify` not installed (recorded). Scratch folder gone; no `web/` change; one staged rename. AC10 developer part ran as two SignalR guest clients — within P3's "as far as the session can". |
| 18 — AC10 two-browser walkthrough | N/A (operator-owned) | `OPERATOR ACTION REQUIRED` list is in `implementation.md` Step 17; AC10 stays open in `summary.md` until recorded. |

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist — no `spec-rules.md`; the plan's `Satisfies:` lines cite spec rows and AC numbers, each found in the spec (AC1–AC10 are the numbered items of `## Acceptance (checkable)`).

**Evidence:**
- Skill log: `.claude/audit/skill-invocations.log` in the worktree, scoped by token `developer:implement` (sessions `095dffcf…`, `a6e01b64…`); 21 developer entries. Every plan-mapped skill for Steps 1–15 is present in the window, plus the second skills the step blocks name (domain-patterns for 3, create-domain-event-handler for 4, service-registration for 10, domain-patterns for 15). Steps 16–17 map `None`. `## Skills Used` matches the log. The main checkout's log holds only an Oct 13 entry from another feature.
- Test entry: one `test-entry` line in the window (2026-10-23T10:26:52Z, raw `dotnet test … --filter`), `blocked: true` — a corrected attempt, no slip. The main checkout's only `test-entry` line is F33's, Oct 12.
- Complete run: `mvl2pwti-bc55eeb1`, profile `complete`, slug `F38-BackendConventions`, verdict `pass` (48 s). The earlier `mvl2ohcm-afc6e6b6` red was the flaky integration test that passed on the unchanged re-run; the Step 1 carry-over row is the same flake class.

**Notes:**
- Plan hygiene (architect's own artifact): `## Decisions` and `## Decisions and why` present and non-silent. P4 and Step 3 say the one `OwnerId` comparison lives in `SprintAccess`, while Step 5 keeps the anonymous `GetSprintSummary` out of `SprintAccess`; its `team.OwnerId == userId` (`GetSprintSummaryEndpoint.cs:26`) therefore stays. Spec AC3's grep (`OwnerId != `) holds. The developer followed the plan; the contradiction is the plan's, and it is already a reviewer carry-over row.
- Step 5's `grep -rn "limit_reached:\|duplicate_name:" src` also hits `Reflekt.API/wwwroot/assets/index-*.js`, the gitignored SPA bundle the complete run's `npm run build` writes (it still parses the prefixes; F39 owns that). No source or tracked file matches.
- Boundary note: the one staged rename (Step 15) is the only git write found; the dispatch sanctions it.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-23*

### Check 2 — 2026-10-23 — after fix cycle 1

**Model:** claude-opus-5-5[1m]
**Re-checks:** check 1

Check 1 failed nothing. This check scores what the dispatch names: Step 4, redone to the amended plan (P7/P8 on questions.md Q2/Q3), and the plan change itself. The window starts at `Fix dispatched: 2026-10-23T14:47:37Z`.

| Step | Disposition | Notes |
|------|-------------|-------|
| 4 — Event publication logs every handler failure (amended: the save and the commit await the batch) | Superseded | The plan was amended mid-implementation, with the record in questions.md Q2/Q3 (both Answered by the architect) and the plan's `## Plan Review` "Fix round 1 amendment". The redo is in `implementation.md` § Fix Round 1. Re-verified in source: `Task.Run`, `PublishDetached` and `Mode.WaitForNone` → no hits. `SavedChangesAsync` is `async` and awaits `PublishInOrderAsync(events)` (interceptor line 65). `TransactionCommittedAsync` returns `PublishInOrderAsync(events)` (line 106). Unchanged: the collected order, `Mode.WaitForAll` (167), `SuppressThrowing` (134), per-handler `LogError` (157, 161), `CancellationToken.None` (144), and the rollback/failed callbacks clearing `_heldEvents` (114, 123). Handler-scope accept → no hits. Code traces (a)–(e) are in the fix-round block. P8 needs no code change: the three handlers are unchanged, and their reads now run before the response. |

**Plan change, scored against the step blocks:**
- **Accept lines hold:** P7/P8, the amended Step 4 and the five changed Decisions rows agree with the fix-round block. Step 4's new accept line (`Task.Run` → no hits) and trace (e) hold. The spec's row "Hub, timer, events" and behaviour change 2 carry the "amended 2026-10-23" marks the Q2 answer names.
- **Stale records:** two Step 17 records still describe the detached task:
  - AC8's "Logged handler failure, request unaffected" row says `SavedChangesAsync` / `TransactionCommittedAsync` "never hold the task".
  - The F40 skill/code list item 3 says the same; the developer flagged that item in the fix-round Deviations.
  
  The fix-round block supersedes both. This is a note for the close and F40, not a fail.
- **Round edits outside Step 4 (fix list rows 3–5, the review's items):**
  - `SeedOptions` moved to `Reflekt.Persistence`, and `Reflekt.API/SeedOptions.cs` (a Step 15 file) is deleted.
  - `Blocks.Persistence` dropped its `Blocks.Core` reference, and `Reflekt.API.csproj` references `Blocks.Core`/`Blocks.Exceptions` directly.
  
  Step 15's accept still holds: `ValidateOnStart` → 4, and no `"Seed:` configuration read is left in `src/Services/Reflekt`. AC4 still holds.

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist — no `spec-rules.md`.

**Evidence (window from 2026-10-23T14:47:37Z):**
- **Skill log:** worktree `.claude/audit/skill-invocations.log`, token `developer:implement`, session `446e0f35…`. It shows `service-registration` (14:49:47, row 3) and `persistence-patterns` (14:56:47, the interceptor). Step 4's mapped skill is in the window. `create-domain-event-handler` was not needed: no handler changed in the round. No skill slip.
- **Test entry:** no `test-entry` line in the window in either home (worktree or main checkout). No test slip.
- **Runs:**
  - Integration role `mvl3fprl-da6b27e6`: `pass`, slug F38, worktree tree. It ran the whole project, which covers the Q2 ruling's `Vote`/`Card`/`Sprint`/`EvidenceLink` files.
  - Fast `mvl3gt0s-8bb944ee`: `pass`. It is the tied call over every file the round changed.
  - Fast `mvl36wse-258e50a6`: `pass` (rows 3–4).
- **Git:** the index still holds only Step 15's sanctioned rename. No `web/` change.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-23*
