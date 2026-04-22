using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Cards.CreateCard;

[Authorize(Policy = AuthPolicies.Participant)]
[HttpPost("/api/sprints/{sprintId}/cards")]
[Tags("Cards")]
public sealed class CreateCardEndpoint(SprintRepository sprintRepository, CardRepository cardRepository, PhaseActionCache phaseActionCache)
    : Endpoint<CreateCardCommand, CardResponse>
{
    public override async Task HandleAsync(CreateCardCommand command, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("sprintId"), out var claimSprintId) || claimSprintId != command.SprintId)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        if (!int.TryParse(User.FindFirstValue("participantId"), out var participantId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        if (!Enum.TryParse<ColumnType>(command.ColumnType, out var columnType))
        {
            ThrowError(x => x.ColumnType, "Invalid column type.");
            return;
        }

        if (!Enum.TryParse<CardType>(command.CardType, out var cardType))
        {
            ThrowError(x => x.CardType, "Invalid card type.");
            return;
        }

        var sprint = await sprintRepository.GetByIdAsync(command.SprintId, ct);
        if (sprint is null) { await SendNotFoundAsync(ct); return; }

        if (sprint.State == SprintState.Closed)
            ThrowError("Sprint is closed.", StatusCodes.Status409Conflict);

        var action = cardType == CardType.ActionItem ? SprintAction.CreateActionItem : SprintAction.CreateNote;
        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, action))
            ThrowError($"{action} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        Card card;
        try
        {
            card = Card.Create(command.Text, command.SprintId, participantId, columnType, cardType, command.AssigneeName);
        }
        catch (ArgumentException ex)
        {
            ThrowError(ex.Message);
            return;
        }

        await cardRepository.AddAsync(card, ct);
        await cardRepository.SaveChangesAsync(ct);

        await SendAsync(CardMappings.ToResponse(card), 201, ct);
    }
}
