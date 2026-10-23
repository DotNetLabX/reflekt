using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace Reflekt.API.Features.Account.Logout;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPost("/auth/logout")]
[Tags("Auth")]
public sealed class LogoutEndpoint : EndpointWithoutRequest
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SendNoContentAsync(ct);
    }
}
