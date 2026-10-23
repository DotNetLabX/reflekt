namespace Reflekt.Domain.Participants;

public sealed partial class Participant : AggregateRoot
{
    public const int DisplayNameMaxLength = 50;

    private Participant() { }

    public required int SprintId { get; init; }
    public required string DisplayName { get; init; }
    public required string GuestToken { get; init; }
    public required DateTime JoinedAt { get; init; }
    public int? UserId { get; init; }
    public DateTime? RemovedAt { get; private set; }

    public bool IsRemoved => RemovedAt is not null;
}
