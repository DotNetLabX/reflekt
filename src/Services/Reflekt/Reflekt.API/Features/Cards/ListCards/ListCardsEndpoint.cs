using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Cards.ListCards;

public sealed record ListCardsRequest(int SprintId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/sprints/{sprintId}/cards")]
[Tags("Cards")]
public sealed class ListCardsEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    VoteRepository voteRepository)
    : Endpoint<ListCardsRequest, List<CardResponse>>
{
    public override async Task HandleAsync(ListCardsRequest req, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await SendNotFoundAsync(ct); return; }

        var participantIdClaim = User.FindFirstValue("participantId");
        if (participantIdClaim is not null)
        {
            if (!int.TryParse(User.FindFirstValue("sprintId"), out var claimSprintId) || claimSprintId != req.SprintId)
            {
                await SendUnauthorizedAsync(ct);
                return;
            }
        }
        else
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
            if (team is null || !string.Equals(team.CreatorEmail, email, StringComparison.OrdinalIgnoreCase))
            {
                await SendForbiddenAsync(ct);
                return;
            }
        }

        var cards = await cardRepository.GetBySprintIdAsync(req.SprintId, ct);
        var voteCounts = await voteRepository.GetVoteCountsByCardIdsAsync(cards.Select(c => c.Id), ct);

        var requestingParticipantId = participantIdClaim is not null ? int.Parse(participantIdClaim) : (int?)null;
        var redactOthers = sprint.CurrentPhase == SprintPhase.Writing && requestingParticipantId is not null;

        await SendOkAsync(
            cards.Select(c =>
            {
                var response = CardMappings.ToResponse(c, voteCounts.GetValueOrDefault(c.Id, 0));
                if (redactOthers && c.ParticipantId != requestingParticipantId)
                    return response with { Text = "", AssigneeName = null, EvidenceLinks = [] };
                return response;
            }).ToList(),
            ct);
    }
}
