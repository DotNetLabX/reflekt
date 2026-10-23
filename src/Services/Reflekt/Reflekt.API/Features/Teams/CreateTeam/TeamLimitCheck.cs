using Blocks.Exceptions;
using Microsoft.Extensions.Options;
using Reflekt.API.Middleware;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Teams.CreateTeam;

public sealed class TeamLimitCheck(TeamRepository teamRepository, IOptions<TeamSettings> teamSettings)
{
    public async Task EnsureBelowLimitAsync(int ownerId, CancellationToken ct)
    {
        var ownedTeamCount = await teamRepository.CountByOwnerIdAsync(ownerId, ct);
        if (ownedTeamCount >= teamSettings.Value.MaxTeamsPerUser)
            throw new ConflictException(
                "Team limit reached. Delete a team to create a new one.", ErrorCodes.LimitReached);
    }
}
