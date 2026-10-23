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

## Round 1 — 2026-10-23 — plan — primary (sonnet)

Header note: one reader. The Codex primary (job task-mvkp93d8-goixi7) was lost — the companion no longer finds the job, past its deadline — and the Sonnet second reader af2266ad765f76ebc died to the usage limit with no output. Re-dispatched under the late rule to a fresh Sonnet critic as main, alone (`Primary: sonnet — Codex dropped: job record lost; after the early window — fresh sonnet main`). Persisted verbatim by the team lead.

# F38 — Backend Conventions Conformance — Critic Review

## Mode: Plan Review
## Verdict: REVISE

No finding carries a CRITICAL or HIGH heading. I chose REVISE, not ACCEPT, for two reasons. P-01's band reaches HIGH in a never-downgrade class (silent wrong output), and one check on a running host settles it. P-02 to P-05 are real plan gaps that a developer would otherwise fill by guessing.

## Pre-commitment Predictions
- **The interceptor's fire-and-forget design (Step 4) hides a lifetime problem.** Found: P-01.
- **The access-order census (44 endpoints) differs from the code in places.** It matches on the endpoints I read (GetSprint, UpdateColumns, KickParticipant, SetReviewStatus, GetGroups). It slips on RetractVote and on the `IsAllowed` file count (P-08). It misses CreateCard's Google-cookie 403 and the third 403 in Edit/Delete (P-06).
- **Spec rows the steps silently drop.** Found: the BE-30 domain polish (P-02) and `MeEndpoint` (P-04).
- **The "no new tests" rule collides with the shared-fixture step.** Found a third inline factory (P-07).
- **Step ordering or banned-grep collisions.** Found the Step 3 provider against Step 11's `GetAwaiter().GetResult()` grep (P-05). The Step 6 `GroupDissolved` trace against the interceptor holds: `ChangeTracker.Entries()` includes `Deleted` entries. The 28-file, 44-endpoint, 39-alias and 11-validator counts all check out.

## Coverage ledger
Round: 1 · Depth: mechanism · Bar: mechanics · Ruler: seed only (no `docs/critic-calibration/` overlay or `corpus.jsonl` on disk, so no shape grep was possible) · Read: full

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| Scope row 1: one error convention and body | spec § Scope; plan Step 1, 5–9; `GlobalExceptionMiddleware.cs`; `DependencyInjection.cs`; FastEndpoints 5.34.0 XML; tests dir | COVERED (Step 1 names `UseFastEndpoints` but the code calls `MapFastEndpoints` — P-08; `MeEndpoint` is not assigned — P-04) |
| Scope row 2: one broadcast mechanism | `DomainEventPublishingInterceptor.cs`; Create/Dissolve/CastVote/RetractVote endpoints; the four handlers; `CardBroadcast.cs`; plan Step 4, 6, 7 | PARTIAL (P-01, P-10) |
| Scope row 3: one access check | plan census; GetSprint, UpdateColumns, KickParticipant, SetReviewStatus, GetGroups, CreateCard, GetCurrentParticipant | PARTIAL (P-05, P-06, P-08) |
| Scope row 4: rules in aggregates | Create/Dissolve/CastVote endpoints; Domain `Card`, `User`, `Participant`, `EvidenceLink`; findings BE-06, SK-12 | COVERED |
| Scope row 5: duplicated handler logic | findings BE-08, SK-15; plan Step 6, 9 | COVERED (not read in code: Reports endpoints, GetGroups response building) |
| Scope row 6: dead code and twins | `Reflekt.API.csproj`; `Directory.Packages.props`; unit-test project; findings BE-09, BE-10 | COVERED |
| Scope row 7: EF in the API layer | grep of API for EF types (3 files + csproj); `Persistence/DependencyInjection.cs`; `Program.cs` | COVERED |
| Scope row 8: hub, timer, events | `SprintHub.cs` lines 1–30 and 235–398; `TimerService.cs`; findings BE-16, BE-33 | PARTIAL (hub lines 30–235 not opened) |
| Scope row 9: reads and size | findings BE-17, BE-18, BE-35; Domain collections; EF configurations | PARTIAL (P-09) |
| Scope row 10: validation and limits | census of 11 validators against 28 mutating endpoints; findings BE-31, SK-20 | COVERED |
| Scope row 11: naming and layout | findings BE-25–BE-30, BE-32, BE-34, SK-19, SK-24–SK-30 against plan Step 14, 15 | PARTIAL (BE-30: P-02; Google options: P-08 note) |
| Scope row 12: TS-10 | `tests/Reflekt.IntegrationTests/*.cs`; Helpers | PARTIAL (P-07) |
| AC1–AC10 | spec § Acceptance; plan Steps 1–18, Testing Strategy, Must-NOT-Change | COVERED (AC7/AC8 re-expressed under owner rule P1, which is allowed) |
| Prior spec review rounds and campaign context | `review-critic.md`; `review-critic-codex.md`; `communication-log.md`; `reader-pair.md` | COVERED |
| F35, F39, F40 specs and plans | — | NOT OPENED — only quoted through the plan and the log |
| `.claude/skills/*` (error-handling, extract-feature-service, …) | — | NOT OPENED — the plan's descriptions of them were taken as given |
| `docs/conventions/csharp.md`, `plan-template.md` | — | NOT OPENED |
| `AdvancePhase*Endpoint`, Reports endpoints, `JoinSprintGoogle`, `AttachEvidenceLink`, `Sprint.cs` guard texts | — | NOT OPENED — census and Step 5 claims about them are unverified |
| `web/` consumers of error bodies | — | NOT OPENED — out of scope for the plan, and AR-2 handles it |

