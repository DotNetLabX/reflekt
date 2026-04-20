# Participants Plan — Reflekt v1

**Status:** Ready for implementation
**Prerequisite:** Auth (landed), Teams (landed), Sprints (landed)
**Prerequisite for:** Cards (needs `ParticipantId`), Votes, EvidenceLinks, Reports
**Companion docs:** `docs/features/Participants.md`, `docs/architecture/v1.md` (§5.1, §6, §7.1–7.2)

## 0. Purpose

Introduce the Participant aggregate — the ephemeral guest identity that enables multi-user retros. A participant joins an Active sprint via a room link, picks a display name, receives a server-generated guest token, and can rejoin silently on refresh. This feature wires up the `GuestTokenAuthenticationHandler` (currently a TODO stub), enables SignalR group membership validation in `SprintHub`, and establishes the ownership foundation that Cards and Votes will build on.

**Non-goals for Participants:**
- No cards, votes, or evidence links — those are separate features.
- No retro room UI (card columns, voting) — that's Cards/Votes.
- No report generation — that's Reports.
- No online/offline presence indicators — ParticipantLeft fires on disconnect but the frontend does not display real-time connection status.
- No display name editing after join.

If you find yourself writing a `Card` aggregate, a `Vote` entity, or any endpoint under `/api/.../cards/`, **stop** — that belongs in a different plan.

## 1. Relevant skills

Read each skill file before applying it — this plan only names them.

- **`create-aggregate`** — for the `Participant` aggregate root. Follow its partial-class split.
- **`create-feature`** — for each endpoint (JoinSprint, GetCurrentParticipant, GetParticipants).
- **`persistence-patterns`** — for `ParticipantConfiguration`, `ParticipantRepository`, `DbSet` registration.
- **`domain-patterns`** — for domain event definitions.
- **`authorization-patterns`** — reference when wiring the `GuestTokenAuthenticationHandler` and `Participant` policy.

**Pattern references (actual codebase):**
- Aggregate split: `Reflekt.Domain/Sprints/Sprint.cs` (properties) + `Reflekt.Domain/Sprints/Behaviors/Sprint.cs` (factory + methods)
- Domain event: `Reflekt.Domain/Sprints/Events/SprintCreatedEvent.cs`
- Endpoint: `Reflekt.API/Features/Sprints/CreateSprint/CreateSprintEndpoint.cs`
- Command + validator: `Reflekt.API/Features/Sprints/CreateSprint/CreateSprintCommand.cs`
- Shared contracts: `Reflekt.API/Features/Sprints/SprintContracts.cs`
- Repository: `Reflekt.Persistence/Repositories/SprintRepository.cs` (extends `Repository<T>`)
- Entity config: `Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs`
- Event handler (SignalR broadcaster): `Reflekt.API/Events/PhaseAdvancedEventHandler.cs`
- Auth handler (TODO target): `Reflekt.API/Auth/GuestTokenAuthenticationHandler.cs`
- Hub (TODO target): `Reflekt.API/Hubs/SprintHub.cs`
- BuildingBlocks base classes: `BuildingBlocks.Domain/Entity.cs`, `BuildingBlocks.Domain/AggregateRoot.cs`, `BuildingBlocks.Persistence/EntityConfigurations/EntityConfiguration.cs`, `BuildingBlocks.Persistence/Repositories/RepositoryBase.cs`
- Integration test: `tests/Reflekt.IntegrationTests/SprintEndpointTests.cs`
- Unit test: `tests/Reflekt.UnitTests/Sprints/SprintTests.cs`

## 2. Judgment calls flagged for the Developer

Small decisions the Developer can make without coming back:

- **Generic ID constraint.** The plan specifies `TId : notnull` as the constraint on `Entity<TId>`. If `IEquatable<TId>` works better for equality, use that instead — just keep the non-generic aliases working.
- **Claim names.** The plan says the auth handler should issue `participantId` and `sprintId` claims. Use custom string claim types (e.g. `"participantId"`, `"sprintId"`) or constants — Developer picks the naming convention.
- **Auth handler injection.** The `GuestTokenAuthenticationHandler` needs the `ParticipantRepository` (or `ReflektDbContext`). Constructor injection should work since the handler resolves within request scope. If it doesn't, use `Context.RequestServices.GetRequiredService<T>()` inside `HandleAuthenticateAsync` — flag the deviation.
- **ParticipantLeft: direct hub push, not domain event.** The architecture §5.1 lists `ParticipantLeft` as a domain event, but disconnect is a transient presence signal — no state changes on the aggregate. Push directly from `SprintHub.OnDisconnectedAsync` to the sprint group. Skip the domain event pipeline. This is the right call: keep domain events for state changes, keep presence signals in the hub.
- **Frontend name picker.** Inline form in the view vs separate component — Developer picks.
- **SignalR connection timing.** The frontend should establish the SignalR connection after receiving the guest token from JoinSprint (or after GetCurrentParticipant confirms the token). Pass the token as `accessTokenFactory` in the SignalR `HubConnectionBuilder`. Developer handles the exact wiring.

