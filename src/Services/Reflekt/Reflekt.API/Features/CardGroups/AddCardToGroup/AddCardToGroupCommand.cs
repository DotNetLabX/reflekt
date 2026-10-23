namespace Reflekt.API.Features.CardGroups.AddCardToGroup;

public sealed record AddCardToGroupRequest(int SprintId, int GroupId, int CardId);
