## Round 1 — 2026-10-22 — spec — primary (codex)

Header note: two readers — Codex primary (read-only job, verdict persisted verbatim in `review-critic-codex.md`) and a Sonnet nexus critic as second reader; pair `codex + sonnet`, picked by the owner. The spec had been committed as Ready without review; the owner caught it and it was set back to Draft before this round.

Primary reader: see `review-critic-codex.md` — `## Verdict: NO-GO`, 2 HIGH, 7 MEDIUM, 3 LOW.

## Second reader — round 1 (sonnet)

Verdict: REVISE. (Condensed by the architect, not verbatim: every finding is kept with its level, evidence and fix; the reader's full message is in the session transcript.)

- **[HIGH] F36-01 — FE-31 rests on a backend rule the code contradicts.** Edit/Delete take the author-only branch whenever a `participantId` claim exists; a Google user in the room always has one (`useRetroRoom` joins with Google, stores send the guest token, `OwnerOrScrumMaster` merges both schemes). The owner branch is unreachable from the room, so `author || isScrumMaster` still shows controls the server refuses. Also: action items in Discussing need a `NameIdentifier` claim. Fix: re-derive from the endpoints (author only), state the Discussing case, add a verification step.
- **[MEDIUM] F36-02 — Acceptance proves only the visual fixes.** FE-07, FE-08, FE-14, FE-28, FE-31, FE-32, FE-22 have no check; FE-13's check proves only that the WAV is gone. Fix: add greps (`requiresAuth`, `inheritAttrs: false`, `team-sprints`) and a failure-injection and role walkthrough.
- **[MEDIUM, band MEDIUM–HIGH] F36-03 — `requiresAuth` on `report` cuts off guests.** `ReportView` loads the guest token; `GetSprintReportEndpoint` accepts matching participants. Fix: name routes; only `sprint` is clearly safe.
- **[MEDIUM] F36-04 — "every site the finding lists" includes non-defective sites.** FE-12: only `RetroCardItem.vue:95` uses `/30`; the rest is safelisted; the action-items branch must be covered. FE-20: the defect is `RetroGroupItem.vue:125`; ring and drag-over are not shown unreadable. Fix: name the sites.
- **[MEDIUM] F36-05 — FE-07: clearing only on success opens a duplicate-submit window.** The add actions swallow errors, so the panel cannot tell outcome; "cleared only on success" and "restored" are two mechanisms. Fix: one mechanism, a success signal, a pending guard.
- **[MEDIUM, band MEDIUM–HIGH] F36-06 — FE-08 underspecified; a failed fetch can show a previously visited sprint.** `fetchSprint` never resets `currentSprint`. Views not enumerated; the retro room's five fetches; `fetchMe` failure outcome unstated. Untouchable: silent-wrong-output. Fix: clear target and set error first; list views; state `fetchMe` outcome.
- **[MEDIUM] F36-07 — FE-13: AudioContext autoplay state.** Expiry arrives without a gesture; a new context starts suspended. Fix: create/resume on a gesture or accept silence; add an audible check.
- **[LOW] F36-08 — FE-05 mechanism does not exist.** The blocking script sets only a class. Fix: name the `<style>` mechanism; say whether the dev flash is accepted.
- **[LOW] F36-09 — "50 sites" is 49 plus the FE-11 site.**
- **[LOW, band LOW–MEDIUM] F36-10 — `vue.md:25` will contradict the code; nothing owns the fix.**
- **[LOW] F36-11 — FE-11 and FE-14 options left open.** Route: plan must resolve.
- **[LOW] F36-12 — F39 does not carry F36's behaviour forward.** Route: F39 owner.

Coverage ledger (second reader): FE-04/11, FE-13, FE-31, FE-32, FE-22, sibling allocation COVERED; FE-05, FE-12, FE-07, FE-14, FE-08, FE-28, FE-20 PARTIAL; F35/F37/F38 specs grep-only; some store/component files NOT OPENED.

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | seed-0007 | FE-31 rule contradicts the endpoints (owner branch unreachable; Discussing action items) | decision premise contradicted by code | owner sees controls the server refuses | quoted, path | — | scope | Scope FE-31, Decisions | codex-1, F36-01 |
| R1-02 | codex, sonnet | HIGH | contested: HIGH / MEDIUM — settled HIGH: seven of thirteen outcomes had no check at all | Acceptance covers only visual fixes | acceptance does not prove outcome | behavioural fixes ship broken with checks green | scenario | — | acceptance | Acceptance | codex-2, F36-02 |
| R1-03 | codex, sonnet | MEDIUM | seed-0023 | `requiresAuth` on `report` cuts off guests | access change not decided | guests redirected from a URL the server serves them | path | — | scope | Scope FE-28 | codex-5, F36-03 |
| R1-04 | codex, sonnet | MEDIUM | seed-0022 | FE-08 store inventory and state contract incomplete; stale sprint shown | state contract not stated | another sprint's data under the wrong URL | scenario, path | silent-wrong-output | scope | Scope FE-08 | codex-3, F36-06 |
| R1-05 | codex | MEDIUM | unranked — Codex severity MEDIUM | FE-28 team names: only `SprintListView` covered; `sprint` and `report` headers still blank | scope narrower than defect | breadcrumb stays blank | path | — | scope | Scope FE-28 | codex-4 |
| R1-06 | codex | MEDIUM | unranked — Codex severity MEDIUM | `inheritAttrs` fix moves `class="flex-1"` off the wrapper | fix breaks layout invariant | note inputs lose sizing | path | — | scope | Scope FE-22 | codex-6 |
| R1-07 | codex, sonnet | MEDIUM | seed-0041 | FE-05 doc update (`vue.md`, skill) has no owner; mechanism misnamed | orphaned doc update | conventions keep prescribing the defect | quoted | — | sibling | Scope FE-05 | codex-7, F36-08, F36-10 |
| R1-08 | codex, sonnet | MEDIUM | seed-0022 | FE-13 autoplay: context suspended; acceptance passes if sound is deleted | mechanism and acceptance gap | chime still silent, unnoticed | path | — | acceptance | Scope FE-13, AC4 | codex-8, F36-07 |
| R1-09 | codex | MEDIUM | unranked — Codex severity MEDIUM | Visual walkthrough lacks the states that expose the defects | weak manual acceptance | defects unseen in a generic look | scenario | — | acceptance | Decisions row 3 | codex-9 |
| R1-10 | sonnet | MEDIUM | seed-0022 | FE-12/FE-20 site lists include non-defective sites | scope broader than defect | working files rewritten | path | — | scope | Scope FE-12, FE-20 | F36-04 |
| R1-11 | sonnet | MEDIUM | seed-0022 | FE-07 duplicate-submit window; no success signal | mechanism gap | duplicate cards | path | — | scope | Scope FE-07 | F36-05 |
| R1-12 | codex, sonnet | LOW | seed-0127 | Radius count 49/18, not 50/19 | count off | reader confusion | path | — | count | Scope FE-04 | codex-11, F36-09 |
| R1-13 | codex | LOW | unranked — Codex severity LOW | Build command lacks a working directory | acceptance mechanics | command fails from root | path | — | acceptance | AC3 | codex-10 |
| R1-14 | codex | LOW | unranked — Codex severity LOW | Delete dialog does not clear before the call; its defect is feedback and busy state | misdescribed defect | wrong fix | path | — | scope | Scope FE-14 | codex-12 |
| R1-15 | sonnet | LOW | seed-0041 | FE-11 / FE-14 options left open | unresolved option | builds differ | quoted | — | scope | Scope FE-11, FE-14 | F36-11 |
| R1-16 | sonnet | LOW | seed-0127 | F39 does not carry F36's outcomes forward | cross-spec hand-off | F39 undoes F36 | path | — | sibling | F39 § What must not change | F36-12 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-22)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | Scope FE-31; Decisions row 1 |
| R1-02 | fixed-real | — | — | Acceptance 5–6 |
| R1-03 | fixed-real | — | — | Scope FE-28; Decisions row 2; What must not change |
| R1-04 | fixed-real | — | — | Scope FE-08 (stores, views, clear-on-start, `fetchMe`) |
| R1-05 | fixed-real | — | — | Scope FE-28 (three routes) |
| R1-06 | fixed-real | — | — | Scope FE-22 (`class`/`style` stay on wrapper) |
| R1-07 | fixed-real | — | — | Scope FE-05; Decisions row 3; F40 spec (vue.md row) |
| R1-08 | fixed-real | — | — | Scope FE-13; Acceptance 4, 6 |
| R1-09 | fixed-real | — | — | Acceptance 6 |
| R1-10 | fixed-real | — | — | Scope FE-12, FE-20 |
| R1-11 | fixed-real | — | — | Scope FE-07 |
| R1-12 | fixed-real | — | — | Scope FE-04 |
| R1-13 | fixed-real | — | — | Acceptance 3, What must not change |
| R1-14 | fixed-real | — | — | Scope FE-14 |
| R1-15 | fixed-real | — | — | Scope FE-11 (`max-w-6xl`), FE-14 (form owns the call) |
| R1-16 | fixed-real | — | — | F39 spec § What must not change |

Reconciled: Scope FE-28 ↔ What must not change ↔ AC5 (only `sprint` guarded); Scope FE-31 ↔ Decisions row 1 ↔ AC6 roles; Scope FE-05 ↔ Decisions row 3 ↔ F40 vue.md row; F39 § What must not change (F36 outcomes carried).

## Round 2 — 2026-10-23 — spec — primary (codex)

Header note: Codex alone. Read: delta. Floor: MEDIUM. Verdict and fold-verification table persisted verbatim in `review-critic-codex.md` § Round 2: all round-1 findings closed except R1-02 partial; no new finding.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — Codex severity MEDIUM | Walkthrough lacks HTTP-error failures, retry success and single-submit success | acceptance does not prove outcome | server-error paths unchecked | quoted | — | fold | AC6 | R1-02 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | AC6 Failures (offline and 500 response; retry; single Enter) |

Reconciled: AC6 ↔ Scope FE-07, FE-08, FE-14. No round 3: no CRITICAL.

## Round 1 — 2026-10-23 — plan — primary (codex)

Header note: primary is a read-only Codex job (task-mvkfu3j3-o8f0dg), persisted by the team lead to `review-critic-codex.md` § "Plan review — round 1"; its verdict and findings are reproduced below. Second reader: nexus:critic on Sonnet. Codex returned no coverage ledger.

### Codex — verdict and findings (verbatim)

Verdict: NO-GO

1. HIGH — plan.md:94, Step 4 / Decision 1: FE-08 explicitly requires clearing target data on every fetch start. The hub callers justify considering a background-refresh exception for cards/groups; they do not justify retaining data across every same-target load. This also breaks Step 6's report mechanism: `ReportView.vue:2` renders its error slot only when `report` is absent, so a failed same-sprint reload with retained data hides the error and Retry. Suggested fix: Restore the required foreground-load contract. Reconcile any narrowly scoped background exception explicitly, and make report errors take precedence over retained content.
2. HIGH — plan.md:136, Step 7 / Decision 2: Background fetch failures remain invisible, with no retry. For example, a failed `fetchGroups` after `GroupDissolved` leaves obsolete groups displayed without recovery controls. FE-08 requires the consuming views to surface errors with Retry; it does not limit that requirement to entry. Suggested fix: Provide visible error/retry handling for background failures too. Preserving the board need not mean hiding the failure.
3. HIGH — plan.md:97, Steps 4–5: Tracking the data's target and clearing at request start does not prevent an older response overwriting a newer target. `SprintListView.vue:27` launches another fetch whenever `teamId` changes, without cancelling the preceding request. Group writes and hub events also overlap `fetchGroups` calls, contradicting the plan's no-concurrent-load premise. Suggested fix: Specify request/target guards for committing data, errors, and loading completion, with independent status where different resources share a store.
4. HIGH — plan.md:135, Step 7: One error scan "after the loads" does not establish safe bootstrap ordering. The existing composable fetches the summary before authentication/join and again inside `enterRoom`; the second call clears the first call's error. Google join failures can also reach the preserved join-form catch before prerequisite failures are surfaced. Existing room `loading` becomes false only on successful entry. Suggested fix: Define explicit stages: check policy/summary before identity or join, check room loads before votes/connect, and finalize loading on every failure. Retry must clear the room error, prevent overlapping attempts, and resume loading without unnecessarily repeating a successful join.
5. HIGH — plan.md:193, Step 13 / Decision 9: The operator fallback treats unperformed walkthrough checks as a valid completion deviation. Spec AC6 requires a recorded walkthrough with observations for every item. This can also leave Step 2's conditional dark-theme decisions unverified. Suggested fix: Permit an operator handoff, but keep AC6 and delivery acceptance pending until the missing observations are recorded.
6. MEDIUM — plan.md:112, Step 5 / Decision 7: `participant.fetchMe` is excluded from the fetch contract even though FE-08 covers every fetch in that store. It currently leaves `currentParticipant` untouched on failure and exposes no loading/error state. Suggested fix: Apply the required data/error/loading lifecycle while retaining the status information needed for the room's 401 handling.
7. MEDIUM — plan.md:135, Steps 6–7: The room's reporting fetch is missing from the recovery plan. `useRetroRoom.ts:63` starts `fetchParticipantReport` after connection, and `RetroRoomView.vue:255` starts it on a phase transition. `RetroReportPanel.vue:4` shows its error but has no Retry. Suggested fix: Add report-specific retry handling for both paths, preserving the existing connection when retrying the report.
8. MEDIUM — plan.md:145, missing `Adds:` rows: No step contains an `Adds:` row. Step 8 introduces unconditional focus restoration, which can steal focus after the user moves to another column; FE-07 does not request that behavior. Step 13 introduces the unsupported acceptance fallback, and Step 2 adds an unknown-colour fallback. Suggested fix: Audit every step and add its required `Adds:` disclosure. Remove unsupported behavior under the campaign constraint; bound any focus restoration to preserving the initiating keyboard interaction.
9. MEDIUM — plan.md:27, missing `Departs from:` rows: Concrete departures include retaining `@apply` in Step 1 despite `tailwind-theme` prohibiting it; replacing that skill's inline-body prescription in Step 2; retaining Options stores and throwing actions in Steps 4–5 despite `pinia-patterns`; and extending Step 6's cited Page Shell Pattern, whose actual template is initializing → empty → content. Suggested fix: Add explicit `Departs from:` rows identifying each adaptation and its governing spec requirement. Preserve F36's scope rather than refactoring merely to match the guides.

## Second reader — round 1 (sonnet)

Verdict: ACCEPT (no CRITICAL/HIGH after self-audit). Decisions 1 and 2 hold against the code as wired (`useSprintHub.ts:67-71,108-120`; `cardGroup.ts:64,79,92,105,158`); 49 radius sites in 18 files plus `PageLayout` confirmed; FE-31 mirror correct against `EditCardEndpoint.cs`, `DeleteCardEndpoint.cs`, `PhaseActionConfiguration.cs`.

Coverage ledger (summary): COVERED — FE-04/11, FE-05/20/12, FE-22/07, FE-08 stores (all 10 store files + useSprintHub), FE-31, FE-14, FE-13, AC1–AC6, skill patterns, F39 sibling. PARTIAL — FE-08 views/room (P-01, P-02), FE-28/32 (P-03), F34 findings (in-scope rows only). NOT OPENED — RetroColumnPanel, RetroMobileTabBar, RetroPhaseHeader, PhaseIndicator, BaseCard, BaseModal, BaseBadge, ToastContainer (radius-only edits, covered by grep); PreviousActionsPanel.vue and TimerDisplay.vue internals; F37/F40 specs (grep only). Ruler: seed only.

- [MEDIUM] P-01 (seed-0130; band MEDIUM–HIGH) — Step 6 does not say error beats held data, and Decision 1 now creates that state. `ReportView.vue` renders its message only when `!reportStore.loading && !report`; with the same report kept and `error` set the stale report shows, with no error or Retry. Fix: in all five views `error` takes precedence over held data; add to Step 6 Accept.
- [MEDIUM] P-02 (seed-0130) — Step 7 puts the entry-load check "after the loads"; a first-time guest never reaches `enterRoom`, so offline lands on the join form with `Sprint #id`. Fix: read policy and summary errors immediately after those loads, before `authStore.fetchMe` and the join branches; guest-identity and `enterRoom` results after theirs; retry re-runs from the top.
- [MEDIUM] P-03 (seed-0130; band MEDIUM–HIGH) — Step 11's header refetch condition (signed in, `teams` empty, no load in flight) is also the state after a failed load, so a refetch loop runs during an outage. Fix: trigger on route change only, skip when `teamsStore.error` is set, no retry from the header; add an offline check to Accept.
- [LOW] P-04 (seed-0137) — Step 10 names `CreateTeamForm` as the shape but the `teamId` prop departure has no `Departs from` tag and no `**Mirrors:**` header line. Fix: add both; recommend keep.
- [LOW] P-05 (seed-0137) — Decision 1 contradicts the literal spec sentence ("clears its target data and its `error` when it starts") and the spec is not reconciled. Fix: one spec line saying the clear applies per target, or mark the plan row a spec amendment.
- [LOW] P-06 (seed-0137) — "No view runs two of one store's loads at once" is false for the hub (`fetchGroups` from a group write and from `CardsGrouped` concurrently); a success never clears a stale `error`; the sprint store shares one pair across three loads. Fix: clear `error` on success too, or reword the premise to "the room reads store `error` only at entry". Route: plan must resolve.
- [LOW] P-07 (seed-0137) — Step 8 returns focus to the input but Step 3 leaves `BaseInput` with no handle (root is a wrapper div, nothing exposed); focus must happen after `disabled` clears. Fix: name the approach in Step 8.
- [LOW] P-08 (seed-0137) — Step 4's acceptance grep passes vacuously on the forms present (`if (res.ok) {…}` in `sprint.ts:90-99` and `policy.ts`; `if (!res.ok) { this.items = []; return }` in `previousActions.ts`). Fix: "no `res.ok` check in a load action without a branch that sets `error`" (review-diff check).

Gap analysis: every spec requirement maps to a step; AC7 handed to the coordinator. Decision 2 holds (`connect()` already disconnects first; Google join is idempotent). Newly non-silent non-OK responses now block room entry — the walkthrough is the only guard, acceptable. The unknown-colour fallback is unreachable (server validates the palette). Campaign constraints respected. Open questions: none.

### Merge — round 1

Coverage: F37/F40 specs (grep only); radius-only components RetroColumnPanel, RetroMobileTabBar, RetroPhaseHeader, PhaseIndicator, BaseCard, BaseModal, BaseBadge, ToastContainer; PreviousActionsPanel.vue and TimerDisplay.vue internals — not opened by the second reader, and Codex returned no ledger showing it opened them

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH (contested: HIGH / MEDIUM — settled HIGH: the narrowing overrides a binding spec clause and a concrete warm-store scenario hides the error) | seed-0130 (sonnet); unranked — no anchor fits; Codex severity HIGH | Decision 1 retains same-target data, overriding spec FE-08; views don't give error precedence over held data | spec clause overridden; new state not carried into views | stale data shown with no error or Retry on a failed same-target reload | ReportView.vue:2; plan Steps 4, 6; spec FE-08 | spec FE-08 wording | decision-1 | plan § Step 4, § Step 6, § Decisions 1; spec FE-08; ReportView.vue, SprintListView.vue, TeamDashboardView.vue | codex-1, P-01, P-05 |
| R1-02 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Background (hub / post-write) fetch failures in the room are invisible, no Retry | requirement scoped too narrowly | obsolete board shown with no recovery after a failed refetch | plan Step 7; FE-08 | — | room-errors | plan § Step 7, § Decision 2; useSprintHub.ts | codex-2 |
| R1-03 | codex, sonnet | HIGH (contested: HIGH / LOW — settled HIGH: an older team's response can commit under a newer teamId, breaking the plan's own "no other target's data" guarantee) | unranked — no anchor fits; Codex severity HIGH; seed-0137 (sonnet) | No request/target guard: late responses overwrite newer targets; concurrent loads break the one-pair premise | contract premise false | wrong team's sprints shown; stale error or loading | SprintListView.vue:27; cardGroup.ts; useSprintHub.ts | — | concurrency | plan § Step 4, § Step 5 | codex-3, P-06 |
| R1-04 | codex, sonnet | HIGH (contested: HIGH / MEDIUM — settled HIGH: the Accept cannot be met by the step text and loading never finalises on failure) | unranked — no anchor fits; Codex severity HIGH; seed-0130 (sonnet) | Room bootstrap ordering: checks not staged; guest path never reaches enterRoom; summary refetch clears error; loading not finalised on failure; retry overlap | ordering unspecified | offline guest lands on the join form with a placeholder name; room stuck loading | useRetroRoom.ts onMounted / enterRoom; RetroRoomView.vue | — | room-errors | plan § Step 7, § Decision 2 | codex-4, P-02 |
| R1-05 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Step 13 operator fallback lets unperformed walkthrough items count as a justified deviation; AC6 needs every observation | acceptance weakened | AC6 and Step 2's conditional dark-theme checks ship unverified | plan Step 13 / Decision 9; spec AC6 | spec AC6 | acceptance | plan § Step 13, § Decision 9; spec § Acceptance | codex-5 |
| R1-06 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | `participant.fetchMe` excluded from the fetch lifecycle contract | requirement scoped too narrowly | no loading or error for participant identity | plan Step 5 / Decision 7 | 401 handling in the room | room-errors | plan § Step 5; stores/participant.ts | codex-6 |
| R1-07 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Room's participant-report fetch (after connect, on phase change) has no Retry path | missing caller | report panel error with no recovery | useRetroRoom.ts:63; RetroRoomView.vue:255; RetroReportPanel.vue:4 | — | room-errors | plan § Steps 6–7 | codex-7 |
| R1-08 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | No `Adds:` rows: focus restoration (Step 8), acceptance fallback (Step 13), unknown-colour fallback (Step 2) | untagged additions | added behaviour not surfaced; campaign forbids new product behaviour | plan Steps 2, 8, 13 | campaign: no new product behaviour | tagging | plan § Steps 2, 8, 13 | codex-8 |
| R1-09 | codex, sonnet | MEDIUM (contested: MEDIUM / LOW — settled MEDIUM: several named-pattern departures, not one) | unranked — no anchor fits; Codex severity MEDIUM; seed-0137 (sonnet) | No `Departs from:` rows or `**Mirrors:**` line: @apply kept, tailwind-theme inline body replaced, Options stores kept, Page Shell extended, CreateSprintForm teamId prop | untagged departures | departures not surfaced as decisions | plan header; Steps 1, 2, 4–6, 10; Decision 8 | — | tagging | plan header, § Steps 1, 2, 4, 5, 6, 10 | codex-9, P-04 |
| R1-10 | sonnet | MEDIUM | seed-0130 | Step 11 header refetch trigger equals the post-failure state, so it loops during an outage | trigger condition true after failure | repeated requests while the server is down | AppHeader.vue; stores/teams.ts; plan Step 11 | — | header-teams | plan § Step 11 | P-03 |
| R1-11 | sonnet | LOW | seed-0137 | Step 8 focus return has no mechanism; BaseInput exposes no handle | mechanism not stated | ad-hoc DOM reach or an unstated defineExpose | BaseInput.vue; plan Steps 3, 8 | — | focus | plan § Step 3, § Step 8 | P-07 |
| R1-12 | sonnet | LOW | seed-0137 | Step 4 acceptance grep is vacuous on the silent forms present | acceptance does not detect the defect | gate green while loads still swallow failures | sprint.ts:90-99; policy.ts; previousActions.ts | — | acceptance | plan § Step 4 Accept | P-08 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | plan § Step 4 (load contract: foreground clear on start; background refresh opt-in for cards/groups only), § Step 5, § Step 6 (error before content in every view, `ReportView` named), § Decisions 1, § P1; spec FE-08 row (refresh clause) and spec § Decisions (new row) |
| R1-02 | fixed-real | — | — | plan § Step 7 (refresh banner with Retry, board kept), § Decisions 2, § P2; spec FE-08 row. Call recorded: the banner is read as FE-08's own "error surfaced with a retry" for the room, not new behaviour, so no `Adds:` tag |
| R1-03 | fixed-real | — | — | plan § Step 4 (per-resource `loading`/`error`; latest request wins; success clears `error`), § Step 5; spec FE-08 row (latest-request sentence) |
| R1-04 | fixed-real | — | — | plan § Step 7 (three checked stages, `loading` finalised on failure, retry no-op while running and resuming at the failed stage, no re-join), § Decisions 2, § P2 |
| R1-05 | fixed-real | — | — | plan § Step 13 (unobserved items not recorded as seen), new § Step 14 (Owner: operator; AC6 open until recorded; close reports open acceptance; change note reads pending), § Decisions 9, § Testing Strategy, § Must-NOT-Change |
| R1-06 | fixed-real | — | — | plan § Step 5 (`participant.fetchMe` in the contract, keeps the HTTP status for the 401 path), § Step 7 stage 2, § Decisions 7 |
| R1-07 | fixed-real | — | — | plan § Step 7 (Report retry in `RetroReportPanel.vue`, connection untouched) |
| R1-08 | fixed-real | — | — | plan § Decisions 3 (`Adds:` focus return, bounded to Enter-started saves and unmoved focus), § Step 8 (built only if kept), § P3; § Step 2 unknown colour now keeps today's outcome (no new fallback); § Step 13/14 fallback is process, not product behaviour — no tag. Campaign call: the focus return is behaviour no source asks for, so it goes to the owner as a tagged row, recommended keep |
| R1-09 | fixed-real | — | — | plan § Decisions 10–13 (`Departs from` tailwind-theme ×2, pinia-patterns, create-vue-feature, each required by the spec), § Skill Mapping note, § Steps 1, 2, 4, 6, § Scope out-of-scope list. No `**Mirrors:**` line: the owner named no pattern (Decision 8) |
| R1-10 | fixed-real | — | — | plan § Step 11 (route-entry trigger, skipped while teams `error` is set, no header retry; offline Accept) |
| R1-11 | fixed-real | — | — | plan § Step 8 (`defineExpose({ focus })` on `BaseInput`, called after `nextTick` once enabled — only if Decision 3 is kept) |
| R1-12 | fixed-real | — | — | plan § Step 4 Accept (review-diff check: both failure paths set `error`; the three silent forms named; latest-request guard), § Step 5 Accept |

Reconciled: plan § Step 4 contract ↔ § Step 5 (refresh option, `participant.fetchMe`) ↔ § Step 7 (foreground loads at entry, background refresh banner) ↔ § Must-NOT-Change SignalR row (only the six call arguments in `useSprintHub.ts`) ↔ § Decisions 1, 2, 7 ↔ § P1, P2 ↔ spec FE-08 row and spec § Decisions; § Step 8 ↔ § Step 3 ↔ § Decisions 3 ↔ § P3; § Step 13 ↔ § Step 14 ↔ § Decisions 9 ↔ § Testing Strategy ↔ § Must-NOT-Change (Steps 13–14) ↔ § Step 2 (`primary-50/100` check); § Step 11 ↔ § Step 4 (teams `error`); header Size (45 files: `useSprintHub.ts` added; Step 14 is operator-owned, outside the slices and the count) ↔ § Build slices (unchanged, 1–3 · 4–7 · 8–13).

## Round 2 — 2026-10-23 — plan — primary (codex)

Header note: Codex read-only job task-mvkgel70-dldqs7, Floor MEDIUM, Read full (round-1 Coverage incomplete); no second reader after round 1. Persisted by the team lead; the Codex heading `## Verdict:` is shown as text below.

### Codex — verdict, fold verification and findings (verbatim)

Verdict: NO-GO

Table lines refer to [plan.md](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md). Collateral identifies a fold-created contradiction or broken guarantee; `none` means none found.

| Round-1 ID | Fold status | Plan lines | Collateral |
|---|---|---|---|
| R1-01 | closed | 96, 122, 127, 247 | R2-01 |
| R1-02 | closed | 143, 248 | R2-01 |
| R1-03 | closed | 95, 98, 99 | none |
| R1-04 | closed | 136, 137, 138, 139, 141 | none |
| R1-05 | closed | 202, 211, 212, 255 | none |
| R1-06 | closed | 113, 138, 253 | none |
| R1-07 | closed | 144 | none |
| R1-08 | partial | 78, 154, 202, 249, 285 | R2-03 |
| R1-09 | closed | 254, 256, 257, 258, 259 | none |
| R1-10 | closed | 181, 184 | none |
| R1-11 | closed | 154 | none |
| R1-12 | closed | 105, 117 | R2-04 |

The background-failure banner and room-report Retry implement FE-08’s required error recovery. The current [spec.md:31](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/definition/spec.md:31) explicitly requires the former and covers the latter through the retro room’s error-with-retry requirement. Neither needs an `Adds:` row.

1. **R2-01 — MEDIUM — [plan.md:112](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:112): The background-refresh inventory and pinned diff limit omit two call sites.**  
   `useSprintHub.ts` contains eight calls: lines 67, 68, 70, 71, 108, 112, 116 and 120. Steps 5 and the invariant at line 229 repeatedly limit changes to six. Following that limit leaves foreground loaders in hub handlers; their new clear-on-start behavior contradicts FE-08’s background exception and the “never blanks the board” guarantee at line 232.  
   **Suggested fix:** Require the background option at all eight existing call sites and reconcile the acceptance check and invariant.  
   **Tag:** `fold`

2. **R2-02 — MEDIUM — [plan.md:126](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:126): Route-parameter changes bypass the proposed stale-data protection.**  
   `SprintView.vue:30`, `ReportView.vue:31` and `RetroRoomView.vue:121` capture IDs once; their loaders run on mount, and `App.vue:3` has an unkeyed `RouterView`. Vue Router [reuses component instances when parameters change](https://router.vuejs.org/guide/essentials/dynamic-matching.html#reacting-to-params-changes). Consequently, navigation between IDs within one route can retain the previous target without issuing another fetch. Request counters cannot protect a request that never starts; Retry also retains the captured IDs. This leaves the plan’s “never another target’s data” outcome unmet.  
   **Suggested fix:** Specify target-dependent reloads or remounting for these views, including room teardown before initializing another sprint.  
   **Tag:** `round-1 miss: route-parameter lifecycle`

3. **R2-03 — MEDIUM — [plan.md:249](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:249): Disclosing focus restoration does not resolve the campaign constraint.**  
   The fold bounds focus restoration and adds its disclosure, but still marks the addition `decided`; Step 8 conditionally builds it and P3 leaves an owner keep/drop choice. The plan itself acknowledges that no source requests this behavior. That conflicts with the binding “no new product behaviour” rule at line 17. R1-08 is therefore only partially closed.  
   **Suggested fix:** Remove the optional focus-restoration work and reconcile Decision 3, P3 and the closure claim with the campaign scope.  
   **Tag:** `fold`

4. **R2-04 — MEDIUM — [plan.md:105](D:/src/reflekt-F36-FrontendDefects/docs/specs/F36-FrontendDefects/delivery/plan.md:105): The strengthened acceptance check contradicts the auth exception immediately above it.**  
   Line 105 requires every listed load action’s non-OK branch to set `error`, including `auth.fetchMe`. Line 104 requires HTTP failures to preserve signed-out behavior with no message. Applying the acceptance rule to a normal unauthorized `/auth/me` response would make Step 6 display the unreachable-server notice during ordinary guest access.  
   **Suggested fix:** Explicitly exempt auth HTTP responses from that acceptance rule; require auth’s notice-producing error only for network failures.  
   **Tag:** `fold`

Read-only review completed; no files modified and no builds or tests run.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Background-refresh inventory names six hub call sites; useSprintHub.ts has eight | inventory incomplete | two hub handlers keep foreground loaders that blank the board | useSprintHub.ts:67,68,70,71,108,112,116,120; plan line 112, 229, 232 | FE-08 background exception | fold | plan § Step 5, § Must-not-change | codex R2-01 |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Route-param changes reuse the component; IDs captured once, so no refetch and stale target shown | lifecycle unhandled | another sprint's data shown under a new URL; Retry targets the old ID | SprintView.vue:30; ReportView.vue:31; RetroRoomView.vue:121; App.vue:3 | — | round-1 miss: route-parameter lifecycle | plan § Steps 6–7 | codex R2-02 |
| R2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Focus restoration still conditionally built; conflicts with the campaign's no-new-behaviour rule | addition kept against a binding constraint | new product behaviour shipped | plan line 17, 249; Decision 3; Step 8 | campaign directive | fold | plan § Step 8, § Decisions 3, P3 | codex R2-03 |
| R2-04 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Step 4 acceptance (every non-OK sets error) contradicts the auth fetchMe exception | acceptance contradicts contract | guests would see the unreachable-server notice on a normal 401 | plan lines 104–105 | auth signed-out behaviour | fold | plan § Step 4 | codex R2-04 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | plan § Step 5 (all eight `fetchCards`/`fetchGroups` hub calls at `useSprintHub.ts:67,68,70,71,108,112,116,120` take the background option; Accept grep → 8 hits), § Must-NOT-Change SignalR row (eight calls) |
| R2-02 | fixed-real | — | — | plan § Step 6 (`App.vue` `<RouterView>` keyed by route path: an id change within one route remounts the view — new-id loads and Retry, room torn down through its `onUnmounted` before the next sprint's room), § Step 6 Accept (key present; back/forward between two sprint, report or room URLs shows each one's data), § Decisions 14 |
| R2-03 | fixed-real | — | — | plan § Step 8 (focus bullet removed: no `BaseInput` `focus()`, no focus move), § Decisions 3 (rewritten: dropped at the plan-departures ask, owner away, presumed (proceed-default), not user-confirmed), § P3 (State: replaced by plan P4), new § P4; also closes round-1 R1-08 |
| R2-04 | fixed-real | — | — | plan § Step 4 Accept (`auth.fetchMe` exempt from the non-OK rule; its `error` only on a rejected `fetch`; a guest's 401 leaves no `error`) |

Reconciled: plan § Step 5 ↔ § Must-NOT-Change SignalR row ↔ spec FE-08 refresh clause (all hub refreshes are background); § Step 6 (`App.vue` key) ↔ § Step 7 (room retry and teardown, hub connect only after stage 3) ↔ § Step 12 (unlock listener removed on unmount) ↔ § Decisions 14 ↔ § Step 13 walkthrough Deep links; § Step 8 ↔ § Step 3 (BaseInput unchanged beyond the attrs split) ↔ § Decisions 3 ↔ § P3 → P4 ↔ § Plan Review note; § Step 4 Accept ↔ § Step 4 `auth.fetchMe` bullet ↔ § Step 6 App notice. Header Size unchanged (13 developer-built steps, 45 files — `App.vue` already counted); lane and slices unchanged. Plan departures (owner away, away line, presumed): Decisions 10–13 kept, Decision 3 dropped. No round 3: no CRITICAL.
