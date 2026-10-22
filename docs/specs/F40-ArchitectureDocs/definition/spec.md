# F40 — Architecture Reference & Conventions Docs

**Kind:** technical, docs only (tech-spec — the architect owns the definition)
**Source:** F34 audit — `docs/specs/F34-ConventionsAudit/delivery/audit-report.md` §§ 5–6; finding rows in `docs/specs/F34-ConventionsAudit/delivery/findings/architecture.md` (AR-*, plus its § 3 as-built inventory) and `findings/deps-tests-conventions.md` (TS-*, CV-*)
**Dependencies:** F35, F36, F37, F38, F39 — the docs describe the code they leave behind
**Rule list:** not applicable — documentation
**Review:** round 1 — `delivery/review-critic.md`
**Status:** Ready
**Plan:** None

---

## Purpose

A student reading this repo's docs today learns a different system from the one in the code: the architecture doc is the v1 *proposal* (5 aggregates and 19 endpoints; the code has 7 and 44), the root `CLAUDE.md` describes a starter template (modules, API gateway, message queue, gRPC — none exist), the sign-in docs describe an allowlist nothing enforces, and the testing and EF conventions describe another app. This feature makes every current-state doc describe the code as it is after F35–F39, and gives the repo a one-page architecture map. Every fact is taken from the final source at build time — the audit's inventory and counts predate F35–F39 and are a starting list, not the answer.

## Scope

Each row's "suggested fix" in the findings file is the required outcome, unless the "Required outcome" column below picks between its options or narrows it.

