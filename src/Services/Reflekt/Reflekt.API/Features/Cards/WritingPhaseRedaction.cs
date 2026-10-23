using System.Security.Claims;
using Reflekt.API.Auth;
using Reflekt.API.Features.CardGroups;
using Reflekt.API.Features.Reports;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;

namespace Reflekt.API.Features.Cards;

public static class WritingPhaseRedaction
{
    public static int? GetViewerParticipantId(ClaimsPrincipal user)
        => user.GetParticipantId();

    public static bool IsWritingPhaseCard(Card card, Sprint sprint)
        => card.SprintId == sprint.Id && sprint.CurrentPhase == SprintPhase.Writing;

    public static bool IsRedacted(Card card, Sprint sprint, int? viewerParticipantId)
        => IsWritingPhaseCard(card, sprint)
            && viewerParticipantId is not null
            && card.ParticipantId != viewerParticipantId;

    public static CardResponse Redact(CardResponse card)
        => card with { Text = "", AssigneeName = null, EvidenceLinks = [] };

    public static CardGroupCardResponse Redact(CardGroupCardResponse card)
        => card with { Text = "", EvidenceLinks = [] };

    public static ReportCardResponse Redact(ReportCardResponse card)
        => card with { Text = "", EvidenceLinks = [] };

    public static ReportActionItemResponse Redact(ReportActionItemResponse card)
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
