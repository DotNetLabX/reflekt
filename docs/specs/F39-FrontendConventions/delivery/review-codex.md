# F39-FrontendConventions — Codex code review (primary)

Persisted by team-lead session 76c1a10a-1a00-4555-84ed-ab33c6d71fec from Codex job task-mvl2d7io-0bvg2z (label "F39-FrontendConventions code-review round 1"), 2026-10-23. Read-only job; model: Codex CLI default.

## Verdict: NO-GO

1. **HIGH — [stores/vote.ts:70](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/stores/vote.ts:70): Superseded vote requests still overwrite confirmed state.** Cast, then retract quickly: if the retract response arrives first, it removes both pending records. The delayed cast response nevertheless restores its stale count. Earlier failures also undo removed changes because `undoVote` applies deltas even when its record is absent. **Suggested fix:** track settlement for both totals and personal counts, and ignore superseded confirmations and rollbacks.

2. **MEDIUM — [stores/readiness.ts:60](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/stores/readiness.ts:60): Overlapping failed toggles leave incorrect readiness.** Starting unready, toggle twice before either request completes. If both fail in request order without an authoritative event, the second rollback restores its captured `wasReady: true`; the count also becomes one. **Suggested fix:** reconcile pending toggles as an ordered chain so failures restore the authoritative state regardless of completion order.

3. **MEDIUM — [stores/card.ts:32](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/stores/card.ts:32): A failed delete can restore a failed edit.** Edit a card, then delete it while the edit is pending. If the edit fails first, its rollback finds no visible card and discards the change. If deletion subsequently fails, it restores the snapshot containing the unsuccessful edit. **Suggested fix:** apply edit reconciliation and rollback to the card retained in `pendingDeletes`, as well as visible cards.

4. **MEDIUM — [views/TeamDashboardView.vue:33](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/views/TeamDashboardView.vue:33): The team-limit tooltip remains inaccessible by keyboard.** At ten teams, its only focus target is disabled, so `focus-within` cannot reveal the explanation. This contradicts the explicit tooltip-focus requirement. **Suggested fix:** provide a focusable, described wrapper when disabled while retaining the button’s disabled behavior.

5. **LOW — [useRetroPhase.ts:46](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/composables/useRetroPhase.ts:46), [TeamCard.vue:2](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/teams/TeamCard.vue:2), [TimerDisplay.vue:8](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/TimerDisplay.vue:8): Unlisted observable changes remain.** The warning reset, changed frame radii, info-icon clicks losing navigation, and narrower timer hit area exceed the specification’s exhaustive visible-change list. **Suggested fix:** preserve the previous state transitions, radii, navigation, and hit area while retaining the refactoring and accessibility improvements.

Every Carry-Over Findings row was assessed, in its original order:

| Carry-over row | Assessment |
|---|---|
| Advance-dialog warning reset | **Confirmed.** Included in finding 5. |
| Assignee disabled during save | **Confirmed.** Consistent with Step 8’s disabled-input requirement. |
| Two report-view `h1`s | **Refuted as outstanding.** Resolved: the room passes `title-tag="h2"`. |
| Unhandled readiness/clipboard rejection | **Confirmed.** Both also exist in HEAD; no new finding. |
| Token shade shifts | **Confirmed.** Follows the required semantic-token replacements. |
| Back-link arrow replacement | **Confirmed.** Authorized by the specification’s unicode-arrow decision. |
| BaseCard corner changes | **Confirmed.** Included in finding 5. |
| Kick becomes a small red button | **Confirmed.** Authorized BaseButton adoption. |
| Second create-team form lacks Cancel | **Confirmed.** Also absent in HEAD. |
| Plain controls outside Base* | **Confirmed.** Menu controls, swatches, and the preserved room controls do not establish another defect. |
| Info icons no longer open the team | **Confirmed.** Included in finding 5. |
| Timer click target narrowed | **Confirmed.** Included in finding 5. |
| Disabled Create Team tooltip | **Confirmed.** Finding 4; a focusable wrapper preserves disabled behavior. |
| Desktop panels retain tabpanel roles | **Confirmed markup.** No additional blocking defect established by source inspection. |
| Seven calls now use `include` | **Confirmed.** Explicitly authorized; requests remain same-origin. |
| AC9 browser walkthrough owed | **Confirmed.** Acceptance remains open under operator-owned Step 16. |
| Graph update omitted | **Confirmed.** Recorded omission; `graphify` is unavailable here. |
| Unsanctioned git staging | **Resolution confirmed:** the audit records unstaging, and the current index is empty. The original command is reported in the implementation record. |

