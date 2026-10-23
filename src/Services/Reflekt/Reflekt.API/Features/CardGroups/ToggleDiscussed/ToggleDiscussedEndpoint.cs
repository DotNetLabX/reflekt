using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.ToggleDiscussed;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPost("/api/sprints/{sprintId}/card-groups/{groupId}/toggle-discussed")]
[Tags("CardGroups")]
public sealed class ToggleDiscussedEndpoint(
    SprintAccess sprintAccess,
    CardGroupRepository cardGroupRepository,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<ToggleDiscussedRequest, ToggleDiscussedResponse>
{
    public override async Task HandleAsync(ToggleDiscussedRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        access.RequireOwner();

        sprint.EnsureActive("Sprint must be Active.");

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.ToggleDiscussed);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId)
            throw new NotFoundException("CardGroup not found");

        if (group.DiscussedAt.HasValue)
            group.UnmarkDiscussed();
        else
            group.MarkDiscussed();

        await cardGroupRepository.SaveChangesAsync(ct);

        await SendOkAsync(new ToggleDiscussedResponse(group.Id, group.DiscussedAt.HasValue, group.DiscussedAt), ct);
    }
}
