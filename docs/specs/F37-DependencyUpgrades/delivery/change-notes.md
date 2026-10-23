# F37-DependencyUpgrades — Change Notes

Notes for shared files this unit does not edit; the campaign merge applies them.

## Note 1 — `docs/backlog.md`: flaky host start in CardEndpointTests

**Target:** `docs/backlog.md` — one new backlog row.
**Source:** `questions.md` Q3 (architect, 2026-10-23); `implementation.md` § Step 2.

Row to add:

> Flaky host start in `CardEndpointTests.InitializeAsync` — `Reflekt.IntegrationTests.CardEndpointTests.CreateCard_ActionItem_Returns201` failed once at `WebApplicationFactory.StartServer()` (`CardEndpointTests.cs:78`, `_factory.CreateClient()`), seen once on Microsoft.* 10.0.13 after F37's bump, green on an unchanged re-run; cause not found (exception message not captured). Full runs: red `mvkgn2o8-461c60a6`, green `mvkgpdv7-eb5d9956`. Next: capture the exception message and check whether it also happens on 9.0.4 (base `f9832b3`).

## Note 2 — `web/reflekt-web/package-lock.json`: `npm audit fix` (DP-08 … DP-12)

**Target:** `web/reflekt-web/package-lock.json` only. `web/reflekt-web/package.json` is not to change.
**Source:** `questions.md` Q1 (AR-1, shared architect of the campaign, 2026-10-23); plan Step 4; spec DP-08 … DP-12, acceptance 4, 5 and 7.
**Order:** apply this note **before every other unit's note on `web/reflekt-web/package.json` or `package-lock.json`** (F39's comes after it).

**Command** (from the repo root, no other flag):

```
npm --prefix web/reflekt-web audit fix
```

No `--force`, no `--legacy-peer-deps`, no `overrides` entry.

**Expected result:**
- `vue` 3.5.42 or later in `package-lock.json`.
- `package.json` unchanged — still `"vue": "^3.5.32"` (3.5.42 is inside that range). If `package.json` changes anyway, the applier records it.
- `npm --prefix web/reflekt-web audit --omit=dev` reports 0 vulnerabilities.
- `git diff --name-only` after the command shows only `web/reflekt-web/package-lock.json`.

**After applying, the applier records side by side:**
1. The "after" output of `npm --prefix web/reflekt-web audit` (plain — a dev-only advisory `npm audit fix` cannot clear without `--force` is accepted and recorded, spec Decisions row 2) and of `npm --prefix web/reflekt-web audit --omit=dev`.
2. The result of a re-run of spec acceptance 5 — the declared full role in `.claude/verify.json` (`dotnet test Reflekt.slnx && npm --prefix web/reflekt-web ci && npm --prefix web/reflekt-web run build`), whose npm leg is the part the lockfile change reaches.

**Owed at merge:** spec acceptance 4, and the lockfile part of spec acceptance 7.

### Before — `npm --prefix web/reflekt-web audit` (2026-10-23T04:27:51Z, exit 1)

```
# npm audit report

@babel/core  <=7.29.0
@babel/core: Arbitrary File Read via sourceMappingURL Comment - https://github.com/advisories/GHSA-4x5r-pxfx-6jf8
fix available via `npm audit fix`
node_modules/@babel/core

@vue/server-renderer  <=3.5.41
Severity: high
@vue/server-renderer: XSS via missing CR in attribute-name blacklist - https://github.com/advisories/GHSA-g2v6-rqmx-r4w6
Depends on vulnerable versions of vue
fix available via `npm audit fix`
node_modules/@vue/server-renderer
  vue  3.2.13 - 3.5.41
  Depends on vulnerable versions of @vue/server-renderer
  node_modules/vue

baseline-browser-mapping  >=2.0.0 <2.11.0
Severity: moderate
baseline-browser-mapping process termination on invalid input causes denial of service - https://github.com/advisories/GHSA-w5vr-8v7q-w6rv
fix available via `npm audit fix`
node_modules/baseline-browser-mapping

browserslist  <=4.28.6
Severity: high
Browserslist: Unbounded memory growth (no cache eviction) via distinct query results, leading to eventual OOM - https://github.com/advisories/GHSA-c83g-rgw3-j3cx
Browserslist: Uncaught crash / prototype write via untrusted browserslist-stats.json custom stats (normalizeStats) - https://github.com/advisories/GHSA-73wf-gq98-2v4g
fix available via `npm audit fix`
node_modules/browserslist

nanoid  <=3.3.17
Severity: high
nanoid: non-secure generators can loop indefinitely with negative size - https://github.com/advisories/GHSA-28wg-ghj8-5hjv
nanoid: custom generators can loop indefinitely when size is zero - https://github.com/advisories/GHSA-2v37-7h3g-55p8
nanoid: Integer Overflow or Wraparound - https://github.com/advisories/GHSA-xwg4-73v4-xw9w
fix available via `npm audit fix`
node_modules/nanoid

postcss  <=8.5.22
Severity: high
PostCSS: Arbitrary file read and information disclosure via attacker-controlled sourceMappingURL in CSS comments - https://github.com/advisories/GHSA-6g55-p6wh-862q
PostCSS: incomplete fix of GHSA-6g55-p6wh-862q — attacker-controlled sourceMappingURL reads arbitrary .map files when `from` is unset - https://github.com/advisories/GHSA-fxqj-rqcc-2cmp
PostCSS: Path Traversal in Previous Source Map Auto-Loading (sourceMappingURL) leads to Arbitrary .map File Disclosure - https://github.com/advisories/GHSA-r28c-9q8g-f849
fix available via `npm audit fix`
node_modules/postcss

shell-quote  <=1.8.4
Severity: critical
shell-quote quote() does not escape newlines in object .op values - https://github.com/advisories/GHSA-w7jw-789q-3m8p
shell-quote: Quadratic-complexity Denial of Service in `parse()` (CWE-407) - https://github.com/advisories/GHSA-395f-4hp3-45gv
fix available via `npm audit fix`
node_modules/shell-quote

source-map-js  1.0.0 - 1.2.1
Severity: high
source-map-js allows event-loop denial of service through indexed source-map section offsets - https://github.com/advisories/GHSA-68fv-2mgg-jv7q
fix available via `npm audit fix`
node_modules/source-map-js

vite  8.0.0 - 8.1.5
Severity: high
launch-editor: NTLMv2 hash disclosure via UNC path handling on Windows - https://github.com/advisories/GHSA-v6wh-96g9-6wx3
vite: `server.fs.deny` bypass on Windows alternate paths - https://github.com/advisories/GHSA-fx2h-pf6j-xcff
launch-editor: escapeCmdArgs() omits % escaping on Windows - https://github.com/advisories/GHSA-698v-4r3x-3vcv
launch-editor: Editor CLI flag misinterpretation causing arbitrary command execution for some editors - https://github.com/advisories/GHSA-r825-w8mm-5pxw
launch-editor: `fs.existsSync()` existence check can be bypassed on Windows - https://github.com/advisories/GHSA-587h-7wmj-h5pm
Vite: Cross origin script execution through Vite custom middleware - https://github.com/advisories/GHSA-9jrq-w75r-8gcw
Vite: Files outside `server.fs.allow` could be served when project paths match system paths on non-Windows machine - https://github.com/advisories/GHSA-rq7h-c2jc-7f22
fix available via `npm audit fix`
node_modules/vite

ws  7.0.0 - 7.5.10
Severity: high
ws: Memory exhaustion DoS from tiny fragments and data chunks - https://github.com/advisories/GHSA-96hv-2xvq-fx4p
fix available via `npm audit fix`
node_modules/ws

11 vulnerabilities (1 low, 1 moderate, 8 high, 1 critical)

To address all issues, run:
  npm audit fix
```

