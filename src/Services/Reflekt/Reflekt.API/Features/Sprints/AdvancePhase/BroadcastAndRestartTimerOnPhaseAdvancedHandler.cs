using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Sprints.Events;

namespace Reflekt.API.Features.Sprints.AdvancePhase;

public sealed class BroadcastAndRestartTimerOnPhaseAdvancedHandler(
    IHubContext<SprintHub> hubContext,
    TimerService timerService,
    ReadinessTracker readinessTracker)
    : IEventHandler<PhaseAdvancedEvent>
{
    public async Task HandleAsync(PhaseAdvancedEvent ev, CancellationToken ct)
    {
        var sprintId = ev.Sprint.Id;
        await hubContext.Clients.Group($"sprint:{sprintId}")
            .SendAsync("PhaseChanged", ev.Sprint.CurrentPhase.ToString(), ct);

        readinessTracker.ClearReadiness(sprintId);
        await hubContext.Clients.Group($"sprint:{sprintId}")
            .SendAsync("ReadinessReset", new { }, ct);

        timerService.ClearTimer(sprintId);

        var snapshot = ev.Sprint.HasTimer
            ? timerService.Start(sprintId, ev.Sprint.GetTimerDefaultForPhase() * 60)
            : TimerSnapshot.Stopped;

        await hubContext.Clients.Group($"sprint:{sprintId}")
            .SendAsync("TimerUpdated", snapshot.ToPayload(), ct);
    }
}
