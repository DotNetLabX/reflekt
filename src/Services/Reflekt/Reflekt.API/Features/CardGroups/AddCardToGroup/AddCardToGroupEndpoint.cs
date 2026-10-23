using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.AddCardToGroup;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPost("/api/sprints/{sprintId}/card-groups/{groupId}/cards")]
[Tags("CardGroups")]
public sealed class AddCardToGroupEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    CardRegrouping cardRegrouping,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<AddCardToGroupRequest, CardGroupResponse>
{
    public override async Task HandleAsync(AddCardToGroupRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        access.RequireOwner();

        sprint.EnsureOpen();

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.GroupCards);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId)
            throw new NotFoundException("CardGroup not found");

        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.SprintId != req.SprintId)
            throw new NotFoundException("Card not found");

        group.EnsureCanAdd(card);

        var sourceGroupId = card.GroupId ?? 0;
        await cardRegrouping.MoveCardsAsync(req.SprintId, [card], group, ct);
        group.RecordCardAdded(req.CardId, sourceGroupId);

        await cardGroupRepository.SaveChangesAsync(ct);

        var sprintCards = await cardRepository.GetBySprintIdAsync(req.SprintId, ct);
        await SendOkAsync(CardGroupResponseBuilder.BuildFromSprintCards(group, sprintCards, sprint), ct);
    }
}
