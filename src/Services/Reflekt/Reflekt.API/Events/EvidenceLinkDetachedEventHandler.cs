using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Cards;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Events;

public sealed class EvidenceLinkDetachedEventHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<EvidenceLinkDetachedEvent>
{
    public Task HandleAsync(EvidenceLinkDetachedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Card.SprintId}")
            .SendAsync("CardUpdated", CardMappings.ToResponse(ev.Card), ct);
}