The recorded complete run `mvl2bnlv-b7a109e5` passed, and its frontend hashes match the current tree. Package manifests are unchanged. No files were modified and no suite was rerun.


---

# Re-review after fixes, cycle 1 — Codex job task-mvl367nx-talsj3, persisted by team-lead 2026-10-23

## Verdict: NO-GO

**MEDIUM — [stores/vote.ts:82](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/stores/vote.ts:82): Settled personal vote counts can still be overwritten.** Delay a successful vote response in sprint A, then enter sprint B and finish loading its votes. The snapshot [marks pending personal counts settled](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/stores/vote.ts:113), but `confirmVote` ignores `change.ownSettled`. A’s delayed response inserts its group into B’s `myVotes`, incorrectly reducing B’s remaining budget. **Suggested fix:** honor `ownSettled` before confirming personal counts, as the total-count branch honors `totalSettled`.

Cycle-1 fix-list verification:

| Row | Status | Evidence |
|---|---|---|
| 1 — Vote settlement | **partial** | Superseded-request guards work; personal-count settlement remains incomplete at [vote.ts:82](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/stores/vote.ts:82). |
| 2 — Failed readiness toggles | **closed** | Flag and count rollback use ordered chains: [readiness.ts:40](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/stores/readiness.ts:40), [pendingValues.ts:44](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/stores/pendingValues.ts:44). |
| 3 — Failed edit followed by failed delete | **closed** | Edit reconciliation updates the retained deletion snapshot: [card.ts:36](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/stores/card.ts:36). |
| 4 — Team-limit tooltip | **closed** | Disabled button has a focusable, described wrapper: [TeamDashboardView.vue:32](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/views/TeamDashboardView.vue:32). |
| 5 — Unlisted behavior changes | **closed** | Warning flag carried at [useRetroPhase.ts:45](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/composables/useRetroPhase.ts:45); original radii selected in [CreateTeamForm.vue:2](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/teams/CreateTeamForm.vue:2), [SprintList.vue:2](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/sprints/SprintList.vue:2), [SprintDetailPanel.vue:2](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/sprints/SprintDetailPanel.vue:2), and [TeamCard.vue:4](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/teams/TeamCard.vue:4). Navigation bubbles through [TeamCard.vue:6](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/teams/TeamCard.vue:6); padded timer target restored at [TimerDisplay.vue:7](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/TimerDisplay.vue:7). |
| 6 — Menu semantics | **closed** | Plain disclosures retain expanded state: [AppHeader.vue:43](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/AppHeader.vue:43), [TeamCard.vue:17](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/teams/TeamCard.vue:17). |
| 7 — Color types | **closed** | `ColumnColor` used in [RetroGroupItem.vue:99](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/RetroGroupItem.vue:99), [RetroCardItem.vue:83](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/RetroCardItem.vue:83), [RetroMobileTabBar.vue:32](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/RetroMobileTabBar.vue:32), [ReportSection.vue:48](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/reports/ReportSection.vue:48), and [EvidenceChip.vue:20](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/reports/EvidenceChip.vue:20). |
| 8 — Component sizes | **closed** | Components now end at [RetroGroupItem.vue:193](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/RetroGroupItem.vue:193), [CreateSprintForm.vue:181](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/sprints/CreateSprintForm.vue:181), and [RetroCardItem.vue:123](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/RetroCardItem.vue:123). Extracted components also remain below 200 lines. |
| 9 — Timer composable toasts | **closed** | Required no-change disposition recorded: [implementation.md:460](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/delivery/implementation.md:460). |
| 10 — Keyboard swatches | **closed** | Focus remains within the editor; keyboard-generated clicks select colors: [RetroColumnPanel.vue:45](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/RetroColumnPanel.vue:45), [RetroColumnPanel.vue:135](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/RetroColumnPanel.vue:135). |

