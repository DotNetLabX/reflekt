# F36-FrontendDefects — spec review, round 1 — Codex (primary)

**Model:** codex
Persisted by the architect session from the read-only Codex job, 2026-10-22.

## Verdict: NO-GO

1. **HIGH — [spec.md:32](docs/specs/F36-FrontendDefects/definition/spec.md:32): The stated permission rule does not match current requests.** `useRetroRoom.ts:90–91` obtains a participant token for Google users, and `stores/card.ts` sends that token alongside the cookie. Both card mutation endpoints check participant ownership first; the team-owner branch runs only without a participant claim. Consequently, replacing `authStore.user !== null` with `isScrumMaster` still exposes actions the server rejects. The endpoints also enforce phase, closed-state, and Discussing/action-item authentication restrictions. Resolve this against the API-preservation constraint and F38’s promise to preserve authorization behavior.

2. **HIGH — [spec.md:56](docs/specs/F36-FrontendDefects/definition/spec.md:56): Acceptance does not verify most required UX outcomes.** No check exercises failed note/action-item saves, create/delete failures, fetch failures, cold-route authentication, permissions, corrected navigation, or single submission on Enter. `.claude/verify.json` runs backend tests and a frontend build; neither executes these interactions. Require recorded manual behavioral checks, including HTTP and network failures, preserved text/assignee/form values, successful retries, and actual requests under each relevant identity.

3. **MEDIUM — [spec.md:30](docs/specs/F36-FrontendDefects/definition/spec.md:30): FE-08’s store inventory leaves existing failure paths uncovered.** `vote.ts:30–37` silently treats failed vote loading as no votes; `previousActions.ts:35–60` clears items on HTTP failure and has no error state; both report loaders have `try/finally` without a network-error catch. These actions participate in room/report initialization. F39 does not explicitly own the remaining FE-08 work. Include these paths or explicitly defer them and narrow the claimed outcome.

4. **MEDIUM — [spec.md:31](docs/specs/F36-FrontendDefects/definition/spec.md:31): Loading teams only in `SprintListView` leaves two cited deep-link cases broken.** `AppHeader.vue:80–86` reads team names on `team-sprints`, `sprint`, and `report`. Neither `SprintView` nor `ReportView` loads teams on a cold entry. Require initialization covering all three routes, or explicitly identify the remaining breadcrumb defect as deferred.

5. **MEDIUM — [spec.md:31](docs/specs/F36-FrontendDefects/definition/spec.md:31): Blanket `requiresAuth` removes existing guest-report access.** The router’s guard recognizes Google sign-in, while `ReportView.vue:42` deliberately loads a guest token and `GetSprintReportEndpoint.cs:29–37` authorizes matching participants. `docs/features/Report.md:84,97` also specifies guest access. Distinguish the admin sprint route from the participant-authorized report route and state the intended guest behavior.

6. **MEDIUM — [spec.md:34](docs/specs/F36-FrontendDefects/definition/spec.md:34): The prescribed attribute fix can violate the unchanged-layout requirement.** Three note inputs in `RetroBoardPanel.vue:68,102,136` pass `class="flex-1"`. Their `BaseInput` wrapper is the flex item. Moving every attribute exclusively to the inner input removes that wrapper sizing. Require preservation of the wrapper’s layout contract, with a narrow/mobile layout check alongside the single-event check.

7. **MEDIUM — [spec.md:18](docs/specs/F36-FrontendDefects/definition/spec.md:18): FE-05’s inherited documentation work conflicts with the other specs.** The cited suggested fix explicitly updates both `vue.md` and the `tailwind-theme` skill. Their current instructions still prescribe the defective inline body background. F39 and F40 preserve project skills, while F40 does not explicitly assign this Vue convention correction. Clarify ownership of the convention update and explicitly replace the inherited skill-edit requirement with a documented deviation.