| Finding(s) | Doc | Required outcome |
|---|---|---|
| AR-04, AR-05…AR-11, AR-13…AR-19, AR-21…AR-24 | Architecture doc | A new as-built `docs/architecture/architecture.md`. Its sections follow the audit's § 3 inventory, re-checked against the final code: solution layout, project reference graph, the 7 aggregates, phase and state machines, phase-action gating, events and their dispatch (transaction-aware, fire-and-forget, failures logged), the feature slices and their endpoints, real-time (hub methods, messages, per-participant delivery, readiness, timers, single-instance constraint), auth (schemes, policies, the access service, guest-token transports, soft kick), persistence (migrations run on every start, seed flags), the error convention and its `code` set (F38), frontend (API layer, stores, optimistic rules — F39), deployment, testing as it is. The `create-architecture-doc` skill is used for its skill inventory and its rule that patterns a skill covers are referenced, not restated; its template's section list does not bind this doc, and the template-only skills (MediatR, gRPC, Redis and the like) appear in the inventory as "not used by this app". The departure is recorded in `known-deviations.md`. `docs/architecture/v1.md` stays as the decision log with its status set to `Accepted — superseded by architecture.md`, and its allowlist and deferred-item text is annotated as superseded where it stands (AR-12, AR-23). |
| AR-25 | One-page map | `docs/architecture/index.md` becomes the one-page map (projects, reference graph, request flow for a mutation, where each concern lives) and `@`-imports `architecture.md` in place of today's `@v1.md`, so the agents that load `index.md` keep getting the architecture; `v1.md` is linked, not imported. |
| AR-01…AR-03, AR-20, AR-28, AR-29, CV-05, CV-06, CV-07, BE-24 | Root `CLAUDE.md` | **Rewritten for Reflekt** (owner, 2026-10-13): real repo tree and reference graph; real tech stack (.NET 10 packages after F37; no MassTransit, gRPC, YARP or compose); real port (prefix 40, service index 1, Azure App Service in production); migration commands from the repo root, with how to install `dotnet-ef` 10.x and what `dotnet ef` needs at design time — **run once and recorded in `implementation.md` before it is written** (the audit could not verify it); cross-cutting as it is; the guardrails kept, "no god folders" qualified to *inside a service project*, and "No secrets in commits" moved here from `project-rules.md`; the domain-events rule with its exception (an event may carry, beside the aggregate, facts not recoverable after the save) — stated here only. The template text stays in git history. |
| AR-01, AR-28, AR-29, TS-07, CV-05 | `docs/conventions/project-rules.md` | The repo tree and reference graph are replaced by a pointer to `CLAUDE.md`; the guardrails it repeats, and its Conventions line on domain events, point to `CLAUDE.md` (the events rule and its exception live there only); its template leftovers ("services, modules, and building blocks tree", "modules, cross-cutting") describe this repo; Build Verification adds `npm --prefix web/reflekt-web run lint` (F39); `docker-compose.*` leaves the "not source files" list; the validator wording says validators live in the slice's `*Command.cs`/`*Query.cs`; the `dotnet ef` commands become the root-relative ones; `npx vitest run` becomes "frontend tests are not set up". |
| (service doc) | `src/Services/Reflekt/CLAUDE.md` | Building blocks used, auth scheme and transports, and its port note corrected to the code. |
| AR-12 | Sign-in | Every current-state doc says any Google account may sign in (owner, 2026-10-13): `CLAUDE.md`, the new docs, `docs/product/v1.md` (its allowlist prose), `v1.md` (annotated as superseded). Old evidence docs are not edited (Decisions). |
| TS-01…TS-09, TS-11, TS-12, DP-06, CV-01 | Testing conventions | `docs/conventions/testing.md` rewritten to the real practice: naming `{Action}_{Condition}_{Outcome}`, with the existing two-part names (`Application_Starts`, `Login_RedirectsToGoogle`) named as the accepted short form; the two test projects and their layout; the real helpers (F38's shared fixture, `TestAuthHandler`, placeholder Google credentials); no mocking library; FluentAssertions kept at 7.2.0 on purpose (later majors are not adopted; the reason is cited from the package's own release notes, or stated without a reason), and xunit 2.x, the test SDK and coverlet kept as they are (F37); frontend testing not set up; the `Speed` trait filter in `.claude/verify.json` documented. `docs/conventions/coding-conventions.md` then `@`-imports `testing.md` beside its current imports and names every conventions file, saying which are loaded elsewhere (`project-rules.md` by the agent files, `known-deviations.md` by the developer agent, `kb-entry-schema.md` for the KB that does not exist). |
| CV-03, CV-04, AR-32, BE-36, SK-23 (team-count race), FE-05 skill drift | Other conventions | `known-deviations.md` keeps its purpose (recurring implementation mistakes) and its table; rows citing another app's features keep their generic lesson and lose the foreign feature ids. A second table, **Accepted deviations**, lists deliberate departures from a skill or rule with what, why and where: fire-and-forget events, single-instance hub state, the two URL schemes, the team-count race, the architecture-doc template departure, the `tailwind-theme` skill's inline-body wording, and every skill/code difference F38 and F39 list in their `implementation.md`. `ef-core.md`'s examples are rewritten with Reflekt types, and its design-time-factory advice is corrected to what the recorded `dotnet ef` run showed. |
| (F36, F38) | `vue.md`, `csharp.md` | `vue.md` § White Flash Prevention describes F36's `<head>` style mechanism instead of the inline body colour. `csharp.md` line 73 ("not found is a result, not an exception") is rewritten to match the exception convention F38 adopted (line 12). |
| AR-27, CV-09…CV-14 | Agent setup docs | `.claude/README.md`: counts fixed; the `docs/kb/` and `docs/skill-backlog.md` mentions footnoted as never created (owner decision 2026-10-12 — the rules stay); the "repo structure in 3 files" design-decision row updated to the new homes; the hooks and `verify.json` listed; a note that the agent files still name `v1.md` as the architecture context (they are not edited). `kb-entry-schema.md` gets the same never-created footnote. No `.claude/agents`, `.claude/rules` or `.claude/skills` file is edited. |
| AR-26 | Code graph | `graphify-out/` regenerated from the final code with a full rebuild (`graphify update . --force`; `graphify` is installed under `D:\tools\graphify-venv`; it honours `.gitignore`, which already excludes `wwwroot/`, `bin/`, `obj/`, `node_modules/`). Fallback when it cannot run: delete the tracked output, say in `README.md` how to generate it, and make the `CLAUDE.md` graph rule conditional on `graphify-out/` existing. |
| AR-30, AR-31, AR-33 | Small fixes | `README.md` links the map and corrects which features have `definition/` folders; `docs/product/index.md` and `docs/product/v2.md` both say `Partially shipped (F23–F32)`; in `docs/backlog.md`, everything above item 23 except the parked item 22 (the preamble "Already agreed" line and sections A–D) is marked historical in one note at its top, which also says the allowlist and `CardVote` statements there were superseded. |

## Decisions (taken — owner or architect)

