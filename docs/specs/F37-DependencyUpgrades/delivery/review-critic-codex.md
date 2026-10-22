# F37-DependencyUpgrades — spec review, round 1 — Codex (primary)

**Model:** codex
Persisted by the architect session from the read-only Codex job, 2026-10-22.

## Verdict: NO-GO

- **HIGH — [spec.md:24](/docs/specs/F37-DependencyUpgrades/definition/spec.md:24): Contradictory Microsoft package scope.** “Every `Microsoft.*` package” includes `Microsoft.NET.Test.Sdk` 17.14.1, which line 34 explicitly freezes. The inventory also double-counts `Microsoft.Data.Sqlite`: there are three central `Microsoft.EntityFrameworkCore*` entries, seven intended packages on 9.0.4, and the separate Test SDK. Enumerate the seven packages and explicitly exclude the SDK.

- **MEDIUM — [spec.md:40](/docs/specs/F37-DependencyUpgrades/definition/spec.md:40): Compilation exception contradicts the permitted diff.** This permits source changes required by upgraded packages, but acceptance 6 permits only package manifests, the lockfile, and delivery docs. A necessary compatibility edit cannot satisfy both requirements. Define whether named compatibility edits are allowed.

- **MEDIUM — [spec.md:33](/docs/specs/F37-DependencyUpgrades/definition/spec.md:33): Migration check is incomplete.** The stated command cannot select a project from the repository root. The context and migrations live in `Reflekt.Persistence`, while the startup application and Design reference live in `Reflekt.API`; there is no design-time factory. Supply the project/startup arguments, compatible tooling prerequisite, and required successful result. These arguments control which application and model the [EF CLI examines](https://learn.microsoft.com/en-us/ef/core/cli/dotnet#target-project-and-startup-project).

- **MEDIUM — [spec.md:52](/docs/specs/F37-DependencyUpgrades/definition/spec.md:52): Version acceptance does not prove DP-03.** Rejecting `9.x` also accepts `8.x`, mixed `10.0.x` patches, or deleted declarations. Vulnerability scans and passing tests do not establish the required shared latest patch. Check the explicit package inventory against the selected patch and record how that patch was determined.

- **MEDIUM — [spec.md:23](/docs/specs/F37-DependencyUpgrades/definition/spec.md:23): SQLite exception lacks sufficient evidence requirements.** “Resolved version” could mean the NuGet version or native SQLite version. DP-02 verified only the 2.1.13 Windows x64 binary; the repository deploys to Linux. A clean package audit does not establish the native engine version. Require the resolved package and native SQLite version for the application’s deployment target before omitting the pin; the [advisory’s threshold concerns SQLite itself](https://github.com/advisories/GHSA-2m69-gcr7-jv3q).

- **MEDIUM — [spec.md:54](/docs/specs/F37-DependencyUpgrades/definition/spec.md:54): The suite does not verify AngleSharp behavior.** `EvidenceLinkEndpointTests` checks attachment responses and pending status, but never verifies fetched title, description, image, or icon metadata. `OpenGraphFetcher` catches parsing failures and returns failure, so compilation and the existing suite can pass despite broken previews. Require a recorded successful metadata-fetch smoke check, consistent with DP-01’s identified coverage gap.

- **MEDIUM — [spec.md:55](/docs/specs/F37-DependencyUpgrades/definition/spec.md:55): Diff acceptance can pass despite forbidden changes.** Plain `git diff --stat` omits staged, committed, and untracked changes. After committing an implementation it can be empty even when tests, migrations, or source changed. Specify the feature baseline and inspect the complete changed-file set and relevant content.

Read-only review completed; no files modified. Build, restore, and audit commands were not executed.

---

# Round 2 — 2026-10-23 — Codex (primary, alone)

## Verdict: NO-GO

| Round-1 id | Status | Line in [spec.md](docs/specs/F37-DependencyUpgrades/definition/spec.md) | Closure note | Collateral |
|---|---|---|---|---|
| R1-01 | closed | 24, 54 | Seven packages enumerated; Test SDK excluded. | none |
| R1-02 | closed | 35 | Integration suite replaces the unavailable CLI check. | none |
| R1-03 | partial | 24–25 | Order and patch selection clarified. Native SQLite version evidence for the deployment target remains absent; only the resolved package version is recorded. | none |
| R1-04 | closed | 36, 58 | Documented compatibility fixes fit the permitted change set. | none |
| R1-05 | closed | 24, 54 | Explicit inventory, shared patch, and selection record required. | none |
| R1-06 | closed | 48, 57 | Recorded preview smoke check required. | none |
| R1-07 | partial | 58 | `{base}` remains undefined. Using `HEAD` after committing still hides committed changes; specify and record the revision preceding implementation. | none |
| R1-08 | closed | 15, 34, 55 | Production audit target and recorded development residue agree. | none |
| R1-09 | closed | 24, 36 | Full solution tests and stop/report rule specified. | none |
| R1-10 | closed | 15; F39:66 | Point-in-time baseline and sibling audit check added. | none |

No new MEDIUM-or-higher findings. No collateral contradiction or newly broken guarantee identified in the checked delta and sibling references. The two partial closures retain their round-1 MEDIUM severity.