## Round-0 fold verification
Not applicable (round 1).

## Findings

### [MEDIUM] P-01 — The detached publication task still carries the request's cancellation token
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Band:** MEDIUM–HIGH; driver: whether Kestrel signals `RequestAborted` once the request completes. One run against a live host settles it.
**Shape:** background work bound to a request lifetime
**Consequence:** A broadcast started after the response can be cancelled mid-way, so other clients silently miss an update.
**Evidence:** quoted, scenario
**Surfaces:** plan § Step 4 "What to build"; `DomainEventPublishingInterceptor.cs` `PublishAsync`; `CastVoteEndpoint.cs`
**Untouchable:** silent-wrong-output (applies if the token is cancelled)
**Source:** plan Step 4: "published … inside one task the request never awaits … The token passed to handlers is the one F35 passes today." F35 passes the `SaveChangesAsync` token, which is the endpoint's `ct`, derived from `RequestAborted`.
**Issue:** The plan makes publication detached, but the token stays request-scoped. The four moved broadcasts are new exposure. Today `VoteCast`, `VoteRetracted`, `GroupCreated` and `GroupDissolved` are sent inline and awaited while the request is alive. After the change, Step 7's handlers do a database count through a new scope and then send, racing the endpoint's own two counts and response write. If the token fires, the query or send throws `OperationCanceledException`. Step 4's new logging records it, and nobody else gets the update. Spec "What must not change" says every client receives the same messages.
**Impact:** The failure would show up only in the AC10 walkthrough, which is the operator's last step.
**Fix:** Say in Step 4 which token the detached task uses. Either use `CancellationToken.None`, or an application-stopping token injected into the interceptor. Add a code-trace line for it. The developer's "confirm `WaitForAll` from the package" instruction should also confirm what happens on a cancelled token.

### [MEDIUM] P-02 — Half of the BE-30 domain polish has no step and no disposition
**Anchor:** seed-0130 · above seed-0364 · below seed-0120
**Shape:** required outcome silently dropped
**Consequence:** A required cleanup is never built, and nothing records the choice to skip it.
**Evidence:** quoted, path
**Surfaces:** spec § Scope row 11; plan § Step 15 "BE-30"; findings `backend.md` BE-30; `Participant.cs`, `User.cs`, `Card.cs`, `EvidenceLink.cs`
**Source:** spec: "domain polish items, the guards they add included"; the Scope intro makes each row's suggested fix the required outcome. BE-30's fix: "Fold `Join*` into one private builder, make `ColumnColor` a record, make `EvidenceLink.Create` internal, add guards to `UpdateProfile`."
**Issue:** Step 15 covers only redundant default assignments, `name?.Trim()`, the timer constants and `ColumnColor`. It does not mention:
- the `Participant.Join` / `JoinWithGoogle` fold, whose duplicated bodies I confirmed in the code;
- `EvidenceLink.Create` made internal;
- `User.UpdateProfile` guards;
- `Card.SetGroupId` and `MarkDeleted` ordering;
- the `CardGroup` title rule written twice (possibly covered by Step 13).

A guard on `UpdateProfile` could change login behaviour, so the plan may have meant to leave it out. Nothing says so.
**Impact:** Either the reviewer reads the omission as a defect, or a developer improvises guards that change a status.
**Fix:** List each BE-30 item in Step 15 with its action. Or add a `## Decisions` row that defers the behaviour-bearing ones (the `UpdateProfile` guards and the `SetGroupId` guard) and gives the reason.

### [MEDIUM] P-03 — The plan overrides spec Decision 2 (`CreateTeamForm.vue`) with no Decisions row, and records no merge order
**Anchor:** seed-0022 · above seed-0127 · below seed-0120
**Shape:** plan and spec disagree on who edits a file
**Consequence:** A developer or reviewer reading the spec edits a frontend file the plan forbids, or ships an API that breaks the SPA.
**Evidence:** quoted
**Surfaces:** spec § Decisions row 2; plan § Context standing rules, Out of scope, Step 17; `communication-log.md` § Campaign context
**Source:** spec: "F38 updates the one SPA consumer … `CreateTeamForm.vue` (prefix parsing → `code`)". Plan: "F38 edits no frontend file at all: the shared answer moved the `CreateTeamForm.vue` edit to F39". Step 17 also says the spec changes only its `**Plan:**` line.
**Issue:** AR-2 comes from the shared architect, not the owner, and it changes an entered decision. The plan puts it only in a Context bullet. `## Decisions` and `## Decisions and why` have no entry for it, and the Ready spec still says the opposite. `limit_reached: ` and `duplicate_name: ` stop travelling as message prefixes in Step 5. If F38 merged before F39 the form would lose its codes, and the plan does not state "F39 merges before F38". Its Related line says only "F38 only produces it".
**Fix:** Add a Decisions row citing AR-2, with the rejected alternative (F38 edits the form). Add the merge-order dependency to Related. Either amend the spec's Decision 2, or add a plan-vs-spec note that the reviewer reads.

