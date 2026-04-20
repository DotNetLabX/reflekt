using System.Security.Claims;
using FastEndpoints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Teams;

public sealed record TeamSummary(int Id, string Name, DateTime CreatedAt);

public sealed class GetMyTeamsEndpoint(TeamRepository teamRepository)
    : EndpointWithoutRequest<List<TeamSummary>>
{
    public override void Configure()
    {
        Get("/api/teams");
        Policies(AuthPolicies.ScrumMaster);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)!;
        var teams = await teamRepository.GetByCreatorEmailAsync(email, ct);
        var result = teams.Select(t => new TeamSummary(t.Id, t.Name, t.CreatedAt)).ToList();
        await SendOkAsync(result, ct);
    }
}
