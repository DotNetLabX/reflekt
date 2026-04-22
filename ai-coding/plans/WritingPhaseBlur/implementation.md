# Writing Phase Blur — Implementation

## Files Modified

- `src/Services/Reflekt/Reflekt.API/Features/Cards/ListCards/ListCardsEndpoint.cs` — During Writing phase, redacts `Text` to `""`, `AssigneeName` to `null`, and `EvidenceLinks` to `[]` for cards not owned by the requesting participant. SM (cookie auth, no participantId claim) sees all cards unredacted.
- `web/reflekt-web/src/components/CardItem.vue` — Added `blurred` prop. When true, renders a blurred placeholder ("Someone's note") with Tailwind `blur-md` + `select-none` + invisible overlay div. Hides evidence links, voting, attach-link UI, and edit/delete controls.
- `web/reflekt-web/src/views/RetroRoomView.vue` — Passes `:blurred="isWritingPhase && card.participantId !== currentParticipantId"` to all three CardItem columns. Added `currentParticipantId` and `isWritingPhase` computed properties.
- `web/reflekt-web/src/composables/useSprintHub.ts` — On `PhaseChanged`, if leaving Writing phase, triggers `cardStore.fetchCards(sprintId)` to replace backend-redacted text with real content.

## Key Decisions

- Defense-in-depth: backend redacts on the list endpoint (initial load), frontend blurs for real-time SignalR pushes.
- SignalR broadcasts still carry full text to avoid per-connection splitting complexity. Frontend blur covers this path. Acceptable v1 tradeoff.
- Phase transition triggers a full card re-fetch so redacted placeholders are replaced with real text from the now-unredacted API response.
- Placeholder text is static ("Someone's note") rather than variable-length to avoid leaking card content length.

## Deviations from Plan

- None (no formal plan — solo fix).
