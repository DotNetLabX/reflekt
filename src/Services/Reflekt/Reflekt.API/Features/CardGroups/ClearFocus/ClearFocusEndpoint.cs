using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.ClearFocus;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpDelete("/api/sprints/{sprintId}/focus")]
[Tags("CardGroups")]
public sealed class ClearFocusEndpoint(
    SprintAccess sprintAccess,
    SprintRepository sprintRepository,
    FocusHandover focusHandover,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<ClearFocusRequest, ClearFocusResponse>
{
    public override async Task HandleAsync(ClearFocusRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        access.RequireOwner();

        sprint.EnsureActive("Sprint must be Active.");

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.FocusGroup);

        var handover = await focusHandover.ClearAsync(sprint, ct);
        await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(new ClearFocusResponse(handover.PreviousGroupId, handover.PreviousGroupDiscussed), ct);
    }
}
