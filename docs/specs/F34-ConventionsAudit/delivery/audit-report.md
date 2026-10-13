# F34 — Conventions & Architecture Audit — Report

**Model:** claude-opus-5-5 (synthesis); five read-only auditors on sonnet
**Date:** 2026-10-13 · **Tree:** branch `F34-ConventionsAudit`, stacked on `F33-PublishCleanup` (`a252cdf`)
**Scope (owner rule):** cleanup, code quality, architecture reference/map, code conventions — no new features, no new tests, no business-rule registries.

## How it was run

Five independent read-only auditors, one per dimension, each verifying every finding against the code (build run where cheap; no tests run; no repo edits). Their full finding tables are kept as evidence in `findings/`:

| Dimension | File | Findings (H / M / L) |
|---|---|---|
| Backend code vs written conventions (`CLAUDE.md`, `docs/conventions/*`) | `findings/backend.md` | 38 (6 / 16 / 16) |
| Backend code vs the pattern skills (`.claude/skills/*`) | `findings/skills.md` | 30 (2 / 15 / 13) |
| Frontend code vs conventions + frontend skills | `findings/frontend.md` | 43 (10 / 22 / 11) |
| Architecture docs vs the code, + an as-built inventory | `findings/architecture.md` | 33 (14 / 12 / 7) |
| Dependencies, tests vs `testing.md`, conventions-set hygiene | `findings/deps-tests-conventions.md` | 39 |

About 180 rows; after removing overlaps between dimensions (e.g. `BE-01` = `SK-01`, `BE-05` ≈ `SK-02`/`SK-06`, `BE-03` ≈ `SK-13`), roughly 140 distinct issues.

## What is already right

No repository interfaces, no entity-wrapper services, no god folders, no endpoint touches `DbContext` directly, no sync-over-async, aggregates created through factories wherever guards or events exist, 0 compiler warnings, type-check clean. The vertical-slice shape (FastEndpoints feature folders) is consistently used.

## The findings, by theme

