using Reflekt.Domain.Cards;

namespace Reflekt.API.Features.Reports;

public sealed record ReportResponse(
    ReportSprintInfo Sprint,
    ReportTeamInfo Team,
    List<ReportParticipantInfo> Participants,
    ReportNotesSection Notes,
    List<ReportActionItemEntry> ActionItems);

public sealed record ReportSprintInfo(int Id, string Name, DateTime CreatedAt, DateTime? ClosedAt);

public sealed record ReportTeamInfo(int Id, string Name);

public sealed record ReportParticipantInfo(string DisplayName);

public sealed record ReportNotesSection(
    List<ReportCardEntry> WentWell,
    List<ReportCardEntry> DidntGoWell);

public sealed record ReportCardEntry(string Text, List<ReportEvidenceLinkEntry> EvidenceLinks);

public sealed record ReportActionItemEntry(string Text, string? AssigneeName, List<ReportEvidenceLinkEntry> EvidenceLinks);

public sealed record ReportEvidenceLinkEntry(string Url, string Source, string? Title, string? ExternalKey);

public static class ReportMappings
{
    public static ReportCardEntry ToCardEntry(Card c) => new(
        c.Text,
        c.EvidenceLinks.Select(ToLinkEntry).ToList());

    public static ReportActionItemEntry ToActionItemEntry(Card c) => new(
        c.Text,
        c.AssigneeName,
        c.EvidenceLinks.Select(ToLinkEntry).ToList());

    public static ReportEvidenceLinkEntry ToLinkEntry(EvidenceLink e) => new(
        e.Url,
        e.Source.ToString(),
        e.Title,
        e.ExternalKey);
}
