using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.RemoveCardFromGroup;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpDelete("/api/sprints/{sprintId}/card-groups/{groupId}/cards/{cardId}")]
[Tags("CardGroups")]
public sealed class RemoveCardFromGroupEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<RemoveCardFromGroupRequest>
{
    public override async Task HandleAsync(RemoveCardFromGroupRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        access.RequireOwner();

        sprint.EnsureOpen();

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.UngroupCards);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId)
            throw new NotFoundException("CardGroup not found");

        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.GroupId != req.GroupId)
            throw new NotFoundException("Card not found");

        await using var transaction = await cardGroupRepository.BeginTransactionAsync(ct);

        var soloGroup = CardGroup.CreateSolo(req.SprintId, card.ColumnType);
        await cardGroupRepository.AddAsync(soloGroup, ct);
        await cardGroupRepository.SaveChangesAsync(ct);

        card.SetGroupId(soloGroup.Id);
        group.RecordCardRemoved(req.CardId, soloGroup.Id);
        await cardGroupRepository.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await SendOkAsync(ct);
    }
}