8. **MEDIUM — [spec.md:27](docs/specs/F36-FrontendDefects/definition/spec.md:27): WebAudio generation alone does not establish audible expiry behavior.** Expiry arrives through a store watcher driven by SignalR, outside a user gesture. A newly created audio context can remain suspended under autoplay restrictions. Specify the initialization/resume behavior and verify audible unmuted expiry in a participant browser, plus muted expiry. Acceptance item 4 also passes if sound playback is simply deleted.

9. **MEDIUM — [spec.md:42](docs/specs/F36-FrontendDefects/definition/spec.md:42): The visual walkthrough lacks the states needed to expose the defects.** A generic board view need not contain a focused Discussing group, drag highlight, join avatar, mobile tabs, or all eight column colors. Welcome and Sprint List—the other body-background failures—are omitted. A settled-page inspection cannot prove pre-paint flash prevention, and one generated tint class cannot prove the full map. Name the required states, viewports, colors, and cold-load checks.

10. **LOW — [spec.md:60](docs/specs/F36-FrontendDefects/definition/spec.md:60): The build command has no valid stated working directory.** There is no root `package.json`; the surrounding search paths assume the repository root. Use `npm --prefix web/reflekt-web run build`, or explicitly separate working directories. The stated output path agrees with `vite.config.ts:32`.

11. **LOW — [spec.md:22](docs/specs/F36-FrontendDefects/definition/spec.md:22): The radius count and “no corners anywhere” claim are inaccurate.** Current source contains **49 radius occurrences in 18 files**. The audit’s 50 occurrences/19 files include the separate FE-11 maximum-width usage. Existing valid utilities, including `TeamCard.vue`’s `rounded-lg`, already produce rounded corners.

12. **LOW — [spec.md:29](docs/specs/F36-FrontendDefects/definition/spec.md:29): Delete-team state is not currently cleared before the request.** `DeleteTeamDialog.vue:95–102` prematurely resets `busy`, but preserves the confirmation text; `TeamDashboardView.vue:112–114` closes the dialog only after successful deletion. Describe its actual defects as missing failure feedback and ineffective pending-state handling.

No files were modified. Builds and tests were not executed because they write files.

---

# Round 2 — 2026-10-23 — Codex (primary, alone)

## Verdict: NO-GO

| Round-1 id | Status | Line | Collateral |
|---|---|---|---|
| R1-01 | closed | F36:33, 41 | none |
| R1-02 | partial | F36:72 — MEDIUM: checks network failures, but does not exercise HTTP failures, successful retries, or successful single submission on Enter | none |
| R1-03 | closed | F36:32, 42, 55–56 | none |
| R1-04 | closed | F36:31 | none |
| R1-05 | closed | F36:32 | none |
| R1-06 | closed | F36:35 | none |
| R1-07 | closed | F36:25, 43; F40:30–31 | none |
| R1-08 | closed | F36:28, 44, 67, 75 | none |
| R1-09 | closed | F36:26, 70–71 | none |
| R1-10 | closed | F36:26–27 | none |
| R1-11 | closed | F36:29 | none |
| R1-12 | closed | F36:23 | none |
| R1-13 | closed | F36:58, 66 | none |
| R1-14 | closed | F36:30 | none |
| R1-15 | closed | F36:24, 30 | none |
| R1-16 | closed | F39:51 | none |

No new MEDIUM-or-above findings. No additional contradictions with the checked sibling text or breaks to earlier guarantees found.

---

# Plan review — round 1 — 2026-10-23 — Codex primary (job task-mvkfu3j3-o8f0dg), persisted by team-lead session d941cd66

## Verdict: NO-GO

1. **HIGH — [plan.md:94](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:94), Step 4 / Decision 1:** FE-08 explicitly requires clearing target data on every fetch start. The hub callers justify considering a background-refresh exception for cards/groups; they do not justify retaining data across every same-target load. This also breaks Step 6’s report mechanism: `ReportView.vue:2` renders its error slot only when `report` is absent, so a failed same-sprint reload with retained data hides the error and Retry. **Suggested fix:** Restore the required foreground-load contract. Reconcile any narrowly scoped background exception explicitly, and make report errors take precedence over retained content.

