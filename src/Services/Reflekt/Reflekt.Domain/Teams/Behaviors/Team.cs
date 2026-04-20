namespace Reflekt.Domain.Teams;

public sealed partial class Team
{
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

        team.AddDomainEvent(new Events.TeamCreatedEvent(team));
        return team;
    }
}