### [MEDIUM] P-04 — `MeEndpoint`'s `SendUnauthorizedAsync` is in no step
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Shape:** acceptance grep unreachable by the steps
**Consequence:** Acceptance grep 1 fails at the end, and the fix lands in a verification step that writes no source.
**Evidence:** path
**Surfaces:** `Features/Auth/Me/MeEndpoint.cs`; plan § Step 2, Step 5–9 Accept lines, Step 17
**Source:** the code has `await SendUnauthorizedAsync(ct)` in `MeEndpoint`, the only Send-family hit outside Steps 5–9 (grep of the 41 files). Step 2 moves the file to `Features/Account` and renames the policy but does not convert it. Spec AC1 greps all of `Reflekt.API`.
**Issue:** The per-area Accept greps all pass, and Step 17 is the first place the miss shows. Step 17 is declared "No source file".
**Fix:** Add "`MeEndpoint`'s 401 becomes `UnauthorizedException`" to Step 2, with a matching Accept grep over `Features/Account`.

### [MEDIUM] P-05 — The phase-policy provider's access shape is unspecified and collides with Step 11's grep
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Shape:** two plan lines rule out the obvious implementation
**Consequence:** The developer chooses sync-over-async, which Step 11 then bans, or invents an API at 16 call sites.
**Evidence:** quoted
**Surfaces:** plan § Step 3 "What to build"; § Step 11 Accept
**Source:** Step 3: `PhaseActionPolicyProvider` is "a singleton that builds one `PhaseActionPolicy` on first use through `IServiceScopeFactory` … `PhaseActionRepository.GetAllAsync`". The current `PhaseActionCache` loads synchronously in its constructor. Step 11 Accept: `grep -rn "GetAwaiter().GetResult()" src/Services/Reflekt` → no hits.
**Issue:** An async repository read behind a singleton "first use" needs either an awaited accessor, so every `EnsureAllowed` site gains an `await provider.GetAsync(...)`, or a blocking wait. The plan says each site becomes `policy.EnsureAllowed(phase, action)` without saying where `policy` comes from.
**Fix:** Pick one in Step 3. For example, `await provider.GetAsync(ct)` returns the policy, backed by a `Lazy<Task<>>` or `SemaphoreSlim`. Or load it once in `MigrateAndSeedAsync`. Say how the hub, which is not async at construction, obtains it.

### [LOW] P-06 — Several 401/403 bodies have no stated text, and CreateCard's cookie check is in no census row
**Anchor:** seed-0127 · above none · below seed-0130
**Shape:** unspecified message for a new body
**Consequence:** Clients get invented refusal texts that nothing pins.
**Evidence:** path
**Surfaces:** `CreateCardEndpoint.cs` lines 44–55; `EditCardEndpoint.cs`, `DeleteCardEndpoint.cs` third 403; plan § Step 1 "New handler-side texts", § Access order reference
**Source:** Step 1 fixes only "You do not own this team.", "You are not the author of this card.", and two 401 texts. CreateCard returns 403 when an ActionItem is created in Discussing and the cookie has no Email. Edit and Delete return 403 when the cookie has no NameIdentifier.
**Issue:** `CreateCard` is an extra, endpoint-specific check made through `HttpContext.AuthenticateAsync(Cookies)`, and the census does not list it. SprintAccess "reads the cookie user without throwing", which does not cover the email claim.
**Fix:** Add the three texts to Step 1, and add a census row or note for CreateCard.

### [LOW] P-07 — Step 16 does not account for a third inline factory in `SprintEndpointTests`
**Anchor:** seed-0127 · above none · below seed-0130
**Shape:** acceptance contradicts the test file
**Consequence:** The Step 16 acceptance grep cannot pass without an edit the step never describes.
**Evidence:** path
**Surfaces:** `SprintEndpointTests.cs` lines 250–280 (`GetTeamSprints_EmptyTeam_ReturnsEmptyArray`); plan § Step 16
**Source:** Step 16: "two `WebApplicationFactory` instances … `grep -rln "new WebApplicationFactory<Program>" *EndpointTests.cs` → no hits". The method above builds a third factory inline, with its own database file and the `ScrumMaster` override that Step 2 renames.
**Fix:** Name that test in Steps 2 and 16: setup-only edit, assertion unchanged.

### [LOW] P-08 — Census and wording slips
**Anchor:** seed-0127 · above none · below seed-0130
**Shape:** inaccurate inventory
**Consequence:** Small chance a developer follows a wrong description.
**Evidence:** path
**Surfaces:** plan § Step 3, Step 7, Step 10, Step 1, Access order reference
**Issues:**
- **`IsAllowed` count.** Step 3 says "the 18 files that call `IsAllowed(` today (endpoints and the hub)". The actual 18 are 16 endpoints, `PhaseActionCache` itself, and the unrelated `OpenGraphAddressPolicy.cs`. The hub has no phase `IsAllowed` call; it checks `is not (Writing or Voting or Discussing)`.
- **RetractVote.** The census says RetractVote is "group → sprint". The code never loads a group; it goes straight to the sprint. Step 7 says "Group-first access order kept". A developer could add a group lookup, which is a new 404. The "code wins" rule mitigates this.
- **Step 10 static grep.** Step 10's `grep -n "static "` finds `public static string ParticipantGroup(int)`, which stays.
- **Step 1 call name.** Step 1 says `UseFastEndpoints`; the code calls `MapFastEndpoints`.
- **Validation messages.** The middleware's `ValidationException` branch says "One or more validation errors occurred." and FastEndpoints says "One or more errors occurred!". The plan fixes only the second.
- **Google options.** `TestGoogleCredentials.cs` says the Google handler already validates its options at startup. Check this before adding Step 15's Google validation.
**Fix:** Correct the lines.

