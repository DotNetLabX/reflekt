# F37 — Dependency Security Upgrades — Implementation

**Model:** claude-opus-5-5[1m]

**Base commit:** `f9832b3b6da8ebbdb6caf791fe460265d9af8a55`

## Step 1 — AngleSharp at 1.8.4, link previews unchanged
**Files:** `Directory.Packages.props` — `AngleSharp` 1.2.0 → 1.8.4 (DP-01 / BE-22, clears GHSA-pgww-w46g-26qg)
**Skills:** central-package-management
**Decisions:** The probe page is a real public page — `https://github.com/dotnet/runtime`, fetched with `curl -sL` on 2026-10-23 (HTTP 200, 372 203 bytes) to `out/f37-scratch/page.html`; it carries `og:title`, `og:description`, `og:image` and `link rel="icon"`. The probe (`out/f37-scratch/Probe.csproj`, `Microsoft.NET.Sdk.Web`, one project reference to `Reflekt.API.csproj`, not in `Reflekt.slnx`) calls `OpenGraphFetcher.ParseOpenGraphAsync` by reflection (`BindingFlags.NonPublic | BindingFlags.Static`) and prints the loaded AngleSharp assembly version.
**Deviations:** None
**Link-preview check (spec acceptance 6, offline route):**
- Before (1.2.0): `AngleSharp 1.2.0+8d12af12ee3518d63b29a5ae566210bd2fc60071 (1.2.0.0)`
  - Ok = True
  - Title = `GitHub - dotnet/runtime: .NET is a cross-platform runtime for cloud, mobile, desktop, and IoT apps.`
  - Description = `.NET is a cross-platform runtime for cloud, mobile, desktop, and IoT apps. - dotnet/runtime`
  - ImageUrl = `https://opengraph.githubassets.com/f87542da8defc9f0555d75555bb53477561d805a2f1bc91c54b51ba60f81f1c0/dotnet/runtime`
  - IconUrl = `https://github.githubassets.com/favicons/favicon.svg`
- After (1.8.4): `AngleSharp 1.8.4+32981ded5b0c78c4a5afcfbb677ffb329078fdf8 (1.8.4.0)`
  - Ok = True
  - Title = `GitHub - dotnet/runtime: .NET is a cross-platform runtime for cloud, mobile, desktop, and IoT apps.`
  - Description = `.NET is a cross-platform runtime for cloud, mobile, desktop, and IoT apps. - dotnet/runtime`
  - ImageUrl = `https://opengraph.githubassets.com/f87542da8defc9f0555d75555bb53477561d805a2f1bc91c54b51ba60f81f1c0/dotnet/runtime`
  - IconUrl = `https://github.githubassets.com/favicons/favicon.svg`
