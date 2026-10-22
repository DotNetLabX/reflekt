## Round 1 — 2026-10-22 — spec — primary (codex)

Header note: two readers, Codex primary (read-only job; its verdict is persisted verbatim in `review-critic-codex.md`) and a Sonnet nexus critic as the second reader. Pair `codex + sonnet`, picked by the owner (`reader-pair.md`). Both read the spec after it had been committed and marked Ready without review — the owner caught it; the spec was set back to Draft before this round.

Primary reader: see `review-critic-codex.md` — `## Verdict: NO-GO`, 1 HIGH, 6 MEDIUM.

## Second reader — round 1 (sonnet)

# F37 Dependency Security Upgrades — Critic Review

## Mode: Spec Review (technical spec, code-grounded)
## Verdict: ACCEPT

No CRITICAL or HIGH. Four MEDIUMs and three LOWs worth a pass before Ready. The central mechanism holds up empirically: I applied the spec's bump in a scratch copy outside the repo — 10.0.13 + AngleSharp 1.8.4 → build 0 warnings, `dotnet list package --vulnerable --include-transitive` clean for all 9 projects, unit 67/67 and integration 104/104 green; `Microsoft.Data.Sqlite` 10.0.13 depends on `SQLitePCLRaw.bundle_e_sqlite3` 2.1.13, so `lib.e_sqlite3` resolved to 2.1.13 with no pin needed; `npm audit fix` took vue / `@vue/server-renderer` to 3.5.44, `package.json` unchanged, `npm audit --omit=dev` → 0 vulnerabilities, `npm ci` + build green. The repo itself was not touched.

