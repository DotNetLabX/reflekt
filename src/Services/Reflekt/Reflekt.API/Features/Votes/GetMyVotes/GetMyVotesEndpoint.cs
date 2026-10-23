using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Votes.GetMyVotes;

[Authorize(Policy = Auth.Policies.Guest)]
[HttpGet("/api/sprints/{sprintId}/votes/mine")]
[Tags("Votes")]
public sealed class GetMyVotesEndpoint(
    SprintAccess sprintAccess,
    VoteRepository voteRepository)
    : Endpoint<GetMyVotesRequest, IReadOnlyList<VoteDistributionResponse>>
{
    public override async Task HandleAsync(GetMyVotesRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        access.RequireGuestOf(req.SprintId);
        var participantId = access.RequireParticipant();
        access.RequireSprint();

        var distribution = await voteRepository.GetVoteDistributionAsync(participantId, ct);
        await SendOkAsync(distribution, ct);
    }
}
