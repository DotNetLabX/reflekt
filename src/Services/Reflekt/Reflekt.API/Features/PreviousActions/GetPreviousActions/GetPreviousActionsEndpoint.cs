using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.PreviousActions.GetPreviousActions;

[Authorize(Policy = Auth.Policies.SignedInUserOrGuest)]
[HttpGet("/api/sprints/{sprintId}/previous-actions")]
[Tags("PreviousActions")]
public sealed class GetPreviousActionsEndpoint(
    SprintAccess sprintAccess,
    SprintRepository sprintRepository,
    CardRepository cardRepository)
    : Endpoint<GetPreviousActionsRequest, IReadOnlyList<PreviousActionItemResponse>>
{
    public override async Task HandleAsync(GetPreviousActionsRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();

        if (access.IsGuest)
            access.RequireGuestOf(req.SprintId);
        else
            access.RequireOwner();

        var previousSprint = await sprintRepository.GetMostRecentClosedByTeamIdAsync(sprint.TeamId, ct);
        if (previousSprint is null)
        {
            await SendOkAsync([], ct);
            return;
        }

        var unreviewedItems = await cardRepository.GetUnreviewedActionItemsBySprintIdAsync(previousSprint.Id, ct);

        var response = unreviewedItems
            .Select(c => new PreviousActionItemResponse(
                c.Id,
                c.Text,
                c.AssigneeName,
                c.ReviewStatus.ToString(),
                previousSprint.Id,
                previousSprint.Name))
            .ToList();

        await SendOkAsync(response, ct);
    }
}
