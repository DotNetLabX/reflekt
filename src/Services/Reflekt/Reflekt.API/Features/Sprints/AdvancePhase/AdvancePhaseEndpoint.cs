using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
        var email = User.FindFirstValue(ClaimTypes.Email);

        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null) { await SendNotFoundAsync(ct); return; }

        if (!string.Equals(team.CreatorEmail, email, StringComparison.OrdinalIgnoreCase))
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
        sprint.AdvancePhase();

        if (previousPhase == SprintPhase.Writing)
        {
            var cards = await cardRepository.GetBySprintIdAsync(request.SprintId, ct);
            var noteCards = cards.Where(c => c.CardType == CardType.Note).ToList();

            var groups = noteCards.Select(c => CardGroup.Create(sprint.Id, c.ColumnType)).ToList();
            foreach (var g in groups) g.ClearDomainEvents();
            await cardGroupRepository.AddRangeAsync(groups, ct);
            await sprintRepository.SaveChangesAsync(ct);

            for (var i = 0; i < noteCards.Count; i++)
                noteCards[i].SetGroupId(groups[i].Id);
        }

        await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(SprintMappings.ToResponse(sprint), ct);
    }
}
