# F36-FrontendDefects — Change notes

Shared files are never edited by the unit; the campaign applies these notes at merge.

## Note 1 — `docs/backlog.md`, row 36: status

**Target:** `docs/backlog.md`, entry `### 36. Frontend Visual & UX Defects`.
**Change:** replace the status line `**Status: Spec Ready** | **Spec:** …` with:

```
**Status: Built — AC6 operator walkthrough pending** | **Spec:** `docs/specs/F36-FrontendDefects/definition/spec.md` | **Summary:** `docs/specs/F36-FrontendDefects/delivery/summary.md`
```

and append to the entry's description: `One pre-existing hub-lifecycle race moved to #{N} (Hub connection lifecycle never waits on a stop).`

## Note 2 — `docs/backlog.md`: new row for the deferred HIGH

**Target:** `docs/backlog.md`, in the same section, after the last fix-feature row (#40).
**Number:** the next free feature number at merge (F41 when this note was written; another unit's note may take it — use the next free one and update Note 1's `#{N}` to match).
**Row text:**

```
### {N}. Hub Connection Lifecycle Never Waits on a Stop
**Status: Not Started** | **Spec:** — (definition: `docs/specs/F36-FrontendDefects/delivery/questions.md` Q4)
**Epic:** Publish Readiness | **Priority:** Should
**Dependencies:** #36
Pre-existing (present on `main`): leaving the retro room while the hub WebSocket is still opening blocks the next room's entry until the browser fails the socket — SignalR 10.0.0's `stop()` waits for transport start, and `useSprintHub.ts` keeps the old connection until its stop returns. Fix: Q4 steps 1–5 (`useSprintHub.ts` `connect()`/`disconnect()` never await a previous connection's stop; `useRetroRoom.ts` ownership by object identity, no lock) — amends F36's pin on `useSprintHub.ts`. Acceptance: Codex's reproductions — (a) held handshake, (a) held WebSocket opening, (b) held `JoinSprint`, (c) concurrent teardown waiters, delayed summary, stale join — all pass; walkthrough: leave during "Joining retro…" with the socket throttled, reopen → exactly one live `/hubs/sprint` connection and the room leaves loading. Source: F36 review cycle 4 (`docs/specs/F36-FrontendDefects/delivery/review.md` § Step 2 — Re-review (cycle 4)).
```
