# Card Grouping — Review

## Verdict: APPROVE (after Cycle 1 fixes)

---

## Pre-commitment Predictions

| Prediction | Actual |
|---|---|
| `CardsGroupedEvent`/`CardUngroupedEvent` never raised — domain events exist but endpoints bypass them | CONFIRMED (HIGH) |
| `GroupDissolved` frontend handler dead code — server never emits this event | CONFIRMED (MEDIUM) |
| Card deletion in Grouping phase leaves orphan solo groups | CONFIRMED (MEDIUM) |
| Double `SaveChangesAsync` in `CreateGroup` causes two DB round-trips | CONFIRMED — assessed as LOW |
| `GetByIdAsync` missing on CardGroupRepository | NOT FOUND — inherits from base |

---

## Findings

### [HIGH] `CardsGroupedEvent` and `CardUngroupedEvent` are never raised

**Files:**
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/AddCardToGroup/AddCardToGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/RemoveCardFromGroup/RemoveCardFromGroupEndpoint.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/CardsGroupedEvent.cs`
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/Events/CardUngroupedEvent.cs`

**Issue:** A grep for `new CardsGroupedEvent` and `new CardUngroupedEvent` across the entire `src/` tree returns zero results. Both domain event types are defined and have corresponding `IEventHandler<T>` broadcast handlers (`BroadcastCardsGroupedHandler`, `BroadcastCardUngroupedHandler`), but no code ever calls `AddDomainEvent(new CardsGroupedEvent(...))` or `AddDomainEvent(new CardUngroupedEvent(...))`. This means:

1. The `CardsGrouped` SignalR event is never broadcast when a card is moved between groups. Other participants in the sprint room see stale group state until they refresh.
2. The `CardUngrouped` SignalR event is never broadcast when a card is removed from a group. Same stale-state problem.
3. The plan (Step 6, domain event section) explicitly specified these events must be raised: "For `CardsGroupedEvent` and `CardUngroupedEvent`, include additional context..."

The `Card.SetGroupId()` method intentionally has no domain event (per plan), so the raise must happen in the endpoint after the group-level operation. `AddCardToGroupEndpoint` calls `card.SetGroupId(group.Id)` but never raises `CardsGroupedEvent`. `RemoveCardFromGroupEndpoint` calls `card.SetGroupId(soloGroup.Id)` but never raises `CardUngroupedEvent`.

**Fix:** In `AddCardToGroupEndpoint.HandleAsync`, after `card.SetGroupId(group.Id)`, raise `group.AddDomainEvent(new CardsGroupedEvent(group, req.CardId, sourceGroupId ?? 0))`. In `RemoveCardFromGroupEndpoint.HandleAsync`, after `card.SetGroupId(soloGroup.Id)`, raise `soloGroup.AddDomainEvent(new CardUngroupedEvent(group, req.CardId, soloGroup.Id))` (note: `SourceGroup` in the event should be the original group, consistent with the event definition). However: domain events must be raised on an aggregate; the endpoint should call a method that does so, or add them directly via `AddDomainEvent`. Review which aggregate is the right owner — `CardGroup` is the aggregate here. The events should be published via `group.AddDomainEvent(...)` or `soloGroup.AddDomainEvent(...)` before `SaveChangesAsync` so the `DomainEventPublishingInterceptor` dispatches them.

---

### [MEDIUM] Frontend registers `GroupDissolved` SignalR handler that server never emits

**File:** `web/reflekt-web/src/composables/useSprintHub.ts:108`

**Issue:**
```ts
connection.on('GroupDissolved', () => {
  cardGroupStore.fetchGroups(sprintId)
})
```
The server never sends a `GroupDissolved` message. `DissolveGroupEndpoint` creates solo groups (each suppressing their `CardGroupCreatedEvent` via `ClearDomainEvents()`) and removes the original group, but emits no SignalR notification at all. The handler in `useSprintHub.ts` is dead code — it will never fire.

The documented deviation says "The frontend re-fetches groups instead, which is simpler and consistent with the PhaseChanged pattern." But that re-fetch is only triggered by `GroupCreated` (which is also suppressed during dissolve). The net result: when the SM dissolves a group, **other participants' UIs do not update** — they continue showing the old multi-card group until they navigate away or a different event triggers a re-fetch.

**Fix:** Two options — either (a) stop suppressing `CardGroupCreatedEvent` in `DissolveGroupEndpoint` so the `BroadcastGroupCreatedHandler` fires for each new solo group (this fires N broadcasts, which is the reason suppression was added), or (b) emit a `GroupDissolved` SignalR event directly from `DissolveGroupEndpoint` after removing the original group, so that the frontend handler actually gets triggered. Option (b) is cleaner: add a direct `IHubContext<SprintHub>` call in the endpoint (consistent with the overall pattern), or create a `GroupDissolvedDomainEvent` + handler. Remove the dead handler from `useSprintHub.ts` only after the server-side emit is in place.

---

### [MEDIUM] Card deletion in Grouping phase leaves orphan solo groups

**File:** `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/DeleteCardEndpoint.cs:58-60`

**Issue:** `DeleteCardEndpoint` removes a card without checking whether the card is the sole occupant of a solo group. If a card has `GroupId` set (i.e., the sprint is in Grouping phase or later), deleting the card via `SET NULL` on the FK leaves the `CardGroup` row in the database with zero cards. This orphan group:
- Appears in `GetGroups` responses (a group with an empty `cards` array)
- Appears in voting phase as a votable item with no content
- Appears in the report as an empty group entry

