namespace Reflekt.API.Features.Reports;

public sealed record ReportResponse(
    ReportSprintResponse Sprint,
    ReportTeamResponse Team,
    IReadOnlyList<ReportParticipantResponse> Participants,
    IReadOnlyList<ReportColumnSectionResponse> Columns,
    IReadOnlyList<ReportActionItemResponse> ActionItems,
    ReportPreviousActionsSectionResponse? PreviousActionsReview = null);

public sealed record ReportSprintResponse(int Id, string Name, DateTime CreatedAt, DateTime? ClosedAt);

public sealed record ReportTeamResponse(int Id, string Name);

public sealed record ReportParticipantResponse(string DisplayName);

public sealed record ReportColumnSectionResponse(string ColumnName, string ColumnColor, IReadOnlyList<ReportGroupResponse> Groups);

public sealed record ReportGroupResponse(string GroupTitle, int VoteCount, bool IsDiscussed, IReadOnlyList<ReportCardResponse> Cards);

public sealed record ReportCardResponse(string Text, IReadOnlyList<ReportEvidenceLinkResponse> EvidenceLinks);

public sealed record ReportActionItemResponse(string Text, string? AssigneeName, IReadOnlyList<ReportEvidenceLinkResponse> EvidenceLinks);

public sealed record ReportEvidenceLinkResponse(string Url, string Source, string? Title, string? ExternalKey);

public sealed record ReportPreviousActionsSectionResponse(string PreviousSprintName, IReadOnlyList<ReportPreviousActionResponse> Items);

public sealed record ReportPreviousActionResponse(string Text, string? AssigneeName, string ReviewStatus);
