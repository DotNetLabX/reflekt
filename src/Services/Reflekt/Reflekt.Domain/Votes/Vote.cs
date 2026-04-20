using BuildingBlocks.Domain;

namespace Reflekt.Domain.Votes;

public sealed partial class Vote : AggregateRoot
{
    private Vote() { }

    public int CardId { get; private set; }
    public int ParticipantId { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