### Before — `npm --prefix web/reflekt-web audit --omit=dev` (2026-10-23T04:27:51Z, exit 1)

```
# npm audit report

@vue/server-renderer  <=3.5.41
Severity: high
@vue/server-renderer: XSS via missing CR in attribute-name blacklist - https://github.com/advisories/GHSA-g2v6-rqmx-r4w6
Depends on vulnerable versions of vue
fix available via `npm audit fix`
node_modules/@vue/server-renderer
  vue  3.2.13 - 3.5.41
  Depends on vulnerable versions of @vue/server-renderer
  node_modules/vue

nanoid  <=3.3.17
Severity: high
nanoid: non-secure generators can loop indefinitely with negative size - https://github.com/advisories/GHSA-28wg-ghj8-5hjv
nanoid: custom generators can loop indefinitely when size is zero - https://github.com/advisories/GHSA-2v37-7h3g-55p8
nanoid: Integer Overflow or Wraparound - https://github.com/advisories/GHSA-xwg4-73v4-xw9w
fix available via `npm audit fix`
node_modules/nanoid

postcss  <=8.5.22
Severity: high
PostCSS: Arbitrary file read and information disclosure via attacker-controlled sourceMappingURL in CSS comments - https://github.com/advisories/GHSA-6g55-p6wh-862q
PostCSS: incomplete fix of GHSA-6g55-p6wh-862q — attacker-controlled sourceMappingURL reads arbitrary .map files when `from` is unset - https://github.com/advisories/GHSA-fxqj-rqcc-2cmp
PostCSS: Path Traversal in Previous Source Map Auto-Loading (sourceMappingURL) leads to Arbitrary .map File Disclosure - https://github.com/advisories/GHSA-r28c-9q8g-f849
fix available via `npm audit fix`
node_modules/postcss

source-map-js  1.0.0 - 1.2.1
Severity: high
source-map-js allows event-loop denial of service through indexed source-map section offsets - https://github.com/advisories/GHSA-68fv-2mgg-jv7q
fix available via `npm audit fix`
node_modules/source-map-js

ws  7.0.0 - 7.5.10
Severity: high
ws: Memory exhaustion DoS from tiny fragments and data chunks - https://github.com/advisories/GHSA-96hv-2xvq-fx4p
fix available via `npm audit fix`
node_modules/ws

6 high severity vulnerabilities

To address all issues, run:
  npm audit fix
```

## Note 3 — `docs/backlog.md`: F37 status to Done

**Target:** `docs/backlog.md` — the `### 37. Dependency Security Upgrades` entry.
**Source:** the F37 close (team-lead, 2026-10-23).

Replace its status line

> **Status: Spec Ready** | **Spec:** `docs/specs/F37-DependencyUpgrades/definition/spec.md`

with

> **Status: Done** | **Spec:** `docs/specs/F37-DependencyUpgrades/definition/spec.md` | **Summary:** `docs/specs/F37-DependencyUpgrades/delivery/summary.md`

and its description's "SQLitePCLRaw.lib.e_sqlite3 pinned to 2.1.13" with "SQLitePCLRaw.lib.e_sqlite3 resolves to 2.1.13 through the Microsoft.* 10.0.13 bump (no pin needed)". The npm part is done when Note 2 is applied.
