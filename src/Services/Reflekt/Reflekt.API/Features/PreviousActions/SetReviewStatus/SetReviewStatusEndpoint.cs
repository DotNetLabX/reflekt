using Blocks.Core;
using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.PreviousActions.SetReviewStatus;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPatch("/api/teams/{teamId}/sprints/{sprintId}/previous-actions/{cardId}")]
[Tags("PreviousActions")]
public sealed class SetReviewStatusEndpoint(
    SprintAccess sprintAccess,
    SprintRepository sprintRepository,
    CardRepository cardRepository)
    : Endpoint<SetReviewStatusCommand, PreviousActionItemResponse>
{
    public override async Task HandleAsync(SetReviewStatusCommand command, CancellationToken ct)
    {
        var access = await sprintAccess.ForTeamAsync(User, command.TeamId, command.SprintId, ct);
        access.RequireTeam();
        access.RequireOwner();
        var sprint = access.RequireSprint();

        if (sprint.State != SprintState.Active || sprint.CurrentPhase != SprintPhase.Writing)
            throw new ConflictException("Review status can only be set during the Writing phase.");

        var previousSprint = Guard.NotFound(await sprintRepository.GetMostRecentClosedByTeamIdAsync(command.TeamId, ct));

        var card = await cardRepository.GetByIdAsync(command.CardId, ct);
        if (card is null || card.SprintId != previousSprint.Id || card.CardType != CardType.ActionItem)
            throw new NotFoundException("Card not found");

        var reviewStatus = Enum.Parse<Domain.Cards.ReviewStatus>(command.ReviewStatus);
        card.SetReviewStatus(reviewStatus, command.SprintId);
        await cardRepository.SaveChangesAsync(ct);

        var response = new PreviousActionItemResponse(
            card.Id,
            card.Text,
            card.AssigneeName,
            card.ReviewStatus.ToString(),
            previousSprint.Id,
            previousSprint.Name);

        await SendOkAsync(response, ct);
    }
}
