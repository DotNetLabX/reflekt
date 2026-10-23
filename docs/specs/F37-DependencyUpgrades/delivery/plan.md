# F37 — Dependency Security Upgrades

**Feature Spec:** `docs/specs/F37-DependencyUpgrades/definition/spec.md`
**Model:** claude-opus-5-5[1m]
**Size:** 5 developer-built steps · 1 file · fullChecks: none — no fullChecks list declared
**Lane:** light — normal gear (as today, by size: 5 steps, 1 file, no fullChecks list)
**Build lane:** fastest — lighter checks — normal gear
**Gear:** normal — campaign
**Related:** F34-ConventionsAudit (closed — source of the DP-* rows); F38-BackendConventions (Ready — depends on F37's packages); F39-FrontendConventions (Ready — re-checks `npm audit --omit=dev` after F37's change note is applied); F40-ArchitectureDocs (Ready — aligns CLAUDE.md with the stack, CV-07)

Base commit: `f9832b3b6da8ebbdb6caf791fe460265d9af8a55`. Run order is fixed by the spec: DP-01, DP-03, the DP-02 check, the npm rows. Owner's standing rules for this run: cleanup only, no new tests, no business-rule registries, never push, never rewrite history. Shared files this unit never edits: `web/reflekt-web/package.json`, `web/reflekt-web/package-lock.json`, `docs/backlog.md`.

## Implementation Steps

### Step 1 — AngleSharp at 1.8.4, link previews unchanged
Accept: `grep 'Include="AngleSharp"' Directory.Packages.props` gives `Version="1.8.4"`; `dotnet list Reflekt.slnx package --vulnerable --include-transitive` lists no AngleSharp line; `implementation.md` opens with the base commit above, and records the link-preview check by the spec's offline route: one saved HTML page that carries `og:title`, `og:description`, `og:image` and a `link rel="icon"` (a real public page saved to `out/f37-scratch/`, or, with no network, a hand-written page carrying those four tags) is run by a scratch probe through the fetcher's own `ParseOpenGraphAsync` (invoked by reflection, so no selector is copied) on 1.2.0 (before the bump) and on 1.8.4 (after); both runs return `Ok = true` with `Title`, `Description`, `ImageUrl` and `IconUrl` all non-null, and the four values are identical across the two runs — the page, the eight values and both versions are pasted in the step block; any null, or a difference, stops the step (spec acceptance 6) · Files: `Directory.Packages.props` · Follow central-package-management · Risk: risky — dependency kind (`Directory.Packages.props`), and no test covers link previews

### Step 2 — The Microsoft.* family on one 10.0.x patch, whole solution green
Accept: `grep -E 'Include="Microsoft\.(EntityFrameworkCore|EntityFrameworkCore\.Sqlite|EntityFrameworkCore\.Design|Data\.Sqlite|AspNetCore\.Authentication\.Google|AspNetCore\.OpenApi|AspNetCore\.Mvc\.Testing)"' Directory.Packages.props` gives 7 lines carrying one and the same `Version="10.0.N"` (today: 7 lines at `9.0.4`; 10.0.13 is the latest shared patch, looked up 2026-10-23T03:54Z); `grep -c 'Version="9\.' Directory.Packages.props` gives 0 (7 today); `grep 'Microsoft.NET.Test.Sdk' Directory.Packages.props` still gives `Version="17.14.1"`; `implementation.md` records the version, the lookup command (`dotnet package search {id} --exact-match`) and its UTC time; the step closes with `node {scripts}/verify-run.js --role full --slug F37-DependencyUpgrades` at verdict `pass`, cited in the step block; every forced fix is named with the package that forced it. A full-run red that passes when re-run unchanged on a quiet machine is flaky: the step closes on the green re-run, the flaky test is recorded in the step block (both run ids, the test, its stack frames) with a carry-over row for the reviewer, and its backlog row goes into `delivery/change-notes.md` for `docs/backlog.md`. If the same failure appears again in any later full run of this unit, it goes to a solo to find its cause (`questions.md` Q3). Any other red that is not a compile error — a pending-model-changes error from `Database.Migrate()` included — stops the step and is handed back as a blocker; no migration file is added (spec Decisions rows 3 and 4) · Files: `Directory.Packages.props` · Follow framework-currency · Risk: risky — dependency kind, EF Core and ASP.NET Core major version (9 → 10)

