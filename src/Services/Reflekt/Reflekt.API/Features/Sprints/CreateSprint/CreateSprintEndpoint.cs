using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Sprints.CreateSprint;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/teams/{teamId}/sprints")]
[Tags("Sprints")]
public sealed class CreateSprintEndpoint(TeamRepository teamRepository, SprintRepository sprintRepository)
    : Endpoint<CreateSprintCommand, SprintResponse>
{
    public override async Task HandleAsync(CreateSprintCommand command, CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        var team = await teamRepository.GetByIdAsync(command.TeamId, ct);
        if (team is null) { await SendNotFoundAsync(ct); return; }

        if (!string.Equals(team.CreatorEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var sprint = Sprint.Create(command.Name, command.TeamId, command.VotesPerParticipant ?? 3);
        await sprintRepository.AddAsync(sprint, ct);
        await sprintRepository.SaveChangesAsync(ct);

        await SendAsync(sprint.Adapt<SprintResponse>(), 201, ct);
    }
}
