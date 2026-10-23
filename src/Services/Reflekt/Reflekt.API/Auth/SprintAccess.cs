using System.Security.Claims;
using Blocks.Core;
using Blocks.Exceptions;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Teams;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Auth;

public sealed record SprintAccessResult(
    Sprint? Sprint,
    Team? Team,
    int? UserId,
    int? ParticipantId,
    int? ClaimSprintId)
{
    public bool IsGuest => ParticipantId is not null;

    public bool IsOwner => IsOwnedBy(UserId);

    public bool IsOwnedBy(int? userId)
        => Team is not null && userId is not null && Team.OwnerId == userId;

    public Sprint RequireSprint() => Guard.NotFound(Sprint);

    public Team RequireTeam() => Guard.NotFound(Team);

    public void RequireOwner()
    {
        if (!IsOwner)
            throw new ForbiddenException("You do not own this team.");
    }

    public void RequireGuestOf(int sprintId)
    {
        if (ClaimSprintId != sprintId)
            throw new UnauthorizedException("Not authorized for this sprint.");
    }

    public int RequireParticipant()
        => ParticipantId ?? throw new UnauthorizedException("Missing participant claims.");
}

public sealed class SprintAccess(SprintRepository sprintRepository, TeamRepository teamRepository)
{
    public async Task<SprintAccessResult> ForSprintAsync(ClaimsPrincipal user, int sprintId, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(sprintId, ct);
        var team = sprint is null ? null : await teamRepository.GetByIdAsync(sprint.TeamId, ct);
        return BuildResult(user, sprint, team);
    }

    public async Task<SprintAccessResult> ForTeamAsync(
        ClaimsPrincipal user, int teamId, int? sprintId, CancellationToken ct)
    {
        var team = await teamRepository.GetByIdAsync(teamId, ct);
        var sprint = sprintId is null ? null : await sprintRepository.GetByIdAsync(sprintId.Value, ct);
        return BuildResult(user, sprint?.TeamId == teamId ? sprint : null, team);
    }

    private static SprintAccessResult BuildResult(ClaimsPrincipal user, Sprint? sprint, Team? team)
        => new(sprint, team, FindUserId(user), user.GetParticipantId(), user.GetSprintId());

    private static int? FindUserId(ClaimsPrincipal user)
        => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
}
