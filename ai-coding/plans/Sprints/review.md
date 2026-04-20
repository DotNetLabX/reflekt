# Sprints — Review

## Step 1: Done check — PASSED

All 12 plan steps have corresponding done.md entries. 2 deviations reported, both reasonable. No scope creep.

## Step 2: Code review — 1 bug found (FIXED)

### Bug: Frontend POST actions missing Content-Type/body

**File:** `web/reflekt-web/src/stores/sprint.ts`
**Affects:** `startSprint`, `advancePhase`, `closeSprint` actions.

These actions send POST requests without `Content-Type` or body. FastEndpoints returns 415 for POST requests without Content-Type, even when the request model binds only from route params (the integration tests discovered this and worked around it with `PostAsJsonAsync(..., new { })`).

**Fix:** Add `headers: { 'Content-Type': 'application/json' }, body: '{}'` to the three affected actions, matching the pattern already used by `createSprint`.

### Everything else: clean

- Domain model matches plan exactly (enums, aggregate split, 4 events, all guards/transitions).
- Persistence matches plan (configuration, repository, DbSet, DI, migration).
- All 6 endpoints follow plan instructions (routes, auth, handler flows, status codes).
- PhaseAdvancedEventHandler correctly pushes "PhaseChanged" to SignalR group.
- SprintHub pre-existed (deviation noted in done.md) — acceptable.
- Seed data is idempotent, clears domain events.
- 18 unit tests cover all aggregate behavior.
- 16 integration tests cover all endpoint scenarios.
- WelcomeView, SprintView, and router match plan.
