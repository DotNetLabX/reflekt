# F37 — Dependency Security Upgrades

**Kind:** technical (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` § 2; finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/deps-tests-conventions.md` (DP-*) and `findings/backend.md` (BE-22)
**Dependencies:** F33 (done)
**Rule list:** not applicable — package versions only, no rule-shaped behaviour
**Review:** round 1 — `delivery/review-critic.md`
**Status:** Ready
**Plan:** None

---

## Purpose

A fresh clone prints known-vulnerability warnings on every restore (one high), the frontend's production dependencies carry six high npm advisories, and the Microsoft packages are 9.0.4 on a `net10.0` target while the docs say .NET 10. Students cloning the repo should get a restore with no vulnerability warning, no production npm advisory, and a stack that matches what the docs say. This feature changes package versions only. The advisory state it reaches is a point-in-time baseline: later features that add packages re-check it (F39 does).

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below says otherwise. The steps run in this order: DP-01, DP-03, then the DP-02 check, then the npm rows.

| Finding | Required outcome |
|---|---|
| DP-01 (BE-22) | AngleSharp 1.2.0 → 1.8.4 in `Directory.Packages.props`. |
| DP-03 | The seven central entries on 9.0.4 move together to one 10.0.x patch: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Data.Sqlite`, `Microsoft.AspNetCore.Authentication.Google`, `Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.Mvc.Testing`. The patch is the latest 10.0.x on NuGet at build time (10.0.13 when this spec was reviewed); the step records which version and when it was looked up. `Microsoft.NET.Test.Sdk` is not part of this family and is not changed. This is **its own step**, and that step runs the whole solution's tests (`dotnet test Reflekt.slnx`), not only the `unit` role. |
| DP-02 (BE-22) | Decided after DP-03: when `dotnet list Reflekt.slnx package --vulnerable --include-transitive` is clean **and** `dotnet list Reflekt.slnx package --include-transitive` shows `SQLitePCLRaw.lib.e_sqlite3` at 2.1.12 or later, no pin is added (the review's trial bump to 10.0.13 resolved 2.1.13, whose native library carries SQLite 3.53.3 — F34 DP-02). Otherwise pin `SQLitePCLRaw.lib.e_sqlite3` 2.1.13 with `CentralPackageTransitivePinningEnabled=true`. Either way the step records the resolved package version, and the SQLite engine version the app actually runs (`select sqlite_version()` through the app's connection, from a test or a one-off run) — 3.50.2 or later. The Linux deployment uses the same package's `linux-x64` native library; the version check on the build machine stands for it. |
| DP-08 … DP-12 | `npm audit fix` in `web/reflekt-web` (lockfile only, no `--force`, no `--legacy-peer-deps`, no `overrides`); `vue` lands ≥ 3.5.42. |
| DP-07 | Cleared by DP-01 + DP-03/DP-02: a restore prints no NU1901–NU1904 line. |

## Decisions (taken — owner or architect)

| Decision | Why | Rejected alternative | Confidence |
|---|---|---|---|
| Microsoft.* 10.0.x bump is in this feature (owner, 2026-10-13). | Stack must match the docs. | A separate later feature. | high — owner's pick |
| The npm target is **no production advisory**. A dev-only advisory npm cannot clear without `--force` — today `vite`, held back by `vite-plugin-vue-devtools`' peer range — is accepted for now and recorded: the plain `npm audit` output after the fix goes into `implementation.md`. | Dev-server only; never shipped to a browser. Overriding a peer range edits `package.json` and risks the dev plugin. | An `overrides` entry or `--force`. | medium — a student running plain `npm audit` still sees it |
| No schema change. The integration suite is the check: the API calls `Database.Migrate()` at start-up, which throws a pending-model-changes error when the model and the migration snapshot differ, so a green suite proves the bump changed no model. No migration file is added; if the suite fails that way, the step stops and reports it. | The suite already runs this path on every host; the `dotnet ef` tool is not installed here and a tool manifest would be a new tracked file. | `dotnet ef migrations has-pending-model-changes`. | high |
| A compile or runtime fix forced by an upgraded package is allowed only if it is named in `implementation.md` with the package that forced it. A test failure that is not a compile error stops the step and is reported, not worked around. | Keeps "versions only" honest while allowing an unavoidable one-line fix. | Forbid any source change. | high |
| Test tooling (FluentAssertions 7.2.0, xunit 2.x, Test.Sdk, coverlet) stays. | Out of the security scope; documented in F40. | Bump test tooling. | high |

## Out of scope

- FastEndpoints 5 → 8 and FluentValidation 11 → 12 (DP-04): breaking majors.
- Scalar, Mapster minor/patch bumps (DP-05).

## What must not change

- No behaviour change; every existing test passes unchanged.
- No migration; `net10.0` TFM; frontend `package.json` (only the lockfile changes).
- Link previews keep working after the AngleSharp bump (checked by hand, below — no test covers them).

## Acceptance (checkable)

1. `dotnet list Reflekt.slnx package --vulnerable --include-transitive` → no vulnerable packages in any project.
2. `dotnet build Reflekt.slnx` → no NU1901–NU1904 warning.
3. The seven entries named under DP-03 all carry the same `10.0.x` version in `Directory.Packages.props`; `Microsoft.NET.Test.Sdk` is unchanged.
4. `npm --prefix web/reflekt-web audit --omit=dev` → 0 vulnerabilities; the plain `npm audit` output is recorded in `implementation.md`.
5. The declared full role in `.claude/verify.json` passes.
6. One link-preview check recorded in `implementation.md`: attach a public URL as evidence on a running app and confirm its title and image appear (or, offline, the fetcher's selectors run against a saved HTML page give the same title, description, image and icon on 1.2.0 and 1.8.4).
7. The feature's whole change set — `git diff --name-only {base}` plus `git status --porcelain` for untracked files, where `{base}` is the commit the feature started from, recorded at the top of `implementation.md` — touches only `Directory.Packages.props`, `web/reflekt-web/package-lock.json`, the delivery docs, and any file named as a forced fix under Decisions.
