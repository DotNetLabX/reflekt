using System.Security.Claims;
using Blocks.Core;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Sprints.GetSprintSummary;

[AllowAnonymous]
[HttpGet("/api/sprints/{sprintId}/summary")]
[Tags("Sprints")]
public sealed class GetSprintSummaryEndpoint(SprintRepository sprintRepository, ParticipantRepository participantRepository, TeamRepository teamRepository)
    : Endpoint<GetSprintSummaryRequest, SprintSummaryResponse>
{
    public override async Task HandleAsync(GetSprintSummaryRequest request, CancellationToken ct)
    {
        var sprint = Guard.NotFound(await sprintRepository.GetByIdReadOnlyAsync(request.SprintId, ct));

        var participants = await participantRepository.GetBySprintIdAsync(request.SprintId, ct);
        var participantDtos = participants.Select(p => new SprintSummaryParticipantResponse(p.DisplayName)).ToList();

        var isScrumMaster = false;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && int.TryParse(userIdClaim, out var userId))
        {
            var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
            isScrumMaster = team is not null && team.OwnerId == userId;
        }

        await SendOkAsync(sprint.Adapt<SprintSummaryResponse>() with
        {
            IsScrumMaster = isScrumMaster,
            Participants = participantDtos,
        }, ct);
    }
}
