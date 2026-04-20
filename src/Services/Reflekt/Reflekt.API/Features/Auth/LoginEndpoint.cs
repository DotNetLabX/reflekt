using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace Reflekt.API.Features.Auth;

public sealed class LoginEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/auth/google/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var props = new AuthenticationProperties { RedirectUri = "/" };
        await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
        await HttpContext.Response.StartAsync(ct);
    }
}
