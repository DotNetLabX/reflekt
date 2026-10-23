using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups;

public sealed record FocusHandoverResult(int? PreviousGroupId, bool? PreviousGroupDiscussed);

public sealed class FocusHandover(CardGroupRepository cardGroupRepository)
{
    public async Task<FocusHandoverResult> FocusAsync(Sprint sprint, int groupId, CancellationToken ct)
    {
        var result = await MarkPreviousDiscussedAsync(sprint, groupId, ct);
        sprint.SetFocus(groupId);
        return result;
    }

    public async Task<FocusHandoverResult> ClearAsync(Sprint sprint, CancellationToken ct)
    {
        var result = await MarkPreviousDiscussedAsync(sprint, nextGroupId: null, ct);
        sprint.ClearFocus();
        return result;
    }

    private async Task<FocusHandoverResult> MarkPreviousDiscussedAsync(
        Sprint sprint, int? nextGroupId, CancellationToken ct)
    {
        var previousGroupId = sprint.FocusedGroupId;
        if (previousGroupId is null || previousGroupId == nextGroupId)
            return new FocusHandoverResult(previousGroupId, PreviousGroupDiscussed: null);

        var previousGroup = await cardGroupRepository.GetByIdAsync(previousGroupId.Value, ct);
        if (previousGroup is null)
            return new FocusHandoverResult(previousGroupId, PreviousGroupDiscussed: null);

        previousGroup.MarkDiscussed();
        return new FocusHandoverResult(previousGroupId, PreviousGroupDiscussed: true);
    }
}
