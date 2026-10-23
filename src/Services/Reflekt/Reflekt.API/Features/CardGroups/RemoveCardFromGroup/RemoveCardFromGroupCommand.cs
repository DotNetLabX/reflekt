namespace Reflekt.API.Features.CardGroups.RemoveCardFromGroup;

public sealed record RemoveCardFromGroupRequest(int SprintId, int GroupId, int CardId);
