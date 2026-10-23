namespace Reflekt.Domain.Sprints;

public sealed class PhaseActionPolicy
{
    private readonly List<(SprintPhase Phase, SprintAction Action)> _allowed = [];
    private readonly HashSet<(SprintPhase Phase, SprintAction Action)> _lookup = [];

    public PhaseActionPolicy(IEnumerable<PhaseAction> rows)
    {
        foreach (var row in rows)
        {
            if (_lookup.Add((row.Phase, row.Action)))
                _allowed.Add((row.Phase, row.Action));
        }
    }

    public bool IsAllowed(SprintPhase phase, SprintAction action)
        => _lookup.Contains((phase, action));

    public void EnsureAllowed(SprintPhase phase, SprintAction action)
    {
        if (!IsAllowed(phase, action))
            throw new DomainException($"{action} is not allowed in phase {phase}.");
    }

    public IReadOnlyDictionary<SprintPhase, IReadOnlyList<SprintAction>> GetActionsByPhase()
        => _allowed
            .GroupBy(x => x.Phase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SprintAction>)g.Select(x => x.Action).ToList());
}
