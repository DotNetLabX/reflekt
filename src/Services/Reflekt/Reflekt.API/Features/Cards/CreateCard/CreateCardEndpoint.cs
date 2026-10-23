using System.Security.Claims;
using Blocks.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Cards.CreateCard;

[Authorize(Policy = Auth.Policies.Guest)]
[HttpPost("/api/sprints/{sprintId}/cards")]
[Tags("Cards")]
public sealed class CreateCardEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<CreateCardCommand, CardResponse>
{
    public override async Task HandleAsync(CreateCardCommand command, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, command.SprintId, ct);
        access.RequireGuestOf(command.SprintId);
        var participantId = access.RequireParticipant();

        var columnType = Enum.Parse<ColumnType>(command.ColumnType);
        var cardType = Enum.Parse<CardType>(command.CardType);

        var sprint = access.RequireSprint();
        sprint.EnsureOpen();

        var action = cardType == CardType.ActionItem ? SprintAction.CreateActionItem : SprintAction.CreateNote;
        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, action);

        if (sprint.CurrentPhase == SprintPhase.Discussing && cardType == CardType.ActionItem)
        {
            var cookieResult = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            if (cookieResult.Principal?.FindFirstValue(ClaimTypes.Email) is null)
                throw new ForbiddenException("Only a signed-in user can create action items while discussing.");
        }

        var card = Card.Create(command.Text, command.SprintId, participantId, columnType, cardType, command.AssigneeName);

        await cardRepository.AddAsync(card, ct);
        await cardRepository.SaveChangesAsync(ct);

        await SendAsync(card.Adapt<CardResponse>(), 201, ct);
    }
}
