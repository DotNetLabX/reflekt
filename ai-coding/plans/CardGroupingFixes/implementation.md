# Card Grouping Fixes — Implementation

## Files Modified

- `web/reflekt-web/src/components/CardGroupItem.vue` — Complete rewrite to fix solo group rendering and add drag-and-drop interaction:
  - **Step 1 (BUG-006):** Added `v-if="isSolo"` / `v-else` conditional rendering. Solo groups (1 card) render as plain cards matching CardItem's visual style — no title header, no expand/collapse, no badge, no group border. Multi-card groups keep full chrome unchanged. Voting controls on solo groups use the ChevronUp/Minus pattern from CardItem instead of ThumbsUp/ThumbsDown.
  - **Step 2:** Added `draggable` attribute (bound to `canGroup`) and `@dragstart`/`@dragend` handlers on both solo and multi-card wrapper divs. Drag payload uses `dataTransfer` with JSON containing `{ type, groupId, columnType, cardIds }`.
  - **Step 3:** Added `@dragover`/`@dragleave`/`@drop` handlers on both wrappers to act as drop targets. Validates same-column constraint and prevents self-drop. Emits `merge` event with source and target groups.
  - **Step 4:** Individual cards inside expanded multi-card groups are draggable when `canUngroup` is true. Uses `@dragstart.stop` to prevent bubbling to parent group drag. Payload has `type: 'card'` with `cardId` and `sourceGroupId`. Drop on another group emits `remove-card`. Drop on column background handled by RetroRoomView.
  - **Step 5:** Visual feedback via `dragClasses` computed — `opacity-50` on drag source, `ring-2 ring-primary-400` on valid drop target, `cursor-grab`/`cursor-grabbing`. GripVertical icon from lucide shown for SM during Grouping phase as drag affordance.
  - New emits: `merge: [source: CardGroup, target: CardGroup]`, `remove-card: [groupId: number, cardId: number]`
  - New export: `DragPayload` interface for drag data typing.

- `web/reflekt-web/src/views/RetroRoomView.vue` — Wired up drag-and-drop merge and ungroup handlers:
  - Added `@merge` and `@remove-card` event handlers on both WentWell and DidntGoWell `CardGroupItem` instances.
  - Added column-level drag/drop handlers (`@dragover`, `@dragleave`, `@drop`) on both TransitionGroup containers for card-level removal (drag card out of group onto column background).
  - Added `columnDragOver` ref for column drop zone highlight (`bg-primary-50/30`).
  - Added `mergeGroups(source, target)` handler implementing the merge logic: solo+multi uses `addCardToGroup`, multi+solo uses `addCardToGroup` (into source), same-size or both-multi uses `createGroup` with all card IDs.
  - Added `removeCardFromGroup(groupId, cardId)` handler.
  - Added `onColumnDragOver`, `onColumnDragLeave`, `onColumnDrop` handlers for column-level drop (card ungroup only, validates `type: 'card'`).

## Key Decisions

- Voting controls on solo groups use the ChevronUp/Minus style (matching CardItem) rather than ThumbsUp/ThumbsDown style (used by multi-card groups). This keeps solo groups visually indistinguishable from regular cards per business rule 14.
- The `merge` event passes lightweight source/target group data. The parent (RetroRoomView) determines which store action to call based on group sizes, keeping CardGroupItem stateless regarding API calls.
- Card-level drag uses `.stop` modifier to prevent the group-level dragstart from also firing.
- Column-level drop only accepts `type: 'card'` payloads (not `type: 'group'`), so dragging a whole group onto empty column space does nothing — groups can only merge with other groups.

## Deviations from Plan

- Steps 2, 3, and 4 were implemented together in CardGroupItem.vue since they all modify the same template sections and are interdependent. Each step's requirements were fully addressed.
- The plan suggested emitting a `merge` event with `[sourceGroup, targetGroup]` tuple. Implementation uses the same signature but reconstructs a minimal source group from the drag payload data, since the full CardGroup object isn't available from `dataTransfer` serialization.
- **Card-to-group drop (MEDIUM finding, intentional):** When a `type: 'card'` drag lands on another CardGroupItem, only `remove-card` is emitted (removing the card from its source group into a new solo group). The card is NOT automatically added to the target group. This follows the plan's Step 4 simplification decision: "Just call `removeCardFromGroup` — the card gets a new solo group. The SM can then drag that solo group onto the target if desired." This is a two-step UX (ungroup then merge) rather than a one-step drag-to-regroup.

## Review Fix Cycle 1

- Fixed TypeScript strict-mode errors flagged by reviewer:
  - `CardGroupItem.vue:16` — `group.cards[0].text` changed to `group.cards[0]?.text`
  - `CardGroupItem.vue:20` — `soloCard.evidenceLinks.length > 0` changed to `soloCard?.evidenceLinks.length`
  - `CardGroupItem.vue:22` — `soloCard.evidenceLinks` changed to `soloCard!.evidenceLinks`
  - `RetroRoomView.vue:638` — `source.cards[0].id` changed to `source.cards[0]!.id`
  - `RetroRoomView.vue:640` — `target.cards[0].id` changed to `target.cards[0]!.id`
- Confirmed MEDIUM finding (card-to-group drop) is intentional per plan's Step 4 simplification. Documented above.
