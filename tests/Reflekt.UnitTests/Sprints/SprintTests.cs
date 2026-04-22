using FluentAssertions;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Sprints.Events;

namespace Reflekt.UnitTests.Sprints;

public class SprintTests
{
    [Fact]
    public void Create_SetsAllPropertiesAndDraftState()
    {
        var before = DateTime.UtcNow;
        var sprint = Sprint.Create("Sprint 1", teamId: 42, votesPerParticipant: 5);
        var after = DateTime.UtcNow;

        sprint.Name.Should().Be("Sprint 1");
        sprint.TeamId.Should().Be(42);
        sprint.VotesPerParticipant.Should().Be(5);
        sprint.State.Should().Be(SprintState.Draft);
        sprint.CurrentPhase.Should().Be(SprintPhase.Writing);
        sprint.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        sprint.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void Create_DefaultsVotesPerParticipantTo3()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.VotesPerParticipant.Should().Be(3);
    }

    [Fact]
    public void Create_RaisesSprintCreatedEvent()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);

        sprint.DomainEvents.Should().HaveCount(1);
        var ev = sprint.DomainEvents[0].Should().BeOfType<SprintCreatedEvent>().Subject;
        ev.Sprint.Should().BeSameAs(sprint);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string name)
    {
        var act = () => Sprint.Create(name, teamId: 1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithZeroVotes_Throws()
    {
        var act = () => Sprint.Create("Sprint 1", teamId: 1, votesPerParticipant: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Start_FromDraft_TransitionsToActive()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.Start();
        sprint.State.Should().Be(SprintState.Active);
    }

    [Fact]
    public void Start_FromDraft_RaisesSprintStartedEvent()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.ClearDomainEvents();
        sprint.Start();

        sprint.DomainEvents.Should().HaveCount(1);
        sprint.DomainEvents[0].Should().BeOfType<SprintStartedEvent>();
    }

    [Fact]
    public void Start_FromActive_Throws()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.Start();
        var act = sprint.Start;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Start_FromClosed_Throws()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.Start();
        sprint.Close();
        var act = sprint.Start;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AdvancePhase_WritingToGrouping()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.Start();
        sprint.AdvancePhase();
        sprint.CurrentPhase.Should().Be(SprintPhase.Grouping);
    }

    [Fact]
    public void AdvancePhase_FullCycle()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.Start();

        sprint.AdvancePhase();
        sprint.CurrentPhase.Should().Be(SprintPhase.Grouping);

        sprint.AdvancePhase();
        sprint.CurrentPhase.Should().Be(SprintPhase.Voting);

        sprint.AdvancePhase();
        sprint.CurrentPhase.Should().Be(SprintPhase.Discussing);

        sprint.AdvancePhase();
        sprint.CurrentPhase.Should().Be(SprintPhase.Reporting);
    }

    [Fact]
    public void AdvancePhase_FromReporting_Throws()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.Start();
        sprint.AdvancePhase(); // Writing → Grouping
        sprint.AdvancePhase(); // Grouping → Voting
        sprint.AdvancePhase(); // Voting → Discussing
        sprint.AdvancePhase(); // Discussing → Reporting
        var act = sprint.AdvancePhase;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AdvancePhase_OnDraftSprint_Throws()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        var act = sprint.AdvancePhase;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AdvancePhase_RaisesPhaseAdvancedEvent()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.Start();
        sprint.ClearDomainEvents();
        sprint.AdvancePhase();

        sprint.DomainEvents.Should().HaveCount(1);
        sprint.DomainEvents[0].Should().BeOfType<PhaseAdvancedEvent>();
    }

    [Fact]
    public void Close_FromActive_TransitionsToClosedAndSetsClosedAt()
    {
        var before = DateTime.UtcNow;
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.Start();
        sprint.Close();
        var after = DateTime.UtcNow;

        sprint.State.Should().Be(SprintState.Closed);
        sprint.ClosedAt.Should().NotBeNull();
        sprint.ClosedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Close_FromActive_RaisesSprintClosedEvent()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.Start();
        sprint.ClearDomainEvents();
        sprint.Close();

        sprint.DomainEvents.Should().HaveCount(1);
        sprint.DomainEvents[0].Should().BeOfType<SprintClosedEvent>();
    }

    [Fact]
    public void Close_FromDraft_Throws()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        var act = sprint.Close;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Close_FromClosed_Throws()
    {
        var sprint = Sprint.Create("Sprint 1", teamId: 1);
        sprint.Start();
        sprint.Close();
        var act = sprint.Close;
        act.Should().Throw<InvalidOperationException>();
    }
}
