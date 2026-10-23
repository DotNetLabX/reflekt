using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Sprints.AdvancePhase;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPost("/api/teams/{teamId}/sprints/{sprintId}/advance-phase/check")]
[Tags("Sprints")]
public sealed class AdvancePhaseCheckEndpoint(
    SprintAccess sprintAccess,
    SprintRepository sprintRepository,
    ParticipantRepository participantRepository,
    CardGroupRepository cardGroupRepository,
    CardRepository cardRepository)
    : Endpoint<SprintRouteRequest, AdvancePhaseCheckResponse>
{
    public override async Task HandleAsync(SprintRouteRequest request, CancellationToken ct)
    {
        var access = await sprintAccess.ForTeamAsync(User, request.TeamId, request.SprintId, ct);
        access.RequireTeam();
        access.RequireOwner();
        var sprint = access.RequireSprint();

        sprint.EnsureActive("Sprint must be Active to check phase advancement.");
        sprint.EnsureCanAdvancePhase("Cannot advance from Reporting phase.");

        var response = sprint.CurrentPhase switch
        {
            SprintPhase.Writing => await BuildWritingCheckAsync(sprint, ct),
            SprintPhase.Voting => await BuildVotingCheckAsync(sprint, ct),
            SprintPhase.Discussing => await BuildDiscussingCheckAsync(sprint, ct),
            _ => new AdvancePhaseCheckResponse(sprint.CurrentPhase.ToString()),
        };

        await SendOkAsync(response, ct);
    }

    private async Task<AdvancePhaseCheckResponse> BuildWritingCheckAsync(Sprint sprint, CancellationToken ct)
    {
        var previousSprint = await sprintRepository.GetMostRecentClosedByTeamIdAsync(sprint.TeamId, ct);
        if (previousSprint is null)
            return new AdvancePhaseCheckResponse(sprint.CurrentPhase.ToString());

        var unreviewedCount = await cardRepository.CountUnreviewedActionItemsBySprintIdAsync(previousSprint.Id, ct);
        var reviewedCount = await cardRepository.CountActionItemsReviewedInSprintAsync(previousSprint.Id, ct);
        var totalCount = unreviewedCount + reviewedCount;

        if (totalCount == 0)
            return new AdvancePhaseCheckResponse(sprint.CurrentPhase.ToString());

        return new AdvancePhaseCheckResponse(
            sprint.CurrentPhase.ToString(),
            UnreviewedActionCount: unreviewedCount,
            TotalPreviousActionCount: totalCount);
    }

    private async Task<AdvancePhaseCheckResponse> BuildVotingCheckAsync(Sprint sprint, CancellationToken ct)
    {
        var totalCount = await participantRepository.CountBySprintIdAsync(sprint.Id, ct);
        var nonVotingCount = await participantRepository.CountWithoutVotesInSprintAsync(sprint.Id, ct);

        return new AdvancePhaseCheckResponse(
            sprint.CurrentPhase.ToString(),
            NonVotingParticipantCount: nonVotingCount,
            TotalParticipantCount: totalCount);
    }

    private async Task<AdvancePhaseCheckResponse> BuildDiscussingCheckAsync(Sprint sprint, CancellationToken ct)
    {
        var discussedCount = await cardGroupRepository.CountDiscussedBySprintIdAsync(sprint.Id, ct);

        return new AdvancePhaseCheckResponse(
            sprint.CurrentPhase.ToString(),
            DiscussedGroupCount: discussedCount,
            FocusedGroupId: sprint.FocusedGroupId);
    }
}
