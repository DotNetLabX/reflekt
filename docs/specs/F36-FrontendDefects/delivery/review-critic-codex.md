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
