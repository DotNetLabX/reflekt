using System.Security.Claims;
using FastEndpoints;
using Microsoft.Extensions.Options;
using Reflekt.API.Auth;
using Reflekt.Domain.Teams;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Teams;

public sealed record CreateTeamRequest(string Name);
public sealed record CreateTeamResponse(int Id, string Name, DateTime CreatedAt);

public sealed class CreateTeamEndpoint(TeamRepository teamRepository, IOptions<AuthOptions> authOptions)
    : Endpoint<CreateTeamRequest, CreateTeamResponse>
{
    public override void Configure()
    {
        Post("/api/teams");
        Policies(AuthPolicies.ScrumMaster);
    }

    public override async Task HandleAsync(CreateTeamRequest req, CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (!AllowlistChecker.IsAllowed(email, authOptions.Value.AllowedEmails))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        if (await teamRepository.ExistsByCreatorEmailAsync(email!, ct))
        {
            AddError("You already have a team");
            await SendErrorsAsync(409, ct);
            return;
        }

        var team = Team.Create(req.Name, email!);
        await teamRepository.AddAsync(team, ct);
        await teamRepository.SaveChangesAsync(ct);

        await SendAsync(new CreateTeamResponse(team.Id, team.Name, team.CreatedAt), 201, ct);
    }
}
