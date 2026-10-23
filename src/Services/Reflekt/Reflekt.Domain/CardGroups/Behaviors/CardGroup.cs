using Reflekt.Domain.Cards;
using Reflekt.Domain.CardGroups.Events;

namespace Reflekt.Domain.CardGroups;

public sealed partial class CardGroup
{
    private const int MinCardsPerGroup = 2;

    public static CardGroup Create(int sprintId, ColumnType columnType, string? title = null)
    {
        EnsureValidTitle(title);

        var group = NewGroup(sprintId, columnType, title);
        group.AddDomainEvent(new CardGroupCreatedEvent(group));
        return group;
    }

    public static CardGroup CreateSolo(int sprintId, ColumnType columnType)
        => NewGroup(sprintId, columnType, title: null);

    private static CardGroup NewGroup(int sprintId, ColumnType columnType, string? title) => new()
    {
        SprintId = sprintId,
        ColumnType = columnType,
        Title = title,
        CreatedAt = DateTime.UtcNow,
    };

    public static void EnsureEnoughCardsToGroup(int cardCount)
    {
        if (cardCount < MinCardsPerGroup)
            throw new ArgumentException($"At least {MinCardsPerGroup} cards are required to create a group.");
    }

    public static ColumnType GetGroupableColumn(IReadOnlyCollection<Card> cards)
    {
        foreach (var card in cards)
            EnsureGroupable(card);

        var columnTypes = cards.Select(c => c.ColumnType).Distinct().ToList();
        if (columnTypes.Count > 1)
            throw new ArgumentException("All cards must be in the same column.");

        return columnTypes[0];
    }

    public void EnsureCanAdd(Card card)
    {
        EnsureGroupable(card);

        if (card.ColumnType != ColumnType)
            throw new ArgumentException("Card must be in the same column as the group.");
    }

    public void EnsureCanDissolve(int cardCount)
    {
        if (cardCount < MinCardsPerGroup)
            throw new ArgumentException("Cannot dissolve a solo group.");
    }

    public void Dissolve()
    {
        AddDomainEvent(new GroupDissolvedEvent(this));
    }

    private static void EnsureValidTitle(string? title)
    {
        if (title is not null && title.Length > TitleMaxLength)
            throw new ArgumentException($"Title must be {TitleMaxLength} characters or fewer.", nameof(title));
    }

    private static void EnsureGroupable(Card card)
    {
        if (card.CardType != CardType.Note)
            throw new ArgumentException("Only Note cards can be grouped.");
    }

    public void SetTitle(string? title)
    {
        EnsureValidTitle(title);

        Title = title;
        AddDomainEvent(new CardGroupTitleChangedEvent(this));
    }

    public void RecordCardAdded(int cardId, int sourceGroupId)
    {
        AddDomainEvent(new CardsGroupedEvent(this, cardId, sourceGroupId));
    }

    public void RecordCardRemoved(int cardId, int newGroupId)
    {
        AddDomainEvent(new CardUngroupedEvent(this, cardId, newGroupId));
    }

    public void MarkDiscussed()
    {
        DiscussedAt = DateTime.UtcNow;
        AddDomainEvent(new GroupDiscussedChangedEvent(this));
    }

    public void UnmarkDiscussed()
    {
        DiscussedAt = null;
        AddDomainEvent(new GroupDiscussedChangedEvent(this));
    }
}
