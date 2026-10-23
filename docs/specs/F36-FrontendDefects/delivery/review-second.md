# F36 — Frontend Visual & UX Defects — Second Reader (Step 2)

## Verdict: COMMENT

Second reader, first review. Covers: the whole working-tree diff against e8347d5 (45 files under `web/reflekt-web`, plus `implementation.md`).
**Model:** claude-sonnet-5-5

## Reviewed By
reviewer (second reader). The primary reader is a Codex job. This file holds only my own read, and the coordinator merges both.

## Pre-commitment Predictions
1. Latest-request guards and background versus foreground races in the card, group and room stores. Found sound: every commit, error and loading write is guarded, and a background refresh that supersedes a foreground load still clears `loading`.
2. `useRetroRoom` retry resuming at the wrong stage, a double join or a double hub connection. Found sound: `retryEntry` is a no-op while an attempt runs, the resume stage is recorded per failure, and `connect()` stops any existing connection first (`useSprintHub.ts` `connect`).
3. `canMutateCard` dropping the `authStore.user !== null` branch could remove Edit/Delete for Google participants. Not a bug: `joinWithGoogle` sets `currentParticipant` (`participant.ts` line ~48). `EditCardEndpoint.cs` lines 33-40 and 57-62 confirm that a caller with a participant claim may touch only their own cards. The Closed, phase and Discussing-action-item checks match the endpoint line by line.
4. A store clear-on-start breaking a consumer that reads the store at mount. Found one (Finding 1).
5. `inheritAttrs: false` losing `class`/`style` or a listener on `BaseInput`. Found sound.

## Findings

### [MEDIUM] Clear-on-start removes the create-sprint form's pre-fill from the previous sprint
**File:** `web/reflekt-web/src/stores/sprint.ts:93-96` (clear) and `web/reflekt-web/src/components/sprints/CreateSprintForm.vue:134-141` (reader)
**Origin:** design
**Issue:** `CreateSprintForm.onMounted` pre-fills the column names and colours from `sprintStore.sprints[0]`. `SprintListView` runs `fetchTeamSprints` in an immediate watch during setup, and that action now sets `this.sprints = []` synchronously (the spec FE-08 clear-on-start). `SprintList` renders the form unconditionally, so the form mounts after the clear and before the response arrives. The pre-fill therefore never runs. Before F36 the store kept the previous list, so re-entering the list (for example breadcrumb back from a sprint) pre-filled from the most recent sprint. The "no behaviour change outside the named defects" invariant (plan Must-NOT-Change, "Layout and colours outside the named defects") is not pinned for this reader, and neither the plan nor `implementation.md` mentions it. The old behaviour was partly accidental. It also pre-filled a different team's columns when you moved between teams. Even so, a user-visible convenience is gone without being recorded.
**Fix:** Pick one, or record it as an accepted change. (a) Have the form apply the pre-fill when the list arrives, for example a `watch` on `sprintStore.sprints[0]` that applies it only while the user has not edited the columns. (b) Mount the form only after the list load has ended (`v-if="!loading"` in `SprintList`), so `onMounted` sees the loaded list. Option (b) is the smaller edit. Either way, add a line to `implementation.md` or the F39 carry-over.
**Confidence:** 80/100

## Positive Observations
- Must-NOT-Change checked mechanically. `git diff` of `stores/` shows every changed `fetch(` line differs only by indentation (URLs, methods, headers, bodies untouched). `useSprintHub.ts` has exactly 8 changed pairs, all `{ background: true }` arguments. `router/index.ts` has one added `requiresAuth: true`, on `sprint`. `docs/backlog.md`, `package.json` and `package-lock.json` are untouched.
- AC1 re-run: `grep -rnE "\[--[a-z-]+\]" web/reflekt-web/src` returns no hits. All `web/reflekt-web` files except `favicon.ico` are `w/crlf`, so the diff carries only the intended lines.
- Every GET `fetch` in the nine named stores follows the load contract (`grep -n "fetch("` cross-checked against the mutating actions). `auth.fetchMe` sharing one in-flight promise is a sound reading of "latest wins". With a counter, the router guard's older request would be discarded and a signed-in user sent away from a `requiresAuth` route.
- The staged room entry is clean. Stage failures stop at once, the hub connects only after all loads succeed, a refused guest token (401) still clears the token, and a failed `connect` becomes a room error with Retry instead of a stuck spinner.
- `BaseInput.inputAttrs()` is a plain function called from the render, with a constraint comment (`useAttrs()` is not reactive). `BaseButton` is not subject to the double-bind: its `v-bind="$attrs"` and the fallthrough land on the same root element with the same function, which Vue's `mergeProps` dedupes.
- Comments added are constraint comments (Tailwind literal class names, router key, attrs reactivity, request sharing). No history, ids or plan rationale in code.

## Gaps
- `auth.fetchMe` is never retried after a network failure. `loaded` is true, so the guard does not call it again, and the "Can't reach the server." notice stays until a reload. The spec asks only for the notice, so this is not a finding. It is worth knowing for F39.
- `AppHeader`'s team-name watch is keyed on `route.fullPath`, so a query or hash change on a team route re-requests `/api/teams` when the user has zero teams and no error. This is bounded (one request per navigation, never a stream) and matches "each route entry".
- Nothing in this session observes the Google-participant, phase, closed-sprint, hub-refresh-banner or chime behaviours. They are listed for the operator (Step 14), and AC6 stays open.
- The `docs/conventions/vue.md` § White Flash Prevention text now contradicts the shipped `index.html`. The plan assigns that doc to F40.

## Carry-Over Findings (implementation.md)
| Title | My disposition |
|-------|----------------|
| Bare `border-b` / `border-t` render black lines in light theme | Confirmed pre-existing. `RetroColumnPanel.vue:9` and `:67` carry no colour class, and F36 changed only the radius token on line 9. Out of scope, an F39 candidate. No finding. |
| Unused `MUTE_KEY` constant | Confirmed. `grep -rn MUTE_KEY` gives the single declaration at `useTimer.ts:12`, and it was unused before F36. LOW, out of scope. |
| Report Retry also shows on 401/403 | Confirmed as built. `ReportView` shows Retry on any `error`. The plan's Step 6 bullet has no status split, so this is not a deviation. LOW, UX only. |
| Walkthrough covers guest flows only | Confirmed. AC6 stays open until the operator records Step 14. The close must list it as open acceptance. |

## Open Questions
- Should `fetchMe` retry once the server is reachable again, so the unreachable-server notice clears and a signed-in user is restored without a reload? The spec stops at the notice. Confidence the current behaviour is a defect: 50.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pass | `verify-run mvkidqtd-2616b88d`, profile complete (`dotnet test Reflekt.slnx && npm ci && npm run build`), cited from the brief and not re-run | verdict pass, per the brief |
| AC1 grep | pass | `grep -rnE "\[--[a-z-]+\]" web/reflekt-web/src` | no hits |
| Line endings | pass | `git ls-files --eol web/reflekt-web` | all `w/crlf` except `favicon.ico` (binary) |
| Must-NOT-Change files | pass | `git status --short docs/backlog.md web/reflekt-web/package.json web/reflekt-web/package-lock.json` | empty |
| Server mirror (FE-31) | pass | read `EditCardEndpoint.cs` 28-66 against `canMutateCard` | order and conditions match |

*Status: COMPLETE — reviewer (second reader), 2026-10-23*
