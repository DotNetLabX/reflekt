using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Auth.Logout;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
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
