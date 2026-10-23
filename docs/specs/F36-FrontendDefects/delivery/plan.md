# F36 — Frontend Visual & UX Defects

**Feature Spec:** `docs/specs/F36-FrontendDefects/definition/spec.md`
**Model:** claude-opus-5-5[1m]
**Build slices:** 1–3 · 4–7 · 8–13; why: visual fixes, then the load-and-error chain (stores before the views that read them), then the interaction fixes and the verification walkthrough
**Size:** 13 developer-built steps · 45 files · fullChecks: none — no fullChecks list declared
**Lane:** full — normal gear (as today, by size: 13 developer-built steps, 45 files)
**Build lane:** thorough — full team — normal gear
**Gear:** normal — campaign

## Context

The SPA builds and type-checks clean, but users see and hit defects: square corners everywhere (Tailwind v3 bracket syntax under v4), a dark page in the light theme, unreadable discussed group in dark mode, card tints that never render, a silent timer chime, typed notes lost on a failed save, pages that hang on "Loading" or show stale/empty data when a fetch fails, blank team names on reload, an admin view reachable signed-out, edit/delete buttons the server then refuses, a wrong "Back to sprints" link and a double-firing Enter on `BaseInput`.

One service is touched: the Vue SPA in `web/reflekt-web/src` (built into `src/Services/Reflekt/Reflekt.API/wwwroot/`). No backend, no API, no SignalR protocol change.

