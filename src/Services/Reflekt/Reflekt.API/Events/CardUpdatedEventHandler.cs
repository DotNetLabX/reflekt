using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Cards;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Events;

public sealed class CardUpdatedEventHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<CardUpdatedEvent>
{
    public Task HandleAsync(CardUpdatedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Card.SprintId}")
            .SendAsync("CardUpdated", CardMappings.ToResponse(ev.Card), ct);
}
