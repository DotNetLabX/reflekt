using Blocks.Domain;

namespace Reflekt.Domain.Sprints.Events;

public sealed record SprintCreatedEvent(Sprint Sprint) : IDomainEvent;
