using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Votes.Events;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Votes.CastVote;

public sealed class BroadcastVoteCountOnVoteCastHandler(
    IHubContext<SprintHub> hubContext,
    CardRepository cardRepository,
    VoteRepository voteRepository)
    : IEventHandler<VoteCastEvent>
{
    public async Task HandleAsync(VoteCastEvent ev, CancellationToken ct)
    {
        var cardId = ev.Vote.CardId;
        var card = await cardRepository.GetByIdAsync(cardId, ct);
        if (card is null) return;

        var newCount = await voteRepository.CountByCardAsync(cardId, ct);
        await hubContext.Clients.Group($"sprint:{card.SprintId}")
            .SendAsync("VoteCast", new { cardId, newCount }, ct);
    }
}
