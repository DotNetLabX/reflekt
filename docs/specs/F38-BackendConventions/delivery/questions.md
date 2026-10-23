# F38 — Backend Conventions Conformance — Questions

## Q1: The policy constant cannot be written as a bare `Policies.X` on an endpoint
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 2 — Claim names, typed claim accessors, merged policies, `Features/Account`
**File:** the 39 endpoint files with `using AuthPolicies = Reflekt.API.Auth.Policies;`

**Context:** Step 2 says that renaming `Features/Auth` to `Features/Account` lets `Policies` resolve without the `AuthPolicies` alias (the audit's BE-29 reading: "the namespace `Reflekt.API.Features.Auth` shadows `Reflekt.API.Auth`"). That holds for `Auth.Policies`, but not for a bare `Policies`.
- The policy is set by a class attribute, `[Authorize(Policy = AuthPolicies.ScrumMaster)]`, on classes that derive from FastEndpoints' `Endpoint<,>` / `EndpointWithoutRequest`.
- FastEndpoints 5.34.0 declares an inherited method `Endpoint<TRequest,TResponse>.Policies(string[])` (`FastEndpoints.xml`, member `M:FastEndpoints.Endpoint`2.Policies(System.String[])`).
- A class attribute's arguments are bound with the class's members in scope, inherited ones included, so a bare `Policies` binds to that method. Confirmed with a compiler probe (net10.0, a base class with a protected `Policies(params string[])` method, a static class `App.Auth.Policies`, `[Attr(Policy = Policies.SignedInUser)]` on the derived class): `error CS0119: 'BaseEp.Policies(params string[])' is a method, which is not valid in the given context`. In the same probe, `Auth.Policies.SignedInUser` compiled once no `Features.Auth` namespace existed. The probe is deleted.
- So the rename is still needed, but what it enables is the qualified form `Auth.Policies.X`: inside `Reflekt.API.Features.*`, `Auth` then resolves to `Reflekt.API.Auth` instead of `Reflekt.API.Features.Auth`. The test classes do not derive from an endpoint, so `Policies.X` works there unchanged.

**Question:** Should the endpoint attributes be written as `Auth.Policies.SignedInUser` (no alias), given that a bare `Policies.SignedInUser` does not compile on a FastEndpoints endpoint?

**Recommendation:** `[Authorize(Policy = Auth.Policies.SignedInUser)]` (Recommended) — no alias, one using fewer per file; every Step 2 accept grep passes as written (`AuthPolicies` gone, `Features.Auth` gone); the rename still does the work the audit wanted.
`using static Reflekt.API.Auth.Policies;` with `[Authorize(Policy = SignedInUser)]` — also alias-free, but the constant name loses its class, which reads worse and differs from the tests and `AuthorizationConfiguration`.
Keep an alias under another name — contradicts the step's "the alias goes from every file".
**Confidence:** high — the compiler error is reproduced; the recommended form meets every accept line of Step 2 and changes no route, policy or status.

### Answer
Answered by the architect: yes. Endpoint attributes are written `[Authorize(Policy = Auth.Policies.SignedInUser)]` (and so on for each constant), with no alias and no `using static`. Files that are not endpoint classes (`AuthorizationConfiguration`, the hub, the tests) keep `Policies.X`, or `Auth.Policies.X` where `Policies` does not resolve. Your analysis holds: the rename to `Features/Account` is still what frees `Auth` to resolve to `Reflekt.API.Auth`, and a bare `Policies` on a FastEndpoints endpoint binds to the inherited `Policies(string[])` method (CS0119). Plan Step 2 is amended to say so, and adds one accept line: `grep -rn "Policy = Policies\." src/Services/Reflekt/Reflekt.API/Features` → no hits. Nothing else changes: same routes, policies and statuses. Continue Step 2.

## Q2: Fix list cycle 1, row 1 — restoring cross-request broadcast order reverses plan P5
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Fix Round 1, row 1 (review.md § Step 2 — Fix list, cycle 1)
**File:** `src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs`

**Context:** Row 1 (HIGH) asks that a broadcast from one request can no longer overtake a later request's broadcast, and the owner's standing rule for this round says rows 1–2 restore the pre-F38 behaviour exactly (same frames, same order, same values). Every design that does so reverses a current plan decision: P5 and plan Decisions rows 601, 609 and 619 decided that the save never awaits the publication ("Awaiting the publication" is a turned-down option, twice), and the spec row "Hub, timer, events" says "event publishing stays fire-and-forget". So this is the architect's call, not mine.
Facts read at base `1228e29`: the four moved sends (`GroupCreated`, `GroupDissolved`, `VoteCast`, `VoteRetracted`) were awaited inline in their endpoints after commit, before the HTTP response; every other handler (18 of the 22 today) ran under `Mode.WaitForNone`, i.e. not awaited by the request. Today all 22 run in one `Task.Run` the request never awaits (`DomainEventPublishingInterceptor.cs:135`). The handlers only send to SignalR, except two that also write a job to the OpenGraph `Channel` and one that restarts the phase timer — none does network I/O inline.

**Question:** Which design should restore the pre-F38 cross-request order?

**Recommendation:**
A (Recommended) — Await the batch in the save: `SavedChangesAsync` and `TransactionCommittedAsync` await `PublishInOrderAsync` instead of starting `Task.Run`; failures stay logged one per handler and are never rethrown, handlers keep `CancellationToken.None`. About ten lines in one file. Every send completes before the response, as the four inline sends did; a failed send still returns the normal status (spec behaviour change 2 kept); AC2 and the one broadcast mechanism stay. Cost: P5, rows 601/609/619 and the spec's "fire-and-forget" wording are amended, and the 18 handlers that were `WaitForNone` before F38 now also finish before the response (a small latency change for those requests; their frames and order do not change).
B — Await only events whose sends were inline before F38: a marker interface on the four events in `Blocks.Domain`, and the interceptor awaits batches that contain one. Exact pre-F38 timing for every handler, but a new building-block concept and a split publication path.
C — A per-sprint ordered dispatch queue (Codex's first suggestion): new infrastructure in the API; order is kept but the request still returns first, and the handlers still re-read state at dispatch time (row 2 stays open unless Q3 takes snapshots).
D — Put the four sends back inline in their endpoints: exactly the pre-F38 frames, order and values, but reverses the spec row "One broadcast mechanism" (BE-03/SK-13) and AC2, and a failing send fails a committed request again (undoes behaviour change 2).
**Confidence:** medium — A is the smallest change that restores the "send before response" order and keeps the spec's two behaviour points, but it rests on the 18 other handlers staying cheap (read today, not measured under load), and it amends a decision the architect and the spec both wrote.

### Answer
Answered by the architect, 2026-10-23: **A.** `SavedChangesAsync` and `TransactionCommittedAsync` await the publication of their batch; remove `PublishDetached` and the `Task.Run`. Keep everything else in the interceptor as it is: events in the collected order, each published with `Mode.WaitForAll`, every handler failure logged one per handler, nothing rethrown, handlers on `CancellationToken.None`.

Why A:
- It restores exactly what row 1 found broken. Before F38 the four moved sends completed before their request's response, so a broadcast could not be overtaken by one from a request sent after that response. With the save awaiting the batch, the same holds again (Codex's vote 1 / retract 0 sequence included).
- It keeps the frames, the payloads and the order within one request: the publication code does not change, only who waits for it.
- It keeps behaviour change 2 (a failed send is logged, the request returns its normal status) and AC2's one broadcast mechanism. It also brings the interceptor closer to the domain-patterns skill, whose publisher awaits `WaitForAll`.
- B gives the other 18 handlers their exact pre-F38 timing, but at the cost of a new building-block concept and two publication paths, in a feature whose purpose is "one way to do each job". The only thing B buys is a little less request time for those 18. Their frames, order and values are the same under A. C still sends after the response and leaves row 2 open. D reverses AC2 and behaviour change 2.

**No owner decision is reversed.** P5, the Decisions rows and the spec's "fire-and-forget" wording are the architect's own: the spec is a tech-spec the architect owns, and SK-03 in the F34 audit already named awaiting `WaitForAll` as the skill's pattern. The owner's standing rule ("no new product behaviour") is what this answer applies.

Plan and spec changes (done):
- **Step 4:** the save and the commit await the batch; new accept line `grep -n "Task.Run" …DomainEventPublishingInterceptor.cs` → no hits; code trace (a) reworded (the save completes normally when a handler throws); new trace (e): the two callbacks return only after the last event of their batch is published.
- **Decisions rows:**
  - The publication row now says the save awaits the batch.
  - "The four moved broadcasts…" now says they reach clients before the response, as before.
  - "Detached publication token" is renamed "Publication token" and keeps `None`. The batch now runs inside the request, so the token matters only after a client aborts post-commit; there `None` still informs the room of committed data.
  - The domain-patterns `Departs from` row is narrowed: the save awaits as the skill does; the departure is only that failures are logged, not rethrown (still required by behaviour change 2).
- **P5** is superseded by **P7**.
- **Spec:** the row "Hub, timer, events" and behaviour change 2's rejected alternative are amended, each marked "amended 2026-10-23, fix round 1, Q2".

Residual to record in your fix-round block (accepted, listed for F40 in P7): the OpenGraph channel is bounded at 100 with `FullMode.Wait`. While 100 fetches are queued, an attach-link request now waits for a free slot before it answers. Correction to your context line: one handler writes to that channel (`BroadcastAndFetchOnEvidenceLinkAttachedHandler`), not two.

Runs for the round: the Step 4 runs (`VoteEndpointTests.cs`, `CardEndpointTests.cs`, `SprintEndpointTests.cs`) plus `EvidenceLinkEndpointTests.cs`, which exercises the channel handler now awaited in the request. Continue the fix round with rows 2–5.

## Q3: Fix list cycle 1, row 2 — snapshot values in events conflict with the CLAUDE.md event rule
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Fix Round 1, row 2 (review.md § Step 2 — Fix list, cycle 1)
**File:** `Features/CardGroups/CreateGroup/BroadcastGroupCreatedHandler.cs`, `Features/Votes/{CastVote,RetractVote}/BroadcastVoteCountOn*Handler.cs`

**Context:** Row 2 (MEDIUM) asks the handlers to send the operation's own values (the created group's card ids, the vote count) instead of re-reading after commit. Codex's suggested fix — capture them on the event when it is raised — conflicts with the root CLAUDE.md rule "Domain events carry a reference to the aggregate object, not individual properties" (guardrail-enforcement: I must name the conflict and ask). It also has no clean raise point: `CardGroup` does not hold its cards (cards carry `GroupId`), and `Vote.Create` cannot know the group's count, so the endpoint would have to fill the event.
Before F38: `GroupCreated.cardIds` came from the request's in-memory `selectedCards`; `VoteCast.newCount` was `CountByGroupAsync` read inside the serializable transaction after the insert, then sent after commit; `VoteRetracted.newCount` was read after the delete, then sent.
If Q2 is answered A, each handler's read runs inside the request, right after commit and before the response, so a later request's change can reach it only by committing in that gap — the same class of gap pre-F38 had between its read and its send. If Q2 is answered D, this question is moot.

**Question:** Should the three handlers carry snapshot values on their events, or is Q2's awaited send enough for row 2?

**Recommendation:**
A (Recommended) — No snapshot; Q2-A's awaited send closes row 2 to the pre-F38 window. Events keep the aggregate reference, as CLAUDE.md requires; the residual gap is recorded in the fix-round block for the reviewer.
B — Snapshot on the events: `CardGroupCreatedEvent` gains the card ids and `VoteCastEvent` / `VoteRetractedEvent` gain the new count, set by the endpoint. Values exactly as the operation saw them, but needs an owner-approved exception to the CLAUDE.md event rule and moves event construction out of the aggregate.
**Confidence:** medium — A keeps the project rule and matches pre-F38 in every non-concurrent sequence; under a commit in the gap it sends the newer value, where pre-F38 sent the older one.

### Answer
Answered by the architect, 2026-10-23: **A — no snapshot values.** The three events keep their aggregate reference and the handlers keep reading the card ids and the vote count through their scope. Q2's answer makes that read run after the commit and before the response. Row 2 is closed by the Q2 change; this answer adds no code change of its own.

Why:
- **B would reverse an owner rule.** The root `CLAUDE.md` rule "Domain events carry a reference to the aggregate object, not individual properties" is the owner's own instruction, and the campaign leaves `CLAUDE.md` to F40. An exception needs the owner, and this run is unattended. A is also the fallback that reverses nothing.
- **A restores the pre-F38 values wherever pre-F38 had a fixed value.** For requests sent one after another's response, the handler now reads the same thing the endpoint used to send:
  - `GroupCreated.cardIds`: the selected cards, all now in the group, in the sprint list's order;
  - each `newCount`: the count right after the operation.
- **The residual is a race pre-F38 already had.** A later request can change the value only by committing in the gap between this request's commit and its read. Two concurrent requests could already interleave their reads and sends before F38, so this is not new behaviour.

Plan changes (done): one new Decisions row ("No snapshot values on events") and **P8**. Record the residual in your fix-round block for the reviewer: "under a concurrent commit between this request's commit and the handler's read, the handler sends the newer value; pre-F38 sent the value read inside the operation". Mark row 2 as fixed by the Q2 change.
