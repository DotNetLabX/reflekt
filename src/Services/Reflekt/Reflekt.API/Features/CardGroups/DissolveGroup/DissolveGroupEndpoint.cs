using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.DissolveGroup;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpDelete("/api/sprints/{sprintId}/card-groups/{groupId}")]
[Tags("CardGroups")]
public sealed class DissolveGroupEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<DissolveGroupRequest>
{
    public override async Task HandleAsync(DissolveGroupRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        access.RequireOwner();

        sprint.EnsureOpen();

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.UngroupCards);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId)
            throw new NotFoundException("CardGroup not found");

        var groupCards = (await cardRepository.GetBySprintIdAsync(req.SprintId, ct))
            .Where(c => c.GroupId == req.GroupId)
            .ToList();

        group.EnsureCanDissolve(groupCards.Count);

        await using var transaction = await cardGroupRepository.BeginTransactionAsync(ct);

        var soloGroups = groupCards.Select(c => CardGroup.CreateSolo(req.SprintId, c.ColumnType)).ToList();
        await cardGroupRepository.AddRangeAsync(soloGroups, ct);
        await cardGroupRepository.SaveChangesAsync(ct);

        for (var i = 0; i < groupCards.Count; i++)
            groupCards[i].SetGroupId(soloGroups[i].Id);

        group.Dissolve();
        cardGroupRepository.Remove(group);
        await cardGroupRepository.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await SendNoContentAsync(ct);
    }
}
