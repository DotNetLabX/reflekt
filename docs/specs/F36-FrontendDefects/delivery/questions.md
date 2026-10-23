# F36-FrontendDefects — Questions

## Q1 — Fix cycle 2, row 2: may the fix change `useSprintHub.ts` beyond the pinned eight arguments?

**From:** team-lead · **To:** architect · **Status:** Answered · **Raised:** 2026-10-23

**Context:** Code re-review cycle 1 (Codex, `review.md` § Step 2 — Re-review (cycle 1)) found a HIGH race: `useSprintHub.connect()` awaits the previous connection's teardown, then builds a new connection without checking whether its room unmounted; closing the room during teardown leaves the new hub running (`useRetroRoom.ts:145`). The dev-fix-c1 developer flagged the same edge in `implementation.md` § Fix Round 1 and noted that a stale attempt cannot simply call the shared `disconnect()`, because it could cut off the next room's connection. Codex's suggested fix makes `connect()` cancellation-aware and lets the attempt stop the specific connection it built. The plan's Must-NOT-Change row "SignalR handling" pins the `useSprintHub.ts` diff to the arguments of the eight `fetchCards`/`fetchGroups` calls.

**Question:** How should fix cycle 2 row 2 be fixed?

**Options:**
1. Amend the pin: allow a minimal, cancellation-aware change to `connect()` in `useSprintHub.ts` (e.g. return or accept an ownership token and stop the connection it built when stale), keeping the event handlers and the eight background calls unchanged; update plan Must-NOT-Change accordingly.
2. Keep the pin: fix it inside `useRetroRoom.ts` only (state how — e.g. after `connect()` resolves for a stale attempt, stop only if the shared connection is still the one this attempt started).
3. Record it as out of scope (a backlog change note) and close row 2 without a fix.

**Recommendation (team lead):** option 1 — Confidence: medium — it is the reviewer's fix and the cleanest ownership model, but it widens a pinned surface; option 2 is acceptable if it can identify the connection without touching the composable.

### Answer
**Ruling: option 2 — keep the pin; fix it in `useRetroRoom.ts` only.** By: architect, 2026-10-23. Confidence: high. I read `useSprintHub.ts` `connect`/`disconnect` and `useRetroRoom.ts`. Both are module-level, single-threaded JS, so ownership can be decided where `connect()` resolves.

**Why it is safe without touching `connect()`.** The shared `connection` can only be replaced by a later `connect()`, and that call first `await`s `disconnect()` of the current one. Stopping a connection rejects its in-flight `start()`/`invoke()`. So:
- If an attempt's `await connect(sprintId)` **resolves**, then at that continuation the shared connection is the one this attempt built. Had a later room replaced it, this attempt's `start`/`invoke` would have rejected instead.
- If it **rejects** because the room was left, the connection it built has already been stopped by whoever left. That is the room's `onUnmounted` `disconnect()`, or the next room's `connect()`.

**The fix (row 2):**
1. In `enterData` (today `useRetroRoom.ts:144-151`), replace the bare `if (superseded(entry)) return` that follows the `try { await connect(sprintId) } catch {…}` with one that, when the entry is superseded, calls `disconnect()` as the **first statement** of the continuation, with no `await` before it and no other code between the resolve and the call. Then it returns without touching `joined`/`loading`. `disconnect()` reads `connection` synchronously before its own `await`, so it stops exactly this attempt's connection and can never reach a connection a later room builds afterwards.
2. The `catch` branch stays as it is: return when superseded, otherwise `failStage`.
3. No change to `useSprintHub.ts`. The Must-NOT-Change "SignalR handling" pin (only the eight `fetchCards`/`fetchGroups` arguments) stands, and the event handlers stay untouched.

**Accept:**
- `git diff -- web/reflekt-web/src/composables/useSprintHub.ts` still touches only the eight call arguments.
- In `useRetroRoom.ts`, the superseded branch after a resolved `connect` calls `disconnect()` before any `await`.
- Walkthrough (Step 13/14): leave the room while it is still joining (navigate away during "Joining retro…"). DevTools then shows no `/hubs/sprint` connection left open on the page you went to. Reopening the room gives exactly one connection.

