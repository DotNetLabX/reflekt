using Reflekt.Domain.Cards.Events;

namespace Reflekt.Domain.Cards;

public sealed partial class Card
{
    public static Card Create(
        string text,
        int sprintId,
        int participantId,
        ColumnType columnType,
        CardType cardType,
        string? assigneeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (cardType == CardType.ActionItem)
        {
            if (string.IsNullOrWhiteSpace(assigneeName))
                throw new ArgumentException("ActionItem cards require an assignee name.", nameof(assigneeName));
            if (columnType != ColumnType.ActionItems)
                throw new ArgumentException("ActionItem cards must be in the ActionItems column.", nameof(columnType));
        }

        if (cardType == CardType.Note)
        {
            if (columnType == ColumnType.ActionItems)
                throw new ArgumentException("Note cards cannot be placed in the ActionItems column.", nameof(columnType));
            if (assigneeName is not null)
                throw new ArgumentException("Note cards cannot have an assignee.", nameof(assigneeName));
        }

        var now = DateTime.UtcNow;
        var card = new Card
        {
            SprintId = sprintId,
            ParticipantId = participantId,
            ColumnType = columnType,
            CardType = cardType,
            Text = text,
            AssigneeName = assigneeName,
            CreatedAt = now,
            UpdatedAt = now,
        };
        card.AddDomainEvent(new CardCreatedEvent(card));
        return card;
    }

    public void Update(string text, string? assigneeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (CardType == CardType.ActionItem && string.IsNullOrWhiteSpace(assigneeName))
            throw new ArgumentException("ActionItem cards require an assignee name.", nameof(assigneeName));
        if (CardType == CardType.Note && assigneeName is not null)
            throw new ArgumentException("Note cards cannot have an assignee.", nameof(assigneeName));

        Text = text;
        AssigneeName = assigneeName;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CardUpdatedEvent(this));
    }

    public void MarkDeleted()
    {
        AddDomainEvent(new CardDeletedEvent(this));
    }
}
