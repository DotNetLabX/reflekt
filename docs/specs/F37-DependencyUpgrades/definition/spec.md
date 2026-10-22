# F37 — Dependency Security Upgrades

**Kind:** technical (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` § 2; finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/deps-tests-conventions.md` (DP-*) and `findings/backend.md` (BE-22)
**Dependencies:** F33 (done)
**Rule list:** not applicable — package versions only, no rule-shaped behaviour
**Status:** Ready
**Plan:** None

---

## Purpose

A fresh clone prints known-vulnerability warnings on every restore (one high), the frontend has six high npm advisories, and the Microsoft packages are 9.0.4 on a `net10.0` target while the docs say .NET 10. Students cloning the repo should get a clean restore, a clean audit and a stack that matches what the docs say. This feature changes package versions only.

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below says otherwise.

| Finding | Required outcome |
|---|---|
| DP-01 (BE-22) | AngleSharp 1.2.0 → 1.8.4 in `Directory.Packages.props`. |
| DP-02 (BE-22) | Pin `SQLitePCLRaw.lib.e_sqlite3` to 2.1.13 via `CentralPackageTransitivePinningEnabled=true` + a `PackageVersion` entry — **unless** the DP-03 bump already pulls a SQLite build ≥ 3.50.2 transitively, in which case no pin is added and the step records the resolved version. |
| DP-03 | Every `Microsoft.*` package in `Directory.Packages.props` (EF Core ×4, `Microsoft.Data.Sqlite`, `Authentication.Google`, `AspNetCore.OpenApi`, `Mvc.Testing`) moves together to the latest 10.0.x patch, as **its own step**, gated by the full suite. |
| DP-08 … DP-12 | `npm audit fix` in `web/reflekt-web` (lockfile only, no `--force`, no major bumps); `vue` lands ≥ 3.5.42. |
| DP-07 | Cleared by DP-01 + DP-02: a restore prints no NU1902/NU1903/NU1904 line. |

## Decisions (taken — owner or architect)

| Decision | Why | Rejected alternative |
|---|---|---|
| Microsoft.* 10.0.x bump is in this feature (owner, 2026-10-13). | Stack must match the docs; tests cover the API surface. | A separate later feature. |
| No migration is added by the EF Core bump. If `dotnet ef migrations has-pending-model-changes` (or an empty `migrations add`) shows a model diff caused only by the provider upgrade, the step stops and reports it instead of committing a migration. | A version bump must not change the schema silently. | Accept whatever migration EF generates. |
| Test tooling (FluentAssertions 7.2.0, xunit 2.x, Test.Sdk, coverlet) stays as is. FluentAssertions 8+ changed licence. | Out of the security scope; FA 7.2.0 is a deliberate choice (documented in F40). | Bump test tooling. |

## Out of scope

- FastEndpoints 5 → 8 and FluentValidation 11 → 12 (DP-04): breaking majors.
- Scalar, Mapster minor/patch bumps (DP-05).
- Any code change other than what a bumped package forces to compile (none is expected; if one is needed it is listed in `implementation.md`).

## What must not change

- No source behaviour change; every existing test passes unchanged.
- No new migration (see Decisions).
- `.slnx` solution, `net10.0` TFM, frontend `package.json` direct ranges other than what `npm audit fix` rewrites.

## Acceptance (checkable)

1. `dotnet list Reflekt.slnx package --vulnerable --include-transitive` → "has no vulnerable packages" for every project.
2. `dotnet build Reflekt.slnx` → 0 warnings of code NU1901–NU1904.
3. `grep -E 'Include="Microsoft\.[^"]+" Version="9\.' Directory.Packages.props` → no hits.
4. `npm --prefix web/reflekt-web audit --omit=dev` → 0 vulnerabilities.
5. The declared full role in `.claude/verify.json` passes (backend tests + `npm ci` + frontend build).
6. `git diff --stat` touches only `Directory.Packages.props`, `web/reflekt-web/package-lock.json` (and `package.json` only if `npm audit fix` rewrote a range), plus delivery docs.