2. **HIGH — [plan.md:136](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:136), Step 7 / Decision 2:** Background fetch failures remain invisible, with no retry. For example, a failed `fetchGroups` after `GroupDissolved` leaves obsolete groups displayed without recovery controls. FE-08 requires the consuming views to surface errors with Retry; it does not limit that requirement to entry. **Suggested fix:** Provide visible error/retry handling for background failures too. Preserving the board need not mean hiding the failure.

3. **HIGH — [plan.md:97](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:97), Steps 4–5:** Tracking the data’s target and clearing at request start does not prevent an older response overwriting a newer target. `SprintListView.vue:27` launches another fetch whenever `teamId` changes, without cancelling the preceding request. Group writes and hub events also overlap `fetchGroups` calls, contradicting the plan’s no-concurrent-load premise. **Suggested fix:** Specify request/target guards for committing data, errors, and loading completion, with independent status where different resources share a store.

4. **HIGH — [plan.md:135](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:135), Step 7:** One error scan “after the loads” does not establish safe bootstrap ordering. The existing composable fetches the summary before authentication/join and again inside `enterRoom`; the second call clears the first call’s error. Google join failures can also reach the preserved join-form catch before prerequisite failures are surfaced. Existing room `loading` becomes false only on successful entry. **Suggested fix:** Define explicit stages: check policy/summary before identity or join, check room loads before votes/connect, and finalize loading on every failure. Retry must clear the room error, prevent overlapping attempts, and resume loading without unnecessarily repeating a successful join.

5. **HIGH — [plan.md:193](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:193), Step 13 / Decision 9:** The operator fallback treats unperformed walkthrough checks as a valid completion deviation. Spec AC6 requires a recorded walkthrough with observations for every item. This can also leave Step 2’s conditional dark-theme decisions unverified. **Suggested fix:** Permit an operator handoff, but keep AC6 and delivery acceptance pending until the missing observations are recorded.

6. **MEDIUM — [plan.md:112](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:112), Step 5 / Decision 7:** `participant.fetchMe` is excluded from the fetch contract even though FE-08 covers every fetch in that store. It currently leaves `currentParticipant` untouched on failure and exposes no loading/error state. Preserving its status-bearing exception does not require omitting those updates. **Suggested fix:** Apply the required data/error/loading lifecycle while retaining the status information needed for the room’s 401 handling.

7. **MEDIUM — [plan.md:135](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:135), Steps 6–7:** The room’s reporting fetch is missing from the recovery plan. `useRetroRoom.ts:63` starts `fetchParticipantReport` after connection, and `RetroRoomView.vue:255` starts it on a phase transition. `RetroReportPanel.vue:4` shows its error but has no Retry; the plan changes that panel only for corners. **Suggested fix:** Add report-specific retry handling for both paths, preserving the existing connection when retrying the report.

8. **MEDIUM — [plan.md:145](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:145), missing `Adds:` rows:** No step contains an `Adds:` row. Step 8 introduces unconditional focus restoration, which can steal focus after the user moves to another column; FE-07 does not request that behavior. Step 13 introduces the unsupported acceptance fallback, and Step 2 adds an unknown-colour fallback. Decision prose does not satisfy the requested rows. **Suggested fix:** Audit every step and add its required `Adds:` disclosure. Remove unsupported behavior under the campaign constraint; bound any focus restoration to preserving the initiating keyboard interaction.

9. **MEDIUM — [plan.md:27](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:27), missing `Departs from:` rows:** The plan declares patterns to follow but records no departure rows. Concrete departures include retaining `@apply` in Step 1 despite `tailwind-theme` prohibiting it; replacing that skill’s inline-body prescription in Step 2; retaining Options stores and throwing actions in Steps 4–5 despite `pinia-patterns`; and extending Step 6’s cited Page Shell Pattern, whose actual template is initializing → empty → content. **Suggested fix:** Add explicit `Departs from:` rows identifying each adaptation and its governing spec requirement. Preserve F36’s scope rather than refactoring merely to match the guides.

No files were modified.

---

# Plan review — round 2 — 2026-10-23 — Codex primary (job task-mvkgel70-dldqs7), persisted by team-lead session d941cd66

