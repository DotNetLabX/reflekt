using FastEndpoints;

namespace BuildingBlocks.Domain;

public abstract class AggregateRoot : Entity
{
    private readonly List<IEvent> _domainEvents = [];

    public IReadOnlyList<IEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IEvent ev) => _domainEvents.Add(ev);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
