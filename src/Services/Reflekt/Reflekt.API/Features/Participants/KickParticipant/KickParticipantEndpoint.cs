using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Participants.KickParticipant;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpDelete("/api/sprints/{sprintId}/participants/{participantId}")]
[Tags("Participants")]
public sealed class KickParticipantEndpoint(
    SprintAccess sprintAccess,
    ParticipantRepository participantRepository)
    : Endpoint<KickParticipantRequest, EmptyResponse>
{
    public override async Task HandleAsync(KickParticipantRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        sprint.EnsureActive("Cannot kick participants from a non-active sprint.");
        access.RequireOwner();

        var participant = await participantRepository.GetByIdAsync(req.ParticipantId, ct);
        if (participant is null || participant.SprintId != req.SprintId)
            throw new NotFoundException("Participant not found");

        participant.Kick();
        await participantRepository.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
