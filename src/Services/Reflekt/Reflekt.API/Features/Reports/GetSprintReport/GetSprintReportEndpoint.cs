using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Domain.Cards;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Reports.GetSprintReport;

public sealed record GetSprintReportRequest(int TeamId, int SprintId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/teams/{teamId}/sprints/{sprintId}/report")]
[Tags("Reports")]
public sealed class GetSprintReportEndpoint(
    TeamRepository teamRepository,
    SprintRepository sprintRepository,
    CardRepository cardRepository,
    VoteRepository voteRepository,
    ParticipantRepository participantRepository)
    : Endpoint<GetSprintReportRequest, ReportResponse>
{
    public override async Task HandleAsync(GetSprintReportRequest req, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null || sprint.TeamId != req.TeamId) { await SendNotFoundAsync(ct); return; }

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
            var team = await teamRepository.GetByIdAsync(req.TeamId, ct);
            if (team is null || !string.Equals(team.CreatorEmail, email, StringComparison.OrdinalIgnoreCase))
            {
                await SendForbiddenAsync(ct);
                return;
            }
        }

        var cards = await cardRepository.GetBySprintIdAsync(req.SprintId, ct);
        var voteCounts = await voteRepository.GetVoteCountsByCardIdsAsync(cards.Select(c => c.Id), ct);
        var participants = await participantRepository.GetBySprintIdAsync(req.SprintId, ct);
        var teamInfo = await teamRepository.GetByIdAsync(req.TeamId, ct);

        var notes = cards
            .Where(c => c.CardType == CardType.Note)
            .Select(c => (Card: c, Votes: voteCounts.GetValueOrDefault(c.Id, 0)))
            .Where(x => x.Votes > 0)
            .OrderByDescending(x => x.Votes)
            .ToList();

        var wentWell = notes
            .Where(x => x.Card.ColumnType == ColumnType.WentWell)
            .Select(x => ReportMappings.ToCardEntry(x.Card))
            .ToList();

        var didntGoWell = notes
            .Where(x => x.Card.ColumnType == ColumnType.DidntGoWell)
            .Select(x => ReportMappings.ToCardEntry(x.Card))
            .ToList();

        var actionItems = cards
            .Where(c => c.CardType == CardType.ActionItem)
            .Select(c => ReportMappings.ToActionItemEntry(c))
            .ToList();

        var response = new ReportResponse(
            new ReportSprintInfo(sprint.Id, sprint.Name, sprint.CreatedAt, sprint.ClosedAt),
            new ReportTeamInfo(teamInfo!.Id, teamInfo.Name),
            participants.Select(p => new ReportParticipantInfo(p.DisplayName)).ToList(),
            new ReportNotesSection(wentWell, didntGoWell),
            actionItems);

        await SendOkAsync(response, ct);
    }
}
