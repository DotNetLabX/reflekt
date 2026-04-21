using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Sprints.Events;

namespace Reflekt.API.Features.Sprints.AdvancePhase;

public sealed class BroadcastPhaseChangedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<PhaseAdvancedEvent>
{
    public Task HandleAsync(PhaseAdvancedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Sprint.Id}")
            .SendAsync("PhaseChanged", ev.Sprint.CurrentPhase.ToString(), ct);
}