### [LOW] P-09 — Step 12 leaves two things open
**Anchor:** seed-0127 · above none · below seed-0130
**Shape:** open-ended clause, no acceptance
**Consequence:** Done is undefined, and the step can grow without bound.
**Evidence:** quoted
**Surfaces:** plan § Step 12; findings BE-35, BE-18
**Source:**
- Step 12 says "`IReadOnlyList<T>` on public collection members of aggregates and on response records". The aggregate already is one (`Card.EvidenceLinks`). BE-35's evidence also lists repository `Task<List<T>>` and `Endpoint<…, List<…>>`, which the step does not mention.
- Step 12 says "Methods within ~60 lines across the files this plan touches". The files this plan touches number about 195, and no Accept line checks the limit.
**Fix:** State whether repository and endpoint return types change. Replace the second line with BE-18's named methods plus a line-count check.

### [LOW] P-10 — Decisions bookkeeping is incomplete
**Anchor:** seed-0137 · above none · below seed-0130
**Shape:** disclosure covers one case of four; promised row absent
**Consequence:** A reviewer cannot tell which timing changes were accepted.
**Evidence:** quoted
**Surfaces:** plan § Skill Mapping row 4, § Decisions
**Source:** Skill Mapping row 4 says "departure, logged (Decisions)", but no Decisions row covers the four handlers moving to `IServiceScopeFactory`. The only timing disclosure is "`GroupCreated` from the handler may now reach clients after the HTTP 201". `GroupDissolved`, `VoteCast` and `VoteRetracted` move behind the response in the same way.
**Fix:** Add the row, and widen the timing row to all four.

### Additions and departures audit
- The plan has no `**Mirrors:**` header and no `Adds:` rows.
- Both `Departs from` rows match spec text.
- Behaviour no source asks for: none that needs an `Adds:` row. `TimerService` logging comes from BE-33's fix, and the handler scope change comes from BE-03 making the dead handlers live (departure row missing: P-10). Dropping the `Microsoft.AspNetCore.App` reference from Persistence is conditional ("if nothing else needs it").
- One unlisted deletion: `CardGroupRepository.GetByIdsAsync` in Step 12, which has no callers.

## Gap Analysis
- **Edge case.** `DissolveGroup` with `GroupDissolvedEvent` raised before `Remove(group)`. The interceptor collects from `Deleted` entries and the event's group keeps its `Id` and `SprintId`, so it holds. Step 6's fallback clause is sound.
- **Ambiguity split.** "Group-first" for RetractVote could mean keep the current sprint-only order or add a group lookup (P-08). Nothing in the plan says which.
- **Backward impact.** Renaming policies touches every test override (Step 2). The inline factory in `SprintEndpointTests` is the one that would be missed (P-07). The SPA reads `errors` and the message prefixes, which depends on F39 landing first (P-03).
- **Left out.** The plan does not say how `SprintAccess` serves read-only endpoints, which would otherwise load tracked entities. This is harmless but contradicts Step 12's intent.

## Open Questions
- Does Kestrel signal `RequestAborted` when a request completes normally? P-01 hangs on this. A one-line check on a running host settles it.
- Is the `UpdateProfile` guard in or out of F38's behaviour budget? That is the architect's call (P-02).

Reviewed: D:\src\reflekt-F38-BackendConventions\docs\specs\F38-BackendConventions\delivery\plan.md

### Merge — round 1

Coverage: F35/F39/F40 specs and plans; `.claude/skills/*` (error-handling, extract-feature-service, …); `docs/conventions/csharp.md`, `plan-template.md`; `AdvancePhase*Endpoint`, Reports endpoints, `JoinSprintGoogle`, `AttachEvidenceLink`, `Sprint.cs` guard texts; `SprintHub.cs` lines 30–235; `web/` error consumers

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | sonnet | MEDIUM | seed-0130 | Detached publication task still carries the request token | background work bound to a request lifetime | broadcast cancelled mid-way; clients miss an update | quoted, scenario | silent-wrong-output (conditional) | publication | plan Step 4; DomainEventPublishingInterceptor.cs | P-01 |
| R1-02 | sonnet | MEDIUM | seed-0130 | Half of BE-30 domain polish has no step or disposition | required outcome silently dropped | cleanup never built, skip unrecorded | quoted, path | — | domain polish | spec Scope row 11; plan Step 15 | P-02 |
| R1-03 | sonnet | MEDIUM | seed-0022 | CreateTeamForm.vue override of spec Decision 2 has no Decisions row; no merge order | plan and spec disagree on who edits a file | wrong file edited or SPA broken | quoted | — | AR-2 | spec Decisions row 2; plan Context, Step 17 | P-03 |
| R1-04 | sonnet | MEDIUM | seed-0130 | MeEndpoint SendUnauthorizedAsync in no step | acceptance grep unreachable by the steps | AC1 grep fails at Step 17 | path | — | errors | MeEndpoint.cs; plan Step 2, 17 | P-04 |
| R1-05 | sonnet | MEDIUM | seed-0130 | Phase-policy provider access shape unspecified; collides with Step 11 grep | two plan lines rule out the obvious implementation | sync-over-async or invented API | quoted | — | access | plan Step 3, Step 11 | P-05 |
| R1-06 | sonnet | LOW | seed-0127 | 401/403 texts unstated; CreateCard cookie check not in census | unspecified message for a new body | invented refusal texts | path | — | errors | CreateCardEndpoint.cs; Edit/DeleteCard; plan Step 1 | P-06 |
| R1-07 | sonnet | LOW | seed-0127 | Third inline factory in SprintEndpointTests unaccounted | acceptance contradicts the test file | Step 16 grep cannot pass | path | — | tests | SprintEndpointTests.cs; plan Step 16 | P-07 |
| R1-08 | sonnet | LOW | seed-0127 | Census and wording slips | inaccurate inventory | developer follows a wrong description | path | — | census | plan Steps 1, 3, 7, 10, 15 | P-08 |
| R1-09 | sonnet | LOW | seed-0127 | Step 12 open-ended clauses | open-ended clause, no acceptance | done undefined | quoted | — | reads | plan Step 12 | P-09 |
| R1-10 | sonnet | LOW | seed-0137 | Decisions bookkeeping incomplete (handler scope, timing of four events) | disclosure covers one case of four | accepted timing changes unclear | quoted | — | decisions | plan Skill Mapping, Decisions | P-10 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-23)

