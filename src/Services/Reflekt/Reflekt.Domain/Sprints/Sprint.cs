namespace Reflekt.Domain.Sprints;

public sealed partial class Sprint : AggregateRoot
{
    public const int NameMaxLength = 100;
    public const int ColumnNameMaxLength = 30;
    public const int MinVotesPerParticipant = 1;
    public const int WritingTimerMinMinutes = 1;
    public const int WritingTimerMaxMinutes = 60;
    public const int VotingTimerMinMinutes = 1;
    public const int VotingTimerMaxMinutes = 60;
    public const int DiscussingTimerMinMinutes = 1;
    public const int DiscussingTimerMaxMinutes = 60;

    private Sprint() { }

    public required int TeamId { get; init; }
    public required string Name { get; init; }
    public SprintState State { get; private set; }
    public SprintPhase CurrentPhase { get; private set; }
    public required int VotesPerParticipant { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ClosedAt { get; private set; }
    public int? FocusedGroupId { get; private set; }

    public string Column1Name { get; private set; } = "Went Well";
    public string Column2Name { get; private set; } = "Didn't Go Well";
    public string Column3Name { get; private set; } = "Start Doing";
    public ColumnColor Column1Color { get; private set; } = ColumnColor.Create("green");
    public ColumnColor Column2Color { get; private set; } = ColumnColor.Create("red");
    public ColumnColor Column3Color { get; private set; } = ColumnColor.Create("amber");

    public required int WritingTimerDefault { get; init; }
    public required int VotingTimerDefault { get; init; }
    public required int DiscussingTimerDefault { get; init; }
}
