using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Teams.DeleteTeam;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpDelete("/api/teams/{teamId}")]
[Tags("Teams")]
public sealed class DeleteTeamEndpoint(
    SprintAccess sprintAccess,
    TeamRepository teamRepository,
    SprintRepository sprintRepository)
    : Endpoint<DeleteTeamCommand, EmptyResponse>
{
    public override async Task HandleAsync(DeleteTeamCommand command, CancellationToken ct)
    {
        var access = await sprintAccess.ForTeamAsync(User, command.TeamId, sprintId: null, ct);
        var team = access.RequireTeam();
        access.RequireOwner();

        if (!string.Equals(team.Name, command.ConfirmationName, StringComparison.Ordinal))
            throw new BadRequestException("Confirmation name does not match the team name.");

        if (await sprintRepository.HasActiveSprintAsync(command.TeamId, ct))
            throw new ConflictException(
                "This team has an active sprint. Close or delete it before deleting the team.");

        await teamRepository.DeleteTeamWithCascadeAsync(command.TeamId, ct);

        await SendNoContentAsync(ct);
    }
}
