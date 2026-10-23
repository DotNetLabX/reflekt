using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Middleware;
using Reflekt.Domain.Teams;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Teams.CreateTeam;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPost("/api/teams")]
[Tags("Teams")]
public sealed class CreateTeamEndpoint(
    TeamRepository teamRepository,
    TeamLimitCheck teamLimitCheck)
    : Endpoint<CreateTeamCommand, CreateTeamResponse>
{
    public override async Task HandleAsync(CreateTeamCommand command, CancellationToken ct)
    {
        var userId = User.GetUserId();

        await teamLimitCheck.EnsureBelowLimitAsync(userId, ct);

        if (await teamRepository.ExistsByOwnerAndNameAsync(userId, command.Name, ct))
            throw new ConflictException(
                $"You already have a team called '{command.Name}'.", ErrorCodes.DuplicateName);

        var team = Team.Create(command.Name, userId);
        await teamRepository.AddAsync(team, ct);
        await teamRepository.SaveChangesAsync(ct);

        await SendAsync(new CreateTeamResponse(team.Id, team.Name, team.CreatedAt), 201, ct);
    }
}
