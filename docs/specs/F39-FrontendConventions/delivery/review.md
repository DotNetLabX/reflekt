# F39-FrontendConventions — Review

## Step 2 — Code Review

**Verdict: REQUEST CHANGES**

Primary: Codex — NO-GO, persisted by team-lead session 76c1a10a-1a00-4555-84ed-ab33c6d71fec
**Model:** codex
Full Codex output (incl. carry-over table assessment): `review-codex.md`. Second reader (sonnet, COMMENT): `review-second.md`. Done check: `done-check.md` Check 1 — PASS.
Run record: verify-run complete `mvl2bnlv-b7a109e5` — pass.

### [HIGH] Superseded vote requests still overwrite confirmed state
`web/reflekt-web/src/stores/vote.ts:70` — cast then retract quickly: if the retract response arrives first it removes both pending records, yet the delayed cast response restores its stale count; earlier failures also undo removed changes because `undoVote` applies deltas even when its record is absent. Fix: track settlement for totals and personal counts; ignore superseded confirmations and rollbacks.

### [MEDIUM] Overlapping failed readiness toggles leave incorrect readiness
`web/reflekt-web/src/stores/readiness.ts:60` — two toggles in flight, both fail in request order: second rollback restores captured `wasReady: true`, count off by one. Fix: reconcile pending toggles as an ordered chain so failures restore authoritative state.

### [MEDIUM] A failed delete can restore a failed edit
`web/reflekt-web/src/stores/card.ts:32` — edit then delete while edit pending; edit fails first, rollback finds no visible card; delete then fails and restores the snapshot with the unsuccessful edit. Fix: apply edit reconciliation/rollback to the card retained in `pendingDeletes` too.

### [MEDIUM] Team-limit tooltip not reachable by keyboard
`web/reflekt-web/src/views/TeamDashboardView.vue:33` — at ten teams the only focus target is disabled, so `focus-within` cannot reveal the explanation. Fix: focusable, described wrapper while the button stays disabled.

### [LOW] Unlisted observable changes
`composables/useRetroPhase.ts:46`, `components/teams/TeamCard.vue:2`, `components/retro/TimerDisplay.vue:8` — advance-dialog warning reset, changed frame radii, info-icon clicks losing navigation, narrower timer hit area exceed the spec's exhaustive visible-change list. Fix: preserve previous transitions, radii, navigation and hit area while keeping the refactor and a11y work.

*Status: COMPLETE — Codex primary, persisted by team-lead, 2026-10-23*

## Step 2 — Fix list, cycle 1

Bar: round 1 — every item. Sources: Codex primary (C), second reader (S). Done check: PASS, no items.

**From Codex (primary)**
1. [HIGH] C1 — `stores/vote.ts:70` superseded vote confirmations/rollbacks overwrite confirmed state; `undoVote` applies deltas with no record. Track settlement for totals and personal counts; ignore superseded confirmations and rollbacks.
2. [MEDIUM] C2 — `stores/readiness.ts:60` overlapping failed toggles leave wrong readiness/count. Reconcile so a failure restores the authoritative state regardless of completion order.
3. [MEDIUM] C3 — `stores/card.ts:32` failed delete restores a failed edit. Reconcile/roll back edits on the card held in `pendingDeletes` too.
4. [MEDIUM] C4 — `views/TeamDashboardView.vue:33` disabled Create Team tooltip unreachable by keyboard. Focusable described wrapper; button stays disabled.
5. [LOW] C5 — unlisted observable changes: restore HEAD's behaviour for the advance-dialog warning reset (`useRetroPhase.ts:46`), the team/sprint frame radii (`TeamCard.vue:2` and the BaseCard frames), info-icon click navigation, and the timer hit area (`TimerDisplay.vue:8`). Spec's visible-change list is exhaustive; owner rule: no new product behaviour. Keep the refactor and a11y attributes.

**From the second reader**
6. [LOW] S1 — `AppHeader.vue:46-59` (and `TeamCard.vue`): `role="menu"` with a non-menuitem child and no arrow-key handling. Either implement the menu keyboard pattern or drop `role="menu"`/`menuitem` for a plain disclosure list (preferred — smallest).
7. [LOW] S2 — colour props typed `string` in `RetroGroupItem`, `RetroCardItem`, `RetroMobileTabBar`, `ReportSection`, `EvidenceChip`; type them `ColumnColor`.
8. [LOW] S3 — `RetroGroupItem.vue` (301), `CreateSprintForm.vue` (281), `RetroCardItem.vue` (222) exceed the 200-line split rule; split by vue-component-architecture with no behaviour change.
9. [LOW] S4 — `useTimerActions` toasts from inside a composable (frontend-review checklist) — the plan asked for this; no change. Record in implementation.md that it stays as planned.
10. [LOW] S5 — `RetroColumnPanel` colour swatches carry `aria-pressed` but are mouse-only (tabbing blurs input and closes the editor). Make them keyboard-operable without closing the editor.

Follow-up review: due — row 1 is HIGH. Covers: the round's changes; steps built or redone: none.

Under the bar: none (round 1 carries every item).

## Step 2 — Re-review (cycle 1)

**Verdict: COMMENT**

Primary: Codex — NO-GO, persisted by team-lead session 76c1a10a-1a00-4555-84ed-ab33c6d71fec
**Model:** codex
Rows 2–10 closed; row 1 partial. Carry-over table re-assessed (`review-codex.md`, cycle-1 section). Run record: fix developer's fast record `mvl34nrt-333c2a41` — pass.

### [MEDIUM] Settled personal vote counts can still be overwritten
`web/reflekt-web/src/stores/vote.ts:82` — delay a successful vote in sprint A, enter sprint B and load its votes: the snapshot marks pending personal counts settled (`vote.ts:113`), but `confirmVote` ignores `change.ownSettled`, so A's late response inserts its group into B's `myVotes` and reduces B's remaining budget. Fix: honour `ownSettled` before confirming personal counts, as the total branch honours `totalSettled`.

*Status: COMPLETE — Codex primary, persisted by team-lead, 2026-10-23*

## Step 2 — Fix list, cycle 2

Bar: round 2 — MEDIUM and up.

**From Codex (re-review, cycle 1)**
1. [MEDIUM] `stores/vote.ts:82` — `confirmVote` ignores `change.ownSettled`; a late vote response from another sprint corrupts `myVotes`. Honour `ownSettled` before confirming personal counts.

Follow-up review: none due — no row reaches HIGH.

Under the bar: none.