## Verdict: NO-GO

Table lines refer to [plan.md](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md). Collateral identifies a fold-created contradiction or broken guarantee; `none` means none found.

| Round-1 ID | Fold status | Plan lines | Collateral |
|---|---|---|---|
| R1-01 | closed | 96, 122, 127, 247 | R2-01 |
| R1-02 | closed | 143, 248 | R2-01 |
| R1-03 | closed | 95, 98, 99 | none |
| R1-04 | closed | 136, 137, 138, 139, 141 | none |
| R1-05 | closed | 202, 211, 212, 255 | none |
| R1-06 | closed | 113, 138, 253 | none |
| R1-07 | closed | 144 | none |
| R1-08 | partial | 78, 154, 202, 249, 285 | R2-03 |
| R1-09 | closed | 254, 256, 257, 258, 259 | none |
| R1-10 | closed | 181, 184 | none |
| R1-11 | closed | 154 | none |
| R1-12 | closed | 105, 117 | R2-04 |

The background-failure banner and room-report Retry implement FE-08’s required error recovery. The current [spec.md:31](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/definition/spec.md:31) explicitly requires the former and covers the latter through the retro room’s error-with-retry requirement. Neither needs an `Adds:` row.

1. **R2-01 — MEDIUM — [plan.md:112](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:112): The background-refresh inventory and pinned diff limit omit two call sites.**  
   `useSprintHub.ts` contains eight calls: lines 67, 68, 70, 71, 108, 112, 116 and 120. Steps 5 and the invariant at line 229 repeatedly limit changes to six. Following that limit leaves foreground loaders in hub handlers; their new clear-on-start behavior contradicts FE-08’s background exception and the “never blanks the board” guarantee at line 232.  
   **Suggested fix:** Require the background option at all eight existing call sites and reconcile the acceptance check and invariant.  
   **Tag:** `fold`

2. **R2-02 — MEDIUM — [plan.md:126](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:126): Route-parameter changes bypass the proposed stale-data protection.**  
   `SprintView.vue:30`, `ReportView.vue:31` and `RetroRoomView.vue:121` capture IDs once; their loaders run on mount, and `App.vue:3` has an unkeyed `RouterView`. Vue Router [reuses component instances when parameters change](https://router.vuejs.org/guide/essentials/dynamic-matching.html#reacting-to-params-changes). Consequently, navigation between IDs within one route can retain the previous target without issuing another fetch. Request counters cannot protect a request that never starts; Retry also retains the captured IDs. This leaves the plan’s “never another target’s data” outcome unmet.  
   **Suggested fix:** Specify target-dependent reloads or remounting for these views, including room teardown before initializing another sprint.  
   **Tag:** `round-1 miss: route-parameter lifecycle`

3. **R2-03 — MEDIUM — [plan.md:249](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:249): Disclosing focus restoration does not resolve the campaign constraint.**  
   The fold bounds focus restoration and adds its disclosure, but still marks the addition `decided`; Step 8 conditionally builds it and P3 leaves an owner keep/drop choice. The plan itself acknowledges that no source requests this behavior. That conflicts with the binding “no new product behaviour” rule at line 17. R1-08 is therefore only partially closed.  
   **Suggested fix:** Remove the optional focus-restoration work and reconcile Decision 3, P3 and the closure claim with the campaign scope.  
   **Tag:** `fold`

4. **R2-04 — MEDIUM — [plan.md:105](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:105): The strengthened acceptance check contradicts the auth exception immediately above it.**  
   Line 105 requires every listed load action’s non-OK branch to set `error`, including `auth.fetchMe`. Line 104 requires HTTP failures to preserve signed-out behavior with no message. Applying the acceptance rule to a normal unauthorized `/auth/me` response would make Step 6 display the unreachable-server notice during ordinary guest access.  
   **Suggested fix:** Explicitly exempt auth HTTP responses from that acceptance rule; require auth’s notice-producing error only for network failures.  
   **Tag:** `fold`

Read-only review completed; no files modified and no builds or tests run.
