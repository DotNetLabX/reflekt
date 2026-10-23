using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.FocusGroup;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPost("/api/sprints/{sprintId}/card-groups/{groupId}/focus")]
[Tags("CardGroups")]
public sealed class FocusGroupEndpoint(
    SprintAccess sprintAccess,
    SprintRepository sprintRepository,
    CardGroupRepository cardGroupRepository,
    FocusHandover focusHandover,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<FocusGroupRequest, FocusGroupResponse>
{
    public override async Task HandleAsync(FocusGroupRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        access.RequireOwner();

        sprint.EnsureActive("Sprint must be Active.");

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.FocusGroup);

        var targetGroup = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (targetGroup is null || targetGroup.SprintId != req.SprintId)
            throw new NotFoundException("CardGroup not found");

        var handover = await focusHandover.FocusAsync(sprint, req.GroupId, ct);
        await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(
            new FocusGroupResponse(req.GroupId, handover.PreviousGroupId, handover.PreviousGroupDiscussed), ct);
    }
}
