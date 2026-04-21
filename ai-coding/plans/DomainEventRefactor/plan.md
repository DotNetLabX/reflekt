# Domain Event Handler Refactor

## Context

All 10 domain event handlers in the Reflekt service live in a flat `Reflekt.API/Events/` folder with namespace `Reflekt.API.Events`. This breaks the vertical slice pattern — every other piece of a feature (endpoint, command, validator) is co-located in `Features/{Domain}/{Action}/`, but the handlers that react to those actions are segregated into a cross-cutting folder.

Handler names echo the event name (`CardCreatedEventHandler`) instead of describing their effect. The name should tell you what the handler does without opening the file.

Additionally, there is no skill for creating domain event handlers. The `create-aggregate` skill covers creating the event *record* but not the handler. A standalone skill is needed so future handlers are placed correctly from the start.

## Scope

**In scope:**
- Move all 10 event handlers from `Reflekt.API/Events/` into their respective feature folders
- Rename each handler to describe its effect
- Update namespaces to match new locations
- Create a new `create-domain-event-handler` skill
- Delete the now-empty `Events/` folder

**Out of scope:**
- Changing handler logic or signatures
- Modifying domain event records (they stay in `{Svc}.Domain/{Aggregate}/Events/`)
- Changing the event dispatch mechanism (`DomainEventPublishingInterceptor`)
- Refactoring the `OpenGraph/` background service

## Domain Model Changes

None. This is a pure file-move + rename refactor with no behavior changes.

## Data Model Changes

None.

## Handler Naming Convention

Handler names describe their effect: `{EffectDescription}Handler` or `{EffectDescription}On{EventName}Handler`.

- When the effect matches the event name (e.g., broadcasting "CardCreated" for `CardCreatedEvent`), use the shorter form: `Broadcast{EventName}Handler`.
- When the effect differs from the event name (e.g., broadcasting "CardUpdated" for `EvidenceLinkDetachedEvent`), use the full form: `{EffectDescription}On{EventName}Handler`.
- When a handler has multiple effects, describe both: `BroadcastAndFetchOn{EventName}Handler`.

## Implementation Steps

### Step 1: Move and rename Card event handlers

Move 3 files from `Reflekt.API/Events/` into their feature folders. Rename each file and class to describe its effect. Update namespace.

| Old File | New File | New Class Name | Location |
|----------|----------|---------------|----------|
| `CardCreatedEventHandler.cs` | `BroadcastCardCreatedHandler.cs` | `BroadcastCardCreatedHandler` | `Features/Cards/CreateCard/` |
| `CardUpdatedEventHandler.cs` | `BroadcastCardUpdatedHandler.cs` | `BroadcastCardUpdatedHandler` | `Features/Cards/EditCard/` |
| `CardDeletedEventHandler.cs` | `BroadcastCardDeletedHandler.cs` | `BroadcastCardDeletedHandler` | `Features/Cards/DeleteCard/` |

For each file:
- Create the new file in the target feature folder with the new class name
- Update `namespace` to `Reflekt.API.Features.Cards.{Action}` (e.g., `Reflekt.API.Features.Cards.CreateCard`)
- Delete the old file from `Events/`
- Handler logic stays identical — only namespace, file name, and class name change

**Pattern reference:** Existing files in `Features/Cards/CreateCard/` — `CreateCardEndpoint.cs`, `CreateCardCommand.cs`

### Step 2: Move and rename Vote event handlers

Move 2 files. Rename, update namespaces.

| Old File | New File | New Class Name | Location |
|----------|----------|---------------|----------|
| `VoteCastEventHandler.cs` | `BroadcastVoteCountOnVoteCastHandler.cs` | `BroadcastVoteCountOnVoteCastHandler` | `Features/Votes/CastVote/` |
| `VoteRetractedEventHandler.cs` | `BroadcastVoteCountOnVoteRetractedHandler.cs` | `BroadcastVoteCountOnVoteRetractedHandler` | `Features/Votes/RetractVote/` |

Namespace changes:
- `BroadcastVoteCountOnVoteCastHandler` → `Reflekt.API.Features.Votes.CastVote`
- `BroadcastVoteCountOnVoteRetractedHandler` → `Reflekt.API.Features.Votes.RetractVote`

Note: These handlers query the vote count before broadcasting, so `BroadcastVoteCount` accurately describes the payload — not just echoing the event.

### Step 3: Move and rename Participant and Sprint event handlers

Move 2 files. Rename, update namespaces.

| Old File | New File | New Class Name | Location |
|----------|----------|---------------|----------|
| `ParticipantJoinedEventHandler.cs` | `BroadcastParticipantJoinedHandler.cs` | `BroadcastParticipantJoinedHandler` | `Features/Participants/JoinSprint/` |
| `PhaseAdvancedEventHandler.cs` | `BroadcastPhaseChangedHandler.cs` | `BroadcastPhaseChangedHandler` | `Features/Sprints/AdvancePhase/` |

Namespace changes:
- `BroadcastParticipantJoinedHandler` → `Reflekt.API.Features.Participants.JoinSprint`
- `BroadcastPhaseChangedHandler` → `Reflekt.API.Features.Sprints.AdvancePhase`

Note: `PhaseAdvancedEventHandler` broadcasts SignalR event "PhaseChanged" (not "PhaseAdvanced"), so the new name uses `PhaseChanged` to match the actual broadcast.

### Step 4: Move and rename EvidenceLink event handlers

Move 3 files. Create `FetchEvidenceLink/` folder for the background-triggered handler. Rename all three.

