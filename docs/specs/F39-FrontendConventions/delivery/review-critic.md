## Round 1 — 2026-10-22 — spec — primary (codex)

Header note: two readers — Codex primary (read-only job, verdict persisted verbatim in `review-critic-codex.md`) and a Sonnet nexus critic as second reader; pair `codex + sonnet`, picked by the owner. The spec had been committed as Ready without review; the owner caught it and it was set back to Draft before this round.

Primary reader: see `review-critic-codex.md` — `## Verdict: NO-GO`, 2 HIGH, 6 MEDIUM, 1 LOW.

## Second reader — round 1 (sonnet)

Verdict: REVISE. (Condensed by the architect, not verbatim: every finding is kept with its level, evidence and fix; the reader's full message is in the session transcript.)

- **[HIGH, band HIGH–MEDIUM] F39-01 — Optimistic-UI scope open; contradicts F36 and "what the user sees stays the same".** No list of which actions become optimistic; no reconcile rule for the HTTP response vs the `CardCreated` echo (duplicates); F36's draft restore vs an immediate clear; group drag going instant is undisclosed.
- **[MEDIUM, band MEDIUM–HIGH] F39-02 — `apiFetch` "injects the guest token" vs "each call keeps today's headers".** 18 of 42 calls send no token today; a stale token on an owner's admin call could hit F38's wrong-sprint check; credentials are same-origin, so `include` everywhere is safe.
- **[MEDIUM] F39-03 — `ApiError`/`describeApiError` underspecified** (only two codes; status-keyed join messages; validation `errors`; fallback order).
- **[MEDIUM] F39-04 — Undisclosed visible changes** (BaseButton sizes, new tokens, heading, `EmptyState`, icons).
- **[MEDIUM] F39-05 — F36's outcomes not in "must not change"; no re-check.**
- **[MEDIUM, band MEDIUM–HIGH] F39-06 — Several rows unchecked; walkthrough covers only the retro room.** Proposed greps for `@apply`, `<svg`, `confirm(`, lazy routes, `RetroColumnBody`, store mutation outside stores.
- **[LOW, band LOW–MEDIUM] F39-07 — Acceptance 1 cannot go green (`policyStore.fetch`); 42 calls, not 44; 20 props, not 19.**
- **[LOW] F39-08 — Lint/format enforcement and the reformat's sequencing unspecified.** Route: plan must resolve.
- Plan must resolve: token source without an `api/` → `stores/` cycle; empty/non-JSON bodies; setup-store conversion details; FE-25 per item; modal focus depth; FE-30 items; `verify.json` re-measure.

Coverage ledger (second reader): setup stores, types, component size, dead code, F36 allocation, siblings COVERED; API layer, optimistic UI, design system, accessibility, tooling, acceptance, F38 body PARTIAL; four frontend skills NOT OPENED (audit citations relied on).

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | sonnet, codex | HIGH | seed-0007 | Optimistic scope open; echo reconcile; F36 draft rule; revert overwrites broadcasts | open scope decision | builds differ; F36 fix undone; lost updates | quoted, path | — | scope | Scope row 3 | F39-01, codex-2 |
| R1-02 | codex | HIGH | unranked — Codex severity HIGH | Report unification drops in-room content (discussed badges, single-card text) | undisclosed content change | visible information removed | path | — | scope | Scope row 5 | codex-1 |
| R1-03 | sonnet, codex | MEDIUM | seed-0022 | Guest-token injection vs per-call headers; credentials check | internal contradiction | owner refused on admin pages | quoted, scenario | — | scope | Scope row 1 | F39-02, codex-5 |
| R1-04 | sonnet, codex | MEDIUM | seed-0022 | `ApiError`/`describeApiError` precedence and fields | cross-spec contract gap | error text regresses | quoted | — | scope | Scope row 1 | F39-03, codex-3 |
| R1-05 | codex | MEDIUM | unranked — Codex severity MEDIUM | 204 and empty bodies unhandled | contract gap | successful mutation reported failed, rolled back | path | — | scope | Scope row 1 | codex-4 |
| R1-06 | sonnet | MEDIUM | seed-0021 | Visible changes undisclosed | undisclosed scope change | owner approves "no change" that is false | quoted | — | scope | Purpose, What must not change | F39-04 |
| R1-07 | sonnet | MEDIUM | seed-0129 | F36 outcomes not protected | sibling coordination | F36 fixes undone silently | path | — | sibling | What must not change | F39-05 |
| R1-08 | sonnet, codex | MEDIUM | seed-0310 | Walkthrough narrow; rows unchecked; roles and stored sessions | acceptance does not prove outcome | regressions ship | quoted | — | acceptance | AC | F39-06, codex-5, codex-8 |
| R1-09 | codex | MEDIUM | unranked — Codex severity MEDIUM | Accessibility acceptance unproven (keyboard) | acceptance does not prove outcome | keyboard access still absent | path | — | acceptance | Scope a11y row, AC | codex-6 |
| R1-10 | sonnet, codex | MEDIUM | seed-0310 | AC1 fetch grep cannot go green | acceptance mismatch | check never passes | path | — | acceptance | AC1 | F39-07, codex-7 |
| R1-11 | sonnet, codex | LOW | seed-0127 | Counts: 42 calls, 20 props | count off | — | path | — | count | Purpose, Scope | F39-07, codex-9 |
| R1-12 | sonnet | LOW | seed-0310 | Lint/format sequencing | mechanics | reformat mixed into logic | quoted | — | plan | Scope tooling row | F39-08 |
| R1-13 | — (architect, from F37 review) | LOW | seed-0129 | No audit check after new dev dependencies | cross-spec hand-off | new advisories unseen | path | — | sibling | AC | F37-07 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-22)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | Scope row 3 (optimistic list, echo rule, per-item revert, F36 draft rule, server-authoritative list); Decisions row 2 |
| R1-02 | fixed-real | — | — | Scope row 5 (keeps both reports' content); Decisions row 1 |
| R1-03 | fixed-real | — | — | Scope row 1 (credentials always include; token opt-in on today's 24 calls; getter, no store import); AC3 |
| R1-04 | fixed-real | — | — | Scope row 1 (`ApiError` fields; precedence) |
| R1-05 | fixed-real | — | — | Scope row 1 (204 / empty / non-JSON) |
| R1-06 | fixed-real | — | — | Decisions row 1; Purpose; What must not change |
| R1-07 | fixed-real | — | — | What must not change (F36 list); AC7 |
| R1-08 | fixed-real | — | — | AC5, AC6, AC9 |
| R1-09 | fixed-real | — | — | Scope a11y row; AC9 keyboard |
| R1-10 | fixed-real | — | — | Scope FE-01 row (`fetch` → `load`); AC1 |
| R1-11 | fixed-real | — | — | Purpose; Scope row 5 |
| R1-12 | fixed-real | — | — | Scope tooling row (reformat first; lint runs prettier --check) |
| R1-13 | fixed-real | — | — | AC8 |

Reconciled: Scope row 1 ↔ F38 Scope row 1 (body fields) ↔ AC3; Scope row 3 ↔ F36 Scope FE-07 ↔ What must not change; Decisions row 1 ↔ Purpose ↔ What must not change; AC7 ↔ F36 Acceptance 1–5; AC8 ↔ F37 Purpose.

## Round 2 — 2026-10-23 — spec — primary (codex)

Header note: Codex alone. Read: delta. Floor: MEDIUM. Verdict and fold-verification table persisted verbatim in `review-critic-codex.md` § Round 2: eleven closed; R1-06 and R1-08 partial; one new finding.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — Codex severity MEDIUM | Visible-change list omits the kick dialog | internal contradiction | required change breaks the appearance guarantee | quoted | — | fold | Decisions row 1 | R2-01, R1-06 |
| R2-02 | codex | MEDIUM | unranked — Codex severity MEDIUM | Request check samples and omits payloads; hub handlers, type use and report adoption unverified | acceptance does not prove outcome | regressions unseen | quoted | — | fold | AC9 | R1-08 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | Decisions row 1 |
| R2-02 | fixed-real | — | — | AC9 (every API function incl. payload; structure greps recorded) |

Reconciled: Decisions row 1 ↔ What must not change ↔ Scope a11y row. No round 3: no CRITICAL.


## Round 1 — 2026-10-23 — plan — primary (codex)

Header note: Mode 2 plan review of `delivery/plan.md`. Primary Codex (job task-mvkn4ge3-8xt9q4, NO-GO) — its verdict is persisted verbatim in `delivery/review-critic-codex.md` § "Plan review — round 1", cited here as the primary's reader section. Second reader nexus:critic on sonnet (ACCEPT), below. Merge by team-lead; the fold is the architect's.

## Second reader — round 1 (sonnet)

Verdict: ACCEPT — 0 CRITICAL, 0 HIGH, 4 MEDIUM (C1–C4), 7 LOW (L1–L7).

- **[MEDIUM] C1 — Lint-to-zero contradicts the pinned hub `connect`, and 9 `!` have no owning step.** Anchor seed-0130. Must-NOT-Change pins "no logic change" to `connect`/`disconnect` in `useSprintHub.ts`; Step 1 sets `no-non-null-assertion: error`; Step 15 forbids disables. `connect()` holds `connection!.start()` and `connection!.invoke('JoinSprint', …)` (useSprintHub.ts:215-216). 18 `!` errors: RetroGroupItem 7, CreateSprintForm 6, RetroColumnPanel 1, useRetroBoardActions 2, useSprintHub 2; Step 9 owns only RetroGroupItem/mergeGroups; 9 have no owning step. Fix: Step 7 allows a local constant in `connect` (stated not a logic change); name steps removing the CreateSprintForm and RetroColumnPanel assertions.
- **[MEDIUM] C2 — Optimistic focus changes two groups, but "revert only that group's field" restores one.** Anchor seed-0130. Focusing B clears A locally; a rejected focus restoring only B leaves A cleared. Fix: a focus revert also restores the previously focused group, each flag only while it still holds the optimistic value; add to the walkthrough's "Optimistic behaviour".
- **[MEDIUM] C3 — One `formatDate` for three different date outputs, with no output rule.** Anchor seed-0130; band MEDIUM–HIGH (driver: whether the builder collapses the three outputs). Report panels `toLocaleDateString()`, SprintDetailPanel `toLocaleString()` (date+time), TeamCard `toLocaleDateString(undefined, {year:'numeric',month:'short',day:'numeric'})`. Steps 11/13 say formatDate once. Fix: name the formatters with byte-identical output per call site, or list the unification in Decisions row 1.
- **[MEDIUM] C4 — Decision 7 breaks spec Scope row 3 and AC9's structure check, and Step 15 has no carve-out.** Anchor seed-0130. Spec: every SignalR event has a store action and handlers are one store-action call; Step 7 keeps `ParticipantLeft` an empty listener; P5 is architect-only with no amendment. Fix: amend Scope row 3 and AC9 with the named exception and carve it out of Step 15's grep, or record owner acceptance.
- **[LOW] L1 — Step 7 relies on actions "built optimistic in Step 8", across the 7|8 slice cut.** Fix: Step 7 wires plain store actions; Step 8 makes them optimistic.
- **[LOW] L2 — Context says `vue.md` "does not exist", but it does** (tracked, 29 lines). Fix: reword.
- **[LOW] L3 — Decision 9 adds a visible rule (a pending card shows no edit/delete) with no `Adds:` tag.** Band LOW–MEDIUM. Fix: tag `Adds:` with recommendation "keep".
- **[LOW] L4 — An offline Google join would read "(0)" instead of "(network error)"** (`useRetroRoom.ts:149`, `0 ?? 'network error'` gives 0). Fix: Step 6 maps status 0 to "network error", text built beside `describeApiError`.
- **[LOW] L5 — FE suggested-fix remnants neither adopted nor narrowed:** FE-42 `classList.toggle('light', …)` (useTheme.ts:17 wipes className); FE-22 redundant `v-bind="$attrs"` in BaseButton.vue:5; FE-27 store `formattedTime` getter unused. Fix: add to Steps 5 and 13 or narrow.
- **[LOW] L6 — `vue-patterns` never mapped** though Context binds the SPA to it; Steps 7, 10, 11, 13 create composables / use `defineModel`. Fix: map it.
- **[LOW] L7 — Verification mechanics a builder must invent:** the markdown-identity harness for Step 11 is unnamed (no new tests, so a throwaway script); no per-step "format touched files" before Step 15's `prettier --check`; the AC6 grep is blind to digit names and indexed writes. Fix: name the harness, add formatting to the per-step routine, note the grep limits.
- Coverage ledger: full read of plan, spec, questions, directives, F34 findings, stores, API sites, hub, Base* components, tooling. NOT OPENED (grep and plan claims only): RetroRoomView, RetroPhaseHeader, TimerDisplay, TimerControls, AppHeader, TeamCard, SprintList, SprintDetailPanel, CreateSprintForm, TeamDashboardView, WelcomeView, SprintView, RetroGroupItem, RetroCardItem, RetroColumnPanel/Body, RetroMobileTabBar, ToastContainer, PhaseIndicator, useTimer.ts.
- Gap analysis: token map, credentials, Content-Type rule, pre-F38 reader texts, App.vue fetchMe removal, `.js` ESLint config and Prettier on main.css verified sound. F38 still lists its CreateTeamForm edit (AR-2 governs). F36's deferred hub-lifecycle note will conflict textually with F39's rewiring; the pin is the right guard. The in-room report header uses action-items tokens where the standalone page uses col-blue — Step 11 must pick the standalone one per Q2.
- Open questions: is `vue/attributes-order` (warnings) acceptable under AC8 "0 errors" (assumed yes)? Is the 7|8 slice built and checked alone (decides L1)?

### Merge — round 1

Coverage: Codex returned no coverage ledger, so the second reader's NOT OPENED components count as unopened: RetroRoomView, RetroPhaseHeader, TimerDisplay, TimerControls, AppHeader, TeamCard, SprintList, SprintDetailPanel, CreateSprintForm, TeamDashboardView, WelcomeView, SprintView, RetroGroupItem, RetroCardItem, RetroColumnPanel/Body, RetroMobileTabBar, ToastContainer, PhaseIndicator, useTimer.ts.

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Vote rollback can erase another participant's update | revert-clobbers-authoritative | a failed local vote subtracted from an absolute hub `newCount` drops a peer's vote | scenario | spec Scope row 3 (revert only own change) | optimistic UI | plan.md:151 | codex-1 |
| R1-02 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Failed deletion can resurrect a server-deleted card | revert-clobbers-authoritative | re-insert-if-absent restores a card another browser deleted; same for link removal | scenario | spec Scope row 3 | optimistic UI | plan.md:148 | codex-2 |
| R1-03 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | Hub handlers contradict spec Scope row 3 / AC9 (ParticipantLeft empty, ParticipantKicked two calls) | plan-departs-from-spec-unamended | AC9 structure check fails or a no-op is invented | quoted | spec Scope row 3, AC9 | contested: HIGH / MEDIUM — settled HIGH: an acceptance check the plan cannot pass without an amendment blocks the close, and Codex adds ParticipantKicked, which C4 missed | plan.md:136, Steps 7 and 15, Decision 7, P5 | codex-3, sonnet-C4 |
| R1-04 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Step 10 imports constants Step 13 creates (wrong slice direction); consumers of removed timer actions and the `reportStore` return are unassigned | step-order-handoff | slice 8–11 cannot build | quoted | build slices | slices | plan.md:174, Steps 5, 7, 10, 13 | codex-4 |
| R1-05 | codex, sonnet | MEDIUM | seed-0130 | `connection!` in the pinned `connect` vs lint-to-zero; 9 `!` without an owning step | accept-criteria-conflict | the build breaks the pin or leaves lint red | quoted | Must-NOT-Change `connect` pin | lint | plan.md:136, Steps 1, 7, 15; useSprintHub.ts:215-216 | codex-5, sonnet-C1 |
| R1-06 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Step 4 acceptance gate (`X-Guest-Token` only in the client) cannot pass before Steps 5–8 | accept-criteria-conflict | slice 1 ends on an impossible gate | quoted | — | verification | plan.md:106 | codex-6 |
| R1-07 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | FE-23 typed `InjectionKey` for board actions has no delivering step | residual-fix-not-mapped | RetroGroupItem keeps its large prop/event interface | quoted | FE-23 suggested fix | component shape | plan.md:161, Step 9 | codex-7 |
| R1-08 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Base* adoption on RetroGroupItem rename controls, and EmptyState for error presentations, exceed the amended Decisions list | visible-change-unsourced | appearance changes outside Q2's screens | quoted | spec What must not change (visual) | design system | plan.md:203-204 | codex-8 |
| R1-09 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Verification does not establish F36's outcomes (chime unlock, cold-link names, permissions, load/refresh failure and retry) | proof-mechanics-unspecified | F36 regressions pass unseen | quoted | spec: F36 outcomes must survive | verification | plan.md:257, Steps 15–16 | codex-9 |
| R1-10 | sonnet | MEDIUM | seed-0130 | Focus revert restores one group; the previously focused group stays cleared | revert-scope-underspecified | the board shows no focus after a failed focus | scenario | spec Scope row 3 | optimistic UI | Step 8; useSprintHub GroupFocused | sonnet-C2 |
| R1-11 | sonnet | MEDIUM | seed-0130 | One `formatDate` for three different date outputs | helper-merge-changes-output | team-card and sprint-panel dates change | quoted | spec visual invariant | duplication | Steps 11, 13; SprintReportPanel:99, RetroReportPanel:96, SprintDetailPanel:15,17, TeamCard:91-92 | sonnet-C3 |
| R1-12 | codex, sonnet | LOW | seed-0137 | FE suggested-fix remnants: store `formattedTime` getter, FE-42 classList.toggle, FE-22 BaseButton `v-bind="$attrs"` | residual-fix-not-mapped | small dead or fragile leftovers | quoted | — | dead code | plan.md:115-116; useTheme.ts:17; BaseButton.vue:5; timer.ts:14 | codex-10, sonnet-L5 |
| R1-13 | codex | LOW | unranked — no anchor fits; Codex severity LOW | Palette grep cannot prove deduplication; intermediate vs final expectations in Step 15's reruns | proof-mechanics-unspecified | weak proof | quoted | — | verification | plan.md:207 | codex-11 |
| R1-14 | sonnet | LOW | seed-0127 | Step 7 relies on actions made optimistic in Step 8, across the slice cut | step-order-handoff | an unstated temporary non-optimistic window | quoted | — | slices | Steps 7, 8 | sonnet-L1 |
| R1-15 | sonnet | LOW | seed-0127 | Context says `vue.md` does not exist; it does | false-statement-in-context | the builder skips a tracked convention file | quoted | — | context | plan Context; docs/conventions/vue.md | sonnet-L2 |
| R1-16 | sonnet | LOW | seed-0127 | Decision 9 (a pending card hides edit/delete) lacks `Adds:` | untagged-addition | a visible rule is not surfaced | quoted | — | decisions | Decisions row 9 | sonnet-L3 |
| R1-17 | sonnet | LOW | seed-0127 | An offline Google join shows "(0)", not "(network error)" | error-text-regression | changed error text | quoted | spec visual/text invariant | errors | Steps 4, 6; useRetroRoom.ts:149 | sonnet-L4 |
| R1-18 | sonnet | LOW | seed-0127 | `vue-patterns` skill not mapped to Steps 7, 10, 11, 13 | skill-not-mapped | patterns skipped | quoted | — | skills | Skill Mapping | sonnet-L6 |
| R1-19 | sonnet | LOW | seed-0127 | Verification mechanics to invent: markdown-identity harness, per-step formatting, AC6 grep limits | proof-mechanics-unspecified | late unplanned work | quoted | campaign: no new tests | verification | Steps 1, 11, 15; AC6 | sonnet-L7 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | plan Step 8 (revert rule; Votes bullet: the total takes ±1 until a `newCount` or the response settles it; a failure takes the 1 back only while unsettled) and Accept (interleaving in the walkthrough); Decision 16 |
| R1-02 | fixed-real | — | — | plan Step 8 (card delete and link removal put back only if nothing settled them — `CardDeleted`, a refresh without the card, `CardUpdated`, `EvidenceLinkFetched`); Decision 16; Step 16 |
| R1-03 | fixed-real | — | — | spec Scope row 3 hub sentence and AC9 structure item (the `ParticipantLeft` exception, kick effect via store state); plan Step 7 (kick through a watched `kicked` state, no `onKicked` callback); Decision 7; P5; header `Amended:` line |
| R1-04 | fixed-real | — | — | plan Step 3 (creates `src/constants/columns.ts` with palette, default columns and default timers, switching all copies); Step 5 (`TimerUpdated`/`TimerSnapshot` handlers call the merged action); Step 7 (`RetroRoomView` reads the report store itself); Step 10 (consumes Step 3's constants); Step 13 (constants bullet removed); Skill Mapping rows 3, 13 |
| R1-05 | fixed-real | — | — | plan Step 7 (local constant replaces `connection!` in `connect`, the one sanctioned edit; owners for all 18 `!`: Step 7 2, Step 9 10, Step 13 6); Must-NOT-Change lifecycle row |
| R1-06 | fixed-real | — | — | plan Step 4 Accept (scoped to `src/api`); Step 8 Accept carries the whole-source AC3 gate |
| R1-07 | fixed-real | — | — | plan Step 9 (typed `InjectionKey` for the board action object; children inject; `RetroGroupItem` props shrink) and Accept; Satisfies FE-23 |
| R1-08 | fixed-real | — | — | spec Decisions row 1 closing sentence (error states and room controls unchanged); plan Step 13 (Base* only on the four screens, `RetroGroupItem`/`RetroRoomView` excluded, `EmptyState` for empty states only) and Accept; Decision 18 |
| R1-09 | fixed-real | — | — | plan Step 15 (F36 regression pass: chime, cold links, roles, foreground and background failure with retry, draft kept, single Enter); Step 16; Must-NOT-Change F36 and roles rows |
| R1-10 | fixed-real | — | — | plan Step 8 Group bullet (focus revert restores both groups, each while unsettled); Accept (failed focus in the walkthrough) |
| R1-11 | fixed-real | — | — | plan Step 11 (`formatDate`, `formatDateTime`, `formatShortDate`, byte-identical); Step 13 (call sites) and Accept (`toLocale` only in `src/format/`); Decision 17 |
| R1-12 | fixed-real | — | — | plan Step 5 (`formattedTime` getter deleted; `classList.toggle`); Step 13 (`BaseButton` `v-bind="$attrs"` dropped) |
| R1-13 | fixed-real | — | — | plan Step 3 Accept (palette declared once: `'slate'` only in `src/constants/columns.ts` and `src/types/domain.ts`); implementation preamble and Step 15 (intermediate vs final-state Accept lines) |
| R1-14 | fixed-real | — | — | plan Step 7 (today's logic moves unchanged into card-store actions; Step 8 rewrites them) |
| R1-15 | fixed-real | — | — | plan Context (`vue.md` exists and binds; its White Flash section is F36-superseded) |
| R1-16 | fixed-real | — | — | plan Decision 9 now an `Adds:` row, recommendation keep; Plan Review note (plan departures owed) |
| R1-17 | fixed-real | — | — | plan Step 6 (Google-join fallback shows "network error" for `ApiError.status` 0) |
| R1-18 | waived | LOW | LOW · seed-0127 · without the repo's skills-per-step switch a step maps one skill; the primary skills stand and `vue-patterns` is not required for the done check | — |
| R1-19 | fixed-real | — | — | plan Step 11 (throwaway harness in `.claude/tmp/` through Vite `ssrLoadModule`, proven at plan time); implementation preamble (per-step `prettier --write`); Step 7 and Step 15 (widened store-write grep, 18 hits today; alias writes read by hand) |

Reconciled: plan Context, Implementation preamble, Skill Mapping rows 3 and 13, Steps 3–11, 13, 15, 16, Must-NOT-Change (lifecycle, F36, roles), Decisions 7, 9, 16–18, P5, Plan Review; spec header `Amended:`, Scope row 3, Decisions row 1, AC9 — re-read against the clauses that name them (Step 10 ← Step 3 constants; Step 13 ← Step 11 formatters; Step 15 ← Step 7 grep; lifecycle row ← Step 7; Decision 7/P5 ← spec Scope row 3 and AC9; spec Decisions row 1 ← Step 13 and spec Scope row 6) — no contradiction found. Size line unchanged (no file added or removed by the fold).


## Round 2 — 2026-10-23 — plan — primary (codex)

Header note: Codex primary alone (no second reader after round 1), Floor MEDIUM, Read full (round 1's coverage was incomplete). Verdict NO-GO, persisted verbatim in `delivery/review-critic-codex.md` § "Plan review — round 2", cited here as the reader section. Fold verification: R1-01..R1-17 and R1-19 closed except R1-05 partial (template assertions → R2-04); R1-18 not closed (waived LOW, below floor). Merge by team-lead; the fold is the architect's. No CRITICAL, so no round 3.

### Merge — round 2

Coverage: complete (Codex reports a full read of plan, amended spec, questions, both prior reviews, F34 findings and all 65 source files).

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Partial events (EvidenceLinkFetched, single-field group events) settle a whole optimistic item | revert-scope-underspecified | a failed link removal leaves link A missing though the server kept it | scenario | spec Scope row 3 | fold | plan.md:150-151 | codex-r2-1 |
| R2-02 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Personal vote rollback "as today" overwrites a newer successful personal count | revert-clobbers-authoritative | `myVotes` and remaining budget wrong after overlapping retract/cast | scenario | spec Scope row 3 | round-1 miss: overlapping personal votes | plan.md:154; stores/vote.ts:122-126 | codex-r2-2 |
| R2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Step 10 replaces persisted per-sprint timer defaults with constants | helper-merge-changes-output | a sprint set to 12 minutes shows 5 when stopped | quoted | spec What must not change | round-1 miss: per-sprint timer settings | plan.md:178; RetroRoomView.vue:46-48; TimerDisplay.vue:20,95-105 | codex-r2-3 |
| R2-04 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Assertion inventory omits two template `!` (AppHeader.vue:43, RetroGroupItem.vue:25) | residual-fix-not-mapped | spec S:27 guard requirement unmet | quoted | — | fold | plan.md:141 | codex-r2-4 |
| R2-05 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Decision 18 (Base* stops at four screens) contradicts Step 12's TimerControls → BaseButton size="sm" | plan-self-contradiction | room timer controls change look outside the visible-change list | quoted | spec Decisions row 1 | fold | plan.md:197,297 | codex-r2-5 |
| R2-06 | codex | LOW | unranked — no anchor fits; Codex severity LOW | "Four create-sprint payload copies" baseline stale after F36 | stale-baseline | wrong inventory | quoted | — | round-1 miss: DTO inventory | plan.md:92 | codex-r2-L1 |
| R2-07 | codex | LOW | unranked — no anchor fits; Codex severity LOW | Questions ledger "None" vs Q4 / departure review | stale-statement | inconsistent ledger | quoted | — | round-1 miss: questions ledger | plan.md:353,357 | codex-r2-L2 |

Note for the fold: Q4 is now answered (PO-3, questions.md § Q4): keep Decisions row 9, recorded as derived from Scope row 3 + What must not change, not `Adds:`; AC9's optimistic walkthrough notes the pending card has no edit/delete and gains them on save.

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | HIGH | — | plan Step 8 (the revert rule now tracks per item and per field, and names what each payload settles; card delete and link-removal bullets; the group bullet's settle clause folded into the rule); Step 8 Accept walkthrough (failed link removal while another link is fetched); Decision 16 |
| R2-02 | fixed-real | HIGH | — | plan Step 8 Votes bullet (total and own count are recorded ±1 deltas; a later successful vote response settles both; a failure undoes only its unsettled delta); Step 8 Accept walkthrough (failed retract then successful cast); Decision 16 |
| R2-03 | fixed-real | MEDIUM | — | plan Step 10 (timers come from the sprint summary's `*TimerDefault` values through the store or `useRetroPhase`; the constants are the fallback only; walkthrough item); Step 3 (constants described as initial values and fallback); Step 15 walkthrough |
| R2-04 | fixed-real | MEDIUM | — | plan Step 7 (all 20 sites owned, with the inventory grep: 20 today, 0 after Step 13); Step 2 (`AppHeader` template site); Step 9 (`RetroGroupItem` 8, template site named); Step 15 (inventory grep → 0) |
| R2-05 | fixed-real | MEDIUM | — | plan Step 12 (`TimerControls` swaps `@apply` for the same utilities, no `BaseButton`); Decision 18 (room controls include the timer buttons; FE-34's suggestion narrowed) |
| R2-06 | fixed-real | LOW | — | plan Step 3 (payload inventory: one create-sprint payload and the column-config payload in the sprint store move to `types/sprint.ts`) |
| R2-07 | fixed-real | LOW | — | plan Decision 9 (re-recorded as derived from spec Scope row 3 + What must not change, per Q4/PO-3, no `Adds:` tag); `## Decisions and why` P6; `## Plan Review` note (no plan departures owed); `## Open Questions` stays "None." and is now consistent; spec AC9 optimistic item and the spec's Amended line (Q4) |

Q4 (PO-3) folded with R2-07: `## Decisions` now holds no `Adds:` or `Departs from` row, so the plan owes no `plan departures` question.

Reconciled: plan Step 8 (revert rule, card, group, readiness and votes bullets, Accept walkthrough) against Decision 16, the Must-NOT-Change rows "Server-authoritative actions stay non-optimistic" and "Roles' permissions" (pending-card clause, now derived) and spec Scope row 3; Step 10 against Step 3 (constants as fallback), Must-NOT-Change "Visual appearance" and spec What must not change; Step 7's site table against Steps 2, 9, 13 and Step 15's grep (2 + 1 + 11 + 6 = 20); Step 12 against Decision 18 and spec Decisions row 1's closing sentence (room controls keep their look — unchanged text covers the timer buttons); Step 3's payload sentence against `src/types/sprint.ts` and `src/types/card.ts` in Step 3's file list; Decision 9 against P6, Q4, the `## Plan Review` note, `## Open Questions` and spec AC9; Step 15 walkthrough against Step 16 (operator items unchanged: it takes every unobserved Step 15 item); the plan header's Size line (no step or file added: 15 developer-built steps, 105 files) and `**Build slices:** 1–4 · 5–7 · 8–11 · 12–15` (unchanged).
