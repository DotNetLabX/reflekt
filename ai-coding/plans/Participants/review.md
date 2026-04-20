# Participants — Review

## Step 2: Code Review

**Verdict: PASS** — clean implementation, all plan instructions matched.

---

### Step-by-step verification

#### Step 1: Generic ID support in BuildingBlocks — PASS

| Plan instruction | Code | Match |
|---|---|---|
| `IHasDomainEvents` with DomainEvents + ClearDomainEvents | `IHasDomainEvents.cs` — exact match | ✓ |
| `Entity<TId>` with `TId : notnull`, equality via `EqualityComparer<TId>.Default` | `Entity.cs` — exact match | ✓ |
| Non-generic `Entity : Entity<int>` | `Entity.cs:24` | ✓ |
| `AggregateRoot<TId> : Entity<TId>, IHasDomainEvents` | `AggregateRoot.cs` — exact match | ✓ |
| Non-generic `AggregateRoot : AggregateRoot<int>` | `AggregateRoot.cs:13` | ✓ |
| `EntityConfiguration<T, TId>` + non-generic alias | `EntityConfiguration.cs` — exact match | ✓ |
| `RepositoryBase<TContext, TEntity, TId>` with `GetByIdAsync(TId)` | `RepositoryBase.cs` — exact match | ✓ |
| Interceptor: `OfType<IHasDomainEvents>()` | `DomainEventPublishingInterceptor.cs:20` | ✓ |
| `Repository<TEntity, TId>` overload | `Repository.cs:10-13` | ✓ |

**Deviation accepted:** Non-generic alias constraints use `Entity<int>` instead of `Entity` class. This is **correct** — `Team : AggregateRoot : AggregateRoot<int> : Entity<int>` does NOT pass through the non-generic `Entity` class, so `where TEntity : Entity` would not match existing aggregates. The Developer caught a plan error.

#### Step 2: Participant aggregate + domain events — PASS

| Plan instruction | Code | Match |
|---|---|---|
| `Participant : AggregateRoot` (int ID) | `Participant.cs:5` | ✓ |
| Private constructor, properties: SprintId, DisplayName, GuestToken, JoinedAt | `Participant.cs:7-13` | ✓ |
| `Join(string displayName, int sprintId)` factory | `Behaviors/Participant.cs:5` | ✓ |
| Guard: `ThrowIfNullOrWhiteSpace(displayName)` | `Behaviors/Participant.cs:7` | ✓ |
| `GuestToken = Guid.NewGuid().ToString()` | `Behaviors/Participant.cs:12` | ✓ |
| `JoinedAt = DateTime.UtcNow` | `Behaviors/Participant.cs:13` | ✓ |
| Raises `ParticipantJoinedEvent(this)` | `Behaviors/Participant.cs:16` | ✓ |
| Event record: `ParticipantJoinedEvent(Participant) : IDomainEvent` | `ParticipantJoinedEvent.cs:5` | ✓ |

#### Step 3: EF config + DbSet + repository + DI — PASS

| Plan instruction | Code | Match |
|---|---|---|
| Cascade delete on SprintId FK | `ParticipantConfiguration.cs:16` `.OnDelete(DeleteBehavior.Cascade)` | ✓ |
| Unique index on GuestToken | `ParticipantConfiguration.cs:19` | ✓ |
| Index on SprintId | `ParticipantConfiguration.cs:20` | �� |
| DisplayName max 50 | `ParticipantConfiguration.cs:17` | ✓ |
| `GetByGuestTokenAsync` returns single by token | `ParticipantRepository.cs:8-10` `SingleOrDefaultAsync` | ✓ |
| `GetBySprintIdAsync` ordered by JoinedAt asc | `ParticipantRepository.cs:12-16` `.OrderBy(p => p.JoinedAt)` | ✓ |
| DbSet registered | `ReflektDbContext.cs:13` | ✓ |
| Scoped DI registration | `DependencyInjection.cs:28` | ✓ |

#### Step 4: EF Migration — PASS
Migration created, no phantom changes to existing tables.

#### Step 5: Wire GuestTokenAuthenticationHandler — PASS

| Plan instruction | Code | Match |
|---|---|---|
| Constructor-inject ParticipantRepository | `GuestTokenAuthenticationHandler.cs:13` | ✓ |
| No token → `NoResult()` | Line 21-22 | ✓ |
| No match → `Fail("Invalid guest token")` | Line 26 | ✓ |
| Claims: participantId, sprintId, displayName | Lines 28-33 | ✓ |
| Auth type: GuestTokenDefaults.AuthenticationScheme | Line 34 | ✓ |
| Return Success with ticket | Line 36 | ✓ |

