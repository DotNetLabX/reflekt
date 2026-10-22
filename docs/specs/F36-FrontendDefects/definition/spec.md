# F36 — Frontend Visual & UX Defects

**Kind:** technical (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` § 1 (frontend bullet); finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/frontend.md` (FE-*)
**Dependencies:** F33 (done)
**Rule list:** not applicable — visual and UX defect fixes
**Review:** round 1 — `delivery/review-critic.md`
**Status:** Ready
**Plan:** None

---

## Purpose

The SPA builds and type-checks clean, but several things a user sees are broken: most corners never round, the light theme keeps a dark page, typed text is lost when a save fails, pages hang on "Loading" or show stale data when a fetch fails, card tints never render, the timer chime is silent, the discussed group is unreadable in dark mode, and edit/delete buttons appear on cards the server will refuse to change. This feature fixes what users see and hit. It does not restructure the frontend — that is F39.

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below narrows it or picks between its options.

| Finding | Defect | Required outcome |
|---|---|---|
| FE-04 | `rounded-[--radius-*]` (Tailwind v3 syntax) emits invalid CSS under v4 — 49 sites in 18 files | Every site uses the theme's own utilities (`rounded-sm`, `rounded-md`, `rounded-lg`, `rounded-full`, `rounded-t-md`, `rounded-b-md`), the `@apply` sites in `TimerControls.vue` included. |
| FE-11 | `max-w-[--page-max-width]` (`PageLayout.vue`) — undefined variable, invalid syntax; the report page has no max width | `max-w-6xl`. |
| FE-05 | Inline dark `<body>` style beats the stylesheet, so the light theme shows a dark page on Welcome, Teams and Sprints | The inline `style` on `<body>` in `index.html` is removed; a `<style>` block in `<head>` sets `html { background: <dark hex> }` and `html.light { background: <light hex> }` (the existing blocking script already sets the `light` class before first paint). A flash in the Vite dev server, where CSS is injected by JS, is accepted. |
| FE-20 | The focused (discussed) group uses a near-white background in both themes — light text on near-white in dark | A theme-aware surface token for the focused group in `RetroGroupItem.vue`. The other `primary-50/100` uses (avatar chips, the phase-indicator ring, the drag-over highlight) are checked in the walkthrough and changed only if unreadable in dark. |
| FE-12 | `bg-col-*-light/30` is built dynamically and not safelisted — card tints never render | In `RetroCardItem.vue`, a literal class map from column colour to its `/30` tint class, covering every palette colour and the action-items branch. The other sites the finding lists already render (their classes are safelisted) and are not changed. |
| FE-13 | The timer expiry chime is a silent WAV | A ~300 ms 440 Hz tone generated with WebAudio; the `AudioContext` is created or resumed on the user's first click or key press in the room and on the mute toggle, so browsers that block audio without a gesture still play it after any interaction. If the browser still blocks it, silence is accepted. The mute setting still gates it; the misleading comment goes. |
| FE-07 (part) | Note and action-item inputs are cleared before the save; on failure the typed text is lost | The add actions report success or failure to the panel; the input is cleared only on success and keeps its text on failure (the error toast stays); while a save is in flight the input and its Add button are disabled, so a second Enter cannot create a duplicate. (Making creates optimistic is F39.) |
| FE-14 | Create-sprint and delete-team: failures are unhandled; create clears the form before the call; delete resets its busy state too early | Each form calls the store itself (as `CreateTeamForm` already does) inside try/catch: values reset only on success, busy stays on until the call ends, and a failure shows an error message inside the form or dialog. |
| FE-08 | Failed fetches look like empty data or leave old data on screen; `SprintView` stays on "Loading sprint…" forever; `fetchMe` can reject inside the router guard | Every fetch in the `sprint`, `teams`, `card`, `cardGroup`, `participant`, `policy`, `vote`, `previousActions` and `report` stores clears its target data and its `error` when it starts, sets `loading`, and on failure (HTTP or network) sets `error` instead of returning silently. The views that load from them — teams dashboard, sprint list, sprint view, retro room, report — show a distinct error state with a retry; the retro room shows the error instead of an empty board. `fetchMe` never rejects: on a network failure it leaves the user signed-out for that load and shows a "can't reach the server" message. |
| FE-28 | Reload on a team-scoped route shows blank team names; the admin sprint view is reachable signed-out | The header's team name loads on cold entry to `team-sprints`, `sprint` and `report` when the user is signed in and the team list is empty. Only the `sprint` route (the admin sprint view) gains `requiresAuth`; `report` stays open to guests, who reach it with their participant token today. |
| FE-31 | Any signed-in Google user sees edit/delete on other people's cards; the server then refuses | The controls show only where the server would allow the action today: in the retro room every caller holds a participant token, and the server then allows edit/delete on the caller's **own** cards only, within the phases the policy matrix allows, not on a closed sprint, and — for an action item in the Discussing phase — only when the caller is signed in with Google. The UI mirrors exactly that. No permission is added or removed. |
| FE-32 | "Back to sprints" links to the welcome page | Links to `{ name: 'team-sprints', params: { teamId } }`. |
| FE-22 (part) | `BaseInput` applies `$attrs` to both the wrapper and the input, so `@keydown.enter` fires twice | `inheritAttrs: false`; `class` (and `style`) still go on the wrapper so the layout stays as it is (`class="flex-1"` sizes the wrapper today), every other attribute and listener goes on the input only. (`defineModel` and ARIA are F39.) |

## Decisions (taken — architect)

| Decision | Why | Rejected alternative | Confidence |
|---|---|---|---|
| FE-31 mirrors the server's current rule (own cards only, plus its phase, closed-sprint and action-item checks) — no owner moderation. | Both readers showed the owner branch is unreachable from the retro room, because every room request carries a participant token. Adding owner moderation would be a new permission. | Show controls for the team owner too (`isScrumMaster`). | high — read from the endpoints |
| `report` keeps guest access; only `sprint` requires sign-in. | Guests open their report with their token today (`ReportView`, `GetSprintReportEndpoint`). | `requiresAuth` on every team-scoped route. | high |
| The `vue.md` and `tailwind-theme` wording that prescribes the inline body colour is corrected in F40 (`vue.md`) and recorded as skill drift (the skill is not edited — owner decision 2026-10-12). | Docs are F40's; skills stay as built. | Edit the docs here. | high |
| Chime by WebAudio, unlocked on the first interaction; silence accepted where the browser still blocks it. | No binary asset; browsers block audio without a gesture. | Ship an audio file. | high |
| No frontend tests are added (owner's scope rule); the outcomes are proven by greps and one recorded manual walkthrough (Acceptance). | — | Vitest or screenshot tests. | medium — manual |

## Out of scope

- Everything conventions-shaped in `findings/frontend.md` not named above (FE-01…03, FE-06, FE-07 rest, FE-09, FE-10, FE-15…19, FE-21, FE-22 rest, FE-23…27, FE-29, FE-30, FE-33…42) — F39, which must keep this feature's outcomes (F39 § What must not change).
- Frontend tests (FE-43) — documented as not set up in F40.
- Backend changes; doc changes.

## What must not change

- Routes and their names (only `requiresAuth` on `sprint`); API calls, payloads and headers; SignalR handling.
- Who can open what: guests keep the report; signed-in users keep everything they have.
- Layout and colours other than the defects named (corners rounding and tints appearing *is* the intended change).
- `npm --prefix web/reflekt-web run type-check` and `npm --prefix web/reflekt-web run build` stay green.

## Acceptance (checkable)

Greps run from the repository root.

1. `grep -rnE "\[--[a-z-]+\]" web/reflekt-web/src` → no hits.
2. `grep -n "<body" web/reflekt-web/index.html` → no `style=`; `index.html`'s `<head>` has a `<style>` block with `html` and `html.light` backgrounds.
3. After `npm --prefix web/reflekt-web run build`, the CSS under `src/Services/Reflekt/Reflekt.API/wwwroot/assets/` contains `border-radius:var(--radius-md)` and a `/30` tint class for every palette colour.
4. `grep -rn "BASE64_CHIME\|data:audio" web/reflekt-web/src` → no hits; `grep -n "AudioContext" web/reflekt-web/src/composables/useTimer.ts` → present.
5. `grep -n "inheritAttrs: false" web/reflekt-web/src/components/ui/BaseInput.vue` → present; `grep -n "team-sprints" web/reflekt-web/src/components/sprints/SprintDetailPanel.vue` → present; in `web/reflekt-web/src/router/index.ts` the `sprint` route has `requiresAuth` and the `report` route does not.
6. One manual walkthrough recorded in `implementation.md`, each item with what was seen:
   - **Light and dark, cold load:** Welcome, Teams, Sprints list, sprint view, report — page background correct, corners rounded.
   - **Board states:** a focused group in Discussing (dark), a drag-over highlight, cards in all palette colours plus action items, the mobile tab bar at a narrow width.
   - **Failures**, each done twice — once offline (devtools) and once with the server answering an error (devtools request blocking or overriding the response with a 500): add a note and an action item → text stays, one toast, no duplicate on a second Enter; create sprint and delete team → values kept, error shown; open the sprint view, the teams dashboard and the retro room → error state, never another sprint's data. Then, back online: retry succeeds in each case, and one Enter adds exactly one note and clears the input.
   - **Roles:** as a guest and as a Google participant, edit/delete appear only on the caller's own cards and disappear where the phase or the closed sprint forbids them; a guest opens the report URL with their token.
   - **Deep links:** reload on `team-sprints`, `sprint` and `report` → team name shown in the header.
   - **Chime:** after clicking once in the room, an unmuted expiry is audible; a muted one is silent.
7. The declared full role in `.claude/verify.json` passes.
