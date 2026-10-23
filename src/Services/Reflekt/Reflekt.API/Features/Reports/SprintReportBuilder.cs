using System.Security.Claims;
using Reflekt.API.Features.Cards;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Teams;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Reports;

public sealed class SprintReportBuilder(
    SprintRepository sprintRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository,
    ParticipantRepository participantRepository)
{
    public async Task<ReportResponse> BuildAsync(Sprint sprint, Team team, ClaimsPrincipal viewer, CancellationToken ct)
    {
        var cards = await cardRepository.GetBySprintIdReadOnlyAsync(sprint.Id, ct);
        var groups = await cardGroupRepository.GetBySprintIdReadOnlyAsync(sprint.Id, ct);
        var voteCounts = await voteRepository.GetVoteCountsByGroupIdsAsync(groups.Select(g => g.Id), ct);
        var participants = await participantRepository.GetBySprintIdAsync(sprint.Id, ct);

        var viewerParticipantId = WritingPhaseRedaction.GetViewerParticipantId(viewer);

        var groupEntries = BuildGroupEntries(sprint, cards, groups, voteCounts, viewerParticipantId);

        var columnSections = new List<ReportColumnSectionResponse>
        {
            BuildColumnSection(sprint.Column1Name, sprint.Column1Color.Value, ColumnType.Column1, groupEntries),
            BuildColumnSection(sprint.Column2Name, sprint.Column2Color.Value, ColumnType.Column2, groupEntries),
            BuildColumnSection(sprint.Column3Name, sprint.Column3Color.Value, ColumnType.Column3, groupEntries),
        };

        var actionItems = cards
            .Where(c => c.CardType == CardType.ActionItem)
            .Select(c => WritingPhaseRedaction.IsRedacted(c, sprint, viewerParticipantId)
                ? WritingPhaseRedaction.Redact(c.Adapt<ReportActionItemResponse>())
                : c.Adapt<ReportActionItemResponse>())
            .ToList();

        var previousActionsSection = await BuildPreviousActionsSectionAsync(sprint.Id, sprint.TeamId, ct);

        return new ReportResponse(
            new ReportSprintResponse(sprint.Id, sprint.Name, sprint.CreatedAt, sprint.ClosedAt),
            new ReportTeamResponse(team.Id, team.Name),
            participants.Select(p => new ReportParticipantResponse(p.DisplayName)).ToList(),
            columnSections,
            actionItems,
            previousActionsSection);
    }

    private static List<GroupEntry> BuildGroupEntries(
        Sprint sprint,
        IReadOnlyList<Card> cards,
        IReadOnlyList<CardGroup> groups,
        Dictionary<int, int> voteCounts,
        int? viewerParticipantId)
    {
        var cardsByGroup = cards
            .Where(c => c.CardType == CardType.Note && c.GroupId.HasValue)
            .GroupBy(c => c.GroupId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreatedAt).ToList());

        return groups
            .Select(g =>
            {
                var votes = voteCounts.GetValueOrDefault(g.Id, 0);
                var groupCards = cardsByGroup.GetValueOrDefault(g.Id, []);
                var title = WritingPhaseRedaction.GetReportGroupTitle(g.Title, groupCards.FirstOrDefault(), sprint, viewerParticipantId);
                var cardEntries = groupCards
                    .Select(c => WritingPhaseRedaction.IsRedacted(c, sprint, viewerParticipantId)
                        ? WritingPhaseRedaction.Redact(c.Adapt<ReportCardResponse>())
                        : c.Adapt<ReportCardResponse>())
                    .ToList();
                return new GroupEntry(g, title, votes, cardEntries, g.DiscussedAt.HasValue);
            })
            .Where(x => x.IsDiscussed || x.Votes > 1)
            .OrderByDescending(x => x.Votes)
            .ToList();
    }

    private static ReportColumnSectionResponse BuildColumnSection(
        string columnName,
        string columnColor,
        ColumnType columnType,
        List<GroupEntry> groupEntries)
    {
        var groups = groupEntries
            .Where(x => x.Group.ColumnType == columnType)
            .Select(x => new ReportGroupResponse(x.Title, x.Votes, x.IsDiscussed, x.Cards))
            .ToList();
        return new ReportColumnSectionResponse(columnName, columnColor, groups);
    }

    private async Task<ReportPreviousActionsSectionResponse?> BuildPreviousActionsSectionAsync(int sprintId, int teamId, CancellationToken ct)
    {
        var previousSprint = await sprintRepository.GetMostRecentClosedByTeamIdAsync(teamId, ct);
        if (previousSprint is null) return null;

        var reviewedItems = await cardRepository.GetActionItemsReviewedInSprintAsync(sprintId, ct);
        var unreviewedEligible = await cardRepository.GetUnreviewedActionItemsBySprintIdAsync(previousSprint.Id, ct);

        if (reviewedItems.Count == 0 && unreviewedEligible.Count == 0) return null;

        var entries = new List<ReportPreviousActionResponse>();

        foreach (var card in reviewedItems)
            entries.Add(new ReportPreviousActionResponse(card.Text, card.AssigneeName, card.ReviewStatus.ToString()));

        foreach (var card in unreviewedEligible)
            entries.Add(new ReportPreviousActionResponse(card.Text, card.AssigneeName, "Not reviewed"));

        return new ReportPreviousActionsSectionResponse(previousSprint.Name, entries);
    }

    private sealed record GroupEntry(CardGroup Group, string Title, int Votes, IReadOnlyList<ReportCardResponse> Cards, bool IsDiscussed);
}
