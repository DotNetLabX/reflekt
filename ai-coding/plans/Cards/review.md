# Cards — Review

## Step 2 Result: Code Review

### Verdict: PASS with 1 bug, 2 minor items

---

### BUG: Duplicate card on createCard success

**Severity:** Medium — affects every card creation in the UI

**Location:** `web/reflekt-web/src/stores/card.ts:59` + `web/reflekt-web/src/views/RetroRoomView.vue:242-244`

**What happens:**
1. `RetroRoomView.addNote` pushes an optimistic card (negative ID) to `cardStore.cards`
2. Calls `cardStore.createCard(...)` which fires the API call
3. On API success, `createCard` action does `this.cards.unshift(card)` — adds the real card (positive ID)
4. Back in the component, `cardStore.cards[idx] = created` replaces the optimistic card with the real card
5. Result: the real card exists **twice** in the array

**Root cause:** Both the store action and the component add the card. The store's `createCard` should not touch the array — the component handles the optimistic lifecycle, and the SignalR `CardCreated` handler has its own deduplicate check for cards from other participants.

**Fix:** Remove `if (!this.cards.some((c) => c.id === card.id)) this.cards.unshift(card)` from the `createCard` action in `card.ts`. The action should only perform the API call and return the card. The component already handles adding (optimistic) and replacing (on success). The SignalR handler handles cards from other participants.

Same pattern applies to `addActionItem` in RetroRoomView — identical flow, identical bug.

---

### MINOR: Misleading test name

**Location:** `tests/Reflekt.IntegrationTests/CardEndpointTests.cs:193`

The test `CreateCard_NonexistentSprint_Returns404` asserts `HttpStatusCode.Unauthorized` (401). The name should match the actual behavior: `CreateCard_NonexistentSprint_Returns401`.

The deviation itself is correct and documented in done.md — the claim check fires before the DB lookup.

---

### MINOR: CardItem allows empty assignee submit for action items

**Location:** `web/reflekt-web/src/components/CardItem.vue:78-79`

When editing an action item, clearing the assignee and clicking Save sends `null` to the backend, which rejects with 400. The optimistic UI catches this and reverts, so it works correctly — but the Save button should be disabled when the assignee is empty for ActionItem cards, same as the "Add" button in the Action Items column does.

Not a bug (backend enforces the rule), but a UX gap.

---

### Plan compliance — instruction by instruction

| Plan Instruction | Code | Match? |
|---|---|---|
| §3.1 Enums: ColumnType, CardType | Exact match | ✅ |
| §3.2 Card properties: 8 fields, private setters, private constructor | Exact match | ✅ |
| §3.2 Create factory: 6 params, 5 guards, timestamps, event | All guards present, exact sequence | ✅ |
| §3.2 Update method: text + assignee guards, stamps UpdatedAt, raises event | Exact match | ✅ |
| §3.2 MarkDeleted: raises CardDeletedEvent | Exact match | ✅ |
| §3.3 Events: 3 events carrying Card reference | Exact match | ✅ |
| §4.2 CardConfiguration: cascade/restrict, indexes, max lengths | Exact match | ✅ |
| Step 2 Repository: GetBySprintIdAsync desc, Remove | Exact match | ✅ |
| Step 2 DbSet + DI registration | Verified in ReflektDbContext + DependencyInjection | ✅ |
| Step 4 CardContracts: CardResponse + CardMappings.ToResponse | Enum serialized as string via .ToString() | ✅ |
| Step 4 CreateCardCommand + validator | Text NotEmpty MaxLength(2000), enums NotEmpty, AssigneeName MaxLength(50) | ✅ |
| Step 4 CreateCardEndpoint: Participant policy, claim check, enum parse, sprint load, closed check, factory + catch, 201 | Exact match to handler flow | ✅ |
| Step 5 EditCardCommand + validator | Text NotEmpty MaxLength(2000), AssigneeName MaxLength(50) | ✅ |
| Step 5 EditCardEndpoint: OwnerOrScrumMaster, dual-auth ownership, card.Update + catch, 200 | Follows GetParticipantsEndpoint pattern exactly | ✅ |
| Step 6 DeleteCardEndpoint: OwnerOrScrumMaster, MarkDeleted → Remove → SaveChanges → 204 | Exact sequence match | ✅ |
| Step 7 ListCardsEndpoint: OwnerOrScrumMaster, dual-auth, sprint 404, mapped list | Follows GetParticipantsEndpoint pattern | ✅ |
| Step 8 SignalR handlers: Created/Updated send full response, Deleted sends {id} | Exact match | ✅ |
| Step 9 Seed data: 5 cards, 3 columns, idempotent, ClearDomainEvents | 2 WentWell + 2 DidntGoWell + 1 ActionItem, idempotent | ✅ |
| Step 10 Card store: interface, state, getters, CRUD actions | Exact match (+ CreateCardPayload, EditCardPayload types) | ✅ |
| Step 10 SignalR events: CardCreated deduplicate+unshift, CardUpdated replace, CardDeleted filter | Exact match | ✅ |
| Step 11 Three-column layout, card input per column, assignee dropdown, optimistic UI, canEdit check | All implemented. Grid-cols-3, empty states, toast on failure | ✅ |
| Step 12 Unit tests: 13 test cases | 13 tests, all cases covered | ✅ |
| Step 13 Integration tests: 26 test cases | 26 tests, all cases covered | ✅ |
| CardItem.vue (extra): inline edit, canEdit gating, edit/delete emits | Clean component, reasonable decomposition | ✅ |

### No new patterns invented ✅
### Domain rules enforced in aggregate, not endpoints ✅
### Build passes ✅ (per done.md)
### No scope creep ✅
