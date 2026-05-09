using FastEndpoints;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
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

        services.AddReflektAuthentication(configuration);
        services.AddReflektAuthorization();
        services.AddReflektOpenGraph();
        services.AddSingleton<PhaseActionCache>();

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

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapFallbackToFile("index.html");

        return app;
    }
}
