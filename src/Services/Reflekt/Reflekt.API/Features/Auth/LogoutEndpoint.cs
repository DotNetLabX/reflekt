using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Auth;

public sealed class LogoutEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/auth/logout");
        Policies(AuthPolicies.ScrumMaster);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SendNoContentAsync(ct);
    }
}
