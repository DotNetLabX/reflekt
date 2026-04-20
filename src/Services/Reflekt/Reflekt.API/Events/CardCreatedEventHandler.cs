using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Cards;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Events;

public sealed class CardCreatedEventHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<CardCreatedEvent>
{
    public Task HandleAsync(CardCreatedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Card.SprintId}")
            .SendAsync("CardCreated", CardMappings.ToResponse(ev.Card), ct);
}
