using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Cards;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.EvidenceLinks.FetchEvidenceLink;

public sealed class BroadcastEvidenceLinkFetchedHandler(IHubContext<SprintHub> hubContext, IServiceScopeFactory scopeFactory)
    : IEventHandler<EvidenceLinkFetchedEvent>
{
    public async Task HandleAsync(EvidenceLinkFetchedEvent ev, CancellationToken ct)
    {
        var card = ev.Card;
        var payload = ev.EvidenceLink.Adapt<EvidenceLinkResponse>();

        var recipients = await CardBroadcast.IsWritingPhaseCardAsync(scopeFactory, card, ct)
            ? hubContext.Clients.Group(SprintHub.ParticipantGroup(card.ParticipantId))
            : hubContext.Clients.Group($"sprint:{card.SprintId}");

        await recipients.SendAsync("EvidenceLinkFetched", payload, ct);
    }
}