#### Step 6: JoinSprint endpoint — PASS

| Plan instruction | Code | Match |
|---|---|---|
| `[AllowAnonymous]` | `JoinSprintEndpoint.cs:8` | ✓ |
| Route: `POST /api/sprints/{sprintId}/join` | Line 9 | ✓ |
| Load sprint → 404 | Lines 16-17 | ✓ |
| Check Active → 409 | Lines 19-20 `ThrowError` with 409 | ✓ |
| `Participant.Join(...)` + save | Lines 22-24 | ✓ |
| Return 201 with JoinSprintResponse | Line 26 | ✓ |
| Validator: NotEmpty + MaxLength(50) | `JoinSprintCommand.cs:11` | ✓ |

#### Step 7: GetCurrentParticipant + GetParticipants — PASS

**GetCurrentParticipant:**
- Auth: Participant policy ✓
- Reads participantId + sprintId from claims ✓
- Validates claim sprintId matches route → 401 ✓
- Loads participant → 404 if null ✓
- Returns ParticipantResponse (no token) ✓

**GetParticipants:**
- Auth: OwnerOrScrumMaster policy ✓
- **Loads sprint first → 404 if null** ✓ (the cross-check gap is properly addressed)
- Guest token path: validates sprintId claim ✓
- Cookie path: loads team, checks `CreatorEmail` with `OrdinalIgnoreCase` ✓
- Returns list of ParticipantResponse ✓

#### Step 8: SprintHub — PASS

| Plan instruction | Code | Match |
|---|---|---|
| JoinSprint validates claim sprintId | `SprintHub.cs:19-21` | ✓ |
| Mismatch → HubException | Line 21 | ✓ |
| OnDisconnectedAsync: reads claims | Lines 31-32 | ✓ |
| Pushes ParticipantLeft to group | Lines 37-38 | ✓ |
| Direct push, not domain event | Correct — no entity modification | ��� |

#### Step 9: ParticipantJoined event handler — PASS
Follows `PhaseAdvancedEventHandler` pattern exactly. Sends `"ParticipantJoined"` with `{ id, displayName }` to `sprint:{sprintId}` group. ✓

#### Step 10: Seed data — PASS
Sprint created AND started before participant seeding. 3 participants (Ana, Mihai, Elena). ClearDomainEvents per participant. Idempotent. ✓
Deviation: Sprint started — correct, participants require Active sprint.

#### Step 11: Frontend stores + SignalR — PASS

- **guest.ts:** loadToken/saveToken/clearToken with correct localStorage key format ✓
- **participant.ts:** join (no auth), fetchMe (X-Guest-Token), fetchParticipants (token + credentials) ✓
- **useSprintHub.ts:** HubConnectionBuilder with accessTokenFactory, auto-reconnect, all three event listeners, deduplication on ParticipantJoined ✓

#### Step 12: Frontend join flow — PASS

- RetroRoomView.vue: full join flow with silent rejoin, stale token handling, name picker, error messages ✓
- Route `/sprint/:sprintId` → RetroRoomView ✓
- Room shell with participant chips + placeholder ✓
- onUnmounted disconnects SignalR ✓

#### Step 13: Unit tests — PASS
5 tests matching plan spec. Uses FluentAssertions. Validates GUID format, unique tokens, domain event reference, and guards. ✓

#### Step 14: Integration tests — PASS
14 tests covering all acceptance criteria. Two factories (auth + anon). Proper cleanup. Helper methods for setup. OwnerOrScrumMaster policy correctly overridden to include both auth schemes. ✓

---

### Findings summary

**Issues: 0**

**Plan corrections needed: 1**
- §3.0 items 5 and 7: non-generic alias constraints should specify `Entity<int>` not `Entity`. The `Entity` alias class is NOT in the inheritance chain of existing aggregates (`Team : AggregateRoot : AggregateRoot<int> : Entity<int>`), so `where TEntity : Entity` would fail to match them. Developer correctly used `Entity<int>`.

**Observations (informational):**
- Participant properties use `private set` where `required init` would be slightly more precise for immutable properties (SprintId, DisplayName, GuestToken, JoinedAt are set once, never mutated). Both work — `private set` is consistent within the Participant class. Not worth changing.
- `guest.saveToken` accepts an extra `displayName` param beyond plan spec — reasonable addition for store consistency.
- Frontend `useSprintHub` adds `withAutomaticReconnect()` — good addition matching spec §7.4.
