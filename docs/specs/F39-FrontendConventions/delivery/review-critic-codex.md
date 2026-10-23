# F39-FrontendConventions — spec review, round 1 — Codex (primary)

**Model:** codex
Persisted by the architect session from the read-only Codex job, 2026-10-22.

## Verdict: NO-GO

1. **HIGH — [spec.md:26](docs/specs/F39-FrontendConventions/definition/spec.md:26): Report unification has unresolved content differences.** FE-24 prescribes using `SprintReportPanel`, but the current in-room report displays discussed badges and every card’s text; the standalone renderer omits those badges and hides card text for single-card groups. See [RetroReportPanel.vue:35](web/reflekt-web/src/components/retro/RetroReportPanel.vue:35) and [ReportSection.vue:14](web/reflekt-web/src/components/reports/ReportSection.vue:14). Following the suggested fix can therefore remove visible information beyond the sole approved change—adding previous actions. Specify which rendering differences must survive or explicitly approve their changes.

2. **HIGH — [spec.md:60](docs/specs/F39-FrontendConventions/definition/spec.md:60): Acceptance never exercises optimistic rollback or reconciliation.** The walkthrough requires no failed request, delayed response, overlapping mutation, or second client. These are central to FE-07: optimistic creates must reconcile temporary IDs with `CardCreated` echoes, and rollback must preserve unrelated updates. The referenced [vote.ts:66](web/reflekt-web/src/stores/vote.ts:66) simply restores snapshot counts, which can overwrite intervening broadcasts. Require recorded failure and response/echo-order checks, including preservation of F36’s draft-restoration behavior. Manual checks satisfy the prohibition on adding frontend tests.

3. **MEDIUM — [spec.md:22](docs/specs/F39-FrontendConventions/definition/spec.md:22): Error mapping assumes more than F38 guarantees.** [F38’s contract](docs/specs/F38-BackendConventions/definition/spec.md:22) makes `code` optional. A helper described only as “keyed on `code`” does not define handling for uncoded validation errors, unknown codes, network failures, or context-specific messages. Existing join and report flows distinguish failures using status and operation context. Define fallback precedence and the error fields retained by `ApiError`, then include representative uncoded failures in acceptance.

4. **MEDIUM — [spec.md:22](docs/specs/F39-FrontendConventions/definition/spec.md:22): The JSON client contract omits successful empty responses.** Logout, card deletion, team deletion, evidence detachment, group dissolution, and kicking currently return 204. For example, [DeleteCardEndpoint.cs:72](src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs:72) sends no content. An unconditional JSON parser would report failure after a successful mutation and could trigger an incorrect optimistic rollback. Specify empty-response handling and verify a migrated 204 operation.

5. **MEDIUM — [spec.md:60](docs/specs/F39-FrontendConventions/definition/spec.md:60): Preservation checks cover credentials incompletely and leave role behavior unverified.** The invariant includes paths, methods, payloads, headers, and role permissions, but the required record checks only credentials. Today, Google join and kick omit the guest header, while participant reads and card operations have different header rules. Require a per-operation request comparison and named owner, guest, and Google-participant checks. Include reload using existing guest-token, theme, and mute storage keys; a fresh join does not prove persisted sessions survived the storage refactor.

6. **MEDIUM — [spec.md:28](docs/specs/F39-FrontendConventions/definition/spec.md:28): Accessibility acceptance does not prove the requested outcome.** Neither lint configuration nor the walkthrough requires keyboard verification. Adding tab roles alone would satisfy a superficial interpretation while leaving keyboard navigation absent. Define checks for modal initial focus, focus containment and restoration, Escape, mobile-tab keyboard operation and panel association, keyboard-accessible tooltips, and toast announcements. The existing [BaseModal.vue](web/reflekt-web/src/components/ui/BaseModal.vue:4) and [RetroMobileTabBar.vue](web/reflekt-web/src/components/retro/RetroMobileTabBar.vue:2) need those behaviors verified explicitly.