### 1. Real defects (behaviour is wrong today)
- **Domain events can be lost or double-published** — the publishing interceptor is a singleton holding per-save state (`Reflekt.Persistence/DependencyInjection.cs:18`); the skill registers it scoped. `BE-01`/`SK-01`.
- **Writing-phase anonymity leaks** — the list endpoint hides other people's card text, but every card SignalR event sends full text to the whole sprint; the client only blurs it. `BE-02`.
- **Domain-rule violations return 500** — aggregates throw `InvalidOperationException`, which the middleware maps to 500 with the raw message; one concrete reachable 500 on previous-action review. `BE-07`, `SK-04`, `SK-05`.
- **Raw exception text returned in production** (EF/SQLite messages). `BE-20`, `SK-07`.
- **Kicking a participant who wrote a card or voted** should fail on a restrict foreign key (from config; no test). `BE-13`, `SK-17`.
- **Non-atomic multi-save sequences** can leave orphan groups / half-advanced phases. `BE-12`, `SK-14`.
- **A closed sprint can still be edited** through endpoints that skip the state check. `BE-14`.
- **Open redirect** after sign-in (`/\` prefix accepted) and a narrow SSRF guard in the link-preview fetcher. `BE-21`, `BE-23`.
- **Frontend:** rounded corners silently dropped everywhere (Tailwind v3 bracket syntax under v4, 50 sites) `FE-04`; light theme keeps a dark page background on three views `FE-05`; note text lost when a save fails `FE-07`; the sprint view hangs on "Loading" when a fetch fails `FE-08`; card tint class never generated `FE-12`; the timer chime is silent `FE-13`; a focused group in dark theme gets a near-white background `FE-20`.

### 2. Security hygiene in dependencies
- AngleSharp 1.2.0 (advisory GHSA-pgww-w46g-26qg) → 1.8.4, minor; SQLitePCLRaw.lib.e_sqlite3 2.1.10 (GHSA-2m69-gcr7-jv3q, high) → pin 2.1.13, patch; `npm audit fix` clears 6 high (no major bumps; mostly build-time). Microsoft.* runtime packages are 9.0.4 on a `net10.0` target (10.0.12 available) — a family bump, separate risk. FastEndpoints 5→8 is breaking: out of scope. `findings/deps-tests-conventions.md` DP-*.

### 3. Backend drifts from its own conventions and skills (no behaviour change intended)
- **Four error idioms, none the documented one**; the exceptions/guards building blocks are unused. `BE-05`, `SK-02`, `SK-06`, `BE-09`.
- **Two broadcast mechanisms** — endpoints send SignalR messages directly and wipe the domain events, leaving three event handlers dead. `BE-03`, `SK-13`.
- **Resource-ownership check pasted into 30 files** in four variants; guest claims parsed by magic string 36 times. `BE-04`, `SK-08`, `SK-09`.
- **Business rules in endpoints, not aggregates** (vote budget, one vote per group, phase transitions). `BE-06`, `BE-15`, `SK-12`.
- Endpoint/request/response layout mixed three ways; 9 validators for 20 mutating endpoints; enum strings parsed loosely. `SK-10`, `SK-11`, `BE-19`.
- Duplicate report builders, dead code, EF leaking into the API layer, tracked read queries, long handlers. `BE-08`, `BE-10`, `BE-11`, `BE-16`–`BE-18`, `SK-15`, `SK-16`.

### 4. Frontend drifts from its own conventions and skills
- Options-style stores where the skill says setup stores only; no API client layer (44 raw `fetch`, guest-token header pasted 24 times); optimistic UI in three different layers; components over the size caps (305 and 268 lines); two generations of UI (raw buttons vs `Base*` components); stringly-typed phases/columns; dead leftovers; very little accessibility markup. `FE-01`–`FE-03`, `FE-06`, `FE-09`, `FE-10`, and the rest of `findings/frontend.md`.

### 5. Architecture reference is out of date
- `docs/architecture/v1.md` is still the v1 *proposal*: misses features 23–32 (User, CardGroup, timers, open actions, Google participants), lists 5 aggregates / ~19 endpoints (code: 7 / 44), wrong phase list, says migrations run in Development only (they run everywhere).
- The documented `Auth:AllowedEmails` allowlist is not enforced by any code — any Google account can sign in.
- Root `CLAUDE.md` is largely starter-template text (modules, API gateway, MassTransit, gRPC, docker-compose — none exist; port table "TBD"); `docs/conventions/project-rules.md` repeats the same stale tree.
- `graphify-out/` is stale (2026-04-21; god nodes are minified JS from an old bundle).
- `findings/architecture.md` ends with an **as-built inventory** (projects, 7 aggregates, 12 slices / 44 endpoints, hub, auth schemes and policies, persistence, 13 stores) from which an accurate architecture doc and one-page map can be written.

### 6. Conventions set hygiene
- `coding-conventions.md` imports 3 of 8 convention files; `testing.md` mostly describes another app's helpers (0 of ~167 tests follow its naming rule); `known-deviations.md` cites features from another app whose IDs collide with this repo's F30–F32; `.claude` rules point at a `docs/kb/` that was never created.

## Proposed fix features

Each is cleanup / code quality / architecture reference / conventions — no new product behaviour, no new tests (existing tests are updated where a fix changes an asserted behaviour). **IDs are proposals — they need the owner's confirmation before any planning.**

| Proposed ID | Name | Covers (themes) | Kind | Size | Depends on |
|---|---|---|---|---|---|
| F35 | Backend Correctness & Security Fixes | 1 (backend rows), allowlist decision | backend bugfix | M | F33 |
| F36 | Frontend Visual & UX Defects | 1 (frontend rows) | frontend bugfix | S–M | F33 |
| F37 | Dependency Security Upgrades | 2 | packages | S | F33 |
| F38 | Backend Conventions Conformance | 3 | refactor, no behaviour change | L (needs build slices) | F35, F37 |
| F39 | Frontend Conventions Conformance | 4 | refactor, no behaviour change | L (needs build slices) | F36 |
| F40 | Architecture Reference & Conventions Docs | 5, 6 (as-built doc, one-page map, CLAUDE.md, conventions index, testing.md, graph refresh) | docs | M | F38, F39 |

**Order / parallelism:** two lanes can run side by side — backend (F37 → F35 → F38) and frontend (F36 → F39) — because they touch disjoint files; F40 runs last so the docs describe the finished code. That is a fit for a campaign (several features built in parallel, each in its own branch), which the owner starts.

## Decisions the owner needs to make (not taken while away)

1. **Confirm the six feature IDs and their grouping** (or merge/split them). Recommendation: as proposed. Confidence: medium — F38/F39 are large; splitting each into two is defensible.
2. **Login allowlist:** enforce `Auth:AllowedEmails` in code (F35), or correct the docs to say any Google account may sign in (F40)? Recommendation: correct the docs — the multi-team features moved the app to open sign-up with per-user team limits. Confidence: medium — depends on how students will deploy it.
3. **Root `CLAUDE.md`:** rewrite it to describe this app (F40), or keep it as the starter-template evidence? Recommendation: rewrite — it is the live instruction file every agent reads, and wrong instructions mislead students' agents; the original stays in history. Confidence: medium.
4. **Microsoft.* 9.0.4 → 10.0.x family bump** inside F37, or later? Recommendation: inside F37, as its own step behind the full suite. Confidence: medium.
5. **Run F35–F40 as a campaign** (parallel lanes, unattended) or one at a time? Recommendation: campaign for the two lanes. Confidence: medium.
6. **The history scrub and publish** (one-way): after the fixes, scrub the credential file, `deploy.zip`, the database, `.omc/` and `settings.local.json` from every commit into a fresh copy, then push to a new repo.