**Turned down:**
- Option 1 widens a pinned surface for no extra safety: the ownership check needs no token, because a later `connect()` always stops the previous connection first.
- Option 3 would leave a live hub mutating stores after the room is gone. That is the HIGH itself.

No plan change: Step 7's "the hub connects only after the loads succeed, so a retry never opens a second connection" already covers this, and the pin row is unchanged.


## Q2 — Fix cycle 3: Q1's ruling regresses with concurrent teardown waiters — what is the fix for row 2?

**From:** team-lead · **To:** architect · **Status:** Answered · **Raised:** 2026-10-23

**Context:** `review.md` § Step 2 — Re-review (cycle 2): the Q1 option-2 fix is implemented (`useRetroRoom.ts:155`) but Codex reproduced a regression — stale entry A and reopened entry B both await the same previous teardown inside `connect()`, both build connections; when B connects first, A's continuation calls `JoinSprint` on B's shared connection, and A's new `disconnect()` then stops B, leaving A's connection open. Codex suggests serializing hub lifecycle operations across room instances inside `useRetroRoom.ts`, rechecking supersession before connecting, and retaining ownership through stale cleanup, with `useSprintHub.ts` still pinned. This is fix cycle 3 of 3 — the last round before escalation.

**Question:** What exact mechanism should cycle 3 row 1 implement (inside `useRetroRoom.ts`, or with a pin amendment if truly needed)?

**Recommendation (team lead):** a module-level promise chain (lock) in `useRetroRoom.ts` that serializes each room's connect and disconnect, with a supersession recheck after acquiring it and before `connect()` — Confidence: medium — it follows the reviewer's fix and keeps the pin, but I have not read `connect`/`disconnect` myself.

### Answer
**Ruling: keep the pin; serialize every hub lifecycle call across room instances in `useRetroRoom.ts`, and stop only the connection the room owns.** By: architect, 2026-10-23. Confidence: high. I read `useSprintHub.ts` `connect`/`disconnect` and the current `useRetroRoom.ts`. A grep shows `useRetroRoom.ts` is the only caller of `connect`/`disconnect`; `TimerControls.vue` and `useReadiness.ts` only read `getHubConnection()`. So one lock in this file covers every lifecycle operation.

**Where my Q1 ruling went wrong.** It assumed a later `connect()` always stops the previous connection before building its own. Two entries waiting on the same teardown inside `connect()` both pass that step and then both `build()`, so the shared `connection` can belong to either one. Q1's bare `disconnect()` then stops whichever connection is current, which can be the other room's. Only exclusion fixes that, not ordering arguments.

**The mechanism (cycle 3, row 1) — all in `web/reflekt-web/src/composables/useRetroRoom.ts`:**
1. **One module-level lock**, outside `useRetroRoom()`, shared by every room instance. It is a promise chain: a helper `withHubLock(op)` runs `op` only after every earlier op has settled, whether it resolved or rejected. A failed op must never jam the chain: the chain stores the run promise with its rejection swallowed, and the caller still gets the real result.
2. **Per room, `ownedConnection`** (type `ReturnType<typeof getHubConnection>`, initially `null`). Import `getHubConnection` from `./useSprintHub`; it is already exported, so the pinned file does not change.
3. **`stopOwned()`** — called only while holding the lock: if `ownedConnection` is non-null and `getHubConnection() === ownedConnection`, `await disconnect()`; then set `ownedConnection = null`. It never stops a connection it does not own.
4. **`releaseHub()`** = `withHubLock(stopOwned)`. It replaces every bare `disconnect()` in the file: the `onUnmounted` hook, the `useSprintHub(onKicked)` callback, and the post-connect stale branch from Q1 (line 155), which is removed.
5. **Connecting** (in `loadRoomData`, replacing the current `try { await connect(sprintId) } catch {…}` and the stale branch after it) runs as one locked op that returns `'ok'`, `'stale'` or `'failed'`:
   - On acquiring the lock, if `superseded(entry)`, return `'stale'` without calling `connect`, so nothing is built.
   - Otherwise `await connect(sprintId)` inside try/catch. Whether it resolves or rejects, set `ownedConnection = getHubConnection()` next; inside the lock, `connect()` always builds before it can fail at `start`/`invoke`, so this is this room's connection.
   - Then, if `superseded(entry)`, `await stopOwned()` and return `'stale'`. Otherwise return `'ok'` on success or `'failed'` on a rejection.
   - After the op, outside the lock: `'stale'` → return; `'failed'` → `failStage('data', "Can't connect to the live board")`; `'ok'` → `joined`, `loading`, then the Reporting report load as today.
   - On `'failed'` the room keeps `ownedConnection`, so a later unmount (via `releaseHub`) or retry still stops it; the retry's `connect()` stops it first anyway.

