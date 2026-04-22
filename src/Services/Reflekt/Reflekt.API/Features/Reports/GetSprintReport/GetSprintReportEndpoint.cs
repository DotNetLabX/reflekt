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
    CardGroupRepository cardGroupRepository,
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
        var groups = await cardGroupRepository.GetBySprintIdAsync(req.SprintId, ct);
        var voteCounts = await voteRepository.GetVoteCountsByGroupIdsAsync(groups.Select(g => g.Id), ct);
        var participants = await participantRepository.GetBySprintIdAsync(req.SprintId, ct);
        var teamInfo = await teamRepository.GetByIdAsync(req.TeamId, ct);

        var cardsByGroup = cards
            .Where(c => c.CardType == CardType.Note && c.GroupId.HasValue)
            .GroupBy(c => c.GroupId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreatedAt).ToList());

        var groupEntries = groups
            .Select(g =>
            {
                var votes = voteCounts.GetValueOrDefault(g.Id, 0);
                var groupCards = cardsByGroup.GetValueOrDefault(g.Id, []);
                var title = g.Title ?? groupCards.FirstOrDefault()?.Text ?? "";
                var isDiscussed = g.DiscussedAt.HasValue;
                return (Group: g, Title: title, Votes: votes, Cards: groupCards, IsDiscussed: isDiscussed);
            })
            .Where(x => x.IsDiscussed || x.Votes > 1)
            .OrderByDescending(x => x.Votes)
            .ToList();

        var wentWell = groupEntries
            .Where(x => x.Group.ColumnType == ColumnType.WentWell)
            .Select(x => new ReportGroupEntry(x.Title, x.Votes, x.IsDiscussed, x.Cards.Select(ReportMappings.ToCardEntry).ToList()))
            .ToList();

        var didntGoWell = groupEntries
            .Where(x => x.Group.ColumnType == ColumnType.DidntGoWell)
            .Select(x => new ReportGroupEntry(x.Title, x.Votes, x.IsDiscussed, x.Cards.Select(ReportMappings.ToCardEntry).ToList()))
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
