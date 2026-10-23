# F36 — Frontend Visual & UX Defects — Implementation

**Model:** claude-opus-5-5[1m]

**Verification setup (announced before it ran):** `npm --prefix web/reflekt-web ci` from the lockfile — the worktree has no `node_modules`, and the plan's `type-check`/`build` need it; expected ~1–3 min; part of the declared `roles.full`; `package.json` / `package-lock.json` checked unchanged with `git status` afterwards (coordinator ruling).

## Step 1 — Corners and report width use the theme's utilities (FE-04, FE-11)
**Files:** one mechanical rewrite across `web/reflekt-web/src`, applied by one regex: `rounded-[--radius-X]` → `rounded-X` (X = sm ×23, md ×18, lg ×1, full ×2), `rounded-t-[--radius-md]` → `rounded-t-md` ×4, `rounded-b-[--radius-md]` → `rounded-b-md` ×1 — 49 sites; `max-w-[--page-max-width]` → `max-w-6xl` ×1. Files and site counts:
- `web/reflekt-web/src/components/AppHeader.vue` — 3 radius sites
- `web/reflekt-web/src/components/layout/PageLayout.vue` — content wrapper `max-w-6xl` (FE-11)
- `web/reflekt-web/src/components/retro/PreviousActionsPanel.vue` — 2
- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue` — 2
- `web/reflekt-web/src/components/retro/RetroCardItem.vue` — 4
- `web/reflekt-web/src/components/retro/RetroColumnPanel.vue` — 2
- `web/reflekt-web/src/components/retro/RetroGroupItem.vue` — 6
- `web/reflekt-web/src/components/retro/RetroJoinForm.vue` — 1
- `web/reflekt-web/src/components/retro/RetroMobileTabBar.vue` — 4
- `web/reflekt-web/src/components/retro/RetroPhaseHeader.vue` — 4
- `web/reflekt-web/src/components/retro/RetroReportPanel.vue` — 5
- `web/reflekt-web/src/components/retro/TimerControls.vue` — 3 (template line 3 plus the two `@apply` lines in `<style scoped>`; `@apply` itself kept — plan Decision 10)
- `web/reflekt-web/src/components/ui/BaseBadge.vue` — 1
- `web/reflekt-web/src/components/ui/BaseButton.vue` — 3
- `web/reflekt-web/src/components/ui/BaseCard.vue` — 1
- `web/reflekt-web/src/components/ui/BaseInput.vue` — 1
- `web/reflekt-web/src/components/ui/BaseModal.vue` — 1
- `web/reflekt-web/src/components/ui/ToastContainer.vue` — 2
- `web/reflekt-web/src/views/RetroRoomView.vue` — 4
**Skills:** tailwind-theme
**Decisions:** None
**Deviations:** None. Housekeeping, not a code change: the edit was applied with Git Bash `sed -i`, which rewrote the 19 files' working-tree line endings from CRLF to LF; they were converted back to CRLF straight away (`git ls-files --eol` → all `w/crlf`, as untouched files are), so the diff carries only the class changes.
**Verification:**
- `npm --prefix web/reflekt-web ci` → added 214 packages; `git status --short web/reflekt-web/package.json web/reflekt-web/package-lock.json` → empty (unchanged).
- `npm --prefix web/reflekt-web run build` (runs `vue-tsc --build` type-check + `vite build`) → green, built in 797 ms.
- AC1: `grep -rnE "\[--[a-z-]+\]" web/reflekt-web/src` → no hits (exit 1).
- AC3 (first half): `wwwroot/assets/index-BCqvEIqi.css` contains `border-radius:var(--radius-md)`; also `.rounded-t-md{border-top-left-radius:var(--radius-md);…}`, `.rounded-b-md{…var(--radius-md)}`, the scoped `.timer-btn[data-v-…]{…border-radius:var(--radius-sm,4px)…}`, and `max-width:var(--container-6xl)`.
- Step close: `verify-run.js --profile fast --tied --changed {the 19 files} --slug F36-FrontendDefects` → pass, run `mvkgziac-6ae6bb7e` (no tied tests for `.vue` files — fell back to `roles.unit`, the .NET unit suite, 8.3 s); `label-tests.js --run mvkgziac-6ae6bb7e` → timed no test, nothing to label.

## Step 2 — Light page background, focused-group surface, card tints (FE-05, FE-20, FE-12)
**Files:**
- `web/reflekt-web/index.html` — removed the inline `style` from `<body>` (the FE-05 defect: it beat the stylesheet, so light theme kept a dark page); added a `<head>` `<style>` with `html { background: #0a0a0f; }` and `html.light { background: #f4f4f8; }` (the `--surface-page` values), placed after `<title>` and before the unchanged blocking theme script.
- `web/reflekt-web/src/assets/main.css` — new theme-aware token: raw `--surface-focus` in `:root` (`color-mix(in srgb, #6366f1 15%, #13131a)`, compiles to `#1f1f3a`) and in `.light` (`#eef2ff`, today's light look = `primary-50`), each beside `--surface-elevated`; `@theme` alias `--color-surface-focus: var(--surface-focus)` beside `--color-surface-raised`.
- `web/reflekt-web/src/components/retro/RetroGroupItem.vue` — `groupBgClass` focused branch `bg-primary-50` → `bg-surface-focus` (ring `ring-2 ring-primary-500` unchanged; the other two branches untouched).
- `web/reflekt-web/src/components/retro/RetroCardItem.vue` — `cardBgClass` no longer builds `bg-col-${colour}-light/30`; a literal `cardTintClasses` map (`green|red|amber|blue|purple|teal|pink|slate` → `bg-col-{colour}-light/30`, `action-items` → `bg-action-items-light/30`) is looked up instead. No colour → `bg-surface` (as today); a colour outside the map → `undefined`, i.e. no background class (as today, where the built class rendered nothing). One constraint comment states why the names are written out (Tailwind emits only classes it finds verbatim); the old narration comment went.
**Skills:** tailwind-theme
**Decisions:** Map keys are the palette from `PALETTE_COLORS` (`RetroColumnPanel.vue:77`, `CreateSprintForm.vue:109`), so every column colour the app can set has an entry. `<style>` placed before the theme script — order is irrelevant to the outcome since the script only sets the class on `<html>`.
**Deviations:** Departs from tailwind-theme § White Flash Prevention (inline `<body>` colour) — plan Decision 11, required by spec FE-05; the skill drift is already in `lessons.md` § Skill Gaps. The four other `primary-50/100` sites (`AppHeader.vue` / `RetroJoinForm.vue` avatar chips, `PhaseIndicator.vue` ring, `RetroColumnPanel.vue` drag-over) are left as the plan says, pending the Step 13/14 dark-readability check.
**Verification:**
- `npm --prefix web/reflekt-web run build` (type-check + vite build) → green, built in 882 ms.
- AC2: `grep -n "<body" web/reflekt-web/index.html` → `25:  <body>` (no `style=`); `<head>` lines 12–13 carry `html { … }` and `html.light { … }`; the built `wwwroot/index.html` carries both and a bare `<body>`.
- AC3 (second half): `wwwroot/assets/index-L-xP9VGk.css` contains `.bg-col-{green,red,amber,blue,purple,teal,pink,slate}-light\/30{` and `.bg-action-items-light\/30{` — all nine; also `.bg-surface-focus{background-color:var(--color-surface-focus)}` and `--surface-focus:#1f1f3a` / `--surface-focus:#eef2ff`.
- Step close: `verify-run.js --profile fast --tied --changed {the 4 files} --slug F36-FrontendDefects` → pass, run `mvkh1xoc-df46008d` (fell back to `roles.unit`, 9.4 s); `label-tests.js` → timed no test, nothing to label.

## Step 3 — `BaseInput` binds attributes once (FE-22 part)
**Files:**
- `web/reflekt-web/src/components/ui/BaseInput.vue` — `defineOptions({ inheritAttrs: false })` stops the automatic pass-through of attributes to the root `<div>` (with `v-bind="$attrs"` also on the `<input>`, a parent's `@keydown.enter` was bound twice). The wrapper `<div>` now takes `attrs.class` and `attrs.style` explicitly (so `class="flex-1"` still sizes the wrapper in `RetroBoardPanel`); the `<input>` takes `v-bind="inputAttrs()"`, i.e. every other attribute and listener (`onKeydown`, `placeholder` from callers, etc.), in the same position `v-bind="$attrs"` held. Props, emits, `inputClasses` and the error line unchanged; no `defineModel`/ARIA (F39).
**Skills:** vue-patterns
**Decisions:** `inputAttrs()` is a plain function called from the render, not a `computed`: `useAttrs()` is not reactive, so a computed would cache the first call's attributes; the render re-runs whenever the parent passes new attributes, so a function read there is always current. One constraint comment states this. `attrs.style` is cast to `StyleValue` in the template (attrs are typed `unknown`).
**Deviations:** None
**Verification:**
- `npm --prefix web/reflekt-web run build` (vue-tsc type-check + vite build) → green, built in 1.05 s.
- AC5 (BaseInput part): `grep -n "inheritAttrs: false" web/reflekt-web/src/components/ui/BaseInput.vue` → `20:defineOptions({ inheritAttrs: false });`.
- "One Enter adds exactly one note" is a walkthrough item (Step 13/14) — not observed here; it also depends on Step 8's in-flight guard.
- Step close: `verify-run.js --profile fast --tied --changed web/reflekt-web/src/components/ui/BaseInput.vue --slug F36-FrontendDefects` → pass, run `mvkh3xj5-78755d7e` (fell back to `roles.unit`, 13.9 s); `label-tests.js` → timed no test, nothing to label.

## Step 4 — Load contract: `sprint`, `teams`, `report`, `auth` stores (FE-08 part)
**Files:**
- `web/reflekt-web/src/stores/sprint.ts` — three resources, each with its own status: `sprintsLoading`/`sprintsError`, `currentSprintLoading`/`currentSprintError`, `currentSummaryLoading`/`currentSummaryError`. `fetchTeamSprints`, `fetchSprint`, `fetchSprintSummary` now clear the resource's data and error and set loading on start; a non-OK response sets the error (`Failed to load sprints`; `Sprint not found` on 404 / `Failed to load sprint` for the two single-sprint loads); a rejected `fetch` sets `Can't reach the server`; nothing throws, and no failure maps to `[]`. Each commit (data, error, loading, and `loaded`) is guarded by a module-level per-resource request counter, so only the latest request commits. `loaded` kept (set when the latest list load ends). Mutating actions untouched (Decision 12).
- `web/reflekt-web/src/stores/teams.ts` — new `loading` / `error`; `fetchMyTeams` follows the same contract (`Failed to load teams` / `Can't reach the server`), latest-request guarded; `loaded` set when the latest load ends. `createTeam` / `deleteTeam` unchanged.
- `web/reflekt-web/src/stores/report.ts` — `fetchReport` and `fetchParticipantReport` share one resource (`report`) and one request counter. Both now clear `report` on start (`_startReportLoad`), catch a rejected `fetch` (`Can't reach the server` — before, it escaped the `finally`), and commit through `_commitReport`, which keeps the existing 404 / 401-403 / other messages (moved into `reportFailureMessage`) and checks the latest request before committing. The two `fetch(` calls (URLs, headers, `credentials`) are unchanged lines.
- `web/reflekt-web/src/stores/auth.ts` — new `error`; `fetchMe` never rejects: a rejected `fetch` leaves `user = null`, sets `error = "Can't reach the server"` (for the Step 6 App notice) and still sets `loaded`; any HTTP response keeps today's behaviour (`ok` → user, otherwise signed out) and clears `error`.
**Skills:** pinia-patterns
**Decisions:**
- Request counters are module-level `let`s, not store state: they are bookkeeping no view reads, and a Pinia store is a singleton per app, so a module counter is per store.
- `auth.fetchMe` meets "latest request wins" by sharing one in-flight request instead of a counter: the router guard (`router/index.ts:69`) and `App.vue:16` both call it on a cold load and each reads `isSignedIn` after its own `await`. With a counter, the guard's older request would be discarded and the guard would read `user = null` before the newer one committed — a signed-in user sent away from a `requiresAuth` route. Concurrent callers now await the same promise, so there is one request and both read its result. One constraint comment states why. `fetchMe` does not clear `user` on start — the spec's clear-on-start list does not include `auth`, and a guard reading mid-load would see a false sign-out.
- Error text: `Failed to load {resource}` for an HTTP failure (the wording `report.ts` already used), `Can't reach the server` for a rejected `fetch` (the spec's wording for `fetchMe`); 404 on a single sprint → `Sprint not found`, as `report.ts` already says.
**Deviations:** Departs from pinia-patterns (setup stores; error from a thrown `Error`) — plan Decision 12: the stores stay Options-style, mutating actions keep their throws. Clearing `currentSummary` on start of `fetchSprintSummary` means the room's current double call (`useRetroRoom.ts:54`, `:85`) blanks the summary between the two loads; Step 7 rewrites that entry into stages.
**Verification:**
- `npm --prefix web/reflekt-web run type-check` (`vue-tsc --build`) → green.
- Acceptance (review of the diff): `grep -nE "res\.ok \?|if \(!res\.ok\) return|if \(res\.ok\) \{"` over the four stores → one hit, `auth.ts:28` `this.user = res.ok ? (await res.json()) as User : null` — the plan's sanctioned "any HTTP response keeps today's behaviour" path (not a `: []` mapping, and not a load-contract store). `git diff -- web/reflekt-web/src/stores | grep "^[-+].*fetch("` → the five changed `fetch(` lines differ only in indentation (moved into `try`).
- Step close: `verify-run.js --profile fast --tied --changed {the 4 stores} --slug F36-FrontendDefects` → pass, run `mvkh8xfa-ad0d29c5` (fell back to `roles.unit`, 4.8 s); `label-tests.js` → timed no test, nothing to label.

## Step 5 — Load contract: room stores, and the room's refreshes (FE-08 part)
**Files:**
- `web/reflekt-web/src/stores/card.ts` — new `loading` / `error`; `fetchCards(sprintId, { background = false } = {})`. A foreground load clears `cards` and `error` and sets `loading`. A background refresh keeps `cards` and leaves `error` alone (so the Step 7 banner stays up until a refresh succeeds). Failure sets `error` (`Failed to load cards` / `Can't reach the server`); success replaces `cards` and clears `error`; latest request wins (module counter). A constraint comment states the background rule.
- `web/reflekt-web/src/stores/cardGroup.ts` — the same for `fetchGroups` (`groups`, `Failed to load groups`); `voteStore.initVoteCounts` runs only on a committed success. The five post-write reloads (`createGroup`, `addCardToGroup`, `removeCardFromGroup`, `dissolveGroup`, `updateGroupTitle`) pass `{ background: true }`. `fetchGroups` never throws, so a failed reload sets `error` and the write still resolves.
- `web/reflekt-web/src/composables/useSprintHub.ts` — the eight `fetchCards` / `fetchGroups` calls (lines 67, 68, 70, 71, 108, 112, 116, 120) now pass `{ background: true }`. That is the only edit; the handlers are unchanged.
- `web/reflekt-web/src/stores/participant.ts` — two resources. `fetchParticipants` (→ `participants`, `participantsLoading` / `participantsError`). `fetchMe(sprintId)` (→ `currentParticipant`, `meLoading` / `meError`) no longer throws. On a non-OK response it sets `meError` and keeps the HTTP status in `meStatus` (a network failure leaves `meStatus = null`), so the room can still clear a stale token on 401. `join`, `joinWithGoogle`, `kickParticipant` unchanged.
- `web/reflekt-web/src/stores/policy.ts` — new `loading` / `error`; `fetch` clears `matrix` to `{}` on start, sets `error` on failure (`Failed to load the retro rules` / `Can't reach the server`), and commits only from the latest request.
- `web/reflekt-web/src/stores/vote.ts` — new `loading` / `error`; `fetchMyVotes` keeps the no-token early return first. After that it follows the contract (`myVotes`, `Failed to load your votes`).
- `web/reflekt-web/src/stores/previousActions.ts` — new `error` (it already had `loading`). `fetchPreviousActions` clears `items`, `previousSprintId` and `previousSprintName` on start. A non-OK response now sets `error` instead of mapping to `[]`. The previous-sprint id and name come from the first item, else `null` (same values as before, written with `?.` / `??`).
**Skills:** pinia-patterns
**Decisions:**
- Background is a `{ background }` options object, not a positional boolean, so the hub and post-write call sites read clearly (`fetchGroups(sprintId, { background: true })`).
- A background refresh does not set `loading`. The room's loading state must not hide the board during a refresh. If it supersedes an in-flight foreground load, its `finally` still clears `loading`, because it is the latest request.
- `cards` / `groups` keep one `error` each. Before entry, only foreground loads run, and the room's stage check reads that error. After entry, only background refreshes run, so the same field drives the Step 7 banner.
**Deviations:** Departs from pinia-patterns (setup stores) — plan Decision 12, as Step 4. Interim state until Step 7: `useRetroRoom.ts:106` still reads a throw from `participantStore.fetchMe` (`try { await participantStore.fetchMe(sprintId); await enterRoom(); return } catch …`). With `fetchMe` no longer throwing, a guest whose stored token is refused would go on into the room until Step 7 replaces that branch with the staged entry, which reads `meError` / `meStatus`. The plan assigns `useRetroRoom.ts` to Step 7, so it is not touched here.
**Verification:**
- `npm --prefix web/reflekt-web run type-check` → green.
- `grep -nE "fetch(Cards|Groups)\(" web/reflekt-web/src/composables/useSprintHub.ts` → 8 hits, each `(sprintId, { background: true })`; `git diff --stat` of that file → 8 insertions, 8 deletions (only those calls).
- `grep -nE "res\.ok \?|if \(!res\.ok\) return|if \(res\.ok\) \{"` over the six stores → no hits. In the store diff, every changed `fetch(` line changes only its indentation (moved into `try`).
- Step close: `verify-run.js --profile fast --tied --changed {the 6 stores + useSprintHub.ts} --slug F36-FrontendDefects` → pass, run `mvkhcv68-fb493ac4` (fell back to `roles.unit`, 6.5 s); `label-tests.js` → timed no test, nothing to label.

## Step 6 — Error states with retry in the views; App unreachable-server notice (FE-08 part)
**Files:**
- `web/reflekt-web/src/views/TeamDashboardView.vue` — signed-in branch now opens with an error state: `EmptyState` (title = `teamsStore.error`) with a `BaseButton` "Retry" → `fetchMyTeams()`. On an error the create-first-team form never shows. The empty (no teams) branch also requires `!teamsStore.loading`, so a retry does not flash the create form (`vue.md` § Reactivity Gotchas).
- `web/reflekt-web/src/components/sprints/SprintList.vue` — new props `loading` and `error`, and a `retry` emit. Order is error (`EmptyState` + Retry) → "No sprints yet" (only when not loading) → list. The heading and the create form are unchanged.
- `web/reflekt-web/src/views/SprintListView.vue` — passes `sprintStore.sprintsLoading` / `sprintsError` to the list; `@retry` → `fetchTeamSprints(teamId)`.
- `web/reflekt-web/src/views/SprintView.vue` — loading, error and content are now distinct. "Loading sprint…" shows only while there is no sprint and no error. The error state is `EmptyState` + Retry, and its message is `currentSprintError` (404 → `Sprint not found`) or the participants error. The panel shows once the sprint is held. The `onMounted` body moved into `load()`, which both mount and Retry run.
- `web/reflekt-web/src/views/ReportView.vue` — `error` takes precedence over a held report (`:empty="!!reportStore.error || !report"`, panel only `v-if="report && !reportStore.error"`). The existing error line gains a `BaseButton` "Retry" → `fetchReport(teamId, sprintId)`, shown only on an error (not on the plain "Report not found." empty).
- `web/reflekt-web/src/App.vue` — a one-line `role="status"` notice under the header while `authStore.error` is set, built from tokens only (`bg-badge-warning-bg text-badge-warning-text`), reading "Can't reach the server.". `<RouterView :key="route.path" />` (Decision 14) has a constraint comment on why it is keyed. Rewritten with `Write` and then restored to CRLF.
**Skills:** create-vue-feature
**Decisions:**
- `SprintList` gets the state as props, not via the view: the list owns the "No sprints yet" line the error must replace, and it stays a props-only component.
- `SprintView` counts the participants error only when this view started that load (`participantsRequested`): the participant store is shared with the retro room, and a room error must not show on a non-Active sprint page. One constraint comment states this.
- `ReportView` keeps its existing error card (danger text) and adds the Retry button to it rather than switching to `EmptyState`. The plan's bullet for this view reads "the message gains Retry", and keeping the card avoids a visual change outside the defect. The other three views use `EmptyState` + `BaseButton` (Decision 5).
- Retry buttons are `BaseButton variant="secondary" size="sm"` everywhere.
**Deviations:** Departs from create-vue-feature § Page Shell Pattern — plan Decision 13: an error state added between loading and empty, no `PageLayout` / `PageToolbar` adoption (`ReportView` already used `PageLayout`; the others still don't).
**Verification:**
- `npm --prefix web/reflekt-web run type-check` → green.
- Acceptance (diff): each of the four views has an `error` branch before its content branch, with a Retry control. `grep -n ':key' web/reflekt-web/src/App.vue` → `11:  <RouterView :key="route.path" />`. The offline and deep-link behaviour is a walkthrough item (Step 13/14), not observed here.
- Step close: `verify-run.js --profile fast --tied --changed {the 6 files} --slug F36-FrontendDefects` → pass, run `mvkhh6vm-f8230cfd` (fell back to `roles.unit`, 5.5 s); `label-tests.js` → timed no test, nothing to label.

## Step 7 — Retro room: staged entry with retry, refresh banner, report retry (FE-08 part)
**Files:**
- `web/reflekt-web/src/composables/useRetroRoom.ts` — the entry now runs in three checked stages (`runEntry(from)`). Each stage reads its loads' `error` before the next starts. A failure calls `failStage`, which sets `roomError`, sets `loading = false`, records the stage to resume at, and stops the entry.
  1. `loadRoomInfo` — `policyStore.fetch` and `fetchSprintSummary` (run together); `policyStore.error ?? currentSummaryError` → room error, never the join form.
  2. `identify` — `authStore.fetchMe` if not loaded (never rejects). Signed in → `joinWithGoogle`, with today's join-form messages on failure. Guest with a stored token → `participantStore.fetchMe` (no longer throws, Step 5): no `meError` → continue; `meStatus === 401` → clear the token and show the join form (today); any other failure, a network one included, → room error. No token → join form. **This closes the Step 5 interim gap at the old `useRetroRoom.ts:106`:** the identity branch now reads `meError` / `meStatus` instead of a throw, so a refused token no longer enters the room.
  3. `loadRoomData` — `loading = true`; foreground `fetchParticipants`, `fetchCards`, `fetchGroups`, `fetchSprintSummary`, `fetchPreviousActions`, then `fetchMyVotes`; any of their errors → room error. Only after all succeed: `readinessStore.reset()`, `connect`, `joined = true`, `loading = false`, then the Reporting report load as today.
  - `retryEntry()` is a no-op while an attempt runs (`entryRunning`). Otherwise it clears `roomError`, sets `loading = true` and resumes at the failed stage, so a participant already identified or joined is not joined again. `handleJoin` keeps its own join-form messages for the `join` call, then runs `runEntry('data')`.
  - `refreshError` (computed) is set only after entry (`joined`) and is `cardStore.error ?? cardGroupStore.error`. `retryRefresh()` runs a background refresh of cards and groups. New returns: `roomError`, `refreshError`, `retryEntry`, `retryRefresh`.
  - Rewritten with one `Write`, then restored to CRLF.
- `web/reflekt-web/src/views/RetroRoomView.vue` — a room-error branch (`EmptyState` + `BaseButton` Retry → `retryEntry`) after the loading spinner and before the join form and the board. Above the board, a `role="status"` one-line notice "The board could not be refreshed." with Retry → `retryRefresh`, shown while `refreshError` is set. The board stays rendered, and the notice clears when a refresh succeeds, because a successful fetch clears the store's `error`. The notice uses the danger badge tokens and the closed-notice shape. `RetroReportPanel` gets `@retry` → `reportStore.fetchParticipantReport(sprintId)`; the hub is not touched.
- `web/reflekt-web/src/components/retro/RetroReportPanel.vue` — the existing error line gains a `BaseButton` "Retry" emitting `retry` (new emit). The line's text and classes are unchanged.
**Skills:** vue-patterns
**Decisions:**
- A failed `connect` (hub `start` / `JoinSprint` rejects) is treated as a stage-3 failure ("Can't connect to the live board") with Retry. Before, it was an unhandled rejection that left the spinner on, or the "Failed to join" join-form message on the form path. The plan's acceptance is "never stuck loading", and `connect` stops any existing connection first, so Retry still holds only one connection.
- Stage 1 runs `policyStore.fetch` and `fetchSprintSummary` side by side (before, one after the other). Neither depends on the other, and the stage checks both errors after both end.
- Stage 3 keeps re-fetching the summary (as today) so the room has fresh phase and state after identity. Its foreground clear (Step 4) happens while the stage's `loading` spinner is up.
- One constraint comment on `refreshError` says why the cards/groups error means a failed refresh after entry.
**Deviations:** None. The coordinator's directive "Step 7 closes the `useRetroRoom.ts:106` fetchMe-throw gap" is met (see stage 2 above).
**Verification:**
- `npm --prefix web/reflekt-web run type-check` → green.
- `grep -n "fetchMe\|meError\|meStatus" web/reflekt-web/src/composables/useRetroRoom.ts` → `participantStore.fetchMe(sprintId)` followed by the `meError` / `meStatus !== 401` checks; no `try`/`catch` around it.
- Offline and 500 behaviour, the single hub connection after Retry, and the banner on a failed hub refresh are walkthrough items (Step 13/14), not observed here.
- Step close: `verify-run.js --profile fast --tied --changed {the 3 files} --slug F36-FrontendDefects` → pass, run `mvkhl5x3-7423da45` (fell back to `roles.unit`, 16.1 s); `label-tests.js` → timed no test, nothing to label.

## Step 8 — Note and action-item inputs keep text on a failed save (FE-07 part)
**Files:**
- `web/reflekt-web/src/composables/useRetroBoardActions.ts` — `addNote` and `addActionItem` now return `Promise<boolean>`: `true` after a successful `createCard`, `false` after a failure. The error toast (`onError`) is unchanged.
- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue` — in-flight flags `noteSaving` (one per note column) and `actionSaving`. While a column's flag is set, its `BaseInput` and Add button are `disabled`, and `onAddNote` returns at once, so a second Enter or click is ignored. The note text is cleared only when the add returned `true`. `onAddActionItem` works the same way: it clears the text and the assignee only on success and keeps both on failure. Creates stay non-optimistic. No focus handling (plan Decision 3 / P4).
**Skills:** vue-patterns
**Decisions:** The flag check sits inside the handlers as well as on `:disabled`: an Enter keydown queued before the re-render that disables the input can still reach the handler, and the guard ignores it. The assignee `<select>` is not disabled while the save runs, because the plan names only the input and its Add button. A change made there mid-save is cleared on success, as today.
**Deviations:** None
**Verification:**
- `npm --prefix web/reflekt-web run type-check` → green.
- Acceptance (diff): `grep -n "newCardText.value\[column\] = ''\|newActionText.value = ''\|newActionAssignee.value = ''" RetroBoardPanel.vue` → only `270: if (added) newCardText.value[column] = ''` and `280: if (added) { newActionText.value = ''; newActionAssignee.value = '' }`. Both clears run after the add resolves `true`.
- Text kept on failure, one toast, no duplicate on a second Enter, and "back online one Enter adds exactly one note" are walkthrough items (Step 13/14), not observed here.
- Step close: `verify-run.js --profile fast --tied --changed {the 2 files} --slug F36-FrontendDefects` → pass, run `mvkhor25-3e149fe1` (fell back to `roles.unit`, 8.1 s); `label-tests.js` → timed no test, nothing to label.

## Step 9 — Edit/delete show only where the server allows them (FE-31)
**Files:**
- `web/reflekt-web/src/composables/useRetroBoardActions.ts` — `canMutateCard` now mirrors `EditCardEndpoint` / `DeleteCardEndpoint` for a caller holding a participant token. It returns false for:
  - a card that is not the caller's own (`card.participantId !== currentParticipant?.id`); the `|| authStore.user !== null` branch is gone;
  - a closed sprint (`currentSummary?.state === 'Closed'`, which the hub's `SprintClosed` handler sets);
  - a phase where the policy matrix does not allow `EditActionItem` / `EditNote` for the card type;
  - an action item in `Discussing` when the caller is not signed in with Google (`authStore.isSignedIn`).

  One flag still drives Edit and Delete (Decision 4); callers (`cardItemProps`, the action-item column in `RetroBoardPanel.vue`) are unchanged.
**Skills:** vue-patterns
**Decisions:** The checks are early returns in the server's order (owner, closed, phase, Discussing action item), so the predicate reads against the endpoints line by line.
**Deviations:** None
**Verification:**
- `npm --prefix web/reflekt-web run type-check` → green.
- Acceptance: `grep -n "authStore.user !== null" web/reflekt-web/src/composables/useRetroBoardActions.ts` → no hits. `canMutateCard` / `canEdit` usages (`grep -rn`) → only `cardItemProps` and `RetroBoardPanel.vue:157`, both unchanged.
- Roles (guest and Google participant, phase and closed sprint) is a walkthrough item (Step 13/14).
- Step close: `verify-run.js --profile fast --tied --changed web/reflekt-web/src/composables/useRetroBoardActions.ts --slug F36-FrontendDefects` → pass, run `mvkhqtpt-086ac70d` (fell back to `roles.unit`, 15.0 s); `label-tests.js` → timed no test, nothing to label.

## Step 10 — Create-sprint and delete-team forms own their calls (FE-14)
**Files:**
- `web/reflekt-web/src/components/sprints/CreateSprintForm.vue` — new `teamId: number` prop (Decision 8); the `create` emit is gone. `handleCreate` now awaits `sprintStore.createSprint(props.teamId, payload)` inside `try`, with the same payload as before (the timer defaults included). `busy` stays on until the call ends (`finally`). `sprintName` and `votesPerParticipant` reset only on success. A failure fills the existing `error` line: the server's first general error if there is one, otherwise `Failed to create sprint.`.
- `web/reflekt-web/src/components/sprints/SprintList.vue` — new `teamId` prop, passed to `<CreateSprintForm :team-id>`; the `create-sprint` emit is gone.
- `web/reflekt-web/src/views/SprintListView.vue` — passes `:team-id`; `handleCreateSprint` and `@create-sprint` deleted. The store's `createSprint` still prepends the new sprint to the list.
- `web/reflekt-web/src/components/teams/DeleteTeamDialog.vue` — awaits `teamsStore.deleteTeam(team.id, confirmationInput)` itself; `busy` stays on until the call ends. On success it emits `deleted` (replaces `confirm`). On failure it shows an error line (`text-danger`) above the buttons — the server's general error (e.g. "Confirmation name does not match…", "This team has an active sprint…") or `Failed to delete team.` — and keeps the typed name. The error clears on a new attempt, on close and on a team change, beside the existing resets.
- `web/reflekt-web/src/views/TeamDashboardView.vue` — `@deleted="pendingDeleteTeam = null"`; `handleDeleteConfirm` deleted.
**Skills:** pinia-patterns
**Decisions:** Error text is read the way `CreateTeamForm` reads it (`errors.generalErrors[0]`, the FastEndpoints `AddError` shape `DeleteTeamEndpoint` sends), with a fixed fallback for a network failure or any other shape. This keeps one error-reading shape across the three forms; a shared `ApiError` is F39 (FE-18).
**Deviations:** None
**Verification:**
- `npm --prefix web/reflekt-web run type-check` → green.
- Acceptance: `grep -n "emit('create'" CreateSprintForm.vue` → no hits; `grep -n "emit('confirm'" DeleteTeamDialog.vue` → no hits. `grep -rn "create-sprint\|handleCreateSprint\|handleDeleteConfirm" web/reflekt-web/src` → no hits.
- "Values kept, error shown" (offline and with a 500) is a walkthrough item (Step 13/14).
- Step close: `verify-run.js --profile fast --tied --changed {the 5 files} --slug F36-FrontendDefects` → pass, run `mvkhul6q-b7a14bbe` (fell back to `roles.unit`, 8.1 s); `label-tests.js` → timed no test, nothing to label.

## Step 11 — Team name on cold entry, guarded admin view, correct back link (FE-28, FE-32)
**Files:**
- `web/reflekt-web/src/components/AppHeader.vue` — a `watch` on `route.fullPath` (`immediate`) calls `teamsStore.fetchMyTeams()` only when all of these hold:
  - the route is `team-sprints`, `sprint` or `report` (the existing `showBreadcrumb`);
  - the user is signed in;
  - `teams` is empty;
  - no teams load is in flight;
  - the teams `error` is not set.

  Both the header breadcrumb and `SprintList`'s heading read the same store, so both get the name. Guests are never sent to `/api/teams`, and after a failed load the header does not retry by itself. One constraint comment states the no-retry rule.
- `web/reflekt-web/src/router/index.ts` — `requiresAuth: true` on the `sprint` route only; `report` unchanged.
- `web/reflekt-web/src/components/sprints/SprintDetailPanel.vue` — "Back to sprints" → `{ name: 'team-sprints', params: { teamId } }` (existing `teamId` prop).
**Skills:** vue-patterns
**Decisions:** Watching the route rather than `authStore` is safe on a cold load: the router guard awaits `authStore.fetchMe()` before the first route resolves. When the watcher sees `team-sprints` / `sprint` / `report`, `isSignedIn` is already known. The `immediate` run on the start location has no team route, so it does nothing.
**Deviations:** None
**Verification:**
- `npm --prefix web/reflekt-web run type-check` → green.
- AC5 greps: `grep -n "team-sprints" SprintDetailPanel.vue` → line 3. In `router/index.ts`, `sprint` (line 42) has `requiresAuth: true` and `report` (line 54) does not. `git diff --stat` of the router → 1 insertion, 1 deletion (that one meta line).
- Deep links (team name after a reload on the three routes) and "offline, one `/api/teams` request, not a stream" are walkthrough items (Step 13/14).
- Step close: `verify-run.js --profile fast --tied --changed {the 3 files} --slug F36-FrontendDefects` → pass, run `mvkhwo0b-970da581` (fell back to `roles.unit`, 5.2 s); `label-tests.js` → timed no test, nothing to label.

## Step 12 — Audible expiry chime (FE-13)
**Files:**
- `web/reflekt-web/src/composables/useTimer.ts` — the base64 WAV, `getChimeAudio`, the `chimeAudio` element and the misleading "provides audible chime" comment are deleted. New module-level `audioContext` (one per page, created lazily). Exported `unlockChime()`: with no `window.AudioContext` it returns, so there is silence. Otherwise it creates the context or resumes a suspended one; a rejected `resume` is ignored. `playChime()` plays a 440 Hz oscillator for 300 ms through a gain envelope (0 → 0.3 in 20 ms → 0 at 300 ms), and only when the context is `running`. `triggerExpiry` calls it when not muted, so a muted expiry is still silent. `toggleMute` calls `unlockChime()` before toggling.
- `web/reflekt-web/src/views/RetroRoomView.vue` — while the room is mounted, document-level `pointerdown`, `click` and `keydown` listeners call `unlockChime`, so a gesture before the timer is shown counts. They are added in `onMounted` and removed in `onUnmounted`; the keyed `<RouterView>` (Step 6) unmounts the room on a sprint change. One constraint comment says why the listeners exist.
**Skills:** vue-patterns
**Decisions:**
- Three gesture events, not one. `pointerdown` and `keydown` count as user activation in Chromium and Firefox; `click` is added for Safari, which is stricter about which events unlock audio. Each listener only calls a function that does nothing once the context is running.
- The listener functions stay attached after the unlock rather than removing themselves: a context the browser suspends again, for example after a long idle, is resumed by the next gesture.
**Deviations:** None. Boy-scout note, not acted on: `const MUTE_KEY = 'reflekt:timer:muted'` in `useTimer.ts` was already unused before this feature (the mute state lives in `stores/timer.ts`). It is left in place to keep the diff to FE-13 and is recorded under Carry-Over.
**Verification:**
- `npm --prefix web/reflekt-web run type-check` → green.
- AC4: `grep -rn "BASE64_CHIME\|data:audio" web/reflekt-web/src` → no hits (exit 1); `grep -n "AudioContext" web/reflekt-web/src/composables/useTimer.ts` → lines 14, 17, 18.
- Chime (an audible unmuted expiry after one click, a silent muted one) is a walkthrough item. Hearing the tone cannot be done in this session, so it is listed for the operator (Step 13/14).
- Step close: `verify-run.js --profile fast --tied --changed {the 2 files} --slug F36-FrontendDefects` → pass, run `mvkhyqbp-6e78f922` (fell back to `roles.unit`, 10.8 s); `label-tests.js` → timed no test, nothing to label.

## Step 13 — Verify: build, greps, recorded walkthrough, graph update
**Files:** None in the product. Scratch only, under `.claude/tmp/walk/` (untracked): `walk.mjs`, a CDP driver for headless Chrome; `shots/*.png`, the screenshots; `walk-log.json`, what was seen; `api.log`; `reflekt.db`, a scratch seeded database.
**Skills:** None (plan: no pattern skill)
**Decisions:**
- The walkthrough ran the API with the built SPA (`dotnet run --no-launch-profile --urls http://localhost:4097` on `Reflekt.API`), using a scratch database (`ConnectionStrings__Reflekt` → `.claude/tmp/walk/reflekt.db`) and `Seed__Enabled=true`. The seed gives an Active sprint 1 in Writing, with three participants and four notes.
- The browser was the installed Chrome, run headless and driven over the DevTools protocol by a Node script (Node 24's built-in `WebSocket`), so no package was added.
- Failures were injected with CDP `Fetch`. The offline case is `Fetch.failRequest` with `InternetDisconnected` on the requests under test (the SPA itself must still load); the 500 case is `Fetch.fulfillRequest` with status 500.
- Only guest-reachable flows can be observed this way: the sign-in is Google OAuth, the seed has no other sign-in, and no guest can move the phase. Every other AC6 item is listed under OPERATOR ACTION REQUIRED below. The API and Chrome were stopped afterwards (port 4097 free).
**Deviations:** `graphify update .` was not run: `graphify` is not installed (`where.exe graphify` → not found; no `graphify` Python module). OPERATOR ACTION REQUIRED item 9.
**Verification — build and greps (spec AC1–AC5):**
- `npm --prefix web/reflekt-web run type-check` → green. `npm --prefix web/reflekt-web run build` → green, built in 756 ms (`wwwroot/assets/index-BAF_RDdr.css`, 42.63 kB).
- AC1: `grep -rnE "\[--[a-z-]+\]" web/reflekt-web/src` → no hits (exit 1).
- AC2: `grep -n "<body" web/reflekt-web/index.html` → `25:  <body>` (no `style=`); `<head>` lines 12–13 `html { background: #0a0a0f; }` / `html.light { background: #f4f4f8; }`; the built `wwwroot/index.html` is the same.
- AC3: the built CSS contains `border-radius:var(--radius-md)`. It also has `.bg-col-{green,red,amber,blue,purple,teal,pink,slate}-light\/30{` (all eight), `.bg-action-items-light\/30{`, `.bg-surface-focus{background-color:var(--color-surface-focus)}` and `max-width:var(--container-6xl)`.
- AC4: `grep -rn "BASE64_CHIME\|data:audio" web/reflekt-web/src` → no hits (exit 1); `grep -n "AudioContext" useTimer.ts` → lines 14, 17, 18.
- AC5: `inheritAttrs: false` → `BaseInput.vue:20`; `team-sprints` → `SprintDetailPanel.vue:3`. In `router/index.ts`, `sprint` has `requiresAuth: true` (line 42) and `report` has none (line 54).
- Must-NOT-Change: `git status --short web/reflekt-web/package.json web/reflekt-web/package-lock.json docs/backlog.md` → empty. Every changed source file is `w/crlf` (`git ls-files --eol`).
- Step close: `verify-run.js --profile fast --tied --changed docs/specs/F36-FrontendDefects/delivery/implementation.md --slug F36-FrontendDefects` → pass, run `mvki9s2f-5183281b` (fell back to `roles.unit`, 6.2 s); `label-tests.js` → timed no test, nothing to label. AC7 (`roles.full`) is the coordinator's handover run, not this step.

**AC6 walkthrough — what was seen (this session, guest, headless Chrome 1366×900 unless noted):**
- **Light and dark, cold load — Welcome:**
  - Dark: `html` has no class, `html` and `body` background `rgb(10, 10, 15)` (#0a0a0f), `<body>` has no `style` attribute, and the button `border-radius` is `8px` (rounded) (`shots/welcome-dark.png`).
  - Light (`localStorage reflekt:theme=light`, then reload): `html.light`, `html` and `body` background `rgb(244, 244, 248)` (#f4f4f8), the page is light, and the Sign-in button corners are rounded (`shots/welcome-light.png`).
  - Teams, Sprints list, sprint view and report as a signed-in user → operator (item 1).
- **Board states:**
  - Retro room, dark: the cards carry `bg-col-green-light/30` and `bg-col-red-light/30`, and they render (computed backgrounds `oklab(… / 0.3)`, not transparent). Card radius is `8px` (`shots/room-board-dark.png`).
  - Light, mobile 390×844: the green and red tints render (`oklab(0.95 … / 0.3)`), and the mobile tab bar shows the three column tabs with rounded corners (`shots/room-mobile-light.png`).
  - Not seen here: amber, the other five palette colours, an action-item tint, the focused group in Discussing (dark), the drag-over highlight and the refresh banner (item 2). The seed's notes are only in green and red columns, and the other cases need the Scrum Master to change phase or colours, or a hub event.
- **`primary-50/100` sites in dark:**
  - `RetroJoinForm.vue` avatar chips: a light-lavender circle with an indigo letter, readable (`shots/room-join-dark.png`).
  - `PhaseIndicator.vue` (`components/PhaseIndicator.vue:53`, `ring-primary-100`): a light ring around the active indigo step, readable (`shots/room-board-dark-after.png`).
  - `AppHeader.vue` avatar chip (signed in only) and the `RetroColumnPanel.vue` drag-over → operator (item 2).
- **Failures — add a note (Column 1), Enter pressed twice in a row:**
  - Offline (POST fails, `InternetDisconnected`): the input keeps "F36 offline note", one "Failed to add card." toast, no card created (`shots/room-addnote-offline.png`).
  - 500: the input keeps "F36 500 note", one toast, no card created. In both runs the second Enter raised no second toast: a second save attempt would have raised a second "Failed to add card." toast.
  - Back online: one Enter added exactly one "F36 online note" card and cleared the input.
  - Action item, create sprint, delete team, sprint view and teams dashboard → operator (item 3). Action items exist only in Discussing, and the rest need sign-in.
- **Failures — retro room entry**, each offline and with a 500, as a guest with a stored token:
  - Stage 1 (`/summary` blocked): "Can't reach the server" / "Failed to load sprint" with Retry. No join form, no board, no spinner.
  - Stage 3 (`GET /cards` blocked): "Can't reach the server" / "Failed to load cards" with Retry (`shots/room-entry-stage3-cards-500.png`).
  - In all four cases, Retry after unblocking entered the board, with exactly one `/hubs/sprint/negotiate` request on the page.
- **Roles — guest, Writing phase:** exactly one Edit and one Delete on the board, both on the guest's own "F36 online note" card. The seeded cards of Ana, Mihai and Elena are blurred and show none (`shots/room-board-dark-after.png`). A Google participant, the phases and a closed sprint → operator (item 4).
- **Guarded admin view (guest):** `/teams/1/sprints/1` → redirected to `/`. `/teams/1/sprints/1/report` stays on its route; without a token for this sprint it shows "Not authorized" with Retry. A guest opening the report with a token on a closed sprint → operator (item 4).
- **Deep links** (team name after reload) → operator (item 5). **Chime** → operator (item 6).

**OPERATOR ACTION REQUIRED — Step 14, AC6 items not observed here.** Run the app as in Decisions above, or as normally deployed. Use a real Google account that owns a team, and a second browser profile as a guest. Record what you see in a `## Walkthrough — operator` block.
1. **Light and dark, cold load, signed in:** in each theme, cold-load Teams (`/teams`), the Sprints list (`/teams/{id}/sprints`), the sprint view (`/teams/{id}/sprints/{sid}`) and the report (`/teams/{id}/sprints/{sid}/report`, closed sprint). Check that the page background is correct (no dark page in light) and that corners are rounded.
2. **Board states:**
   - Set the columns to amber, blue, purple, teal, pink and slate (column settings), and check that every card tint renders.
   - Advance to Discussing, add an action item and check its tint. Focus a group and check, in dark, that the focused group is readable.
   - In Grouping, drag a card over another column and check that the drag-over highlight is readable in dark. Check the header avatar chip in dark.
   - Refresh banner: with two clients in the room, use devtools to block `GET /api/sprints/{sid}/groups` on client A, then group two cards on client B. On A the board must stay, with "The board could not be refreshed." and Retry above it. Unblock, press Retry, and the notice clears.
   - If any `primary-50/100` site is unreadable in dark, that becomes a fix (plan Step 2 rule).
3. **Failures**, each offline (devtools) and with a 500 (devtools override):
   - Add an action item in Discussing: the text and the assignee stay, one toast, no duplicate on a second Enter.
   - Create a sprint and delete a team: the values are kept and an error shows in the form or dialog.
   - Open the sprint view and the teams dashboard: an error state with Retry, never another sprint's data.
   - Back online, Retry succeeds in each case.
4. **Roles:**
   - As a Google participant in the room: Edit/Delete only on your own cards. They disappear on action items in Discussing for a guest, in phases the matrix forbids, and after the sprint is closed (watch the open board when the Scrum Master closes it).
   - As a guest with a token, open the report URL of a closed sprint: the report shows.
5. **Deep links:** signed in, reload on `team-sprints`, `sprint` and `report`: the team name shows in the header and in the Sprints list heading. Offline, reloading `team-sprints` sends one `/api/teams` request, not a stream.
6. **Chime:** in the room, click once, then let a timer expire unmuted → the ~300 ms tone is audible. Mute it and let one expire → silent.
7. Also, signed out, open `/teams/{id}/sprints/{sid}` → redirected to the welcome page (seen here as a guest; repeat once after a real sign-out).
8. Dark-readability verdicts for the `AppHeader.vue` chip and the `RetroColumnPanel.vue` drag-over (from item 2).
9. Run `graphify update .` from the repo root wherever `graphify` is installed (`CLAUDE.md` § graphify); it is not installed on this machine.

## Fix Round 1
**Files:**
- `web/reflekt-web/src/composables/useRetroRoom.ts` — row 1 [HIGH]. A per-instance entry number (`currentEntry`), raised by every `runEntry` and by `onUnmounted`. `loadRoomInfo`, `identify` and `loadRoomData` take the entry's number and return, without writing, after every await once it is no longer current: after policy+summary; after `authStore.fetchMe`; after `joinWithGoogle` (before `guestStore.saveToken`) and in its catch; after the guest `fetchMe`; after the five data loads; after `fetchMyVotes` (before `readinessStore.reset()` and `connect`); in the `connect` catch and after `connect` (before `joined` and the report load). `handleJoin` captures the number before `participantStore.join` and skips `saveToken` / `runEntry` once unmounted. `entryRunning` is cleared only by the entry that is still current.
- `web/reflekt-web/src/views/SprintView.vue` — row 2 [MEDIUM]. `load()` takes a number (`currentLoad`), raised by every load and by `onUnmounted`; after `fetchSprint` a superseded or unmounted load returns. Participants are fetched only when `currentSprint.id === sprintId` and the sprint is Active.
- `web/reflekt-web/src/composables/useTimer.ts` — row 3 [LOW]. Removed the unused `MUTE_KEY` constant (the mute setting lives in `stores/timer.ts`).
- `web/reflekt-web/src/components/sprints/CreateSprintForm.vue` — row 4 [MEDIUM]. The column pre-fill from the most recent sprint moved from `onMounted` to a `watch` on `sprintStore.sprintsLoading` (immediate): it applies once, when the list load has ended and the list has a sprint. `fetchTeamSprints` clears the list when it starts, so at mount the list is empty and the old `onMounted` read nothing. A sprint created in the form (`createSprint` unshifts without a load) and a later Retry do not re-apply it over the user's edits.
**Skills:** None — fix rows, not plan steps; no skill slip on the list.
**Decisions:**
- Row 1: one counter serves both "superseded by a newer entry" and "unmounted", as the fix asked ("on unmount and on a new entry"). After a stale `connect` the entry returns without calling `disconnect()`: the hub connection is module-wide in `useSprintHub.ts`, so a stale instance's `disconnect()` could stop the next room's connection. The unmount's own `disconnect()` stops a connection the stale entry built.
- Row 4: pre-fill once per form instance, as before F36 (`onMounted`); a team switch that reuses the form does not pre-fill again, as before.
**Deviations:** None. One narrow case left as is: the room unmounts while its `connect` is still waiting for a previous room's connection to stop (`useSprintHub.connect` → `await disconnect()`); its new connection is then built after the unmount's `disconnect()` has run. Closing that needs `useSprintHub` to hand back the connection it built, which is outside the row's file; recorded here for the reviewer.
**Verify:**
- `npm --prefix web/reflekt-web run type-check` (`vue-tsc --build`) → green.
- Step close: `verify-run.js --profile fast --tied --changed {the 4 files} --slug F36-FrontendDefects` → pass, run `mvkiscxu-e263bd1d` (no installed runner ties `.ts`/`.vue` files — fell back to `roles.unit`, 5.2 s); `label-tests.js --run mvkiscxu-e263bd1d` → timed no test, nothing to label.
- No new tests (campaign directive). `graphify update .` not run — `graphify` is not installed (operator item 9 above).
- CRLF kept on all four files.

## Fix Round 2
**Files:**
- `web/reflekt-web/src/stores/participant.ts` — row 1 [HIGH]. One module-level `identityRequest` generation shared by `join`, `joinWithGoogle` and `fetchMe`: each takes a number when it starts and writes `currentParticipant` only if no later identity action has started since. New action `abandonPendingIdentity()` raises the number, so in-flight identity responses are dropped. `fetchMe` keeps `meRequest` for its own `meError` / `meStatus` / `meLoading`; its `currentParticipant` write now checks the identity number.
- `web/reflekt-web/src/composables/useRetroRoom.ts` — row 1: `onUnmounted` calls `participantStore.abandonPendingIdentity()`, so a join that lands after the room closed (before the next room starts its own identity action) writes nothing. Row 2 [HIGH], per the `questions.md` Q1 ruling (option 2): in `loadRoomData` (the ruling's `enterData`), the superseded branch after a resolved `connect` calls `disconnect()` as its first statement, with no await before it, then returns without setting `joined` / `loading`. The `catch` branch is unchanged. The comment on `currentEntry` now says a stale entry may stop only the connection it built.
- `web/reflekt-web/src/components/sprints/CreateSprintForm.vue` — row 3 [MEDIUM]. An `edited` set records the column fields the user changed: `@input` on each name and the colour-button `@click`. The pre-fill (`prefillColumn`) fills only fields not in the set. Fields the user has not touched get the most recent sprint's values, as before F36.
**Skills:** None. These are fix rows, not plan steps, and the list has no skill slip.
**Decisions:**
- Row 1: the identity guard sits in the store, where the write happens, as the row asked. The composable's existing entry checks still stop `saveToken` and the later stages. Abandoning on unmount covers the gap between room A unmounting and room B's first identity action.
- Row 3: chose "pre-fill only untouched fields" over disabling the form while it loads. Disabling would add a state the form never had.
**Deviations:** None. `useSprintHub.ts` was not touched (its diff is still the eight pinned call arguments: `git diff --stat` shows 8+/8−). This supersedes the Fix Round 1 decision that a stale entry never calls `disconnect()`: the Q1 ruling shows that a resolved `connect` means the connection is this entry's own.
**Verify:**
- `npm --prefix web/reflekt-web run type-check` (`vue-tsc --build`) → green.
- Step close: `verify-run.js --profile fast --tied --changed {the 3 files} --slug F36-FrontendDefects` → pass, run `mvkj40mm-3ad18620`. No installed runner ties `.ts`/`.vue` files, so it fell back to `roles.unit` (5.2 s). Re-run after the CRLF repair below → pass, run `mvkj4ywh-6f838414`. `label-tests.js` on both runs → each timed no test, so there was nothing to label.
- No new tests (campaign directive). CRLF kept on all three files. A Git Bash `sed -i` template edit had left `CreateSprintForm.vue` LF; `perl -pi` restored CRLF, and `git ls-files --eol` now shows `w/crlf` for all three files.

## Fix Round 3
**Files:**
- `web/reflekt-web/src/composables/useRetroRoom.ts` — row 1 [HIGH], per the `questions.md` Q2 ruling. A module-level `withHubLock(op)` (a promise chain whose stored link swallows rejections, so a failed op never jams it; the caller still gets the real result) serializes every hub connect/disconnect across room instances. Per room: `ownedConnection` (`ReturnType<typeof getHubConnection>`, starts `null`; `getHubConnection` imported from `./useSprintHub`); `stopOwned()` stops the shared connection only when it is the room's own, then clears `ownedConnection`; `releaseHub()` = `withHubLock(stopOwned)` replaces the `onUnmounted` and kicked-callback `disconnect()` calls. Connecting is `connectHub(entry)`, one locked op returning `'ok' | 'failed' | 'stale'`: superseded on acquiring the lock → `'stale'` without calling `connect`; otherwise `connect` in try/catch, then `ownedConnection = getHubConnection()`, then superseded → `await stopOwned()` and `'stale'`. `loadRoomData` acts on the outcome outside the lock: `'stale'` returns, `'failed'` → `failStage('data', "Can't connect to the live board")`, `'ok'` → `joined`, `loading`, Reporting report load as before. The Q1 post-connect stale branch (bare `disconnect()`) and its comment are removed.
- `docs/specs/F36-FrontendDefects/delivery/implementation.md` — this block; the `MUTE_KEY` carry-over row marked resolved (dispatch instruction).
**Skills:** None. A fix row, not a plan step, and the list has no skill slip.
**Decisions:** `readinessStore.reset()` stays where it was, before the locked op; the ruling places only the connect/disconnect calls under the lock. `stopOwned` is called directly inside `connectHub`'s op (never through `releaseHub`), so the lock is never awaited from inside itself.
**Deviations:** None. Q2 accept checks: `connect(` appears only inside `connectHub`'s locked op (line 79) and `disconnect(` only in `stopOwned` (line 66); no other bare `disconnect()` remains. `git diff --stat -- web/reflekt-web/src/composables/useSprintHub.ts` → 8+/8−, the eight pinned call arguments. The Q2 walkthrough checks (leave during "Joining retro…" and reopen, twice; leave for a non-room page while joining) need DevTools on a live hub and join the Step 13/14 operator walkthrough.
**Verify:**
- `npm --prefix web/reflekt-web run type-check` (`vue-tsc --build`) → green.
- Step close: `verify-run.js --profile fast --tied --changed web/reflekt-web/src/composables/useRetroRoom.ts --slug F36-FrontendDefects` → pass, run `mvkjjd89-9d869908`. No installed runner ties `.ts` files, so it fell back to `roles.unit` (12.0 s). `label-tests.js --run mvkjjd89-9d869908` → timed no test, nothing to label.
- No new tests (campaign directive). CRLF kept: 280 lines, 280 carriage returns.

## Fix Round 4
**Files:**
- `web/reflekt-web/src/composables/useRetroRoom.ts` — row 1 [HIGH], per the `questions.md` Q3 ruling. The Q2 lock, `ownedConnection`, `stopOwned()` and `releaseHub()` stay. Inside `connectHub`'s locked op, after the superseded-on-acquire check: a leftover shared connection is stopped first (`if (getHubConnection()) await disconnect()`); then `const pending = connect(sprintId)` runs without being awaited, and on the next line, before any `await`, `ownedConnection = getHubConnection()` and a new per-room `connectInFlight = true` are set. The op then settles with `try { await pending } catch { failed = true }`, sets `connectInFlight = false`, and runs the Q2 handling (superseded → `await stopOwned()` → `'stale'`; else `'ok'` / `'failed'`). New per-room `cancelHub()`: when `connectInFlight` and `ownedConnection` are set, it calls `ownedConnection.stop()` directly (not through the lock, not awaited, with `.catch(() => undefined)` so a rejection is not unhandled), then `releaseHub()`. `onUnmounted` (after `currentEntry++` and `abandonPendingIdentity()`) and the `useSprintHub` kicked callback now call `cancelHub()` instead of `releaseHub()`.
**Skills:** None. This is a fix row, not a plan step, and the list has no skill slip.
**Decisions:**
- A second superseded check sits between the leftover `disconnect()` and `connect(`. Without it, a room that unmounts while that leftover stop is pending would find `connectInFlight` false (so `cancelHub` stops nothing). Its op would then build a connection and wait on `JoinSprint` under the lock with nothing to cancel it, which is the same class as the row. The check skips connecting, exactly like the acquire-time check Q2 already prescribes.
- A kick during an in-flight connect is not treated as stale. The stopped connect returns `'failed'`. `failStage` runs, but the view shows `kicked` first (`RetroRoomView.vue:3` `v-if="kicked"` comes before `loading` / `roomError`). The kicked callback's `releaseHub()` then stops and clears the owned connection under the lock. Q3 step 2 keeps the outcome to superseded / ok / failed.
- I checked Q3's open risk against the installed package (`@microsoft/signalr` 10.0.0, `dist/esm/HubConnection.js`). `stop()` while `Connecting` sets `_stopDuringStartError`, so `start()` throws. `_connectionClosed` runs `_cancelCallbacksWithError("Invocation canceled due to the underlying connection being closed.")`, so a pending `JoinSprint` rejects. A `stop()` that lands after `start()` resolves makes the next `invoke` reject because the connection is not Connected. `stop()` on a connection already Disconnecting or Disconnected returns the existing promise or resolves, so the later `disconnect()` in `stopOwned` is harmless.
**Deviations:** None. Q3 accept checks:
- `ownedConnection = getHubConnection()` is on line 88, the line right after `const pending = connect(sprintId)` (line 87), with no `await` between them.
- `onUnmounted` (line 267) and the kicked callback (line 62) call `cancelHub()`.
- `disconnect(` appears only in `stopOwned` (line 67) and in the locked op (line 85). `connect(` appears only in the locked op.
- `git diff --stat -- web/reflekt-web/src/composables/useSprintHub.ts` shows 8+/8−, the eight pinned call arguments.

Codex reproductions (a)–(c) and the throttled-`JoinSprint` DevTools walkthrough are for the re-review and the Step 13/14 operator walkthrough. I wrote no harness (campaign directive: no new tests).
**Verify:**
- `npm --prefix web/reflekt-web run type-check` (`vue-tsc --build`) → green.
- Tied close: `verify-run.js --profile fast --tied --changed web/reflekt-web/src/composables/useRetroRoom.ts --slug F36-FrontendDefects` → pass, run `mvklebrl-955403fb`. No installed runner ties `.ts` files, so it fell back to `roles.unit` (5.7 s). `label-tests.js --run mvklebrl-955403fb` → timed no test, so there was nothing to label.
- No new tests (campaign directive). CRLF kept: 293 lines, 293 carriage returns.

## Key Decisions
- Step 2: the tint map keys are the palette from `PALETTE_COLORS`, so every column colour the app can set has an entry. A colour outside the map gets no class, as today.
- Step 3: `BaseInput.inputAttrs()` is a function called from the render, not a `computed`, because `useAttrs()` is not reactive.
- Step 4: request counters are module-level; `auth.fetchMe` shares one in-flight promise instead of a counter, so the router guard and `App.vue` both read the result of one request. Error texts: `Failed to load {resource}` / `Can't reach the server` / `Sprint not found` (404).
- Step 5: background refresh is an options object, `{ background: true }`; it never sets `loading`. Cards and groups keep one `error` each, which serves as the entry check before entry and as the refresh banner after it.
- Step 6: `SprintList` takes `loading` / `error` as props. `SprintView` counts the participants error only when it started that load. `ReportView` keeps its error card and adds Retry.
- Step 7: a failed hub `connect` is a stage-3 room error with Retry. Stage 1 runs policy and summary side by side. Stage 3 re-fetches the summary.
- Step 8: the in-flight check sits in the handlers as well as on `:disabled`; the assignee `<select>` is not disabled.
- Step 9: `canMutateCard` checks run in the server's order (owner, closed, phase, Discussing action item).
- Step 10: create-sprint and delete-team errors are read like `CreateTeamForm` (`errors.generalErrors[0]`), with a fixed fallback.
- Step 11: the header's teams load watches the route; the router guard makes sure auth is known first.
- Step 12: `pointerdown`, `click` and `keydown` unlock the chime; the listeners stay attached for the room's lifetime.
- Step 13: the walkthrough is CDP-driven headless Chrome against a scratch seeded database; it covers guest flows only.

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | tailwind-theme | |
| 2 | tailwind-theme | departs § White Flash Prevention (plan Decision 11) |
| 3 | vue-patterns | |
| 4 | pinia-patterns | departs setup stores (plan Decision 12) |
| 5 | pinia-patterns | departs setup stores (plan Decision 12) |
| 6 | create-vue-feature | departs Page Shell adoption (plan Decision 13) |
| 7 | vue-patterns | |
| 8 | vue-patterns | |
| 9 | vue-patterns | |
| 10 | pinia-patterns | |
| 11 | vue-patterns | |
| 12 | vue-patterns | |
| 13 | None | plan: no pattern skill (verification only); TDD no on every step (plan § Skill Mapping) |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| Bare `border-b` / `border-t` render black lines in light theme | low | architect | `RetroColumnPanel.vue:9` (`border-b`, column header) and `:67` (`border-t`, input row) have no colour class; Tailwind v4's default border colour is `currentColor`, so black rules show under each column header and above its input in light (`.claude/tmp/walk/shots/room-mobile-light.png`) | Already there before F36 (F36 changed only the radius class on line 9); not one of the named defects, so not fixed — an F39 candidate |
| Unused `MUTE_KEY` constant | low | reviewer | `useTimer.ts:12` `const MUTE_KEY = 'reflekt:timer:muted'` has no reader (mute lives in `stores/timer.ts`) | Resolved — removed in Fix Round 1 (row 3) |
| Report Retry also shows on 401/403 | low | reviewer | `ReportView` shows Retry on any `error`; a guest without a token sees "Not authorized" + Retry, which cannot succeed | The plan's Step 6 bullet reads "the message gains Retry" with no status split; kept as built |
| Walkthrough covers guest flows only | medium | architect | AC6 items needing Google sign-in, Scrum Master phase moves, a hub-triggered refresh, or hearing audio were not observed | Listed under OPERATOR ACTION REQUIRED in Step 13; AC6 stays open until Step 14 |

## Deviations from Plan
- Steps 2, 4, 5, 6: skill departures the plan sanctions (Decisions 11, 12, 13); no other deviation in Steps 1–12.
- Step 5 → 7: an interim gap (a refused guest token no longer threw) existed between Step 5 and Step 7 and was closed in Step 7's stage-2 identity check.
- Step 13: the AC6 walkthrough was driven through CDP in headless Chrome rather than by hand in devtools. "Offline" was injected per request (`InternetDisconnected`), not as browser-wide offline, so the SPA could still load. Items needing sign-in, phase changes, a hub refresh or hearing audio go to the operator (Step 14). `graphify update .` was not run because `graphify` is not installed.

*Status: COMPLETE — developer, 2026-10-23*
