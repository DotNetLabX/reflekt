## Round 1 — 2026-10-22 — spec — primary (codex)

Header note: two readers — Codex primary (read-only job, verdict persisted verbatim in `review-critic-codex.md`) and a Sonnet nexus critic as second reader; pair `codex + sonnet`, picked by the owner. The spec had been committed as Ready without review; the owner caught it and it was set back to Draft before this round.

Primary reader: see `review-critic-codex.md` — `## Verdict: NO-GO`, 3 HIGH, 12 MEDIUM.

## Second reader — round 1 (sonnet)

Verdict: REVISE (4 HIGH → round-1 adversarial sweep). (Condensed by the architect, not verbatim: every finding is kept with its level, evidence and fix; the reader's full message is in the session transcript.)

- **[HIGH] F35-01 — "Events published only after the final commit" has no mechanism.** The interceptor publishes in `SavedChangesAsync`, inside any outer transaction; "one save" is not buildable (`Card` has no group navigation); the mechanism is also claimed by F38. Fix: name it (navigation, or defer publication to commit); one owner.
- **[HIGH] F35-02 — Soft kick evicts from the group but the connection stays authenticated and can rejoin.** `JoinSprint` checks only claims; `ToggleReady` never re-checks; the kicked client must still receive `ParticipantKicked`; readiness and connection state are hub statics. Untouchable: security. Fix: hub methods reject removed participants or abort; one ordered handler (broadcast → evict → purge → re-broadcast readiness).
- **[HIGH] F35-03 — Writing-phase redaction misses the report endpoints.** Both report endpoints return action-item text, assignee and evidence to a guest with a matching sprint claim, no phase check. Untouchable: security.
- **[HIGH] F35-04 — Return-URL rule ambiguous.** Reading A rejects `/sprint/12` (the only caller); reading B accepts `/<TAB>/evil.com`, which browsers turn into `//evil.com`. Acceptance says "rejects" while the endpoint falls back to `/`; no seam named. Untouchable: security.
- **[MEDIUM] F35-05 — Acceptance 3's 409 case has no reachable path** unless the BE-14 guard is itself a `DomainException`.
- **[MEDIUM, band MEDIUM–HIGH] F35-06 — "Owner's view unchanged" undefined on SignalR** (every hub connection is a guest connection; an owner-participant is redacted in REST today).
- **[MEDIUM] F35-07 — Acceptance covers ~6 of 14 rows** (SSRF, query token, indexes, enums, missing claim, BE-12, failed-save cleanup unchecked); a unit test of the redaction function does not prove the handlers use it.
- **[MEDIUM] F35-08 — "Every mutating endpoint, one message" collides with existing 409 messages** and Draft sprints; name the endpoints and the order.
- **[MEDIUM] F35-09 — Which participant readers honour "removed" is unstated** (`AdvancePhaseCheck`, display-name check, Google rejoin, re-kick).
- **[MEDIUM, band MEDIUM–HIGH] F35-10 — The migration repair deletes votes irreversibly and is never exercised.** Untouchable: data-loss.
- **[MEDIUM] F35-11 — Unit tests assert `InvalidOperationException`; not every throw is a state rule** (`Sprint.cs:64,117`, `Card.cs:88,97`).
- **[LOW] F35-12 — Scope bookkeeping:** SK-23 both in and out; SK-03 nowhere; camelCase claimed by F35 and F38; Development `Details`; backlog row incomplete.
- **[LOW] F35-13 — Acceptance 5's tool is not installed.**
- **[LOW] F35-14 — Plan must resolve:** author-connection window; `EvidenceLinkFetched` payload; unique-violation → 409 mapping; open-ended SSRF list; `InternalsVisibleTo`.

Coverage ledger (second reader): BE-01, BE-20, BE-19/SK-09/BE-28, BE-23, siblings COVERED; BE-02, BE-07, BE-13, BE-12, BE-14, BE-15, BE-21, acceptance PARTIAL; several test bodies, repositories and the migration snapshot NOT OPENED.

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | seed-0008 | Kicked connection can rejoin; eviction order | access-revocation gap | removed participant keeps live access | quoted, path | security | scope | Scope BE-13 | codex-1, F35-02 |
| R1-02 | codex, sonnet | HIGH | seed-0007 | Redaction misses `GetGroups` and both report endpoints | guarantee bypass via second path | Writing-phase text readable via REST | quoted, path | security | scope | Scope BE-02 | codex-2, F35-03 |
| R1-03 | codex, sonnet | HIGH | seed-0008 | "Publish after commit" has no mechanism; double-claimed with F38 | acceptance-unreachable mechanism | the BE-12 defect survives | quoted, path | — | scope | Scope BE-12 | codex-9, F35-01 |
| R1-04 | sonnet, codex | HIGH | seed-0008 | Return-URL rule ambiguous; acceptance outcome undefined | ambiguous validation rule | breaks the caller or keeps the open redirect | quoted, scenario | security | scope | Scope BE-21, AC6 | F35-04, codex-13 |
| R1-05 | codex, sonnet | HIGH | contested: HIGH / MEDIUM — settled HIGH: deleting votes is the data-loss class and the name repair could fail the migration | Migration repair: unsafe renames, vote deletion, never exercised | destructive migration untested | rewritten reports or a failed live migration | scenario | data-loss | scope | Decisions row 2, AC5 | codex-3, F35-10, codex-12 |
| R1-06 | codex, sonnet | MEDIUM | seed-0022 | Owner's view undefined across identity combinations | actor undefined | builds differ, REST vs live disagree | path | — | scope | Scope BE-02 | codex-4, F35-06 |
| R1-07 | codex | MEDIUM | unranked — Codex severity MEDIUM | `EvidenceLinkFetched` payload is not a card | contract unresolved | redaction shape undefined | path | — | scope | Scope BE-02 | codex-5 |
| R1-08 | codex, sonnet | MEDIUM | seed-0021 | Closed-sprint guard boundary (SetReviewStatus, hub timers, existing messages, order) | scope vs must-not-change | breaks previous-action review, or misses hub | path | — | scope | Scope BE-14 | codex-6, F35-08 |
| R1-09 | codex, sonnet | MEDIUM | seed-0041 | SK-23 in and out; team-count race unassigned | split ownership | race neither fixed nor recorded | quoted | — | sibling | Scope, Out of scope | codex-7, F35-12 |
| R1-10 | codex, sonnet | MEDIUM | seed-0310 | Redaction unit test cannot prove broadcasts | acceptance does not prove outcome | handlers keep leaking with tests green | path | — | acceptance | AC4 | codex-8, F35-07 |
| R1-11 | codex | MEDIUM | unranked — Codex severity MEDIUM | Closed-group test passes before the fix (phase guard) | vacuous acceptance | defect unproven | path | — | acceptance | AC4 | codex-10 |
| R1-12 | codex, sonnet | MEDIUM | seed-0022 | 409 mapping test unreachable; `Details` leak; casing | acceptance unreachable | mapping ships untested | path | — | acceptance | AC3 | codex-11, F35-05 |
| R1-13 | codex, sonnet | MEDIUM | seed-0364 | EF command incomplete; tool not installed | acceptance mechanics | drift check unrunnable | path | — | acceptance | AC5 | codex-12, F35-13 |
| R1-14 | codex | MEDIUM | unranked — Codex severity MEDIUM | SSRF acceptance never observes connections | acceptance does not prove outcome | SSRF fix unproven | path | — | acceptance | AC | codex-14 |
| R1-15 | codex, sonnet | MEDIUM | seed-0310 | Several rows have no acceptance case | acceptance coverage | security fixes untested | path | — | acceptance | AC | codex-15, F35-07 |
| R1-16 | sonnet | MEDIUM | seed-0021 | Readers of participants honouring "removed" unstated | reader enumeration | removed participant still counted | path | — | scope | Scope BE-13 | F35-09 |
| R1-17 | sonnet | MEDIUM | seed-0021 | Unit tests asserting `InvalidOperationException`; defensive arms | blast radius | tests break; internal errors become 409 | path | — | scope | Scope BE-07, AC2 | F35-11 |
| R1-18 | sonnet | LOW | seed-0364 | Plan-must-resolve items | mechanics | — | path | — | plan | — | F35-14 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-22)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | Scope BE-13 (hub re-check, ordered handler) |
| R1-02 | fixed-real | — | — | Scope BE-02 (all read paths + broadcasts) |
| R1-03 | fixed-real | — | — | Scope BE-12 (transaction-aware interceptor, F35 owns); Decisions row 2; F38 Scope row 2 |
| R1-04 | fixed-real | — | — | Scope BE-21 (exact rule, fallback); AC6 |
| R1-05 | fixed-real | — | — | Scope BE-15 (no vote index); Decisions row 3 (collision-safe rename, no deletion); AC9 |
| R1-06 | fixed-real | — | — | Scope BE-02 (who sees what); Decisions row 4 |
| R1-07 | fixed-real | — | — | Scope BE-02 (`EvidenceLinkFetched` to author only) |
| R1-08 | fixed-real | — | — | Scope BE-14 (named endpoints, hub timers, order, SetReviewStatus boundary) |
| R1-09 | fixed-real | — | — | Scope BE-15; Out of scope; F40 known deviation |
| R1-10 | fixed-real | — | — | AC5 (grep that handlers use the rule); Decisions row 6 |
| R1-11 | fixed-real | — | — | AC3 (phase that allows grouping; data unchanged) |
| R1-12 | fixed-real | — | — | Scope BE-14 (`EnsureOpen` throws `DomainException`); Scope BE-20 (no `Details`, casing owner); AC3 |
| R1-13 | fixed-real | — | — | AC9 (suite start-up + recorded migration run) |
| R1-14 | fixed-real | — | — | Scope BE-23 (public address check); AC7 |
| R1-15 | fixed-real | — | — | AC1, AC6–AC8 |
| R1-16 | fixed-real | — | — | Scope BE-13 (repository filters, rejoin, re-kick) |
| R1-17 | fixed-real | — | — | Scope BE-07 (defensive arms kept; tests named); AC2 |
| R1-18 | plan-must-resolve | LOW | — | handed to the plan |

