using BuildingBlocks.Domain;

namespace Reflekt.Domain.Participants;

public sealed partial class Participant : AggregateRoot
{
    private Participant() { }

    public int SprintId { get; private set; }
    public string DisplayName { get; private set; } = default!;
    public string GuestToken { get; private set; } = default!;
    public DateTime JoinedAt { get; private set; }
}
