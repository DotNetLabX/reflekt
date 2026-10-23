using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Cards.ListCards;

[Authorize(Policy = Auth.Policies.SignedInUserOrGuest)]
[HttpGet("/api/sprints/{sprintId}/cards")]
[Tags("Cards")]
public sealed class ListCardsEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    VoteRepository voteRepository)
    : Endpoint<ListCardsRequest, IReadOnlyList<CardResponse>>
{
    public override async Task HandleAsync(ListCardsRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();

        if (access.IsGuest)
            access.RequireGuestOf(req.SprintId);
        else
            access.RequireOwner();

        var cards = await cardRepository.GetBySprintIdReadOnlyAsync(req.SprintId, ct);
        var groupIds = cards.Where(c => c.GroupId.HasValue).Select(c => c.GroupId!.Value).Distinct();
        var voteCounts = await voteRepository.GetVoteCountsByGroupIdsAsync(groupIds, ct);

        var viewerParticipantId = WritingPhaseRedaction.GetViewerParticipantId(User);

        await SendOkAsync(
            cards.Select(c =>
            {
                var voteCount = c.GroupId.HasValue ? voteCounts.GetValueOrDefault(c.GroupId.Value, 0) : 0;
                var response = c.Adapt<CardResponse>() with { VoteCount = voteCount };
                return WritingPhaseRedaction.IsRedacted(c, sprint, viewerParticipantId)
                    ? WritingPhaseRedaction.Redact(response)
                    : response;
            }).ToList(),
            ct);
    }
}
