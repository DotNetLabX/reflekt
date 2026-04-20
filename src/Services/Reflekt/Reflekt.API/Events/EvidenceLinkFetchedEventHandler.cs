using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Cards;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Events;

public sealed class EvidenceLinkFetchedEventHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<EvidenceLinkFetchedEvent>
{
    public Task HandleAsync(EvidenceLinkFetchedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Card.SprintId}")
            .SendAsync("EvidenceLinkFetched", CardMappings.ToResponse(ev.EvidenceLink), ct);
}