7. **MEDIUM — [spec.md:54](docs/specs/F39-FrontendConventions/definition/spec.md:54): The fetch acceptance command rejects legitimate store methods.** It matches both `async fetch()` in [policy.ts:13](web/reflekt-web/src/stores/policy.ts:13) and `policyStore.fetch()` in [useRetroRoom.ts:84](web/reflekt-web/src/composables/useRetroRoom.ts:84). Both can remain after all network requests move into `api/`. Replace the substring check with a check that identifies network-fetch usage without rejecting unrelated method names.

8. **MEDIUM — [spec.md:55](docs/specs/F39-FrontendConventions/definition/spec.md:55): Structural acceptance leaves major required outcomes unchecked.** All listed checks could pass while direct store writes remain in composables, SignalR handlers still contain mutation logic, or the new unions and `PHASES` constant remain unused. None verifies shared-report adoption, lazy routes, or in-flight `fetchMe` deduplication either. Require explicit structural-review evidence or targeted lint rules, plus browser-network evidence for auth-request deduplication.

9. **LOW — [spec.md:14](docs/specs/F39-FrontendConventions/definition/spec.md:14) and [spec.md:26](docs/specs/F39-FrontendConventions/definition/spec.md:26): Two inherited counts are incorrect.** There are **42 network `fetch()` calls**; the 44 text matches include the policy method declaration and invocation above. `RetroPhaseHeader` declares **20 props**, not 19, at [RetroPhaseHeader.vue:83](web/reflekt-web/src/components/retro/RetroPhaseHeader.vue:83). The claims of 13 Options stores and 24 guest-token header sites are accurate.

No files were modified. Build/full verification was not run because those commands write generated files.

---

# Round 2 — 2026-10-23 — Codex (primary, alone)

## Verdict: NO-GO

Lines refer to `docs/specs/F39-FrontendConventions/definition/spec.md`.

| Round-1 id | Verification | Line | Collateral |
|---|---|---|---|
| R1-01 | closed | 25, 38, 69 | none |
| R1-02 | closed | 27 | none |
| R1-03 | closed | 23, 61 | none |
| R1-04 | closed | 23 | none |
| R1-05 | closed | 23, 68 | none |
| R1-06 | partial — exhaustive visible-change list omits kick’s replacement dialog | 29, 37, 52 | R2-01 |
| R1-07 | closed | 51, 65 | none |
| R1-08 | partial — request sampling omits payloads and per-operation coverage; hub handlers, type usage and shared-report adoption remain unverified | 64, 70 | none |
| R1-09 | closed | 29, 71 | none |
| R1-10 | closed | 24, 59 | none |
| R1-11 | closed | 15, 27 | none |
| R1-12 | closed | 31, 66 | none |
| R1-13 | closed | 66 | none |

**R2-01 — MEDIUM — [spec.md:37](docs/specs/F39-FrontendConventions/definition/spec.md:37) — Tag: `fold`.** The new “Visible changes, the whole list” excludes replacing kick’s native confirmation with `BaseModal`, explicitly required at line 29. Line 52 permits appearance changes only through that Decisions list. Implementing the required dialog therefore violates the retained appearance guarantee. Add the dialog replacement to the exceptions.

No additional MEDIUM+ sibling-spec contradictions or weakened guarantees were found in the checked text.


# Plan review — round 1 — 2026-10-23 — Codex job task-mvkn4ge3-8xt9q4 (persisted by team-lead session 86f5019d)

## Verdict: NO-GO

All findings refer to `docs/specs/F39-FrontendConventions/delivery/plan.md`.

