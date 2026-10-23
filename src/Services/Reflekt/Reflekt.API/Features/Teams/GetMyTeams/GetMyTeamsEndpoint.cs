using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Teams.GetMyTeams;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpGet("/api/teams")]
[Tags("Teams")]
public sealed class GetMyTeamsEndpoint(TeamRepository teamRepository)
    : EndpointWithoutRequest<IReadOnlyList<TeamSummaryResponse>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var summaries = await teamRepository.GetTeamSummariesByOwnerAsync(userId, ct);
        await SendOkAsync(summaries, ct);
    }
}
