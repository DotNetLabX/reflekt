namespace Reflekt.API.Features.Sprints.AdvancePhase;

public sealed record AdvancePhaseCheckResponse(
    string CurrentPhase,
    int? NonVotingParticipantCount = null,
    int? TotalParticipantCount = null,
    int? DiscussedGroupCount = null,
    int? FocusedGroupId = null,
    int? UnreviewedActionCount = null,
    int? TotalPreviousActionCount = null);
