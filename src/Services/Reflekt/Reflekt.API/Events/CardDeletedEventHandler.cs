using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Events;

public sealed class CardDeletedEventHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<CardDeletedEvent>
{
    public Task HandleAsync(CardDeletedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Card.SprintId}")
            .SendAsync("CardDeleted", new { id = ev.Card.Id }, ct);
}
