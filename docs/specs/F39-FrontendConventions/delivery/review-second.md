# F39-FrontendConventions — Second reader (Step 2, first review)

## Reviewed By
reviewer (second reader). Primary's `review.md` was not read or written.
**Model:** claude-sonnet-5-5

## Verdict: COMMENT

No CRITICAL or HIGH. Five LOW findings, none blocking. All 18 Carry-Over rows are addressed below. The change is a faithful, careful conformance pass: every behaviour path I diffed against `HEAD` came out equivalent, and the exceptions are the ones the developer already flagged.

## Pre-commitment Predictions
| Predicted problem area | What I found |
|---|---|
| Optimistic revert logic (pendingValues, card, vote, group, readiness) has an interleaving bug | Traced the in-flight pairs (two edits, edit-confirm-then-undo, out-of-order confirms, cast+cast with the first failing, failed focus across two groups, echo-before-response and response-before-echo for create). All land on the ratified rule. No defect. |
| A hub handler is not one call, or `connect`/`disconnect` changed beyond the sanctioned edit | All 26 listeners are one store call (`ParticipantLeft` empty, P5). `git show HEAD` diff of `connect`: only `build()` now returns the connection and `connect` holds `const built`; `disconnect` untouched. No defect. |
| A request lost its path/method/body/token in the API move | Compared every `fetch(` in `HEAD` stores and `PreviousActionsPanel` with `src/api/*.ts`: paths, methods, bodies (`'{}'` included), token opt-in all match; kick sends no token as before; clear-focus removed per Q3. No defect. |
| `BaseModal`/`BaseInput`/`BaseButton` rework regresses behaviour | `.number`/`.trim` modifiers still apply (the component `emit` runs them on `update:modelValue`); modal listener is added and removed on open/close/unmount; `RouterLink` branch keeps attrs. No defect. |
| Accessibility additions that overclaim | Found three (see F1, F5 and Open Question 2). |
| Skill/convention drift (size caps, `string` where a union exists, composable side effects) | Found three (F2, F3, F4). |

## Findings

### [LOW] Account menu is `role="menu"` but holds a non-menuitem child and has no menu keyboard model
**File:** `web/reflekt-web/src/components/AppHeader.vue:46-59`
**Origin:** implementation
**Issue:** The dropdown is `role="menu"` and its first child is a plain `div` showing the email; ARIA requires a menu's children to be `menuitem`/`group`/`separator`. A `role="menu"` also promises arrow-key navigation, which nothing implements (same for `TeamCard.vue:25` and the avatar `aria-haspopup="menu"`). FE-29/FE-30 asked for `aria-expanded`/`aria-haspopup`, not menu roles; the roles are the developer's addition and make the control worse for a screen-reader user than a plain disclosure button.
**Fix:** Either keep the disclosure pattern (`aria-expanded` on the trigger, drop `role="menu"`/`menuitem`, `aria-haspopup`) or keep the menu roles and give the email `role="presentation"`/move it out of the menu, plus Escape/arrow handling.
**Confidence:** 80/100

### [LOW] Colour props still typed `string` in five components though `ColumnColor` exists
**File:** `web/reflekt-web/src/components/retro/RetroGroupItem.vue:187`, `retro/RetroCardItem.vue:152`, `retro/RetroMobileTabBar.vue:32-36,46`, `reports/ReportSection.vue:47`, `reports/EvidenceChip.vue:19`
**Origin:** implementation
**Issue:** Spec Scope row (FE-15) says the domain unions are "used by the stores, props and phase logic". `RetroColumnPanel` and the composable use `ColumnColor`, but these five props take `string`, and `RetroCardItem` indexes a `Record<string, string>` with it (`cardTintClasses[props.columnColor]` is `string | undefined` for an unlisted key; the `'action-items'` entry is unreachable because the action column passes `'blue'`). Not caught by AC9's structure grep, which names only stores and phase logic.
**Fix:** Type them `ColumnColor` (and the tint map `Record<ColumnColor, string>`; drop the dead `'action-items'` key).
**Confidence:** 85/100

### [LOW] Three components are over the skill's 200-line split rule, two of them newly
**File:** `web/reflekt-web/src/components/retro/RetroGroupItem.vue` (301 lines, 186 at `HEAD`), `sprints/CreateSprintForm.vue` (281, 201), `retro/RetroCardItem.vue` (222, 138)
**Origin:** design
**Issue:** `vue-component-architecture` says "MUST split when a component exceeds 200 lines" and `frontend-review` Stage 2 checks the level caps. The plan scoped splits to the board panel, room view and column body only, and put the Prettier reformat first, so the growth (long lines wrapped, ARIA attributes added) was not weighed against the cap. `RetroGroupItem` holds two near-identical template branches. This is not a plan deviation; it is a follow-up item for the backlog.
**Fix:** Follow-up row: split solo/multi group rendering and the evidence-link block of `RetroCardItem`; extract the column-config rows of `CreateSprintForm`.
**Confidence:** 80/100

