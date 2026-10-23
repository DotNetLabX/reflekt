namespace Reflekt.API.Features.CardGroups.FocusGroup;

public sealed record FocusGroupRequest(int SprintId, int GroupId);

public sealed record FocusGroupResponse(int FocusedGroupId, int? PreviousGroupId, bool? PreviousGroupDiscussed);
