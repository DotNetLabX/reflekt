using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Reflekt.API.Hubs;

public readonly record struct ReadinessCounts(int ReadyCount, int TotalCount);

public readonly record struct ReadinessToggle(bool IsReady, int ReadyCount, int TotalCount);

public sealed record ReadinessSnapshot(IReadOnlyList<int> ReadyParticipantIds, int ReadyCount, int TotalCount);

public sealed class ReadinessTracker
{
    // sprintId -> participantId -> displayName (ready participants)
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, string>> _readiness = new();

    // sprintId -> connectionId -> participantId (all connected participants)
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, int>> _connections = new();

    // connectionId -> caller; cleared only on disconnect, so a connection that left the sprint is still found
    private readonly ConcurrentDictionary<string, RegisteredConnection> _registry = new();

    private sealed record RegisteredConnection(HubCallerContext Caller, int SprintId, int ParticipantId);

    public void RegisterConnection(HubCallerContext caller, int sprintId, int participantId)
    {
        _registry[caller.ConnectionId] = new RegisteredConnection(caller, sprintId, participantId);
        SprintConnections(sprintId)[caller.ConnectionId] = participantId;
    }

    public void UnregisterConnection(int sprintId, string connectionId)
    {
        SprintConnections(sprintId).TryRemove(connectionId, out _);
        _registry.TryRemove(connectionId, out _);
    }

    public void LeaveSprint(int sprintId, string connectionId)
        => _connections.GetValueOrDefault(sprintId)?.TryRemove(connectionId, out _);

    public ReadinessSnapshot GetSnapshot(int sprintId)
    {
        var ready = SprintReadiness(sprintId);
        var totalCount = SprintConnections(sprintId).Values.Distinct().Count();
        return new ReadinessSnapshot(ready.Keys.ToList(), ready.Count, totalCount);
    }

    public ReadinessToggle ToggleReady(int sprintId, int participantId, string displayName)
    {
        var ready = SprintReadiness(sprintId);
        bool isReady;

        if (ready.ContainsKey(participantId))
        {
            ready.TryRemove(participantId, out _);
            isReady = false;
        }
        else
        {
            ready[participantId] = displayName;
            isReady = true;
        }

        var totalCount = SprintConnections(sprintId).Values.Distinct().Count();
        return new ReadinessToggle(isReady, ready.Count, totalCount);
    }

    public void ClearReadiness(int sprintId)
    {
        if (_readiness.TryGetValue(sprintId, out var ready))
            ready.Clear();
    }

    public IReadOnlyList<HubCallerContext> GetParticipantConnections(int sprintId, int participantId)
        => _registry.Values
            .Where(c => c.SprintId == sprintId && c.ParticipantId == participantId)
            .Select(c => c.Caller)
            .ToList();

    public ReadinessCounts RemoveParticipantState(int sprintId, int participantId)
    {
        var connections = _connections.GetValueOrDefault(sprintId);
        if (connections is not null)
        {
            foreach (var connectionId in connections.Where(c => c.Value == participantId).Select(c => c.Key).ToList())
                connections.TryRemove(connectionId, out _);
        }

        var ready = _readiness.GetValueOrDefault(sprintId);
        ready?.TryRemove(participantId, out _);

        return new ReadinessCounts(ready?.Count ?? 0, connections?.Values.Distinct().Count() ?? 0);
    }

    public void Forget(string connectionId)
        => _registry.TryRemove(connectionId, out _);

    public ReadinessCounts? Disconnect(int sprintId, string connectionId, int participantId)
    {
        var connections = _connections.GetValueOrDefault(sprintId);
        connections?.TryRemove(connectionId, out _);

        var stillConnected = connections?.Values.Contains(participantId) ?? false;
        if (stillConnected)
            return null;

        var ready = _readiness.GetValueOrDefault(sprintId);
        if (ready is null || !ready.TryRemove(participantId, out _))
            return null;

        return new ReadinessCounts(ready.Count, connections?.Values.Distinct().Count() ?? 0);
    }

    private ConcurrentDictionary<int, string> SprintReadiness(int sprintId)
        => _readiness.GetOrAdd(sprintId, _ => new ConcurrentDictionary<int, string>());

    private ConcurrentDictionary<string, int> SprintConnections(int sprintId)
        => _connections.GetOrAdd(sprintId, _ => new ConcurrentDictionary<string, int>());
}
