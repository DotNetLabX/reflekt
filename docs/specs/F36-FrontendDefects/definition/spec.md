# F36 — Frontend Visual & UX Defects

**Kind:** technical (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` § 1 (frontend bullet); finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/frontend.md` (FE-*)
**Dependencies:** F33 (done)
**Rule list:** not applicable — visual and UX defect fixes
**Status:** Ready
**Plan:** None

---

## Purpose

The SPA builds and type-checks clean, but several things a user sees are broken: corners never round, the light theme keeps a dark page, typed text is lost when a save fails, pages hang on "Loading", card tints never render, the timer chime is silent, and the discussed group is unreadable in dark mode. This feature fixes what users see and hit. It does not restructure the frontend — that is F39.

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below narrows it or picks between its options.

| Finding | Defect | Required outcome |
|---|---|---|
| FE-04 | `rounded-[--radius-*]` (Tailwind v3 syntax) emits invalid CSS under v4 — 50 sites, no corners anywhere | Every site uses the theme's own utilities (`rounded-sm`, `rounded-md`, `rounded-lg`, `rounded-full`, `rounded-t-md`, `rounded-b-md`), `@apply` sites included. |
| FE-11 | `max-w-[--page-max-width]` — undefined variable, invalid syntax; report page has no max width | A page width token in `@theme` and its utility, or `max-w-6xl`. |
| FE-05 | Inline dark `<body>` style beats the stylesheet, so the light theme shows a dark page on Welcome, Teams and Sprints | No inline body colour; the pre-paint background is set per theme by the existing blocking script, so there is no white flash in dark and no dark page in light. |
| FE-20 | Focused group uses a near-white background in both themes — unreadable in dark | Theme-aware surface tokens for the focused group and the other `primary-50/100` surfaces the finding lists. |
| FE-12 | `bg-col-*-light/30` built dynamically and not safelisted — card tints never render | Column colour → literal class map (Tailwind sees every class), applied to every site the finding lists. |
| FE-13 | Timer expiry chime is a silent WAV | Tone generated with WebAudio (440 Hz, ~300 ms); the mute toggle governs it; the misleading comment fixed. |
| FE-07 (part) | Note and action-item inputs cleared before the save; on failure the typed text is lost | Draft cleared only on success; on failure the text is restored and the error toast stays. (Optimistic-UI unification is F39.) |
| FE-14 | Create-sprint and delete-team forms clear state before the async call; failures are unhandled | Form state resets only on success; a failure shows an error message and keeps the dialog/values. |
| FE-08 | Failed fetches look like empty data; `SprintView` stays on "Loading sprint…" forever; `fetchMe` can reject inside the router guard | The sprint, teams, card, card-group, participant and policy stores expose `loading` and `error`; the views that bootstrap from them render an error state distinct from the empty state; `fetchMe` never rejects into the guard. |
| FE-28 | Reload on a team-scoped route shows blank team names; admin sprint view reachable signed-out | `SprintListView` loads teams when missing; the team-scoped routes are `requiresAuth`. |
| FE-31 | Any signed-in Google user sees edit/delete on other people's cards; the server then refuses | The controls show only where the server allows the action: the card's own author, or the team owner (`isScrumMaster`). |
| FE-32 | "Back to sprints" links to the welcome page | Links to the team's sprint list. |
| FE-22 (part) | `BaseInput` applies `$attrs` to both the wrapper and the input, so `@keydown.enter` fires twice | `inheritAttrs: false` on `BaseInput`, attrs on the input only. (`defineModel` and ARIA are F39.) |

## Decisions (taken — architect)

| Decision | Why | Rejected alternative | Confidence |
|---|---|---|---|
| FE-31 matches the backend's existing rule (author or team owner) — no new permission. | The server already decides; the UI was lying. | Show controls to any Google user. | high |
| Chime by WebAudio, no audio asset. | No binary in the repo; the finding's preferred option. | Ship a short audio file in `public/`. | high |
| Visual results are checked by one manual look per theme (light and dark) on the board, the teams page and the report page, recorded in `implementation.md` with what was seen. | There are no frontend tests and none are added (owner's scope rule). | Add Vitest or screenshot tests. | medium — manual check only |

## Out of scope

- Everything conventions-shaped in `findings/frontend.md` not named above (FE-01…03, FE-06, FE-07 rest, FE-09, FE-10, FE-15…19, FE-21, FE-22 rest, FE-23…27, FE-29, FE-30, FE-33…42) — F39.
- Frontend tests (FE-43) — not added (owner's scope rule); documented as not set up in F40.
- Backend changes.

## What must not change

- Routes and their names (only `requiresAuth` is added on the team-scoped ones); API calls and payloads; SignalR handling.
- Layout and colours other than the defects named (rounded corners and tints appearing *is* the intended change).
- `npm run type-check` and `npm run build` stay green.

## Acceptance (checkable)

1. `grep -rnE "\[--[a-z-]+\]" web/reflekt-web/src` → no hits.
2. `grep -n "style=" web/reflekt-web/index.html` → no `background-color` on `<body>`.
3. Built CSS (`npm run build`, then search `src/Services/Reflekt/Reflekt.API/wwwroot/assets/*.css`): contains `border-radius:var(--radius-md)` and at least one `bg-col-` class with `/30`.
4. `grep -rn "BASE64_CHIME\|data:audio" web/reflekt-web/src` → no hits (chime no longer an embedded WAV).
5. `implementation.md` records the manual light/dark check per page named in Decisions.
6. The declared full role in `.claude/verify.json` passes.
