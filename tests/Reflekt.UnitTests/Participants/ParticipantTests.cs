using FluentAssertions;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Participants.Events;

namespace Reflekt.UnitTests.Participants;

public class ParticipantTests
{
    [Fact]
    public void Join_SetsAllPropertiesCorrectly()
    {
        var before = DateTime.UtcNow;
        var participant = Participant.Join("Ana", sprintId: 7);
        var after = DateTime.UtcNow;

        participant.DisplayName.Should().Be("Ana");
        participant.SprintId.Should().Be(7);
        participant.GuestToken.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(participant.GuestToken, out _).Should().BeTrue();
        participant.JoinedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Join_GeneratesUniqueGuestTokens()
    {
        var p1 = Participant.Join("Ana", sprintId: 1);
        var p2 = Participant.Join("Mihai", sprintId: 1);

        p1.GuestToken.Should().NotBe(p2.GuestToken);
    }

    [Fact]
    public void Join_RaisesParticipantJoinedEvent()
    {
        var participant = Participant.Join("Ana", sprintId: 1);

        participant.DomainEvents.Should().HaveCount(1);
        var ev = participant.DomainEvents[0].Should().BeOfType<ParticipantJoinedEvent>().Subject;
        ev.Participant.Should().BeSameAs(participant);
    }

    [Fact]
    public void Join_WithEmptyDisplayName_Throws()
    {
        var act = () => Participant.Join("", sprintId: 1);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Join_WithWhitespaceDisplayName_Throws(string displayName)
    {
        var act = () => Participant.Join(displayName, sprintId: 1);
        act.Should().Throw<ArgumentException>();
    }
}
