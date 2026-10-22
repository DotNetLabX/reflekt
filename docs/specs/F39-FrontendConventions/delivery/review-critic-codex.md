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
