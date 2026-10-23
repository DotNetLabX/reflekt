# F39 — Frontend Conventions Conformance

**Feature Spec:** `docs/specs/F39-FrontendConventions/definition/spec.md`
**Model:** claude-opus-5-5[1m]
**Build slices:** 1–4 · 5–7 · 8–11 · 12–15; why: foundations (tooling, imports, types, API layer), then the store conversion and the hub, then the optimistic actions and the three large retro surfaces that consume them, then design system, accessibility and verification. The 7|8 cut is the least-coupled point of the store chain: Step 8 reads the converted stores from the tree.
**Size:** 15 developer-built steps · 105 files · fullChecks: none — no fullChecks list declared
**Lane:** full — normal gear (as today, by size: 15 developer-built steps, 105 files)
**Build lane:** thorough — full team — normal gear
**Gear:** normal — campaign

## Context

The SPA (`web/reflekt-web/src`, 65 files, built into `src/Services/Reflekt/Reflekt.API/wwwroot/`) does each job several ways: 13 options-style stores, 42 network `fetch(` calls in stores and one component with the guest-token header pasted 24 times, optimistic updates in three layers, components far over the size caps, stringly-typed phases, two generations of UI controls and 6 ARIA attributes in the whole app. This feature brings it onto the repo's frontend skills (`create-vue-feature`, `pinia-patterns`, `vue-component-architecture`, `vue-patterns`, `tailwind-theme`, `frontend-review`). `docs/conventions/vue.md` (listed by `docs/conventions/coding-conventions.md`) complements the skills and binds too — notably "store actions only mutate store-owned state" and `RouterLink` needing no import; its White Flash section is superseded by F36 and is F40's to rewrite.

One service is touched: the SPA. No backend, API or SignalR protocol change.

Binding campaign constraints: no new product behaviour; no new tests (the frontend has none); no business-rule registries; never edit `docs/backlog.md`, `web/reflekt-web/package.json` or `web/reflekt-web/package-lock.json` — those changes go to `docs/specs/F39-FrontendConventions/delivery/change-notes.md` (AR-1). F39 merges **before** F38, and this tree's backend still sends FastEndpoints' error body (AR-2, questions.md Q1). Never write outside this worktree, never leave the branch, no git writes.

