using Microsoft.AspNetCore.Authentication.Cookies;

namespace Reflekt.API.Auth;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddReflektAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.SignedInUser, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .AddPolicy(Policies.Guest, policy => policy
                .AddAuthenticationSchemes(GuestTokenDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .AddPolicy(Policies.SignedInUserOrGuest, policy => policy
                .AddAuthenticationSchemes(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    GuestTokenDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());

        return services;
    }
}
