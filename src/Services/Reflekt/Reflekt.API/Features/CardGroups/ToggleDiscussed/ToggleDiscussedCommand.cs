namespace Reflekt.API.Features.CardGroups.ToggleDiscussed;

public sealed record ToggleDiscussedRequest(int SprintId, int GroupId);

public sealed record ToggleDiscussedResponse(int GroupId, bool IsDiscussed, DateTime? DiscussedAt);
