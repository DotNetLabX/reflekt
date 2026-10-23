using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.Cards.CreateCard;

public sealed class BroadcastCardCreatedHandler(IHubContext<SprintHub> hubContext, IServiceScopeFactory scopeFactory)
    : IEventHandler<CardCreatedEvent>
{
    public Task HandleAsync(CardCreatedEvent ev, CancellationToken ct)
        => CardBroadcast.BroadcastAsync(hubContext, scopeFactory, ev.Card, "CardCreated", ct);
}