Decisions that should come back if they feel wrong mid-implementation:

- If the generic ID refactor breaks something unexpected in existing Team/Sprint code — flag it immediately.
- If the `GuestTokenAuthenticationHandler` can't resolve scoped services (ParticipantRepository) due to middleware lifetime issues — flag it.

## 3. Domain model changes

### 3.0 Generic ID support in BuildingBlocks (prerequisite)

This step makes the base classes generic on `TId` so aggregates can choose `int`, `Guid`, or another key type. Existing aggregates (Team, Sprint) are unaffected — they continue using the non-generic aliases which default to `int`.

**`BuildingBlocks.Domain` changes:**

1. **Add `IHasDomainEvents` interface** — new file `src/BuildingBlocks/BuildingBlocks.Domain/IHasDomainEvents.cs`:
   - Exposes `IReadOnlyList<IDomainEvent> DomainEvents { get; }`
   - Exposes `void ClearDomainEvents()`
   - This interface is what the interceptor filters on.

2. **Make `Entity` generic** — modify `src/BuildingBlocks/BuildingBlocks.Domain/Entity.cs`:
   - Rename current `Entity` to `Entity<TId>` where `TId : notnull`.
   - `Id` property becomes type `TId` (was `int`).
   - Equality: compare `Id` using `EqualityComparer<TId>.Default` instead of `==`.
   - Add non-generic alias: `abstract class Entity : Entity<int> { }` — keeps all existing code compiling.

3. **Make `AggregateRoot` generic** — modify `src/BuildingBlocks/BuildingBlocks.Domain/AggregateRoot.cs`:
   - Rename current `AggregateRoot` to `AggregateRoot<TId>` where `TId : notnull`.
   - Inherits from `Entity<TId>`, implements `IHasDomainEvents`.
   - Domain events collection stays here (unchanged logic).
   - Add non-generic alias: `abstract class AggregateRoot : AggregateRoot<int> { }`.

**`BuildingBlocks.Persistence` changes:**

4. **Make `EntityConfiguration` generic on TId** — modify `src/BuildingBlocks/BuildingBlocks.Persistence/EntityConfigurations/EntityConfiguration.cs`:
   - Rename to `EntityConfiguration<T, TId>` where `T : Entity<TId>`, `TId : notnull`.
   - `Configure` method: `builder.HasKey(e => e.Id)` + conditional `ValueGeneratedOnAdd` (unchanged logic, now works with any `TId`).
   - Add non-generic alias: `abstract class EntityConfiguration<T> : EntityConfiguration<T, int> where T : Entity<int>` (or just `Entity`).

5. **Make `RepositoryBase` generic on TId** — modify `src/BuildingBlocks/BuildingBlocks.Persistence/Repositories/RepositoryBase.cs`:
   - Rename to `RepositoryBase<TContext, TEntity, TId>` where `TEntity : Entity<TId>`, `TId : notnull`.
   - `GetByIdAsync` signature becomes `GetByIdAsync(TId id, CancellationToken ct)`.
   - Add non-generic alias: `abstract class RepositoryBase<TContext, TEntity> : RepositoryBase<TContext, TEntity, int>` with matching constraints.

6. **Update `DomainEventPublishingInterceptor`** — modify `src/BuildingBlocks/BuildingBlocks.Persistence/DomainEventPublishingInterceptor.cs`:
   - Change `Entries<AggregateRoot>()` to `Entries().Select(e => e.Entity).OfType<IHasDomainEvents>()`.
   - This makes the interceptor work with `AggregateRoot<int>`, `AggregateRoot<Guid>`, or any future key type.

**`Reflekt.Persistence` changes:**

7. **Update `Repository<TEntity>`** — modify `src/Services/Reflekt/Reflekt.Persistence/Repositories/Repository.cs`:
   - Keep existing `Repository<TEntity>` (constrains to `Entity` = `Entity<int>`).
   - Add `Repository<TEntity, TId>` that extends `RepositoryBase<ReflektDbContext, TEntity, TId>` — available for future Guid-keyed entities.

**Verification:** `dotnet build` passes, all existing Sprint and Team tests pass unchanged.

### 3.1 Participant aggregate — partial class split

