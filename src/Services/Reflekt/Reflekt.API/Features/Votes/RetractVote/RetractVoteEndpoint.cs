using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Votes.RetractVote;

public sealed record RetractVoteRequest(int SprintId, int CardId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpDelete("/api/sprints/{sprintId}/cards/{cardId}/votes")]
[Tags("Votes")]
public sealed class RetractVoteEndpoint(
    SprintRepository sprintRepository,
    VoteRepository voteRepository)
    : Endpoint<RetractVoteRequest, VoteResponse>
{
    public override async Task HandleAsync(RetractVoteRequest req, CancellationToken ct)
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

        if (sprint.State == SprintState.Closed)
            ThrowError("Sprint is closed.", StatusCodes.Status409Conflict);

        var vote = await voteRepository.GetOneByParticipantAndCardAsync(participantId, req.CardId, ct);
        if (vote is null)
            ThrowError("No votes to retract on this card.", StatusCodes.Status409Conflict);

        vote.MarkRetracted();
        voteRepository.Remove(vote);
        await voteRepository.SaveChangesAsync(ct);

        var voteCount = await voteRepository.CountByCardAsync(req.CardId, ct);
        var myVoteCount = await voteRepository.CountByParticipantAndCardAsync(participantId, req.CardId, ct);

        await SendOkAsync(new VoteResponse(req.CardId, voteCount, myVoteCount), ct);
    }
}
