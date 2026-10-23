using System.Security.Claims;
using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Cards.EditCard;

[Authorize(Policy = Auth.Policies.SignedInUserOrGuest)]
[HttpPut("/api/sprints/{sprintId}/cards/{cardId}")]
[Tags("Cards")]
public sealed class EditCardEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<EditCardCommand, CardResponse>
{
    public override async Task HandleAsync(EditCardCommand command, CancellationToken ct)
    {
        var card = await cardRepository.GetByIdAsync(command.CardId, ct);
        if (card is null || card.SprintId != command.SprintId)
            throw new NotFoundException("Card not found");

        var access = await sprintAccess.ForSprintAsync(User, command.SprintId, ct);
        var sprint = access.RequireSprint();
        sprint.EnsureOpen();

        if (access.IsGuest)
        {
            if (access.ParticipantId != card.ParticipantId)
                throw new ForbiddenException("You are not the author of this card.");
        }
        else
            access.RequireOwner();

        var action = card.CardType == CardType.ActionItem ? SprintAction.EditActionItem : SprintAction.EditNote;
        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, action);

        if (sprint.CurrentPhase == SprintPhase.Discussing
            && card.CardType == CardType.ActionItem
            && User.FindFirstValue(ClaimTypes.NameIdentifier) is null)
            throw new ForbiddenException("Only a signed-in user can edit action items while discussing.");

        card.Update(command.Text, command.AssigneeName);

        await cardRepository.SaveChangesAsync(ct);
        await SendOkAsync(card.Adapt<CardResponse>(), ct);
    }
}
