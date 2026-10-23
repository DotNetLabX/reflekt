using Reflekt.API.Features.Cards;

namespace Reflekt.API.Features.CardGroups;

public sealed record CardGroupCardResponse(int Id, string Text, DateTime CreatedAt, IReadOnlyList<EvidenceLinkResponse> EvidenceLinks);

public sealed record CardGroupResponse(int Id, int SprintId, string ColumnType, string? Title, IReadOnlyList<CardGroupCardResponse> Cards, int VoteCount, bool IsDiscussed, bool IsFocused);

