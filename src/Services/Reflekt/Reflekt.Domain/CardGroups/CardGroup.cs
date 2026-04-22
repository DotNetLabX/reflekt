using Reflekt.Domain.Cards;

namespace Reflekt.Domain.CardGroups;

public sealed partial class CardGroup : AggregateRoot
{
    private CardGroup() { }

    public int SprintId { get; private set; }
    public ColumnType ColumnType { get; private set; }
    public string? Title { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
