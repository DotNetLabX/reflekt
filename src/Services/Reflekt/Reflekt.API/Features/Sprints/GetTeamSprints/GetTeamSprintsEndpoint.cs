using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Sprints.GetTeamSprints;

public sealed record GetTeamSprintsRequest(int TeamId);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpGet("/api/teams/{teamId}/sprints")]
[Tags("Sprints")]
public sealed class GetTeamSprintsEndpoint(TeamRepository teamRepository, SprintRepository sprintRepository)
    : Endpoint<GetTeamSprintsRequest, List<SprintResponse>>
{
    public override async Task HandleAsync(GetTeamSprintsRequest request, CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null) { await SendNotFoundAsync(ct); return; }

        if (!string.Equals(team.CreatorEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var sprints = await sprintRepository.GetByTeamIdAsync(request.TeamId, ct);
        await SendOkAsync(sprints.Select(SprintMappings.ToResponse).ToList(), ct);
    }
}
