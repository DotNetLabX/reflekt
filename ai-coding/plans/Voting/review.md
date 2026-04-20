# Voting — Code Review

**Reviewer:** Architect
**Date:** 2026-04-20
**Verdict:** Approved with one required fix

---

## Step 2 Results

### Domain Layer ✓

- `Vote.cs` — correct partial-class split, properties match plan §3.1 exactly.
- `Behaviors/Vote.cs` — `Create` factory and `MarkRetracted` method match plan. No guards per plan (budget/card-type checks at handler level per architecture doc §5.2). Events raised correctly.
- `VoteCastEvent.cs` / `VoteRetractedEvent.cs` — correct `IDomainEvent` records carrying aggregate reference.

### Persistence Layer ✓

- `VoteConfiguration.cs` — matches plan §4.2: cascade on Card, restrict on Participant, indexes on CardId and ParticipantId.
- `VoteRepository.cs` — all 6 planned methods + `GetVoteCountsByCardIdsAsync` (bulk query for ListCards). Clean LINQ queries. `VoteDistributionEntry` defined here at persistence level (separate from API contract type in same name — acceptable cross-layer separation).
- `ReflektDbContext.cs` — `DbSet<Vote>` registered. ✓
- `DependencyInjection.cs` — `VoteRepository` registered. ✓

### API Endpoints ✓

- **CastVoteEndpoint** — handler flow matches plan Step 4 exactly: claim check → card null/sprint mismatch (404) → ActionItem guard (400) → sprint null (404) → closed (409) → budget exhausted (409) → create → save → return counts (201). Auth policy correct.
- **RetractVoteEndpoint** — matches plan Step 5: claim check → sprint load → closed (409) → find vote (409 if null) → MarkRetracted → Remove → save → return counts (200). Correct.
- **GetMyVotesEndpoint** — matches plan Step 6: claim check → sprint load → get distribution → return. Correct.
- **VoteContracts.cs** — `VoteResponse(int CardId, int VoteCount, int MyVoteCount)` matches spec. `VoteDistributionEntry` matches API surface.

### SignalR Event Handlers ✓

- Both handlers load card for SprintId, query newCount, send to correct group with payload `{ cardId, newCount }`. Matches spec §7.1 field name exactly.

### ListCards Extension ✓

- `CardContracts.cs` updated with `int VoteCount = 0` default parameter — backwards compatible.
- `ListCardsEndpoint.cs` uses bulk `GetVoteCountsByCardIdsAsync` — correct, avoids N+1. Maps per-card count via `GetValueOrDefault(c.Id, 0)`.

### Seed Data ✓

- Idempotent via `!db.Votes.Any()`. Vote distribution matches plan §8 (Participant A: 3 votes, Participant B: 2 votes).

### Frontend ✓ (one fix needed)

- **vote.ts** — correct store shape: state (myVotes, voteCounts, votesPerParticipant), getters (remainingBudget, myVotesOnCard, voteCountForCard), optimistic actions with revert on failure.
- **useSprintHub.ts** — VoteCast/VoteRetracted listeners correctly call `setVoteCount`.
- **CardItem.vue** — vote button with ▲ count, retract button (−), "mine" indicator, disabled when budget=0. Only shows when `showVoting` prop is true.
- **RetroRoomView.vue** — passes `:show-voting="true"` to WentWell and DidntGoWell columns, does NOT pass it to ActionItems column. ✓ Correct per spec rule 4.
- **card.ts** — `voteCount` field added to Card interface, `initVoteCounts` called after fetch.
- Data loading sequence: `fetchCards` → `initVoteCounts` → `fetchMyVotes` → `connect`. ✓

### Tests ✓

- **Unit tests (5)** — cover Create properties, VoteCastEvent raised, event reference, MarkRetracted event, retracted event reference. Correct.
- **Integration tests (20)** — comprehensive coverage: all CastVote error codes, multiple votes same card, budget exhaustion+restoration, RetractVote flows, GetMyVotes distribution, ListCards voteCount. Test setup helpers are clean and reusable.

---

## Issues

### 🔴 Required Fix: Frontend `castVote` missing Content-Type and body

**File:** `web/reflekt-web/src/stores/vote.ts`, line 57-59

The `castVote` action sends a POST with no body and no `Content-Type` header:
```ts
const res = await fetch(`/api/sprints/${sprintId}/cards/${cardId}/votes`, {
  method: 'POST',
  headers: { 'X-Guest-Token': guestStore.token },
})
```

The integration tests discovered this exact issue (done.md: "POST endpoints require `Content-Type: application/json` even with route-only params") and fixed it by adding `Content = JsonContent.Create(new { })`. The frontend code was not updated with the same fix.

**Fix:** Add `Content-Type` header and empty JSON body:
```ts
const res = await fetch(`/api/sprints/${sprintId}/cards/${cardId}/votes`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json', 'X-Guest-Token': guestStore.token },
  body: JSON.stringify({}),
})
```

### 🟡 Minor: `votesPerParticipant` hardcoded to 3

**File:** `web/reflekt-web/src/stores/vote.ts`, line 13

The store uses `votesPerParticipant: 3` as a constant. If a sprint is created with a different budget, the remaining-votes display will be wrong. The server enforces the real budget, so functionality is correct — only the UI counter is misleading.

**Acceptable for now** — the sprint data is already available from `GET /api/sprints/{sprintId}` but integrating it would touch Sprint-related code outside this plan's scope. Flag for a follow-up if sprints with non-default budgets become common.

### 🟡 Style nit: null-forgiving operator

**File:** `RetractVoteEndpoint.cs`, line 43: `vote!.MarkRetracted()`

The `!` is redundant — `ThrowError` on line 41 throws before reaching line 43 when vote is null. Not a bug; just a compiler-warning suppression. No action needed.

---

## Verdict

One required fix (frontend POST body). Apply it and the feature is complete. No architectural deviations found — implementation follows the plan faithfully.
