using BuildingBlocks.Domain;

namespace Reflekt.Domain.Teams;

public sealed class Team : AggregateRoot
{
    private Team() { }

    public string Name { get; private set; } = string.Empty;
    public string CreatorEmail { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public static Team Create(string name, string creatorEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorEmail);

        var team = new Team
        {
            Name = name,
            CreatorEmail = creatorEmail,
            CreatedAt = DateTime.UtcNow,
        };

        team.AddDomainEvent(new TeamCreatedEvent(team));
        return team;
    }
}
