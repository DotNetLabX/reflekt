using System.Security.Claims;
using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Reflekt.API.Features.Account.Me;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpGet("/auth/me")]
[Tags("Auth")]
public sealed class MeEndpoint : EndpointWithoutRequest<MeResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var displayName = User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(displayName))
            throw new UnauthorizedException("Missing user claims.");

        var givenName = User.FindFirstValue(ClaimTypes.GivenName) ?? displayName;

        await SendOkAsync(new MeResponse(email, displayName, givenName), ct);
    }
}
