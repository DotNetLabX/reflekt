using Blocks.Core;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Participants.GetCurrentParticipant;

[Authorize(Policy = Auth.Policies.Guest)]
[HttpGet("/api/sprints/{sprintId}/participants/me")]
[Tags("Participants")]
public sealed class GetCurrentParticipantEndpoint(SprintAccess sprintAccess, ParticipantRepository participantRepository)
    : Endpoint<GetCurrentParticipantRequest, ParticipantResponse>
{
    public override async Task HandleAsync(GetCurrentParticipantRequest request, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, request.SprintId, ct);
        access.RequireGuestOf(request.SprintId);
        var participantId = access.RequireParticipant();

        var participant = Guard.NotFound(await participantRepository.GetByIdAsync(participantId, ct));

        await SendOkAsync(participant.Adapt<ParticipantResponse>(), ct);
    }
}
