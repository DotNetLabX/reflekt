# Domain Event Handler Refactor — Done

## Files Created
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/BroadcastCardCreatedHandler.cs` — handles `CardCreatedEvent`, broadcasts via SignalR
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/BroadcastCardUpdatedHandler.cs` — handles `CardUpdatedEvent`, broadcasts via SignalR
- `src/Services/Reflekt/Reflekt.API/Features/Cards/DeleteCard/BroadcastCardDeletedHandler.cs` — handles `CardDeletedEvent`, broadcasts via SignalR
- `src/Services/Reflekt/Reflekt.API/Features/Votes/CastVote/BroadcastVoteCountOnVoteCastHandler.cs` — handles `VoteCastEvent`, broadcasts updated vote count
- `src/Services/Reflekt/Reflekt.API/Features/Votes/RetractVote/BroadcastVoteCountOnVoteRetractedHandler.cs` — handles `VoteRetractedEvent`, broadcasts updated vote count
- `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/BroadcastParticipantJoinedHandler.cs` — handles `ParticipantJoinedEvent`, broadcasts via SignalR
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/BroadcastPhaseChangedHandler.cs` — handles `PhaseAdvancedEvent`, broadcasts phase change
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/AttachEvidenceLink/BroadcastAndFetchOnEvidenceLinkAttachedHandler.cs` — handles `EvidenceLinkAttachedEvent`, broadcasts card update and enqueues OpenGraph fetch
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/DetachEvidenceLink/BroadcastCardUpdatedOnEvidenceLinkDetachedHandler.cs` — handles `EvidenceLinkDetachedEvent`, broadcasts card update
- `src/Services/Reflekt/Reflekt.API/Features/EvidenceLinks/FetchEvidenceLink/BroadcastEvidenceLinkFetchedHandler.cs` — handles `EvidenceLinkFetchedEvent`, broadcasts fetched link data (new folder created)
- `.claude/skills/create-domain-event-handler/SKILL.md` — skill definition with effect-based naming
- `.claude/skills/create-domain-event-handler/workflows/Handler.md` — handler workflow with location rules, effect-based naming convention, and FastEndpoints/MediatR patterns

## Files Deleted
- `src/Services/Reflekt/Reflekt.API/Events/` — entire folder deleted after all 10 handlers moved out
- 10 old handler files with `{EventName}Handler.cs` naming deleted after rename to `Broadcast*Handler.cs` naming

## Key Decisions
- Removed `using Reflekt.API.Features.Cards;` from Card handlers after moving them into child namespaces of `Reflekt.API.Features.Cards` — parent namespace types are visible without an explicit using in C#
- Kept `using Reflekt.API.Features.Cards;` on EvidenceLink handlers since they live in `Reflekt.API.Features.EvidenceLinks.*` and need explicit access to `CardMappings`
- Renamed all 10 handlers from `{EventName}Handler` to effect-based names (`Broadcast*Handler`, `BroadcastAndFetch*Handler`, etc.) per updated plan

## Deviations from Plan
- None. All steps followed exactly as specified across both rounds.

## Build Verification
- After file moves (Step 6): passed (0 warnings, 0 errors)
- After renames (Step 8): passed (0 warnings, 0 errors)
- After skill updates and fixes: passed (0 warnings, 0 errors)
