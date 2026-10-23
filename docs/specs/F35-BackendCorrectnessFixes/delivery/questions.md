# F35 — Backend Correctness & Security Fixes — Questions

## Q1: New tests for the fixes
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** This run's instructions say to write no new tests and to change an existing test only where a fix changes what it asserts.
The backlog entry you confirmed for these six fixes says existing test classes may be extended with a regression case where a fix changes behaviour, and this fix's spec relies on that: most of its checks are new test cases, plus three small new test classes for the sign-in redirect rule, the link-fetch address check and the hiding of other people's cards.
The two rules cannot both hold, so the plan cannot be written until one wins.

**Question:** For the security and data fixes in this feature, should the plan add regression test cases?

**Recommendation:** Add regression cases, as the spec says (Recommended) — Each fix gets a test case in the existing test projects, with three new test classes where no class covers the area; no new test project, framework or package.
Add regression cases to existing test classes only — The same fixes are tested, but the three areas with no class get their cases placed in the nearest existing class instead.
No new test cases at all — Only the existing tests whose assertions the fixes change are updated; the open-redirect, link-fetch and card-hiding fixes ship with no test, checked only by code search and one recorded manual run.
**Confidence:** medium — the confirmed backlog wording and the spec agree on regression cases, but this run's instructions are your most recent words and say the opposite.

### Answer
**Answered by:** the owner's standing campaign directive, relayed by the campaign's runner (binds as an owner answer) — not a presumed default.
**Answer:** No new test cases at all. The directive reads: "Write no new tests; update an existing test only where a fix changes what it asserts." Only existing tests whose assertions a fix changes are updated (e.g. the `SprintTests` throw-type assertions). The fixes whose spec checks were new test cases are verified by code trace in the done check and code review, and by recorded manual/evidence runs where the spec names one. Where a spec acceptance line asks for a new test, the plan rewrites it as a code-trace or existing-test check and lists each such change in `## Decisions`.
**Date:** 2026-10-23

## Q2: How to run the plan's named integration test files per step
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Steps 1–5 (every **Runs:** line), Step 3 most of all
**File:** `tests/Reflekt.IntegrationTests/*.cs`

**Context:** The plan has each step run its integration files by name: `verify-run.js --profile fast --files tests/Reflekt.IntegrationTests/{File}.cs`. That form does not run them here. `.claude/verify.json` declares `slow.runner: vstest`, and no installed plugin provides a vstest runner: nexus-dotnet 2.0.7 in the plugin cache has no `hooks/scripts/test-runner.js`. In that case `verify-run.js` (lines 804–817) prints `--files: no installed plugin runs vstest tests — running roles.unit` and runs `roles.unit`, which is `Reflekt.UnitTests` only. The communication log's Runtime/Plugin Issues line records the same thing. The tied step close (`--tied --changed`) falls back to `roles.unit` the same way, so no integration test runs at any step of slice 1. Step 3 depends on those runs. It says "Run every integration test file", and the 400/500 → 409 and body-key-casing assertion updates it allows can only be found by running them. Without the runs, those updates would first show up at the handover complete run, as a red that needs a fix round.

**Question:** Which command should a step use to run its **Runs:** integration files?

**Recommendation:**
A (Recommended) — Run each step's named classes directly: `timeout 300 dotnet test tests/Reflekt.IntegrationTests/Reflekt.IntegrationTests.csproj --filter "FullyQualifiedName~Reflekt.IntegrationTests.{Class}"` (one filter, the classes joined with `|`). For Step 3 that is the whole integration project, less than the full role's 68 s measured in `.claude/verify.json`, which also includes the npm build. The output summary goes in the step block, marked as a plan-required run outside the runner. The tied `verify-run.js` step close still runs and is recorded as the step close. The reviewer reads it as sanctioned, not as a test slip.
B — The team lead adds an `integration` role to `.claude/verify.json` (the whole integration project), and the developer runs `verify-run.js --role integration --slug F35-BackendCorrectnessFixes` where a step names integration files. The run is recorded, but every call runs every class.
C — Skip the per-step integration runs, so the main-session handover complete run is the first to run them. Step 3's assertion updates then come in through a fix round.
**Confidence:** medium — A keeps the plan's intent and cost, but the agent rules call a raw test command a slip unless you sanction it; B is cleaner but needs a config change that isn't the developer's to make.

