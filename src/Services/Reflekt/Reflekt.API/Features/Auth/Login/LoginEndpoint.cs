using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;

namespace Reflekt.API.Features.Auth.Login;

[AllowAnonymous]
[HttpGet("/auth/google/login")]
[Tags("Auth")]
public sealed class LoginEndpoint(IWebHostEnvironment environment, IConfiguration configuration)
    : EndpointWithoutRequest
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var returnUrl = HttpContext.Request.Query["returnUrl"].FirstOrDefault();
        var redirectUri = ResolveRedirectUri(returnUrl);

        var props = new AuthenticationProperties { RedirectUri = redirectUri };
        await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
        await HttpContext.Response.StartAsync(ct);
    }

    public static bool IsSafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl)
        && IsSafeAsReceived(returnUrl)
        && IsSafeWhenDecoded(returnUrl);

    private string ResolveRedirectUri(string? returnUrl)
    {
        var isDevelopment = environment.IsDevelopment();
        var devFrontendOrigin = configuration["Auth:DevFrontendOrigin"];

        if (IsSafeReturnUrl(returnUrl))
        {
            return isDevelopment ? $"{devFrontendOrigin}{returnUrl}" : returnUrl!;
        }

        return isDevelopment ? $"{devFrontendOrigin}/" : "/";
    }

    private static bool IsSafeAsReceived(string url) =>
        url.StartsWith('/')
        && (url.Length == 1 || url[1] is not ('/' or '\\'))
        && !url.Any(c => c < ' ' || c == '\u007F')
        && !url.Contains("://");

    private static bool IsSafeWhenDecoded(string url)
    {
        var decoded = Uri.UnescapeDataString(url);
        return decoded.StartsWith('/') && !decoded.StartsWith("//") && !decoded.Contains("://");
    }
}
