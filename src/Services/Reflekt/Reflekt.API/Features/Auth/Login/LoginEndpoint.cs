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

        var returnUrl = HttpContext.Request.Query["returnUrl"].FirstOrDefault();
        var redirectUri = ResolveRedirectUri(returnUrl, env.IsDevelopment());

        var props = new AuthenticationProperties { RedirectUri = redirectUri };
        await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
        await HttpContext.Response.StartAsync(ct);
    }

    private static string ResolveRedirectUri(string? returnUrl, bool isDevelopment)
    {
        if (!string.IsNullOrEmpty(returnUrl) && IsValidReturnUrl(returnUrl))
        {
            return isDevelopment ? $"http://localhost:5173{returnUrl}" : returnUrl;
        }

        return isDevelopment ? "http://localhost:5173/" : "/";
    }

    private static bool IsValidReturnUrl(string url)
    {
        var decoded = Uri.UnescapeDataString(url);
        return decoded.StartsWith('/') && !decoded.StartsWith("//") && !decoded.Contains("://");
    }
}
