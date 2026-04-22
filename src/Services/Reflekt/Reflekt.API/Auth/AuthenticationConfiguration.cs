using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace Reflekt.API.Auth;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddReflektAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = configuration["Auth:Google:ClientId"] ?? "";
                options.ClientSecret = configuration["Auth:Google:ClientSecret"] ?? "";
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.Scope.Add("profile");
                options.SaveTokens = false;
                options.Events.OnTicketReceived = OnGoogleTicketReceived;
            })
            .AddScheme<AuthenticationSchemeOptions, GuestTokenAuthenticationHandler>(
                GuestTokenDefaults.AuthenticationScheme, _ => { });

        return services;
    }

    private static Task OnGoogleTicketReceived(TicketReceivedContext ctx)
    {
        return Task.CompletedTask;
    }
}
