using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.Cards;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.GetGroups;

[Authorize(Policy = Auth.Policies.SignedInUserOrGuest)]
[HttpGet("/api/sprints/{sprintId}/card-groups")]
[Tags("CardGroups")]
public sealed class GetGroupsEndpoint(
    SprintAccess sprintAccess,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository)
    : Endpoint<GetGroupsRequest, IReadOnlyList<CardGroupResponse>>
{
    public override async Task HandleAsync(GetGroupsRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();

        if (access.IsGuest)
            access.RequireGuestOf(req.SprintId);
        else
            access.RequireOwner();

        var groups = await cardGroupRepository.GetBySprintIdReadOnlyAsync(req.SprintId, ct);
        var cards = await cardRepository.GetBySprintIdReadOnlyAsync(req.SprintId, ct);
        var noteCards = cards.Where(c => c.CardType == CardType.Note && c.GroupId.HasValue);
        var cardsByGroup = noteCards.GroupBy(c => c.GroupId!.Value).ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreatedAt).ToList());

        var viewerParticipantId = WritingPhaseRedaction.GetViewerParticipantId(User);

        var voteCounts = sprint.CurrentPhase >= SprintPhase.Voting
            ? await voteRepository.GetVoteCountsByGroupIdsAsync(groups.Select(g => g.Id), ct)
            : new Dictionary<int, int>();

        await SendOkAsync(
            groups.Select(g => CardGroupResponseBuilder.Build(
                g,
                cardsByGroup.GetValueOrDefault(g.Id, [])
                    .Select(c => WritingPhaseRedaction.IsRedacted(c, sprint, viewerParticipantId)
                        ? WritingPhaseRedaction.Redact(c.Adapt<CardGroupCardResponse>())
                        : c.Adapt<CardGroupCardResponse>()),
                sprint,
                voteCounts.GetValueOrDefault(g.Id, 0))).ToList(),
            ct);
    }
}
