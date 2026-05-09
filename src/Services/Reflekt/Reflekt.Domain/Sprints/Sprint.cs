using Blocks.Domain;

namespace Reflekt.Domain.Sprints;

public sealed partial class Sprint : AggregateRoot
{
    private Sprint() { }

    public required int TeamId { get; init; }
    public required string Name { get; init; }
    public SprintState State { get; private set; }
    public SprintPhase CurrentPhase { get; private set; }
    public required int VotesPerParticipant { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ClosedAt { get; private set; }
    public int? FocusedGroupId { get; private set; }
}