**Properties file:** `src/Services/Reflekt/Reflekt.Domain/Participants/Participant.cs`

- Extends `AggregateRoot` (non-generic — int ID, consistent with Team and Sprint).
- Properties:
  - `int SprintId` — FK to Sprint (by ID, no navigation — cross-aggregate reference).
  - `string DisplayName` — participant's chosen name.
  - `string GuestToken` — server-generated GUID string, unique.
  - `DateTime JoinedAt` — UTC, stamped at creation.
- Private parameterless constructor for EF Core.

**Behaviors file:** `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs`

One method:

**`Join(string displayName, int sprintId)`** — static factory:
- Guard: `displayName` must not be null/whitespace.
- Sets `SprintId`, `DisplayName`, `JoinedAt = DateTime.UtcNow`.
- Generates `GuestToken = Guid.NewGuid().ToString()`.
- Raises `ParticipantJoinedEvent(this)`.

No `Leave()` method — disconnect is a hub-level presence signal, not a domain state change (see §2 judgment call).

Skill: `create-aggregate`

### 3.2 Domain events

One event in `src/Services/Reflekt/Reflekt.Domain/Participants/Events/`:

```
ParticipantJoinedEvent(Participant Participant) : IDomainEvent
```

Follows CLAUDE.md convention: carries the aggregate reference. At dispatch time (post-`SaveChangesAsync`), all Participant state including `Id` is populated.

No `ParticipantLeftEvent` in domain — the hub pushes `ParticipantLeft` directly on disconnect (§2).

Skill: `domain-patterns`

## 4. Data model changes

### 4.1 Table: `Participants`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `INTEGER` | PK, auto-increment (from `Entity` base) |
| `SprintId` | `INTEGER` | Required, FK to `Sprints.Id` |
| `DisplayName` | `TEXT` | Required, max 50 |
| `GuestToken` | `TEXT` | Required, unique |
| `JoinedAt` | `TEXT` (SQLite datetime) | Required |

FK: `SprintId → Sprints.Id`, cascade delete (spec §4 note — all FK cascade on Sprint removal).
Unique index on `GuestToken` (used by auth handler lookup on every request).
Index on `SprintId` (used by GetParticipants list query).

### 4.2 EF Configuration — `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/ParticipantConfiguration.cs`

`EntityConfiguration<Participant>` (extends base from BuildingBlocks):
- `ToTable("Participants")`
- `Property(p => p.SprintId).IsRequired()`
- `HasOne<Sprint>().WithMany().HasForeignKey(p => p.SprintId).OnDelete(DeleteBehavior.Cascade)`
- `Property(p => p.DisplayName).IsRequired().HasMaxLength(50)`
- `Property(p => p.GuestToken).IsRequired()`
- `HasIndex(p => p.GuestToken).IsUnique()`
- `HasIndex(p => p.SprintId)`
- `Property(p => p.JoinedAt).IsRequired()`

Skill: `persistence-patterns`

## 5. Implementation steps

### Step 1: Generic ID support in BuildingBlocks

**What:** Make Entity, AggregateRoot, EntityConfiguration, and RepositoryBase generic on `TId`. Add `IHasDomainEvents`. Keep non-generic aliases for backward compatibility.
**Files to modify:**
- `src/BuildingBlocks/BuildingBlocks.Domain/Entity.cs`
- `src/BuildingBlocks/BuildingBlocks.Domain/AggregateRoot.cs`
- `src/BuildingBlocks/BuildingBlocks.Persistence/EntityConfigurations/EntityConfiguration.cs`
- `src/BuildingBlocks/BuildingBlocks.Persistence/Repositories/RepositoryBase.cs`
- `src/BuildingBlocks/BuildingBlocks.Persistence/DomainEventPublishingInterceptor.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/Repository.cs`
**Files to create:**
- `src/BuildingBlocks/BuildingBlocks.Domain/IHasDomainEvents.cs`
**Details:** See §3.0. The key changes: `Entity<TId>`, `AggregateRoot<TId>` implementing `IHasDomainEvents`, interceptor using `OfType<IHasDomainEvents>()`, non-generic aliases preserving existing code.
**Verification:** `dotnet build Reflekt.slnx` passes. All existing Team and Sprint tests pass unchanged. Team and Sprint aggregates are NOT modified — they use the non-generic aliases.
**Dependencies:** None.

### Step 2: Participant aggregate + domain events