Folded by a fresh architect replacing the planning architect (its session is gone), from the artifacts. The critic's two open questions are settled: the request token is removed from the detached task rather than measured (presumed proceed-default, plan Decisions row "Detached publication token"), and the BE-30 guards are out of F38's no-new-behaviour budget (presumed proceed-default, row "BE-30 guards", status deferred). Facts re-checked in source before folding: the interceptor (`PublishAsync` passes the save token, `Mode.WaitForNone`); `PhaseActionCache` (sync constructor load, 16 endpoint callers, no hub caller); `MeEndpoint`; `CreateCard`/`EditCard`/`DeleteCard` action-item 403s; `CastVote` vs `RetractVote` order; `SprintEndpointTests` lines 250–280; `User`/`Participant`/`EvidenceLink`/`Card` (BE-30) and their callers; `TestGoogleCredentials.cs`; the middleware's validation text; `MapFastEndpoints`.

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | MEDIUM | — | plan Step 4 What to build (handlers get `CancellationToken.None`) and Accept (trace d; `WaitForAll` behaviour recorded from the package); P5 Decided; Skill Mapping row 4; Decisions row "Detached publication token" (new) |
| R1-02 | fixed-real | MEDIUM | — | plan Step 15 Files and the BE-30 list item by item; Step 15 Accept (`EvidenceLink.Create` internal, `Participant` fold, one title check); Domain Model Changes; Decisions row "BE-30 guards" (new, deferred); Step 17 open-findings list |
| R1-03 | fixed-real | MEDIUM | — | plan Context standing rule and Related (F39 merges before F38); Decisions row "AR-2" (new); Step 17 file-list line; spec § Decisions row 2 and § Out of scope amended (dated, AR-2) |
| R1-04 | fixed-real | MEDIUM | — | plan Step 2 What to build (`MeEndpoint` → `UnauthorizedException("Missing user claims.")`), Accept (Send-family grep over `Features/Account`), Satisfies; Step 1 texts |
| R1-05 | fixed-real | MEDIUM | — | plan Step 3 Files (16 endpoints, hub and `OpenGraphAddressPolicy` excluded), What to build (awaited `GetAsync()`, load once on success, `CancellationToken.None`), Accept (no cache, no blocking wait); Step 10 hub injection (no provider); Decisions row "`PhaseActionPolicyProvider` …" (new) |
| R1-06 | fixed-real | LOW | — | plan Step 1 new texts (three action-item 403s, `MeEndpoint` 401, fallback rule); Access order reference Card-first row and new `CreateCard` row |
| R1-07 | fixed-real | LOW | — | plan Step 2 Files (inline host's policy override); Step 16 What to build (fresh-database host in the helper, setup-only edit) |
| R1-08 | fixed-real | LOW | — | plan Step 3 file count; Access order reference (Group-first `CastVote` only, new `RetractVote` row); Step 7 order bullet; Step 10 static grep; Step 1 `MapFastEndpoints` and both validation texts; Step 15 Google check (3 or 4 `ValidateOnStart` bindings) |
| R1-09 | fixed-real | LOW | — | plan Step 12 (`IReadOnlyList<T>` in repositories, records, endpoint types; `SprintAccess` tracked loads; BE-18 methods named, line counts pasted, greps); Steps 5, 6, 9 BE-18 size lines |
| R1-10 | fixed-real | LOW | — | plan Decisions: timing row widened to the four moved broadcasts; handler-scope `Departs from` row (new); Step 4 cross-reference; Step 17 skill/code difference list |

Reconciled: plan Context (standing rules, Related); Skill Mapping rows 4 and 15; Domain Model Changes; Access order reference and its readers (Steps 5–8 "today's order", P4); Error codes (unchanged); Step 1 texts and their users (Steps 2, 5, 8); Step 3 and its readers (Step 10 hub, Step 11 `GetAwaiter` grep, Decisions provider row); Step 4 and its readers (P5, the publisher and handler-scope `Departs from` rows, Testing Strategy, AC8 mapping in Step 17); Steps 5, 6, 9, 11 against Step 12's BE-18 check; Step 15 against Size line (198 files) and Domain Model Changes; Step 16 against Step 2's renamed overrides and its own grep; Step 17 file-list line against the spec amendment; Decisions rows 1–2 numbering (unchanged by the new rows, which follow the timing row); spec § Decisions row 2, § Out of scope against plan Context. No schema touched; Must-NOT-Change and the error-code table need no change.

## Round 2 — 2026-10-23 — plan — primary (sonnet)

Header note: one reader (same Sonnet primary as round 1; no second reader after round 1). Read: full — round 1 coverage was incomplete. Persisted verbatim by the team lead.

# F38 — Backend Conventions Conformance — Critic Review (Round 2)

## Mode: Plan Review
## Verdict: REVISE

Round 1's ten findings are all closed. Opening the surfaces round 1 never reached turned up one HIGH and one MEDIUM that the fold could not have caught. The HIGH is a revocation-race regression in the hub (N-01). The MEDIUM is two ordering traps in Step 5's `AdvancePhase` (N-02). Both are tagged as round-1 misses.

## Pre-commitment Predictions
- **Folds introduce a contradiction between plan sections.** Found only a size-line mismatch (N-03).
- **The spec amendment disagrees with a sibling feature.** Found: F39's header (N-04).
- **The unopened round-1 surfaces hide a mechanics trap.** Found: the hub's second read (N-01) and `AdvancePhaseEndpoint` (N-02).
- **`CancellationToken.None` creates a new problem.** Not found. `Handlers that run at shutdown fail and are logged` is a disclosed, acceptable cost.
- **The BE-30 deferral breaks a unit test.** Not found. `EvidenceLink.Create` has one caller (`Card.cs:79`), no test calls it, and the Participant unit-test file exists.

## Coverage ledger
Round: 2 · Depth: mechanism · Bar: mechanics · Ruler: seed only (no overlay or corpus on disk) · Read: full

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| Round-1 fold (R1-01..R1-10) | plan.md in full (688 lines); `review-critic.md` merge and fold; spec.md diff (Decisions row 2, Out of scope, `Plan:` line); code for each claim | COVERED |
| Spec amendment (AR-2) | spec § Decisions row 2, § Out of scope; plan Context, Related, Decisions "AR-2"; `communication-log.md`; F39 spec header | COVERED (N-04) |
| Previously unopened: hub `SprintHub.cs` lines 30–235 | opened | COVERED (N-01) |
| Previously unopened: `AdvancePhaseEndpoint`, `Sprint.cs` guard texts | opened | COVERED (N-02) |
| Previously unopened: `GetSprintReportEndpoint`, `JoinSprintGoogleEndpoint`, `AttachEvidenceLinkEndpoint`, `RepositoryBase`, `ParticipantRepository`, kick handler | opened | COVERED (N-05) |
| F39 spec | headers and rows only | PARTIAL |
| F35 spec and plan, F40 spec | — | NOT OPENED |
| `.claude/skills/*`, `docs/conventions/csharp.md`, `plan-template.md` | — | NOT OPENED |
| `AdvancePhaseCheckEndpoint`, `GetParticipantReportEndpoint`, `web/` consumers | — | NOT OPENED |

## Round-1 fold verification
| id | fold | collateral | line |
|---|---|---|---|
| R1-01 | closed | none | Step 4 line 219: "The detached task passes `CancellationToken.None` to the handlers"; trace (d); row "Detached publication token"; P5 |
| R1-02 | closed | N-03 | Step 15 lines 491–498 take each BE-30 item with an action; row "BE-30 guards" (deferred); Domain Model Changes line 71. I confirmed `EvidenceLink.Create` has a single caller (`Card.cs:79`) and `CardGroup` repeats the title check in `Create` and `SetTitle` |
| R1-03 | closed | N-04 | spec Decisions row 2 and Out of scope amended; plan lines 21 and 26; row "AR-2" |
| R1-04 | closed | none | Step 2 line 166 (`MeEndpoint` → `UnauthorizedException("Missing user claims.")`); Accept line 174; Step 1 text |
| R1-05 | closed | none | Step 3 line 194 (`Task<PhaseActionPolicy> GetAsync()`, shared load, no blocking, failed load not kept); line 195; the hub makes no phase-action check; row at 609 |
| R1-06 | closed | none | Step 1 line 135 adds the three action-item 403 texts; Access order reference rows 86 and 89 match the code in `CreateCardEndpoint`, `EditCardEndpoint` and `DeleteCardEndpoint` |
| R1-07 | closed | none | Step 2 line 160; Step 16 line 523 |
| R1-08 | closed | none | Access order reference rows 87–88 (CastVote and RetractVote split, "No group lookup"); Step 3 line 189; Step 10 line 376; Step 1 line 132 names `MapFastEndpoints` and both validation texts; Step 15 line 488 |
| R1-09 | closed | none | Step 12 lines 422–425 and Accept lines 428–429; the BE-18 methods are named in Steps 5, 6, 9 and 11 |
| R1-10 | closed | none | Skill Mapping row 4; Decisions rows 606 (four broadcasts) and 610 (handler-scope departure) |

## Findings

### [HIGH] N-01 — Constructor-injecting the hub's repositories breaks F35's second-read revocation check
**Anchor:** seed-0120 · above seed-0130 · below seed-0169
**Shape:** a pattern change removes the fresh-read property a race fix depends on
**Consequence:** A participant kicked during a join can stay connected and keep receiving updates, with no error anywhere.
**Evidence:** quoted, path, scenario
**Surfaces:** plan § Step 10 "What to build" line 370; `Hubs/SprintHub.cs` lines 41–66 (`JoinSprint`) and 224–230 (`IsParticipantRemovedAsync`); `RepositoryBase.cs` `GetByIdAsync` (`FindAsync`); spec § What must not change ("F35's outcomes: … soft kick")
**Untouchable:** security (a revocation bypass)
**Tag:** round-1 miss: SprintHub.cs lines 30–235
**Source:** the plan says "The hub takes its repositories … by constructor injection". `JoinSprint` calls `RequireActiveParticipantAsync(participantId)` and later `IsParticipantRemovedAsync(participantId)`. Each does `using var scope = scopeFactory.CreateScope()`. The code comment reads "A kick committed before this read is undone here; one committed after it finds this connection in the registry". `GetByIdAsync` is `Db.Set<T>().FindAsync([id], ct)`.
**Issue:** The two reads rely on two scopes, so the second one reads fresh data. With one constructor-injected scoped `ParticipantRepository` there is one `DbContext` per hub invocation. `FindAsync` returns the instance tracked by the first read, with `RemovedAt == null`, without querying again. Scenario: participant 7 calls `JoinSprint`, and the first read sees them active. The scrum master kicks participant 7 and the kick commits. The second read returns the stale tracked entity, so `IsRemoved` is false. The connection stays in the sprint group. The kick handler ran before the registry entry existed, so nothing evicts it. Today's code closes exactly this window. Neither the plan nor the Must-NOT-Change table pins it, and no test exercises it.
**Impact:** A revoked participant keeps live access after the race.
**Fix:** In Step 10, require that the post-registration removed-check reads fresh. For example, add a `ParticipantRepository.IsRemovedAsync` that queries `AsNoTracking`, or keep a scope per read for that one call. Add a code-trace line to Step 10's Accept for the order "register, then re-read".

### [MEDIUM] N-02 — Step 5's `AdvancePhase` has two ordering traps that change a count or a message
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Band:** MEDIUM–HIGH; driver: whether the developer computes the discussed count from the database or from the in-memory list after the focus mark.
**Shape:** ambiguous refactor of an order-dependent endpoint
**Consequence:** A valid advance can be refused, or a refused advance can carry a different message than today.
**Evidence:** quoted, path, scenario
**Surfaces:** plan § Step 5 lines 244–245; `AdvancePhaseEndpoint.cs` lines 39–76; `Sprint.cs` lines 112–115
**Tag:** round-1 miss: AdvancePhaseEndpoint.cs (not opened in round 1)
**Source:** two statements in the plan: "the discussed-count rule moves to `Sprint.EnsureEnoughGroupsDiscussed(int)` … the count comes from the repository", and "where the domain method already throws with the same text, the endpoint check goes".
**Issue 1 (count).** Today the focused group is marked discussed in memory first (`focusedGroup.MarkDiscussed()`, then `sprint.ClearFocus()`). The code then counts `groups.Count(g => g.DiscussedAt.HasValue)` over the tracked list. Scenario: one saved discussed group plus the focused group, and the focused group is not yet discussed. Today the count is 2 and the advance succeeds. A database count, which Step 12 encourages for count-only reads, sees 1 and answers 409. The plan warns about this trap for `CardRegrouping` but not here.
**Issue 2 (order).** The endpoint's "Sprint must be Active to advance phase." check runs before the phase-action check, and `Sprint.AdvancePhase()` runs after it. The domain text is identical (`Sprint.cs:115`). Removing the endpoint check moves that failure behind the phase check. A closed sprint in Reporting would then answer "AdvancePhase is not allowed in phase Reporting." instead of "Sprint must be Active…". The status is unchanged but the message differs, which the plan calls a defect.
**Fix:** In Step 5, say that the count is taken in memory from the loaded groups, after the focus mark, before `AdvancePhase()`. Keep an `EnsureActive("Sprint must be Active to advance phase.")` call at the endpoint's current position, ahead of the phase-action check.

### [LOW] N-03 — Plan header disagrees with itself on file count
**Anchor:** seed-0127 · above none · below seed-0130
**Shape:** stale count after a fold
**Consequence:** A reader cannot tell which size to trust.
**Evidence:** quoted
**Surfaces:** plan lines 6–7
**Tag:** collateral of R1-02
**Source:** line 6 says "198 files" and line 7 says "as today, by size: 17 developer-built steps, 195 files".
**Fix:** Make both lines say 198.

### [LOW] N-04 — F39's header contradicts the merge order the plan and spec now state
**Anchor:** seed-0129 · above seed-0127 · below seed-0123
**Shape:** sibling spec not reconciled
**Consequence:** The merge order is stated differently in two Ready specs.
**Evidence:** quoted
**Surfaces:** plan § Related line 26; spec § Decisions row 2; `F39-FrontendConventions/definition/spec.md` line 5
**Tag:** collateral of R1-03
**Source:** F38 says "F39 merges before F38". F39's header says "Dependencies: … F38 (the error body this feature's API client reads)". F39 reads `ApiError` with `code` and `errors` from F38's body.
**Issue:** The F38 side follows AR-2 correctly. F39 declares F38 as a prerequisite, and nothing says F39 tolerates the current backend shape until F38 lands.
**Fix:** This is not an F38 plan change. Report it to the team lead or the F39 owner to reword F39's dependency line, or to add a "reads both shapes until F38 merges" note.