Campaign constraints (binding): no new product behaviour; no new tests (an existing test is updated only where a fix changes what it asserts — there are no frontend tests); no business-rule registries; never edit `docs/backlog.md`, `web/reflekt-web/package.json` or `web/reflekt-web/package-lock.json` — the close records the backlog change in `docs/specs/F36-FrontendDefects/delivery/change-notes.md`. Build and type-check run against the current lockfile (shared architect AR-1: F36 does not wait for F37's package change notes).

## Scope

**In scope** — the 13 spec rows: FE-04, FE-11, FE-05, FE-20, FE-12, FE-13, FE-07 (part), FE-14, FE-08, FE-28, FE-31, FE-32, FE-22 (part). Each row's "Required outcome" in the spec is binding; the steps below carry only what the plan adds to it.

**Out of scope** — every other FE finding (F39), including the setup-store conversion (FE-01), the shared `ApiError` shape (FE-18), `@apply` removal (FE-34) and Page Shell adoption (FE-21, FE-25); frontend tests (FE-43, F40); backend and doc changes, including `docs/conventions/vue.md` (F40) and the `tailwind-theme` skill (not edited — the drift is recorded in `lessons.md` § Skill Gaps).

**Rule list:** not applicable — the spec header reads `Rule list: not applicable`.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | tailwind-theme | Follow | no | the six bracket forms → theme utilities; `max-w-6xl` on `PageLayout` | — |
| 2 | tailwind-theme | Follow | no | `index.html` head `<style>`; token `surface-focus`; literal tint map in `RetroCardItem` | — |
| 3 | vue-patterns | Follow | no | `BaseInput` attrs split (`class`/`style` → wrapper) | — |
| 4 | pinia-patterns | Follow | no | `sprint`, `teams`, `report`, `auth` load actions | — |
| 5 | pinia-patterns | Follow | no | `card`, `cardGroup`, `participant`, `policy`, `vote`, `previousActions` load actions; refresh option for the hub | — |
| 6 | create-vue-feature | Follow | no | Page Shell states plus an error state, in four views + App notice | — |
| 7 | vue-patterns | Follow | no | `useRetroRoom` staged entry, retry, refresh banner, report retry | — |
| 8 | vue-patterns | Follow | no | `addNote` / `addActionItem` return success; in-flight state per input | — |
| 9 | vue-patterns | Follow | no | `canMutateCard` predicate | — |
| 10 | pinia-patterns | Follow | no | Async Action Pattern inside `CreateSprintForm` and `DeleteTeamDialog` | — |
| 11 | vue-patterns | Follow | no | header team-name load on route entry; `requiresAuth` on `sprint`; back link | — |
| 12 | vue-patterns | Follow | no | module-level `AudioContext` in `useTimer.ts`; room-wide unlock listener | — |
| 13 | (none) | — | no | build, acceptance greps, recorded walkthrough, `graphify update .` | — (verification only, no pattern to capture) |

TDD is `no` on every step: the frontend has no test setup and the campaign forbids new tests; each outcome is proven by the acceptance greps, `type-check`/`build`, review of the diff, and the walkthrough (Steps 13–14).

Where a step departs from its skill, the departure is a `Departs from` row in `## Decisions` (rows 10–13), each required by the spec's scope.

## Domain Model Changes

None.

## Data Model Changes

None.

## Implementation Steps

All paths are under `web/reflekt-web/` unless they start with `src/Services` or `docs/`. After each step run `npm --prefix web/reflekt-web run type-check` (through `verify-run.js` per your seat's test-entry rule where it applies); after Step 1, 2 and 13 also run `npm --prefix web/reflekt-web run build`.

### Step 1 — Corners and report width use the theme's utilities (FE-04, FE-11)

Follow tailwind-theme.

- Replace every v3 bracket radius form with the utility the `@theme` block (`src/assets/main.css` `--radius-sm|md|lg|full`) already generates: `rounded-[--radius-X]` → `rounded-X`, `rounded-t-[--radius-md]` → `rounded-t-md`, `rounded-b-[--radius-md]` → `rounded-b-md` — 49 sites in 18 files, the two `@apply` lines in `TimerControls.vue`'s `<style scoped>` included (the `@apply` itself stays — Decision 10). Any other `{prefix}-[--radius-X]` form the grep finds maps the same way.
- `src/components/layout/PageLayout.vue`: `max-w-[--page-max-width]` → `max-w-6xl`.
- Files: `src/components/AppHeader.vue`, `src/components/layout/PageLayout.vue`, `src/components/retro/PreviousActionsPanel.vue`, `src/components/retro/RetroBoardPanel.vue`, `src/components/retro/RetroCardItem.vue`, `src/components/retro/RetroColumnPanel.vue`, `src/components/retro/RetroGroupItem.vue`, `src/components/retro/RetroJoinForm.vue`, `src/components/retro/RetroMobileTabBar.vue`, `src/components/retro/RetroPhaseHeader.vue`, `src/components/retro/RetroReportPanel.vue`, `src/components/retro/TimerControls.vue`, `src/components/ui/BaseBadge.vue`, `src/components/ui/BaseButton.vue`, `src/components/ui/BaseCard.vue`, `src/components/ui/BaseInput.vue`, `src/components/ui/BaseModal.vue`, `src/components/ui/ToastContainer.vue`, `src/views/RetroRoomView.vue`.
- Accept: `grep -rnE "\[--[a-z-]+\]" web/reflekt-web/src` → no hits (spec AC1); after the build, the CSS under `src/Services/Reflekt/Reflekt.API/wwwroot/assets/` contains `border-radius:var(--radius-md)` (spec AC3, first half).
- Satisfies: spec FE-04, FE-11, AC1, AC3.

### Step 2 — Light page background, focused-group surface, card tints (FE-05, FE-20, FE-12)

Follow tailwind-theme — except its § White Flash Prevention, which the spec replaces (Decision 11).

- `index.html`: remove the `style` attribute from `<body>`; add a `<style>` block in `<head>` with `html { background: #0a0a0f }` and `html.light { background: #f4f4f8 }` (the `--surface-page` values in `main.css` `:root` / `.light`). The existing blocking script is unchanged.
- `src/assets/main.css`: add a theme-aware surface token for the focused group — raw var `--surface-focus` in `:root` (`color-mix(in srgb, #6366f1 15%, #13131a)`) and `.light` (`#eef2ff`, today's light look), aliased in `@theme` as `--color-surface-focus: var(--surface-focus)`.
- `src/components/retro/RetroGroupItem.vue` `groupBgClass`: the focused branch uses `bg-surface-focus` (ring unchanged). The other `primary-50/100` sites (`AppHeader.vue` avatar chip, `RetroJoinForm.vue` avatar chip, `PhaseIndicator.vue` ring, `RetroColumnPanel.vue` drag-over `bg-primary-50/30`) are left as they are unless the walkthrough (Step 13 or 14) finds one unreadable in dark — then it takes the same token, recorded in `implementation.md`.
- `src/components/retro/RetroCardItem.vue` `cardBgClass`: replace the built string with a literal map from column colour to its tint class — `green|red|amber|blue|purple|teal|pink|slate` → `bg-col-{colour}-light/30`, `action-items` → `bg-action-items-light/30`. Keep today's other outcomes: no colour → `bg-surface`; a colour outside the map → no background class (today's built class renders nothing). Do not touch the other dynamic sites the finding lists (they are safelisted and render).
- Accept: `grep -n "<body" web/reflekt-web/index.html` has no `style=`; the `<head>` has the `<style>` block with `html` and `html.light` (spec AC2); the built CSS contains `bg-col-{colour}-light\/30` for all eight colours and `bg-action-items-light\/30` (spec AC3, second half).
- Satisfies: spec FE-05, FE-20, FE-12, AC2, AC3.

### Step 3 — `BaseInput` binds attributes once (FE-22 part)

Follow vue-patterns.

- `src/components/ui/BaseInput.vue`: `defineOptions({ inheritAttrs: false })`; `class` and `style` go on the wrapper `<div>` (keeps `class="flex-1"` sizing as today), every other attribute and listener goes on the `<input>` only. Props, emits and markup otherwise unchanged (`defineModel` and ARIA are F39).
- Accept: `grep -n "inheritAttrs: false" web/reflekt-web/src/components/ui/BaseInput.vue` → present (spec AC5); an `@keydown.enter` passed to `BaseInput` reaches the input exactly once (walkthrough "one Enter adds exactly one note").
- Satisfies: spec FE-22, AC5.

### Step 4 — Load contract: `sprint`, `teams`, `report`, `auth` stores (FE-08 part)

Follow pinia-patterns (Async Action Pattern; Store vs View State) — within the existing Options-style stores (Decision 12).

**The load contract** every load action in Steps 4 and 5 meets (Decision 1, P1):
- **Per resource status.** Each resource a store loads has its own `loading` and `error: string | null`. One-resource stores keep one pair; `sprint` has three resources (`sprints`, `currentSprint`, `currentSummary`), `participant` two (`participants`, `currentParticipant`), `report` one (both its fetches load `report`). Names are the developer's.
- **Foreground load (the default).** On start: set the resource's `loading`, clear its `error` and **clear its data** (spec FE-08).
- **Background refresh (cards and groups only, opt-in — Step 5).** Keeps the data on screen while it runs; on success replaces it.
- **Failure** (non-OK response or a rejected `fetch`): set the resource's `error` to a short user-facing message; never throw; never map the failure to an empty list. **Success** clears the resource's `error`.
- **Latest request wins.** Each resource commits data, `error` and `loading` only from its latest request; a response to an older request is discarded (e.g. a per-resource request counter). This covers `SprintListView`'s `teamId` watch and overlapping group refreshes.

- `src/stores/sprint.ts`: `fetchTeamSprints` (→ `sprints`), `fetchSprint` (→ `currentSprint`), `fetchSprintSummary` (→ `currentSummary`). Keep `loaded`. Mutating actions (`createSprint`, `updateColumns`, `startSprint`, `advancePhaseCheck`, `advancePhase`, `closeSprint`) keep throwing as today (Decision 12).
- `src/stores/teams.ts`: `fetchMyTeams` (→ `teams`). `createTeam` / `deleteTeam` unchanged.
- `src/stores/report.ts`: `fetchReport` and `fetchParticipantReport` (→ `report`) — keep the existing 404 / 401-403 / other messages; add the clear on start and catch network failures (today a rejected `fetch` escapes the `finally`).
- `src/stores/auth.ts`: `fetchMe` never rejects. A network failure leaves `user = null`, `loaded = true`, and sets an `error` the App notice (Step 6) reads; any HTTP response keeps today's behaviour (`ok` → user, otherwise signed out, no message).
- Accept (review of the diff): in every load action above except `auth.fetchMe`, both the non-OK branch and a rejected `fetch` set the resource's `error`; `auth.fetchMe` sets its `error` only on a rejected `fetch` (network failure) — a non-OK response (a guest's normal 401 included) leaves the user signed out with no `error`; no `res.ok ? … : []`, no `if (!res.ok) return`, no `if (res.ok) { … }` without an `else` that sets `error`; each commit is guarded by the latest-request check; `fetchMe` has a `catch` that sets `loaded`; `type-check` green.
- Satisfies: spec FE-08 (stores `sprint`, `teams`, `report`; `fetchMe`).

### Step 5 — Load contract: room stores, and the room's refreshes (FE-08 part)

Follow pinia-patterns. Same contract as Step 4.

- `src/stores/card.ts` `fetchCards` (→ `cards`) and `src/stores/cardGroup.ts` `fetchGroups` (→ `groups`; `voteStore.initVoteCounts` only on success) each take a background-refresh option (Step 4 contract). Callers that use it: all eight `fetchCards`/`fetchGroups` calls in the hub handlers of `src/composables/useSprintHub.ts` (today at lines 67, 68, 70, 71, 108, 112, 116, 120 — the phase-change handler and the four group handlers; passing the option is the only edit there, the handlers themselves are unchanged) and the reload after a successful write in `cardGroup.ts` (`createGroup`, `addCardToGroup`, `removeCardFromGroup`, `dissolveGroup`, `updateGroupTitle`); a failed reload there sets `error` and does not turn the write into a failure. The room entry (Step 7) calls them as foreground loads.
- `src/stores/participant.ts`: `fetchParticipants` (→ `participants`) and `fetchMe(sprintId)` (→ `currentParticipant`) both follow the contract; `fetchMe` stops throwing and keeps the failing HTTP status beside its `error` (e.g. `meStatus`) so the room can still clear a stale token on 401 (Step 7). `join`, `joinWithGoogle`, `kickParticipant` unchanged.
- `src/stores/policy.ts` `fetch` (→ `matrix`).
- `src/stores/vote.ts` `fetchMyVotes` (→ `myVotes`); the no-token early return stays.
- `src/stores/previousActions.ts` `fetchPreviousActions` (→ `items`, `previousSprintId`, `previousSprintName`); it already has `loading` — add `error`.
- Accept: as Step 4 for these six stores; `git diff -- web/reflekt-web/src/composables/useSprintHub.ts` touches only the arguments of the eight `fetchCards`/`fetchGroups` calls, and every one of them passes the background option (`grep -nE "fetch(Cards|Groups)\(" web/reflekt-web/src/composables/useSprintHub.ts` → 8 hits, each with the option).
- Satisfies: spec FE-08 (stores `card`, `cardGroup`, `participant`, `policy`, `vote`, `previousActions`; the background-refresh clause).

### Step 6 — Error states with retry in the views; App unreachable-server notice (FE-08 part)

Follow create-vue-feature (Page Shell Pattern) — with an error state added and no `PageLayout`/`PageToolbar` adoption (Decision 13). The error state reuses `src/components/ui/EmptyState.vue` (title = the resource's message) with a `BaseButton` "Retry" in its slot that re-runs the failed load(s) (Decision 5). In every view the order is loading → **error** → empty → content: an `error` hides any data the store still holds.

- `src/views/TeamDashboardView.vue`: on the teams `error` show the error state, never the create-first-team form; Retry → `fetchMyTeams`.
- `src/views/SprintListView.vue` + `src/components/sprints/SprintList.vue`: a failed `fetchTeamSprints` shows the error state instead of "No sprints yet" (how the state reaches the list — props or the view — is the developer's); Retry → `fetchTeamSprints(teamId)`.
- `src/views/SprintView.vue`: loading, error and content are distinct — "Loading sprint…" only while loading; a failed `fetchSprint` (404 included) or `fetchParticipants` shows the error state; Retry re-runs the `onMounted` loads.
- `src/views/ReportView.vue`: `error` takes precedence over a held `report`; the message gains Retry → `fetchReport(teamId, sprintId)`.
- `src/App.vue`: while the auth `error` is set, a one-line notice that the server can't be reached (tokens only, no raw palette colours). The `<RouterView>` is keyed by the route path (`route.path`), so a change of team or sprint id within one route mounts the view afresh — its mount-time loads run for the new id, Retry uses the new id, and the retro room unmounts (hub disconnect, unlock listener removed) before the next sprint's room starts (Decision 14).
- Accept: each view has a branch on its resource's `error`, placed before its content branch, with a Retry control; offline, none of the four views shows an empty-data message or another target's data (walkthrough Failures); `src/App.vue`'s `<RouterView>` carries a `:key` bound to the route path; going back/forward between two sprint (or report, or room) URLs shows each one's own data (walkthrough Deep links).
- Satisfies: spec FE-08 (views; `fetchMe` message).

### Step 7 — Retro room: staged entry with retry, refresh banner, report retry (FE-08 part)

Follow vue-patterns (Composable Pattern).

`src/composables/useRetroRoom.ts` runs the entry in stages; each stage checks its loads' `error` before the next starts, and any failure sets the room error (message), sets `loading = false` and stops (Decision 2, P2):
1. **Room info** — `policyStore.fetch` and `fetchSprintSummary`. A failure here shows the room error, never the join form.
2. **Identity** — `authStore.fetchMe` if not loaded (never rejects). Signed in → `joinWithGoogle`: its failures keep today's join-form messages. Guest with a stored token → `participantStore.fetchMe`: a 401 clears the token and shows the join form (today); any other failure is a room error. No token → join form. A join through the form (`handleJoin`) continues at stage 3; its own failures keep today's join-form messages.
3. **Room data** — `loading = true` for the stage; foreground `fetchParticipants`, `fetchCards`, `fetchGroups`, `fetchSprintSummary`, `fetchPreviousActions`, then `fetchMyVotes`. Only when all succeeded: `connect` (the hub connects only after the loads succeed, so a retry never opens a second connection), `joined = true`, then the Reporting report load as today.

- **Retry** clears the room error, sets `loading = true`, is a no-op while an attempt is running, and resumes at the stage that failed — a participant already identified or joined is not joined again.
- `src/views/RetroRoomView.vue`: an error branch (before the join form and the board) showing the message with Retry (`EmptyState` + `BaseButton`, as Step 6).
- **Refresh banner** — after entry, when a background refresh of cards or groups (Step 5) has failed, `RetroRoomView.vue` shows a one-line notice above the board that the board could not be refreshed, with Retry → background refresh of cards and groups; the board stays on screen; the notice clears when a refresh succeeds (spec FE-08 background clause).
- **Report retry** — `src/components/retro/RetroReportPanel.vue`'s existing error line gains a Retry that asks the view to re-run `fetchParticipantReport(sprintId)` (both the after-connect load and the phase-change load at `RetroRoomView.vue` use the same store); the hub connection is untouched.
- Accept: offline or with a 500 on any stage-1 or stage-3 load, the room shows the error state — never the join form with a placeholder name, never an empty board, never stuck loading; Retry back online enters with one hub connection; a failed hub refresh keeps the board and shows the banner (walkthrough).
- Satisfies: spec FE-08 (retro room; background clause).

### Step 8 — Note and action-item inputs keep text on a failed save (FE-07 part)

Follow vue-patterns.

- `src/composables/useRetroBoardActions.ts`: `addNote` and `addActionItem` report success or failure to the caller (e.g. `Promise<boolean>`); the error toast stays.
- `src/components/retro/RetroBoardPanel.vue`: per input (each note column and the action-item input) an in-flight flag; while it is set the `BaseInput` and its Add button are `disabled` and a second Enter/click is ignored; the text (and the action-item assignee) is cleared only on success and kept on failure. Creates stay non-optimistic (F39).
- No focus handling is added: `BaseInput` exposes nothing new and focus is not moved after a save (Decision 3 — dropped).
- Accept: in `RetroBoardPanel.vue`, the clears of `newCardText` / `newActionText` / `newActionAssignee` run only after a successful add; walkthrough Failures (text stays, one toast, no duplicate on a second Enter; back online one Enter adds exactly one note and clears the input).
- Satisfies: spec FE-07 (part). Depends on Step 3.

### Step 9 — Edit/delete show only where the server allows them (FE-31)

Follow vue-patterns.

- `src/composables/useRetroBoardActions.ts` `canMutateCard`: true only when all hold — the card is the caller's own (`card.participantId === participantStore.currentParticipant?.id`; drop the `|| authStore.user !== null` branch); the policy matrix allows `EditNote` / `EditActionItem` for the card type in the current phase; the sprint is not closed (`sprintStore.currentSummary?.state !== 'Closed'` — the hub sets it on `SprintClosed`); and, for an action item in the `Discussing` phase, the caller is signed in with Google (`authStore.isSignedIn`). One flag still drives both buttons (Decision 4).
- Server rule mirrored: `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs` and `.../DeleteCard/DeleteCardEndpoint.cs` (closed → 409, participant claim ≠ owner → 403, phase action → 409, Discussing action item without Google → 403). No permission added or removed; no backend change.
- Accept: `grep -n "authStore.user !== null" web/reflekt-web/src/composables/useRetroBoardActions.ts` → no hits; walkthrough Roles.
- Satisfies: spec FE-31.

### Step 10 — Create-sprint and delete-team forms own their calls (FE-14)

Follow pinia-patterns (Async Action Pattern), with `src/components/teams/CreateTeamForm.vue` as the in-repo shape.

- `src/components/sprints/CreateSprintForm.vue`: gains a `teamId: number` prop (Decision 8) and awaits `sprintStore.createSprint(teamId, payload)` itself inside try/catch; `busy` stays on until the call ends; `sprintName` and `votesPerParticipant` reset only on success (the fields reset today); a failure shows a message in the form's existing `error` line. Remove the `create` emit.
- `src/components/sprints/SprintList.vue`: takes `teamId`, passes it to the form; drop the `create-sprint` emit. `src/views/SprintListView.vue`: pass `teamId`; delete `handleCreateSprint`. The store already prepends the new sprint to the list.
- `src/components/teams/DeleteTeamDialog.vue`: awaits `teamsStore.deleteTeam(team.id, confirmationName)` itself inside try/catch; `busy` stays on until the call ends; a failure shows an error message inside the dialog and keeps the typed name; on success it emits that it is done (e.g. `deleted`). `src/views/TeamDashboardView.vue`: `pendingDeleteTeam = null` on that event; delete `handleDeleteConfirm`.
- Accept: no `emit('create'` in `CreateSprintForm.vue`; no `emit('confirm'` in `DeleteTeamDialog.vue`; walkthrough Failures (values kept, error shown).
- Satisfies: spec FE-14.

### Step 11 — Team name on cold entry, guarded admin view, correct back link (FE-28, FE-32)

Follow vue-patterns.

- `src/components/AppHeader.vue`: on each route entry to `team-sprints`, `sprint` or `report` (a cold load included — watch the route, not the store), when the user is signed in, the teams list is empty, no teams load is in flight **and the teams `error` is not set**, call `teamsStore.fetchMyTeams()` — so both the header breadcrumb and `SprintList`'s heading get the name. The header never retries on its own; guests are never sent to `/api/teams`.
- `src/router/index.ts`: `requiresAuth: true` on the `sprint` route only; `report` unchanged.
- `src/components/sprints/SprintDetailPanel.vue`: "Back to sprints" → `{ name: 'team-sprints', params: { teamId } }` (prop `teamId` exists).
- Accept: spec AC5 greps (`team-sprints` in `SprintDetailPanel.vue`; `sprint` route has `requiresAuth`, `report` does not); walkthrough Deep links; offline, reloading `team-sprints` sends one `/api/teams` request, not a stream.
- Satisfies: spec FE-28, FE-32, AC5. Depends on Step 4 (teams contract).

### Step 12 — Audible expiry chime (FE-13)

Follow vue-patterns (module-level shared state, per `docs/conventions/vue.md` § Reactivity Gotchas).

- `src/composables/useTimer.ts`: delete the base64 WAV, `getChimeAudio` and its misleading comment. One module-level `AudioContext` (created lazily; `window.AudioContext` absent → silence) with an exported unlock function that creates or resumes it; the expiry plays a ~300 ms 440 Hz tone (oscillator + short gain envelope) only when not muted and the context is running — otherwise silence. `toggleMute` also calls the unlock.
- `src/views/RetroRoomView.vue`: for the room's lifetime, a document-level click/pointer and keydown listener calls the unlock (so a click before the timer is visible counts); removed on unmount.
- Accept: `grep -rn "BASE64_CHIME\|data:audio" web/reflekt-web/src` → no hits; `grep -n "AudioContext" web/reflekt-web/src/composables/useTimer.ts` → present (spec AC4); walkthrough Chime.
- Satisfies: spec FE-13, AC4.

### Step 13 — Verify: build, greps, recorded walkthrough, graph update

No pattern skill.

- Run `npm --prefix web/reflekt-web run type-check` and `npm --prefix web/reflekt-web run build` (both green), then spec AC1–AC5 greps from the repo root; record each command and result in `implementation.md`.
- Run the spec AC6 walkthrough against the app (API serving the built SPA, e.g. `dotnet run` on `src/Services/Reflekt/Reflekt.API`, or the `run` skill), recording for every item what was seen: Light and dark cold load · Board states (plus the Step 7 refresh banner) · Failures (each twice: offline and with a 500 via devtools request blocking/overriding) plus the back-online retry and the single-Enter check · Roles · Deep links · Chime. Also record whether each of the four `primary-50/100` sites from Step 2 is readable in dark.
- Every AC6 item this session cannot observe (Google sign-in, hearing the tone, no browser) is listed under `OPERATOR ACTION REQUIRED` in `implementation.md` with exact steps — it is **not** recorded as seen, and AC6 stays open until Step 14 records it (Decision 9).
- Run `graphify update .` from the repo root (`CLAUDE.md` § graphify); if the command is not installed, record that in `implementation.md`.
- AC7 (the declared full role) is the handover complete run, from the coordinating session — not this step.
- Satisfies: spec AC1–AC5; AC6 for the items observed here.

### Step 14 — Operator completes the AC6 walkthrough (Owner: operator)

Owner: operator — outside the build slices; `N/A (operator-owed)` at the done check.

- The operator performs every item Step 13 listed under `OPERATOR ACTION REQUIRED` and appends what was seen to `implementation.md` (a `## Walkthrough — operator` block). If a `primary-50/100` site is unreadable in dark, that becomes a fix (Step 2 rule) before AC6 is met.
- Until this step is recorded, AC6 is open: the close lists it in `summary.md` as open acceptance, and the change note for the backlog row (`delivery/change-notes.md`) reads built, AC6 walkthrough pending — not done.
- Satisfies: spec AC6 (remaining items).

## Cross-Service Changes

None.

## Migration Notes

None.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| Route paths and names; only `sprint` gains `requiresAuth` | `git diff` of `src/router/index.ts` shows one added `requiresAuth: true` on `sprint` | pinned (review diff) |
| API calls, payloads and headers (URLs, methods, `X-Guest-Token`, `credentials`) | `git diff -- web/reflekt-web/src/stores` shows no changed `fetch(` URL, method, header or body line | pinned (review diff) |
| SignalR handling | `git diff -- web/reflekt-web/src/composables/useSprintHub.ts` touches only the arguments of the eight `fetchCards`/`fetchGroups` calls | pinned (review diff) |
| Guests keep the report; signed-in users keep everything | AC5 router grep; walkthrough Roles (guest opens report with token) | pinned (grep) + accepted-gap (manual, Steps 13–14) |
| Layout and colours outside the named defects; `class="flex-1"` sizing of note inputs | walkthrough Light/dark + Board states | accepted-gap (manual — no visual tests by campaign rule) |
| A hub or post-write refresh never blanks the board | walkthrough Board states with a second client grouping cards | accepted-gap (manual) |
| `type-check` and `build` stay green | Step 13 commands; handover complete run (`roles.full`) | pinned |

## Testing Strategy

No new tests (campaign rule; the frontend has no test setup). Proof is: the spec AC1–AC5 greps and the built-CSS checks; `type-check` + `build` after each step; review of the store diffs against the Step 4 contract; the recorded AC6 walkthrough (Step 13, completed by the operator in Step 14); the backend suites in `roles.full` stay green (no backend change).

## KB Impact

None — `docs/kb/index.md` does not exist and no KB entry covers the frontend. No anchored set (`docs/conventions/anchored-set.md` absent), so no re-anchor step.

## Decisions

| # | Decision | Why | Rejected alternative | Confidence | Status |
|---|---|---|---|---|---|
| 1 | Loads clear their data on start (spec FE-08); only a refresh of cards or groups that the room starts itself — a hub event or the reload after a group write — keeps the board on screen, and its failure shows a retry banner. The spec FE-08 row was reconciled to say so (spec § Decisions). See plan P1. | `fetchGroups`/`fetchCards` are re-run by hub handlers (`useSprintHub.ts:67-71,108-120`) and after every group write (`cardGroup.ts:64,79,92,105,158`); a plain clear blanks the live board on every change. Round 1 (R1-01) rejected the earlier "keep same-target data" rule. | Keep same-target data on every load (round 1 draft); clear on every load. | high — read from the callers | decided |
| 2 | The room's entry runs in three checked stages with a retry that resumes at the failed stage; a failed background refresh keeps the board and shows a retry banner above it. See plan P2. | Spec FE-08: the room shows the error instead of an empty board, and errors are surfaced with a retry; the staging keeps the join form and the hub out of failure paths (R1-02, R1-04). | Room error from one check after all loads (round 1 draft); a room that tears down the board on any store error. | high — read from `useRetroRoom.ts` | decided |
| 3 | No focus return after a note or action-item save: the `Adds:` row proposed at round 1 was dropped at the plan-departures ask (owner away — away line, presumed (proceed-default), not user-confirmed). After a save the input may lose focus. See plan P3, P4. | Campaign rule: no new product behaviour; no source asks for the return (R2-03). | Return focus after an Enter-started save (the dropped `Adds:` row). | high — the campaign directive is binding | decided |
| 4 | One `canEdit` flag (keyed on `EditNote`/`EditActionItem`) still drives both Edit and Delete. | The seeded matrix grants Edit and Delete in the same phases (`PhaseActionConfiguration.cs:17-18,28-29`); a second prop is restructuring for no visible change. | Separate `canEdit` / `canDelete` from the Delete actions. | high — matrix read | decided |
| 5 | Error states reuse the existing `EmptyState.vue` + `BaseButton` Retry. | An existing, unused component built for this; no new pattern. | A new `ErrorState.vue`; inline markup per view. | high | decided |
| 6 | Focused-group token is `--surface-focus` / `bg-surface-focus` (dark ≈15% indigo over card, light `#eef2ff`). | Matches the token naming in `main.css`; light keeps today's look. | `bg-primary-500/15` (not a token, as the spec asks). | high | decided |
| 7 | `participant.fetchMe(sprintId)` follows the load contract and stops throwing; it keeps the failing HTTP status so the room still clears a stale token on 401. Replaces the round-1 draft (keep throwing). | Spec FE-08 covers every fetch in the `participant` store (R1-06). | Keep it throwing outside the contract. | high | decided |
| 8 | `CreateSprintForm` takes `teamId` as a prop (via `SprintList`); the unused emit chain and `SprintListView.handleCreateSprint` go. | The form must own the call (spec FE-14); a prop keeps it route-independent like its siblings. No `**Mirrors:**` line: the owner named no pattern; `CreateTeamForm` is the in-repo example, and it has no team id to take. | Read `route.params.teamId` inside the form. | medium — both work | decided |
| 9 | AC6 items the build session cannot observe go to an operator step (Step 14); AC6 stays open until they are recorded, and the close reports it as open acceptance. Replaces the round-1 draft (fallback counted as a valid deviation). | Spec AC6 requires an observation for every item (R1-05); Google OAuth and hearing a tone cannot be automated here. | Count unobserved items as a justified deviation; block the build on a manual pass. | high | decided |
| 10 | Departs from tailwind-theme (no `@apply` in components), required by spec FE-04 / § Out of scope: `TimerControls.vue` keeps its `@apply` rules; only their radius forms change. | Removing `@apply` is FE-34, owned by F39; the spec names the `@apply` sites only for the radius fix. | Rewrite the timer buttons with utilities now. | high | decided |
| 11 | Departs from tailwind-theme § White Flash Prevention (inline `<body>` colour), required by spec FE-05 and spec § Decisions row 3: the colour moves to a `<head>` `<style>` on `html` / `html.light`. | The inline style is the FE-05 defect; the skill is not edited (owner decision 2026-10-12), the drift is in `lessons.md` § Skill Gaps. | Keep the inline style and patch it from script. | high | decided |
| 12 | Departs from pinia-patterns (setup stores; `error` from a thrown `Error`), required by spec § Out of scope: the stores stay Options-style and mutating actions keep their plain-object throws; only the load actions gain the FE-08 contract. | Setup-store conversion (FE-01) and one `ApiError` shape (FE-18) are F39's. | Convert the stores while touching them. | high | decided |
| 13 | Departs from create-vue-feature § Page Shell Pattern (initializing → empty → content in `PageLayout` + `PageToolbar`), required by spec FE-08 and § Out of scope: the views gain an error state between loading and empty, and do not adopt `PageLayout`/`PageToolbar`. | FE-08 requires a distinct error state; Page Shell adoption is FE-21/FE-25, owned by F39. | Adopt the full Page Shell now. | high | decided |
| 14 | `App.vue`'s `<RouterView>` is keyed by the route path, so a change of id within one route remounts the view. See Step 6. | `SprintView`, `ReportView` and `RetroRoomView` read their ids once and load on mount; Vue Router reuses the instance on a param change, so without a remount the old target stays on screen (R2-02). One key covers every view and tears the room down through its existing `onUnmounted`. | Watch the route params in each view (three lifecycles to rewrite). | high — Vue Router reuse is documented; read from the views | decided |

## Decisions and why

### P1 — Clear on load; the room's own refreshes keep the board
- **By:** architect
- **Decided:** every load clears its data on start as spec FE-08 says; a refresh of cards or groups that the room starts itself (hub event, reload after a group write) keeps the board on screen and reports a failure through a retry banner; only a resource's latest request may commit.
- **Why:** architect's reasoning — the hub handlers and the group writes reload cards and groups, so a clear on every load would blank the live board on every change; round 1 of the plan review showed the earlier draft (keep same-target data everywhere) overrode the spec and could hide an error behind held data. The spec's FE-08 row now carries the refresh clause.
- **Turned down:** keep same-target data on every load — overrides the spec and hides errors; clear on every load — blanks the live board.
- **Rests on:** `fetchGroups`/`fetchCards` being re-run by hub handlers and group writes; if F39 makes those updates local, the refresh path can go.
- **Confidence:** high — read from `useSprintHub.ts` and `cardGroup.ts`.
- **Source:** Phase 1 analysis, 2026-10-23; plan review round 1 (R1-01, R1-03), 2026-10-23.
- **State:** current

### P2 — Room entry in checked stages; refresh failures as a banner
- **By:** architect
- **Decided:** the room checks room info, then identity, then room data, stopping on the first failure with a room error and a retry that resumes at the failed stage; a failed background refresh keeps the board and shows a retry banner.
- **Why:** architect's reasoning — the spec asks for the error instead of an empty board and for errors surfaced with a retry; round 1 showed one check after all loads lets an offline guest fall through to the join form and leaves the room stuck loading, and that background failures were invisible.
- **Turned down:** one check after all loads (round 1 draft); tearing down the board on any store error.
- **Rests on:** the hub connecting only after the stage-3 loads, so a retry cannot double-connect.
- **Confidence:** high — read from `useRetroRoom.ts`.
- **Source:** Phase 1 analysis, 2026-10-23; plan review round 1 (R1-02, R1-04), 2026-10-23.
- **State:** current

### P3 — Focus returns to the input after a keyboard save
- **By:** architect
- **Decided:** when a save started by Enter ends, focus returns to that input unless focus moved elsewhere — tagged `Adds:` for the owner's keep or drop.
- **Why:** architect's reasoning — the spec's "disabled while in flight" drops focus, which would break today's keyboard flow; no source asks for the return, so it is disclosed as an addition (round 1, R1-08).
- **Turned down:** leave focus lost; `readonly` in place of `disabled`.
- **Rests on:** browsers dropping focus from a disabled input.
- **Confidence:** medium — a reasonable owner might accept the lost focus.
- **Source:** Phase 1 analysis, 2026-10-23; plan review round 1 (R1-08, R1-11), 2026-10-23.
- **State:** replaced by plan P4

### P4 — Focus return not built
- **By:** team lead (away answerer, plan-departures ask)
- **Decided:** the focus return proposed in P3 is not built; `BaseInput` gains no `focus()` and Step 8 moves no focus.
- **Why:** presumed (proceed-default), not user-confirmed — the away line drops a tagged row no source requires, and the campaign forbids new product behaviour (plan review round 2, R2-03).
- **Turned down:** keep the focus return (P3).
- **Rests on:** the campaign's no-new-product-behaviour directive; an owner keep would reopen it.
- **Confidence:** high — the directive is binding.
- **Source:** plan-departures ask, 2026-10-23; plan review round 2 (R2-03).
- **State:** current

## Plan Review

Round 1 (2026-10-23, Codex primary NO-GO + Sonnet second reader ACCEPT): 12 merged findings, all folded — see `review-critic.md` § Round 1 (plan) `### Fold — round 1`. Spec FE-08 row and spec § Decisions reconciled for R1-01. Round 2 (2026-10-23, Codex, full read): R1-01..R1-12 verified closed except R1-08 partial; 4 MEDIUM findings R2-01..R2-04 folded — see `### Fold — round 2`; R1-08 closed with R2-03. No CRITICAL, so no round 3. Plan departures (owner away, away line, presumed): Decisions 10–13 kept, Decision 3 dropped.

## Open Questions

None.