**What:** Create the Participant domain model.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Domain/Participants/Participant.cs`
- `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs`
- `src/Services/Reflekt/Reflekt.Domain/Participants/Events/ParticipantJoinedEvent.cs`
**Pattern:** Follow `create-aggregate` skill. Reference `Reflekt.Domain/Sprints/` for the partial-class split and event shape.
**Feature-specific inputs:**
- Factory method name: `Join` (not `Create` — domain language for participants)
- Parameters: `string displayName, int sprintId`
- Guards: displayName not null/whitespace
- Generated field: `GuestToken = Guid.NewGuid().ToString()`
- Timestamp: `JoinedAt = DateTime.UtcNow`
- Event raised: `ParticipantJoinedEvent(this)`
**Dependencies:** Step 1.

### Step 3: EF config + DbSet + repository + DI

**What:** Add persistence for the Participant aggregate.
**Files to create:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/ParticipantConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs`
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `public DbSet<Participant> Participants => Set<Participant>();`
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `ParticipantRepository` as scoped.

**ParticipantRepository** extends `Repository<Participant>`:
- Inherits `AddAsync`, `SaveChangesAsync`, `GetByIdAsync` from base.
- Custom methods:
  - `GetByGuestTokenAsync(string guestToken, CancellationToken ct)` — returns single `Participant?`. Used by auth handler on every authenticated request — must query by the unique-indexed `GuestToken` column.
  - `GetBySprintIdAsync(int sprintId, CancellationToken ct)` — returns `List<Participant>` ordered by `JoinedAt` ascending. Used by GetParticipants endpoint.

**Pattern:** Follow `persistence-patterns` skill. Reference `SprintRepository.cs` and `SprintConfiguration.cs`.
**Dependencies:** Step 2.

### Step 4: EF Migration

**What:** Generate the Participant migration.
**Command:**
```bash
dotnet ef migrations add AddParticipant -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```
**Important:** The generic ID refactor in Step 1 may cause EF to detect changes in existing entities (even though the runtime schema is identical). If the generated migration includes changes to the `Teams` or `Sprints` tables, verify those changes are cosmetic (e.g., annotation differences). If they alter columns or constraints, flag it — the refactor should NOT change existing table schemas.
**Verify:** `dotnet ef migrations list` shows the new migration. `dotnet run` auto-applies in Development.
**Dependencies:** Step 3.

### Step 5: Wire GuestTokenAuthenticationHandler

**What:** Complete the TODO in `src/Services/Reflekt/Reflekt.API/Auth/GuestTokenAuthenticationHandler.cs`. Replace the stub with a real participant lookup that issues claims.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Auth/GuestTokenAuthenticationHandler.cs`

**Handler behavior:**
1. Read token from `X-Guest-Token` header or `access_token` query parameter (already implemented).
2. If no token present → `AuthenticateResult.NoResult()` (already implemented — keep this).
3. Look up participant via `ParticipantRepository.GetByGuestTokenAsync(token)`.
4. If no match → `AuthenticateResult.Fail("Invalid guest token")`.
5. If match → create `ClaimsIdentity` with:
   - `participantId` claim = participant's `Id`
   - `sprintId` claim = participant's `SprintId`
   - `displayName` claim = participant's `DisplayName`
   - Authentication type = `GuestTokenDefaults.AuthenticationScheme`
6. Return `AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name))`.

**Injection:** Add `ParticipantRepository` as a constructor parameter (or resolve from `Context.RequestServices` if constructor injection doesn't work with the auth handler lifecycle — see §2 judgment call).

**Pattern:** Reference `authorization-patterns` skill for auth handler conventions.
**Dependencies:** Step 3 (ParticipantRepository must be registered).

### Step 6: Shared contracts + JoinSprint endpoint

**What:** Create shared Participant contracts and the JoinSprint endpoint.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Participants/ParticipantContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/JoinSprintCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/JoinSprintEndpoint.cs`

**ParticipantContracts.cs** — shared records:
- `JoinSprintResponse(int Id, int SprintId, string DisplayName, string GuestToken, DateTime JoinedAt)` — includes token, only used by JoinSprint.
- `ParticipantResponse(int Id, int SprintId, string DisplayName, DateTime JoinedAt)` — no token, used by GetCurrentParticipant and list items.
- Static mapping helpers `ToJoinResponse(Participant)` and `ToResponse(Participant)`.

**JoinSprintCommand.cs:**
- Request record: `JoinSprintCommand { int SprintId, string DisplayName }` — `SprintId` from route, `DisplayName` from body.
- Validator: `DisplayName` required (not empty/whitespace), max 50 chars.

**JoinSprintEndpoint.cs:**
- **No auth required** — `[AllowAnonymous]`, `[HttpPost("/api/sprints/{sprintId}/join")]`, `[Tags("Participants")]`
- `Endpoint<JoinSprintCommand, JoinSprintResponse>`
- Injects: `SprintRepository`, `ParticipantRepository`

