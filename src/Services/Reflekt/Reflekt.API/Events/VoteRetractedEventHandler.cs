using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Votes.Events;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Events;

public sealed class VoteRetractedEventHandler(
    IHubContext<SprintHub> hubContext,
    CardRepository cardRepository,
    VoteRepository voteRepository)
    : IEventHandler<VoteRetractedEvent>
{
    public async Task HandleAsync(VoteRetractedEvent ev, CancellationToken ct)
    {
        var cardId = ev.Vote.CardId;
        var card = await cardRepository.GetByIdAsync(cardId, ct);
        if (card is null) return;

        var newCount = await voteRepository.CountByCardAsync(cardId, ct);
        await hubContext.Clients.Group($"sprint:{card.SprintId}")
            .SendAsync("VoteRetracted", new { cardId, newCount }, ct);
    }
}