1. **HIGH — plan.md:151 — Vote rollback can erase another participant’s update.** Hub vote events replace the total with an absolute `newCount`. Starting at 0, a local optimistic vote produces 1; another participant’s successful vote sets the total to 1; subtracting the failed local vote then incorrectly produces 0. This violates spec Scope row 3’s preservation requirement. **Suggested fix:** track whether the optimistic delta remains applied after an authoritative update; reconcile without subtracting it from a replaced total. Record this interleaving in the walkthrough.

2. **HIGH — plan.md:148 — Failed deletion can resurrect a server-deleted card.** “Re-inserts … if it is absent” also applies when another browser successfully deleted that card during the pending request. Absence alone cannot establish ownership of the removal. **Suggested fix:** invalidate the pending rollback when a matching authoritative deletion arrives, using operation/version tracking. Apply equivalent protection to link removal.

3. **HIGH — plan.md:136 — The proposed hub handlers contradict the amended spec.** `ParticipantLeft` expressly has no store action, while `ParticipantKicked` performs a store call plus `onKicked`; one statement does not make that a single store-action call. Spec Scope row 3 and AC9 require every handler to delegate to one store action. Decision P5 acknowledges the contradiction but does not amend the spec. **Suggested fix:** delegate `ParticipantLeft` to a no-op action and handle kick UI effects outside the handler through an observed store outcome, or obtain an explicit spec amendment.

4. **HIGH — plan.md:174 — A dependency crosses build slices in the wrong direction.** Step 10 imports timer defaults from `src/constants/columns.ts`, but Step 13 first creates that module and does not list timer defaults among its exports. Step 10 belongs to slice 8–11; Step 13 belongs to slice 12–15. **Suggested fix:** create the shared constants, including timer defaults, before Step 10. Also explicitly assign consumer updates when Step 5 removes timer action names and Step 7 removes the `reportStore` return still consumed by `RetroRoomView`.

5. **MEDIUM — plan.md:136 — Unchanged connection bodies conflict with mandatory assertion removal.** Current `useSprintHub.connect` contains `connection!` twice. Keeping its body unchanged leaves violations of spec FE-17 and Step 1’s mandatory lint rule; Step 15 forbids suppressing that rule. **Suggested fix:** permit and schedule guards or safe local narrowing inside `connect`, while preserving connection ownership, sequencing and lifecycle behavior.

6. **MEDIUM — plan.md:106 — Step 4’s acceptance gate cannot pass at that stage.** It requires `X-Guest-Token` to occur only in the client, although stores retain their header construction until Steps 5–6 and `PreviousActionsPanel` retains it until Step 8. The first slice therefore ends with an impossible gate. **Suggested fix:** restrict Step 4’s check to the new API layer and schedule the whole-source assertion after Step 8.

7. **MEDIUM — plan.md:161 — The board action boundary has no delivering step.** FE-23’s suggested fix requires a typed `InjectionKey` for board actions. Step 9 claims to follow `vue-component-architecture` but leaves the existing large `RetroGroupItem` prop/event interface without an injection or split task. That pattern requires action injection for three or more handlers. **Suggested fix:** schedule the typed board action provider and consumers, and resolve the excessive props, or document a justified `Departs from` row.

8. **MEDIUM — plan.md:203 — Base* adoption extends appearance changes beyond the amended Decisions list.** Including `RetroGroupItem` changes its compact rename controls to Base* styling outside the named teams, sprints, welcome and report screens. Line 204 additionally replaces error presentations with `EmptyState`, while Decisions authorizes empty-state presentation changes. Q2 did not authorize these extensions. **Suggested fix:** preserve the existing appearance at those sites, or explicitly source the additions and amend the spec’s visible-changes list; an `Adds:` row alone cannot override that invariant.

9. **MEDIUM — plan.md:257 — The verification does not establish preservation of F36’s outcomes.** F36 AC1–AC5 are structural greps. Steps 15–16 schedule F39 AC9, which does not explicitly cover F36’s chime unlock, cold-link team names, permission restrictions, or foreground/background failure and retry behavior. The referenced “walkthrough Roles” is not actually scheduled. **Suggested fix:** include the relevant F36 manual scenarios and record their observed results, including interrupted loads and background refresh failures. No new tests are needed.