**Handler flow:**
1. Load sprint: `sprintRepository.GetByIdAsync(command.SprintId, ct)` → 404 if null.
2. Check `sprint.State == SprintState.Active` → 409 with message if not (Draft or Closed).
3. Create aggregate: `Participant.Join(command.DisplayName, command.SprintId)`.
4. Persist: `participantRepository.AddAsync` + `participantRepository.SaveChangesAsync`.
5. Respond: 201 with `JoinSprintResponse` (includes `GuestToken`).

**Pattern:** Follow `create-feature` skill. Reference `CreateSprintEndpoint.cs` for handler structure.
**Dependencies:** Steps 2–4.

### Step 7: GetCurrentParticipant + GetParticipants endpoints

**What:** Two read endpoints.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Features/Participants/GetCurrentParticipant/GetCurrentParticipantEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/GetParticipants/GetParticipantsEndpoint.cs`

**GetCurrentParticipantEndpoint — `GET /api/sprints/{sprintId}/participants/me`:**
- Auth: `Participant` policy (requires valid guest token).
- Request record with `int SprintId` (from route).
- Handler:
  1. Read `participantId` from claims (set by GuestTokenAuthenticationHandler in Step 5).
  2. Read `sprintId` from claims.
  3. Verify claim's `sprintId` matches route's `SprintId` → 401 if mismatch (prevents cross-sprint token use).
  4. Load participant: `participantRepository.GetByIdAsync(participantId, ct)` → 404 if null (shouldn't happen with valid claims, but defensive).
  5. Respond: 200 with `ParticipantResponse` (no token in response — client already has it).

**GetParticipantsEndpoint — `GET /api/sprints/{sprintId}/participants`:**
- Auth: `OwnerOrScrumMaster` policy (accepts either guest token or SM cookie).
- Request record with `int SprintId` (from route).
- Handler:
  1. Load sprint: `sprintRepository.GetByIdAsync(request.SprintId, ct)` → **404 if null** (spec requires 404 for nonexistent sprint, not empty list).
  2. If caller is authenticated via guest token: verify claim's `sprintId` matches route `SprintId` → 401 if mismatch.
  3. If caller is authenticated via cookie: verify caller is the SM of the sprint's team. Load team via `sprint.TeamId` → check `team.CreatorEmail` (case-insensitive, same `StringComparison.OrdinalIgnoreCase` pattern as Sprint endpoints).
  4. Load participants: `participantRepository.GetBySprintIdAsync(request.SprintId, ct)`.
  5. Respond: 200 with `List<ParticipantResponse>`.

**Pattern:** Follow `create-feature` skill. Reference `GetSprintEndpoint.cs` for read endpoint structure.
**Dependencies:** Steps 3, 5.

### Step 8: SprintHub participant validation + disconnect handling

**What:** Wire up the two TODOs in `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`

**JoinSprint validation** (existing method, currently adds to group unconditionally):
1. Read `sprintId` from claims (set by GuestTokenAuthenticationHandler).
2. Verify claim's `sprintId` matches the requested `sprintId` parameter.
3. If mismatch → throw `HubException` or silently return without adding to group.
4. If match → add to group `sprint:{sprintId}` (existing behavior).

**OnDisconnectedAsync** (new override):
1. Read `participantId` from claims.
2. Read `sprintId` from claims.
3. If claims are present (authenticated connection): push `ParticipantLeft` event to group `sprint:{sprintId}` with payload `{ participantId }`.
4. Call `base.OnDisconnectedAsync(exception)`.

**Note:** This is a direct hub push, not a domain event. See §2 judgment call for rationale.

**Dependencies:** Step 5 (auth handler must be wired for claims to be available).

### Step 9: ParticipantJoined event handler (SignalR broadcaster)

**What:** Create the SignalR broadcaster for `ParticipantJoinedEvent`.
**Files to create:**
- `src/Services/Reflekt/Reflekt.API/Events/ParticipantJoinedEventHandler.cs`

**Handler:**
- Implements `IEventHandler<ParticipantJoinedEvent>`.
- Injects `IHubContext<SprintHub>`.
- `HandleAsync`: sends `"ParticipantJoined"` to SignalR group `sprint:{ev.Participant.SprintId}` with payload `{ id: ev.Participant.Id, displayName: ev.Participant.DisplayName }`.

**Pattern:** Follow `PhaseAdvancedEventHandler.cs` exactly — same structure, different event and payload.
**Dependencies:** Steps 2, 8.

### Step 10: Seed data update

**What:** Add seeded participants to the existing seeded sprint.
**Files to modify:**
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs`

