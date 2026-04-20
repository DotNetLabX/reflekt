using BuildingBlocks.Domain;

namespace Reflekt.Domain.Teams.Events;

public sealed record TeamCreatedEvent(Team Team) : IDomainEvent;
