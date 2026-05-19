using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Sprints.Timer;
using Reflekt.API.Hubs;
using Reflekt.Domain.Sprints.Events;

namespace Reflekt.API.Features.Sprints.AdvancePhase;

public sealed class BroadcastPhaseChangedHandler(IHubContext<SprintHub> hubContext, TimerService timerService)
    : IEventHandler<PhaseAdvancedEvent>
{
    public async Task HandleAsync(PhaseAdvancedEvent ev, CancellationToken ct)
    {
        var sprintId = ev.Sprint.Id;
        await hubContext.Clients.Group($"sprint:{sprintId}")
            .SendAsync("PhaseChanged", ev.Sprint.CurrentPhase.ToString(), ct);

        SprintHub.ClearReadiness(sprintId);
        await hubContext.Clients.Group($"sprint:{sprintId}")
            .SendAsync("ReadinessReset", new { }, ct);

        timerService.ClearTimer(sprintId);
        await hubContext.Clients.Group($"sprint:{sprintId}")
            .SendAsync("TimerUpdated", new
            {
                status = "stopped",
                remainingSeconds = 0,
                endTimeUtc = (DateTime?)null,
            }, ct);
    }
}
