using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.EvidenceLinks.DetachEvidenceLink;

[Authorize(Policy = Auth.Policies.SignedInUserOrGuest)]
[HttpDelete("/api/sprints/{sprintId}/cards/{cardId}/evidence-links/{evidenceLinkId}")]
[Tags("EvidenceLinks")]
public sealed class DetachEvidenceLinkEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<DetachEvidenceLinkRequest, EmptyResponse>
{
    public override async Task HandleAsync(DetachEvidenceLinkRequest req, CancellationToken ct)
    {
        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.SprintId != req.SprintId)
            throw new NotFoundException("Card not found");

        if (!card.EvidenceLinks.Any(e => e.Id == req.EvidenceLinkId))
            throw new NotFoundException("EvidenceLink not found");

        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        sprint.EnsureOpen();

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.DetachEvidenceLink);

        if (access.IsGuest)
        {
            if (access.ParticipantId != card.ParticipantId)
                throw new ForbiddenException("You are not the author of this card.");
        }
        else
            access.RequireOwner();

        card.DetachEvidenceLink(req.EvidenceLinkId);
        await cardRepository.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