10. **LOW — plan.md:115 — An unused timer getter is deliberately retained.** Requiring the formatter to be used by the store preserves `timer.formattedTime`, which has no consumer. Step 116 removes only the other two unused timer getters. This leaves the unused-member requirement in FE-25/FE-27 incomplete. **Suggested fix:** delete `timer.formattedTime`; share the formatter between the actual consumers.

11. **LOW — plan.md:207 — The palette grep cannot prove deduplication.** Correct imports and template references to the shared `PALETTE_COLORS`/`paletteColors` still match, while duplicated arrays renamed to another identifier pass. **Suggested fix:** check palette declarations and inspect consumer imports rather than banning references. Also distinguish intermediate acceptance expectations from final checks when Step 15 reruns them: Step 5’s expected `CreateSprintForm` hit and Step 6’s expected `PreviousActionsPanel` hit should both be absent by then.


# Plan review — round 2 — 2026-10-23 — Codex job task-mvknyo3g-ihluw8 (primary, alone; persisted by team-lead session 86f5019d)

## Verdict: NO-GO

Full read completed: plan, amended spec, questions, both prior reviews, F34 findings, and all 65 source files. No files modified; no builds or tests run.

Below, **P** means [delivery/plan.md](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/delivery/plan.md), **S** means [definition/spec.md](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/definition/spec.md). “Closed” verifies the original finding; related remaining problems appear under collateral.

| R1-xx | Status | Line | Collateral |
|---|---|---|---|
| R1-01 | closed | P:150,154,157,295 | Personal vote rollback remains unsafe: R2-02. |
| R1-02 | closed | P:150–151,157,295 | New settlement rule mishandles partial events: R2-01. |
| R1-03 | closed | P:137,286,343; S:8,26,72 | Spec exception and kick delegation now agree. |
| R1-04 | closed | P:93,116,139,178 | Ordering fixed; timer-value preservation remains unclear: R2-03. |
| R1-05 | partial | P:137,141,168,210; S:27 | Script sites assigned; two template assertions omitted: R2-04. |
| R1-06 | closed | P:107,157 | Intermediate and whole-source gates separated. |
| R1-07 | closed | P:167–170 | Typed action provider and consumers scheduled. |
| R1-08 | closed | P:207–211,297; S:38 | Named sites fixed; new guarantee conflicts with timer controls: R2-05. |
| R1-09 | closed | P:232,240,261–262 | F36 scenarios explicitly scheduled. |
| R1-10 | closed | P:150,152,157 | Both focus flags restored; broad settlement still affects correctness: R2-01. |
| R1-11 | closed | P:188,210–211,296 | Three existing date outputs preserved. |
| R1-12 | closed | P:115,117,209 | Getter, class handling and redundant attributes addressed. |
| R1-13 | closed | P:61,94,229 | Declaration check and final-state expectations corrected. |
| R1-14 | closed | P:140 | Step 7 moves existing logic; Step 8 rewrites it. |
| R1-15 | closed | P:13 | Existing convention file correctly acknowledged. |
| R1-16 | closed | P:288,353 | `Adds:` present; Q4 remains unresolved at LOW. |
| R1-17 | closed | P:127 | Status-zero fallback explicitly preserves “network error”. |
| R1-18 | not closed | P:29–47 | Explicitly waived at LOW in prior ledger:156; below floor. |
| R1-19 | closed | P:61,142,189,229 | Harness, formatting routine and grep limitations specified. |

The 41 surviving API operations and 23 token-bearing calls match the source. No remaining reversed dependency across the build slices was found.

