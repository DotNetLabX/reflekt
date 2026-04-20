using FluentAssertions;
using Reflekt.Domain.Votes;
using Reflekt.Domain.Votes.Events;

namespace Reflekt.UnitTests.Votes;

public class VoteTests
{
    // Create factory

    [Fact]
    public void Create_SetsAllPropertiesCorrectly()
    {
        var before = DateTime.UtcNow;
        var vote = Vote.Create(cardId: 5, participantId: 10);
        var after = DateTime.UtcNow;

        vote.CardId.Should().Be(5);
        vote.ParticipantId.Should().Be(10);
        vote.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_RaisesVoteCastEvent()
    {
        var vote = Vote.Create(cardId: 5, participantId: 10);

        vote.DomainEvents.Should().HaveCount(1);
        vote.DomainEvents[0].Should().BeOfType<VoteCastEvent>();
    }

    [Fact]
    public void Create_EventContainsVoteReference()
    {
        var vote = Vote.Create(cardId: 5, participantId: 10);

        var ev = vote.DomainEvents[0].Should().BeOfType<VoteCastEvent>().Subject;
        ev.Vote.Should().BeSameAs(vote);
    }

    // MarkRetracted

    [Fact]
    public void MarkRetracted_RaisesVoteRetractedEvent()
    {
        var vote = Vote.Create(cardId: 5, participantId: 10);
        vote.ClearDomainEvents();

        vote.MarkRetracted();

        vote.DomainEvents.Should().HaveCount(1);
        vote.DomainEvents[0].Should().BeOfType<VoteRetractedEvent>();
    }

    [Fact]
    public void MarkRetracted_EventContainsVoteReference()
    {
        var vote = Vote.Create(cardId: 5, participantId: 10);
        vote.ClearDomainEvents();

        vote.MarkRetracted();

        var ev = vote.DomainEvents[0].Should().BeOfType<VoteRetractedEvent>().Subject;
        ev.Vote.Should().BeSameAs(vote);
    }
}
