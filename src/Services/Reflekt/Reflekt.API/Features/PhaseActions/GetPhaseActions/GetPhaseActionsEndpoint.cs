using Microsoft.AspNetCore.Authorization;

namespace Reflekt.API.Features.PhaseActions.GetPhaseActions;

[AllowAnonymous]
[HttpGet("/api/phase-actions")]
[Tags("PhaseActions")]
public sealed class GetPhaseActionsEndpoint(PhaseActionPolicyProvider phaseActionPolicyProvider)
    : EndpointWithoutRequest<IReadOnlyList<PhaseActionsResponse>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var policy = await phaseActionPolicyProvider.GetAsync();
        var response = policy.GetActionsByPhase()
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new PhaseActionsResponse(kvp.Key.ToString(), kvp.Value.Select(a => a.ToString()).ToList()))
            .ToList();
        await SendOkAsync(response, ct);
    }
}
