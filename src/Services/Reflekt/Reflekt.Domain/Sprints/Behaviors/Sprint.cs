namespace Reflekt.Domain.Sprints;

public sealed partial class Sprint
{
    public static Sprint Create(string name, int teamId, int votesPerParticipant = 3)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(votesPerParticipant, 1);

        var sprint = new Sprint
        {
            Name = name,
            TeamId = teamId,
            VotesPerParticipant = votesPerParticipant,
            CreatedAt = DateTime.UtcNow,
        };
        sprint.State = SprintState.Draft;
        sprint.CurrentPhase = SprintPhase.Writing;
        sprint.AddDomainEvent(new Events.SprintCreatedEvent(sprint));
        return sprint;
    }

    public void Start()
    {
        if (State != SprintState.Draft)
            throw new InvalidOperationException("Sprint must be in Draft state to start.");
        State = SprintState.Active;
        AddDomainEvent(new Events.SprintStartedEvent(this));
    }

    public void AdvancePhase()
    {
        if (State != SprintState.Active)
            throw new InvalidOperationException("Sprint must be Active to advance phase.");
        CurrentPhase = CurrentPhase switch
        {
            SprintPhase.Writing => SprintPhase.Voting,
            SprintPhase.Voting => SprintPhase.Discussing,
            SprintPhase.Discussing => SprintPhase.Closed,
            SprintPhase.Closed => throw new InvalidOperationException("Sprint phase is already Closed."),
            _ => throw new InvalidOperationException($"Unknown phase: {CurrentPhase}."),
        };
        AddDomainEvent(new Events.PhaseAdvancedEvent(this));
    }

    public void Close()
    {
        if (State != SprintState.Active)
            throw new InvalidOperationException("Sprint must be Active to close.");
        State = SprintState.Closed;
        ClosedAt = DateTime.UtcNow;
        AddDomainEvent(new Events.SprintClosedEvent(this));
    }
}
