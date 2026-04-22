using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Votes.GetMyVotes;

public sealed record GetMyVotesRequest(int SprintId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpGet("/api/sprints/{sprintId}/votes/mine")]
[Tags("Votes")]
public sealed class GetMyVotesEndpoint(
    SprintRepository sprintRepository,
    VoteRepository voteRepository)
    : Endpoint<GetMyVotesRequest, List<VoteDistributionEntry>>
{
    public override async Task HandleAsync(GetMyVotesRequest req, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("sprintId"), out var claimSprintId) || claimSprintId != req.SprintId)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        if (!int.TryParse(User.FindFirstValue("participantId"), out var participantId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await SendNotFoundAsync(ct); return; }

        var distribution = await voteRepository.GetVoteDistributionAsync(participantId, ct);
        await SendOkAsync(distribution.Select(e => new VoteDistributionEntry(e.GroupId, e.Count)).ToList(), ct);
    }
}