- All four fields non-null on both runs, and identical across the two runs.
**Acceptance:** `grep 'Include="AngleSharp"' Directory.Packages.props` → `<PackageVersion Include="AngleSharp" Version="1.8.4" />`. `dotnet list Reflekt.slnx package --vulnerable --include-transitive` lists no AngleSharp line (remaining: `SQLitePCLRaw.lib.e_sqlite3` 2.1.10 High in Reflekt.API, Reflekt.Persistence, Reflekt.IntegrationTests, Reflekt.UnitTests — Step 3's). Step close: `verify-run.js --profile fast --tied --changed Directory.Packages.props --light`, run `mvkgl784-5926ba55`, verdict `pass` (6.4 s; tied fallback to the whole `roles.unit`).

## Step 2 — The Microsoft.* family on one 10.0.x patch, whole solution green
**Status:** blocked — handed back to the architect (`questions.md` Q3); the version change is in the working tree, the step is not closed.
**Files:** `Directory.Packages.props` — the seven Microsoft.* entries 9.0.4 → 10.0.13 (DP-03): `Microsoft.Data.Sqlite`, `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.Google`, `Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.Mvc.Testing`
**Skills:** framework-currency
**Version lookup:** `dotnet package search {id} --exact-match --format json` for each of the seven ids, run 2026-10-23T04:18:34Z–04:18:39Z; every package's highest 10.0.x listing is 10.0.13 (10.0.12 below it), so 10.0.13 is the shared patch.
**Acceptance so far:** the 7-line grep gives `Version="10.0.13"` on all seven; `grep -c 'Version="9\.' Directory.Packages.props` → 0; `Microsoft.NET.Test.Sdk` still `Version="17.14.1"`. `dotnet build Reflekt.slnx -nologo -v q` → 0 errors, 0 warnings. No forced fix; no source file changed.
**Full role:**
- Run `mvkgn2o8-461c60a6` (04:18:56Z): verdict `fail` — `Reflekt.IntegrationTests` Failed 1, Passed 103, Total 104. Failing test `Reflekt.IntegrationTests.CardEndpointTests.CreateCard_ActionItem_Returns201`, thrown in `CardEndpointTests.InitializeAsync()` at `CardEndpointTests.cs:line 78` (`_factory.CreateClient()`), inside `WebApplicationFactory<Program>.StartServer()` → `CreateHost`. The exception type and message fall in the part of the record the runner elides, so they are not known. Not a compile error.
- Run `mvkgpdv7-eb5d9956` (04:20:44Z), same tree hash `4dcd021f…`, run once more only to see whether the failure repeats: verdict `pass` (all of `dotnet test Reflekt.slnx`, `npm ci`, `npm run build`; 50 s).
- Reading: the red is intermittent, at host start-up, in one test of 104; the other CardEndpointTests start the same host the same way and pass, so it does not look like a pending-model-changes error from `Database.Migrate()` (that would fail every host). Whether it also happens on 9.0.4 is not known.
**Decisions:** None
**Deviations:** The second full run was a diagnostic re-run, not a way round the red; under the plan's rule the step stops on the first red.
**Bug found:** intermittent host start-up failure — `CardEndpointTests.CreateCard_ActionItem_Returns201` in full run `mvkgn2o8-461c60a6`, green on re-run `mvkgpdv7-eb5d9956` — `tests/Reflekt.IntegrationTests/CardEndpointTests.cs:78`, cause not established (may predate the bump)
**Closed (2026-10-23, after Q3's answer — architect, option 1 with a tripwire):** Step 2 closes on the green full run `mvkgpdv7-eb5d9956` under the flaky rule now in the plan's Step 2 Accept: the red passed when re-run unchanged (same `tree_hash` `4dcd021f…`) on a quiet machine (`others: 0` on both records).
**Flaky test (listed, not fixed):** `Reflekt.IntegrationTests.CardEndpointTests.CreateCard_ActionItem_Returns201` — red in `mvkgn2o8-461c60a6`, green in `mvkgpdv7-eb5d9956`. Top stack frames: `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`1.StartServer()` ← `ConfigureHostBuilder` ← `DelegatedWebApplicationFactory.CreateHost`; reached from `WebApplicationFactory`1.CreateClient()` in `Reflekt.IntegrationTests.CardEndpointTests.InitializeAsync()`, `CardEndpointTests.cs:line 78`. Exception message not captured (record excerpt elided). Carry-over row for the reviewer: gathered into `## Carry-Over Findings`. Backlog row for `docs/backlog.md`: `delivery/change-notes.md` § Note 1. Tripwire (Q3): the same host-start failure in any later full run of this unit goes to a solo on a `Bug found:` line.
**Step close:** `verify-run.js --profile fast --tied --changed Directory.Packages.props --light`, run `mvkgv18s-5fd35e57`, verdict `pass` (6.0 s). The `Bug found:` line above stands as the record of the red; per Q3 it is handled as flaky, not as a solo hand-off.

## Step 3 — SQLite native library on a fixed version; restore and build clean
**Files:** None — no pin needed, `Directory.Packages.props` unchanged in this step
**Skills:** central-package-management
**Resolved version (DP-02):** `dotnet list Reflekt.slnx package --include-transitive` → `SQLitePCLRaw.lib.e_sqlite3` 2.1.13 (with `SQLitePCLRaw.bundle_e_sqlite3`, `.core`, `.provider.e_sqlite3` at 2.1.13) in every project that has it: Reflekt.API, Reflekt.Persistence, Reflekt.IntegrationTests, Reflekt.UnitTests. 2.1.13 ≥ 2.1.12, and the vulnerable list is clean, so **no pin**: no `CentralPackageTransitivePinningEnabled`, no `SQLitePCLRaw.lib.e_sqlite3` entry, and no full-role re-run (plan Decisions: the re-run is owed only for a pin).
**Acceptance:**
- `dotnet list Reflekt.slnx package --vulnerable --include-transitive` → "has no vulnerable packages given the current sources" for all nine projects (Blocks.Core, Blocks.Domain, Blocks.Exceptions, Blocks.Persistence, Reflekt.API, Reflekt.Domain, Reflekt.Persistence, Reflekt.IntegrationTests, Reflekt.UnitTests) — spec acceptance 1.
- `dotnet build Reflekt.slnx -nologo -v q --no-incremental 2>&1 | grep -cE "NU190[1-4]"` → `0` — spec acceptance 2, DP-07.
- `select sqlite_version()` through the app's connection → `3.53.3` (≥ 3.50.2). The scratch probe (`out/f37-scratch/`, mode `sqlite`) builds a `ConfigurationBuilder` with `ConnectionStrings:Reflekt` = `Data Source=:memory:`, calls `AddReflektPersistence(configuration)` on a `ServiceCollection`, resolves `ReflektDbContext` from a scope, opens `Database.GetDbConnection()` (`Microsoft.Data.Sqlite.SqliteConnection`) and runs the query; it also printed `SQLitePCLRaw.core 2.1.13`, and the native library loaded from `runtimes/win-x64/native/e_sqlite3.dll`.
**Step close:** `verify-run.js --profile fast --light` (no file changed in this step, so no `--tied --changed` list), run `mvkgxf87-e75f192c`, verdict `pass` (7.9 s).
**Decisions:** The probe reaches `Reflekt.Persistence` through its existing project reference to `Reflekt.API.csproj` (which references Persistence), so it resolves the app's own package graph; the plan named a direct Persistence reference, which the API reference already brings in.
**Deviations:** None

## Step 4 — The npm fix handed to the campaign merge as a change note
**Files:** `docs/specs/F37-DependencyUpgrades/delivery/change-notes.md` — Note 2 added: target `web/reflekt-web/package-lock.json` only; command `npm --prefix web/reflekt-web audit fix` with no `--force`, `--legacy-peer-deps` or `overrides`; expected result (`vue` 3.5.42 or later in the lockfile, `package.json` unchanged at `"vue": "^3.5.32"`, `audit --omit=dev` gives 0, `git diff --name-only` shows only `package-lock.json`); applied before every other unit's note on these files; the applier records the "after" audit outputs beside a re-run of spec acceptance 5 (the declared full role). The note carries the "before" outputs of both audits, taken 2026-10-23T04:27:51Z.
**Skills:** None (plan: `None`)
**Before (summary):** plain `npm audit` — 11 vulnerabilities (1 low, 1 moderate, 8 high, 1 critical): @babel/core, @vue/server-renderer + vue 3.2.13–3.5.41, baseline-browser-mapping, browserslist, nanoid, postcss, shell-quote, source-map-js, vite 8.0.0–8.1.5, ws. `npm audit --omit=dev` — 6 high: @vue/server-renderer + vue, nanoid, postcss, source-map-js, ws. Full text in `change-notes.md` § Note 2.
**Acceptance:** `git status --porcelain web/reflekt-web` → empty (the earlier `npm ci` in the full role and both `npm audit` runs left both npm files untouched). `package.json` read only: `"vue": "^3.5.32"`.
**Owed at merge:** spec acceptance 4 (`npm --prefix web/reflekt-web audit --omit=dev` → 0, plain audit recorded) and the lockfile part of spec acceptance 7 — not Missing (`questions.md` Q1).
**Step close:** `verify-run.js --profile fast --tied --changed docs/specs/F37-DependencyUpgrades/delivery/change-notes.md --light`, run `mvkh09w6-e2dc039a`, verdict `pass` (9.4 s; docs-only change, runner fell back to `roles.unit`).
**Decisions:** None
**Deviations:** None

## Step 5 — The change set holds only the allowed files
**Files:** None beyond the step records; `out/f37-scratch/` (the probe project, its build output and `page.html`) deleted with `rm -r out/f37-scratch`, then `rmdir out`
**Skills:** None (plan: `None`)
**Acceptance:** `test ! -e out/f37-scratch` succeeds.
`git diff --name-only f9832b3b6da8ebbdb6caf791fe460265d9af8a55`:
```
Directory.Packages.props
docs/specs/F37-DependencyUpgrades/definition/spec.md
docs/specs/F37-DependencyUpgrades/delivery/communication-log.md
docs/specs/F37-DependencyUpgrades/delivery/lessons.md
docs/specs/F37-DependencyUpgrades/delivery/plan.md
docs/specs/F37-DependencyUpgrades/delivery/questions.md
docs/specs/F37-DependencyUpgrades/delivery/reader-pair.md
docs/specs/F37-DependencyUpgrades/delivery/review-critic-codex.md
docs/specs/F37-DependencyUpgrades/delivery/review-critic.md
```
`git status --porcelain`:
```
 M Directory.Packages.props
 M docs/specs/F37-DependencyUpgrades/delivery/communication-log.md
 M docs/specs/F37-DependencyUpgrades/delivery/plan.md
 M docs/specs/F37-DependencyUpgrades/delivery/questions.md
?? .claude/.personas.json
?? docs/specs/F37-DependencyUpgrades/delivery/change-notes.md
?? docs/specs/F37-DependencyUpgrades/delivery/implementation.md
```
Every entry is allowed: `Directory.Packages.props` (8 changed lines — AngleSharp and the seven Microsoft.* entries; no pin); files under `docs/specs/F37-DependencyUpgrades/delivery/`; `docs/specs/F37-DependencyUpgrades/definition/spec.md`, whose diff against the base is the one `**Plan:**` line (`None` → `` `docs/specs/F37-DependencyUpgrades/delivery/plan.md` ``), nothing else under `definition/`; and `.claude/.personas.json` — the campaign's own artifact, not this unit's. No forced fix was made in Step 2 or 3, so no source file and no `graphify-out/` file is in the set, and `graphify update .` did not run (plan Decisions). Nothing under `.claude/tmp/` is present. `web/reflekt-web/` is absent — the lockfile part of spec acceptance 7 is owed at merge (Step 4).
**Step close:** `verify-run.js --profile fast --tied --changed docs/specs/F37-DependencyUpgrades/delivery/implementation.md --light`, run `mvkh1sfe-2feb2130`, verdict `pass` (9.2 s).
**Decisions:** None
**Deviations:** None

## Key Decisions
- Step 1: the link-preview probe page is a real public page (`https://github.com/dotnet/runtime`, saved 2026-10-23), not a hand-written one; network was available.
- Step 2: 10.0.13 — the highest 10.0.x patch all seven packages list on NuGet at 2026-10-23T04:18Z.
- Step 2: closed on the green re-run `mvkgpdv7-eb5d9956` under the flaky rule (`questions.md` Q3, architect); the one-off red `mvkgn2o8-461c60a6` is listed, not fixed.
- Step 3: no transitive pin — `SQLitePCLRaw.lib.e_sqlite3` resolves 2.1.13 under the 10.0.13 family and the vulnerable list is clean; so no full-role re-run was owed.
- Step 3: the probe reached `Reflekt.Persistence` through its `Reflekt.API.csproj` reference rather than a second direct reference — the same package graph.

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | central-package-management | |
| 2 | framework-currency | |
| 3 | central-package-management | invoked again for this step |
| 4 | None | plan: `None`; docs-only change note |
| 5 | None | plan: `None`; check-only step |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| Flaky host start in `CardEndpointTests.InitializeAsync` | medium | reviewer | `CardEndpointTests.CreateCard_ActionItem_Returns201` red in full run `mvkgn2o8-461c60a6` at `WebApplicationFactory.StartServer()` from `CardEndpointTests.cs:78`; green on unchanged re-run `mvkgpdv7-eb5d9956`; exception message not captured (record excerpt elided) | Listed under Q3's flaky rule; backlog row in `change-notes.md` § Note 1. Tripwire: the same failure in the handover's complete run goes to a solo to capture the message and check 9.0.4. |
| npm fix owed at merge | low | reviewer | `change-notes.md` § Note 2; `git status --porcelain web/reflekt-web` empty | Spec acceptance 4 and the lockfile part of acceptance 7 are discharged when the campaign merge applies Note 2 (Q1). |

## Deviations from Plan
- None. Step 2 closed under the flaky rule the architect added to its Accept after Q3, not under the original wording.

*Status: COMPLETE — developer, 2026-10-23*
