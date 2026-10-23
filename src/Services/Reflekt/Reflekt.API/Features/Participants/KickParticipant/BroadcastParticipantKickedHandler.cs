using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Participants.Events;

namespace Reflekt.API.Features.Participants.KickParticipant;

public sealed class BroadcastParticipantKickedHandler(IHubContext<SprintHub> hubContext)
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
        var connections = SprintHub.GetParticipantConnections(sprintId, participantId);

        foreach (var connection in connections)
        {
            await RunAsync(() => hubContext.Groups.RemoveFromGroupAsync(
                connection.ConnectionId, $"sprint:{sprintId}", CancellationToken.None), failures);
            await RunAsync(() => hubContext.Groups.RemoveFromGroupAsync(
                connection.ConnectionId, SprintHub.ParticipantGroup(participantId), CancellationToken.None), failures);
        }

        await RunAsync(() =>
        {
            SprintHub.AbortConnections(connections);
            return Task.CompletedTask;
        }, failures);

        var counts = (ReadyCount: 0, TotalCount: 0);
        await RunAsync(() =>
        {
            counts = SprintHub.RemoveParticipantState(sprintId, participantId);
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