Plan-time baseline (executed 2026-10-23, from the repo root): spec AC1 grep → 43 hits (42 network calls + the policy store's `fetch()` method); AC2 → 13 stores; AC3 → 24 sites in 7 files; AC5 → 1 `as unknown as`, 22 raw-palette hits in 13 files, 11 `@apply`/`<svg`/`confirm(` hits in 4 files; AC6 → 9 store writes outside stores (18 with the widened grep of Step 7, which also catches indexed writes and names with digits), 6 eager routes. The planned ESLint config (Step 1) on today's `src`: 66 errors — `no-restricted-globals` (fetch) 42, `no-non-null-assertion` 18, `no-unused-vars` 4, `no-explicit-any` 2; Prettier flags all 65 files.

## Scope

**In scope** — every spec Scope row (FE-01, -02, -03, -06, -07, -09, -10, -15…-19, -21…-27, -29, -30, -33…-42), as amended at plan time (spec header `Amended:`). Each row's required outcome is binding; FE rows not narrowed by the spec take the "suggested fix" in `docs/specs/F34-ConventionsAudit/delivery/findings/frontend.md`. The steps below add only what the plan decides.

**Out of scope** — backend and docs (F40 owns `docs/conventions/vue.md` and frontend-testing docs); frontend tests (FE-43); editing `.claude/skills/` (owner decision 2026-10-12); renaming store files; the hub connection lifecycle (`useSprintHub` `connect`/`disconnect`, pinned by F36's deferred backlog row).

**Rule list:** not applicable — the spec header reads `Rule list: not applicable`.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | (none) | — | no | ESLint + Prettier configs, `--no-save` install, one-time reformat, change note 1 | gap: frontend lint/format setup |
| 2 | create-vue-feature | Follow | no | `@/` imports, lazy routes, PhaseIndicator move, App `fetchMe` call | — |
| 3 | create-vue-feature | Follow | no | `src/types/*` files, unions, `PHASES`; `src/constants/columns.ts` | — |
| 4 | create-vue-feature | Find or build | no | `apiFetch` (skill cites `client/src/api/settings.ts`, absent) → build `src/api/client.ts`; the request table | — |
| 5 | pinia-patterns | Follow | no | auth, guest, policy, readiness, timer, teams; storage helper | — |
| 6 | pinia-patterns | Follow | no | card, cardGroup, vote, participant, sprint, report, previousActions | — |
| 7 | pinia-patterns | Follow | no | 26 hub events → store actions; `api/hub.ts` provider; `useTimerActions` | — |
| 8 | pinia-patterns | Follow | no | optimistic actions list | — |
| 9 | vue-component-architecture | Follow | no | board panel split, column merge, dummy props | — |
| 10 | vue-component-architecture | Follow | no | room view split, `useRetroPhase`, phase header | — |
| 11 | vue-component-architecture | Follow | no | one report component, markdown pure function | — |
| 12 | tailwind-theme | Follow | no | tokens, `success` variant, `@apply` out, lucide | — |
| 13 | create-vue-feature | Follow | no | Base* adoption on the four screens, Page Shell `EmptyState`, `defineModel`, deletions | — |
| 14 | frontend-review | Follow | no | Stage 3 accessibility items from FE-29/FE-30 | — |
| 15 | (none) | — | no | lint, type-check, build, greps, requests table, walkthrough, graph | — (verification only) |

TDD is `no` on every step: no frontend test setup and the campaign forbids new tests. Proof is the greps, `type-check`/`build`, lint, diff review and the walkthrough (Steps 15–16).

## Domain Model Changes

None.

## Data Model Changes

None.

## Implementation Steps

All paths are under `web/reflekt-web/` unless they start with `docs/` or `.claude/`. After each step run `npm --prefix web/reflekt-web run type-check` (through `verify-run.js` where your seat's test-entry rule applies); after Steps 1, 4, 8, 11 and 15 also `npm --prefix web/reflekt-web run build`. A complete run (`npm ci`) wipes the `--no-save` lint packages — reinstall them with Step 1's command before linting. From Step 2 on, before each hand-back run `npx prettier --write` on the files the step touched, so Step 15's `prettier --check` meets a formatted tree. An Accept line that names an intermediate hit (Steps 5 and 6) describes that step's state; Step 15 checks the final state, where those hits are gone. Public surfaces that are binding: route paths/names, API paths/methods/bodies/headers/credentials, hub event and method names, storage keys, component file paths named here. Store action names, composable internals and helper names are the developer's call.

### Step 1 — Lint and format tooling; one-time reformat (FE-37 part)

No pattern skill.

- Install, without touching the package files: `npm --prefix web/reflekt-web install --no-save --no-audit --no-fund eslint@10.13.0 eslint-plugin-vue@10.11.1 @vue/eslint-config-typescript@14.9.0 @vue/eslint-config-prettier@10.2.0 prettier@3.9.10` (proven at plan time: both package files stay byte-identical).
- Create `eslint.config.js` (JavaScript, not TypeScript: `tsconfig.node.json` includes `eslint.config.*`, and a `.ts` config would break `type-check` in every tree where the packages are not installed): `defineConfigWithVueTs` with `pluginVue.configs['flat/recommended']`, `vueTsConfigs.recommended`, `@vue/eslint-config-prettier/skip-formatting`, plus `@typescript-eslint/no-non-null-assertion: error`; `no-restricted-globals` for `fetch` on `src/**` except `src/api/**`; `no-restricted-imports` of any store module (`@/stores/*`, `**/stores/*`) inside `src/api/**`.
- Create `.prettierrc.json` — `semi: false`, `singleQuote: true`, `printWidth: 100`, `trailingComma: "all"` — and `.prettierignore` (nothing outside `src` is formatted by this feature).
- Run `prettier --write src` once and nothing else in this step: no `eslint --fix`, no hand edit to `src`.
- Create `docs/specs/F39-FrontendConventions/delivery/change-notes.md` in the shape of F37's notes (Target, Source, Order, Command, Expected result) with **Note 1 — lint dev-dependencies and script** (Target `web/reflekt-web/package.json`, `package-lock.json`; Order: after F37's notes; Command: `npm --prefix web/reflekt-web install -D eslint@10.13.0 eslint-plugin-vue@10.11.1 @vue/eslint-config-typescript@14.9.0 @vue/eslint-config-prettier@10.2.0 prettier@3.9.10` then `npm --prefix web/reflekt-web pkg set scripts.lint="eslint . && prettier --check src"`; re-check that `typescript-eslint` (pulled in at 8.72.0, TypeScript peer `>=4.8.4 <6.1.0`) still accepts F37's TypeScript; Expected: `run lint` 0 errors, `audit --omit=dev` 0 vulnerabilities) and **Note 2 — verify role** (Target `.claude/verify.json`; Order: after Note 1; append ` && npm --prefix web/reflekt-web run lint` to `roles.full` and re-time `measured.full`).
- Accept: `git status --porcelain web/reflekt-web/package.json web/reflekt-web/package-lock.json` → empty; `npx --prefix web/reflekt-web prettier --check web/reflekt-web/src` → clean; the step's `src` diff is exactly the output of the recorded `prettier --write src` (record the command and file count in `implementation.md`); `type-check` and `build` green.
- Satisfies: spec FE-37 (tooling, reformat-first), Decisions row 3, AC8 (setup).

### Step 2 — `@/` imports, PhaseIndicator move, lazy routes, one `/auth/me` (FE-37 rest, FE-38)

Follow create-vue-feature.

- Every import that climbs a folder (`from '../…'`, 34 files today) uses `@/`; same-folder `./` imports stay (Decision 8).
- Move `src/components/PhaseIndicator.vue` → `src/components/retro/PhaseIndicator.vue`; update its importer(s).
- `src/router/index.ts`: all six routes `component: () => import('@/views/…')`; paths, names, `meta` and the guard unchanged.
- `src/App.vue`: delete the `authStore.fetchMe()` call — the router guard runs on every navigation, the cold load included, and `fetchMe` already shares one in-flight request (`auth.ts` `fetchMeInFlight`). `src/components/AppHeader.vue`: drop the explicit `RouterLink` import, and guard the template's `authStore.user!.email` (FE-17).
- Accept: `grep -rnE "from '\.\./" web/reflekt-web/src` → no hits; `grep -c "() => import(" web/reflekt-web/src/router/index.ts` → 6; `grep -n "fetchMe" web/reflekt-web/src/App.vue` → no hits; `test ! -e web/reflekt-web/src/components/PhaseIndicator.vue`.
- Satisfies: spec FE-37, FE-38, AC6 (routes), AC9 (one `/auth/me`).

### Step 3 — `types/` folder (FE-15, FE-16, FE-17 types)

Follow create-vue-feature.

- Create `src/types/domain.ts` (unions `SprintPhase` = Writing·Grouping·Voting·Discussing·Reporting, `SprintState` = Draft·Active·Closed, `ColumnType` = Column1·Column2·Column3·ActionItems, `CardType` = Note·ActionItem, `ReviewStatus` = None·Done·CarriedOver·Dropped, `FetchStatus` = Pending·Ok·Failed, `ColumnColor` = the eight palette names; and `PHASES` — ordered, per phase `{ name, short, next, timed, readiness }`, **no icons** (Decision 12)). The report's previous-action status is `ReviewStatus | 'Not reviewed'`.
- Create `src/types/auth.ts`, `team.ts`, `sprint.ts` (exported `Sprint`, `SprintSummary`, one create-sprint payload with the timer fields), `card.ts` (card, evidence link, card group), `participant.ts`, `vote.ts` (incl. the vote response), `report.ts` (report + previous actions), `timer.ts` (`TimerStatus`, `TimerPayload` — ends the `stores/timer.ts` ↔ `composables/useTimer.ts` cycle).
- Move every exported or duplicated DTO/interface out of stores, composables and components into these files; stores, composables and components import types only from `@/types/…`. The create-sprint payload (one copy left after F36, in the sprint store) and the column-config payload move to `src/types/sprint.ts`; `RetroGroupItem`'s `DragPayload` moves to `src/types/card.ts`.
- Create `src/constants/columns.ts` (FE-35): the palette list, the default column names and colours, and the default timers (Writing 5, Voting 3, Discussing 10 — the create form's initial values and the room's fallback when a sprint carries none). Switch today's copies to it in this step: `PALETTE_COLORS`/`paletteColors` in `RetroColumnPanel.vue` and `CreateSprintForm.vue`, the default names/colours in `CreateSprintForm.vue` and `RetroBoardPanel.vue`, the default timers in `CreateSprintForm.vue` and the `?? 5`/`?? 3`/`?? 10` fallbacks in `RetroRoomView.vue`.
- Accept: `grep -rnE "^export (interface|type) " web/reflekt-web/src/stores web/reflekt-web/src/composables` → no hits; `grep -rn "from '@/composables" web/reflekt-web/src/stores` → no hits; `grep -rln "'slate'" web/reflekt-web/src` → only `src/constants/columns.ts` and `src/types/domain.ts` (the palette is declared once; today 2 declarations); `type-check` green.
- Satisfies: spec FE-15, FE-16, FE-17 (types), FE-35 (constants).

### Step 4 — The API layer (FE-02, FE-18, FE-17 API part)

Find or build `apiFetch` (looked for: `src/api/`, the skill's `client/src/api/settings.ts`, any shared package — none exists), built once in `src/api/client.ts`; then follow create-vue-feature.

- `src/api/client.ts`: `apiFetch<T>` (credentials always `include`; `Content-Type: application/json` exactly when a body is sent; opt-in guest token per call; 204 and empty bodies resolve as success, a non-JSON body never crashes; a rejected network call becomes an `ApiError` with `status: 0`); `setGuestTokenSource(getter)`; `ApiError extends Error` (`status`, `code`, `message`, `errors: { field, message }[]`); the body readers — F38's shape first, then **one function marked as the pre-F38 reader** (AR-2: `errors.generalErrors[0]` → message; a leading `duplicate_name: ` / `limit_reached: ` → `code`, stripped, only those two; other `errors` keys → `{ field, message }`; FastEndpoints' "One or more errors occurred!" ignored); `describeApiError(err, { statusMessages?, fallback })` in the spec's order — known `code` (shows the server's text for that code, prefix removed), the operation's status message, the server's `message`, the fallback.
- `src/api/hub.ts`: `invoke(method, ...args)` through a connection provider that `useSprintHub` registers (Step 7); throws when there is no connection. No store import.
- Modules, each function declaring whether it sends the token, keeping today's path, method and body byte-for-byte (`'{}'` bodies included): `auth.ts`, `teams.ts`, `sprints.ts`, `cards.ts`, `cardGroups.ts`, `participants.ts`, `votes.ts`, `reports.ts`, `previousActions.ts`, `policy.ts`.
- **Sends `X-Guest-Token` (23):** cards — list, create, edit, delete, attach link, detach link; cardGroups — list, create, add card, remove card, dissolve, focus, toggle discussed, update title; participants — me, list; previousActions — list, set review status; reports — team report, participant report; votes — mine, cast, retract. **No token (18):** auth — me, logout; teams — list, create, delete; sprints — team sprints, sprint, summary, create, update columns, start, advance check, advance, close; participants — join, join with Google, kick; policy — phase actions. Clear focus has no function (Q3).
- `src/main.ts`: after `app.use(pinia)`, hand the client a getter that reads the guest store's token — `src/api/` itself imports no store.
- Change note: append **Note 3 — backlog** to `delivery/change-notes.md`: Target `docs/backlog.md`; add the row "Remove the pre-F38 error-body reader from web/reflekt-web/src/api/client.ts (F38 is merged; only F38's shape remains)" and record that #39 is built on F36 and merges before #38 (AR-2).
- Accept: inside the new layer, `grep -rln "X-Guest-Token" web/reflekt-web/src/api` → only `src/api/client.ts` (the one place that sets it), and exactly the 23 functions above pass the opt-in (the whole-source AC3 gate is Step 8's — stores and `PreviousActionsPanel` keep their own headers until then); `grep -rnE "useGuestStore|from '@/stores|from '\.\./stores" web/reflekt-web/src/api` → no hits; the pre-F38 reader is one named function with a comment pointing at Note 3; `type-check`, `build` green.
- Satisfies: spec FE-02, FE-18, FE-17, AC3 (api half); Decisions P1.

### Step 5 — Small setup stores; storage helper (FE-01, FE-42, FE-27 timer part, FE-25 part)

Follow pinia-patterns.

- Convert to setup stores, calling only `src/api/`: `auth`, `guest`, `policy` (action `fetch` → `load`; caller `useRetroRoom.ts`), `readiness`, `timer`, `teams`. Keep every F36 outcome: per-resource `loading`/`error`, latest-request-wins counters, `fetchMe` shared in-flight and never rejecting.
- `src/storage/storage.ts`: one guarded helper (try/catch, validated reads) and the key constants `reflekt:sprint:{id}:token`, `reflekt:theme`, `reflekt:timer:muted` — names unchanged. `guest`, `timer` and `src/composables/useTheme.ts` use it (the theme read is validated to `'dark' | 'light'`, defaulting to `'dark'` as today). `useTheme` toggles the `light` class with `classList.toggle('light', …)` instead of overwriting `className` (FE-42; same class set today).
- Timer (FE-27): one timer-update action replaces `applyTimerUpdate`/`applyTimerSnapshot`, and the `TimerUpdated`/`TimerSnapshot` handlers in `src/composables/useSprintHub.ts` call it in this step; one `mm:ss` formatter in `src/format/format.ts`, used by `useTimer.ts` and `TimerDisplay.vue`.
- Unused members deleted: `guest.displayName` (and its parameter), the timer store's `formattedTime`, `isRunning` and `isExpired` getters, and `useTimer`'s `isRunning`/`isExpired` returns.
- Error consumers of `teams`: `src/components/teams/CreateTeamForm.vue` and `src/components/teams/DeleteTeamDialog.vue` read `describeApiError` with fallbacks "Failed to create team." / "Failed to delete team." — today's text in this tree (AR-2).
- Accept: `grep -rn "localStorage" web/reflekt-web/src | grep -v "src/storage/"` → no hits; `grep -rnE "reflekt:(sprint|theme|timer)" web/reflekt-web/src | grep -v "src/storage/"` → no hits; `grep -rn "generalErrors" web/reflekt-web/src | grep -v "src/api/"` → only `CreateSprintForm.vue` (done in Step 6).
- Satisfies: spec FE-01, FE-42, FE-27, FE-25 (part).

### Step 6 — Large setup stores (FE-01, FE-02, FE-25 part)

Follow pinia-patterns.

- Convert `card`, `cardGroup`, `vote`, `participant`, `sprint`, `report`, `previousActions` to setup stores calling only `src/api/`, keeping the F36 load contract (per-resource status, latest request wins, background refresh for cards and groups, `participant` keeping the failing status for the room's 401 check) and today's thrown-error contract for mutating actions — now an `ApiError`.
- Status messages move into `describeApiError` calls: report (404 "Sprint not found", 401/403 "Not authorized"), sprint (404 "Sprint not found"), the join flow in `src/composables/useRetroRoom.ts` (join: 409 "Name already taken. Choose another.", 404 "Sprint not found.", fallback "Failed to join. Please try again."; join with Google: 409 "Sprint is not accepting participants.", 404 "Sprint not found.", fallback "Could not join with Google ({status}). You can join as a guest instead." where a network failure (`ApiError.status` 0) shows "network error" in place of the status, as today). `src/components/sprints/CreateSprintForm.vue` uses `describeApiError` (fallback "Failed to create sprint.").
- Unused members deleted: `cardGroup.focusedGroup`, `cardGroup.clearFocus` and its API function (Q3), `previousActions.reviewedCount`/`unreviewedCount`.
- `report.ts`: one private load used by both report fetches (FE-33 part); `copyAsMarkdown` stays until Step 11.
- Accept (spec AC1, AC2): `grep -rnE "(^|[^.A-Za-z])fetch\(" web/reflekt-web/src | grep -v "src/api/"` → only `PreviousActionsPanel.vue` (Step 8); `grep -rn "defineStore(" web/reflekt-web/src/stores | grep -v "() =>"` → no hits; `grep -rn "clearFocus" web/reflekt-web/src` → no hits; `type-check`, `build` green.
- Satisfies: spec FE-01, FE-02, FE-25, AC1 (stores), AC2.

### Step 7 — Store writes only in stores; one-line hub handlers (FE-06, FE-34 hub part, FE-39)

Follow pinia-patterns.

- `src/composables/useSprintHub.ts`: each of the 26 event listeners (`ParticipantJoined`, `ParticipantLeft`, `ParticipantKicked`, `PhaseChanged`, `CardCreated`, `CardUpdated`, `CardDeleted`, `EvidenceLinkFetched`, `VoteCast`, `VoteRetracted`, `GroupCreated`, `CardsGrouped`, `CardUngrouped`, `GroupDissolved`, `GroupTitleChanged`, `GroupFocused`, `GroupFocusCleared`, `GroupDiscussedChanged`, `ColumnConfigUpdated`, `SprintClosed`, `ReadinessChanged`, `ReadinessReset`, `ReadinessSnapshot`, `TimerUpdated`, `TimerSnapshot`, `ReviewStatusChanged`) is one store-action call and nothing else. Owners: participant (joined; kicked — the action removes the participant and, when it is the current one, sets a `kicked` state that `useRetroRoom` watches to run today's kick handling — token cleared, hub released, kicked screen; `useSprintHub` no longer takes an `onKicked` callback), sprint (phase changed — sets the phase and, when the new phase is not Writing, starts the background refresh of cards and groups (FE-39: the duplicate branch collapses), closed, column config), card (created, updated, deleted, link fetched), vote, cardGroup (the four structure events refresh in the background as today; title, focused, focus cleared, discussed), readiness, timer, previousActions. `ParticipantLeft` keeps an empty listener (spec Scope row 3 as amended; Decision 7, P5). The module registers its connection getter with `src/api/hub.ts`. `connect`/`disconnect` keep their order, ownership and calls; the one sanctioned edit is replacing the two `connection!` in `connect` with a local constant holding the built connection (FE-17, lint) — no logic change.
- `src/composables/useReadiness.ts` invokes through `src/api/hub.ts` (optimistic in Step 8); `src/composables/useTimerActions.ts` (new, mirrors `useReadiness`) owns the timer hub calls `TimerControls.vue` makes today, keeping "Not connected to server." and its toasts; `TimerControls.vue` stops importing `getHubConnection`; `catch (e: any)` → `unknown`.
- `src/composables/useRetroRoom.ts`: the `votesPerParticipant` write becomes a store action; `console.error` removed; it returns only what `RetroRoomView` uses — `src/views/RetroRoomView.vue` reads the report store itself where it used the returned `reportStore` (updated in this step). The second summary fetch in the data stage stays (Decision 13).
- `src/composables/useRetroBoardActions.ts`: edit, delete and evidence-link handlers stop writing `cardStore.cards` / `card.evidenceLinks` — their current logic (today's optimistic change and snapshot revert) moves unchanged into card-store actions, which Step 8 then rewrites to the per-item revert rule.
- Remove the two `useSprintHub` `!` here. The 20 non-null assertions today (18 in scripts, which lint sees, and 2 in templates, which it does not) are owned: Step 2 `AppHeader` template 1; Step 7 `useSprintHub` 2; Step 9 `RetroGroupItem` 8 (7 script, 1 template), `RetroColumnPanel` 1, `useRetroBoardActions` 2; Step 13 `CreateSprintForm` 6. Inventory grep (scripts and templates): `grep -rnoE "[]A-Za-z0-9_)]![].)[ ,;]" web/reflekt-web/src --include=*.vue --include=*.ts` → 20 today, 0 after Step 13.
- Accept: spec AC6 grep → no hits, and the widened grep `grep -rnE "Store\.[A-Za-z0-9_.]+(\[[^]]*\])?(\.push\(|\.unshift\(|\.splice\(| = [^=])" web/reflekt-web/src/composables web/reflekt-web/src/components web/reflekt-web/src/views` → no hits (18 today; it still cannot see writes through a local alias such as `group.title = …` — those are review items); `grep -rn "getHubConnection" web/reflekt-web/src | grep -v "useSprintHub.ts\|useRetroRoom.ts"` → no hits; each of the 26 event names appears once in `useSprintHub.ts`; `git diff` of `connect`/`disconnect` shows only the local-constant change.
- Satisfies: spec FE-06, FE-34 (hub), FE-39, AC6, AC9 (structure: handlers).

### Step 8 — Optimistic actions in the stores (FE-07, FE-03)

Follow pinia-patterns (§ Optimistic Update with Revert).

- **Card create** (notes and action items): a temporary card (negative id, the current participant, the payload) appears at once; the response replaces it unless the `CardCreated` echo already has (echo rule: an id already present updates in place; else a pending temporary card of mine with the same column type, card type and text is replaced; else the card is added). A pending card shows no edit or delete control (Decision 9). *Amended 2026-10-23 (questions.md Q5, architect applying PO-3): nor the add-link control — `canAttachLink` is false while the card is pending, and the Step 15 walkthrough's pending-card item checks it too.* On failure the temporary card goes and the store action rejects, so the input gets its text back (F36 draft rule). The inputs clear at submit, stay disabled while in flight, and refill on failure.
- **The revert rule, for every action below (Decision 16):** each optimistic change records the values it changed — per item **and per field** (a card's text and assignee; a card's presence; one link; a group's title, discussed flag or focus; a group's vote total; the user's own vote count on a group; a participant's ready flag). An authoritative payload **settles** only the values it actually replaces or establishes: `CardUpdated` (all of that card's fields and its link list), `CardDeleted` (the card's presence), a card or group refresh (everything it lists or omits), `EvidenceLinkFetched` (that one link only), `GroupTitleChanged` (title only), `GroupDiscussedChanged` (discussed only), `GroupFocused`/`GroupFocusCleared` (every group's focus), `VoteCast`/`VoteRetracted` (that group's total), a vote response (that group's total and the user's own count), readiness events (the flags and counts they carry), and a later request's response (the values it returns). On failure only unsettled values are undone, each by its own change; a settled value is left to the server's state.
- **Card edit and delete; evidence link attach and detach** (optimistic today, Decision 10): in the card store, under the rule. A failed delete puts the card back (with its latest known content) unless a `CardDeleted` or a refresh without it settled its presence, so a card deleted from a second tab is never resurrected; a failed link removal puts the link back unless a `CardUpdated` or a refresh settled the card's link list — an `EvidenceLinkFetched` for another link settles nothing about it.
- **Group title, discussed, focus**: local change, then reconcile from the response or the echo, under the rule. Focus changes two groups — the newly focused and the previously focused — and its revert restores both, each only while unsettled. The title write no longer reloads all groups (Decision 11).
- **Readiness toggle**: the readiness store flips the current participant and the count, invokes `ToggleReady` through `src/api/hub.ts`, and undoes its flip on failure only while unsettled; with no connection it does nothing, as today.
- **Votes**: both the group's total and the user's own count on that group take the request's ±1 as a recorded change (never a restored snapshot). A `VoteCast`/`VoteRetracted` `newCount` settles the total; a later successful vote response on the same group settles the total and the user's own count. A failure takes its own 1 back from each value only while that value is unsettled — so neither a peer's vote nor the user's own newer successful vote is overwritten; the remaining budget follows from the user's counts.
- **Previous-action review status** (FE-03): `previousActions` store action through the API; `src/components/retro/PreviousActionsPanel.vue` only calls it and shows an error toast on rejection (spec Decisions row 1).
- Server-authoritative actions (spec row 3 list) are unchanged.
- Accept: spec AC1 grep → no hits; spec AC3 (whole source: `X-Guest-Token` only under `src/api/`; no store import there) → holds; AC6 and the widened grep → no hits; `useRetroBoardActions.ts` holds no `cards =`, `.push(`, `evidenceLinks =` on store state; walkthrough "Optimistic behaviour" (Step 15/16), including the interleavings: a failed vote while another browser votes on the same group; a failed retract followed by a successful cast on the same group (own count and budget stay correct); a failed delete of a card the same user deleted from a second tab; a failed link removal while another link of that card is fetched; a failed focus; and a pending card showing no edit/delete, which it gains on save.
- Satisfies: spec FE-07, FE-03, AC1, AC3, AC9 (optimistic).

### Step 9 — Board panel and columns (FE-09, FE-40, FE-26, FE-17 rest)

Follow vue-component-architecture.

- `src/components/retro/RetroBoardPanel.vue` < 200 lines: the three note columns render by `v-for` over a computed column list; extract `RetroNoteInput.vue`, `RetroActionItemInput.vue`, `RetroReadyButton.vue` (all `src/components/retro/`); `isMobile` and its resize listener become CSS (tab-driven classes with `sm:` breakpoints); one shared drag-payload type in `src/types/card.ts`; no `isDark`.
- Merge `RetroColumnBody.vue` into `RetroColumnPanel.vue` (delete the former); drop `RetroColumnPanel`'s dead scoped `.card-list-*` CSS and the palette alias.
- FE-26: remove the dummy voting props from `cardItemProps`, the card-level `castVote`/`retractVote` handlers, and the `@vote`/`@retract-vote` listeners and props on `RetroCardItem.vue`.
- FE-23 board actions: `useRetroBoardActions` exports a typed `InjectionKey` for its action object; `RetroBoardPanel` provides it, and `RetroColumnPanel`, `RetroGroupItem` and `RetroCardItem` inject the handlers instead of re-emitting them level by level — their props shrink to the entity and its display flags (no handler-only events left on `RetroGroupItem`).
- `RetroGroupItem.vue` merges as `{ sourceGroupId, targetGroupId }` and the board resolves both groups from the store (no fabricated `CardGroup`). Its 8 `!` assertions (7 in the script, `group.cards[0]!.evidenceLinks` in the template), `RetroColumnPanel`'s 1 and `useRetroBoardActions`' 2 (`mergeGroups`' `cards[0]!`) become guards.
- Accept: `wc -l < web/reflekt-web/src/components/retro/RetroBoardPanel.vue` < 200; `test ! -e web/reflekt-web/src/components/retro/RetroColumnBody.vue`; `grep -rn "as unknown as" web/reflekt-web/src` → no hits; `grep -n "showVoting: false" web/reflekt-web/src/composables/useRetroBoardActions.ts` → no hits; `grep -rn "InjectionKey" web/reflekt-web/src/composables/useRetroBoardActions.ts` → present and `inject(` in the three child components.
- Satisfies: spec FE-09, FE-40, FE-26, FE-17, FE-23 (board actions), AC4, AC5 (first grep).

### Step 10 — Room view and phase header (FE-10, FE-23, FE-15 phase logic, FE-35 timers)

Follow vue-component-architecture.

- `src/composables/useRetroPhase.ts` (new): typed current phase, next phase from `PHASES`, the is-phase flags, closed, scrum master, show-readiness, show-votes-badge. It replaces `phaseNextMap` and every phase literal compare in views, components and composables (`RetroRoomView`, `useRetroBoardActions`, `RetroPhaseHeader`, `TimerDisplay`, `PhaseIndicator`, `CreateSprintForm` read `PHASES` or the composable).
- `src/views/RetroRoomView.vue` < 100 lines: extract `src/components/retro/AdvancePhaseDialog.vue` (check, warning, advance), `CloseSprintDialog.vue` and `RetroReportTabs.vue` (the report/board toggle); copy-markdown goes in Step 11.
- `src/components/retro/RetroPhaseHeader.vue` reads readiness, votes, timer, participants and phase from composables/stores; `defineProps` holds at most `sprintId`. The timer values come from the sprint summary's `writingTimerDefault`, `votingTimerDefault` and `discussingTimerDefault` — the sprint's own settings — read through the store or `useRetroPhase` instead of being threaded through props; `src/constants/columns.ts`'s defaults (Step 3) are only the fallback when the summary has none, as `?? 5`/`?? 3`/`?? 10` are today. Walkthrough: a sprint created with non-default timers shows them when the timer is stopped.
- Accept: `wc -l < web/reflekt-web/src/views/RetroRoomView.vue` < 100; `grep -rnE "'(Writing|Grouping|Voting|Discussing|Reporting)'" web/reflekt-web/src | grep -v "src/types/\|src/composables/useRetroPhase.ts\|src/stores/"` → no hits; `grep -c "phaseNextMap" -r web/reflekt-web/src` → 0.
- Satisfies: spec FE-10, FE-23, FE-15, FE-35 (timers), AC4, AC9 (structure: PHASES).

### Step 11 — One report component (FE-24, FE-33, FE-35 report part)

Follow vue-component-architecture.

- `src/components/reports/SprintReportPanel.vue` (with `ReportSection.vue` and a new `EvidenceChip.vue`) is the one report component, props-driven, showing the union (spec Decisions row 1, Q2): participants, every column with every card's text (single-card groups included) and discussed badges, action items, previous actions when present; its frame is `BaseCard`/`BaseBadge`/`BaseButton`.
- `src/components/retro/RetroReportPanel.vue` becomes a thin wrapper: loading and error states, then `SprintReportPanel` with `reportStore.report`. `src/views/ReportView.vue` renders `SprintReportPanel`.
- `src/format/reportMarkdown.ts`: the markdown as a pure function of the report (content unchanged); `src/composables/useCopyReport.ts` does the clipboard call and is used by `ReportView.vue` and `RetroRoomView.vue`; `report.copyAsMarkdown` is deleted. `truncateUrl` and `formatStatus` live once in `src/format/format.ts`, and so do the date formatters — one per output today, byte-identical (Decision 17): `formatDate` (`toLocaleDateString()` — both report panels and the markdown), `formatDateTime` (`toLocaleString()` — `SprintDetailPanel`, Step 13) and `formatShortDate` (`toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })` — `TeamCard`, Step 13).
- Accept: `grep -rln "SprintReportPanel" web/reflekt-web/src/views web/reflekt-web/src/components/retro` → `ReportView.vue` and `RetroReportPanel.vue`; `grep -rn "copyAsMarkdown\|truncateUrl = \|function truncateUrl" web/reflekt-web/src | grep -v "src/format/"` → no hits; the markdown for one report is identical before and after, proven by a throwaway script in `.claude/tmp/` (not a test, never staged, deleted after): it holds the old method's formatting body from `git show fb01e29:web/reflekt-web/src/stores/report.ts`, loads `/src/format/reportMarkdown.ts` through Vite's `createServer(…).ssrLoadModule` with the project's `vite.config.ts` (resolves `@/`; proven at plan time on `/src/stores/guest.ts` — the dependency-scan message printed on close is harmless, `optimizeDeps: { noDiscovery: true }` silences it), runs both on one fixture report and compares — the command and result recorded in `implementation.md`.
- Satisfies: spec FE-24, FE-33, FE-35, AC9 (structure: report).

### Step 12 — Tokens, `success` variant, no `@apply`, lucide icons (FE-19, FE-34, FE-36, FE-41 arrows)

Follow tailwind-theme.

- Raw palette colours (22 hits, 13 files) become tokens; add to `src/assets/main.css` the `success` solid/hover and `danger-hover` tokens the replacements need; `BaseButton.vue` gains `variant="success"`; no `isDark` colour branching anywhere.
- `TimerControls.vue`: no `<style>`/`@apply` — each button carries the same utilities its `@apply` class holds today, so the room's timer controls keep their look (FE-34's `BaseButton` suggestion narrowed by spec Decisions row 1; Decision 18); the `+1m/+2m/+5m` row by `v-for` over `[1, 2, 5]`.
- Inline `<svg>` in `TimerDisplay.vue` and `TeamCard.vue` → `Volume2`/`VolumeX`, `ChevronDown`, `EllipsisVertical`, `Info`; `PreviousActionsPanel.vue`'s unicode arrows → lucide chevrons.
- Keep F36's literal card-tint map and `bg-surface-focus` exactly.
- Accept (spec AC5): `grep -rnE "(bg|text|border|ring)-(red|green|blue|emerald|amber)-[0-9]" web/reflekt-web/src` → no hits; `grep -rn "@apply\|<svg" web/reflekt-web/src --include=*.vue` → no hits; `grep -rn "isDark" web/reflekt-web/src | grep -v "AppHeader.vue\|useTheme.ts"` → no hits.
- Satisfies: spec FE-19, FE-34, FE-36, FE-41, AC5.

### Step 13 — Base* adoption, page shells, dead code (FE-21, FE-22, FE-25, FE-41)

Follow create-vue-feature (§ Page Shell Pattern).

- The teams, sprints, welcome and report screens — and only those (spec Decisions row 1 as amended; Decision 18) — use `BaseButton`/`BaseInput`/`BaseBadge`/`BaseCard`: `CreateTeamForm`, `DeleteTeamDialog`, `TeamCard`, `CreateSprintForm`, `SprintList`, `SprintDetailPanel`, `TeamDashboardView`, `WelcomeView`, `SprintReportPanel`. `DeleteTeamDialog` becomes a `BaseModal` (Q2); status/state badges use `BaseBadge` with one status→variant map; the second "Sign in with Google" button goes with the dead branch below, leaving `WelcomeView`'s. `RetroGroupItem`'s rename controls and `RetroRoomView`'s tabs keep their look (FE-21 narrowed by the spec's screen list).
- Empty states in the page shells use `EmptyState` (spec Decisions row 1); error states keep F36's presentation. Delete `src/components/layout/PageToolbar.vue` and `src/components/ui/index.ts`; delete `TeamDashboardView`'s unreachable signed-out branch.
- `BaseInput.vue`: `defineModel`, keeping F36's `inheritAttrs` split, plus `id`/label wiring, `aria-invalid` and `aria-describedby` for its error text. `RetroMobileTabBar.vue`: `defineModel`. `BaseButton.vue`: drop the redundant `v-bind="$attrs"` (FE-22).
- `CreateTeamForm` takes a heading prop — "Create your first team" only when the user has no team. `SprintDetailPanel` uses `formatDateTime` and `TeamCard` `formatShortDate` (Step 11). `CreateSprintForm`'s 6 `!` assertions (and its `as string` casts) become guards.
- Accept: `test ! -e web/reflekt-web/src/components/layout/PageToolbar.vue && test ! -e web/reflekt-web/src/components/ui/index.ts`; `grep -rn "update:modelValue" web/reflekt-web/src/components/ui/BaseInput.vue web/reflekt-web/src/components/retro/RetroMobileTabBar.vue` → no hits; `grep -n "inheritAttrs: false" web/reflekt-web/src/components/ui/BaseInput.vue` → present; `grep -rn "toLocale" web/reflekt-web/src | grep -v "src/format/"` → no hits; `RetroGroupItem.vue` and `RetroRoomView.vue` are not among this step's changed files (listed in its `implementation.md` block).
- Satisfies: spec FE-21, FE-22, FE-25, FE-41.

### Step 14 — Accessibility (FE-29, FE-30)

Follow frontend-review (§ Stage 3: Accessibility).

- The sites FE-29/FE-30 list: accessible names on every icon-only control; `aria-expanded`/`aria-haspopup` on the avatar and team menus; toasts in a `role="status"` live region; `aria-current="step"` in `PhaseIndicator`; the mobile tab bar as `tablist`/`tab`/`tabpanel` with arrow keys and `aria-controls`; the clickable `div`/`li`/`h2` (TeamCard, SprintList, PreviousActionsPanel header, TimerDisplay, RetroColumnPanel title) become `<button>` or `RouterLink`; hover tooltips become `src/components/ui/BaseTooltip.vue`, opening on focus too; labels tied to inputs (CreateSprintForm, DeleteTeamDialog).
- `BaseModal.vue`: takes focus on open, keeps it inside, restores it on close, closes on Escape from anywhere, `aria-labelledby` its title.
- `src/views/SprintView.vue`: the kick confirmation is a `BaseModal` (no `confirm(`; do not name a handler `confirm(`).
- Accept: `grep -rn "confirm(" web/reflekt-web/src --include=*.vue` → no hits; `grep -rnE "aria-|role=" web/reflekt-web/src --include=*.vue | wc -l` well above today's 6, with each FE-29/FE-30 site listed in `implementation.md`; walkthrough "Keyboard".
- Satisfies: spec FE-29, FE-30, AC5 (`confirm(`), AC9 (keyboard).

### Step 15 — Verify: lint, build, greps, requests table, walkthrough, graph

No pattern skill.

- Lint to zero: `cd web/reflekt-web && npx eslint . && npx prettier --check src` → 0 errors (spec AC8 in this tree; warnings from the Vue `recommended` set do not count); fix remaining findings in code (no rule disabled, no `eslint-disable`).
- `type-check` and `build` green. Spec AC1–AC7 greps, the widened store-write grep (Step 7), and every step's Accept grep in its **final-state** form (Step 5's `CreateSprintForm` and Step 6's `PreviousActionsPanel` hits now absent), from the repo root, each command and result recorded in `implementation.md`; F36 AC1–AC5 (spec AC7) included. The non-null inventory grep (Step 7) → 0 hits in scripts and templates. Store writes through a local alias (`group.title = …`), which no grep sees, are listed by a read of `src/components`, `src/composables` and `src/views` — none may remain.
- The requests table (spec AC9): per API function — path, method, body, credentials, token — before (from today's code, `git show fb01e29:<store file>`) and after (devtools, or the module source where no browser is available, marked so); 23 token calls plus the removed clear-focus line.
- The structure greps (AC9): handlers one store call each; unions and `PHASES` imported by stores and phase logic; both report screens on `SprintReportPanel`.
- Run the AC9 walkthrough as far as this session can (API serving the built SPA, e.g. `dotnet run` on `src/Services/Reflekt/Reflekt.API`, or the `run` skill), light and dark, recording what was seen — the optimistic item includes the pending card showing no edit/delete until saved (Q4) and a sprint with non-default timers showing them (Step 10) — plus an **F36 regression pass** (spec "F36's outcomes" must survive): the chime sounds after a first click and stays silent when muted; a cold deep link to `team-sprints`, `sprint` and `report` shows the team name; roles — a guest, a Google participant and the team owner each see and do what they did before, edit/delete only on one's own cards, `sprint` signed-out redirects; each view's foreground load failure (offline and a 500) shows its error state and retries; a failed background refresh shows the room's banner and retries; a failed note save keeps its text; one Enter adds one note. Every item this session cannot observe (Google sign-in, two browsers, a screen reader, throttled network) goes under `OPERATOR ACTION REQUIRED` with exact steps — never recorded as seen.
- `delivery/change-notes.md` holds Notes 1–3. `graphify update .` from the repo root (record if not installed). AC8's `audit --omit=dev` and `run lint` are owed at merge (AR-1); AC10 is the handover complete run, not this step.
- Satisfies: spec AC1–AC9 (AC9 for what is observed here).

### Step 16 — Operator completes the AC9 walkthrough (Owner: operator)

Owner: operator — outside the build slices; `N/A (operator-owned)` at the done check.

- The operator performs every `OPERATOR ACTION REQUIRED` item from Step 15 and appends a `## Walkthrough — operator` block to `implementation.md`, including both report screens and the delete-team dialog in light and dark (Q2), the requests table rows marked "source, not devtools", the Step 8 interleavings and the F36 regression pass items not observed in Step 15.
- Until recorded, AC9 is open: the close lists it in `summary.md` as open acceptance, and the backlog change note reads built, AC9 walkthrough pending.
- Satisfies: spec AC9 (remaining items).

## Cross-Service Changes

None.

## Migration Notes

None.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| Route paths, names, `meta`, guard | `git diff -- web/reflekt-web/src/router/index.ts` changes only `component:` lines and imports | pinned (review diff) |
| Each request's path, method, body, credentials, token | Step 4 table vs the api modules; Step 15 requests table (before/after) | pinned (review) + accepted-gap (devtools half, operator) |
| Hub event and method names | the 26 event names each once in `useSprintHub.ts`; `JoinSprint`, `ToggleReady`, `PauseTimer`, `ResumeTimer`, `ResetTimer`, `AddTime` each still invoked | pinned (grep) |
| Hub connection lifecycle | `git diff` of `connect`/`disconnect` in `useSprintHub.ts`: only the local constant replacing `connection!` (Step 7) | pinned (review diff) |
| Storage keys | Step 5 greps: keys only in `src/storage/`, names unchanged | pinned (grep) |
| F36 outcomes | F36 AC1–AC5 greps and the F36 regression pass (Steps 15–16) | pinned (grep) + accepted-gap (manual) |
| Roles' permissions; edit/delete only on own cards | `canMutateCard` diff (only the pending-card clause added); F36 regression pass "roles" (Steps 15–16) | pinned (review) + accepted-gap (manual) |
| Server-authoritative actions stay non-optimistic | diff review: their store actions change state only after the response or reload | pinned (review diff) |
| Visual appearance outside spec Decisions row 1 | walkthrough light/dark | accepted-gap (manual — no visual tests by campaign rule) |
| `package.json`, `package-lock.json` untouched | `git status --porcelain` of both → empty | pinned |
| `type-check` and `build` green | every step; handover complete run | pinned |

## Testing Strategy

No new tests (campaign rule). Proof: the Accept greps per step; spec AC1–AC8 greps; lint from the `--no-save` install; `type-check` + `build` per step; diff review against the Must-NOT-Change table; the recorded AC9 walkthrough (Step 15, completed by the operator in Step 16); the backend suites in `roles.full` stay green.

## KB Impact

None — no `docs/kb/`, no anchored set (`docs/conventions/anchored-set.md` absent).

## Decisions

| # | Decision | Why | Rejected alternative | Confidence | Status |
|---|---|---|---|---|---|
| 1 | Lint tooling lands as committed configs plus a `--no-save` install; dev-dependencies, the `lint` script and the `verify.json` full-role change go to change notes 1–2, applied after F37's. See plan P4. | No unit edits the package files (AR-1); adding lint to `roles.full` now would fail every complete run until the packages land. `--no-save` proven at plan time to leave both files byte-identical. | Edit `verify.json` now; skip lint until merge. | high — executed at plan time | decided |
| 2 | `eslint.config.js` (not `.ts`) with flat `recommended` Vue rules, TypeScript recommended, `skip-formatting`, and three repo rules: no `!`, no `fetch` outside `src/api`, no store import inside `src/api`. | `tsconfig.node.json` includes `eslint.config.*`; the three rules turn AC1, AC3 and FE-17 into lint errors. Probe on today's code: 66 errors, all expected. | Essential-only rules; rules without the repo checks. | medium — rule set is a taste call | decided |
| 3 | Prettier: no semicolons, single quotes, width 100, trailing commas. | Spec fixes no semicolons; the rest matches the majority of today's files. | Prettier defaults. | medium | decided |
| 4 | The pre-F38 error reader lives in `src/api/client.ts` as one marked function; F39 owns all three error consumers. See plan P1. | AR-2; keeps today's text in this tree and F38's after. | Read F38's shape only. | high — ruled by AR-2 | decided |
| 5 | Hub invokes go through `src/api/hub.ts`, whose connection getter `useSprintHub` registers; the lifecycle is untouched. | Stores (readiness) need the hub without importing a composable; F36's deferred row pins `connect`/`disconnect`. | Stores import `getHubConnection`. | high | decided |
| 6 | `App.vue`'s `fetchMe` call is removed; the guard covers every navigation. | Spec FE-38 + AC9 "one `/auth/me`"; the in-flight share already exists. | Keep both callers. | high | decided |
| 7 | `ParticipantLeft` keeps an empty listener with no store action; spec Scope row 3 and AC9 amended to say so. A kick's effect on the room follows a store `kicked` state that `useRetroRoom` watches, so its handler stays one call. See plan P5. | The server sends it (`SprintHub.cs:292`); without a listener the SignalR client warns on every leave; no state changes on leave today, and adding one would be new behaviour (R1-03). | A no-op store action; an action that removes the participant. | medium — a writer's amendment of the spec's acceptance | decided |
| 8 | Cross-folder imports use `@/`; same-folder `./` imports stay. | FE-37 targets the climbing `../` imports; `./` is idiomatic and unambiguous. | `@/` everywhere. | medium | decided |
| 9 | A pending (temporary) card shows no edit or delete control until the server's card replaces it — derived from spec Scope row 3 (the temporary card) and "What must not change" (edit/delete only on the caller's own cards within the server's rules; a pending card is not yet one the server knows), not an addition. See plan P6. | Its id is negative, so an edit or delete would hit the API with it — the controls would only fail. Same rule shape as F36's `canMutateCard`. | Queue edits on a pending card; show controls that fail. | high — ruled by PO-3 | decided |
| 10 | Evidence-link attach and detach stay optimistic (as today), moved into the card store with per-item revert. | Spec lists neither; today's behaviour is optimistic; moving it keeps "only stores mutate". | Make them server-authoritative. | high | decided |
| 11 | The group-title write no longer reloads all groups; the optimistic value, the response and the `GroupTitleChanged` echo keep it current. | Spec makes rename optimistic; a reload after it is redundant and can flicker the board. | Keep F36's reload after the title write. | medium | decided |
| 12 | `PHASES` lives in `src/types/domain.ts` without icons; icons stay in `PhaseIndicator`. | Types stay framework-free (no lucide import in `types/`). | Icons in `PHASES` (FE-15's list). | high | decided |
| 13 | The data-stage summary fetch in `useRetroRoom` stays; FE-39's parallel load is already F36's. | The second fetch picks up the joined participant and scrum-master flag after the join. | Drop it (FE-39). | medium | decided |
| 14 | Shared helpers go in purpose-named folders `src/format/`, `src/constants/`, `src/storage/`, not `utils/`. | `CLAUDE.md` guardrail forbids god folders (`Utils/`); FE-33/FE-35 suggest `utils/`. | `src/utils/`. | high | decided |
| 15 | `SprintReportPanel` (+ `ReportSection`, `EvidenceChip`) is the one report component; `RetroReportPanel` is a thin loading/error wrapper. | FE-24's suggested fix; keeps the standalone page's frame (Q2). | `RetroReportPanel` as the one component. | high | decided |
| 16 | One revert rule for every optimistic action, tracked per item and per field: an authoritative payload settles only the values it replaces or establishes; a failure undoes only its own unsettled changes, by delta. | Absence or a stale snapshot cannot tell "my change" from "the server's state": a delta revert after an absolute `newCount` drops a peer's vote, re-inserting an absent card resurrects one deleted elsewhere (R1-01, R1-02, R1-10); a partial event settling a whole card loses a link the server kept, and restoring an old personal count overwrites a newer successful vote (R2-01, R2-02). | Revert while the item still holds the optimistic value (round-1 draft); settle the whole item on any event about it (round-1 fold). | high — each interleaving is concrete | decided |
| 17 | Three date formatters, one per output today (`formatDate`, `formatDateTime`, `formatShortDate`), each byte-identical to its call sites. | One `formatDate` would change the team card and sprint panel dates — a visible change outside spec Decisions row 1 (R1-11). | One unified formatter. | high | decided |
| 18 | Base* adoption stops at the four screens the spec names; the room's own controls — `RetroGroupItem`'s rename controls, `RetroRoomView`'s tabs, `TimerControls`' buttons — keep their look (the timer buttons swap `@apply` for the same utilities), and error states keep F36's. The spec's Decisions row 1 now says so. | FE-21's location list and FE-34's `BaseButton` suggestion reach into the room, but the spec's required outcome and Q2's list name only teams, sprints, welcome and report (R1-08, R2-05). | Adopt Base* in the room too, amending the visible-changes list. | medium — narrows two findings' suggested fixes | decided |

## Decisions and why

### P1 — Read both error bodies until F38 merges
- **By:** campaign shared architect (AR-2), relayed by the campaign coordinator — not the owner; owner to rule: no
- **Decided:** `api/client.ts` reads F38's body first and today's FastEndpoints body through one marked reader; F39 owns `CreateTeamForm`, `CreateSprintForm` and `DeleteTeamDialog`; F39 merges before F38; a change note adds the backlog row that removes the reader after F38.
- **Why:** F39 is built on F36 only, so this tree sends the old body; reading only F38's shape would show generic text until F38 merged.
- **Turned down:** F38's shape only (visible regression); waiting for F38 (stalls F39).
- **Rests on:** F38 not changing the three forms' error text.
- **Confidence:** high — ruled.
- **Source:** `delivery/questions.md` Q1, 2026-10-23.
- **State:** current

### P2 — The report and Base* look changes are listed
- **By:** campaign PO (PO-1), relayed by the campaign coordinator — presumed (proceed-default), not user-confirmed; owner to rule: yes
- **Decided:** spec Decisions row 1 now lists one report look with the union of both screens' content, Base* inputs and badges on the teams, sprints, welcome and report screens, and the delete-team dialog as a `BaseModal`.
- **Why:** FE-21 and FE-24 require them; listing them keeps "nothing else changes" honest.
- **Turned down:** a props switch keeping two report renderings; adopting only `BaseButton`.
- **Rests on:** the owner accepting the visible changes; an owner "no" reopens Steps 11 and 13.
- **Confidence:** medium — presumed.
- **Source:** `delivery/questions.md` Q2, 2026-10-23.
- **State:** current

### P3 — Clear focus is deleted
- **By:** campaign PO (PO-2), relayed by the campaign coordinator — not the owner; owner to rule: no
- **Decided:** delete `clearFocus` and its API call; `GroupFocusCleared` keeps its store action; 23 token calls.
- **Why:** no control calls it; keeping it is dead code; adding a control is new behaviour.
- **Turned down:** keep it uncalled.
- **Rests on:** no hidden caller (grep: none).
- **Confidence:** high.
- **Source:** `delivery/questions.md` Q3, 2026-10-23.
- **State:** current

### P4 — Lint joins the full role through the change note
- **By:** architect
- **Decided:** the `verify.json` full-role change and its re-timing ship as change note 2, after the package note; this tree lints from a `--no-save` install.
- **Why:** architect's reading — spec Decisions row 3 asks for lint in `roles.full`, but no `lint` script can exist in this tree (AR-1), so the role would fail every complete run.
- **Turned down:** edit `verify.json` now.
- **Rests on:** the notes being applied in order at merge.
- **Confidence:** high — executed at plan time.
- **Source:** Phase 2 planning, 2026-10-23.
- **State:** current

### P5 — `ParticipantLeft` keeps an empty listener
- **By:** architect
- **Decided:** the one hub event with no store action; its listener stays empty. As the tech-spec's definer, the architect amended spec Scope row 3 ("every SignalR event that changes client state has a store action") and AC9's structure item to carry the exception; the kick's room effect moves behind a watched store state so its handler stays one call.
- **Why:** architect's reading — the spec's aim is that store state is written only by store actions; this event writes nothing today, and removing the listener makes the SignalR client warn on every leave. Round 1 (R1-03) showed the unamended spec made AC9 unpassable.
- **Turned down:** remove the listener; a no-op store action (code that exists only to satisfy a check); an action that drops the participant (new behaviour).
- **Rests on:** the server sending `ParticipantLeft` (`SprintHub.cs:292`).
- **Confidence:** medium — a writer's amendment of the spec's acceptance.
- **Source:** Phase 2 planning, 2026-10-23; plan review round 1 (R1-03), 2026-10-23.
- **State:** current

### P6 — A pending card has no edit or delete control
- **By:** campaign PO (PO-3), relayed by the campaign coordinator — not the owner; owner to rule: no
- **Decided:** keep Decision 9; it is derived from spec Scope row 3 (the temporary card) and "What must not change" (edit/delete only on the caller's own cards within the server's rules), not an `Adds:` row. AC9's optimistic walkthrough notes the pending card has no edit/delete and gains them on save.
- **Why:** the controls on a pending card could only fail — its id is not one the server knows.
- **Turned down:** drop the rule and show controls that fail.
- **Rests on:** the temporary card staying in the spec.
- **Confidence:** high.
- **Source:** `delivery/questions.md` Q4, 2026-10-23.
- **State:** current

## Plan Review

Round 1 (2026-10-23, Codex primary NO-GO + Sonnet second reader ACCEPT): 19 merged findings (4 HIGH, 7 MEDIUM, 8 LOW), 18 folded, 1 LOW waived — see `review-critic.md` § Round 1 (plan) `### Fold — round 1`. Spec amended for R1-03 (Scope row 3, AC9) and R1-08 (Decisions row 1). 

Round 2 (2026-10-23, Codex primary alone, full read): round-1 fold verified, R1-05 partial; 7 merged findings (2 HIGH, 3 MEDIUM, 2 LOW), all 7 folded `fixed-real` — see `review-critic.md` § Round 2 `### Fold — round 2`. The revert rule is now per field and per link (R2-01) and the vote revert is by delta on both counts (R2-02); timers read the sprint's own settings (R2-03); all 20 non-null sites are owned (R2-04); `TimerControls` keeps its look (R2-05). Q4 (PO-3) recorded Decision 9 as derived, not `Adds:` — `## Decisions` holds no tagged row, so no plan departures are owed; spec AC9's optimistic item notes the pending card's controls. No round 3 (no CRITICAL).

## Open Questions

None.
