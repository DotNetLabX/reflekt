using Reflekt.Domain.Cards;

namespace Reflekt.API.Features.Cards;

public sealed record EvidenceLinkResponse(
    int Id,
    int CardId,
    string Url,
    string Source,
    string? Title,
    string? Description,
    string? IconUrl,
    string? ImageUrl,
    string? ExternalKey,
    DateTime? FetchedAt,
    string FetchStatus);

public sealed record CardResponse(
    int Id,
    int SprintId,
    int ParticipantId,
    string ColumnType,
    string CardType,
    string Text,
    string? AssigneeName,
    int? GroupId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int VoteCount = 0,
    List<EvidenceLinkResponse>? EvidenceLinks = null);

public static class CardMappings
{
    public static CardResponse ToResponse(Card c, int voteCount = 0) => new(
        c.Id,
        c.SprintId,
        c.ParticipantId,
        c.ColumnType.ToString(),
        c.CardType.ToString(),
        c.Text,
        c.AssigneeName,
        c.GroupId,
        c.CreatedAt,
        c.UpdatedAt,
        voteCount,
        c.EvidenceLinks.Count > 0
            ? c.EvidenceLinks.Select(ToResponse).ToList()
            : []);

    public static EvidenceLinkResponse ToResponse(EvidenceLink e) => new(
        e.Id,
        e.CardId,
        e.Url,
        e.Source.ToString(),
        e.Title,
        e.Description,
        e.IconUrl,
        e.ImageUrl,
        e.ExternalKey,
        e.FetchedAt,
        e.FetchStatus.ToString());
}
