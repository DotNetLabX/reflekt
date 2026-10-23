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


## Round 1 — 2026-10-23 — plan — primary (codex)

Header note: plan review round 1. Primary Codex (job task-mvkiap30-p0ko8a, completed, NO-GO) — its verdict is persisted verbatim in `review-critic-codex.md` § Plan review, round 1. Second reader Sonnet (`critic-second`), verdict ACCEPT; its message is below. Pair `codex + sonnet` — gear: normal. Team lead wrote the merge; the fold is the architect's.

## Second reader — round 1 (sonnet)

Verdict: ACCEPT. Findings (verbatim titles, full text in the team lead's relay to the architect):
- [MEDIUM] P1-01 — Held and collected events are not separated, and EF's implicit save transaction fires the same callbacks (seed-0130). Fix: two lists, `collected` and `held`; transaction callbacks touch only `held`; add a trace for a multi-command save with no explicit transaction.
- [MEDIUM] P1-02 — Step 6 deletes `ParticipantRepository.Remove`, but the kick endpoint still calls it until Step 8 (seed-0130).
- [MEDIUM] P1-03 — The hub's closed-sprint rejection cannot come from `EnsureOpen()` as written; name a `RequireOpen` wrapper rethrowing `HubException`, `JoinSprint` keeps a plain state check (seed-0130).
- [MEDIUM] P1-04 — The per-step `unit` role never runs the integration tests several accept lines cite (seed-0129).
- [LOW] P1-05 — The Step 2 dissolve-loop grep already passes today (2 sites, not 3) (seed-0127).
- [LOW] P1-06 — Development fallback when the origin key is missing has no `Adds:` row (seed-0127).
- Open Q-1 (ungraded): join/kick race window in `JoinSprint`. Q-2: P2 rationale stale. Q-3: graphify-out churn in the Step 14 file-list check.
Coverage ledger: every scope row and AC opened; sibling specs F38/F40, `SprintRepository`, `CardRepository`, `TimerService`, `OpenGraphBackgroundService` not opened (no step depends on them).

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | `IDbTransactionInterceptor` needs EF Core Relational; `Blocks.Persistence` references base EF only | missing dependency | Step 1 does not compile; inventory and close-out file list omit the csproj change | quoted | — | dependency | plan Step 1; Blocks.Persistence.csproj | codex-1 |
| R1-02 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | Join can race kick revocation | race | A kicked participant's connection can end up in both groups after eviction (BE-13 requires an open connection cannot rejoin) | quoted | — | kick | plan Step 9; SprintHub.JoinSprint | codex-2, sonnet Q-1 (contested: HIGH / ungraded — settled HIGH: the spec makes rejoin-proofing a requirement, and only a code coordination or compensating check meets it) |
| R1-03 | sonnet | MEDIUM | seed-0130 | Collected vs held events not separated; implicit save transaction fires the same callbacks | ambiguous mechanism | Multi-statement saves could silently drop their events | quoted | — | interceptor | plan Step 1; DomainEventPublishingInterceptor.cs | sonnet P1-01 |
| R1-04 | codex, sonnet | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | P2 does not close R1-18's author-connection window; rationale stale | race / stale rationale | A new author connection can get the redacted copy only | quoted | — | redaction | plan P2; useSprintHub.ts:75–78 | codex-3, sonnet Q-2 |
| R1-05 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | New participant group has no leave lifecycle (`LeaveSprint`) | incomplete lifecycle | A left connection keeps receiving full author notifications and escapes revocation | quoted | — | hub | plan Step 9; SprintHub.LeaveSprint | codex-4 |
| R1-06 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Production 5xx body still emits `details: null` | spec contradiction | BE-20/AC3 require no `details` outside Development | quoted | — | errors | plan Step 3 | codex-5 |
| R1-07 | codex, sonnet | MEDIUM | seed-0130 | Step 6 deletes `ParticipantRepository.Remove` still called until Step 8 | step-ordering break | Build fails at the Step 6/7 boundary | quoted | — | kick | plan Steps 6, 8; KickParticipantEndpoint.cs:44 | codex-6, sonnet P1-02 |
| R1-08 | sonnet | MEDIUM | seed-0130 | Hub closed-sprint rejection mechanism unnamed (`DomainException` vs `HubException`) | exception/transport mismatch | Client sees a generic error, or the accept grep fails | quoted | — | hub | plan Step 4; SprintHub.cs | sonnet P1-03 |
| R1-09 | sonnet | MEDIUM | seed-0129 | Step `unit` role never runs the integration tests the accept lines cite | verification role mismatch | Regressions in the riskiest steps surface only at Step 7/14 | quoted | — | verification | .claude/verify.json; plan preamble, Steps 2, 4, 8, 12 | sonnet P1-04 |
| R1-10 | codex, sonnet | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Missing step-local `Adds:` rows (AdvancePhase every branch; return-URL extra representation; Dev origin fallback) | untagged addition | Unrecorded behaviour beyond the spec | quoted | — | adds | plan Steps 2, 12 | codex-7, sonnet P1-06 (contested: MEDIUM / LOW — settled MEDIUM: three additions, one at a security surface) |
| R1-11 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Missing `Departs from:` rows (error-handling mapping/serialisation; WaitForNone; inline resource checks; broadcast-only handler name) | untagged departure | Skill departures unrecorded | quoted | — | departs | plan Steps 1, 3, 8, 9 | codex-8 |
| R1-12 | codex, sonnet | LOW | seed-0127 | Dissolve-loop grep already passes today | non-discriminating check | A left-in loop save passes | path | — | verification | plan Step 2; DissolveGroupEndpoint.cs | codex-9, sonnet P1-05 |
| R1-13 | sonnet | LOW | seed-0127 | graphify-out regeneration churns the Step 14 file-list check | scope noise | Unreviewable churn in the change set | quoted | — | close-out | plan Step 14 | sonnet Q-3 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | — | plan Step 1 (files: `Blocks.Persistence.csproj`, `Directory.Packages.props`; Relational reference bullet; grep accept; risk adds dependency kind); Context package-rule bullet; Size line (48 files); Skill Mapping row 1; Must-NOT-Change package row; Step 14 package check (one added line); Decisions row "Relational reference"; P7 (new) |
| R1-02 | fixed-real | HIGH | — | plan Step 9 (registry; `JoinSprint` order with a second removed check after the group joins, and its undo path; accept traces); Decisions rows "JoinSprint re-check", "connection registry" |
| R1-03 | fixed-real | MEDIUM | — | plan Step 1 (collected vs held lists; `SavedChangesAsync` routing; callbacks touch held only; `TransactionStartedAsync` clears held; EF's own save transaction covered; trace (e) added); Decisions row "two lists"; Decisions row "AC1" (five traces) |
| R1-04 | fixed-real | MEDIUM | — | plan Step 11 (Writing broadcasts to participant groups only: full to the author's, redacted to each other active participant's; accept trace); P2 State → superseded by P8; P8 (new); Decisions row "participant groups only" |
| R1-05 | fixed-real | MEDIUM | — | plan Step 9 (`LeaveSprint` leaves the participant group; the registry is kept until disconnect, so revocation still finds a left connection; the kick handler reads the registry; accept traces); Decisions rows "connection registry", "LeaveSprint" |
| R1-06 | fixed-real | MEDIUM | — | plan Step 3 (5xx body fields exactly `statusCode`, `message`, `traceId`; `details` key absent; accept trace on the serialised JSON); Decisions row "5xx body" |
| R1-07 | fixed-real | MEDIUM | — | plan Step 6 (`Remove` kept, grep removed); Step 8 (files add `ParticipantRepository.cs`; deletes `Remove`; grep accept); Decisions row "`Remove` deleted in Step 8" |
| R1-08 | fixed-real | MEDIUM | — | plan Step 4 (private hub helper: `EnsureOpen()` with `DomainException` rethrown as `HubException("Sprint is closed.")`; `JoinSprint` plain state check; grep accept names the catch); Decisions row "hub closed sprint" |
| R1-09 | fixed-real | MEDIUM | — | plan Implementation Steps preamble (`unit` runs only `Reflekt.UnitTests`; each step's **Runs:** integration files by name through `verify-run.js --profile fast --files`); a **Runs:** line on Steps 1–13; Step 3 runs every integration file and updates only assertions the spec's status or casing change alters; Testing Strategy; Must-NOT-Change tests row; Decisions row 1; P1 Decided; Step 14 file list |
| R1-10 | fixed-real | MEDIUM | — | plan Step 2 (`AdvancePhase` transaction narrowed to leaving Writing, as the spec names it — no addition left); Step 12 (received-value check is the spec's, unescaped check is today's `LoginEndpoint.cs:35`, kept — not an addition; dev origin prefixed as today, the designed missing-key fallback removed); Decisions rows "AdvancePhase", "return URL" |
| R1-11 | fixed-real | MEDIUM | — | plan Decisions: four `Departs from` rows, each naming the source that requires it — domain-patterns (`WaitForNone`, spec BE-12), error-handling (spec BE-07/BE-20), authorization-patterns (inline kick checks, spec Out of scope → F38), create-domain-event-handler (handler name, spec Out of scope → F38 BE-25) |
| R1-12 | fixed-real | LOW | — | plan Step 2 (dissolve grep replaced by a code trace: no save inside the per-card `foreach`, one batched add and one save) |
| R1-13 | fixed-real | LOW | — | plan Step 14 (`graphify-out/` committed, not reviewed, excluded from the file-list check by pathspec) |

Reconciled: plan Context (package rule ↔ P7, Step 14 package check, Must-NOT-Change package row — all now "one added line at 10.0.13, nothing moves"); Size line ↔ Step 1 and Step 8 file lists; Skill Mapping row 1 ↔ Step 1; Step 9 seams ↔ Step 11 (Step 11 no longer reads the author's connection ids, so the seam list names only the kick handler's needs); Step 6 ↔ Step 8 (`Remove`, `ParticipantRepository.cs` in both lists); Step 11 ↔ Step 6 (`GetBySprintIdAsync` skips removed participants, so a kicked participant's group gets no redacted copy); Step 3 test rule ↔ Testing Strategy, Must-NOT-Change tests row, Decisions row 1, P1, Step 14 file list; P2 ↔ P8 and Decisions row "participant groups only"; P1 and Testing Strategy row range corrected to Decisions rows 2–8 (the AC replacement rows — the old "2–9" took in the interceptor row). Plan-side summary: plan.md § Plan Review.


## Round 2 — 2026-10-23 — plan — primary (codex)

Header note: plan review round 2, floor MEDIUM, Read: full (sample — slug number 35 divisible by 5). Primary Codex alone (job task-mvkj2ttx-ctkrnt, completed, NO-GO); verdict and fold-verification table persisted verbatim in `review-critic-codex.md` § Plan review, round 2. Fold verification: R1-01..R1-09, R1-11..R1-13 closed; R1-10 partial (→ R2-02). Round 3 does not run: no CRITICAL.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Step 6 integration checks run before Step 7's migration exists | step-ordering break | `Database.Migrate()` rejects pending model changes; Step 6's required integration runs fail | quoted | — | fold | plan Steps 6–7; DependencyInjection.cs:47; ParticipantEndpointTests.cs:75 | codex R2-01 |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Decoded return-URL validation expands today's check without an `Adds:` row | untagged addition | `/docs/%09notes` newly rejected; preservation claim at plan line 540 incorrect | quoted | — | fold | plan Step 12, line 430, 540; LoginEndpoint.cs:36 | codex R2-02 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | MEDIUM | — | plan Step 6 Runs line (no integration file by name; Steps 6 and 7 are one verification boundary, because the start-up `Migrate()` rejects Step 6's model change until Step 7's migration exists); Step 7 Runs line (runs Step 6's `ParticipantEndpointTests.cs` and `SprintEndpointTests.cs`, then the `full` role); plan § Plan Review |
| R2-02 | fixed-real | MEDIUM | — | plan Step 12 (the spec's four parts on the received value; today's three decoded-form predicates from `LoginEndpoint.cs:35` kept exactly — starts with `/`, not `//`, no `://` — so `/docs/%09notes` is still accepted); Decisions row "return-URL rule" (rewritten, the "nothing it rejects today" claim replaced by the exact predicates, R2-02 cited); plan § Plan Review |

Reconciled: Step 6 ↔ the Implementation Steps preamble (every step closes on the build and the `unit` role; the integration files run by name per Runs line, so Step 6 naming none is consistent) ↔ Step 7 (its Runs line takes Step 6's files) ↔ the Build slices line (Steps 6 and 7 are both in slice 6–11, one developer) ↔ Testing Strategy (per-step Runs files, unchanged wording); Step 12 ↔ Decisions row "return-URL rule" ↔ the Step 12 scratch-probe cases (`/sprint/12` accepted; `//evil.com`, `/\evil.com`, `/` + TAB + `/evil.com` and `https://evil.com` rejected — each still rejected by the received-value rule) ↔ Must-NOT-Change (no other behaviour change). Plan departures recorded as kept (away answerer, recommendation taken) in plan.md § Plan Review; plan approved there.