### Step 3 — SQLite native library on a fixed version; restore and build clean
Accept: `dotnet list Reflekt.slnx package --include-transitive` shows `SQLitePCLRaw.lib.e_sqlite3` at 2.1.12 or later in every project that has it (2.1.10 today); if it does not, or if the vulnerable list below is not clean, `Directory.Packages.props` gains `CentralPackageTransitivePinningEnabled=true` and `SQLitePCLRaw.lib.e_sqlite3` 2.1.13 with its one-line reason comment, and the full role is run again and passes; `dotnet list Reflekt.slnx package --vulnerable --include-transitive` reports "has no vulnerable packages" for every project (spec acceptance 1); `dotnet build Reflekt.slnx -nologo -v q --no-incremental 2>&1 | grep -cE "NU190[1-4]"` gives 0 (28 today; spec acceptance 2, DP-07); `implementation.md` records the resolved `SQLitePCLRaw.lib.e_sqlite3` version, pin or no pin, and `select sqlite_version()` run through the app's connection, 3.50.2 or later (3.46.1 today): the scratch probe registers the app's persistence with `AddReflektPersistence(configuration)`, its configuration setting `ConnectionStrings:Reflekt` to `Data Source=:memory:`, resolves `ReflektDbContext`, opens `Database.GetDbConnection()` and queries it · Files: `Directory.Packages.props` · Follow central-package-management · Risk: risky — dependency kind; a pin changes the native library every SQLite path loads

### Step 4 — The npm fix handed to the campaign merge as a change note
Accept: `docs/specs/F37-DependencyUpgrades/delivery/change-notes.md` holds the note `questions.md` § Q1's answer specifies — target `web/reflekt-web/package-lock.json` only; command `npm --prefix web/reflekt-web audit fix` with no `--force`, `--legacy-peer-deps` or `overrides`; expected result `vue` 3.5.42 or later in the lockfile, `package.json` unchanged (`"vue": "^3.5.32"`), `npm --prefix web/reflekt-web audit --omit=dev` gives 0, `git diff --name-only` after the command shows only `package-lock.json`; F37's note applied before every other unit's note on these files; the applier then re-runs spec acceptance 5 (the declared full role in `.claude/verify.json`, whose npm leg `npm ci && npm run build` is the part the lockfile change reaches) and records its result beside the "after" `npm audit` output. The note also carries the "before" outputs of `npm --prefix web/reflekt-web audit` and `npm --prefix web/reflekt-web audit --omit=dev`. `git status --porcelain web/reflekt-web` gives nothing; `implementation.md` marks spec acceptance 4 and the lockfile part of acceptance 7 "owed at merge" · Files: `docs/specs/F37-DependencyUpgrades/delivery/change-notes.md` · None

### Step 5 — The change set holds only the allowed files
Accept: `git diff --name-only f9832b3b6da8ebbdb6caf791fe460265d9af8a55` plus `git status --porcelain`, both pasted into `implementation.md`, list only `Directory.Packages.props`; files under `docs/specs/F37-DependencyUpgrades/delivery/`; `docs/specs/F37-DependencyUpgrades/definition/spec.md`, where `git diff f9832b3b6da8ebbdb6caf791fe460265d9af8a55 -- docs/specs/F37-DependencyUpgrades/definition/spec.md` changes the one `**Plan:**` line only (any other change under `definition/` fails the step); any file named as a forced fix in Step 2 or 3 (with its `graphify-out/` files, see Decisions); and the campaign's own artifacts, `.claude/.personas.json` and anything under `.claude/tmp/` — not this unit's, named as such in the step block; `test ! -e out/f37-scratch` succeeds (spec acceptance 7, lockfile part owed at merge) · Files: none beyond the step records · None

