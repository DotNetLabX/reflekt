# EvidenceLinks — Questions

## Q1: Step 12 file target — RetroRoomView.vue vs CardItem.vue
**From:** Developer
**To:** Architect
**Status:** Resolved
**Step:** Step 12: Frontend — evidence link UI on cards
**File:** `web/reflekt-web/src/views/RetroRoomView.vue`

**Context:** Step 12 says to modify `RetroRoomView.vue` for evidence link display and attach/detach UI. After reading the codebase, card rendering (text, assignee badge, edit/delete buttons, vote row) all lives in `web/reflekt-web/src/components/CardItem.vue`. `RetroRoomView.vue` only renders `<CardItem>` components and handles events emitted from them (`@edit`, `@delete`, `@vote`, `@retract-vote`). Putting evidence link display inside `RetroRoomView.vue` would break the component structure and duplicate card-level rendering outside the card component.

**Question:** Should Step 12 target `CardItem.vue` for the evidence link display + attach/detach UI (emitting new events that `RetroRoomView.vue` handles), instead of `RetroRoomView.vue` as written? And if so, should `RetroRoomView.vue` receive new event handler wiring (for `@attach-evidence-link` and `@detach-evidence-link`) similar to the existing `@edit` and `@delete` handlers?

### Answer
Yes, exactly. The plan was wrong about the file target. Evidence link display and attach/detach UI belong in `CardItem.vue` — that's where all card-level rendering lives. `RetroRoomView.vue` gets the new event wiring only.

**Corrected split:**
- **`CardItem.vue`** — evidence link display (inline previews below card text), attach UI (link icon → input → Enter/Add), detach UI (× button gated by `canEdit`). Emits `@attach-evidence-link(cardId, url)` and `@detach-evidence-link(cardId, evidenceLinkId)`.
- **`RetroRoomView.vue`** — handles the two new events with optimistic update + API call + error toast, same pattern as `saveEdit` and `confirmDelete`. Pass `card.evidenceLinks` as a prop to `CardItem` (already available since the Card interface includes it after Step 11).

Plan updated. Status → Answered.
