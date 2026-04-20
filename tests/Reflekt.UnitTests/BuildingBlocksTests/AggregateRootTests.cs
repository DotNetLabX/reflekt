using BuildingBlocks.Domain;
using FluentAssertions;

namespace Reflekt.UnitTests.BuildingBlocksTests;

public class AggregateRootTests
{
    [Fact]
    public void AggregateRoot_Raise_AppendsEvent()
    {
        var aggregate = new TestAggregate();
        var ev = new TestEvent();

        aggregate.RaiseTestEvent(ev);

        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents[0].Should().Be(ev);
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public TestAggregate() => Id = 1;
        public void RaiseTestEvent(IDomainEvent ev) => AddDomainEvent(ev);
    }

    private sealed record TestEvent : IDomainEvent;
}
