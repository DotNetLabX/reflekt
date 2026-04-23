using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Sprints.GetSprintSummary;

public sealed record GetSprintSummaryRequest(int SprintId);

[AllowAnonymous]
[HttpGet("/api/sprints/{sprintId}/summary")]
[Tags("Sprints")]
public sealed class GetSprintSummaryEndpoint(SprintRepository sprintRepository, ParticipantRepository participantRepository, TeamRepository teamRepository)
    : Endpoint<GetSprintSummaryRequest, SprintSummaryResponse>
{
    public override async Task HandleAsync(GetSprintSummaryRequest request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null) { await SendNotFoundAsync(ct); return; }

        var participants = await participantRepository.GetBySprintIdAsync(request.SprintId, ct);
        var participantDtos = participants.Select(p => new SprintSummaryParticipant(p.DisplayName)).ToList();

        var email = User.FindFirstValue(ClaimTypes.Email);
        var isScrumMaster = false;
        if (email is not null)
        {
            var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
            isScrumMaster = team is not null && string.Equals(team.CreatorEmail, email, StringComparison.OrdinalIgnoreCase);
        }

        await SendOkAsync(new SprintSummaryResponse(sprint.Id, sprint.Name, sprint.CurrentPhase.ToString(), sprint.VotesPerParticipant, isScrumMaster, participantDtos), ct);
    }
}