**Seed logic (after sprint seeding):**
1. Load the seeded sprint (or use the one just created).
2. If `!db.Participants.Any(p => p.SprintId == sprint.Id)`: create 2–3 participants using `Participant.Join(...)`, clear domain events (same pattern as team/sprint seeding), add, save.
3. Suggested names: "Ana", "Mihai", "Elena" (Romanian team context).
4. Idempotent.

**Dependencies:** Steps 2–4.

### Step 11: Frontend — guest store + participant store + SignalR connection

**What:** Create the Pinia stores for guest token management, participant API calls, and SignalR connection.
**Files to create:**
- `web/reflekt-web/src/stores/guest.ts`
- `web/reflekt-web/src/stores/participant.ts`
- `web/reflekt-web/src/composables/useSprintHub.ts`

**guest.ts — localStorage token manager:**
```
state: {
  token: string | null
  displayName: string | null
}

actions: {
  loadToken(sprintId: number)     // reads from localStorage key `reflekt:sprint:{sprintId}:token`
  saveToken(sprintId: number, token: string)  // writes to localStorage
  clearToken(sprintId: number)    // removes from localStorage
}
```

**participant.ts — API calls:**
```
state: {
  currentParticipant: Participant | null
  participants: Participant[]
}

actions: {
  join(sprintId: number, displayName: string)       // POST /api/sprints/{sprintId}/join
  fetchMe(sprintId: number)                          // GET  /api/sprints/{sprintId}/participants/me
  fetchParticipants(sprintId: number)                // GET  /api/sprints/{sprintId}/participants
}
```

API calls include `X-Guest-Token` header from the guest store (except `join`, which is public).

**useSprintHub.ts — SignalR composable:**
- Creates `HubConnection` to `/hubs/sprint` with `accessTokenFactory` returning the guest token.
- Exposes `connect(sprintId)`, `disconnect()`.
- On connect: calls `JoinSprint(sprintId)` on the hub.
- Listens for: `ParticipantJoined`, `ParticipantLeft`, `PhaseChanged`.
- On `ParticipantJoined`: add to participant store list.
- On `ParticipantLeft`: optionally mark in store (no UI indicator in v1, but store can track it).
- Install `@microsoft/signalr` package.

**Pattern:** Reference `stores/sprint.ts` for API call patterns. Reference `stores/teams.ts` for store shape.
**Dependencies:** Steps 5–7 (API endpoints).

### Step 12: Frontend — join flow UI

**What:** Add the participant room link route and join flow.
**Files to create:**
- `web/reflekt-web/src/views/RetroRoomView.vue`
**Files to modify:**
- `web/reflekt-web/src/router/index.ts` — add room link route.
- `web/reflekt-web/src/views/SprintView.vue` — add participant list section (optional, for SM view).

**Router update:**
```ts
{
  path: '/sprint/:sprintId',
  name: 'retro-room',
  component: RetroRoomView,
}
```

**RetroRoomView.vue — join flow:**
1. On mount: extract `sprintId` from route params.
2. `guestStore.loadToken(sprintId)` — check localStorage.
3. If token exists: call `participantStore.fetchMe(sprintId)`.
   - On 200: set `currentParticipant`, connect SignalR, show retro room shell (participant list + "retro features coming soon" placeholder).
   - On 401: clear stale token via `guestStore.clearToken(sprintId)`, fall through to step 4.
4. If no token (or cleared): show name picker form.
   - Text input for display name, "Join" button.
   - On submit: call `participantStore.join(sprintId, displayName)`.
   - On 201: save token via `guestStore.saveToken(sprintId, response.guestToken)`, connect SignalR, show retro room shell.
   - On 409: show "Sprint is not accepting participants" message (Draft or Closed).
   - On 404: show "Sprint not found" message.
5. Retro room shell: show sprint info (name, phase), participant list (from `participantStore.participants`), and placeholder for cards/votes.

**SprintView.vue (optional enhancement):**
- If time allows, add a small participant list section showing who's joined. Uses `participantStore.fetchParticipants` with SM cookie auth. This is a nice-to-have — skip if it adds scope.

**Styling:** Tailwind. Match existing aesthetic. Simple card layout, form inputs, participant chips/badges.

**Dependencies:** Step 11.

### Step 13: Unit tests

**What:** Test the Participant aggregate domain logic and the generic ID base classes.
**Files to create:**
- `tests/Reflekt.UnitTests/Participants/ParticipantTests.cs`
- `tests/Reflekt.UnitTests/BuildingBlocks/EntityTests.cs` (optional — only if generic ID changes warrant it)