### [LOW] `useTimerActions` toasts from inside a composable
**File:** `web/reflekt-web/src/composables/useTimerActions.ts:9-20`
**Origin:** design
**Issue:** `frontend-review` Stage 4: "No composables calling UI side effects (toasts, alerts) — expose error state". The plan (Step 7) asked for exactly this ("keeping 'Not connected to server.' and its toasts"), so the code conforms to the plan and the plan departs from the checklist. `useRetroBoardActions` shows the cleaner shape already in the repo (`onError` callback supplied by the component).
**Fix:** Take an `onError` callback like `useRetroBoardActions`, or return the failure and let `TimerControls` toast. Optional.
**Confidence:** 80/100

### [LOW] Colour swatches announce `aria-pressed` but cannot be operated from the keyboard
**File:** `web/reflekt-web/src/components/retro/RetroColumnPanel.vue:28-37`
**Origin:** implementation
**Issue:** The swatches select on `@mousedown.prevent` only, and the name input commits on `@blur`. Tabbing from the input to a swatch blurs the input, `confirmEdit` runs, `editing` turns false and the swatches vanish; Enter/Space on a swatch fires `click`, which has no handler. The behaviour is as before F39, but Step 14 added `aria-label`/`aria-pressed` to these buttons and records the keyboard sites as done, so the control now claims an operability it does not have.
**Fix:** Add `@click="selectColor(color)"` and stop `@blur` from committing when focus moves to a swatch (or commit on Enter/explicit button), or drop the `aria-pressed` claim and list the gap. The `CreateSprintForm.vue` swatches are fine (they use `@click`).
**Confidence:** 80/100

## Carry-Over Findings (implementation.md) — each confirmed or refuted
| Row | Disposition | Evidence |
|---|---|---|
| Advance-dialog warning reset | **Confirmed**, LOW, accept. | `HEAD` `RetroRoomView.vue` Writing branch sets `advanceIsWritingWarning`; the Voting/Discussing/other branches never reset it, so the "not reviewed" tooltip could persist onto a later dialog. `useRetroPhase.advancePrompt` returns `warning: false` on every other branch. A strictly-correct small behaviour change; no user would want the old stale tooltip. Worth one line in the summary. |
| Assignee `<select>` disabled while saving | **Confirmed**, LOW, accept. | `RetroActionItemInput.vue` `:disabled="saving"`; the text input beside it was already disabled in `HEAD` (`RetroBoardPanel` `:disabled="actionSaving"`). Needed so the failure refill cannot overwrite a mid-flight choice. |
| Two `h1`s in the room's report view | **Confirmed resolved.** | `SprintReportPanel` `titleTag` prop (default `h1`); `RetroReportPanel.vue` passes `title-tag="h2"`; standalone `ReportView` keeps `h1`. |
| Failed readiness toggle / clipboard write reject unhandled | **Confirmed as today's behaviour.** | `HEAD` `useReadiness.toggleReady` awaited `invoke` with no catch; `HEAD` `copyAsMarkdown` awaited `clipboard.writeText` with no catch. Same in the new code. Not a regression. |
| Token shade shifts | **Confirmed**, LOW, accept. | `main.css` `--color-danger: #ef4444`, `--color-info`, `--color-warning` are the existing v3-hex tokens; solid fills got exact-value oklch tokens (`--color-success-solid`, `--color-danger-hover/-active`). FE-19's suggested fix names these tokens. Walkthrough item 5 stands. |
| Back-link arrow beyond the plan's list | **Confirmed**, in scope. | Spec Decisions row 1: "pasted icons and unicode arrows become lucide icons". `SprintDetailPanel` uses `ArrowLeft`. |
| `BaseCard` corners on team/sprint frames | **Confirmed.** | `HEAD` frames: `rounded` (CreateTeamForm, SprintList, SprintDetailPanel) and `rounded-lg` (TeamCard); `BaseCard` is `rounded-md`. Spec Scope row 6 requires `BaseCard` on these screens; Decisions row 1 does not list the corner. The owner should see it in the summary; not a code fix. |
| Kick is a small red button | **Confirmed**, in scope ("buttons take `BaseButton`'s look"). | `SprintDetailPanel.vue` `BaseButton variant="danger" size="sm"`. |
| Dashboard second create-team form shows no Cancel | **Confirmed pre-existing.** | `HEAD` `TeamDashboardView.vue:53` passes `@cancel` and no `show-cancel`; `HEAD` `CreateTeamForm` had `showCancel?` defaulting off. Unchanged. |
| Plain controls left outside Base* | **Confirmed**, matches Decision 18 / Step 13 text. | `TeamCard` menu rows, `CreateSprintForm` swatches, `PreviousActionsPanel` pills, `RetroJoinForm` Google link. |
| Info icons no longer open the team | **Confirmed**, accepted consequence of the stretched-name-button pattern. | `TeamCard.vue`: info buttons sit in `BaseTooltip class="z-10"` above the `after:inset-0` overlay; click does not reach the name button. |
| SM timer click target narrowed | **Confirmed.** | `TimerDisplay.vue`: trigger is the timer + chevron `<button>`; mute button is a sibling (no nested buttons). |
| Disabled "Create Team" tooltip is hover only | **Confirmed**; fine. | `BaseButton` disabled has `pointer-events-none`, so the hover reaches the `BaseTooltip` wrapper; a disabled button cannot take focus. |
| Tab panels keep `role="tabpanel"` on desktop | **Confirmed**; see Open Question 2. | `RetroColumnPanel.vue:2-5` `role="tabpanel"` always; the tab bar is `sm:hidden`. Referenced ids exist (hidden, not removed). |
| Credentials now `include` on seven calls | **Confirmed**, no effect. | `api/client.ts:50` always `include`; every path is relative/same-origin, where the fetch default (`same-origin`) already sends the cookie. |
| AC9 browser walkthrough owed | **Confirmed open.** | Step 15 lists the `OPERATOR ACTION REQUIRED` items; Step 16 is operator-owned. The summary must list AC9 as open acceptance. |
| `graphify update .` not run | **Confirmed.** | Not part of this diff (no `graphify-out` change in `git status`); run after merge. |
| Unsanctioned `git rm --cached` in Step 9 | **Confirmed resolved.** | `git diff --cached --stat` is empty now; `RetroColumnBody.vue` is a working-tree deletion only. |