Reconciled: Scope BE-12 ↔ F38 Scope row 2 (F38 relies, does not rebuild); Scope BE-14 ↔ What must not change ↔ AC3; Scope BE-15 ↔ Decisions row 3 ↔ AC9 ↔ F40 (team-count race); Scope BE-02 ↔ Decisions rows 4 and 6 ↔ AC5; Scope BE-20 ↔ F38 Scope row 1 (casing); Decisions row 5 ↔ AC6–AC7 (new unit-test classes allowed).

## Round 2 — 2026-10-23 — spec — primary (codex)

Header note: Codex alone (round ≥ 2 has no second reader). Read: full — the slug's number (35) is divisible by 5. Floor: MEDIUM. Verdict and fold-verification table persisted verbatim in `review-critic-codex.md` § Round 2: R1-02, 04, 06, 07, 09, 11, 12, 13, 16, 17 closed; R1-01, 03, 05, 08, 14, 15, 18 partial; R1-10 not closed.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Kick cleanup skipped if the broadcast throws | ordering gap | kicked connections stay subscribed | path | security | fold | Scope BE-13 | R2-01 |
| R2-02 | codex | MEDIUM | unranked — Codex severity MEDIUM | Grep cannot prove broadcaster recipients and payloads | acceptance does not prove outcome | leak ships with checks green | path | security | fold | AC5, Decisions | R2-02 |
| R2-03 | codex | MEDIUM | unranked — Codex severity MEDIUM | Redaction would hide previous-sprint action items | scope boundary missing | previous actions blanked | path | — | fold | Scope BE-02 | R2-03 |
| R2-04 | codex | MEDIUM | unranked — Codex severity MEDIUM | Report group title copies a redacted card's text | derived-field leak | private text in the group title | path | security | round-1 miss: report-derived fields | Scope BE-02 | R2-04 |
| R2-05 | codex | MEDIUM | unranked — Codex severity MEDIUM | Inline CreateGroup/DissolveGroup sends go out before commit | mechanism gap | clients read uncommitted data | path | — | fold | Scope BE-12 | R2-05 |
| R2-06 | codex | MEDIUM | unranked — Codex severity MEDIUM | Round-1 partials: interceptor acceptance (R1-03), migration fixture and enforcement (R1-05), JoinSprint timer auto-start (R1-08), SSRF connect behaviour (R1-14), missing claim and token transports (R1-15) | acceptance coverage | gaps survive | path | — | fold | AC1, AC7–AC9, Scope BE-14 | R1-03, R1-05, R1-08, R1-14, R1-15 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | Scope BE-13 (revocation runs even when a send fails); AC4 |
| R2-02 | fixed-real | — | — | Decisions row 6 (handler tests with hub fakes); AC5 |
| R2-03 | fixed-real | — | — | Scope BE-02 (current-sprint cards only); AC3, AC5 |
| R2-04 | fixed-real | — | — | Scope BE-02 (derived values redacted); AC5 |
| R2-05 | fixed-real | — | — | Scope BE-12 (inline sends after commit); AC1 |
| R2-06 | fixed-real | — | — | AC1, AC7, AC8, AC9; Scope BE-14 (JoinSprint timer auto-start) |

Reconciled: Scope BE-02 ↔ AC5 ↔ Decisions row 6; Scope BE-12 ↔ AC1 ↔ F38 Scope row 2; Scope BE-13 ↔ AC4; Scope BE-14 ↔ AC3. No round 3: round 2 holds no CRITICAL.
