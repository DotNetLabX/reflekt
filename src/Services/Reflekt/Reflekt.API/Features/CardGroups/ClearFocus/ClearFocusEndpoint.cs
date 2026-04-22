using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.ClearFocus;

public sealed record ClearFocusRequest(int SprintId);

public sealed record ClearFocusResponse(int? PreviousGroupId, bool? PreviousGroupDiscussed);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpDelete("/api/sprints/{sprintId}/focus")]
[Tags("CardGroups")]
public sealed class ClearFocusEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<ClearFocusRequest, ClearFocusResponse>
{
    public override async Task HandleAsync(ClearFocusRequest req, CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await SendNotFoundAsync(ct); return; }

        var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
        if (team is null || !string.Equals(team.CreatorEmail, email, StringComparison.OrdinalIgnoreCase))
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

        int? previousGroupId = sprint.FocusedGroupId;
        bool? previousGroupDiscussed = null;

        if (previousGroupId.HasValue)
        {
            var previousGroup = await cardGroupRepository.GetByIdAsync(previousGroupId.Value, ct);
            if (previousGroup is not null)
            {
                previousGroup.MarkDiscussed();
                previousGroupDiscussed = true;
            }
        }

        sprint.ClearFocus();
        await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(new ClearFocusResponse(previousGroupId, previousGroupDiscussed), ct);
    }
}
