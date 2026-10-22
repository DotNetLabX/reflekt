# F40 — Architecture Reference & Conventions Docs

**Kind:** technical, docs only (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` §§ 5–6; finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/architecture.md` (AR-*, plus its § 3 as-built inventory) and `findings/deps-tests-conventions.md` (TS-*, CV-*)
**Dependencies:** F38, F39 (the docs describe the finished code); F37 (the stack versions)
**Rule list:** not applicable — documentation
**Status:** Ready
**Plan:** None

---

## Purpose

A student reading this repo's docs today learns a different system from the one in the code: the architecture doc is the v1 *proposal* (5 aggregates and 19 endpoints; the code has 7 and 44), the root `CLAUDE.md` describes a starter template (modules, API gateway, message queue, gRPC — none exist), the sign-in docs describe an allowlist nothing enforces, and the testing and EF conventions describe another app. This feature makes every current-state doc describe the code as it is after F35–F39, and gives the repo a one-page architecture map.

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below picks between its options or narrows it. Facts are re-checked against the code at build time — the audit's inventory predates F35–F39.

| Finding(s) | Doc | Required outcome |
|---|---|---|
| AR-04, AR-05…AR-11, AR-13…AR-19, AR-21…AR-24 | Architecture doc | A new as-built `docs/architecture/architecture.md`, written with the `create-architecture-doc` skill from the code after F38/F39: solution layout, project reference graph, the 7 aggregates, phase and state machines, phase-action gating, events and their dispatch (fire-and-forget), the 12 slices / endpoints, real-time (hub methods, events, readiness, timers, single-instance constraint), auth (schemes, policies, ownership check, guest-token transports), persistence (migrations run on every start, seed flags), cross-cutting (error convention from F38), frontend, deployment, testing as it is. `docs/architecture/v1.md` stays as the decision log with its status set to `Accepted — superseded by architecture.md`; its sections 12–13 marked resolved where the code answered them (AR-23). |
| AR-25 | One-page map | `docs/architecture/index.md` becomes the one-page map (projects, reference graph, request flow for a mutation, where each concern lives) linking to `architecture.md` and `v1.md`. |
| AR-01…AR-03, AR-20, AR-28, AR-29, CV-05, CV-06, CV-07, BE-24 | Root `CLAUDE.md` | **Rewritten for Reflekt** (owner, 2026-10-13): real repo tree and reference graph, real tech stack (.NET 10 packages after F37; no MassTransit/gRPC/YARP/compose), real port (prefix 40, service index 1, App Service in production), migration commands from the repo root with how to install `dotnet-ef` and the user-secrets prerequisite, cross-cutting as it is, the guardrails kept (qualified: no god folders *inside a service project*), the domain-events rule with its stated exception (aggregate plus facts not recoverable after save). Guardrails live in `CLAUDE.md`; `docs/conventions/project-rules.md` points at them instead of repeating the tree. The template text stays in git history. |
| AR-12 | Sign-in | Every current-state doc that describes the allowlist says any Google account may sign in (owner, 2026-10-13): `docs/architecture/v1.md` (marked superseded), `docs/product/v1.md`, the new docs, `CLAUDE.md`. Feature docs under `docs/features/` and old specs are evidence and are not edited. |
| TS-01…TS-09, TS-11, TS-12, DP-06, CV-01 | Testing conventions | `docs/conventions/testing.md` rewritten to the real practice: naming `{Action}_{Condition}_{Outcome}`, the two test projects and their layout, the real helpers (the F38 shared fixture, `TestAuthHandler`, placeholder Google credentials), no mocking library, FluentAssertions 7.2.0 kept deliberately (licence), frontend testing not set up, the `Speed` trait filter in `.claude/verify.json` documented. Then imported by `coding-conventions.md`, which lists every conventions file and states which are loaded elsewhere. |
| CV-03, CV-04, AR-32 | Other conventions | `known-deviations.md` cleared of another app's rows and reseeded with this repo's real deviations (fire-and-forget events, single-instance hub state, the domain-event exception, the two URL schemes from BE-36, any skill/code drift F38/F39 left); `ef-core.md` examples re-written with Reflekt types. |
| AR-27, CV-09…CV-14 | Agent setup docs | `.claude/README.md` counts fixed and its `docs/kb/` and `docs/skill-backlog.md` mentions footnoted as never created (owner decision 2026-10-12 — the rules stay). No `.claude/agents`, `.claude/rules` or `.claude/skills` file is edited. |
| AR-26 | Code graph | `graphify-out/` regenerated from the current code with `wwwroot/`, `bin/`, `obj/`, `node_modules/` excluded. Fallback when `graphify` cannot run at build time: delete the stale tracked output, keep `CLAUDE.md`'s graphify rule conditional ("if `graphify-out/` exists"), and say in `README.md` how to generate it. |
| AR-30, AR-31, AR-33 | Small fixes | `README.md` links the map; `docs/product/index.md` marks v2 `Partially shipped (F23–F32)`; `docs/backlog.md` section A marked historical. |

## Decisions (taken — owner or architect)

| Decision | Why | Rejected alternative | Confidence |
|---|---|---|---|
| Root `CLAUDE.md` rewritten (owner, 2026-10-13). | It is the live instruction file every agent reads. | Keep the template text as evidence. | high — owner's pick |
| Sign-in docs say any Google account may sign in (owner, 2026-10-13). | Matches the code; no code change. | Enforce the allowlist. | high — owner's pick |
| A new `architecture.md` instead of editing `v1.md` in place; `v1.md` kept as the decision log. | 14 high-severity drift rows trace to `v1.md`; the conflict log in it is worth keeping. | Edit `v1.md` in place. | high |
| Old feature docs (`docs/features/`, `ai-coding/`, `docs/plans/`, past `docs/specs/`) are not edited. | Owner: keep them as evidence. | Correct them too. | high |
| Graph fallback is deletion, not a stale graph. | A wrong map misleads more than no map. | Keep the stale output. | medium |

## Out of scope

- Any source, test or config change, except deleting `graphify-out/` under the fallback.
- Project skills, agents and rules under `.claude/`, other than `.claude/README.md`.
- Product scope decisions.

## What must not change

- Old evidence docs (see Decisions).
- `CLAUDE.md`'s guardrails in substance (they are qualified and deduplicated, not dropped) and its authority section.

## Acceptance (checkable)

1. `grep -nE "MassTransit|RabbitMQ|gRPC|ApiGateway|docker-compose|Modules/" CLAUDE.md docs/conventions/project-rules.md` → each remaining hit is in a "not used" note.
2. `grep -rn "AllowedEmails" CLAUDE.md docs/architecture docs/product docs/conventions README.md` → only in `v1.md`'s superseded text, marked as such.
3. `grep -nE "Ticket|IssueType|TestData\.|NSubstitute|_sut|Should_" docs/conventions/*.md` → no hits.
4. `docs/conventions/coding-conventions.md` names all eight conventions files.
5. `docs/architecture/index.md` links `architecture.md`; every project, aggregate and feature-slice folder in the code appears in `architecture.md` (the plan pins the check, e.g. a listing compared against the doc).
6. `graphify-out/GRAPH_REPORT.md` is dated after F39's merge, or `graphify-out/` is gone and `README.md` says how to generate it.
7. `git diff --stat` touches only `*.md` files and `graphify-out/`.