The `DeleteNote` action is permitted in Writing phase (before groups exist) and in Discussing/Reporting phases per seed data. Whether it is permitted in Grouping phase depends on the phase action matrix — but regardless, any phase where `DeleteNote` is allowed after groups exist can orphan a group.

**Fix:** In `DeleteCardEndpoint`, after removing the card, check if `card.GroupId` has a value and — if after removal no other card points to that group — remove the group as well. This requires injecting `CardGroupRepository` into `DeleteCardEndpoint`. Alternatively, enforce a domain invariant that prevents deleting a card that is the last in a multi-card group without dissolving first, but that is a planner-level architecture call. At minimum the orphan cleanup should be done the same way `AddCardToGroupEndpoint` and `CreateGroupEndpoint` already do it.

---

### [LOW] Double `SaveChangesAsync` in `CreateGroupEndpoint` is a minor inefficiency

**File:** `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs:60-61, 90`

**Issue:** The endpoint calls `SaveChangesAsync` twice — once immediately after `AddAsync(newGroup)` to materialise the new group's auto-increment `Id` (so it can be used in `SetGroupId` calls), and again after updating card `GroupId`s and removing empty source groups. This is a deliberate pattern because EF Core SQLite requires the first save to obtain the PK before FK assignment. The implementation is correct and the two-save pattern is used consistently elsewhere (e.g., `DissolveGroupEndpoint:57-58`). No action required.

---

## Positive Observations

- Domain model split (data/behavior partial classes) is clean and consistent with existing aggregates.
- `ClearDomainEvents()` usage in `AdvancePhaseEndpoint` to suppress N broadcasts during bulk solo-group creation is a sound pattern, consistently applied.
- Authorization in all new endpoints exactly mirrors the existing pattern (sprint lookup → team lookup → email comparison for SM; claims-based check for guest/participant).
- `CardGroupConfiguration` FK/index/cascade setup is correct: `CASCADE` on `Sprint→CardGroup`, `SET NULL` on `Card→CardGroup`, `CASCADE` on `CardGroup→Vote`.
- Migration is clean — `Down()` is symmetric and the phase action seed data rebalances correctly.
- Test coverage is thorough: `VoteEndpointTests` was fully restructured to be group-based and covers the happy path plus all key error cases.
- `GetGroupsEndpoint` correctly gates `voteRepository.GetVoteCountsByGroupIdsAsync` behind `>= SprintPhase.Voting` to avoid unnecessary DB calls in Grouping phase.
- `DbInitializer` correctly seeds solo groups and votes referencing `GroupId`, ensuring a clean dev environment after migration.
- `RetroRoomView` inline `GroupDissolved` re-fetch in the SM's own `dissolveGroup` action store call provides immediate local UI update for the SM — the gap is only for other participants.

---

## Gaps

- No test coverage for `GroupCreated` / `CardsGrouped` / `CardUngrouped` SignalR events firing (integration tests use HTTP polling, not SignalR).
- No test for dissolve → verify other-participant group list updates (would require SignalR test infrastructure).
- No test that deleting the last card in a solo group produces a clean result (no orphan group).
- `CreateGroup` does not validate that the selected cards are not already in the same multi-card group (grouping cards already together returns a new duplicate group — functional but wasteful).

---

## Open Questions

- Is `DeleteNote` permitted in Grouping phase? If so, the orphan group issue is exploitable in normal use. If not (i.e., the phase action matrix gates it out), the risk is limited to Discussing/Reporting phases where groups already exist from Voting. This needs confirmation against the phase action seed data (enum values in migration are integers — needs cross-referencing with `SprintAction` enum order).

---

## Evidence

### Initial review (pre-fix)

| Check | Result | Command | Output |
|---|---|---|---|
| Build | PASS | `dotnet build Reflekt.API.csproj` | 0 errors, 0 warnings |
| Unit tests | PASS | `dotnet test Reflekt.UnitTests.csproj` | 75/75 passed |
| Integration tests (run 1) | 1 FAIL (flaky) | `dotnet test Reflekt.IntegrationTests.csproj` | `CastVote_Returns201` 500 — passed when run alone |
| Integration tests (run 2) | PASS | `dotnet test Reflekt.IntegrationTests.csproj` | 105/105 passed |
| `CardsGroupedEvent` raised | FAIL | `grep "new CardsGroupedEvent" src/` | 0 matches |
| `CardUngroupedEvent` raised | FAIL | `grep "new CardUngroupedEvent" src/` | 0 matches |
| `GroupDissolved` server emit | FAIL | `grep "GroupDissolved" src/` | 0 matches in server code |

### Cycle 1 re-review (post-fix)

| Check | Result | Command | Output |
|---|---|---|---|
| Build | PASS | `dotnet build Reflekt.API.csproj` | 0 errors, 0 warnings |
| Unit tests | PASS | `dotnet test Reflekt.UnitTests.csproj` | 75/75 passed |
| Integration tests | PASS | `dotnet test Reflekt.IntegrationTests.csproj` | 105/105 passed |
| `RecordCardAdded` raises `CardsGroupedEvent` | PASS | Code read | `CardGroup.Behaviors/CardGroup.cs:33-36` |
| `RecordCardRemoved` raises `CardUngroupedEvent` | PASS | Code read | `CardGroup.Behaviors/CardGroup.cs:38-41` |
| `AddCardToGroup` calls `RecordCardAdded` before save | PASS | Code read | `AddCardToGroupEndpoint.cs:56` |
| `RemoveCardFromGroup` calls `RecordCardRemoved` before save | PASS | Code read | `RemoveCardFromGroupEndpoint.cs:54` |
| `DissolveGroup` emits `GroupDissolved` to hub | PASS | Code read | `DissolveGroupEndpoint.cs:67-68` |
