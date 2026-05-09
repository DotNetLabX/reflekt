using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Features.Cards;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.EvidenceLinks.AttachEvidenceLink;

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpPost("/api/sprints/{sprintId}/cards/{cardId}/evidence-links")]
[Tags("EvidenceLinks")]
public sealed class AttachEvidenceLinkEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    IConfiguration configuration,
    PhaseActionCache phaseActionCache)
    : Endpoint<AttachEvidenceLinkCommand, EvidenceLinkResponse>
{
    public override async Task HandleAsync(AttachEvidenceLinkCommand command, CancellationToken ct)
    {
        var card = await cardRepository.GetByIdAsync(command.CardId, ct);
        if (card is null || card.SprintId != command.SprintId) { await SendNotFoundAsync(ct); return; }

        var sprint = await sprintRepository.GetByIdAsync(command.SprintId, ct);
        if (sprint is null) { await SendNotFoundAsync(ct); return; }

        if (sprint.State == SprintState.Closed)
            ThrowError("Sprint is closed.", StatusCodes.Status409Conflict);

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.AttachEvidenceLink))
            ThrowError($"{SprintAction.AttachEvidenceLink} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        var participantIdClaim = User.FindFirstValue("participantId");
        if (participantIdClaim is not null)
        {
            if (!int.TryParse(participantIdClaim, out var claimParticipantId) || claimParticipantId != card.ParticipantId)
            {
                await SendForbiddenAsync(ct);
                return;
            }
        }
        else
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
            if (team is null || !string.Equals(team.CreatorEmail, email, StringComparison.OrdinalIgnoreCase))
            {
                await SendForbiddenAsync(ct);
                return;
            }
        }

        var jiraDomain = configuration["EvidenceLinks:JiraDomain"];
        var (source, externalKey) = EvidenceLinkSourceDetector.Detect(command.Url, jiraDomain);

        var link = card.AttachEvidenceLink(command.Url, source, externalKey);
        await cardRepository.SaveChangesAsync(ct);

        await SendAsync(link.Adapt<EvidenceLinkResponse>(), 201, ct);
    }
}
