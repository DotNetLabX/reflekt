using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.FocusGroup;

public sealed record FocusGroupRequest(int SprintId, int GroupId);

public sealed record FocusGroupResponse(int FocusedGroupId, int? PreviousGroupId, bool? PreviousGroupDiscussed);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/sprints/{sprintId}/card-groups/{groupId}/focus")]
[Tags("CardGroups")]
public sealed class FocusGroupEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<FocusGroupRequest, FocusGroupResponse>
{
    public override async Task HandleAsync(FocusGroupRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await SendNotFoundAsync(ct); return; }

        var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await SendForbiddenAsync(ct);
            return;
        }

        if (sprint.State != SprintState.Active)
        {
            AddError("Sprint must be Active.");
            await SendErrorsAsync(409, ct);
            return;
        }

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.FocusGroup))
            ThrowError($"{SprintAction.FocusGroup} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        var targetGroup = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (targetGroup is null || targetGroup.SprintId != req.SprintId) { await SendNotFoundAsync(ct); return; }

        int? previousGroupId = sprint.FocusedGroupId;
        bool? previousGroupDiscussed = null;

        if (previousGroupId.HasValue && previousGroupId.Value != req.GroupId)
        {
            var previousGroup = await cardGroupRepository.GetByIdAsync(previousGroupId.Value, ct);
            if (previousGroup is not null)
            {
                previousGroup.MarkDiscussed();
                previousGroupDiscussed = true;
            }
        }

        sprint.SetFocus(req.GroupId);
        await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(new FocusGroupResponse(req.GroupId, previousGroupId, previousGroupDiscussed), ct);
    }
}
