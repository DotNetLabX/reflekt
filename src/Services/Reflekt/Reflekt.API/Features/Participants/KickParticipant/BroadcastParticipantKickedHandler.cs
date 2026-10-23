using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Participants.Events;

namespace Reflekt.API.Features.Participants.KickParticipant;

public sealed class BroadcastParticipantKickedHandler(IHubContext<SprintHub> hubContext, ReadinessTracker readinessTracker)
    : IEventHandler<ParticipantKickedEvent>
{
    public async Task HandleAsync(ParticipantKickedEvent ev, CancellationToken ct)
    {
        var sprintId = ev.Participant.SprintId;
        var participantId = ev.Participant.Id;
        var failures = new List<Exception>();

        try
        {
            await hubContext.Clients.Group($"sprint:{sprintId}")
                .SendAsync("ParticipantKicked", new { participantId }, ct);
        }
        catch (Exception ex)
        {
            failures.Add(ex);
        }
        finally
        {
            await RevokeAsync(sprintId, participantId, failures);
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }

    private async Task RevokeAsync(int sprintId, int participantId, List<Exception> failures)
    {
        var connections = readinessTracker.GetParticipantConnections(sprintId, participantId);

        foreach (var connection in connections)
        {
            await RunAsync(() => hubContext.Groups.RemoveFromGroupAsync(
                connection.ConnectionId, $"sprint:{sprintId}", CancellationToken.None), failures);
            await RunAsync(() => hubContext.Groups.RemoveFromGroupAsync(
                connection.ConnectionId, SprintHub.ParticipantGroup(participantId), CancellationToken.None), failures);
        }

        await RunAsync(() =>
        {
            AbortConnections(connections);
            return Task.CompletedTask;
        }, failures);

        var counts = new ReadinessCounts(0, 0);
        await RunAsync(() =>
        {
            counts = readinessTracker.RemoveParticipantState(sprintId, participantId);
            return Task.CompletedTask;
        }, failures);

        await RunAsync(() => hubContext.Clients.Group($"sprint:{sprintId}").SendAsync("ReadinessChanged", new
        {
            participantId,
            displayName = (string?)null,
            isReady = false,
            readyCount = counts.ReadyCount,
            totalCount = counts.TotalCount,
        }, CancellationToken.None), failures);
    }

    private static void AbortConnections(IReadOnlyList<HubCallerContext> connections)
    {
        var failures = new List<Exception>();
        foreach (var connection in connections)
        {
            try
            {
                connection.Abort();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }

        if (failures.Count > 0)
            throw new AggregateException(failures);
    }

    private static async Task RunAsync(Func<Task> revocationStep, List<Exception> failures)
    {
        try
        {
            await revocationStep();
        }
        catch (Exception ex)
        {
            failures.Add(ex);
        }
    }
}
