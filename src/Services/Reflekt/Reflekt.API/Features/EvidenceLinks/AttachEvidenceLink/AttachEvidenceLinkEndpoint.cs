using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Reflekt.API.Auth;
using Reflekt.API.Features.Cards;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.EvidenceLinks.AttachEvidenceLink;

[Authorize(Policy = Auth.Policies.SignedInUserOrGuest)]
[HttpPost("/api/sprints/{sprintId}/cards/{cardId}/evidence-links")]
[Tags("EvidenceLinks")]
public sealed class AttachEvidenceLinkEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    IOptions<EvidenceLinkSettings> evidenceLinkSettings,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<AttachEvidenceLinkCommand, EvidenceLinkResponse>
{
    public override async Task HandleAsync(AttachEvidenceLinkCommand command, CancellationToken ct)
    {
        var card = await cardRepository.GetByIdAsync(command.CardId, ct);
        if (card is null || card.SprintId != command.SprintId)
            throw new NotFoundException("Card not found");

        var access = await sprintAccess.ForSprintAsync(User, command.SprintId, ct);
        var sprint = access.RequireSprint();
        sprint.EnsureOpen();

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.AttachEvidenceLink);

        if (access.IsGuest)
        {
            if (access.ParticipantId != card.ParticipantId)
                throw new ForbiddenException("You are not the author of this card.");
        }
        else
            access.RequireOwner();

        var (source, externalKey) = EvidenceLinkSourceDetector.Detect(command.Url, evidenceLinkSettings.Value.JiraDomain);

        var link = card.AttachEvidenceLink(command.Url, source, externalKey);
        await cardRepository.SaveChangesAsync(ct);

        await SendAsync(link.Adapt<EvidenceLinkResponse>(), 201, ct);
    }
}