**Why it holds under concurrent waiters.** Lifecycle ops of all rooms run one at a time, so within an op only that op changes the shared `connection`. Two entries can no longer both pass the teardown inside `connect()`, `JoinSprint` is always invoked on the connection the same op built, and a stale room skips connecting or stops only its own connection. An unmount or kick that runs after a newer room has connected finds `getHubConnection() !== ownedConnection` and stops nothing.

**Not changed:** `useSprintHub.ts` (the Must-NOT-Change "SignalR handling" pin stands — only the eight `fetchCards`/`fetchGroups` arguments), the event handlers, the entry stages, and `retryEntry`'s `entryRunning` guard. No plan amendment.

**Accept (review of the diff and the walkthrough):**
- In `useRetroRoom.ts`, `connect(` and `disconnect(` each appear only inside the locked op and `stopOwned`. No other bare `disconnect()` call remains.
- `git diff -- web/reflekt-web/src/composables/useSprintHub.ts` still touches only the eight call arguments.
- Walkthrough (Step 13/14), checked in DevTools:
  - Leave the room during "Joining retro…" and reopen it at once, twice in a row: exactly one `/hubs/sprint` connection is open and live updates reach the reopened room.
  - Leave for a non-room page while joining: no hub connection stays open.


## Q3 — Escalation after fix cycle 3/3: pending `JoinSprint` blocks teardown — what fix, and continue or abort?

**From:** team-lead · **To:** architect · **Status:** Answered · **Raised:** 2026-10-23

**Context:** `review.md` § Step 2 — Re-review (cycle 3): the Q2 lock closed the concurrent-waiters race, but `releaseHub()` queues behind the locked `connect()`, which can wait on `JoinSprint` indefinitely. A room left mid-join stays connected, its events mutate the next room's stores, and the next room stays loading. The fix-round cap is spent; the run is unattended, so the item is deferred to the review queue with this recommendation (force-accept not available).

**Question:** What exact fix closes this, how sure are you, and should the item continue (3 more cycles at the HIGH bar) or be aborted?

### Answer
**Recommendation: continue — one more fix cycle at the HIGH bar — with the fix below. `useSprintHub.ts` stays pinned; no plan change.** By: architect, 2026-10-23. **Confidence that this closes the class: medium-high.** The mechanism no longer depends on any lifecycle call settling by itself, but two of my rulings on this surface (Q1, Q2) have already failed review, so the cycle should be re-reviewed by Codex with its reproduction harness rerun.

**What Q2 got wrong.** It made lifecycle operations exclusive but not cancellable. An unmount has to wait for the active `connect()` to finish, and `invoke('JoinSprint')` has no time limit, so cleanup can wait forever. The cure is for the leaving room to stop the connection its active operation built **directly**, without queuing. `HubConnection.stop()` rejects an in-flight `start()` (stopped during negotiation) and every pending `invoke()` (cancelled because the connection closed), so the locked `connect()` settles at once and the lock is released only after cleanup.

**The fix (cycle 4, one row) — all in `web/reflekt-web/src/composables/useRetroRoom.ts`; the Q2 lock, `ownedConnection`, `stopOwned()` and `releaseHub()` stay:**
1. **Capture before awaiting.** Inside the locked connect operation, after the superseded-on-acquire check:
   - If `getHubConnection()` is non-null, `await disconnect()` first. We hold the lock, so this stale connection belongs to no live room; it is the same step `connect()` would take, done here so the next point is guaranteed.
   - Then **call `connect(sprintId)` without awaiting it**: `const pending = connect(sprintId)`. With `connection` null, `connect()` runs `build()` synchronously before its first `await`.
   - On the very next line, **before any `await`**, set `ownedConnection = getHubConnection()` and set a per-room `connectInFlight = true`.
