using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Sprints.Events;

namespace Reflekt.API.Events;

public sealed class PhaseAdvancedEventHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<PhaseAdvancedEvent>
{
    public Task HandleAsync(PhaseAdvancedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Sprint.Id}")
            .SendAsync("PhaseChanged", ev.Sprint.CurrentPhase.ToString(), ct);
}
