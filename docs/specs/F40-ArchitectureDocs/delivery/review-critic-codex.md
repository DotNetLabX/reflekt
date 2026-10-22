# F40-ArchitectureDocs — spec review, round 1 — Codex (primary)

**Model:** codex
Persisted by the architect session from the read-only Codex job, 2026-10-22.

## Verdict: NO-GO

File reviewed: [docs/specs/F40-ArchitectureDocs/definition/spec.md](docs/specs/F40-ArchitectureDocs/definition/spec.md).

1. **High — spec.md:27 — Vue convention contradicts F36.** `docs/conventions/vue.md:25` mandates an inline dark background on `<body>`. F36 explicitly removes that background because it breaks light mode (`F36…/definition/spec.md:24`). Neither feature explicitly owns correcting this convention, so agents would remain instructed to recreate the defect. Add `vue.md` to F40’s required edits.

2. **High — spec.md:27 — C# convention contradicts F38’s chosen error handling.** `docs/conventions/csharp.md:73` says validation failures and “not found” are results, not exceptions; line 12 prescribes exceptions. F38 expressly adopts exceptions for these outcomes. F40’s conventions scope leaves this conflicting guidance intact. Reconcile both sections with F38.

3. **Medium — spec.md:25,30 — Historical marking misses the stale backlog claims.** Marking only section A historical leaves the “Already agreed” preamble asserting four aggregates and `CardVote` inside `Card` (`docs/backlog.md:6`). Section B separately claims allowlist enforcement at lines 48 and 56. These contradict the seven live aggregates and open Google sign-in. Correct those claims or explicitly include their sections in the historical boundary.

4. **Medium — spec.md:59 — Architecture acceptance proves names, not architectural accuracy.** Listing every project, aggregate and slice would also pass for a document copied from the old audit inventory. That inventory describes a singleton interceptor and static hub readiness state; F35 requires a scoped interceptor and F38 requires `ReadinessTracker`. Require recorded verification against the final source revision for the requested sections, including routes, auth, event dispatch, persistence and frontend behavior.

5. **Medium — spec.md:56 — Allowlist check misses existing prose claims.** The exact command finds only `docs/architecture/v1.md:210` today. It misses the configuration-driven allowlist and “allowlisted emails” claims in `docs/product/v1.md:531–545`, and does not scan `docs/backlog.md`. Consequently, marking the architecture document superseded can satisfy the check while incorrect sign-in guidance remains. Check prose variants and all current-state locations.

6. **Medium — spec.md:58 — Naming conventions files does not prove testing conventions are loaded.** A plain list of eight filenames satisfies acceptance while `testing.md` remains orphaned—the problem identified by CV-01. Verify the actual `@testing.md` import, retain the existing imports, and check that references resolve.

7. **Medium — spec.md:57 — Foreign-example check misses known foreign examples.** `BugRatioTarget` and `CreateDefault()` in `docs/conventions/ef-core.md:22` evade the expression. `SprintIssueSyncService` in `CLAUDE.md:146` and `project-rules.md:22` is also outside its effective coverage. The command can pass without completing CV-04/AR-32. Require review of all examples against Reflekt types, beyond this keyword check.

8. **Medium — spec.md:60 — A report date does not prove graph freshness or exclusions.** Changing `GRAPH_REPORT.md`’s date satisfies acceptance without regenerating `graph.json` or `manifest.json`. It also proves nothing about excluded directories—the current report explicitly contains `wwwroot` bundle nodes. Require generation evidence tied to the final revision and inspection of the resulting graph corpus.

9. **Medium — spec.md:61 — `git diff --stat` cannot enforce scope or preservation.** It omits staged changes and untracked files, including newly created architecture documents. Its extension rule also permits edits to protected evidence and `.claude/skills/*.md`, and cannot demonstrate preservation of guardrails or authority. Check the complete feature diff and untracked inventory against explicit allowed paths and protected files.

10. **Low — spec.md:30 — Product status remains inconsistent at the destination.** Updating `docs/product/index.md` to “Partially shipped” leaves `docs/product/v2.md:3` saying “Draft.” Update the destination too, or distinguish specification maturity from shipment status explicitly.

11. **Low — spec.md:26 — Test naming is presented more uniformly than the source supports.** Of 167 current test methods, 41 have only two naming components, including `Application_Starts` and `Login_RedirectsToGoogle`. Document `{Action}_{Condition}_{Outcome}` with the existing exceptions rather than presenting it as universal current practice.

12. **Low — spec.md:38 — The high-severity count is misattributed.** The cited architecture audit has 14 High rows overall; its primary `v1.md` locations account for ten. AR-01–03 primarily concern root instructions, and AR-26 concerns graph output. Correct the justification’s count.

13. **Low — spec.md:30 — README retains a factual metadata error.** The prescribed map link leaves `README.md:98` claiming only F31/F32 have `definition/` directories, although F35–F40 also have them. Include this correction under the promised current-state documentation refresh.

Read-only review completed; no files modified.