### [LOW] N-05 — The Reports endpoints add a team-mismatch 404 that the census row omits
**Anchor:** seed-0127 · above none · below seed-0130
**Shape:** inventory gap, mitigated by "the code wins"
**Consequence:** A developer using the census alone could drop a 404.
**Evidence:** path
**Surfaces:** `GetSprintReportEndpoint.cs` lines 26–27; plan Access order reference row 85
**Tag:** round-1 miss: Reports endpoints
**Source:** the Sprint-first row reads "sprint missing → 404; team missing or not owner → 403". The report endpoints use `sprint is null || sprint.TeamId != req.TeamId` → 404.
**Fix:** Add "sprint in another team → 404" to the Sprint-first row's note for the report endpoints.

## Gap Analysis
- **Edge case probed.** `JoinSprint` with a kick committed between the two reads (N-01).
- **Edge case probed.** Focused group undiscussed plus exactly one saved discussed group (N-02).
- **Backward impact.** `CancellationToken.None` in the detached task has no knock-on effect on the existing handlers.
- **Kick handler.** `BroadcastParticipantKickedHandler` (the F35 kick handler) reads `SprintHub` statics; moving them to `ReadinessTracker` keeps the same data and lifetime.
- **Left open.** The Must-NOT-Change row for the F35 soft kick is marked "pinned where tested". The hub path is untested, so N-01 is pinned by nothing.

