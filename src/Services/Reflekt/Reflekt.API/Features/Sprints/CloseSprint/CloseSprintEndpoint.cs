using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Sprints.CloseSprint;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/teams/{teamId}/sprints/{sprintId}/close")]
[Tags("Sprints")]
public sealed class CloseSprintEndpoint(TeamRepository teamRepository, SprintRepository sprintRepository)
    : Endpoint<SprintRouteRequest, SprintResponse>
{
    public override async Task HandleAsync(SprintRouteRequest request, CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null) { await SendNotFoundAsync(ct); return; }

        if (!string.Equals(team.CreatorEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null || sprint.TeamId != request.TeamId) { await SendNotFoundAsync(ct); return; }

        if (sprint.State != SprintState.Active)
        {
            AddError("Sprint must be Active to close.");
            await SendErrorsAsync(409, ct);
            return;
        }

        sprint.Close();
        await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(SprintMappings.ToResponse(sprint), ct);
    }
}
