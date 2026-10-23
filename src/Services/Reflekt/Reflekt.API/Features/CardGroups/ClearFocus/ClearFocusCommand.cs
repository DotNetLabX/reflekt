namespace Reflekt.API.Features.CardGroups.ClearFocus;

public sealed record ClearFocusRequest(int SprintId);

public sealed record ClearFocusResponse(int? PreviousGroupId, bool? PreviousGroupDiscussed);
