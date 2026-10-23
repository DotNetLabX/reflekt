using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Sprints.AdvancePhase;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPost("/api/teams/{teamId}/sprints/{sprintId}/advance-phase")]
[Tags("Sprints")]
public sealed class AdvancePhaseEndpoint(
    SprintAccess sprintAccess,
    SprintRepository sprintRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<SprintRouteRequest, SprintResponse>
{
    public override async Task HandleAsync(SprintRouteRequest request, CancellationToken ct)
    {
        var access = await sprintAccess.ForTeamAsync(User, request.TeamId, request.SprintId, ct);
        access.RequireTeam();
        access.RequireOwner();
        var sprint = access.RequireSprint();

        sprint.EnsureActive("Sprint must be Active to advance phase.");
        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.AdvancePhase);

        var previousPhase = sprint.CurrentPhase;

        if (previousPhase == SprintPhase.Discussing)
            await CloseDiscussionAsync(sprint, ct);

        sprint.AdvancePhase();

        if (previousPhase == SprintPhase.Writing)
            await LeaveWritingAsync(sprint, ct);
        else
            await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(sprint.Adapt<SprintResponse>(), ct);
    }

    private async Task CloseDiscussionAsync(Sprint sprint, CancellationToken ct)
    {
        var groups = await cardGroupRepository.GetBySprintIdAsync(sprint.Id, ct);

        if (sprint.FocusedGroupId is { } focusedGroupId)
        {
            var focusedGroup = groups.FirstOrDefault(g => g.Id == focusedGroupId);
            if (focusedGroup is not null && !focusedGroup.DiscussedAt.HasValue)
                focusedGroup.MarkDiscussed();

            sprint.ClearFocus();
        }

        sprint.EnsureEnoughGroupsDiscussed(groups.Count(g => g.DiscussedAt.HasValue));
    }

    private async Task LeaveWritingAsync(Sprint sprint, CancellationToken ct)
    {
        await using var transaction = await sprintRepository.BeginTransactionAsync(ct);

        var cards = await cardRepository.GetBySprintIdAsync(sprint.Id, ct);
        var noteCards = cards.Where(c => c.CardType == CardType.Note).ToList();

        var groups = noteCards.Select(c => CardGroup.CreateSolo(sprint.Id, c.ColumnType)).ToList();
        await cardGroupRepository.AddRangeAsync(groups, ct);
        await sprintRepository.SaveChangesAsync(ct);

        for (var i = 0; i < noteCards.Count; i++)
            noteCards[i].SetGroupId(groups[i].Id);

        await sprintRepository.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