| Old File | New File | New Class Name | Location |
|----------|----------|---------------|----------|
| `EvidenceLinkAttachedEventHandler.cs` | `BroadcastAndFetchOnEvidenceLinkAttachedHandler.cs` | `BroadcastAndFetchOnEvidenceLinkAttachedHandler` | `Features/EvidenceLinks/AttachEvidenceLink/` |
| `EvidenceLinkDetachedEventHandler.cs` | `BroadcastCardUpdatedOnEvidenceLinkDetachedHandler.cs` | `BroadcastCardUpdatedOnEvidenceLinkDetachedHandler` | `Features/EvidenceLinks/DetachEvidenceLink/` |
| `EvidenceLinkFetchedEventHandler.cs` | `BroadcastEvidenceLinkFetchedHandler.cs` | `BroadcastEvidenceLinkFetchedHandler` | `Features/EvidenceLinks/FetchEvidenceLink/` (new folder) |

Namespace changes:
- `BroadcastAndFetchOnEvidenceLinkAttachedHandler` → `Reflekt.API.Features.EvidenceLinks.AttachEvidenceLink`
- `BroadcastCardUpdatedOnEvidenceLinkDetachedHandler` → `Reflekt.API.Features.EvidenceLinks.DetachEvidenceLink`
- `BroadcastEvidenceLinkFetchedHandler` → `Reflekt.API.Features.EvidenceLinks.FetchEvidenceLink`

Notes:
- `FetchEvidenceLink/` is a new folder — it represents the background OpenGraph fetch as a feature slice.
- `EvidenceLinkAttachedEventHandler` does two things: broadcasts "CardUpdated" AND enqueues an OpenGraph fetch job. The name `BroadcastAndFetch` captures both effects.
- `EvidenceLinkDetachedEventHandler` broadcasts "CardUpdated" (not "EvidenceLinkDetached"), so the full `On` form is used to show the mismatch between event and effect.

### Step 5: Delete the empty Events folder

After all 10 files are moved, delete `src/Services/Reflekt/Reflekt.API/Events/`. Verify no files remain.

### Step 6: Build verification

Run `dotnet build` from the solution root. The build must pass with zero errors — FastEndpoints auto-discovery finds `IEventHandler<T>` implementations regardless of namespace or class name.

### Step 7: Create the `create-domain-event-handler` skill

Create a new skill at `.claude/skills/create-domain-event-handler/SKILL.md` with a single workflow `Handler.md`.

**SKILL.md** should define:
- Name: `create-domain-event-handler`
- Description: Creates a domain event handler following the vertical slice pattern. Places the handler in the feature folder it serves. Names the handler by its effect.
- Steps: determine service and event, determine target feature folder, determine handler name from its effect, create handler file, verify build
- Arguments: event name (e.g., `CardCreatedEvent`)

**Handler.md workflow** should specify:

- **Location rule:** Handler lives in `{Service}.API/Features/{Domain}/{Feature}/` — the feature folder it *serves*, not necessarily the one that raised the event.
- **Naming rule:** Handler names describe their effect, not their trigger.
  - When effect matches event name: `Broadcast{EventName}Handler.cs` (short form).
  - When effect differs from event name: `{EffectDescription}On{EventName}Handler.cs` (full form).
  - Multiple effects: `{Effect1}And{Effect2}On{EventName}Handler.cs`.
- **Namespace:** Matches the folder path (e.g., `Reflekt.API.Features.Cards.CreateCard`).
- **Pattern — FastEndpoints variant (Reflekt, Auth, Journals):**
```csharp
public sealed class {HandlerName}({Dependencies})
    : IEventHandler<{EventName}>
{
    public async Task HandleAsync({EventName} ev, CancellationToken ct)
    {
        // side effect: SignalR broadcast, background job trigger, etc.
    }
}
```
- **Pattern — MediatR variant (Submission, Review, Production):**
```csharp
public sealed class {HandlerName}({Dependencies})
    : INotificationHandler<{EventName}>
{
    public async Task Handle({EventName} notification, CancellationToken ct)
    {
        // side effect
    }
}
```
- **Reference existing handler:** `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/BroadcastCardCreatedHandler.cs` (after this refactor completes)

### Step 8: Final build verification

Run `dotnet build` one more time to confirm everything still builds after all changes.

## Cross-Service Changes

None. This refactor is entirely within the Reflekt service. The skill applies cross-service but doesn't modify other services.

## Migration Notes

None. No database changes.

## Testing Strategy

- **Build pass:** `dotnet build` must succeed after steps 6 and 8.
- **Runtime verification:** Start the app, perform an action that triggers a domain event (e.g., create a card), confirm SignalR broadcast still fires. FastEndpoints auto-discovers `IEventHandler<T>` implementations regardless of namespace or class name — the move and rename should be transparent.
- **Skill verification:** The new skill files are markdown — verify they follow the existing skill format by comparing structure against `create-aggregate/SKILL.md` and its workflows.

## Open Questions

None — all decisions resolved during planning discussion.

## Design Decisions (for reference)

These decisions were made during planning and should be captured in the skill:

1. **Handler placement rule:** A handler lives with the feature it *serves*. For single-handler events, this is typically the feature that raises the event. For multi-handler events, each handler goes where its effect belongs.
2. **Naming convention:** Handler names describe their effect. Short form `Broadcast{EventName}Handler` when effect matches event. Full form `{EffectDescription}On{EventName}Handler` when they differ.
3. **Background-triggered events:** Treat as a feature slice (e.g., `FetchEvidenceLink/`). The handler still gets a feature folder home.