(Condensed by the architect, not verbatim: every finding is kept with its level, evidence and fix; the reader's full message is in the session transcript.)

- **[MEDIUM] F37-01 — "Clean audit" is promised, but the check covers only production dependencies and the dev tree is not clean.** Plain `npm audit` today reports 11 (1 critical `shell-quote`, dev). After `npm audit fix` 1 high remains: `vite` 8.0.8 dev-server advisories; npm will not move it because `vite-plugin-vue-devtools` → `vite-plugin-inspect` / `vite-dev-rpc` peer ranges stop at ^7; `--omit=dev` passes. Fix: narrow Purpose to production and record the residue, or decide how to clear it; record plain `npm audit` in `implementation.md`; counts are point-in-time.
- **[MEDIUM] F37-02 — The "no silent schema change" check names a tool that is not installed.** No `.config/dotnet-tools.json`, no global `dotnet-ef`; a manifest would break AC6; an empty `migrations add` writes files. The guard that exists: `UseReflektPersistence` calls `Migrate()`, which throws `PendingModelChangesWarning` on drift, and the integration suite runs it (104/104 green on 10.0.13). Fix: name the suite as the check.
- **[MEDIUM] F37-03 — DP-02's condition, target version and step order are not pinned.** "Latest 10.0.x" moves (10.0.12 → 10.0.13); the condition is stated in native SQLite version while the only check is the package advisory range; DP-02 listed before the step it depends on. Fix: name the version, order DP-01 → DP-03 → DP-02 check, decide by audit + `lib.e_sqlite3` ≥ 2.1.12.
- **[MEDIUM] F37-04 — "Gated by the full suite" does not match the per-step `unit` role.** `unit` is domain tests only (67 in 81 ms), none touch EF/SQLite/HTTP; `full` is 68 s vs `stepLimitSeconds` 30. Fix: name `dotnet test Reflekt.slnx` for the DP-03 step; add stop-and-report for non-compile failures.
- **[LOW] F37-05 — The Microsoft.* list is miscounted and AC3 proves only absence of 9.x.** Seven entries; `Microsoft.NET.Test.Sdk` is also "Microsoft.*" but frozen. Fix: list the seven, exclude the SDK, positive AC3.
- **[LOW] F37-06 — The AngleSharp bump has no behavioural check.** No OpenGraph tests; DP-01 asked for a smoke test. Throwaway comparison on sample HTML gave identical output on 1.2.0 and 1.8.4. Fix: manual smoke step or record accepted risk.
- **[LOW] F37-07 — Overlap with F36 and F39 in the frontend lane is not acknowledged.** The advisory state is point-in-time; F39 adds ESLint/Prettier without an audit check. Fix: say it is a baseline; add `npm audit --omit=dev` to F39's acceptance.

Coverage ledger (second reader): DP-01, DP-07, test-tooling decision COVERED; DP-02, DP-03, no-migration decision, DP-08…12, Acceptance 1–6 PARTIAL; siblings PARTIAL (F40 full, others grep); `backend.md` BE-22 row NOT OPENED (content not read); `docs/critic-calibration/ruler.md` absent.

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | "Every Microsoft.*" contradicts the frozen Test SDK; list double-counts | internal contradiction | wrong package set bumped or AC3 unprovable | quoted | — | scope | Scope DP-03, AC3 | codex-1, F37-05 |
| R1-02 | codex, sonnet | MEDIUM | seed-0022 | Migration check names an uninstalled tool; the real guard is unnamed | acceptance not runnable | schema-drift guard skipped or a stray file committed | path | — | acceptance | Decisions row 2 | codex-3, F37-02 |
| R1-03 | codex, sonnet | MEDIUM | seed-0022 | DP-02 condition, version and order unpinned; native vs package version | conditional with unmeasurable test | builders differ on pin | scenario | — | scope | Scope DP-02/DP-03 | codex-5, F37-03 |
| R1-04 | codex | MEDIUM | unranked — Codex severity MEDIUM | Compile-fix allowance contradicts diff acceptance | internal contradiction | a necessary fix fails AC6 | quoted | — | acceptance | Out of scope, AC6 | codex-2 |
| R1-05 | codex, sonnet | MEDIUM | seed-0022 | AC3 accepts 8.x, mixed patches or deletions | weak negative check | DP-03 unproven | quoted | — | acceptance | AC3 | codex-4, F37-05 |
| R1-06 | codex, sonnet | MEDIUM | contested: MEDIUM / LOW — settled MEDIUM: OpenGraph failures are swallowed, so nothing else would notice | AngleSharp behaviour unchecked | claimed behaviour with no test | broken previews ship unnoticed | scenario | — | acceptance | What must not change | codex-6, F37-06 |
| R1-07 | codex | MEDIUM | unranked — Codex severity MEDIUM | `git diff --stat` misses staged, committed and untracked files | weak acceptance | forbidden changes pass | path | — | acceptance | AC6 | codex-7 |
| R1-08 | sonnet | MEDIUM | seed-0022 | "Clean audit" vs prod-only check; dev `vite` residue | acceptance narrower than purpose | student still sees a high advisory | scenario | — | scope | Purpose, AC4 | F37-01 |
| R1-09 | sonnet | MEDIUM | seed-0022 | Step gate names `unit`; full-suite gate and failure rule missing | gate-role mismatch | the run that matters is skipped | path | — | acceptance | Scope DP-03 | F37-04 |
| R1-10 | sonnet | LOW | seed-0129 | Advisory state is point-in-time; F39 adds packages unchecked | cross-spec hand-off | later advisories unseen | path | — | sibling | Purpose; F39 AC | F37-07 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-22)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | Scope DP-03 (seven named, SDK excluded); AC3 |
| R1-02 | fixed-real | — | — | Decisions row 3 (suite is the check) |
| R1-03 | fixed-real | — | — | Scope preamble (order); DP-03 (version recorded); DP-02 (decision rule) |
| R1-04 | fixed-real | — | — | Decisions row 4; AC7 |
| R1-05 | fixed-real | — | — | AC3 |
| R1-06 | fixed-real | — | — | What must not change; AC6 |
| R1-07 | fixed-real | — | — | AC7 |
| R1-08 | fixed-real | — | — | Purpose; Decisions row 2; AC4 |
| R1-09 | fixed-real | — | — | Scope DP-03; Decisions row 4 |
| R1-10 | fixed-real | — | — | Purpose (baseline); F39 spec AC (audit added) |

Reconciled: Purpose ↔ Decisions row 2 ↔ AC4 (production-only target, residue recorded); Scope DP-03 ↔ AC3 ↔ Decisions row 1; Decisions row 4 ↔ AC7 (forced fixes allowed and listed); F39 spec § Acceptance (audit line added in the same fold).

## Round 2 — 2026-10-23 — spec — primary (codex)

Header note: Codex alone. Read: delta. Floor: MEDIUM. Verdict and fold-verification table persisted verbatim in `review-critic-codex.md` § Round 2: eight closed, R1-03 and R1-07 partial; no new finding.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — Codex severity MEDIUM | No SQLite engine-version evidence | evidence gap | advisory clearance unproven | path | — | fold | Scope DP-02 | R1-03 |
| R2-02 | codex | MEDIUM | unranked — Codex severity MEDIUM | Base revision undefined in the change-set check | acceptance mechanics | committed changes hidden | quoted | — | fold | AC7 | R1-07 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | Scope DP-02 (sqlite_version() recorded; Linux note) |
| R2-02 | fixed-real | — | — | AC7 (base commit recorded in implementation.md) |

Reconciled: Scope DP-02 ↔ AC1. No round 3: no CRITICAL.
