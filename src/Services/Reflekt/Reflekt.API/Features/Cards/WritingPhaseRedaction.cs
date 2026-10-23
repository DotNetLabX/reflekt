using System.Security.Claims;
using Reflekt.API.Features.CardGroups;
using Reflekt.API.Features.Reports;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;

namespace Reflekt.API.Features.Cards;

public static class WritingPhaseRedaction
{
    public static int? GetViewerParticipantId(ClaimsPrincipal user)
        => int.TryParse(user.FindFirstValue("participantId"), out var participantId) ? participantId : null;

    public static bool IsWritingPhaseCard(Card card, Sprint sprint)
        => card.SprintId == sprint.Id && sprint.CurrentPhase == SprintPhase.Writing;

    public static bool IsRedacted(Card card, Sprint sprint, int? viewerParticipantId)
        => IsWritingPhaseCard(card, sprint)
            && viewerParticipantId is not null
            && card.ParticipantId != viewerParticipantId;

    public static CardResponse Redact(CardResponse card)
        => card with { Text = "", AssigneeName = null, EvidenceLinks = [] };

    public static CardGroupCardEntry Redact(CardGroupCardEntry card)
        => card with { Text = "", EvidenceLinks = [] };

    public static ReportCardEntry Redact(ReportCardEntry card)
        => card with { Text = "", EvidenceLinks = [] };

    public static ReportActionItemEntry Redact(ReportActionItemEntry card)
        => card with { Text = "", AssigneeName = null, EvidenceLinks = [] };

    public static string GetReportGroupTitle(string? title, Card? firstCard, Sprint sprint, int? viewerParticipantId)
    {
        if (title is not null)
            return title;
        if (firstCard is null || IsRedacted(firstCard, sprint, viewerParticipantId))
            return "";
        return firstCard.Text;
    }
}
