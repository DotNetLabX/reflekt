using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Participants.GetParticipants;

[Authorize(Policy = Auth.Policies.SignedInUserOrGuest)]
[HttpGet("/api/sprints/{sprintId}/participants")]
[Tags("Participants")]
public sealed class GetParticipantsEndpoint(
    SprintAccess sprintAccess,
    ParticipantRepository participantRepository)
    : Endpoint<GetParticipantsRequest, IReadOnlyList<ParticipantResponse>>
{
    public override async Task HandleAsync(GetParticipantsRequest request, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, request.SprintId, ct);
        access.RequireSprint();

        if (access.IsGuest)
            access.RequireGuestOf(request.SprintId);
        else
            access.RequireOwner();

        var participants = await participantRepository.GetBySprintIdAsync(request.SprintId, ct);
        await SendOkAsync(participants.Select(p => p.Adapt<ParticipantResponse>()).ToList(), ct);
    }
}
