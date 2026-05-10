using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Sprints.StartSprint;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/teams/{teamId}/sprints/{sprintId}/start")]
[Tags("Sprints")]
public sealed class StartSprintEndpoint(TeamRepository teamRepository, SprintRepository sprintRepository)
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

        if (sprint.State != SprintState.Draft)
        {
            AddError("Sprint must be in Draft state to start.");
            await SendErrorsAsync(409, ct);
            return;
        }

        sprint.Start();
        await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(sprint.Adapt<SprintResponse>(), ct);
    }
}
