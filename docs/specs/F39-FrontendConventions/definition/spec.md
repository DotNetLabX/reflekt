# F39 — Frontend Conventions Conformance

**Kind:** technical refactoring (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` § 4; finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/frontend.md` (FE-*)
**Dependencies:** F36 (defects fixed first), F38 (the error body this feature's API client reads)
**Rule list:** not applicable — restructuring with no intended behaviour change
**Status:** Ready
**Plan:** None

---

## Purpose

The frontend mixes two generations of code, and neither matches what the repo's skills teach: options-style stores instead of setup stores, 44 raw `fetch` calls with the guest-token header pasted 24 times, optimistic updates done in three different layers, components far over the size caps, stringly-typed phases, two sets of UI controls, and almost no accessibility markup. This feature brings the SPA onto the repo's own frontend conventions (`CLAUDE.md` § Frontend, `docs/conventions/vue.md`, the `pinia-patterns`, `create-vue-feature`, `vue-component-architecture`, `vue-patterns`, `tailwind-theme` and `frontend-review` skills), so each job is done one way. What the user sees stays the same, except where Decisions says otherwise.

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below picks between its options or narrows it.

| Finding(s) | Theme | Required outcome |
|---|---|---|
| FE-02, FE-18, FE-17 (API part), FE-03 | One API layer | `api/client.ts` with `apiFetch<T>` (guest token, credentials, JSON, typed `ApiError extends Error` carrying `status` and the F38 error body's `code`), plus one module per area; stores call only these; no `fetch(` outside `api/`. Each call keeps today's credentials and headers — the calls that omit `credentials` today are checked one by one and the outcome recorded. Error messages come from one `describeApiError()` keyed on `code`. |
| FE-01 | Setup stores | All 13 stores become setup stores (`defineStore(id, () => …)`). |
| FE-06, FE-07 (rest), FE-03 | State mutation and optimistic UI | Only store actions mutate store state; every SignalR event has a store action and the hub handlers are one-liners; user-initiated mutations follow the `vote.ts` shape (optimistic change, reconcile or revert) in the store. Server-authoritative actions (phase advance, close sprint) are exempt; the plan lists them. |
| FE-15, FE-16, FE-17 (rest) | Types | `types/` folder with domain unions (`SprintPhase`, `SprintState`, `ColumnType`, `ReviewStatus`, `ColumnColor`) and a `PHASES` metadata constant; DTO types imported from there; no `as unknown as`, no store↔composable type cycle; non-null `!` replaced by guards. |
| FE-09, FE-10, FE-23, FE-24, FE-40 | Component size and shape | `RetroBoardPanel` < 200 lines (columns rendered by `v-for`), `RetroRoomView` < 100 lines (advance/close dialogs and tabs extracted), `RetroPhaseHeader` reads state from composables instead of 19 props, one report component used by both report screens, `RetroColumnBody` merged. |
| FE-19, FE-21, FE-22 (rest), FE-34, FE-36 | Design system | Tokens instead of raw palette colours (no `isDark` colour branching); `BaseButton`/`BaseInput`/`BaseModal`/`BaseBadge`/`BaseCard` used in teams, sprints, welcome and report screens too; `defineModel` in `BaseInput` and the tab bar; no `@apply` in components; lucide icons instead of pasted SVG. |
| FE-29, FE-30 | Accessibility | Every icon-only control has an accessible name; toasts in a live region; `aria-current` on the phase indicator; tab semantics on the mobile tab bar; clickable `div`/`li`/`h2` become buttons or links; `BaseModal` closes on Escape and manages focus; labels tied to inputs; the native `confirm()` replaced by `BaseModal`. |
| FE-25, FE-26, FE-27, FE-33, FE-35, FE-39, FE-41, FE-42 | Dead code and duplication | Unused components, barrel and store members deleted or adopted (`EmptyState` adopted in the page shells); dummy voting props removed; one `mm:ss` formatter and one timer-update action; report markdown in a pure util; shared format/constants modules; storage access through one guarded helper with key constants. |
| FE-37, FE-38 | Tooling and loading | ESLint (`eslint-plugin-vue`, TypeScript config) and Prettier configured and run once over `src`; a `lint` script; the `@/` alias used; lazy-loaded routes; `fetchMe` idempotent. |

## Decisions (taken — architect)

| Decision | Why | Rejected alternative | Confidence |
|---|---|---|---|
| The report shown inside the retro room gains the previous-actions section, because both report screens now use one component. | Unifying the two copies is the finding; the section is already on the standalone report. | Keep two report components. | medium — small visible change |
| ESLint + Prettier are added and `npm --prefix web/reflekt-web run lint` joins the declared `full` role in `.claude/verify.json`. | A convention a machine can check should be checked (owner's standing preference). | Format once, enforce nothing. | medium — new dev dependencies |
| No frontend tests are added (FE-43); F40 documents frontend testing as not set up. | Owner's scope rule. | Add Vitest. | high |
| Project skills under `.claude/skills/` are not edited (owner decision 2026-10-12); naming the skill paths cite (`client/src/…`, `{name}Store.ts`) is not copied — the existing `src/stores/{name}.ts` layout stays. | Owner kept the agent setup as evidence; patterns, not paths, are the convention. | Rename every store file. | high |

## Out of scope

- Backend changes; visual defects (F36); docs (F40).
- Behaviour changes other than the Decisions above.

## What must not change

- Routes, API calls (paths, methods, payloads, credentials and headers per call), SignalR method and event names.
- What each user role can see and do; visual appearance except the Decisions above and the design-system swaps (same tokens, same layout).
- `npm run type-check` and `npm run build` stay green.

## Acceptance (checkable)

1. `grep -rn "fetch(" web/reflekt-web/src --include=*.ts --include=*.vue | grep -v "src/api/"` → no hits.
2. `grep -rn "defineStore(" web/reflekt-web/src/stores | grep -v "() =>"` → no hits.
3. `grep -rn "X-Guest-Token" web/reflekt-web/src` → only in `src/api/`.
4. `wc -l` on `RetroBoardPanel.vue` < 200 and `RetroRoomView.vue` < 100.
5. `grep -rn "as unknown as" web/reflekt-web/src` → no hits; `grep -rnE "(bg|text|border|ring)-(red|green|blue|emerald|amber)-[0-9]" web/reflekt-web/src` → no hits.
6. `npm --prefix web/reflekt-web run lint` → 0 errors.
7. `implementation.md` records one manual walkthrough (join, write, group, vote, discuss, report; light and dark) and the per-call credentials check.
8. The declared full role in `.claude/verify.json` passes.
