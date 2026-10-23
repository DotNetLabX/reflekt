using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;

namespace Reflekt.API.Features.Sprints.GetSprint;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpGet("/api/teams/{teamId}/sprints/{sprintId}")]
[Tags("Sprints")]
public sealed class GetSprintEndpoint(SprintAccess sprintAccess)
    : Endpoint<SprintRouteRequest, SprintResponse>
{
    public override async Task HandleAsync(SprintRouteRequest request, CancellationToken ct)
    {
        var access = await sprintAccess.ForTeamAsync(User, request.TeamId, request.SprintId, ct);
        access.RequireTeam();
        access.RequireOwner();
        var sprint = access.RequireSprint();

        await SendOkAsync(sprint.Adapt<SprintResponse>(), ct);
    }
}