**ParticipantTests.cs — test cases:**
1. `Join_SetsAllPropertiesCorrectly` — verify SprintId, DisplayName, GuestToken (non-null, GUID format), JoinedAt set.
2. `Join_GeneratesUniqueGuestTokens` — create two participants, tokens differ.
3. `Join_RaisesParticipantJoinedEvent` — verify DomainEvents contains one ParticipantJoinedEvent with the participant reference.
4. `Join_WithEmptyDisplayName_Throws` — ArgumentException.
5. `Join_WithWhitespaceDisplayName_Throws` — ArgumentException.

**EntityTests.cs (optional):**
- Verify `Entity<int>` and `Entity<Guid>` equality semantics work correctly.
- Verify non-generic `Entity` alias behaves identically to `Entity<int>`.

**Pattern:** Reference `tests/Reflekt.UnitTests/Sprints/SprintTests.cs`.
**Dependencies:** Steps 1–2.

### Step 14: Integration tests

**What:** Test Participant endpoints end-to-end.
**Files to create:**
- `tests/Reflekt.IntegrationTests/ParticipantEndpointTests.cs`

**Test setup:**
- Same `WebApplicationFactory<Program>` pattern as `SprintEndpointTests.cs`.
- Each test creates a team and an Active sprint first (via API calls, reusing existing patterns).
- For guest-token-authenticated requests: call JoinSprint first, capture the `guestToken` from the response, include it as `X-Guest-Token` header on subsequent requests.

**Test cases:**
1. `JoinSprint_ActiveSprint_Returns201` — join with valid displayName → 201, response has all fields including guestToken.
2. `JoinSprint_EmptyDisplayName_Returns400`.
3. `JoinSprint_DisplayNameTooLong_Returns400` — 51+ chars.
4. `JoinSprint_DraftSprint_Returns409` — create sprint (Draft), try to join → 409.
5. `JoinSprint_ClosedSprint_Returns409` — create, start, close sprint, try to join → 409.
6. `JoinSprint_NonexistentSprint_Returns404`.
7. `GetCurrentParticipant_ValidToken_Returns200` — join, then GET /me with token → 200.
8. `GetCurrentParticipant_NoToken_Returns401`.
9. `GetCurrentParticipant_InvalidToken_Returns401` — send a random GUID as token.
10. `GetCurrentParticipant_WrongSprint_Returns401` — join sprint A, call /me on sprint B with sprint A's token.
11. `GetParticipants_WithGuestToken_ReturnsList` — join 2 participants, GET list → 2 items.
12. `GetParticipants_WithSmCookie_ReturnsList` — SM calls list with cookie auth.
13. `GetParticipants_NoAuth_Returns401`.
14. `GuestTokenAuth_SetsCorrectClaims` — join, then call /me, verify response matches the join response (proves claims pipeline works).

**Pattern:** Reference `SprintEndpointTests.cs`.
**Dependencies:** Steps 5–7 and all prior steps.

## 6. Migration notes

Third EF Core migration. Command:
```bash
dotnet ef migrations add AddParticipant -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API
```

**Watch for:** The generic ID refactor may cause EF to detect phantom changes in existing tables. If the migration includes anything beyond the new `Participants` table, review carefully — existing schemas should be untouched.

Auto-applied on startup in Development. Delete `data/reflekt.db` before first run if migration conflicts arise.

## 7. Testing strategy

- **Unit tests** (Step 13) cover the factory method, guards, token generation uniqueness, and domain event raising. Optionally test generic ID base class equality.
- **Integration tests** (Step 14) cover the full HTTP pipeline: join flow, guest token auth, claims propagation, sprint-state gating, and cross-sprint token rejection.
- **SignalR is not integration-tested** — `ParticipantJoinedEventHandler` pushes to a group; verifying it doesn't throw is sufficient. Full SignalR round-trip testing (connect → join group → receive event) is complex and deferred. The frontend manual test covers this.
- **Auth handler** is tested indirectly through integration tests (GetCurrentParticipant with valid/invalid tokens).
- **No frontend tests** — not in scope for v1.

## 8. Verification — acceptance criteria

Participants is done when all of these pass:

1. `dotnet build Reflekt.slnx` — zero errors, zero warnings.
2. `dotnet test` — all tests pass (existing + new unit + new integration).
3. `dotnet run` in Development:
   - `data/reflekt.db` has `Participants` table with unique index on `GuestToken`.
   - Seed data creates team, sprint, and 2–3 participants.
