# Bugs

Log of user-visible defects awaiting a fix. Each bug gets its own entry with repro, root cause (when known), and proposed fix.

Format: one entry per bug. Include file:line, severity, repro, and what to do. Move to "Resolved" with the commit that fixes it.

Severity guide:
- **Critical** — data loss, security, feature completely broken for everyone.
- **High** — feature broken for a common flow, no workaround.
- **Medium** — feature broken for an edge case, or workaround exists.
- **Low** — cosmetic, rare, or minor annoyance.

---

## Open

_(none)_

---

## Resolved

### BUG-011 — Reporting phase hides Action Items column
**Severity:** Medium
**Source:** User testing, 2026-04-22
**File:** `web/reflekt-web/src/views/RetroRoomView.vue:441`

**Root cause:** `showActionItems` was gated on `policyStore.isAllowed(phase, 'CreateActionItem')`, which only returns true for `Discussing`. Reporting had no policy entries, so the Action Items column was hidden.

**Fix:** `showActionItems` now also returns true when `currentPhase === 'Reporting'` (read-only display).

---

### BUG-010 — Card groups not sorted by vote count
**Severity:** Medium
**Source:** User testing, 2026-04-22
**File:** `web/reflekt-web/src/views/RetroRoomView.vue:439-440`, `web/reflekt-web/src/stores/cardGroup.ts:26-29`

**Root cause:** `wentWellGroups` and `didntGoWellGroups` getters filtered by column type but never sorted. Groups with high vote counts appeared below solo cards with fewer votes.

**Fix:** Computed properties in `RetroRoomView.vue` now sort groups by `voteStore.voteCountForGroup` descending.

---

### BUG-009 — Vote retract not broadcast to other participants in real time
**Severity:** High
**Source:** User testing, 2026-04-22
**File:** `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/RetractVoteEndpoint.cs`, `src/BuildingBlocks/BuildingBlocks.Persistence/DomainEventPublishingInterceptor.cs`

**Root cause:** `MarkRetracted()` raised `VoteRetractedEvent` on the Vote entity, then `Remove(vote)` deleted it. After `SaveChangesAsync`, the deleted entity was `Detached` from the EF change tracker. The `DomainEventPublishingInterceptor.SavedChangesAsync` scans `ChangeTracker.Entries()` — detached entities are invisible, so `VoteRetractedEvent` was silently lost and never broadcast.

**Fix:** Removed `MarkRetracted()` call (only purpose was raising the lost event). Added manual `hubContext.Clients.Group(...).SendAsync("VoteRetracted", ...)` after save.

---

### BUG-008 — Vote cast not broadcast to other participants in real time
**Severity:** High
**Source:** User testing, 2026-04-22
**File:** `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/CastVoteEndpoint.cs`

**Root cause:** Serializable transaction wraps the vote insert. `SaveChangesAsync` fires the domain event within the transaction (before `CommitAsync`). The `BroadcastVoteCountOnVoteCastHandler` resolves in a new DI scope with a separate `DbContext` that cannot see uncommitted data — it broadcast the OLD vote count.

**Fix:** `vote.ClearDomainEvents()` suppresses automatic broadcast. Manual `hubContext.Clients.Group(...).SendAsync("VoteCast", ...)` fires after `tx.CommitAsync()` with the correct count.

---

### BUG-007a — Grouped cards still visible as individual cards on other participants
**Severity:** High
**Source:** User testing, 2026-04-22
**File:** `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs`

**Root cause:** Two `SaveChangesAsync` calls — first creates the group (fires `GroupCreated` domain event), second reassigns cards. The broadcast handler fires on the first save, queries cards with `GroupId == newGroup.Id`, finds none (cards not yet reassigned), and broadcasts an empty group. Clients see the new group AND the original individual cards.

**Fix:** `newGroup.ClearDomainEvents()` suppresses automatic broadcast. Manual `hubContext.Clients.Group(...).SendAsync("GroupCreated", ...)` fires after the second save with complete data.

---

### BUG-007 — Voting broken: race condition, multi-vote, counts not syncing
**Severity:** Critical
**Source:** User testing, 2026-04-22
**File:** `CastVoteEndpoint.cs`, `VoteRepository.cs`, `CardGroupItem.vue`, `cardGroup.ts`, `card.ts`

Multiple voting defects:
- Budget could be exceeded (negative remaining votes) — no atomic check-and-insert.
- Users could vote multiple times on the same group — no per-group uniqueness check.
- Vote counts showed 0 on page load — `fetchGroups` never initialized the vote store.
- Frontend vote buttons stayed enabled after voting.

**Fix:**
- Backend: serializable transaction wraps budget check + existing vote check + insert atomically. `BeginSerializableAsync` added to `VoteRepository`.
- Backend: `GetOneByParticipantAndGroupAsync` rejects duplicate votes per group (409).
- Frontend: `cardGroup.ts` `fetchGroups` now calls `voteStore.initVoteCounts()` with server data. Removed broken init from `card.ts`.
- Frontend: vote button disabled when `myVoteCount > 0`. Unified solo/group voting UI to single toggle button (chevron-up → check icon when voted).

