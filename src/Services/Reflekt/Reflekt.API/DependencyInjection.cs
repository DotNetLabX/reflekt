using FastEndpoints;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.API.Features.Sprints.Timer;
using Reflekt.API.Features.Teams;
using Reflekt.API.Hubs;
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
        Mapster.TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
        services.AddSingleton(Mapster.TypeAdapterConfig.GlobalSettings);
        services.AddScoped<MapsterMapper.IMapper, MapsterMapper.ServiceMapper>();
        services.AddOpenApi();

        services.Configure<SeedOptions>(configuration.GetSection("Seed"));
        services.Configure<TeamSettings>(configuration.GetSection("TeamSettings"));

        services.AddReflektAuthentication(configuration);
        services.AddReflektAuthorization();
        services.AddReflektOpenGraph();
        services.AddSingleton<PhaseActionCache>();
        services.AddSingleton<TimerService>();

        return services;
    }

    public static WebApplication UseReflektApi(this WebApplication app)
    {
        app.UseMiddleware<Reflekt.API.Middleware.GlobalExceptionMiddleware>();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapFastEndpoints();
        app.MapHub<SprintHub>("/hubs/sprint");

        // Wire timer expiry callback: on expiry, broadcast TimerUpdated(expired) to sprint group
        var timerService = app.Services.GetRequiredService<TimerService>();
        var hubContext = app.Services.GetRequiredService<IHubContext<SprintHub>>();
        timerService.RegisterExpiryCallback(async sprintId =>
        {
            await hubContext.Clients.Group($"sprint:{sprintId}").SendAsync("TimerUpdated", new
            {
                status = "expired",
                remainingSeconds = 0,
                endTimeUtc = (DateTime?)null,
            });
        });

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapFallbackToFile("index.html");

        return app;
    }
}
