# Card Grouping Fixes

## Context

Two post-ship frontend bugs in the CardGrouping feature. The backend and stores are fully implemented and working. Both bugs are purely in Vue components and composables.

**BUG-006:** Solo groups (1 card) render with full group chrome — bold title header that duplicates the card text, expand/collapse toggle, group border. The spec's business rule 14 says solo groups must be "visually indistinguishable from ungrouped cards."

**Backlog #20:** The Grouping phase UI has no interaction mechanism for the facilitator to actually merge cards into groups. The store has `createGroup`, `addCardToGroup`, `removeCardFromGroup`, `dissolveGroup` — but no drag-and-drop or any other grouping interaction is wired up. The spec describes drag-and-drop (Flows 2-4).

## Scope

**In scope:**
- Fix CardGroupItem.vue solo group rendering (BUG-006)
- Add drag-and-drop interaction for the Grouping phase (Backlog #20)
- Both WentWell and DidntGoWell columns

**Out of scope:**
- Backend changes (none needed — all endpoints exist and work)
- Store changes (all actions exist: `createGroup`, `addCardToGroup`, `removeCardFromGroup`, `dissolveGroup`)
- New dependencies (use native HTML5 Drag and Drop API, no library)
- Drag-and-drop card reordering within a group (per spec Out of Scope)
- Re-grouping after Voting starts (per spec Out of Scope)

## Domain Model Changes

None.

## Data Model Changes

None.

## Implementation Steps

### Step 1: Fix solo group rendering in CardGroupItem.vue (BUG-006)

**What:** Add a conditional rendering path in `CardGroupItem.vue` so solo groups (1 card) render as plain card items without group chrome — no bold title header, no expand/collapse, no card count badge, no group border. Multi-card groups (2+ cards) keep the current rendering unchanged.

**Files to modify:**
- `web/reflekt-web/src/components/CardGroupItem.vue`

**Details:**
- When `group.cards.length === 1`: render just the card text (matching CardItem's visual style), evidence links if any, and the voting controls (if in voting phase). No title row, no expand/collapse, no "Dissolve" button. The Rename button should still appear for SM during Grouping phase (so the SM can pre-title a solo card before merging).
- When `group.cards.length > 1`: keep the current rendering exactly as-is (title row, badge, expand/collapse, dissolve, rename).
- The outer `div` should not have the group shadow/border styling for solo groups — match CardItem's `bg-{color}-light/30` background with same rounded corners and shadow.
- Solo groups still need to be valid drag/drop targets (Step 3 will add this), so the wrapper div must remain.

**Pattern:** Reference `CardItem.vue` for the visual treatment of a single card.

**Dependencies:** None.

### Step 2: Make CardGroupItem and solo groups draggable (SM only, Grouping phase)

**What:** Add HTML5 drag support to `CardGroupItem.vue`. During the Grouping phase, the SM can drag a solo group (single card) or an entire multi-card group to merge it with another.

**Files to modify:**
- `web/reflekt-web/src/components/CardGroupItem.vue`

**Details:**
- Add `draggable="true"` to the outer wrapper div when `canGroup` is true (SM + Grouping phase). Non-SM participants and non-Grouping phases must not be draggable.
- On `dragstart`: set `dataTransfer` with the group's id and column type (as JSON: `{ groupId: number, columnType: string, cardIds: number[] }`). Apply a visual drag class (e.g., reduced opacity).
- On `dragend`: remove the visual drag class.
- The component needs to emit drag-related data upward, or the parent (RetroRoomView) can handle DnD at the column level. **Recommendation:** Handle drag data via `dataTransfer` (no new props/emits needed for drag start). Drop handling belongs on the parent or on each CardGroupItem as a drop target.

**Pattern:** Native HTML5 DnD — `@dragstart`, `@dragend`, `@dragover.prevent`, `@drop` event handlers.

**Dependencies:** None (can be done with Step 1 in the same file edit).

### Step 3: Add drop targets on CardGroupItem for merging

**What:** Make each `CardGroupItem` a drop target during the Grouping phase. When a dragged group/card is dropped onto another group/card in the same column, call the appropriate store action to merge them.

**Files to modify:**
- `web/reflekt-web/src/components/CardGroupItem.vue`

**Details:**
- On `dragover`: prevent default (to allow drop), but only if the dragged item is from the same column and is not the same group. Apply a visual hover highlight (e.g., ring or border color change).
- On `dragleave`: remove the hover highlight.
- On `drop`: parse `dataTransfer` JSON. Validate same column. Determine the merge action:
  - **If both are solo groups (1 card each):** call `createGroup(sprintId, [draggedCardIds[0], targetCardIds[0]])` — this creates a new 2-card group.
  - **If the drag source is a solo group and the target is a multi-card group:** call `addCardToGroup(sprintId, targetGroupId, draggedCardIds[0])`.
  - **If the drag source is a multi-card group and the target is a solo group:** call `addCardToGroup(sprintId, dragGroupId, targetCardIds[0])` — adds the solo card into the dragged group. OR: create a new group from all cards. **Decision:** Use `addCardToGroup` to add the target's card into the source group. This is simpler and the store already re-fetches groups after each operation.
  - **If both are multi-card groups:** This requires creating a new group from all cards in both. The `createGroup` endpoint accepts 2+ card IDs, so collect all cardIds from both groups and call `createGroup(sprintId, allCardIds)`. The endpoint handles cleanup of the old groups.
- Emit a new event `merge` with the necessary parameters, handled by RetroRoomView. OR: inject the store directly into CardGroupItem. **Decision:** Emit a `merge` event — keeps the component stateless regarding API calls, consistent with how `vote`, `dissolve`, and `update-title` events work.

**New emit:** `merge: [sourceGroup: CardGroup, targetGroup: CardGroup]` — the parent handles calling the right store action based on the group sizes.

**Files to modify (for merge handler):**
- `web/reflekt-web/src/views/RetroRoomView.vue` — add `mergeGroups(source: CardGroup, target: CardGroup)` handler that determines which store action to call and wires it to the `@merge` event on both WentWell and DidntGoWell `CardGroupItem` instances.

**Pattern:** Same emit pattern as existing `@vote`, `@dissolve`, `@update-title` events on CardGroupItem.

**Dependencies:** Step 2.

### Step 4: Add drag-out interaction for removing cards from multi-card groups

**What:** During the Grouping phase, the SM can remove a card from a multi-card group. The spec (Flow 4) describes dragging a card out of a group. Since expanded group cards are visible inside CardGroupItem, add a drag handle on individual cards within an expanded group, and allow dropping them outside the group (onto the column empty space or onto another group).

**Files to modify:**
- `web/reflekt-web/src/components/CardGroupItem.vue`

**Details:**
- Inside the expanded cards list (`v-if="expanded"`), make each individual card row draggable when `canUngroup` is true.
- On `dragstart` for an individual card: set `dataTransfer` with `{ cardId: number, sourceGroupId: number, columnType: string, type: 'card' }` (distinguish from group-level drags which use `type: 'group'`).
- The column container in RetroRoomView needs to act as a drop zone: when a `type: 'card'` drag is dropped onto the column background (not onto another group), call `removeCardFromGroup(sprintId, sourceGroupId, cardId)`.
- When a `type: 'card'` drag is dropped onto a different group, call `removeCardFromGroup` first, then `addCardToGroup` on the target. **Simpler approach:** Since the store re-fetches after each call and the operations are sequential, this works. However, there's a race condition if we chain two calls. **Better approach:** Just call `removeCardFromGroup` — the card gets a new solo group. The SM can then drag that solo group onto the target if desired. This matches the spec's Flow 4 exactly: "SM drags card A out of a group" creates a solo group, period.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue` — add `@dragover.prevent` and `@drop` handlers on each column's TransitionGroup container. On drop of a `type: 'card'`, call `removeCardFromGroup`.

**New emit from CardGroupItem:** None needed — the card-level drag uses `dataTransfer` and the column-level drop handler in RetroRoomView reads it.

**Pattern:** Same HTML5 DnD pattern as Steps 2-3.

**Dependencies:** Steps 1, 2, 3.

### Step 5: Visual polish and drag feedback

**What:** Add CSS classes for drag-and-drop visual feedback to make the interaction discoverable and clear.

**Files to modify:**
- `web/reflekt-web/src/components/CardGroupItem.vue`
- `web/reflekt-web/src/views/RetroRoomView.vue` (column drop zone styling)

**Details:**
- **Drag source:** When dragging, apply `opacity-50` to the source element.
- **Valid drop target:** When a compatible item is dragged over a group, apply a ring highlight (e.g., `ring-2 ring-primary-400`) to signal it's a valid drop target.
- **Invalid drop target:** If dragged item is from a different column or is the same group, no highlight (dragover handler doesn't call preventDefault, so the browser shows the "not allowed" cursor).
- **Column drop zone:** When a card (type: 'card') is dragged over the column background, apply a subtle background highlight to indicate the card will be ungrouped.
- **Cursor:** `cursor-grab` on draggable items during Grouping phase (SM only). `cursor-grabbing` while dragging.
- **SM-only visual cue:** During Grouping phase, SM sees a subtle drag handle indicator (a grip icon or dashed border) on each group/card to signal drag-ability. Participants see no such indicator.

**Dependencies:** Steps 2, 3, 4.

### Step 6: Verify end-to-end

**What:** Build the frontend, start the dev server, and manually verify both fixes.

**Verification checklist:**
- Solo groups in Grouping phase look like plain cards (no title duplication, no expand/collapse, no group border)
- Multi-card groups still show title, badge, expand/collapse, dissolve
- SM can drag a solo card onto another solo card to create a 2-card group
- SM can drag a solo card onto an existing group to add it
- SM can drag a group onto another group to merge all cards
- SM can drag a card out of an expanded group to ungroup it
- Participants cannot drag anything (no drag handles, draggable=false)
- Drop targets only highlight for same-column drops
- Voting phase: groups display correctly, voting works
- Discussing phase: groups sorted by votes
- Non-Grouping phases: no drag-and-drop interaction visible

**Dependencies:** All previous steps.

## Testing Strategy

**Key scenarios:**
1. Solo group renders without group chrome (business rule 14)
2. Multi-card group renders with full chrome (unchanged)
3. Drag solo card onto solo card -> creates group (Flow 2)
4. Drag solo card onto existing group -> adds to group (Flow 3)
5. Drag card out of group -> creates solo group (Flow 4)
6. Drag multi-card group onto another group -> merges all cards
7. Cross-column drag rejected (same column only)
8. Non-SM cannot drag (participants see no drag handles)
9. Outside Grouping phase, no drag interaction
10. After grouping, Voting phase renders groups correctly with vote buttons

## Open Questions

None.
