using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Sprints.GetSprint;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpGet("/api/teams/{teamId}/sprints/{sprintId}")]
[Tags("Sprints")]
public sealed class GetSprintEndpoint(TeamRepository teamRepository, SprintRepository sprintRepository)
    : Endpoint<SprintRouteRequest, SprintResponse>
{
    public override async Task HandleAsync(SprintRouteRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null) { await SendNotFoundAsync(ct); return; }

        if (team.OwnerId != userId)
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null || sprint.TeamId != request.TeamId) { await SendNotFoundAsync(ct); return; }

        await SendOkAsync(sprint.Adapt<SprintResponse>(), ct);
    }
}
