using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Teams.GetMyTeams;

public sealed record TeamSummary(int Id, string Name, DateTime CreatedAt);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpGet("/api/teams")]
[Tags("Teams")]
public sealed class GetMyTeamsEndpoint(TeamRepository teamRepository)
    : EndpointWithoutRequest<List<TeamSummary>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)!;
        var teams = await teamRepository.GetByCreatorEmailAsync(email, ct);
        var result = teams.Select(t => new TeamSummary(t.Id, t.Name, t.CreatedAt)).ToList();
        await SendOkAsync(result, ct);
    }
}
