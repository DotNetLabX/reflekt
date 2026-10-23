using Reflekt.API.Auth;
using Reflekt.API.Features.CardGroups;
using Reflekt.API.Features.EvidenceLinks;
using Reflekt.API.Features.PhaseActions;
using Reflekt.API.Features.Reports;
using Reflekt.API.Features.Teams;
using Reflekt.API.Features.Teams.CreateTeam;
using Reflekt.API.Features.Votes.CastVote;
using Reflekt.API.Hubs;
using Reflekt.API.Middleware;
using Reflekt.API.OpenGraph;
using Scalar.AspNetCore;

namespace Reflekt.API;

public static class DependencyInjection
{
    public static IServiceCollection AddReflektApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFastEndpoints();
        services.AddSignalR();
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
        services.AddOpenApi();

        services.AddReflektOptions(configuration);
        services.AddReflektAuthentication();
        services.AddReflektAuthorization();
        services.AddReflektOpenGraph();
        services.AddSingleton<PhaseActionPolicyProvider>();
        services.AddScoped<SprintAccess>();
        services.AddScoped<TeamLimitCheck>();
        services.AddScoped<CardRegrouping>();
        services.AddScoped<FocusHandover>();
        services.AddScoped<VoteBudget>();
        services.AddScoped<SprintReportBuilder>();
        services.AddSingleton<TimerService>();
        services.AddSingleton<ReadinessTracker>();

        return services;
    }

    private static void AddReflektOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthSettings>()
            .Bind(configuration.GetSection("Auth"))
            .Validate(
                s => !string.IsNullOrEmpty(s.Google.ClientId) && !string.IsNullOrEmpty(s.Google.ClientSecret),
                "Auth:Google:ClientId and Auth:Google:ClientSecret must be set.")
            .ValidateOnStart();

        services.AddOptions<TeamSettings>()
            .Bind(configuration.GetSection("TeamSettings"))
            .Validate(s => s.MaxTeamsPerUser >= 1, "TeamSettings:MaxTeamsPerUser must be at least 1.")
            .ValidateOnStart();

        services.AddOptions<SeedOptions>()
            .Bind(configuration.GetSection("Seed"))
            .ValidateOnStart();

        services.AddOptions<EvidenceLinkSettings>()
            .Bind(configuration.GetSection("EvidenceLinks"))
            .ValidateOnStart();
    }

    public static WebApplication UseReflektApi(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapFastEndpoints(config =>
        {
            config.Errors.ResponseBuilder = GlobalExceptionMiddleware.BuildEndpointValidationResponse;
            config.Errors.ProducesMetadataType = typeof(GlobalExceptionMiddleware.ErrorResponse);
        });
        app.MapHub<SprintHub>("/hubs/sprint");

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapFallbackToFile("index.html");

        return app;
    }
}
