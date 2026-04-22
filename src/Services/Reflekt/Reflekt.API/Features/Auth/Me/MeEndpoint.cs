using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Auth.Me;

public sealed record MeResponse(string Email, string DisplayName, string GivenName);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpGet("/auth/me")]
[Tags("Auth")]
public sealed class MeEndpoint : EndpointWithoutRequest<MeResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var displayName = User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(displayName))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var givenName = User.FindFirstValue(ClaimTypes.GivenName) ?? displayName;

        await SendOkAsync(new MeResponse(email, displayName, givenName), ct);
    }
}
