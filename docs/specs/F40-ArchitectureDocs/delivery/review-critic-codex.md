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


# Plan review — round 1 — 2026-10-23 — Codex (job task-mvl8l3zh-gt1bux), persisted by team-lead session 999a4869

## Verdict: NO-GO

All findings concern [delivery/plan.md](D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md).

- **Medium — Step 2, lines 79–81:** Installing with `--tool-path obj/dotnet-ef` does not make `dotnet ef` available automatically. Neither the executable path nor a temporary `PATH` update is specified. The global tool list is empty, no tool manifest exists, and `dotnet ef --version` fails. Specify the installed executable invocation so the required design-time verification actually runs.

- **Medium — Step 3, lines 97–108:** AR-16 and the audit’s §3.8 lack implementation coverage. The architecture sections omit OpenGraph source detection, the Jira fetch bypass, and the background-fetch pipeline. These remain implemented in `EvidenceLinkSourceDetector.cs`, `EvidenceLink.cs`, and `OpenGraphConfiguration.cs`. Add explicit documentation and acceptance coverage.

- **Medium — Step 8, lines 171–185:** TS-09 requires replacement coverage guidance for aggregate behaviour, phase/state transitions, and authorization. The plan only removes the analytics coverage line. It does not address the existing CRUD skip guidance that contradicts actual integration-test practice.

- **Medium — Step 9, line 197:** The F39 scan does not explicitly carry its recorded composable-toast exception. F39’s `implementation.md:460` preserves `useTimerActions` toasting, contrary to `vue-component-architecture`’s prohibition; the current implementation still does this. Name that accepted deviation and include fix-round records in the scan instead of permitting a misleading “none listed” result.

- **Low — Step 3, lines 95–108:** The named `create-architecture-doc` pattern is intentionally departed from, but the step has no required `Departs from:` row. The narrative and future `known-deviations.md` entry do not supply that plan row. Record the template and inventory-disposition departures with their spec authorization.

- **Low — P1, line 299:** “Handlers get `CancellationToken.None`” overstates the guarantee. The interceptor starts publication with that token, but F38’s recorded FastEndpoints probe shows handlers receive linked tokens that can be cancelled when a sibling fails. Distinguish publication-token selection from handler-token behaviour.


# Plan review — round 2 — 2026-10-23 — Codex (job task-mvl90l8e-3b0rfx), persisted by team-lead session 999a4869

## Verdict: NO-GO

| Finding | Status | Plan lines | Fold verification |
|---|---|---|---|
| R1-01 | closed | [64–66, 104–106, 118](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:64) | Fetch pipeline, packages and cross-cutting sections now have inventory inputs and acceptance checks. |
| R1-02 | closed | [203–207](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:203) | Whole F39 record scanned; both known exceptions named and candidates recorded. |
| R1-03 | closed | [81–93](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:81) | Installed tool invoked directly; version and results recorded. |
| R1-04 | closed | [185–192](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:185) | Domain coverage guidance added; CRUD-skip guidance removed. |
| R1-05 | closed | [124–127](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:124) | Both allowlist locations explicitly included within permitted annotations. |
| R1-06 | closed | [302](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:302) | Explicit departure decision and rationale for the untagged row added. |
| R1-07 | closed | [63, 102, 309](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:309) | Publication token distinguished consistently from handler tokens. |
| R1-08 | closed | [145, 165, 282](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:165) | Checks require the rule at its canonical home and remove its duplicate. |
| R1-09 | closed | [254–256, 301](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:254) | Document refresh is observed rather than assumed; ordering rationale corrected. |
| R1-10 | closed | [187, 216, 260](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:260) | Example-list producers and final consolidation assigned. |
| R1-11 | closed | [6, 162–165](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:162) | Count exclusions explained; service port duplication explicitly prohibited and checked. |

No MEDIUM-or-higher contradiction introduced by the fold was found. Two source-verification gaps remain:

1. **R2-01 — MEDIUM — [plan.md:66](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:66)** — `round-1 miss: endpoint inventory`

   Step 1 requires extracting verbs, routes and policies “from each `Configure()`”, but the source contains **44 endpoint files, 44 HTTP attributes and zero `Configure()` declarations**. For example, [CreateCardEndpoint.cs:14](/D:/src/reflekt-F40-ArchitectureDocs/src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs:14) declares its policy and route through attributes. The prescribed extraction cannot produce the required endpoint inventory, which later slices must reuse. Require extraction from `[Http*]`, `[Authorize]` and `[AllowAnonymous]`, reconciled against every endpoint class.

2. **R2-02 — MEDIUM — [plan.md:67](/D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/plan.md:67)** — `round-1 miss: real-time message inventory`

   Enumerating literal `SendAsync("…")` calls yields **24 names**, missing `CardCreated` and `CardUpdated`. Those names enter through [BroadcastCardCreatedHandler.cs:11](/D:/src/reflekt-F40-ArchitectureDocs/src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/BroadcastCardCreatedHandler.cs:11) and [BroadcastCardUpdatedHandler.cs:11](/D:/src/reflekt-F40-ArchitectureDocs/src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/BroadcastCardUpdatedHandler.cs:11); [CardBroadcast.cs:19](/D:/src/reflekt-F40-ArchitectureDocs/src/Services/Reflekt/Reflekt.API/Features/Cards/CardBroadcast.cs:19) sends the supplied variable. Both are consumed by [useSprintHub.ts:65](/D:/src/reflekt-F40-ArchitectureDocs/web/reflekt-web/src/composables/useSprintHub.ts:65). The doc could match its incomplete fact sheet while omitting two central messages. Include helper call sites and reconcile the resulting 26 names with client registrations.

Read-only review; no files modified.
