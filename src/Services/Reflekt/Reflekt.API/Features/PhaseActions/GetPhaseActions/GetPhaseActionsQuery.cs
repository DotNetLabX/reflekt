namespace Reflekt.API.Features.PhaseActions.GetPhaseActions;

public sealed record PhaseActionsResponse(string Phase, IReadOnlyList<string> Actions);