1. **R2-01 — HIGH — `fold` — [plan.md:150](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/delivery/plan.md:150): Partial events cannot settle an entire optimistic item.**  
   The new rule treats any authoritative information about the item as settlement. Yet `EvidenceLinkFetched` supplies one link, and group events supply individual fields. For example, while removal of link A is pending, metadata for link B on the same card arrives. Line 151 explicitly treats that event as settlement; if removing A then fails, A stays missing although the server retained it. Similarly, link metadata cannot establish whether an optimistic card-text edit succeeded.

   **Suggested fix:** Track settlement per operation and affected field/link ID. Settle only values the authoritative payload actually replaces or outcomes it establishes. Record a failed mutation interleaved with an unrelated partial event.

2. **R2-02 — HIGH — `round-1 miss: overlapping personal votes` — [plan.md:154](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/delivery/plan.md:154): Keeping personal vote rollback “as today” still overwrites successful work.**  
   Today’s rollback restores `prevCount` in `stores/vote.ts:122–126`. Starting with one personal vote, a pending retract changes it to zero; another click casts again and succeeds before the retract fails. The successful response can establish two personal votes, but the older failure restores one. The corrected group total remains authoritative while `myVotes` and the remaining budget become wrong. The controls permit this overlapping retract/cast sequence.

   **Suggested fix:** Make personal-count reconciliation operation-aware as well as group-total reconciliation. An older failure must preserve a newer authoritative personal count. Add this sequence to the recorded walkthrough.

3. **R2-03 — MEDIUM — `round-1 miss: per-sprint timer settings` — [plan.md:178](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/delivery/plan.md:178): Constants are specified as the replacement for configured timer values.**  
   The existing props carry `currentSummary.writingTimerDefault`, `votingTimerDefault` and `discussingTimerDefault`, with fallback constants (`RetroRoomView.vue:46–48`). These are persisted sprint settings. Step 10 instead directs the default timers to come from the constants module, without explicitly preserving the summary values. Following that instruction literally makes a sprint configured for 12 minutes display the shared five-minute default when the timer is stopped (`TimerDisplay.vue:20,95–105`).

   **Suggested fix:** Read configured values from the sprint summary through the store/composable; use shared constants only as fallbacks. Verify non-default timer settings.

4. **R2-04 — MEDIUM — `fold` — [plan.md:141](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/delivery/plan.md:141): The assertion inventory covers scripts only.**  
   There are **20** non-null assertions: 18 in scripts and two in templates. The fold assigns the 18 script sites but omits `authStore.user!.email` in `components/AppHeader.vue:43` and `group.cards[0]!.evidenceLinks` in `components/retro/RetroGroupItem.vue:25`. Consequently, completing the assigned cleanup can still violate S:27’s requirement that non-null assertions become guards.

   **Suggested fix:** Assign both template sites and verify assertions across scripts and template expressions, independently of the script lint count.

5. **R2-05 — MEDIUM — `fold` — [plan.md:297](D:/src/reflekt-F39-FrontendConventions/docs/specs/F39-FrontendConventions/delivery/plan.md:297): The new appearance guarantee contradicts Step 12.**  
   Decision 18 says Base* adoption stops at the four named screens, preserving room controls. However, P:197 still requires `TimerControls.vue` to adopt `BaseButton size="sm"`. Existing timer buttons use `px-3 py-1`, and add-time buttons use `px-1 py-1`; BaseButton’s small size uses `px-2.5 py-1.5`, with additional variant styling. That changes room controls outside the amended visible-change list.

   **Suggested fix:** Preserve their existing appearance while replacing `@apply` with utilities, explicitly narrowing FE-34’s recommendation. Alternatively, amend the visible-change contract to name the timer-control changes.

**LOW, briefly:**

- **`round-1 miss: DTO inventory` — P:92:** The “four create-sprint payload copies” baseline is stale after F36; the payload interface remains in the sprint store. Update the inventory.
- **`round-1 miss: questions ledger` — P:357:** “None” conflicts with `questions.md:72`, where Q4 remains Open, and P:353’s departure-review obligation. Reconcile the status.
