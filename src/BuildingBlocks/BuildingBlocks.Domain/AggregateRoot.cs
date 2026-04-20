namespace BuildingBlocks.Domain;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent ev) => _domainEvents.Add(ev);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