2. **Then settle it.** `try { await pending } catch {}`, then `connectInFlight = false`. Then, as Q2 says: if `superseded(entry)` → `await stopOwned()` and return `'stale'`; otherwise return `'ok'` or `'failed'`.
3. **Cancel on leave.** Add a per-room `cancelHub()` and call it in place of `releaseHub()` in `onUnmounted` and in the `onKicked` callback (after `currentEntry++` on unmount, as today). It does the following:
   - If `connectInFlight` and `ownedConnection` are set, call `ownedConnection.stop()` **immediately, not through the lock and not awaited**. The pending `connect()` rejects, the locked operation sees `superseded` (or the kick), runs `stopOwned()` and releases the lock. `stop()` is idempotent, so the later `disconnect()` inside `stopOwned()` is harmless.
   - Then `releaseHub()` as today; by the time it runs, `ownedConnection` is null and it does nothing.
   - It never calls the shared `disconnect()` outside the lock, and it stops only this room's own connection object.
4. **Kick while connected** (no connect in flight) behaves as today: `releaseHub()` stops the owned connection under the lock.

**Why this closes the class.** Every way a room leaves now ends its connection without waiting on the network:
- **Before connect** — the room is skipped at lock acquire.
- **During negotiation or `JoinSprint`** — the room's own `stop()` makes `connect()` reject; cleanup runs inside the lock.
- **After connect** — `stopOwned()` runs under the lock.

Exclusion still holds until cleanup settles, so the next room's connect waits only for a `stop()`, which completes on its own. The Q2 guarantees are unchanged: two entries cannot both build a connection, and a stale room never stops a connection it does not own.

**What could still be wrong (why not high):** an `@microsoft/signalr` build where `stop()` does not reject a pending `invoke`. The version in use (^10) rejects pending invocations on close, but this is not verified against the installed package. The re-review should rerun Codex's reproduction for (a) a room left during `start`, (b) a room left during a held `JoinSprint`, and (c) the Q2 concurrent-waiters case.

**Accept:**
- `useSprintHub.ts` diff: still only the eight `fetchCards`/`fetchGroups` arguments.
- In `useRetroRoom.ts`: `ownedConnection` is set on the line after `connect(` with no `await` between; `onUnmounted` and `onKicked` call `cancelHub()`; no `disconnect(` outside `stopOwned` and the locked operation.
- Codex reproductions (a)–(c): the left room ends with zero open connections, and the next room connects and leaves loading.
- Walkthrough: leave during "Joining retro…" (with `JoinSprint` throttled in DevTools) → no `/hubs/sprint` connection remains, and reopening gives exactly one.

**Turned down:**
- **Abort:** the other 12 spec outcomes are built and reviewed, and this defect is a narrow teardown race in code F36 already rewrote.
- **Amending the pin** to give `connect()` a cancellation token: equally sound, but it widens a pinned SignalR surface when the fix fits without it. It is the fallback if this cycle fails.
- **Force-accept:** not available; and a HIGH that leaks a live hub into the next room should not ship as accepted.

## Q4 — Escalation after owner-granted cycle 4: leave during WebSocket transport startup still blocks the next room — what fix, and continue or abort?

**From:** team-lead · **To:** architect · **Status:** Answered · **Raised:** 2026-10-23

**Context:** The owner granted one extra cycle ("continue", campaign directive d1). Cycle 4 applied the Q3 fix. `review.md` § Step 2 — Re-review (cycle 4): reproductions (b) held `JoinSprint` and (c) concurrent waiters pass, as do the earlier races; the held-handshake half of (a) passes. New HIGH at `useRetroRoom.ts:78`: with the WebSocket held before `onopen`, `stop()` in SignalR 10.0.0 waits for transport startup (`HttpConnection._stopInternal()`), so A stays `Disconnecting`, B stays loading; no SignalR startup timeout applies. Codex suggests a cancellable startup or a deadline that aborts the attempt, or Q3's fallback pin amendment on `useSprintHub.ts`. The run is unattended; the granted cycle is spent.

