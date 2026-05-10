using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Reflekt.API.Auth;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Teams.DeleteTeam;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpDelete("/api/teams/{teamId}")]
[Tags("Teams")]
public sealed class DeleteTeamEndpoint(TeamRepository teamRepository, SprintRepository sprintRepository)
    : Endpoint<DeleteTeamCommand, EmptyResponse>
{
    public override async Task HandleAsync(DeleteTeamCommand command, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var team = await teamRepository.GetByIdAsync(command.TeamId, ct);
        if (team is null) { await SendNotFoundAsync(ct); return; }

        if (team.OwnerId != userId)
        {
            await SendForbiddenAsync(ct);
            return;
        }

        if (!string.Equals(team.Name, command.ConfirmationName, StringComparison.Ordinal))
        {
            AddError("Confirmation name does not match the team name.");
            await SendErrorsAsync(400, ct);
            return;
        }

        var hasActiveSprint = await sprintRepository.Query()
            .AnyAsync(s => s.TeamId == command.TeamId && s.State == SprintState.Active, ct);

        if (hasActiveSprint)
        {
            AddError("This team has an active sprint. Close or delete it before deleting the team.");
            await SendErrorsAsync(409, ct);
            return;
        }

        await teamRepository.DeleteTeamWithCascadeAsync(command.TeamId, ct);

        await SendNoContentAsync(ct);
    }
}
