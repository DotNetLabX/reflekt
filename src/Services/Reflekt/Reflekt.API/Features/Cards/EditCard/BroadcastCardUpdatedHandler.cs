using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.Cards.EditCard;

public sealed class BroadcastCardUpdatedHandler(IHubContext<SprintHub> hubContext, IServiceScopeFactory scopeFactory)
    : IEventHandler<CardUpdatedEvent>
{
    public Task HandleAsync(CardUpdatedEvent ev, CancellationToken ct)
        => CardBroadcast.BroadcastAsync(hubContext, scopeFactory, ev.Card, "CardUpdated", ct);
}
