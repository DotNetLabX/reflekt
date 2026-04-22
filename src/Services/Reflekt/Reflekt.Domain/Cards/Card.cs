using BuildingBlocks.Domain;

namespace Reflekt.Domain.Cards;

public sealed partial class Card : AggregateRoot
{
    private Card() { }

    public int SprintId { get; private set; }
    public int ParticipantId { get; private set; }
    public ColumnType ColumnType { get; private set; }
    public CardType CardType { get; private set; }
    public string Text { get; private set; } = default!;
    public string? AssigneeName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int? GroupId { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<EvidenceLink> _evidenceLinks = [];
    public IReadOnlyList<EvidenceLink> EvidenceLinks => _evidenceLinks.AsReadOnly();
}
