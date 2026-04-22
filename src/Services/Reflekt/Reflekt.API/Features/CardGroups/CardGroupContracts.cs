using Reflekt.API.Features.Cards;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;

namespace Reflekt.API.Features.CardGroups;

public sealed record CardGroupCardEntry(int Id, string Text, DateTime CreatedAt, List<EvidenceLinkResponse> EvidenceLinks);

public sealed record CardGroupResponse(int Id, int SprintId, string ColumnType, string? Title, List<CardGroupCardEntry> Cards, int VoteCount);

public static class CardGroupMappings
{
    public static CardGroupResponse ToResponse(CardGroup group, List<Card> cards, int voteCount = 0) => new(
        group.Id,
        group.SprintId,
        group.ColumnType.ToString(),
        group.Title,
        cards.Select(c => new CardGroupCardEntry(
            c.Id,
            c.Text,
            c.CreatedAt,
            c.EvidenceLinks.Select(CardMappings.ToResponse).ToList())).ToList(),
        voteCount);
}
