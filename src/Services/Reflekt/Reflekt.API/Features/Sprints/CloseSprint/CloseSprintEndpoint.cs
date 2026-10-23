using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Sprints.CloseSprint;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPost("/api/teams/{teamId}/sprints/{sprintId}/close")]
[Tags("Sprints")]
public sealed class CloseSprintEndpoint(SprintAccess sprintAccess, SprintRepository sprintRepository)
    : Endpoint<SprintRouteRequest, SprintResponse>
{
    public override async Task HandleAsync(SprintRouteRequest request, CancellationToken ct)
    {
        var access = await sprintAccess.ForTeamAsync(User, request.TeamId, request.SprintId, ct);
        access.RequireTeam();
        access.RequireOwner();
        var sprint = access.RequireSprint();

        sprint.Close();
        await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(sprint.Adapt<SprintResponse>(), ct);
    }
}
