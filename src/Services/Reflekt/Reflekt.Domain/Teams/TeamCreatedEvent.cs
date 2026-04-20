using FastEndpoints;

namespace Reflekt.Domain.Teams;

public sealed record TeamCreatedEvent(Team Team) : IEvent;
