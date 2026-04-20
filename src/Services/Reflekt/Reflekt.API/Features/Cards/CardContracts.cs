using Reflekt.Domain.Cards;

namespace Reflekt.API.Features.Cards;

public sealed record CardResponse(
    int Id,
    int SprintId,
    int ParticipantId,
    string ColumnType,
    string CardType,
    string Text,
    string? AssigneeName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int VoteCount = 0);

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
        c.CreatedAt,
        c.UpdatedAt,
        voteCount);
}
