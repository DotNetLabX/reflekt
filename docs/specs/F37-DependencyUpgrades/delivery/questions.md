# F37-DependencyUpgrades — Questions

## Q1 — The npm rows touch a shared file (`web/reflekt-web/package-lock.json`)

**From:** team-lead · **To:** shared architect (campaign) · **Status:** Answered · **Asked:** 2026-10-23

The spec's DP-08…DP-12 rows require `npm audit fix` in `web/reflekt-web`, which rewrites `web/reflekt-web/package-lock.json`. The campaign marks that file as shared: a unit never edits it and writes a change note instead.

Options:
1. **(Recommended)** The unit does not touch the lockfile. It writes a change note in `delivery/change-notes.md` (target file, the exact command `npm --prefix web/reflekt-web audit fix` with no `--force` / `--legacy-peer-deps` / `overrides`, the expected outcome `vue` ≥ 3.5.42 and `npm audit --omit=dev` = 0), and records the current `npm audit` output. Acceptance items 4 and the lockfile part of 7 are discharged at the campaign merge, not in this unit. Confidence: high — it is what the campaign's shared-file rule says.
2. The unit runs `npm audit fix` and commits the lockfile anyway. Confidence: low — breaks the shared-file rule.

Default if unanswered in 30 minutes: option 1, recorded presumed (proceed-default).

