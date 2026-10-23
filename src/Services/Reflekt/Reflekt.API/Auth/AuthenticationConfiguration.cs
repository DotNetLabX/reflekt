using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;
using Reflekt.Domain.Users;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Auth;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddReflektAuthentication(this IServiceCollection services)
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
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.Scope.Add("profile");
                options.SaveTokens = false;
                options.Events.OnTicketReceived = OnGoogleTicketReceived;
            })
            .AddScheme<AuthenticationSchemeOptions, GuestTokenAuthenticationHandler>(
                GuestTokenDefaults.AuthenticationScheme, _ => { });

        services.AddOptions<GoogleOptions>(GoogleDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthSettings>>((options, authSettings) =>
            {
                options.ClientId = authSettings.Value.Google.ClientId;
                options.ClientSecret = authSettings.Value.Google.ClientSecret;
            });

        return services;
    }

    private static async Task OnGoogleTicketReceived(TicketReceivedContext ctx)
    {
        var principal = ctx.Principal!;
        var email = principal.FindFirstValue(ClaimTypes.Email)!;
        var name = principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var givenName = principal.FindFirstValue(ClaimTypes.GivenName) ?? name;

        var userRepository = ctx.HttpContext.RequestServices.GetRequiredService<UserRepository>();

        var user = await userRepository.GetByEmailAsync(email, ctx.HttpContext.RequestAborted);
        if (user is null)
        {
            user = User.Create(email, name, givenName);
            await userRepository.AddAsync(user, ctx.HttpContext.RequestAborted);
        }
        else
        {
            user.UpdateProfile(name, givenName);
        }

        await userRepository.SaveChangesAsync(ctx.HttpContext.RequestAborted);

        var identity = (ClaimsIdentity)principal.Identity!;
        var existing = identity.FindFirst(ClaimTypes.NameIdentifier);
        if (existing is not null)
            identity.RemoveClaim(existing);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
    }
}