4. Manual API flow (Scalar or curl):
   - Start a sprint (create + start via SM endpoints).
   - `POST /api/sprints/{sprintId}/join` with `{ "displayName": "Ana" }` → 201, response includes `guestToken`.
   - `GET /api/sprints/{sprintId}/participants/me` with `X-Guest-Token: {token}` → 200, matches join response.
   - `GET /api/sprints/{sprintId}/participants` with guest token → list with Ana.
   - `POST /api/sprints/{sprintId}/join` with `{ "displayName": "Mihai" }` → 201, different token.
   - `GET /api/sprints/{sprintId}/participants` → list with 2 participants.
   - Join on Draft sprint → 409.
   - Join on Closed sprint → 409.
   - `/me` with invalid token → 401.
   - `/me` with token from different sprint → 401.
5. Frontend flow (`npm run dev`):
   - Navigate to `/sprint/{sprintId}` (Active sprint).
   - See name picker. Enter name, click Join.
   - See retro room shell with participant list.
   - Refresh page → silent rejoin (no name picker).
   - Open second browser tab → join as second participant → first tab sees new participant appear (SignalR).
6. Existing Team and Sprint endpoints still work — generic ID refactor caused no regressions.
7. `GuestTokenAuthenticationHandler` returns valid claims — all guest-token-protected endpoints work.

## 9. Out of scope

If tempted to add any of these, stop and check the relevant feature plan:

- `Card` aggregate or card CRUD — **Cards plan**.
- Vote budget enforcement — **Votes plan**.
- Evidence link management — **EvidenceLinks plan**.
- Report generation — **Reports plan**.
- Retro room UI (card columns, voting buttons, discussion mode) — **Cards + Votes plans**.
- Participant deletion or kick — **not in v1**.
- Display name editing after join — **not in v1**.
- Online/offline presence indicators — **not in v1**.
- Cross-sprint identity linking — **not in v1**.

## 10. File summary

**New files:**
- `src/BuildingBlocks/BuildingBlocks.Domain/IHasDomainEvents.cs`
- `src/Services/Reflekt/Reflekt.Domain/Participants/Participant.cs`
- `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs`
- `src/Services/Reflekt/Reflekt.Domain/Participants/Events/ParticipantJoinedEvent.cs`
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/ParticipantConfiguration.cs`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/ParticipantContracts.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/JoinSprintCommand.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/JoinSprintEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/GetCurrentParticipant/GetCurrentParticipantEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Participants/GetParticipants/GetParticipantsEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Events/ParticipantJoinedEventHandler.cs`
- `web/reflekt-web/src/stores/guest.ts`
- `web/reflekt-web/src/stores/participant.ts`
- `web/reflekt-web/src/composables/useSprintHub.ts`
- `web/reflekt-web/src/views/RetroRoomView.vue`
- `tests/Reflekt.UnitTests/Participants/ParticipantTests.cs`
- `tests/Reflekt.IntegrationTests/ParticipantEndpointTests.cs`

**Modified files:**
- `src/BuildingBlocks/BuildingBlocks.Domain/Entity.cs` — generic `Entity<TId>` + non-generic alias
- `src/BuildingBlocks/BuildingBlocks.Domain/AggregateRoot.cs` — generic `AggregateRoot<TId>` + `IHasDomainEvents` + non-generic alias
- `src/BuildingBlocks/BuildingBlocks.Persistence/EntityConfigurations/EntityConfiguration.cs` — generic on TId + alias
- `src/BuildingBlocks/BuildingBlocks.Persistence/Repositories/RepositoryBase.cs` — generic on TId + alias
- `src/BuildingBlocks/BuildingBlocks.Persistence/DomainEventPublishingInterceptor.cs` — `OfType<IHasDomainEvents>()`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/Repository.cs` — add `Repository<TEntity, TId>` overload
- `src/Services/Reflekt/Reflekt.Persistence/ReflektDbContext.cs` — add `DbSet<Participant>`
- `src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs` — register `ParticipantRepository`
- `src/Services/Reflekt/Reflekt.Persistence/DbInitializer.cs` — seed participants
- `src/Services/Reflekt/Reflekt.API/Auth/GuestTokenAuthenticationHandler.cs` — wire participant lookup + claims
- `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs` — validate group join + push ParticipantLeft on disconnect
- `web/reflekt-web/src/router/index.ts` — add `/sprint/:sprintId` route
- `web/reflekt-web/src/views/SprintView.vue` — optional participant list section

**Unchanged:**
- `Program.cs`, all Auth config files except `GuestTokenAuthenticationHandler`, all Team files, all Sprint domain/endpoint files, `Policies.cs`, `appsettings*.json`.
