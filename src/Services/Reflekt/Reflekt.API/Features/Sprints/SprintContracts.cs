namespace Reflekt.API.Features.Sprints;

public sealed record SprintResponse(
    int Id,
    int TeamId,
    string Name,
    string State,
    string CurrentPhase,
    int VotesPerParticipant,
    DateTime CreatedAt,
    DateTime? ClosedAt);

public sealed record SprintSummaryParticipant(string DisplayName);

public sealed record SprintSummaryResponse(int Id, string Name, string CurrentPhase, int VotesPerParticipant, bool IsScrumMaster, List<SprintSummaryParticipant> Participants);

public sealed record SprintRouteRequest(int TeamId, int SprintId);

