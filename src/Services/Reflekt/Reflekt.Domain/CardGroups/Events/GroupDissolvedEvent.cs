namespace Reflekt.Domain.CardGroups.Events;

public sealed record GroupDissolvedEvent(CardGroup Group) : IDomainEvent;