**Answer (AR-1, shared architect of the campaign — not the owner; Owner to rule: no), 2026-10-23:** Option 1. Do not edit `web/reflekt-web/package.json` or `package-lock.json`. Write a change note in `delivery/change-notes.md`:
- Target: `web/reflekt-web/package-lock.json` only. Command: `npm --prefix web/reflekt-web audit fix` (no `--force`, no `--legacy-peer-deps`, no `overrides`).
- Expected result: `vue` >= 3.5.42 in the lockfile (`package.json` keeps `"vue": "^3.5.32"`, and 3.5.42 is inside that range); `npm --prefix web/reflekt-web audit --omit=dev` reports 0; `git diff --name-only` after the command shows only `package-lock.json`. If `package.json` also changes, whoever applies the note records it.
- The current plain `npm audit` output is the "before" state; the "after" output is recorded when the note is applied.
- Acceptance 4 and the lockfile part of acceptance 7 are marked "owed at merge", not Missing.
- F37's note is applied before every other unit's note on these files (F39's comes after it).

## Q2: Who runs the whole-solution tests at the Microsoft.* step
**From:** architect
**To:** team-lead
**Status:** Answered
**Step:** Phase 1 analysis (plan Step 2 — Microsoft.* 9.0.4 -> 10.0.x)
**File:** `.claude/verify.json`

**Context:** The spec makes the Microsoft.* step run `dotnet test Reflekt.slnx`, and the owner's standing rule puts that upgrade behind the full suite. The declared `unit` role (the step close, `verify-run.js --profile fast`) runs only `tests/Reflekt.UnitTests` — the integration tests, where `Database.Migrate()` proves the bump changed no model, are not in it. Developers run tests only through `verify-run.js`; the `full` role (`dotnet test Reflekt.slnx && npm ci && npm run build`) measured 68 s on 2026-10-13, well inside a subagent's limits, and `npm ci` leaves the shared lockfile untouched.

**Question:** At the close of plan Step 2, should the developer run the full role itself, or hand back so the coordinating session runs it before Step 3?

**Recommendation:**
1. (Recommended) The developer runs `node {scripts}/verify-run.js --role full --slug F37-DependencyUpgrades` at Step 2's close and cites the record; a red that is not a compile error stops the step and is handed back as a blocker (spec Decisions row 4). One extra full run before the handover's complete run; no extra hand-back.
2. The developer hands back after Step 2 with "full run owed"; the coordinating session runs `--profile complete`, then resumes the developer for Steps 3–5. Keeps full runs in the main session, at the cost of one hand-back and resume.
**Confidence:** medium — option 1 is cheap and within limits, but whether a developer seat may run the `full` role is the coordinator's process call.

### Answer

**Answered by team-lead (coordinator's process call — not the owner), 2026-10-23:** Option 1. At Step 2's close the developer runs `verify-run.js --role full --slug F37-DependencyUpgrades` and cites the record; a red that is not a compile error stops the step and is handed back as a blocker. The full run stays well inside a subagent's limits (68 s measured).


## Q3: Step 2's full run went red once, then green on re-run
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 2 — The Microsoft.* family on one 10.0.x patch
**File:** `tests/Reflekt.IntegrationTests/CardEndpointTests.cs`

**Context:** After the bump to 10.0.13 (build clean, no source change), full run `mvkgn2o8-461c60a6` failed 1 of 104 integration tests: `CardEndpointTests.CreateCard_ActionItem_Returns201`, thrown at host start (`InitializeAsync`, line 78, `_factory.CreateClient()` → `WebApplicationFactory.StartServer()`). The runner's record elides the exception message. One diagnostic re-run on the same tree, `mvkgpdv7-eb5d9956`, passed in full. The plan says a red that is not a compile error stops the step and is handed back; it does not say how to treat a red that does not repeat. Details: `implementation.md` § Step 2.

**Question:** Does Step 2 close on the green re-run (`mvkgpdv7-eb5d9956`), with the one-off red recorded as an intermittent host-start failure for the reviewer, or does the step stay stopped until the cause is found?

**Recommendation:**
1. (Recommended) Close Step 2 on the green re-run; keep the red recorded in the step block and as a carry-over for the reviewer; the handover's complete run is a second chance to see it again. Only one test of 104, at host start, and every other host in the same class started the same way — not the model-drift error the rule was written for.
2. Keep Step 2 stopped: hand the failure to a solo to find the cause (and whether it happens on 9.0.4) before Step 3.
**Confidence:** medium — the evidence points to a flake, but the exception message was not captured, so a bump-caused start-up race cannot be ruled out.

### Answer
**Architect, 2026-10-23 — option 1, with a tripwire.** Step 2 closes on the green re-run `mvkgpdv7-eb5d9956`, under the plugin's flaky rule: a red that passes when re-run unchanged, on a quiet machine (`others: 0` on both records), is flaky. It is listed, not fixed in this run, and counts as no attempt.

Why this is not the failure the stop rule guards: everything a framework bump could break at host start is deterministic on an unchanged tree. That covers a pending-model-changes error from `Database.Migrate()` in `UseReflektPersistence`, a removed API, and a changed configuration binding. Any of them would fail every factory start in all eight integration classes on both runs. Here 103 of 104 passed, and then 104 of 104 passed on the same `tree_hash` (`4dcd021f…`). What remains is a one-off at `CardEndpointTests.InitializeAsync`, line 78. That class starts two hosts on SQLite files under `Path.GetTempPath()`, with xUnit running the test classes in parallel: an intermittent start-up race or file-lock shape that predates this feature. There are only five earlier full runs on 9.0.4 to compare with, so the base rate is unknown.

What the developer does:
1. **Step 2 block in `implementation.md`:** record the flaky test as such. Give both run ids, the test name, the top stack frames (`WebApplicationFactory.StartServer`, from `CardEndpointTests.InitializeAsync`, line 78), and the line "exception message not captured (record excerpt elided)". Also add a carry-over row for the reviewer.
2. **`delivery/change-notes.md`:** add one row for `docs/backlog.md`, the shared file. It is a backlog row: flaky host start in `CardEndpointTests.InitializeAsync`, seen once on 10.0.13 after F37's bump, cause not found; run ids as above.
3. **Then continue to Step 3.**

**Tripwire:** if the same host-start failure shows up again in any later full run of this unit — Step 3's re-run if a pin is added, or the handover's complete run — it is no longer a one-off. It then goes to a solo on a `Bug found:` line, to capture the exception message and say whether it also happens on the base commit's 9.0.4. No solo now.

Plan updated: Step 2 Accept now states this rule.
**Confidence:** medium — the deterministic bump failures are ruled out by the identical green re-run; a rare bump-caused race cannot be, since the message was not captured. The tripwire covers that case.
