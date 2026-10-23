using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Cards;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.EvidenceLinks.DetachEvidenceLink;

public sealed class BroadcastCardUpdatedOnEvidenceLinkDetachedHandler(IHubContext<SprintHub> hubContext, IServiceScopeFactory scopeFactory)
    : IEventHandler<EvidenceLinkDetachedEvent>
{
    public Task HandleAsync(EvidenceLinkDetachedEvent ev, CancellationToken ct)
        => CardBroadcast.BroadcastAsync(hubContext, scopeFactory, ev.Card, "CardUpdated", ct);
}
