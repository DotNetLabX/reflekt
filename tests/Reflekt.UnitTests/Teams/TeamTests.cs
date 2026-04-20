using FluentAssertions;
using Reflekt.Domain.Teams;
using Reflekt.Domain.Teams.Events;

namespace Reflekt.UnitTests.Teams;

public class TeamTests
{
    [Fact]
    public void Create_SetsNameAndCreatorEmailAndCreatedAt()
    {
        var before = DateTime.UtcNow;
        var team = Team.Create("Alpha", "user@example.com");
        var after = DateTime.UtcNow;

        team.Name.Should().Be("Alpha");
        team.CreatorEmail.Should().Be("user@example.com");
        team.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_RaisesTeamCreatedEvent()
    {
        var team = Team.Create("Alpha", "user@example.com");

        team.DomainEvents.Should().HaveCount(1);
        var ev = team.DomainEvents[0].Should().BeOfType<TeamCreatedEvent>().Subject;
        ev.Team.Should().BeSameAs(team);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string name)
    {
        var act = () => Team.Create(name, "user@example.com");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEmail_Throws(string email)
    {
        var act = () => Team.Create("Alpha", email);
        act.Should().Throw<ArgumentException>();
    }
}
