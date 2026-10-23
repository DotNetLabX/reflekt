using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.UpdateGroupTitle;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPatch("/api/sprints/{sprintId}/card-groups/{groupId}")]
[Tags("CardGroups")]
public sealed class UpdateGroupTitleEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<UpdateGroupTitleRequest, CardGroupResponse>
{
    public override async Task HandleAsync(UpdateGroupTitleRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        access.RequireOwner();

        sprint.EnsureOpen();

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.RenameGroup);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId)
            throw new NotFoundException("CardGroup not found");

        group.SetTitle(req.Title);

        await cardGroupRepository.SaveChangesAsync(ct);

        var sprintCards = await cardRepository.GetBySprintIdAsync(req.SprintId, ct);
        await SendOkAsync(CardGroupResponseBuilder.BuildFromSprintCards(group, sprintCards, sprint), ct);
    }
}