## Open Questions
None.

Reviewed: D:\src\reflekt-F38-BackendConventions\docs\specs\F38-BackendConventions\delivery\plan.md

### Merge — round 2

Coverage: F35 spec and plan, F40 spec; F39 spec beyond headers; `.claude/skills/*`, `docs/conventions/csharp.md`, `plan-template.md`; `AdvancePhaseCheckEndpoint`, `GetParticipantReportEndpoint`; `web/` consumers

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | sonnet | HIGH | seed-0120 | Constructor-injected hub repositories break F35's second-read revocation check | pattern change removes fresh-read property a race fix depends on | kicked participant stays connected | quoted, path, scenario | security | round-1 miss: SprintHub.cs lines 30–235 | plan Step 10; SprintHub.cs 41–66, 224–230; RepositoryBase.cs | N-01 |
| R2-02 | sonnet | MEDIUM | seed-0130 | AdvancePhase ordering traps (discussed count; Active check order) | ambiguous refactor of an order-dependent endpoint | valid advance refused or message changed | quoted, path, scenario | — | round-1 miss: AdvancePhaseEndpoint.cs | plan Step 5; AdvancePhaseEndpoint.cs 39–76; Sprint.cs 112–115 | N-02 |
| R2-03 | sonnet | LOW | seed-0127 | Plan header file count disagrees (198 vs 195) | stale count after a fold | unclear size | quoted | — | fold | plan lines 6–7 | N-03 |
| R2-04 | sonnet | LOW | seed-0129 | F39 header contradicts F38-first merge order | sibling spec not reconciled | two Ready specs disagree on merge order | quoted | — | fold | plan Related; spec Decisions row 2; F39 spec line 5 | N-04 |
| R2-05 | sonnet | LOW | seed-0127 | Reports endpoints' team-mismatch 404 missing from census | inventory gap | developer could drop a 404 | path | — | round-1 miss: Reports endpoints | GetSprintReportEndpoint.cs 26–27; plan Access order row 85 | N-05 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

