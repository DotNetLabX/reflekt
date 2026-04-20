using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Participants.GetCurrentParticipant;

public sealed record GetCurrentParticipantRequest(int SprintId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpGet("/api/sprints/{sprintId}/participants/me")]
[Tags("Participants")]
public sealed class GetCurrentParticipantEndpoint(ParticipantRepository participantRepository)
    : Endpoint<GetCurrentParticipantRequest, ParticipantResponse>
{
    public override async Task HandleAsync(GetCurrentParticipantRequest request, CancellationToken ct)
    {
        var participantIdClaim = User.FindFirstValue("participantId");
        var sprintIdClaim = User.FindFirstValue("sprintId");

        if (!int.TryParse(sprintIdClaim, out var claimSprintId) || claimSprintId != request.SprintId)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        if (!int.TryParse(participantIdClaim, out var participantId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var participant = await participantRepository.GetByIdAsync(participantId, ct);
        if (participant is null) { await SendNotFoundAsync(ct); return; }

        await SendOkAsync(ParticipantMappings.ToResponse(participant), ct);
    }
}
