using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Auth;

namespace Reflekt.API.Hubs;

[Authorize(AuthenticationSchemes = GuestTokenDefaults.AuthenticationScheme)]
public class SprintHub(ILogger<SprintHub> logger) : Hub
{
    // sprintId -> participantId -> displayName (ready participants)
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<int, string>> ReadinessState = new();

    // sprintId -> connectionId -> participantId (all connected participants)
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, int>> ConnectionState = new();

    public override Task OnConnectedAsync()
    {
        logger.LogInformation("Client {ConnectionId} connected to SprintHub", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public async Task JoinSprint(int sprintId)
    {
        var claimSprintId = Context.User?.FindFirstValue("sprintId");
        if (!int.TryParse(claimSprintId, out var parsedSprintId) || parsedSprintId != sprintId)
            throw new HubException("Not authorized to join this sprint.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");

        // Track connection
        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        if (int.TryParse(participantIdClaim, out var participantId))
        {
            var connections = ConnectionState.GetOrAdd(sprintId, _ => new ConcurrentDictionary<string, int>());
            connections[Context.ConnectionId] = participantId;
        }

        // Send readiness snapshot to joining client
        var readyDict = ReadinessState.GetOrAdd(sprintId, _ => new ConcurrentDictionary<int, string>());
        var connections2 = ConnectionState.GetOrAdd(sprintId, _ => new ConcurrentDictionary<string, int>());
        var totalCount = connections2.Values.Distinct().Count();

        await Clients.Caller.SendAsync("ReadinessSnapshot", new
        {
            readyParticipantIds = readyDict.Keys.ToList(),
            readyCount = readyDict.Count,
            totalCount,
        });
    }

    public Task LeaveSprint(int sprintId)
    {
        var connections = ConnectionState.GetValueOrDefault(sprintId);
        connections?.TryRemove(Context.ConnectionId, out _);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");
    }

    public async Task ToggleReady(int sprintId)
    {
        var claimSprintId = Context.User?.FindFirstValue("sprintId");
        if (!int.TryParse(claimSprintId, out var parsedSprintId) || parsedSprintId != sprintId)
            throw new HubException("Not authorized for this sprint.");

        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        var displayNameClaim = Context.User?.FindFirstValue("displayName");
        if (!int.TryParse(participantIdClaim, out var participantId) || displayNameClaim is null)
            throw new HubException("Missing participant claims.");

        var readyDict = ReadinessState.GetOrAdd(sprintId, _ => new ConcurrentDictionary<int, string>());
        bool isReady;

        if (readyDict.ContainsKey(participantId))
        {
            readyDict.TryRemove(participantId, out _);
            isReady = false;
        }
        else
        {
            readyDict[participantId] = displayNameClaim;
            isReady = true;
        }

        var connections = ConnectionState.GetOrAdd(sprintId, _ => new ConcurrentDictionary<string, int>());
        var totalCount = connections.Values.Distinct().Count();

        await Clients.Group($"sprint:{sprintId}").SendAsync("ReadinessChanged", new
        {
            participantId,
            displayName = displayNameClaim,
            isReady,
            readyCount = readyDict.Count,
            totalCount,
        });
    }

    public static void ClearReadiness(int sprintId)
    {
        if (ReadinessState.TryGetValue(sprintId, out var readyDict))
            readyDict.Clear();
    }

    public static int GetParticipantCount(int sprintId)
    {
        if (ConnectionState.TryGetValue(sprintId, out var connections))
            return connections.Values.Distinct().Count();
        return 0;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        var sprintIdClaim = Context.User?.FindFirstValue("sprintId");

        if (int.TryParse(participantIdClaim, out var participantId) &&
            int.TryParse(sprintIdClaim, out var sprintId))
        {
            // Remove from connection tracking
            var connections = ConnectionState.GetValueOrDefault(sprintId);
            connections?.TryRemove(Context.ConnectionId, out _);

            // Remove from readiness if present and no other connections remain for this participant
            var stillConnected = connections?.Values.Contains(participantId) ?? false;
            if (!stillConnected)
            {
                var readyDict = ReadinessState.GetValueOrDefault(sprintId);
                if (readyDict is not null && readyDict.TryRemove(participantId, out _))
                {
                    var totalCount = connections?.Values.Distinct().Count() ?? 0;
                    await Clients.Group($"sprint:{sprintId}").SendAsync("ReadinessChanged", new
                    {
                        participantId,
                        displayName = (string?)null,
                        isReady = false,
                        readyCount = readyDict.Count,
                        totalCount,
                    });
                }
            }

            await Clients.Group($"sprint:{sprintId}")
                .SendAsync("ParticipantLeft", new { participantId });
        }

        await base.OnDisconnectedAsync(exception);
    }
}
