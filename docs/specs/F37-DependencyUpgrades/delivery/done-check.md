# F37 — Dependency Security Upgrades — Done Check

## Step 1 — Done-Check

### Check 1 — 2026-10-23 — after the build

**Model:** claude-opus-5-5[1m]

Lane: light (light-format plan; steps graded on outcome and acceptance evidence). Suite: complete run pending — not waited for; this check ran targeted reads only.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — AngleSharp at 1.8.4, link previews unchanged | Implemented | `Directory.Packages.props` diff: `AngleSharp` 1.2.0 → 1.8.4. Step block records the base commit, a real saved page (`github.com/dotnet/runtime`) run through `ParseOpenGraphAsync` by reflection on 1.2.0 and on 1.8.4 — `Ok = True`, all four fields non-null and identical, both assembly versions printed. `--vulnerable` lists no AngleSharp line. Step close fast record `mvkgl784-5926ba55` pass. |
| 2 — The Microsoft.* family on one 10.0.x patch, whole solution green | Superseded | The step's Accept was amended mid-build by the flaky-test rule after `questions.md` Q3 (Answered). Under the amended Accept: all seven entries at `10.0.13` in the diff, no `9.` version left, `Microsoft.NET.Test.Sdk` still 17.14.1; lookup command and UTC time recorded; full role red once (`mvkgn2o8-461c60a6`, one host-start failure in `CardEndpointTests`), green on the unchanged re-run (`mvkgpdv7-eb5d9956`, same tree hash `4dcd021f`); the flaky test is recorded with both run ids and stack frames, carry-over row present, backlog row in `change-notes.md` § Note 1. No forced fix, no source file changed. Pass with note. |
| 3 — SQLite native library on a fixed version; restore and build clean | Implemented | `SQLitePCLRaw.lib.e_sqlite3` resolves 2.1.13 in every project, so no pin and no re-run owed. Vulnerable list clean for all nine projects (spec acceptance 1); NU1901–1904 count 0 (acceptance 2); `sqlite_version()` 3.53.3 through `ReflektDbContext`'s connection via `AddReflektPersistence`. The probe reached Persistence through its API project reference rather than a direct one — same package graph, recorded under Decisions; not a deviation on a light step. Step close `mvkgxf87-e75f192c` pass. |
| 4 — The npm fix handed to the campaign merge as a change note | Implemented | `change-notes.md` § Note 2 holds target, command with no `--force`/`--legacy-peer-deps`/`overrides`, the expected results (vue ≥ 3.5.42, `package.json` unchanged, `--omit=dev` 0, `package-lock.json` only), the ordering rule, the acceptance-5 re-run duty, and both "before" audit outputs. `git status --porcelain web/reflekt-web` empty (re-confirmed now). Spec acceptance 4 and the lockfile part of acceptance 7 marked owed at merge — not Missing (Q1, AR-1). |
| 5 — The change set holds only the allowed files | Implemented | Both listings pasted. Re-run now: the diff holds `Directory.Packages.props` (8 version lines), `definition/spec.md` (the `**Plan:**` line only — confirmed by diff), and `delivery/` files; status adds `.claude/.personas.json` and `.claude/tmp/` (campaign artifacts, allowed by the plan) and the checks' own `review.md`/`lessons.md`/`reader-pair.md` writes under `delivery/`, all allowed. `out/` absent. |

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist (no `Satisfies:` rules; spec `Rule list: not applicable`).

**Skill conformance:** scoped by token `developer:implement`, session `e1039f04-…`, in `.claude/audit/skill-invocations.log` (worktree home; the main checkout's log holds no line for this session): `central-package-management` (04:16:28Z, 04:25:35Z), `framework-currency` (04:18:14Z) — every non-`None` mapping (Steps 1, 2, 3) logged; Steps 4 and 5 are `None`. `## Skills Used` matches the log.

**Test entry:** no `test-entry` line for this session in either home's `violations.log` (the worktree has none; the main checkout's has no line for this session).

**Acceptance records:** every step close is a fast-profile `verify-run.js` record with verdict `pass` at `limit_ms` 15000, inside its step's window per `handoff.jsonl`; Step 2's full-role records are cited by id. The steps' acceptances are grep/command checks, whose results sit in the step blocks.

**Notes:**
- Plan hygiene: `## Decisions` (10 rows) and `## Decisions and why` (P1, P2) present and non-silent.
- Carried for the close, not a step failure: the flaky `CardEndpointTests.CreateCard_ActionItem_Returns201` host start (exception message not captured) — Q3's tripwire sends a repeat in the handover's complete run to a solo.
- Owed at merge (not this unit's to discharge): spec acceptance 4 and the lockfile part of acceptance 7 via `change-notes.md` § Note 2; `docs/backlog.md` row via § Note 1.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-23*
