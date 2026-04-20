using FluentAssertions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.UnitTests.Cards;

public class CardTests
{
    // Create — Note

    [Fact]
    public void Create_Note_SetsAllPropertiesCorrectly()
    {
        var before = DateTime.UtcNow;
        var card = Card.Create("Great teamwork", sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.Note, null);
        var after = DateTime.UtcNow;

        card.Text.Should().Be("Great teamwork");
        card.SprintId.Should().Be(1);
        card.ParticipantId.Should().Be(2);
        card.ColumnType.Should().Be(ColumnType.WentWell);
        card.CardType.Should().Be(CardType.Note);
        card.AssigneeName.Should().BeNull();
        card.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        card.UpdatedAt.Should().Be(card.CreatedAt);
    }

    [Fact]
    public void Create_ActionItem_SetsAllPropertiesCorrectly()
    {
        var card = Card.Create("Update docs", sprintId: 1, participantId: 2, ColumnType.ActionItems, CardType.ActionItem, "Ana");

        card.CardType.Should().Be(CardType.ActionItem);
        card.ColumnType.Should().Be(ColumnType.ActionItems);
        card.AssigneeName.Should().Be("Ana");
    }

    [Fact]
    public void Create_Note_RaisesCardCreatedEvent()
    {
        var card = Card.Create("Good sprint", sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.Note, null);

        card.DomainEvents.Should().HaveCount(1);
        var ev = card.DomainEvents[0].Should().BeOfType<CardCreatedEvent>().Subject;
        ev.Card.Should().BeSameAs(card);
    }

    [Fact]
    public void Create_WithEmptyText_Throws()
    {
        var act = () => Card.Create("", sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.Note, null);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithWhitespaceText_Throws(string text)
    {
        var act = () => Card.Create(text, sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.Note, null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ActionItem_WithMissingAssigneeName_Throws()
    {
        var act = () => Card.Create("Fix bug", sprintId: 1, participantId: 2, ColumnType.ActionItems, CardType.ActionItem, null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ActionItem_InWrongColumn_Throws()
    {
        var act = () => Card.Create("Fix bug", sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.ActionItem, "Ana");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Note_InActionItemsColumn_Throws()
    {
        var act = () => Card.Create("Nice work", sprintId: 1, participantId: 2, ColumnType.ActionItems, CardType.Note, null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Note_WithAssigneeName_Throws()
    {
        var act = () => Card.Create("Nice work", sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.Note, "Ana");
        act.Should().Throw<ArgumentException>();
    }

    // Update

    [Fact]
    public void Update_ChangesTextAndTimestamp()
    {
        var card = Card.Create("Original", sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.Note, null);
        var createdAt = card.CreatedAt;
        var before = DateTime.UtcNow;

        card.Update("Updated text", null);
        var after = DateTime.UtcNow;

        card.Text.Should().Be("Updated text");
        card.CreatedAt.Should().Be(createdAt);
        card.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Update_ActionItem_ChangesAssigneeName()
    {
        var card = Card.Create("Fix bug", sprintId: 1, participantId: 2, ColumnType.ActionItems, CardType.ActionItem, "Ana");

        card.Update("Fix critical bug", "Mihai");

        card.Text.Should().Be("Fix critical bug");
        card.AssigneeName.Should().Be("Mihai");
    }

    [Fact]
    public void Update_RaisesCardUpdatedEvent()
    {
        var card = Card.Create("Original", sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.Note, null);
        card.ClearDomainEvents();

        card.Update("Updated", null);

        card.DomainEvents.Should().HaveCount(1);
        var ev = card.DomainEvents[0].Should().BeOfType<CardUpdatedEvent>().Subject;
        ev.Card.Should().BeSameAs(card);
    }

    [Fact]
    public void Update_WithEmptyText_Throws()
    {
        var card = Card.Create("Original", sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.Note, null);
        var act = () => card.Update("", null);
        act.Should().Throw<ArgumentException>();
    }

    // MarkDeleted

    [Fact]
    public void MarkDeleted_RaisesCardDeletedEvent()
    {
        var card = Card.Create("Original", sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.Note, null);
        card.ClearDomainEvents();

        card.MarkDeleted();

        card.DomainEvents.Should().HaveCount(1);
        var ev = card.DomainEvents[0].Should().BeOfType<CardDeletedEvent>().Subject;
        ev.Card.Should().BeSameAs(card);
    }
}