**Question:** What exact fix closes this (including whether the `useSprintHub.ts` pin must be amended), how sure are you, and should the owner be asked for one more cycle, or should row 2 be aborted to a backlog row and F36 closed with it listed? Force-accept is not available.

### Answer
**Recommendation: abort the row to a backlog row and close F36 with the HIGH listed as pre-existing. Do not ask for another cycle.** By: architect, 2026-10-23. **Confidence in this recommendation: high.** The defect is not new. `main`'s code blocks the same way. Any real fix has to amend the pinned `useSprintHub.ts`, and three rulings on this surface have already failed review. **Confidence that the fix below closes the class: medium-high.** It is checked line by line against the installed `@microsoft/signalr` 10.0.0, but it has not been re-run against Codex's reproductions.

**What I verified in the installed package** (`node_modules/@microsoft/signalr`, `package.json` version 10.0.0, `dist/esm`):
- `HttpConnection.stop()` → `_stopInternal()` runs `await this._startInternalPromise` before it calls `transport.stop()` (`HttpConnection.js` lines 118–124).
- That promise waits on `WebSocketTransport.connect()`, which settles only in `onopen` or `onclose` (`WebSocketTransport.js`, the `new Promise` in `connect`). `onerror` only logs. No timeout is armed at this stage: `options.timeout` covers only the negotiate POST (line 231), and `serverTimeout` starts only after the handshake request.
- The socket object is stored in `this._webSocket` only inside `onopen`. Until then, nothing outside the transport can close it.
- `HttpConnection.start()` also awaits `_startInternalPromise` before it checks `Disconnecting` (lines 71–80). So a held start cannot be rejected from outside: `stop()` makes it reject only after the socket settles.
- Once `stop()` has been called, that connection never delivers a hub event. Its state becomes `Disconnecting` synchronously. When the socket later opens, `_startInternal` does not set `Connected`, `_stopInternal` closes the transport, `start()` rejects, and the hub handshake is never sent.

**Why the pin must be amended (no fix exists in `useRetroRoom.ts` alone).** The module-level `connection` in `useSprintHub.ts` stays set to A's connection until `await connection.stop()` returns (`disconnect()`, lines 219–224). It is private: only the getter is exported. `connect()` begins with `if (connection) await disconnect()` (line 213). So once A's socket is held, every path B can take waits on A's held start: through the lock, through `stopOwned`, or through `connect()` itself. A deadline in `useRetroRoom.ts` could release the lock, but B's `connect()` would still wait inside `disconnect()`. Q3 said this fix "fits without" the amendment, and that was wrong.

**The exact fix, for the backlog row** (or for a cycle 5 if the owner overrules me). Rule: **nothing ever waits for a previous connection to finish stopping.** Everything that reads or replaces the shared reference runs synchronously, so ownership is decided by object identity and no lock is needed.

1. **Pin amendment.** Plan Must-NOT-Change row "SignalR handling" becomes: "`useSprintHub.ts` diff: the eight `fetchCards`/`fetchGroups` arguments, plus `connect()` and `disconnect()` as below; `build()`, its event handlers and `getHubConnection()` unchanged."
2. **`useSprintHub.ts` `disconnect()`**, a plain function that returns a promise: `const c = connection; connection = null; return c ? c.stop() : Promise.resolve()`. The reference is cleared before the stop starts, not after.
3. **`useSprintHub.ts` `connect(sprintId)`**:
   - If `connection` is set, `disconnect().catch(() => undefined)`, not awaited.
   - Then `build(sprintId)`, then capture `const c = connection!` before any `await`.
   - Then `await c.start()` and `await c.invoke('JoinSprint', sprintId)` on `c`. Today the code uses `connection!`, which can be another room's connection after an `await`.
   
   `connect` is now synchronous up to `c.start()`. So `getHubConnection()` read on the line after the call is this call's connection; Q3 already relies on that capture.