## Where to Look

- Step 1: `Directory.Packages.props` — the `HTML parsing` item group; `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphFetcher.cs` — `FetchAsync` (public entry) and `ParseOpenGraphAsync` (the selectors); `ParseOpenGraphAsync` is `private static`, reached by reflection, so the HTTP and host checks of `FetchAsync` are not part of this check. The probe is a new scratch project in `out/f37-scratch/` (git-ignored by `[Oo]ut/`) referencing `src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj` — no precedent; it is never added to `Reflekt.slnx` and is deleted before Step 5.
- Step 2: `Directory.Packages.props` — the `EF Core`, `Auth`, `OpenAPI` and `Testing` item groups; `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — `UseReflektPersistence` (`Database.Migrate()`, the model-drift check every integration factory runs); `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — `AddOpenApi`, `MapOpenApi`, `MapScalarApiReference` (the OpenAPI 10 surfaces; no document transformers in the code); `src/Services/Reflekt/Reflekt.API/Auth/AuthenticationConfiguration.cs` — `AddGoogle`; `src/Services/Reflekt/Reflekt.Persistence/Repositories/TeamRepository.cs` — the `ExecuteDeleteAsync` calls over `Contains` (EF 10 translates parameter collections differently).
- Step 3: `Directory.Packages.props` — the top `PropertyGroup`; `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — `AddReflektPersistence` (registers `ReflektDbContext` on `ConnectionStrings:Reflekt`; its `DomainEventPublishingInterceptor` takes no constructor dependency). The scratch probe in `out/f37-scratch/` references `src/Services/Reflekt/Reflekt.Persistence/Reflekt.Persistence.csproj`, so it resolves the app's own package graph, pin included — no precedent. This probe shape ran at plan time (one `Microsoft.NET.Sdk.Web` project referencing `Reflekt.API.csproj`, serving Steps 1 and 3) and printed `sqlite 3.46.1`, and the reflection call returned `Ok = True` with all four fields on a hand-written page.
- Step 4: `docs/specs/F37-DependencyUpgrades/delivery/questions.md` — § Q1, its Answer; `web/reflekt-web/package.json` — read only, `dependencies.vue`.
- Step 5: `implementation.md` — its header (the base commit). Delete the scratch folder with `rm -r out/f37-scratch` then `rmdir out`: the campaign fence refuses `rm -rf out`.

## Decisions

| Decision | Why | Rejected alternative | Confidence | Status |
|---|---|---|---|---|
| The DP-03 version is the latest 10.0.x patch all seven packages share on NuGet at build time (10.0.13 on 2026-10-23); where they differ, the highest patch all seven share. | The spec asks for one patch across the family. | Each package's own latest. | high — every package listed 10.0.13 at plan time | decided |
| AngleSharp stays at 1.8.4, though NuGet lists 1.8.5 on 2026-10-23. If `--vulnerable` flags 1.8.4, the step stops and hands back. | The spec names 1.8.4, which clears the advisory (fixed in 1.5.0); taking 1.8.5 would change a Ready spec. | 1.8.5. | high — the spec value | decided |
| The link-preview check takes the spec's offline route: one saved page through the fetcher's own `ParseOpenGraphAsync` (reflection), on both versions, every field non-null and identical. | The running-app route needs Google OAuth credentials an unattended unit does not have, and the owner's rule forbids a new test; reflection runs the fetcher's code instead of a copy. | The running app; a probe calling `FetchAsync` on a live URL (no source asks for it); copying the selectors; adding a test. | high — spec acceptance 6 names this route | decided |
| The SQLite engine version is read through the app's connection: a scratch probe calling `AddReflektPersistence` and querying `ReflektDbContext`'s connection. | Spec DP-02 asks for the app's connection; a one-off run leaves no tracked file. | A separate `SqliteConnection`; a version-string scan of the native library; a test. | high — the plain-connection form of the probe ran at plan time | decided |
| Every scratch probe lives in `out/f37-scratch/` (git-ignored) and is deleted before Step 5. | The campaign forbids writing outside the worktree; the ignored folder keeps acceptance 7 clean. | `D:\temp`. | high — `git check-ignore` confirms the path | decided |
| A pin added in Step 3 re-runs the full role. | The pin changes the native library the integration suite loads, after Step 2's green run. | The fast step close only. | high | decided |
| A bump of another package (for example Scalar) counts as a forced fix only when the build or app start fails without it: the smallest version that fixes it, named with the package that forced it; otherwise the step stops. | Keeps DP-05 out of scope while allowing an unavoidable fix (spec Decisions row 4). | Bumping Scalar with the family. | medium — no break is expected (no OpenAPI transformers in the code) | decided |
| `graphify update .` does not run: no code file is expected to change. If a forced fix changes a `.cs` file, `graphify update .` runs and its `graphify-out/` changes are named with that forced fix under acceptance 7. | `graphify-out/` is tracked; CLAUDE.md asks for the update only after code files change. | Always running it. | high | decided |
| `.claude/.personas.json` and anything under `.claude/tmp/` are listed in Step 5 as the campaign's own artifacts, not this unit's. | Both are campaign tooling (the coordinator's persona file, its review scratch); `.claude/tmp/` may reappear between rounds. | Treating them as forbidden changes. | high — the coordinator names them as campaign tooling | decided |
| The `**Plan:**` line of `definition/spec.md` is the one allowed change outside `delivery/` under acceptance 7. | Pointing the spec at its plan is the pipeline's cross-reference duty, written at plan time; acceptance 7 names delivery docs only, so the plan names this one line rather than widening the folder. | Allowing all of the feature folder; reverting the `Plan:` line. | high — `git diff` shows the one line | decided |

## Decisions and why

### P1 — The npm rows become a change note
- **By:** architect (the campaign's shared architect, AR-1)
- **Decided:** this unit does not touch `web/reflekt-web/package.json` or `package-lock.json`; it writes the change note in `delivery/change-notes.md`, and spec acceptance 4 and the lockfile part of acceptance 7 are owed at merge.
- **Why:** the campaign marks both npm files as shared; a unit writes a change note instead, and F37's note is applied before every other unit's note on these files.
- **Turned down:** running `npm audit fix` in the unit and committing the lockfile — breaks the shared-file rule.
- **Rests on:** the campaign's shared-file list naming `web/reflekt-web/package-lock.json`.
- **Confidence:** high — it is the campaign rule.
- **Source:** `questions.md` Q1
- **State:** current

### P2 — The developer runs the full role at Step 2's close
- **By:** team lead (answering `questions.md` Q2, recommended option)
- **Decided:** the developer runs `verify-run.js --role full --slug F37-DependencyUpgrades` at Step 2's close; a red that is not a compile error stops the step as a blocker.
- **Why:** architect's reading — the `unit` role runs only `tests/Reflekt.UnitTests`, so the integration suite, where `Database.Migrate()` proves no model change, runs only in the `full` role (68 s measured on 2026-10-13).
- **Turned down:** handing back after Step 2 for the coordinating session's complete run — one more hand-back and resume.
- **Rests on:** `.claude/verify.json` `roles.unit` leaving out the integration tests.
- **Confidence:** medium — a process call by the coordinator.
- **Source:** `questions.md` Q2
- **State:** current

## Open Questions

None

## Plan Review

Round 1 (2026-10-23, Codex primary NO-GO + Sonnet second reader ACCEPT): six findings (four MEDIUM, two LOW) are all folded `fixed-real`. Record: `review-critic.md` § Round 1 — 2026-10-23 — plan, its Merge and Fold tables. Round 2 (2026-10-23, Codex alone, full read, MEDIUM floor): GO. All six round-1 findings are closed, with no new finding, and the two surfaces round 1 left unopened were read with no issue. The schedule ends there (no CRITICAL). Re-derived after the last fold: Size, Lane and Build lane unchanged. **Approved 2026-10-23** — no open questions, no cut candidates, and no tagged decision row, so nothing waits for the owner.
