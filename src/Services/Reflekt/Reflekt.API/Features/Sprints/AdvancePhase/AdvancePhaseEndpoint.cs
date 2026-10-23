using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Sprints.AdvancePhase;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/teams/{teamId}/sprints/{sprintId}/advance-phase")]
[Tags("Sprints")]
public sealed class AdvancePhaseEndpoint(
    TeamRepository teamRepository,
    SprintRepository sprintRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<SprintRouteRequest, SprintResponse>
{
    public override async Task HandleAsync(SprintRouteRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null) { await SendNotFoundAsync(ct); return; }

        if (team.OwnerId != userId)
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null || sprint.TeamId != request.TeamId) { await SendNotFoundAsync(ct); return; }

        if (sprint.State != SprintState.Active)
        {
            AddError("Sprint must be Active to advance phase.");
            await SendErrorsAsync(409, ct);
            return;
        }

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.AdvancePhase))
            ThrowError($"{SprintAction.AdvancePhase} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        var previousPhase = sprint.CurrentPhase;

        if (previousPhase == SprintPhase.Discussing)
        {
            var groups = await cardGroupRepository.GetBySprintIdAsync(request.SprintId, ct);

            if (sprint.FocusedGroupId.HasValue)
            {
                var focusedGroup = groups.FirstOrDefault(g => g.Id == sprint.FocusedGroupId.Value);
                if (focusedGroup is not null && !focusedGroup.DiscussedAt.HasValue)
                    focusedGroup.MarkDiscussed();

                sprint.ClearFocus();
            }

            var discussedCount = groups.Count(g => g.DiscussedAt.HasValue);
            if (discussedCount < 2)
            {
                AddError("At least 2 groups must be discussed before advancing to Reporting.");
                await SendErrorsAsync(409, ct);
                return;
            }
        }

        sprint.AdvancePhase();

        if (previousPhase == SprintPhase.Writing)
            await LeaveWritingAsync(sprint, ct);
        else
            await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(sprint.Adapt<SprintResponse>(), ct);
    }

    private async Task LeaveWritingAsync(Sprint sprint, CancellationToken ct)
    {
        await using var transaction = await sprintRepository.BeginTransactionAsync(ct);

        var cards = await cardRepository.GetBySprintIdAsync(sprint.Id, ct);
        var noteCards = cards.Where(c => c.CardType == CardType.Note).ToList();

        var groups = noteCards.Select(c => CardGroup.Create(sprint.Id, c.ColumnType)).ToList();
        foreach (var g in groups) g.ClearDomainEvents();
        await cardGroupRepository.AddRangeAsync(groups, ct);
        await sprintRepository.SaveChangesAsync(ct);

        for (var i = 0; i < noteCards.Count; i++)
            noteCards[i].SetGroupId(groups[i].Id);

        await sprintRepository.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