4. **`useRetroRoom.ts`:**
   - Remove `hubLock`, `withHubLock`, `releaseHub`, `connectInFlight` and `cancelHub`. Keep `ownedConnection` and `superseded`.
   - `stopOwned()` becomes synchronous and is never awaited. If `getHubConnection() === ownedConnection`, call `disconnect().catch(() => undefined)`. Otherwise call `ownedConnection?.stop().catch(() => undefined)`, which stops only the room's own object; `stop()` is idempotent. Then set `ownedConnection = null`.
   - `connectHub(entry)` is no longer locked:
     - If `superseded(entry)`, return `'stale'`.
     - Otherwise `const pending = connect(sprintId)`, and on the next line `ownedConnection = getHubConnection()`.
     - Then `try { await pending } catch { failed = true }`.
     - Then, if `superseded(entry)`: `stopOwned()` and return `'stale'`. Otherwise return `'failed'` or `'ok'`.
   - `onUnmounted`, after `currentEntry++` and `abandonPendingIdentity()`, calls `stopOwned()`. So does the kicked callback.
5. **Readers unaffected.** `TimerControls.vue:57/68` and `useReadiness.ts:11` already handle a `null` `getHubConnection()`. The connection is now `null` slightly earlier, during a stop.

**Why it closes the class.** All three failed rulings had one root: something waited on another connection's lifecycle. Q1 relied on ordering, Q2 on a lock, and Q3 waited on the network. Here nothing waits:
- A leaving room stops its own object at once.
- The next room's `connect()` starts the previous connection's stop and builds its own in the same synchronous run.
- A late continuation of a stale entry finds a different current connection, so it stops only its own.
- A stopped connection delivers no further events (verified above).
- The Q2 race, two entries both building after one shared teardown, cannot happen, because `connect()` no longer awaits before `build()`.

A held socket stays `CONNECTING` until the browser gives up. SignalR then closes it at once on open, and nothing waits on it.

**Why abort rather than a cycle 5:**
- **Pre-existing, not introduced by F36.** On `main`, `useRetroRoom.ts` calls `disconnect()` in `onUnmounted` and `await connect()` on entry, and `connect()` awaits `disconnect()`. A room left while its socket is held therefore blocks the next room in exactly the same way today. F36 has fixed every race it introduced: reproductions (b) and (c), the delayed summary and the stale join all pass. So shipping F36 regresses nothing on this path.
- **Narrow trigger.** It needs a WebSocket upgrade that is accepted but never answered, and the user leaving during that window. It ends when the browser fails the socket.
- **The fix is a scope change.** It amends a pinned surface and rewrites the lifecycle F36 has reworked four times. It deserves its own plan and a fresh first review, not a fifth fix round on a spent cap.

**For the close (team lead):**
- Record the HIGH as deferred. Wording: "Pre-existing (present on `main`): leaving the retro room while the hub WebSocket is still opening blocks the next room's entry until the browser fails the socket; fix needs the `useSprintHub.ts` lifecycle change in `questions.md` Q4." Link it to the backlog row.
- Backlog row: the next free `F{N}`, assigned by you or the PO, not by me. Suggested title "Hub connection lifecycle never waits on a stop". Scope: Q4's fix steps 1–5. Acceptance: Codex's reproductions (a) held handshake, (a) held WebSocket opening, (b), (c), delayed summary and stale join all pass; walkthrough: leave during "Joining retro…" with the socket throttled, then reopen; there is exactly one live `/hubs/sprint` connection and the room leaves loading.
- The Q3 walkthrough item for a held `JoinSprint` still belongs to Step 13/14. The held-WebSocket item moves to the backlog row.

**Turned down:**
- **A deadline in `useRetroRoom.ts`:** it cannot help while `connect()` awaits the old stop inside the pinned file (above).
- **A custom `WebSocket` constructor passed to `withUrl`:** it lives in `build()`, which is pinned, and it is more code than step 3.
- **Asking the owner for cycle 5:** the run would wait on the owner for a defect that `main` already has.

**Team lead's disposition (2026-10-23):** recommendation taken — presumed (proceed-default), not user-confirmed; owner away. Row 1 of the cycle-4 fix list (the transport-startup HIGH) is moved out of F36 as pre-existing (present on `main`), to a backlog row via `change-notes.md` Note 2; F36 closes with it listed in `summary.md`. The owner's granted cycle ("continue", directive d1) was used in full; no further cycle requested.
