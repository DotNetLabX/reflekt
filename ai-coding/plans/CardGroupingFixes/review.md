# Card Grouping Fixes — Review

## Reviewed By
`reviewer` (Sonnet agent)

## Verdict: APPROVE

## Pre-commitment Predictions

1. **DnD cross-column validation** — easy to get wrong; dragging from a different column onto a target should be rejected. *Found:* Correctly validated via `payload.columnType !== props.group.columnType` in `onDrop` and `payload.columnType !== column` in `onColumnDrop`. No issue.
2. **Self-drop guard** — dragging a group onto itself should be a no-op. *Found:* Correctly guarded with `payload.groupId === props.group.id`. No issue.
3. **`as unknown as CardGroup` cast in `onDrop`** — reconstructing a partial CardGroup from the payload is lossy; the parent's merge logic depends on `cards.length` being accurate. *Found: confirmed issue — see HIGH finding.*
4. **TypeScript strictness on array index access** — `cards[0]` with `noUncheckedIndexedAccess` or strict mode could fail. *Found: confirmed — build fails with 3 TS errors introduced by this PR.*
5. **`remove-card` on a group drop** — when a card-type drag lands on a CardGroupItem drop target, the component emits `remove-card` instead of adding the card to the target group. *Found: confirmed design issue — see MEDIUM finding.*

## Findings

### ~~HIGH: TypeScript build fails — 3 type errors introduced by this PR~~ RESOLVED (Cycle 1)

**File:** `web/reflekt-web/src/components/CardGroupItem.vue:16`, `:20`, `:22`
**File:** `web/reflekt-web/src/views/RetroRoomView.vue:638`, `:640`

**Issue (original):** `vue-tsc` reported 3 type errors on `cards[0]` index access without null-guard.

**Resolution:** Fixed via `?.` optional chaining at line 16 and 20, `!` non-null assertion at line 22 in `CardGroupItem.vue`, and `!` non-null assertions at lines 638/640 in `RetroRoomView.vue`. All three errors eliminated. The remaining `card.ts:65` error is pre-existing and not introduced by this PR.

---

### MEDIUM: Card dragged onto a group target triggers `remove-card` instead of `add-to-group`

**File:** `web/reflekt-web/src/components/CardGroupItem.vue:291-294`

**Issue:** When a `type: 'card'` payload (a card dragged out of an expanded group) lands on another `CardGroupItem` drop target, `onDrop` emits `remove-card(sourceGroupId, cardId)` and returns early — it never adds the card to the target group. The user sees the card detached from its source group but not added to the target group they dropped it on.

The plan (Step 4) says: "When a `type: 'card'` drag is dropped onto a different group, call `removeCardFromGroup` first, then `addCardToGroup` on the target" — then resolves to "Just call `removeCardFromGroup` — the card gets a new solo group. The SM can then drag that solo group onto the target if desired."

The implementation follows the simplified plan decision (remove only), which is a valid deviation. However the UX consequence is that dropping a card directly onto another group visually does nothing to the target — only removes from source. This could confuse users who expect a merge. The implementation.md does not mention this deviation explicitly.

**Fix (if keeping current behaviour):** This is acceptable given the plan's own simplification decision. Downgraded to MEDIUM — executor should confirm this matches the intent and document it in implementation.md as a deliberate deviation. No code change required if the simplification is intentional.

---

### LOW: `dragClasses` applies `cursor-grab` to the whole card even in non-Grouping phases

**File:** `web/reflekt-web/src/components/CardGroupItem.vue:235`

**Issue:** `cursor-grab` is applied when `props.canGroup && !dragging.value`. `canGroup` is only true during the Grouping phase for the SM, so this is functionally correct — but `canGroup` is also passed as `false` for participants in all phases, so the cursor won't appear for them. No bug; cosmetic note only.

---

### LOW: Solo group rename button visible during Grouping but title is never shown in solo rendering

**File:** `web/reflekt-web/src/components/CardGroupItem.vue:63-70`

**Issue:** The plan says "The Rename button should still appear for SM during Grouping phase (so the SM can pre-title a solo card before merging)." This is correctly implemented. However the `editingTitle` input and `saveTitle` emit `update-title` correctly. The solo group's title is saved server-side but never rendered (solo groups show `group.cards[0].text` as their display, not `group.title`). This is by design (business rule 14 — solo groups look like plain cards), but a pre-set title will silently be discarded visually unless the group is later merged. No defect — matches spec intent.

## Positive Observations

- The `dragClasses` computed object is clean and centralises all drag state styling in one place.
- `@dragstart.stop` on individual cards correctly prevents the parent group's dragstart from firing — a subtle correctness detail done right.
- Column-level drop correctly rejects `type: 'group'` payloads (only `type: 'card'` triggers ungroup), matching the plan decision.
- Firefox `try/catch` around `dataTransfer.types` read in `onDragOver` is a good defensive pattern.
- The `onColumnDragLeave` guard (`if (columnDragOver.value === column)`) prevents the highlight from clearing when the mouse moves over a child element that re-fires dragleave on the parent — a subtle DnD correctness detail.
- Both `@merge` and `@remove-card` are wired on both columns symmetrically.
- Error handling with `showToast` on all async store calls is consistent with the rest of the file.
- `DragPayload` interface is exported, enabling future consumers to type-check drag data.

## Gaps

- No test for cross-column drag rejection at runtime (only validated by code path; no automated coverage). Acceptable given the project has no frontend unit tests.
- The `mergeGroups` handler's `else` branch (both multi-card groups) calls `createGroup` with all card IDs. The backend is expected to dissolve both old groups — this is not verified in the review, but was described as correct in the plan.

## Open Questions

- None. All CRITICAL/HIGH findings are high-confidence.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build (vite) | pass | `npm run build` | Built in 784ms, 1816 modules (Cycle 1 re-run) |
| Type-check (vue-tsc) | pass* | `npm run build` | 0 new errors; 1 pre-existing error in card.ts (not introduced by this PR) |
