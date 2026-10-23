using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Sprints.GetTeamSprints;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpGet("/api/teams/{teamId}/sprints")]
[Tags("Sprints")]
public sealed class GetTeamSprintsEndpoint(SprintAccess sprintAccess, SprintRepository sprintRepository)
    : Endpoint<GetTeamSprintsRequest, IReadOnlyList<SprintResponse>>
{
    public override async Task HandleAsync(GetTeamSprintsRequest request, CancellationToken ct)
    {
        var access = await sprintAccess.ForTeamAsync(User, request.TeamId, sprintId: null, ct);
        access.RequireTeam();
        access.RequireOwner();

        var sprints = await sprintRepository.GetByTeamIdAsync(request.TeamId, ct);
        await SendOkAsync(sprints.Select(s => s.Adapt<SprintResponse>()).ToList(), ct);
    }
}