### Answer
**Answered by:** architect, 2026-10-23
**Answer:** B, with one change: the role is declared once and every step uses it. A cannot run: a raw `dotnet test` on the developer seat is refused by the test-entry gate (`hooks/scripts/test-entry-gate.js:113`), so it would not even be a sanctioned slip.
- The team lead adds one role to `.claude/verify.json`, before the slice-1 developer resumes: `"integration": "dotnet test tests/Reflekt.IntegrationTests/Reflekt.IntegrationTests.csproj"`. That is the integration half of today's `full` role, without the npm build. `verify-run.js` runs any declared role (`--role <name>`) under its 30-minute default limit.
- Every step with a **Runs:** line runs `node {verify-run.js} --role integration --slug F35-BackendCorrectnessFixes` once, after its `unit` step close. The step block reports the result for the files the **Runs:** line names. Step 3 reads the whole run for the 400/500 → 409 and key-casing assertions it may update.
- Step 6 still runs no integration files (they fail until Step 7's migration exists); Step 7's run covers Step 6's files.
- If the team lead cannot add the role, the fallback is C: the handover complete run is the first integration run, and Step 3's assertion updates come in through a fix round.
- Plan amended: the Implementation Steps preamble, Steps 7 and 14 accept and Runs lines, the Step 7 hand-written-migration fallback, Must-NOT-Change, Testing Strategy, the Step 14 file list (adds `.claude/verify.json`), and one Decisions row.
**Confidence:** high — the gate refuses A, and the runner already takes any declared role.

## Q3: `--role full` at Steps 7 and 14 run by a developer
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Steps 7 and 14 (slices 2 and 3; it does not block slice 1)
**File:** —

**Context:** Steps 7 and 14 tell the developer to run `verify-run.js --role full`. The developer's spawn contract says the full suite is the close gate's, run from the main session, and never a subagent's ("full: … (main session only)" in the delivered verify roles). Step 7 uses the full run to prove that the start-up `Migrate()` sees no pending model change. Step 14 uses it for spec AC10.

**Question:** Should the full-role runs at Steps 7 and 14 move to the main session?

**Recommendation:**
A (Recommended) — Yes. At Step 7 the developer runs the integration project (per Q2's answer), which also starts the host and runs `Migrate()`. Step 14's `--role full` becomes the close gate's complete run, and the developer records only that it is owed.
B — Keep them as written, and the team lead tells the slice-2 and slice-3 developers that the full role is sanctioned for them.
**Confidence:** high — the runner contract names the full suite main-session-only, and Q2's integration run covers Step 7's migration proof.

### Answer
**Answered by:** architect, 2026-10-23
**Answer:** A — yes.
- **Step 7:** the developer runs the `integration` role (Q2). Every integration test starts the host, whose start-up `Migrate()` rejects a pending model change, so a green run is the migration proof Step 7 needs.
- **Step 14:** spec AC10's full suite is the close gate's complete run, made by the team lead from the main session (as the team lead runs it at handover too). The developer runs the `integration` role on the final tree and records the complete run as owed.
- Plan amended: Steps 7 and 14 accept lines, and the preamble names the full role as the main session's.
**Confidence:** high — the full role is main-session only, and the integration run carries Step 7's proof.

## Q4: Step 11's broadcast handlers make the integration suite flaky at test teardown
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 11 — Card broadcasts: full to the author, redacted to the others
**File:** the 5 Step 11 handlers; `tests/Reflekt.IntegrationTests/*EndpointTests.cs` `DisposeAsync`

**Context:** Step 11 is built as the plan says. Each handler loads the card's sprint (and, in Writing, the active participants) through a scope it creates, and its greps and traces hold. Build and unit step close are green (runs `mvklgtcc-82b87629`, `mvklh5s7-a53be803`). The `integration` role is now intermittently red, with a failure this feature did not have before:
- With the Step 11 handlers: 4 runs, 2 red.
  - `mvklha2p-1e3e7b9f`: `EvidenceLinkEndpointTests.AttachLink_GenericUrl_Returns201`. Also the known host-start flake in `ParticipantEndpointTests.InitializeAsync`.
  - `mvklm8ji-40ce14b0`: `VoteEndpointTests.CastVote_WrongSprint_Returns401`.
  - Both new failures are the same: `System.IO.IOException : The process cannot access the file 'D:\temp\test-….db' because it is being used by another process.`, thrown at `File.Delete` in the test class's `DisposeAsync` (`VoteEndpointTests.cs:92`, `EvidenceLinkEndpointTests.cs:92`), after `_factory.DisposeAsync()` and `SqliteConnection.ClearAllPools()`.
  - Green: `mvkljqbb-6d8e9a2e`, `mvklks06-8b4ad9de`.
- Probe, one variable: the same tree with only the 5 handlers put back to `HEAD` (restored afterwards). 4 runs, 0 `IOException`. One red (`mvklr7rc-15e19198`) is the pre-existing host-start flake (F37 `change-notes.md` Note 1), in `SprintEndpointTests.InitializeAsync`.
- Steps 1–10: 10 integration runs, 0 `IOException`, 1 host-start flake.

Reading: the event bus publishes with `Mode.WaitForNone` (kept by plan decision). So each handler's new database read runs in the background, after the HTTP response. A test that creates or edits a card and ends straight away can reach its `DisposeAsync`, and delete the database file, while that read still holds it open. Production is unaffected; there the database file is never deleted. `BroadcastGroupCreatedHandler` already reads the database the same way, through an injected `CardRepository`, so the race existed before. The card events are far more frequent, which is what makes it show now. I found no fix inside the plan:
- The sprint's phase is needed for every card event, and it is not on the event.
- The handlers may not await inside the request.
- The owner's rule forbids test changes other than assertions.

**Question:** How should the teardown race that the Step 11 handlers expose be handled?

**Recommendation:**
B (Recommended) — Ask the owner for a teardown-only exception. Each integration class's `DisposeAsync` retries its `File.Delete` briefly (or ignores an `IOException` on the cleanup delete). No assertion, case or file is added, and the code stays as built. The suite is deterministic again for the handover and close runs.
A — Keep the code and the tests as they are. Record the `IOException` in `DisposeAsync` as a known flake, beside F37's host-start one. The handover and close complete runs are then red about half the time and get re-run; the teardown fix goes to F38.
C — Remove the database read from the handlers. The card events (`CardCreatedEvent`, `CardUpdatedEvent`, `EvidenceLinkAttachedEvent`, `EvidenceLinkDetachedEvent`, `EvidenceLinkFetchedEvent`) also carry the sprint's phase, set where they are raised. This changes Domain event shapes and their raising sites, outside Step 11's file list. It also departs from the root `CLAUDE.md` rule that events carry the aggregate reference.
D — Publish with `Mode.WaitForAll`, so handlers finish before the response. This reverses the plan's `Departs from domain-patterns` decision and changes request timing.
**Confidence:** medium — B is the smallest change that restores a deterministic suite, but it needs an exception to the owner's test rule. A keeps every rule but leaves the close gate's complete run flaky.

### Answer
**Answered by:** architect, 2026-10-23
**Answer:** A for this run, because it is the only option inside the owner's rules. B is listed for the owner as Q5; it is not asked now and blocks nothing.
- **Keep Step 11 as built.** The race is not a product defect. A background handler's read holds the test's database file while the test class deletes it in `DisposeAsync`. Production never deletes the file. And the race is older than F35: `BroadcastGroupCreatedHandler` reads the database the same way after the response.
  - C changes the domain event shapes and breaks the root `CLAUDE.md` rule that events carry the aggregate.
  - D changes request timing and reverses a kept `Departs from` row.
  - Both are product changes this cleanup may not make.
  - B changes a test file in a way that is not an assertion update, so it needs the owner's exception.
- **Shorten the window, inside Step 11's own files.** Each handler does its reads first, as few queries as it needs, untracked (`AsNoTracking` or the repository's read path). It disposes its scope before any SignalR send, so the connection goes back to the pool as early as it can. Outside Writing it reads only the sprint. This narrows the window; it does not close it.
- **Record it as a known flake.** The step block, and later `summary.md` Notes, record `IOException at File.Delete in an integration class's DisposeAsync` as a known teardown flake beside F37's host-start flake (F37 `change-notes.md` Note 1). It is listed and never fixed in this run. A red integration or complete run whose only failures are these two flakes is re-run, as the handover and close ladders already do for a file that passes when re-run by name.
- **For the team lead at close:** one change note for `docs/backlog.md`, a follow-up row: "Integration test teardown deletes the SQLite file while background event handlers may still hold it; make the teardown wait or retry." It goes to F38 or a solo once the owner rules on Q5.
- Plan amended: Step 11 accept, and Testing Strategy.
**Confidence:** medium — A keeps every rule but leaves the suite flaky about half the time until the owner rules on Q5; the handler-side shortening reduces that but cannot remove it.

## Q5: Let the integration tests' cleanup wait for the database file
**From:** architect
**To:** user
**Status:** Answered
**Step:** Step 11 (listed for the owner; nothing waits on it)

**Context:** The card-hiding fix makes background notifications read the database after each response, and the integration tests delete their database file as soon as a test class ends. About half of the integration runs now fail at that delete, though no test assertion fails. Your rule allows test changes only where a fix changes what a test checks, and this cleanup change checks nothing.

**Question:** May each integration test class retry deleting its database file briefly when cleaning up?

**Recommendation:**
Allow it (Recommended) — The integration runs go back to passing every time. No check, test case or test file is added, and only the cleanup line of each test class changes.
Keep the rule — The runs keep failing about half the time at cleanup, and each failure is re-run and listed as a known flake until a later cleanup feature fixes it.
**Confidence:** medium — the change checks nothing, but it is still a test-file change your standing rule does not cover.

### Answer
**Answered by:** the owner, relayed by the campaign coordinator (campaign directive d2, 2026-10-23 07:24 UTC).
**Answer:** Allow it — owner, 2026-10-23. Only the cleanup line of each test class changes; no new check, test case or test file.
