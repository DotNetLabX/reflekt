# F36 — Frontend Visual & UX Defects — Done Check

## Step 1 — Done-Check

### Check 1 — 2026-10-23 — after the build

**Model:** claude-opus-5-5[1m]

Pre-commitment predictions: (1) a mapped skill missing from the skill log in a later build slice; (2) a raw test command (`npm run type-check` / `dotnet test`) on the developer seat; (3) Step 13 recording AC6 items as seen that this session cannot observe. Found: none of the three.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Corners and report width use the theme's utilities | Implemented | AC1 grep re-run: no hits. `max-w-6xl` on `PageLayout`; built CSS `border-radius:var(--radius-md)` recorded. 19 files, all named by the plan. |
| 2 — Light page background, focused-group surface, card tints | Implemented | AC2 re-run: `<body>` bare (line 25), `html` / `html.light` in `<head>` (lines 12–13). `--surface-focus` token, literal tint map; built CSS holds all eight `bg-col-*-light/30` plus `bg-action-items-light/30`. Skill departure is plan Decision 11. |
| 3 — `BaseInput` binds attributes once | Implemented | `inheritAttrs: false` at `BaseInput.vue:20`; `class`/`style` on the wrapper. Single-Enter observed in the Step 13 walkthrough. |
| 4 — Load contract: `sprint`, `teams`, `report`, `auth` | Implemented | Per-resource status, clear on start, never throws, latest-request counters; `auth.fetchMe` shares one in-flight request (developer decision, reasoned: router guard + App both await it). Store `fetch(` lines re-diffed: identical after indentation (Must-NOT-Change holds). Departure is plan Decision 12. |
| 5 — Load contract: room stores, room refreshes | Implemented | `useSprintHub.ts`: 8 `fetchCards`/`fetchGroups` hits, each `{ background: true }`; diff 8+/8−. Five post-write reloads pass background. The Step 5→7 interim gap (`participant.fetchMe` no longer throwing) was closed in Step 7. |
| 6 — Error states with retry; App notice | Implemented | Error branch before content in the four views; `<RouterView :key="route.path" />` at `App.vue:11`. `ReportView` keeps its error card and adds Retry — within the step's "the message gains Retry". Departure is plan Decision 13. |
| 7 — Retro room staged entry, refresh banner, report retry | Implemented | Three checked stages with resume-at-failed-stage retry; refresh banner on `refreshError`; `RetroReportPanel` Retry emit. Stage-1/stage-3 failures and the one-hub-connection retry observed in the Step 13 walkthrough. |
| 8 — Inputs keep text on a failed save | Implemented | `addNote`/`addActionItem` return `Promise<boolean>`; clears only on `true` (`RetroBoardPanel.vue:270`, `:280`); no focus handling (plan Decision 3 / P4). Offline and 500 note cases observed. |
| 9 — Edit/delete only where the server allows | Implemented | `grep "authStore.user !== null"` re-run: no hits; four checks in the endpoints' order; one flag drives both buttons (Decision 4). |
| 10 — Create-sprint and delete-team forms own their calls | Implemented | No `emit('create'` / `emit('confirm'`; `handleCreateSprint` / `handleDeleteConfirm` gone; `teamId` prop (Decision 8). |
| 11 — Team name on cold entry, guarded admin view, back link | Implemented | Router diff is the one `requiresAuth: true` on `sprint`; `team-sprints` link at `SprintDetailPanel.vue:3`; header watch guarded by signed-in / empty / not loading / no error. |
| 12 — Audible expiry chime | Implemented | AC4 re-run: no `BASE64_CHIME` / `data:audio`; `AudioContext` at `useTimer.ts:14,17,18`; room-lifetime unlock listeners. |
| 13 — Verify: build, greps, walkthrough, graph update | Deviated (valid reason) | type-check, build, AC1–AC5 recorded green. The walkthrough was driven through CDP in headless Chrome, with failures injected per request (`Fetch.failRequest` / `fulfillRequest` 500) so the SPA itself still loads — the mechanism the plan names ("devtools request blocking/overriding"), driven by script. Guest flows recorded as seen; every item needing Google sign-in, phase moves, a hub refresh or hearing audio is listed under OPERATOR ACTION REQUIRED (items 1–9), never recorded as seen. `graphify` not installed — recorded, as the step allows (operator item 9). |
| 14 — Operator completes the AC6 walkthrough | N/A (operator-owned) | Not yet performed. AC6 stays open: the close lists it as open acceptance and the change note reads "built, AC6 walkthrough pending". |

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist (no rule list — spec `Rule list: not applicable`).

Notes:
- **Skill log** (`.claude/audit/skill-invocations.log`, worktree home; token `developer:implement`, session `d941cd66…`): every non-`None` step's mapped skill is logged inside that step's hand-off window — tailwind-theme ×2 (Steps 1–2), vue-patterns (3), pinia-patterns ×2 (4–5), create-vue-feature (6), vue-patterns (7, 8, 9), pinia-patterns (10), vue-patterns (11, 12). Step 13 is `Skill: None`. `## Skills Used` agrees with the log. Main-checkout home holds no entries in the window.
- **Test entry:** no `test-entry` line in either home's `violations.log` in the developer window; every step close went through `verify-run.js`.
- **Complete run:** `verify-verdict.json` holds `profile: complete`, `verdict: pass`, slug F36-FrontendDefects, 2026-10-23T05:07:40Z (the handover run the brief names).
- **`Satisfies:`** referents all exist: FE ids in the spec's scope table; AC1–AC7 are the spec's numbered "Acceptance (checkable)" items 1–7.
- **Plan hygiene:** `## Decisions` (14 rows) and `## Decisions and why` (P1–P4) present and non-silent.
- **Scope:** the changed files are exactly those the plan's steps name, plus this feature's delivery docs; walkthrough scratch lives untracked under `.claude/tmp/walk/` and must not be staged at the close.
- **Operator-owed (open production gate):** AC6 is not met until Step 14's `## Walkthrough — operator` block is recorded — the verdict below is the build's conformance, not AC6 acceptance.
- **For the code review, not a disposition here:** Step 7 treats a failed hub `connect` as a stage-3 room error with Retry (before: an unhandled rejection), and `ReportView` shows Retry on 401/403 too (developer carry-over row). Whether either is new product behaviour under the campaign directive is the reviewer's call.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-23*
