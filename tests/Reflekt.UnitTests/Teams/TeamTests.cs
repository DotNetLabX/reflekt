using FluentAssertions;
using Reflekt.Domain.Teams;
using Reflekt.Domain.Teams.Events;

namespace Reflekt.UnitTests.Teams;

public class TeamTests
{
    [Fact]
    public void Create_SetsNameAndOwnerIdAndCreatedAt()
    {
        var before = DateTime.UtcNow;
        var team = Team.Create("Alpha", 42);
        var after = DateTime.UtcNow;

        team.Name.Should().Be("Alpha");
        team.OwnerId.Should().Be(42);
        team.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_RaisesTeamCreatedEvent()
    {
        var team = Team.Create("Alpha", 42);

        team.DomainEvents.Should().HaveCount(1);
        var ev = team.DomainEvents[0].Should().BeOfType<TeamCreatedEvent>().Subject;
        ev.Team.Should().BeSameAs(team);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string name)
    {
        var act = () => Team.Create(name, 42);
        act.Should().Throw<ArgumentException>();
    }
}
