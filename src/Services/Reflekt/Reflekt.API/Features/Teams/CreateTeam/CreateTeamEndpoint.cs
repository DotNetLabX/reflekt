using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Domain.Teams;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Teams.CreateTeam;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/teams")]
[Tags("Teams")]
public sealed class CreateTeamEndpoint(TeamRepository teamRepository)
    : Endpoint<CreateTeamCommand, CreateTeamResponse>
{
    public override async Task HandleAsync(CreateTeamCommand command, CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (await teamRepository.ExistsByCreatorEmailAsync(email!, ct))
        {
            AddError("You already have a team");
            await SendErrorsAsync(409, ct);
            return;
        }

        var team = Team.Create(command.Name, email!);
        await teamRepository.AddAsync(team, ct);
        await teamRepository.SaveChangesAsync(ct);

        await SendAsync(new CreateTeamResponse(team.Id, team.Name, team.CreatedAt), 201, ct);
    }
}
