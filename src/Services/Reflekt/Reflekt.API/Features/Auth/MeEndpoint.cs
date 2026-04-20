using System.Security.Claims;
using FastEndpoints;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Auth;

public sealed record MeResponse(string Email, string DisplayName);

public sealed class MeEndpoint : EndpointWithoutRequest<MeResponse>
{
    public override void Configure()
    {
        Get("/auth/me");
        Policies(AuthPolicies.ScrumMaster);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var displayName = User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(displayName))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        await SendOkAsync(new MeResponse(email, displayName), ct);
    }
}