Carry-over table assessment, in its original order. “Closed” includes accepted or pre-existing behavior requiring no F39 fix.

| Carry-over row | Disposition and evidence |
|---|---|
| Advance-dialog warning reset | **closed — refuted now:** previous warning persists outside Writing. [useRetroPhase.ts:45](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/composables/useRetroPhase.ts:45) |
| Assignee disabled during save | **closed — confirmed, authorized:** [RetroActionItemInput.vue:13](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/RetroActionItemInput.vue:13). |
| Two report-view `h1`s | **closed — refuted as outstanding:** room report uses `h2`. [RetroReportPanel.vue:13](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/RetroReportPanel.vue:13) |
| Unhandled readiness/clipboard failures | **closed — confirmed, pre-existing:** [useReadiness.ts:9](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/composables/useReadiness.ts:9), [useCopyReport.ts:9](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/composables/useCopyReport.ts:9). |
| Token shade shifts | **closed — confirmed, authorized:** [main.css:181](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/assets/main.css:181). |
| Back-link arrow replacement | **closed — confirmed, authorized:** [SprintDetailPanel.vue:8](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/sprints/SprintDetailPanel.vue:8). |
| BaseCard corner changes | **closed — refuted now:** original frame radii restored; see fix row 5 and [BaseCard.vue:33](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/ui/BaseCard.vue:33). |
| Small red Kick button | **closed — confirmed, authorized:** [SprintDetailPanel.vue:67](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/sprints/SprintDetailPanel.vue:67). |
| Second create-team form lacks Cancel | **closed — confirmed, pre-existing:** [TeamDashboardView.vue:47](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/views/TeamDashboardView.vue:47). |
| Plain controls outside Base* | **closed — confirmed, within retained exceptions:** [TeamCard.vue:12](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/teams/TeamCard.vue:12), [SprintColumnConfigRow.vue:12](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/sprints/SprintColumnConfigRow.vue:12), [PreviousActionsPanel.vue:50](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/PreviousActionsPanel.vue:50). |
| Info icons lose navigation | **closed — refuted now:** clicks bubble to the card handler. [TeamCard.vue:6](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/teams/TeamCard.vue:6) |
| Narrower timer target | **closed — refuted now:** wrapper handles clicks. [TimerDisplay.vue:7](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/TimerDisplay.vue:7) |
| Disabled Create Team tooltip hover-only | **closed — refuted now:** wrapper accepts keyboard focus. [TeamDashboardView.vue:34](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/views/TeamDashboardView.vue:34) |
| Desktop `tabpanel` roles | **closed — confirmed markup; no additional defect established:** [RetroColumnPanel.vue:4](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/components/retro/RetroColumnPanel.vue:4). |
| Seven calls gain `include` credentials | **closed — confirmed, authorized same-origin requests:** [client.ts:56](D:/src/reflekt-F39-FrontendConventions/web/reflekt-web/src/api/client.ts:56). |
| AC9 browser walkthrough owed | **not closed — confirmed, operator-owned:** [implementation.md:395](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/delivery/implementation.md:395). |
| Graph update omitted | **not closed — confirmed recorded omission:** [implementation.md:436](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/delivery/implementation.md:436). |
| Unsanctioned staging | **closed — historical action documented; current index verified empty:** [implementation.md:519](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/delivery/implementation.md:519). |

No additional findings introduced by the fixes were identified.

No files modified or suite rerun. Package manifests are unchanged. The supplied fast run covers backend tests; frontend type-check/build results remain developer-reported.
