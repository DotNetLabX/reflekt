using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Votes;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Votes.CastVote;

public sealed record CastVoteRequest(int SprintId, int CardId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpPost("/api/sprints/{sprintId}/cards/{cardId}/votes")]
[Tags("Votes")]
public sealed class CastVoteEndpoint(
    SprintRepository sprintRepository,
    CardRepository cardRepository,
    VoteRepository voteRepository)
    : Endpoint<CastVoteRequest, VoteResponse>
{
    public override async Task HandleAsync(CastVoteRequest req, CancellationToken ct)
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

        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.SprintId != req.SprintId) { await SendNotFoundAsync(ct); return; }

        if (card.CardType == CardType.ActionItem)
            ThrowError("Cannot vote on action items.", StatusCodes.Status400BadRequest);

        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await SendNotFoundAsync(ct); return; }

        if (sprint.State == SprintState.Closed)
            ThrowError("Sprint is closed.", StatusCodes.Status409Conflict);

        var usedVotes = await voteRepository.CountByParticipantAsync(participantId, ct);
        if (usedVotes >= sprint.VotesPerParticipant)
            ThrowError("Vote budget exhausted.", StatusCodes.Status409Conflict);

        var vote = Vote.Create(req.CardId, participantId);
        await voteRepository.AddAsync(vote, ct);
        await voteRepository.SaveChangesAsync(ct);

        var voteCount = await voteRepository.CountByCardAsync(req.CardId, ct);
        var myVoteCount = await voteRepository.CountByParticipantAndCardAsync(participantId, req.CardId, ct);

        await SendAsync(new VoteResponse(req.CardId, voteCount, myVoteCount), 201, ct);
    }
}