| Decision | Why | Rejected alternative | Confidence |
|---|---|---|---|
| Root `CLAUDE.md` rewritten (owner, 2026-10-13). | It is the live instruction file every agent reads. | Keep the template text as evidence. | high — owner's pick |
| Sign-in docs say any Google account may sign in (owner, 2026-10-13). | Matches the code. | Enforce the allowlist. | high — owner's pick |
| A new `architecture.md`, `index.md` imports it; `v1.md` kept as the decision log. | `v1.md` is the root of ten of the audit's high-severity drift rows; agents keep their architecture context through `index.md`. | Edit `v1.md` in place; link-only map. | high |
| The architecture-doc skill governs the inventory and the reference-don't-restate rule, not the section list. | Its template forbids the as-built detail this doc exists for, and skills are not edited. | Follow the template strictly. | medium |
| Old evidence docs (`docs/features/`, `ai-coding/`, `docs/plans/`, past `docs/specs/`) are not edited. | Owner: keep them as evidence. | Correct them too. | high — owner's pick |
| Graph fallback is deletion, not a stale graph. | A wrong map misleads more than no map. | Keep the stale output. | medium |

## Out of scope

- Any source, test or config change, except deleting `graphify-out/` under the fallback.
- `.claude/agents`, `.claude/rules`, `.claude/skills`.
- Product scope decisions.

## What must not change

- Old evidence docs (Decisions).
- `CLAUDE.md`'s guardrails in substance (qualified and moved, not dropped) and its authority section.

## Acceptance (checkable)

Greps run from the repository root.

1. `grep -nE "MassTransit|RabbitMQ|gRPC|ApiGateway|docker-compose|Modules/" CLAUDE.md docs/conventions/project-rules.md` → each remaining hit is inside a "not used by this app" note.
2. `grep -rniE "allowlist|AllowedEmails|allowlisted|not.invited" CLAUDE.md README.md docs/architecture docs/product docs/conventions docs/backlog.md` → every hit either sits in text marked superseded or historical, or states that no allowlist exists (backlog item 35 is one such line); none says sign-in is limited to a list.
3. `grep -nE "Ticket|IssueType|TestData\.|BugRatioTarget|SprintIssueSyncService|CreateDefault|Memberships" CLAUDE.md docs/conventions/*.md` → no hits; every code example in `docs/conventions/` names a Reflekt type (reviewed and listed in `implementation.md`).
4. `grep -n "^@" docs/conventions/coding-conventions.md` shows `@testing.md` beside the existing imports, and the file names all eight conventions files; `grep -n "^@" docs/architecture/index.md` → `@architecture.md`.
5. `implementation.md` records, per section of `architecture.md`, the final commit it was checked against and how (file listing, endpoint list, hub method list, the error `code` set); the counts in the doc match that listing.
6. Either `graphify-out/GRAPH_REPORT.md`, `graph.json` and `manifest.json` were regenerated after F39's merge, the report's god nodes and community names hold no `wwwroot` bundle, `HelloWorld` or allowlist entry, and its code nodes (`file_type: code`) include `User` and `CardGroup` and no allowlist code — document nodes from the unedited evidence docs are expected; or `graphify-out/` is gone and `README.md` says how to generate it.
7. The feature's change set — `git diff --name-only {base}` (the commit the feature started from, recorded in `implementation.md`) plus untracked files from `git status --porcelain` — holds only `*.md` files outside `docs/features/`, `ai-coding/`, `docs/plans/`, past `docs/specs/*` (except this feature's own) and `.claude/` (except `.claude/README.md`), plus `graphify-out/`.
8. The other rows, by grep: `docs/conventions/vue.md` mentions the `<style>` mechanism and no inline body colour; `docs/conventions/csharp.md` no longer says "not found" is a result rather than an exception; `docs/conventions/known-deviations.md` has an `Accepted deviations` table and no `F28-`, `F30-`, `F31-`, `F32-` or `adhoc-Suite` ids; `docs/architecture/v1.md` status reads superseded; `docs/product/index.md` and `v2.md` read `Partially shipped`; `docs/backlog.md` opens with the historical note; `docs/conventions/project-rules.md` has `run lint` and no `vitest`; `.claude/README.md` gives the real skill count; `implementation.md` holds the recorded `dotnet ef` run and the list of `architecture.md` inventory entries marked "not used by this app".
