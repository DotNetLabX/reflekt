## Round 1 — 2026-10-22 — spec — primary (codex)

Header note: two readers — Codex primary (read-only job, verdict persisted verbatim in `review-critic-codex.md`) and a Sonnet nexus critic as second reader; pair `codex + sonnet`, picked by the owner. The spec had been committed as Ready without review; the owner caught it and it was set back to Draft before this round.

Primary reader: see `review-critic-codex.md` — `## Verdict: NO-GO`, 5 HIGH, 6 MEDIUM, 1 LOW.

## Second reader — round 1 (sonnet)

Verdict: REVISE. (Condensed by the architect, not verbatim: every finding is kept with its level, evidence and fix; the reader's full message is in the session transcript.)

- **[HIGH, band HIGH–MEDIUM] F38-01 — "Same check order everywhere" contradicts "no status code changes".** `EditCard` returns 409 (closed) before 403; `AttachEvidenceLink` 409 phase before 403; `GetSprint` 403 before sprint 404. Unifying changes statuses; the card-author check is not covered by a sprint/team/role result. Fix: scope the order or list accepted changes.
- **[MEDIUM, band MEDIUM–HIGH] F38-02 — Error-body contract half-defined, unchecked, false premise.** No test asserts any body; `errors[]` item shape, the `code` set and the Development `Details` undefined; auth-layer 401/403 are bodyless. Fix: pin the shape, state bodyless auth responses, add one body assertion.
- **[MEDIUM] F38-03 — Un-suppressing events adds SignalR messages.** Dissolve and RemoveCardFromGroup solo groups would each broadcast `GroupCreated`; `GroupDissolved` has no event. Fix: no new messages; name `GroupDissolved`.
- **[MEDIUM, band MEDIUM–HIGH] F38-04 — Moved rules cannot keep their 400.** Domain references only `Blocks.Domain`; `DomainException` maps to 409 under F35. Fix: state the exception per moved rule.
- **[MEDIUM, band MEDIUM–HIGH] F38-05 — "AsNoTracking on read-only repository methods" matches no method.** `GetBySprintIdAsync` has 22 callers, mutating ones included. Untouchable: silent-wrong-output. Fix: new read variants.
- **[MEDIUM] F38-06 — "Publish after commit" is claimed by F35 and F38.**
- **[MEDIUM] F38-07 — The hub cannot use a throwing access check without changing messages** (`HubException` text shown in toasts).
- **[MEDIUM] F38-08 — Acceptance greps cannot pass or pass vacuously** (#4 hits the csproj and bin/obj, collides with the moved migrate call; #6 `**` not recursive and misses responses; #3 misses the hub); no schema-change guard.
- **[MEDIUM] F38-09 — `csharp.md:12` vs `:73` contradiction; no feature reconciles it.**
- **[MEDIUM] F38-10 — Seams with F35:** "BE-15 (rule part)" undefined; BE-23 truncation constants unassigned; F35's closed-sprint message must be preserved.
- **[LOW] F38-11 — Wording slips:** `TimerEntry` is internal; 6 `*EndpointTests`, not 8; backlog says "No behaviour change"; domain polish adds guards.
- **[LOW] F38-12 — "Handler failures are logged" has no implementing row.** Route: plan must resolve.

Coverage ledger (second reader): SPA and test consumers of error bodies, dead code/EF/hub/timer/naming, TS-10, siblings COVERED; error convention, broadcast, access, rules, reads, seams, acceptance PARTIAL; domain aggregates, persistence configs and several skills NOT OPENED (findings text used).

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | seed-0007 | Unified check order contradicts "no status changes" | two requirements cannot both hold | statuses change silently | quoted, scenario | — | scope | Scope row 3, Decisions | codex-1, F38-01 |
| R1-02 | codex | HIGH | unranked — Codex severity HIGH | Access service has no permission contract (anonymous endpoints, identity precedence, F36's rule) | contract missing | centralisation changes who is allowed | path | — | scope | Scope row 3 | codex-2 |
| R1-03 | codex | HIGH | unranked — Codex severity HIGH | Fire-and-forget turns a post-commit 500 into success | undisclosed status change | contradicts the status invariant | path | — | scope | Scope row 2, Decisions | codex-3 |
| R1-04 | codex, sonnet | HIGH | contested: HIGH / MEDIUM — settled HIGH: F39 builds on this contract | Error contract incomplete and unchecked | under-specified cross-feature contract | F39 builds on an unverifiable shape | quoted, path | — | scope | Scope row 1, Decisions | codex-4, F38-02 |
| R1-05 | codex, sonnet | HIGH | contested: HIGH / MEDIUM — settled HIGH: removing broadcasts could pass every check | Broadcast replacement unverified; extra messages; payload order; `GroupDissolved` | behaviour-neutral claim not pinned | clients get different messages | path, scenario | — | acceptance | Scope row 2, AC | codex-5, codex-6, F38-03 |
| R1-06 | codex | MEDIUM | unranked — Codex severity MEDIUM | "Remaining" validation undefined after F35; route-id validators would turn 404 into 400 | scope undefined | status changes | path | — | scope | Scope validation row | codex-7 |
| R1-07 | codex, sonnet | MEDIUM | seed-0022 | AC4 hits csproj and bin/obj; collides with moved migrate call | acceptance cannot pass | — | path | — | acceptance | AC4 | codex-8, F38-08 |
| R1-08 | codex, sonnet | MEDIUM | seed-0022 | AC6 vacuous (`**`) and misses responses/commands | acceptance vacuous | layout unproven | path | — | acceptance | AC6 | codex-9, F38-08 |
| R1-09 | codex | MEDIUM | unranked — Codex severity MEDIUM | 401 → 403 wrong-sprint change leaves the stored token uncleared | consumer missed | stale token kept | path | — | scope | Decisions | codex-10 |
| R1-10 | codex, sonnet | MEDIUM | seed-0022 | `csharp.md` self-contradiction unreconciled | two rules in force | new code violates a live rule | quoted | — | sibling | Decisions row 1 | codex-11, F38-09 |
| R1-11 | sonnet | MEDIUM | seed-0022 | Moved 400-class rules become 409 | mapping gap | status changes | path | — | scope | Scope rules row | F38-04 |
| R1-12 | sonnet | MEDIUM | seed-0022 | AsNoTracking on shared methods would drop writes | ambiguous outcome | changes silently not saved | path, scenario | silent-wrong-output | scope | Scope reads row | F38-05 |
| R1-13 | sonnet | MEDIUM | seed-0022 | Publish-after-commit double-claimed with F35 | double assignment | built twice or never | quoted | — | sibling | Scope row 2 | F38-06 |
| R1-14 | sonnet | MEDIUM | seed-0022 | Hub messages change if the access check throws | mechanism gap | user-visible messages change | path | — | scope | Scope row 3 | F38-07 |
| R1-15 | sonnet | MEDIUM | seed-0022 | F35 seams: BE-15 rule part, BE-23 truncation, closed-sprint message | split ownership | parts done twice or never | quoted | — | sibling | Scope | F38-10 |
| R1-16 | codex, sonnet | LOW | seed-0310 | Fixture targets 6 endpoint classes, not 8; other wording slips | inaccurate count | rework | path | — | count | Scope TS-10 | codex-12, F38-11 |
| R1-17 | sonnet | LOW | seed-0310 | "Handler failures logged" has no row | claim without a row | faults unobserved | quoted | — | plan | Decisions | F38-12 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-22)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | Scope row 3 (each endpoint keeps today's status); Decisions row 4 |
| R1-02 | fixed-real | — | — | Scope row 3 (anonymous endpoints excluded; identity precedence as today; author check stays) |
| R1-03 | fixed-real | — | — | Decisions row 3 (disclosed behaviour change 2) |
| R1-04 | fixed-real | — | — | Scope row 1 (`errors` item shape, closed `code` set, bodyless auth responses); Decisions row 2; AC7 |
| R1-05 | fixed-real | — | — | Scope row 2 (same messages and payloads, `GroupDissolved` event, no-event factories); What must not change; AC9 |
| R1-06 | fixed-real | — | — | Scope validation row (body input only; route ids not validated) |
| R1-07 | fixed-real | — | — | Scope EF row (migrate via Persistence method); AC4 |
| R1-08 | fixed-real | — | — | AC6 |
| R1-09 | fixed-real | — | — | Decisions row 4 (no 401 → 403 change) |
| R1-10 | fixed-real | — | — | Decisions row 1; F40 spec (csharp.md row) |
| R1-11 | fixed-real | — | — | Scope rules row (400 → `ArgumentException`, 409 → `DomainException`) |
| R1-12 | fixed-real | — | — | Scope reads row (new read methods; mutating callers tracked) |
| R1-13 | fixed-real | — | — | Scope row 2 (uses F35's mechanism); F35 Scope BE-12 |
| R1-14 | fixed-real | — | — | Scope row 3 (hub translates into today's `HubException` messages) |
| R1-15 | fixed-real | — | — | Scope rules row (F35 guard kept); validation row (truncation); BE-15 dropped from F38 |
| R1-16 | fixed-real | — | — | Scope TS-10; dead-code row (`InternalsVisibleTo` reason); backlog row 38 |
| R1-17 | fixed-real | — | — | Scope hub/timer/events row (interceptor logs handler failures) |

Reconciled: Scope row 1 ↔ Decisions row 2 ↔ AC7 ↔ F39 Scope row 1 (`ApiError` reads this shape); Scope row 2 ↔ F35 Scope BE-12 ↔ What must not change ↔ AC9; Scope row 3 ↔ Decisions row 4 ↔ F36 Scope FE-31 (author-only rule unchanged); Decisions row 1 ↔ F40 csharp.md row; backlog row 38 wording.

## Round 2 — 2026-10-23 — spec — primary (codex)

Header note: Codex alone. Read: delta. Floor: MEDIUM. Verdict and fold-verification table persisted verbatim in `review-critic-codex.md` § Round 2: fourteen closed; R1-03, R1-05, R1-06 partial; one new finding.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — Codex severity MEDIUM | Disclosed broadcast-failure change contradicts the "every status code" lines | internal contradiction | guarantee unreadable | quoted | — | fold | Decisions rows 3–4, What must not change | F2-01, R1-03 |
| R2-02 | codex | MEDIUM | unranked — Codex severity MEDIUM | No check of commit, rollback and handler-failure behaviour for the moved broadcasts | acceptance does not prove outcome | broadcast replacement unverified | path | — | fold | AC | R1-05 |
| R2-03 | codex | MEDIUM | unranked — Codex severity MEDIUM | Residual validators deferred to the plan | scope deferred | — | quoted | — | fold | Scope validation row | R1-06 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | Decisions row 4; What must not change |
| R2-02 | fixed-real | — | — | AC8 (handler tests: same payloads, throwing handler logged, rollback) |
| R2-03 | plan-must-resolve | MEDIUM | — | Scope validation row: the plan lists the endpoints from the code after F35; the rule (body input only, route ids untouched) is fixed here |

Reconciled: Decisions rows 3–4 ↔ What must not change ↔ AC8. No round 3: no CRITICAL.