---

### BUG-006 — Solo groups render with group chrome and duplicated text
**Severity:** Medium
**Source:** User report, 2026-04-22
**File:** `web/reflekt-web/src/components/CardGroupItem.vue`

**Root cause:** `CardGroupItem` renders all groups identically regardless of card count. Solo groups showed title + expand/collapse + card text (duplicated).

**Fix:** Render solo groups (1 card) as plain card items without group chrome. Group styling only for 2+ cards.

---

### BUG-005 — Card duplicated on writer's screen after creation
**Severity:** Medium
**Source:** User report, 2026-04-22
**File:** `web/reflekt-web/src/views/RetroRoomView.vue`, `web/reflekt-web/src/composables/useSprintHub.ts`

**Root cause:** Optimistic UI pushed a card with negative id. SignalR broadcast added a second copy with the real id. Dedup guard missed because ids differed.

**Fix:** Removed optimistic UI from `addNote()` and `addActionItem()`. Cards appear on SignalR broadcast only.

---

### BUG-001 — Closed sprint page is a navigation dead-end
**Severity:** High
**Source:** User report, 2026-04-21
**File:** `web/reflekt-web/src/views/SprintView.vue`
**Fix commit:** `4f38656` ("[Fixes] Round 1")

After closing a sprint, the Sprint detail page shows the closed-state card but renders no action buttons — every button in the template (`Start Sprint`, `Open Retro Room`, `Copy invite link`, `Advance to…`, `Close Sprint`) is gated on `state === 'Draft'` or `state === 'Active'`. There is no link back to the sprint list (which lives on `/` / `WelcomeView`) and no link forward to the sprint report, so the organizer is stranded on the page with only the browser back button as an escape hatch.

**Repro:**
1. Sign in, open an Active sprint, click "Close Sprint".
2. The card re-renders with state `Closed`. No buttons. No way to return to the sprint list or start a new sprint without editing the URL.

**Root cause:** `SprintView.vue` has no always-visible navigation. The button block (lines 24–67) is fully state-conditional; `Closed` has no branch.

**Suggested fix:**
- Add an always-visible "Back to Sprints" router-link → `{ name: 'welcome' }` so the page is never a dead end, regardless of state.
- Add a "View Report" router-link → `{ name: 'report', params: { teamId, sprintId } }`, shown when `state === 'Closed'`. The report route already exists but nothing links to it from the app — closing a sprint currently hides the report from the organizer entirely.

---

### BUG-002 — Cards added by one participant don't reach others in real time
**Severity:** High
**Source:** User report, 2026-04-21
**File:** `web/reflekt-web/src/views/RetroRoomView.vue` (consumer), `src/Services/Reflekt/Reflekt.API/Features/Cards/**` (broadcasters)
**Fix commit:** `4f38656` ("[Fixes] Round 1")

Two participants open the same retro room side-by-side. Participant A adds a note; Participant B's screen does not update. B must refresh the page to see A's card. Same behavior expected to affect edits, deletes, votes, and evidence-link events unless verified otherwise.

**Repro:**
1. Open `/sprint/{id}` in two browsers (two different participants).
2. In browser A, type a note and click Add.
3. Browser B: card does not appear.
4. Refresh browser B: card appears.

**Suspected root cause (not yet verified):** Either the server-side broadcast handler isn't publishing the `CardCreated` event to the `sprint:{sprintId}` SignalR group, or the Vue client isn't subscribed to the hub on the retro-room page, or the handler is publishing to the wrong group. Recent git status shows newly-scaffolded `Broadcast*Handler` files in `Features/Cards/**` — the refactor from the old `Events/` folder to feature-slice handlers may have lost a wire-up.

**Investigation steps:** verify (a) SignalR connection established on `RetroRoomView` mount, (b) client joined the `sprint:{sprintId}` group, (c) `BroadcastCardCreatedHandler` is registered in DI and publishes to the correct group, (d) the card store reacts to the incoming SignalR event.

---

### BUG-003 — Other participants' card text is visible during Writing phase
**Severity:** High
**Source:** User report, 2026-04-21
**File:** `web/reflekt-web/src/views/RetroRoomView.vue`, `web/reflekt-web/src/components/CardItem.vue`, `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs`
**Fix commit:** pending (WritingPhaseBlur implementation)
**Note:** Originally marked fixed in `4f38656` but that commit did not implement the blur. Actually fixed via WritingPhaseBlur solo fix (2026-04-22).

Defense-in-depth approach: backend redacts text/assignee/evidence-links for other participants' cards during Writing phase on the list endpoint; frontend applies CSS blur + placeholder on `CardItem.vue` for real-time SignalR pushes; phase transition triggers a card re-fetch to reveal content.

---

### BUG-004 — Participants can perform actions outside their permitted phase
**Severity:** High
**Status:** Resolved via PhaseActionGating feature (`ai-coding/plans/PhaseActionGating/summary.md`).
Phases were informational only — participants could write notes during Voting, vote during Discussing, etc. The PhaseActionGating feature enforces the action matrix on both backend (409 on forbidden actions) and frontend (hidden/disabled controls).
