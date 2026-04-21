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

### BUG-001 — Closed sprint page is a navigation dead-end
**Severity:** High
**Source:** User report, 2026-04-21
**File:** `web/reflekt-web/src/views/SprintView.vue`

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
**File:** `web/reflekt-web/src/views/RetroRoomView.vue`, `web/reflekt-web/src/components/CardItem.vue` (likely)

During the Writing phase, each participant should see that other people's cards exist (so they know progress is being made) but the card text must be blurred/hidden. Revealing other people's notes during Writing biases the group — people copy or anchor on what they see. Today the text is fully visible as soon as the page is refreshed.

**Desired behavior:**
- **Writing phase:** the card is visible as a tile; text of **other participants' cards** is blurred/obscured. The participant always sees their own cards in full and can still edit/delete them.
- **Voting phase:** text of all cards is revealed — voters need to read them to vote.
- **Discussing phase:** text remains visible.

**Suggested fix:** In `RetroRoomView.vue` / `CardItem.vue`, compare `card.authorParticipantId` against the current participant. When `sprint.currentPhase === 'Writing'` and the card is not authored by the current participant, render a blur overlay over the text (e.g. `filter: blur(6px)` with a non-selectable overlay). Lift the blur when phase is `Voting` or later. Ensure the blur is applied via CSS on the rendered text so the real content still isn't shipped in a form the user can trivially un-blur in devtools — ideally the API omits other-participant card text during Writing phase altogether and returns a placeholder; however, that is a deeper backend change, so the frontend blur may be the pragmatic v1 fix. Flag for discussion.

---

## Resolved

_(nothing yet)_
