# Kick Participant — Implementation

## Files Created
- `src/Services/Reflekt/Reflekt.Domain/Participants/Events/ParticipantKickedEvent.cs` — domain event raised when a participant is kicked
- `src/Services/Reflekt/Reflekt.API/Features/Participants/KickParticipant/KickParticipantEndpoint.cs` — DELETE endpoint, ScrumMaster-only, Active sprints only
- `src/Services/Reflekt/Reflekt.API/Features/Participants/KickParticipant/BroadcastParticipantKickedHandler.cs` — SignalR broadcast of ParticipantKicked to sprint group

## Files Modified
- `src/Services/Reflekt/Reflekt.Domain/Participants/Behaviors/Participant.cs` — added `Kick()` method that raises `ParticipantKickedEvent`
- `src/Services/Reflekt/Reflekt.Persistence/Repositories/ParticipantRepository.cs` — added `Remove()` method
- `web/reflekt-web/src/stores/participant.ts` — added `kickParticipant()` action
- `web/reflekt-web/src/composables/useSprintHub.ts` — added `ParticipantKicked` SignalR handler with `onKicked` callback; removes kicked participant from store and notifies the kicked client
- `web/reflekt-web/src/views/SprintView.vue` — added Participants section with kick buttons (Active sprints only), confirm dialog, fetch on mount
- `web/reflekt-web/src/views/RetroRoomView.vue` — added kicked state: clears guest token, disconnects SignalR, shows "You were removed" screen

## Key Decisions
- Route uses `DELETE /api/sprints/{sprintId}/participants/{participantId}` (no team prefix) to match existing participant endpoint patterns, deviating from the backlog spec which included `/teams/{teamId}/`
- Kick gated to Active sprints only — participants only exist in active retros
- SM self-kick is not prevented — cookie auth identity cannot be reliably linked to a specific participant row (participants don't store email), and self-kicking is harmless

## Deviations from Plan
- Route pattern simplified to match codebase conventions (see Key Decisions)
