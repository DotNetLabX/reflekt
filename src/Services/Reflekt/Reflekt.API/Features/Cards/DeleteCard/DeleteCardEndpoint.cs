using System.Security.Claims;
using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Cards.DeleteCard;

[Authorize(Policy = Auth.Policies.SignedInUserOrGuest)]
[HttpDelete("/api/sprints/{sprintId}/cards/{cardId}")]
[Tags("Cards")]
public sealed class DeleteCardEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<DeleteCardRequest, EmptyResponse>
{
    public override async Task HandleAsync(DeleteCardRequest req, CancellationToken ct)
    {
        var card = await cardRepository.GetByIdAsync(req.CardId, ct);
        if (card is null || card.SprintId != req.SprintId)
            throw new NotFoundException("Card not found");

        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        sprint.EnsureOpen();

        if (access.IsGuest)
        {
            if (access.ParticipantId != card.ParticipantId)
                throw new ForbiddenException("You are not the author of this card.");
        }
        else
            access.RequireOwner();

        var action = card.CardType == CardType.ActionItem ? SprintAction.DeleteActionItem : SprintAction.DeleteNote;
        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, action);

        if (sprint.CurrentPhase == SprintPhase.Discussing
            && card.CardType == CardType.ActionItem
            && User.FindFirstValue(ClaimTypes.NameIdentifier) is null)
            throw new ForbiddenException("Only a signed-in user can delete action items while discussing.");

        card.MarkDeleted();
        cardRepository.Remove(card);
        await cardRepository.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
