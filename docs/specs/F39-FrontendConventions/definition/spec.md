# F39 — Frontend Conventions Conformance

**Kind:** technical refactoring (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` § 4; finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/frontend.md` (FE-*)
**Dependencies:** F36 (defects fixed first — its outcomes must survive), F38 (the error body this feature's API client reads)
**Rule list:** not applicable — restructuring; the visible changes are listed in Decisions
**Review:** round 1 — `delivery/review-critic.md`
**Status:** Ready
**Plan:** None

---

## Purpose

The frontend mixes two generations of code, and neither matches what the repo's skills teach: options-style stores instead of setup stores, 42 raw `fetch` calls with the guest-token header pasted 24 times, optimistic updates done in three different layers, components far over the size caps, stringly-typed phases, two sets of UI controls, and almost no accessibility markup. This feature brings the SPA onto the repo's own frontend conventions (`CLAUDE.md` § Frontend, `docs/conventions/vue.md`, and the `pinia-patterns`, `create-vue-feature`, `vue-component-architecture`, `vue-patterns`, `tailwind-theme` and `frontend-review` skills), so each job is done one way. What changes on screen is listed in Decisions; nothing else does.

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below picks between its options or narrows it.

| Finding(s) | Theme | Required outcome |
|---|---|---|
| FE-02, FE-18, FE-17 (API part), FE-03 | One API layer | `api/client.ts` with `apiFetch<T>` and one module per area; stores call only these; no network `fetch(` outside `api/`. **Per call, today's request is kept:** credentials are always `include` (the app is same-origin, so this changes nothing for the calls that omit it today); the guest token is opt-in per call — each API function declares whether it sends it, and exactly the 24 calls that send `X-Guest-Token` today keep doing so, no other; the token is read through a getter handed to the client at start-up, so `api/` never imports a store. `apiFetch` handles 204 and empty bodies as success and non-JSON bodies without crashing. Errors are one `ApiError extends Error` carrying `status`, `code`, `message` and `errors` from F38's body. `describeApiError()` picks the text in this order: a known `code`, then the operation's own status message (the join flow's 409 "Name already taken" and 404 "Sprint not found" move here), then the server's `message`, then a fixed fallback. |
| FE-01 | Setup stores | All 13 stores become setup stores (`defineStore(id, () => …)`); the policy store's `fetch` action is renamed `load`. |
| FE-06, FE-07 (rest) | State mutation and optimistic UI | Only store actions mutate store state; every SignalR event has a store action and the hub handlers are one-liners. **Optimistic** (vote.ts shape — change, then reconcile or revert), in the store: card create, edit and delete; group title rename; discussed toggle; focus and clear focus; readiness toggle; votes (already); previous-action review status. A create shows a temporary card at once, is replaced by the server's card on response, and the `CardCreated` echo of a card already present updates it in place, so no duplicate appears. A revert undoes only that item's own change, never a whole snapshot, so updates that arrived in between survive. F36's draft rule holds: on a failed create the temporary card disappears and the typed text is back in the input. **Server-authoritative** (not optimistic, unchanged): start, advance and close sprint; create sprint; update columns; create and delete team; create, merge, move and dissolve groups by drag; kick. |
| FE-15, FE-16, FE-17 (rest) | Types | A `types/` folder with domain unions (`SprintPhase`, `SprintState`, `ColumnType`, `ReviewStatus`, `ColumnColor`) and a `PHASES` metadata constant, used by the stores, props and phase logic; DTO types imported from there; no `as unknown as`, no store↔composable type cycle; non-null `!` replaced by guards. |
| FE-09, FE-10, FE-23, FE-24, FE-40 | Component size and shape | `RetroBoardPanel` < 200 lines (columns rendered by `v-for`), `RetroRoomView` < 100 lines (advance/close dialogs and tabs extracted), `RetroPhaseHeader` reads state from composables instead of its 20 props, `RetroColumnBody` merged into `RetroColumnPanel`. **One report component** for both report screens that keeps everything either shows today: the in-room report keeps its discussed badges and the text of single-card groups, and gains the previous-actions section the standalone report has. |
| FE-19, FE-21, FE-22 (rest), FE-34, FE-36 | Design system | Tokens instead of raw palette colours (no `isDark` colour branching), with the `success` button variant and its tokens added; `BaseButton`/`BaseInput`/`BaseModal`/`BaseBadge`/`BaseCard` used in teams, sprints, welcome and report screens too; `defineModel` in `BaseInput` (keeping F36's `inheritAttrs` split) and the tab bar; `aria-invalid`/`aria-describedby` on `BaseInput`; no `@apply` in components; lucide icons instead of pasted SVG and unicode arrows. |
| FE-29, FE-30 | Accessibility | Every icon-only control has an accessible name; `aria-expanded`/`aria-haspopup` on menus; toasts in a live region; `aria-current` on the phase indicator; the mobile tab bar has tab roles, arrow-key movement and panels it controls; clickable `div`/`li`/`h2` become buttons or links; tooltips open on keyboard focus too; `BaseModal` takes focus on open, keeps it inside, restores it on close and closes on Escape; labels tied to inputs; the native `confirm()` for kick replaced by `BaseModal`. |
| FE-25, FE-26, FE-27, FE-33, FE-35, FE-39, FE-41, FE-42 | Dead code and duplication | `EmptyState` adopted in the page shells; `PageToolbar` and the unused `ui/index.ts` barrel deleted; unused store members deleted; the dummy voting props removed; one `mm:ss` formatter and one timer-update action; report markdown in a pure util; shared format/constants modules; the create-team heading becomes a prop ("Create your first team" only for the first); storage access through one guarded helper with key constants — the existing keys (guest token, theme, mute) keep their names. |
| FE-37, FE-38 | Tooling and loading | ESLint (`eslint-plugin-vue`, TypeScript config) and Prettier (no semicolons — the majority style today); the one-time reformat of `src` is its own first step, before any logic change; a `lint` script runs ESLint and `prettier --check`; the `@/` alias used; lazy-loaded routes; `fetchMe` shares one in-flight request. |

## Decisions (taken — architect)

| Decision | Why | Rejected alternative | Confidence |
|---|---|---|---|
| **Visible changes, the whole list:** buttons on the teams, sprints, welcome and report screens take `BaseButton`'s padding, radius and weight; the `success` variant and its tokens are added; empty states use `EmptyState`; pasted icons and unicode arrows become lucide icons; the create-team heading reads correctly for a second team; the in-room report gains the previous-actions section; the kick confirmation becomes a `BaseModal` dialog instead of the browser's native one; optimistic actions update the screen before the server answers. | Each comes from a finding; listing them keeps "nothing else changes" honest. | Promise no visible change. | medium |
| Group drag, merge and dissolve stay server-authoritative. | The server computes the result (solo groups, cleanup); optimistic versions would duplicate that logic in the browser. | Make them optimistic too. | high |
| `npm --prefix web/reflekt-web run lint` joins the declared `full` role in `.claude/verify.json`, and its `measured.full` is re-timed. | A convention a machine can check should be checked (owner's standing preference). | Format once, enforce nothing. | medium — new dev dependencies |
| No frontend tests are added (FE-43); outcomes are proven by greps and one recorded walkthrough; F40 documents frontend testing as not set up. | Owner's scope rule. | Add Vitest. | high |
| Project skills under `.claude/skills/` are not edited (owner decision 2026-10-12); the `src/stores/{name}.ts` layout stays even where a skill cites other paths. | Patterns, not paths, are the convention. | Rename every store file. | high |

## Out of scope

- Backend changes; visual defects (F36); docs (F40).

## What must not change

- Routes and their guards; API paths, methods, payloads, and per-call credentials and headers; SignalR method and event names.
- What each role (team owner, guest, Google participant) can see and do; persisted sessions — the guest-token, theme and mute storage keys.
- **F36's outcomes:** rounded corners, the `<head>` background style, card tints, the focused-group surface, the WebAudio chime and its unlock, draft text kept on failure, loading/error states in every store and view it named, the team name on cold deep links, `requiresAuth` only on `sprint`, edit/delete only on the caller's own cards within the server's rules, the back link, `BaseInput`'s attribute split.
- Visual appearance, except the Decisions list.
- `npm --prefix web/reflekt-web run type-check` and `run build` stay green.

## Acceptance (checkable)

Greps run from the repository root.

1. `grep -rnE "(^|[^.A-Za-z])fetch\(" web/reflekt-web/src | grep -v "src/api/"` → no hits.
2. `grep -rn "defineStore(" web/reflekt-web/src/stores | grep -v "() =>"` → no hits.
3. `grep -rn "X-Guest-Token" web/reflekt-web/src` → only in `src/api/`; `grep -rn "useGuestStore\|from '../stores\|from '@/stores" web/reflekt-web/src/api` → no hits.
4. `wc -l` on `RetroBoardPanel.vue` < 200 and on `RetroRoomView.vue` < 100; `RetroColumnBody.vue` no longer exists.
5. `grep -rn "as unknown as" web/reflekt-web/src` → no hits; `grep -rnE "(bg|text|border|ring)-(red|green|blue|emerald|amber)-[0-9]" web/reflekt-web/src` → no hits; `grep -rn "@apply\|<svg\|confirm(" web/reflekt-web/src --include=*.vue` → no hits.
6. Store state is written only inside store files: `grep -rnE "Store\.[A-Za-z.]+(\.push\(|\.unshift\(|\.splice\(| = [^=])" web/reflekt-web/src/composables web/reflekt-web/src/components web/reflekt-web/src/views` → no hits; every route in `router/index.ts` uses `() => import(`.
7. F36's acceptance greps 1–5 still pass.
8. `npm --prefix web/reflekt-web run lint` → 0 errors; `npm --prefix web/reflekt-web audit --omit=dev` → 0 vulnerabilities (F37's baseline holds after the new dev dependencies).
9. One walkthrough recorded in `implementation.md`, light and dark, each item with what was seen:
   - **Every route and flow:** sign-in, teams dashboard, create and delete team (incl. duplicate name and team limit errors), sprint list, create sprint, admin sprint view, kick (in `BaseModal`), join as guest and as Google participant, write, group, vote, discuss, report (in-room and standalone), reload with an existing guest token, theme and mute setting.
   - **Optimistic behaviour,** with a slowed network and with the API stopped: a created card appears at once and is not duplicated by its echo; a failed create removes it and restores the typed text; with two browsers, an update from the other browser that arrives during a failed edit survives the revert.
   - **Requests:** every API function's request compared before and after in devtools — path, method, payload, credentials and whether `X-Guest-Token` is sent — listed in a table in `implementation.md`.
   - **Structure:** each SignalR handler in `useSprintHub.ts` is a single store-action call; the domain unions and `PHASES` are imported by the stores and the phase logic; both report screens render the one report component — each confirmed by a grep recorded in `implementation.md`.
   - **Keyboard:** modal focus moves in, stays in, returns on close, closes on Escape; mobile tabs move with arrow keys; tooltips open on focus; a screen reader announces a toast.
   - **Auth requests:** a cold load sends one `/auth/me` request.
10. The declared full role in `.claude/verify.json` passes.
