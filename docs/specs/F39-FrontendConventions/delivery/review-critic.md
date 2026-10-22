## Round 1 — 2026-10-22 — spec — primary (codex)

Header note: two readers — Codex primary (read-only job, verdict persisted verbatim in `review-critic-codex.md`) and a Sonnet nexus critic as second reader; pair `codex + sonnet`, picked by the owner. The spec had been committed as Ready without review; the owner caught it and it was set back to Draft before this round.

Primary reader: see `review-critic-codex.md` — `## Verdict: NO-GO`, 2 HIGH, 6 MEDIUM, 1 LOW.

## Second reader — round 1 (sonnet)

Verdict: REVISE. (Condensed by the architect, not verbatim: every finding is kept with its level, evidence and fix; the reader's full message is in the session transcript.)

- **[HIGH, band HIGH–MEDIUM] F39-01 — Optimistic-UI scope open; contradicts F36 and "what the user sees stays the same".** No list of which actions become optimistic; no reconcile rule for the HTTP response vs the `CardCreated` echo (duplicates); F36's draft restore vs an immediate clear; group drag going instant is undisclosed.
- **[MEDIUM, band MEDIUM–HIGH] F39-02 — `apiFetch` "injects the guest token" vs "each call keeps today's headers".** 18 of 42 calls send no token today; a stale token on an owner's admin call could hit F38's wrong-sprint check; credentials are same-origin, so `include` everywhere is safe.
- **[MEDIUM] F39-03 — `ApiError`/`describeApiError` underspecified** (only two codes; status-keyed join messages; validation `errors`; fallback order).
- **[MEDIUM] F39-04 — Undisclosed visible changes** (BaseButton sizes, new tokens, heading, `EmptyState`, icons).
- **[MEDIUM] F39-05 — F36's outcomes not in "must not change"; no re-check.**
- **[MEDIUM, band MEDIUM–HIGH] F39-06 — Several rows unchecked; walkthrough covers only the retro room.** Proposed greps for `@apply`, `<svg`, `confirm(`, lazy routes, `RetroColumnBody`, store mutation outside stores.
- **[LOW, band LOW–MEDIUM] F39-07 — Acceptance 1 cannot go green (`policyStore.fetch`); 42 calls, not 44; 20 props, not 19.**
- **[LOW] F39-08 — Lint/format enforcement and the reformat's sequencing unspecified.** Route: plan must resolve.
- Plan must resolve: token source without an `api/` → `stores/` cycle; empty/non-JSON bodies; setup-store conversion details; FE-25 per item; modal focus depth; FE-30 items; `verify.json` re-measure.

Coverage ledger (second reader): setup stores, types, component size, dead code, F36 allocation, siblings COVERED; API layer, optimistic UI, design system, accessibility, tooling, acceptance, F38 body PARTIAL; four frontend skills NOT OPENED (audit citations relied on).

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | sonnet, codex | HIGH | seed-0007 | Optimistic scope open; echo reconcile; F36 draft rule; revert overwrites broadcasts | open scope decision | builds differ; F36 fix undone; lost updates | quoted, path | — | scope | Scope row 3 | F39-01, codex-2 |
| R1-02 | codex | HIGH | unranked — Codex severity HIGH | Report unification drops in-room content (discussed badges, single-card text) | undisclosed content change | visible information removed | path | — | scope | Scope row 5 | codex-1 |
| R1-03 | sonnet, codex | MEDIUM | seed-0022 | Guest-token injection vs per-call headers; credentials check | internal contradiction | owner refused on admin pages | quoted, scenario | — | scope | Scope row 1 | F39-02, codex-5 |
| R1-04 | sonnet, codex | MEDIUM | seed-0022 | `ApiError`/`describeApiError` precedence and fields | cross-spec contract gap | error text regresses | quoted | — | scope | Scope row 1 | F39-03, codex-3 |
| R1-05 | codex | MEDIUM | unranked — Codex severity MEDIUM | 204 and empty bodies unhandled | contract gap | successful mutation reported failed, rolled back | path | — | scope | Scope row 1 | codex-4 |
| R1-06 | sonnet | MEDIUM | seed-0021 | Visible changes undisclosed | undisclosed scope change | owner approves "no change" that is false | quoted | — | scope | Purpose, What must not change | F39-04 |
| R1-07 | sonnet | MEDIUM | seed-0129 | F36 outcomes not protected | sibling coordination | F36 fixes undone silently | path | — | sibling | What must not change | F39-05 |
| R1-08 | sonnet, codex | MEDIUM | seed-0310 | Walkthrough narrow; rows unchecked; roles and stored sessions | acceptance does not prove outcome | regressions ship | quoted | — | acceptance | AC | F39-06, codex-5, codex-8 |
| R1-09 | codex | MEDIUM | unranked — Codex severity MEDIUM | Accessibility acceptance unproven (keyboard) | acceptance does not prove outcome | keyboard access still absent | path | — | acceptance | Scope a11y row, AC | codex-6 |
| R1-10 | sonnet, codex | MEDIUM | seed-0310 | AC1 fetch grep cannot go green | acceptance mismatch | check never passes | path | — | acceptance | AC1 | F39-07, codex-7 |
| R1-11 | sonnet, codex | LOW | seed-0127 | Counts: 42 calls, 20 props | count off | — | path | — | count | Purpose, Scope | F39-07, codex-9 |
| R1-12 | sonnet | LOW | seed-0310 | Lint/format sequencing | mechanics | reformat mixed into logic | quoted | — | plan | Scope tooling row | F39-08 |
| R1-13 | — (architect, from F37 review) | LOW | seed-0129 | No audit check after new dev dependencies | cross-spec hand-off | new advisories unseen | path | — | sibling | AC | F37-07 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-22)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | Scope row 3 (optimistic list, echo rule, per-item revert, F36 draft rule, server-authoritative list); Decisions row 2 |
| R1-02 | fixed-real | — | — | Scope row 5 (keeps both reports' content); Decisions row 1 |
| R1-03 | fixed-real | — | — | Scope row 1 (credentials always include; token opt-in on today's 24 calls; getter, no store import); AC3 |
| R1-04 | fixed-real | — | — | Scope row 1 (`ApiError` fields; precedence) |
| R1-05 | fixed-real | — | — | Scope row 1 (204 / empty / non-JSON) |
| R1-06 | fixed-real | — | — | Decisions row 1; Purpose; What must not change |
| R1-07 | fixed-real | — | — | What must not change (F36 list); AC7 |
| R1-08 | fixed-real | — | — | AC5, AC6, AC9 |
| R1-09 | fixed-real | — | — | Scope a11y row; AC9 keyboard |
| R1-10 | fixed-real | — | — | Scope FE-01 row (`fetch` → `load`); AC1 |
| R1-11 | fixed-real | — | — | Purpose; Scope row 5 |
| R1-12 | fixed-real | — | — | Scope tooling row (reformat first; lint runs prettier --check) |
| R1-13 | fixed-real | — | — | AC8 |

Reconciled: Scope row 1 ↔ F38 Scope row 1 (body fields) ↔ AC3; Scope row 3 ↔ F36 Scope FE-07 ↔ What must not change; Decisions row 1 ↔ Purpose ↔ What must not change; AC7 ↔ F36 Acceptance 1–5; AC8 ↔ F37 Purpose.

## Round 2 — 2026-10-23 — spec — primary (codex)

Header note: Codex alone. Read: delta. Floor: MEDIUM. Verdict and fold-verification table persisted verbatim in `review-critic-codex.md` § Round 2: eleven closed; R1-06 and R1-08 partial; one new finding.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — Codex severity MEDIUM | Visible-change list omits the kick dialog | internal contradiction | required change breaks the appearance guarantee | quoted | — | fold | Decisions row 1 | R2-01, R1-06 |
| R2-02 | codex | MEDIUM | unranked — Codex severity MEDIUM | Request check samples and omits payloads; hub handlers, type use and report adoption unverified | acceptance does not prove outcome | regressions unseen | quoted | — | fold | AC9 | R1-08 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | Decisions row 1 |
| R2-02 | fixed-real | — | — | AC9 (every API function incl. payload; structure greps recorded) |

Reconciled: Decisions row 1 ↔ What must not change ↔ Scope a11y row. No round 3: no CRITICAL.