## Positive Observations
- The optimistic machinery (`pendingValues.ts` plus the per-store `settle`/`confirm`/`undo` calls) is small, uniform across card, group, vote, readiness and review status, and I could not construct an interleaving that violates Decision 16. Out-of-order responses and stale confirms are ignored correctly (`confirm`/`undo` return when the entry is gone).
- Behaviour fidelity is high where I diffed against `HEAD`: `mergeGroups` booleans, `dropOnColumn` guards, `renameColumn` payload, `TimerControls` button sets per status, advance-dialog texts, hub handler bodies, `canMutateCard` (only the pending clause added), `reportMarkdown` vs the old `copyAsMarkdown` body.
- `describeApiError`/`isHttpError` keep today's two load texts apart; the pre-F38 reader is one named, commented function (AR-2).
- All spec greps I re-ran pass (see Evidence). `package.json`/`package-lock.json` are untouched, no backend file changed, the index is clean, no `eslint-disable`/`@ts-ignore`, no `any`, no `!` left.
- Comment discipline is good: the comments that exist state constraints the code cannot show; the only feature-id reference (`client.ts:105`) is the plan-mandated pointer to change-note 3.

## Gaps
- Lint and `prettier --check` were not re-run by me (`npm ci` in the run record removed the `--no-save` packages and the brief bars re-running the suite). I checked the lint-relevant classes by hand instead: unused imports (script over `src/`: none), unused store returns (see Evidence), non-null inventory (0), `any` (0).
- Behaviour that needs a browser (focus trap/return, arrow keys, tooltips on focus, pending-card controls, throttled-network interleavings, light/dark look of the Base* adoption) is the operator's Step 16; I did not simulate any of it.
- `aria-invalid`/`aria-describedby` wiring exists in `BaseInput` but the three forms (`CreateTeamForm`, `CreateSprintForm`, `DeleteTeamDialog`) still show their errors in a separate `<p>` and never pass `error`, so the wiring is unused there. Matches the plan text (the wiring is for `BaseInput`'s own error), noted for the summary.
- A few store members are returned and referenced nowhere outside their store: `participant.meLoading`/`participantsLoading`, `sprint.currentSprintLoading`/`currentSummaryLoading`, `policy.matrix`, `vote.voteCounts`. Not on FE-25's list and the loading flags are F36's per-resource contract, so left alone; a candidate for the next dead-code sweep.

## Open Questions
1. `BaseInput.vue:19`: `v-bind="inputAttrs()"` comes after the component's own `:aria-describedby`, so a caller-supplied `aria-describedby` (the tooltip id in `CreateTeamForm`/`DeleteTeamDialog`) would replace the error id if a caller ever passes both `error` and `aria-describedby`. No caller does today; confidence 60 that this matters later. Merge the two ids if you want it robust.
2. `RetroColumnPanel` is `role="tabpanel"` at every width, but the tab bar is hidden at `sm` and up, so desktop screen-reader users meet three "tab panels" without tabs. The developer chose it so the markup is width-independent (confidence 55 that this is a defect rather than an accepted compromise). Option: set the role only below `sm` or leave it to the panel's heading.
3. `RetroCardItem.vue` nests a `<button>` (remove link) inside an `<a>`; the nesting is pre-existing and was only labelled in Step 14. Left out of Findings (confidence 50 that it is in scope).

## Evidence
| Check | Result | Command | Output |
|---|---|---|---|
| Run record | pass | brief: verify-run complete, run `mvl2bnlv-b7a109e5` (`dotnet test Reflekt.slnx && npm ci && npm run build`) | verdict pass, cited as the test/build evidence; suite not re-run |
| AC1 network `fetch(` outside `src/api/` | no hits | `grep -rnE "(^\|[^.A-Za-z])fetch\(" src \| grep -v "src/api/"` | empty |
| AC2 non-setup stores | no hits | `grep -rn "defineStore(" src/stores \| grep -v "() =>"` | empty |
| AC3 guest header only in api | no hits | `grep -rn "X-Guest-Token" src \| grep -v "src/api/"` | empty |
| AC4 sizes | 162 / 99 lines | `wc -l RetroBoardPanel.vue RetroRoomView.vue` | `RetroColumnBody.vue` deleted |
| AC5 | no hits | `as unknown as`; raw palette classes; `@apply\|<svg\|confirm(` in `.vue` | all empty |
| AC6 widened store-write grep | no hits | the plan's grep over `composables components views` | empty |
| AC7 / F36-1 | no hits | `grep -rnE "\[--[a-z-]+\]" src` | empty |
| Non-null inventory, `any`, suppressions | 0 / 0 / 0 | `grep -rnoE "[]A-Za-z0-9_)]![].)[ ,;]"`; `: any\|as any`; `eslint-disable\|@ts-ignore` | empty |
| `isDark` and `toLocale` placement | clean | `grep -rn "isDark" src \| grep -v "AppHeader.vue\|useTheme.ts"`; `grep -rn "toLocale" src \| grep -v src/format/` | empty |
| Unused imports in `src/` | none | scratch script `D:/temp/rev588/unused.mjs` (outside the repo) | one false positive (`...DEFAULT_COLUMNS[0]` spread) |
| Unused store returns | 6 listed under Gaps | scratch script `D:/temp/rev588/members.mjs` | none on FE-25's list |
| Request table vs `HEAD` | match | compared all `fetch(` in `git show HEAD:` stores/panel with `src/api/*.ts` (path, method, body, token) | 41 functions, 23 with `guest: true` |
| Hub lifecycle diff | only sanctioned edit | `git show HEAD:.../useSprintHub.ts` `connect`/`disconnect` vs working tree | `build` returns the connection; `const built`; `disconnect` unchanged |
| Relocated block (markdown) | equivalent by read | `git show HEAD:.../stores/report.ts` `copyAsMarkdown` vs `format/reportMarkdown.ts` | same lines, same order, `formatDate`/`formatStatus` equal the inline expressions; developer's `ssrLoadModule` fixture result recorded in `implementation.md` Step 11 |
| Package files / index / backend | untouched / empty / none | `git status --porcelain package*.json`; `git diff --cached --stat`; `git status` outside `web/reflekt-web/src` | only `.prettierrc.json`, `.prettierignore`, `eslint.config.js`, docs; `.claude/tmp/` and `.claude/.personas.json` are untracked, not the developer's (the developer's scratch files were deleted) — keep them unstaged |
| Platform-conditional code | none | SPA only; no `process.platform`/OS branch | n/a |
| Mutation battery | n/a | no frontend tests exist and none may be added (campaign rule) | — |
| Lessons | none cleared the capture bar | — | nothing appended |

*Status: COMPLETE — reviewer (second reader), 2026-10-23*
