using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;

namespace Reflekt.API.Features.Auth.Login;

[AllowAnonymous]
[HttpGet("/auth/google/login")]
[Tags("Auth")]
public sealed class LoginEndpoint : EndpointWithoutRequest
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var redirectUri = env.IsDevelopment() ? "http://localhost:5173/" : "/";

        var props = new AuthenticationProperties { RedirectUri = redirectUri };
        await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
        await HttpContext.Response.StartAsync(ct);
    }
}