Facts re-checked in source before folding: `SprintHub.JoinSprint` (lines 41–66) and `IsParticipantRemovedAsync` (each read in its own `scopeFactory.CreateScope()`, `GetByIdAsync`, "missing or removed" = removed); `AdvancePhaseEndpoint` lines 36–80 (Active 409 before the phase-action check; `MarkDiscussed()` and `ClearFocus()` in memory, then `groups.Count(g => g.DiscussedAt.HasValue)` over the tracked list).

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | HIGH | — | plan Step 10 Files (`ParticipantRepository.cs`), What to build (revocation re-check reads fresh through `ParticipantRepository.IsActiveAsync`, `AsNoTracking` exists query; order check → register → groups → re-check → undo), Accept (code trace), Satisfies/Risk; Must-NOT-Change F35 row; Decisions row "The hub's two participant checks …" (new) |
| R2-02 | fixed-real | MEDIUM | — | plan Step 5 What to build (state-guard rule: an endpoint check stays at its position unless no check sits between it and the domain throw; `AdvancePhase` keeps `EnsureActive` ahead of the phase check; discussed count in memory after the focus mark, Step 12's count queries excluded) and Accept (code trace) |
| R2-03 | fixed-real | LOW | — | plan header `**Lane:**` line (198 files) |
| R2-04 | waived | LOW | — | no F38 change: F38 follows AR-2 (F39 merges before F38). F39's dependency line is F39's to reword; the coordinator sent it to the campaign's shared architect as a notice. F38 edits nothing in F39. plan § Plan Review note |
| R2-05 | fixed-real | LOW | — | plan Access order reference, Sprint-first row (report endpoints' team-mismatch 404) |

Reconciled: Step 5 state-guard rule against the Access order reference (team-first row order unchanged), Step 12's count-query bullet (the `AdvancePhase` exclusion is stated in Step 5, and Step 12 already names only `CardRegrouping`'s count), Domain Model Changes (`EnsureActive`, `EnsureEnoughGroupsDiscussed` unchanged), Decisions rows on the discussed-count guard and the endpoint-text guards (still true); Step 10 against Step 3 (hub takes no policy provider), Step 11 (no blocking wait; `IsActiveAsync` is async), Step 12 (`AsNoTracking` count rises; the method is listed there), the hub texts list in the Access order reference ("Participant has been removed." unchanged), Must-NOT-Change; header Size and Lane lines (both 198); Build slices line (1–4 · 5–7 · 8–10 · 11–14 · 15–17) — unchanged, since every fold stays inside its existing step.
