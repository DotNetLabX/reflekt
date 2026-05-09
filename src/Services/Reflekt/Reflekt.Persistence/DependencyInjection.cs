using Blocks.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reflekt.Persistence.Repositories;

namespace Reflekt.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddReflektPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<DomainEventPublishingInterceptor>();

        services.AddDbContext<ReflektDbContext>((sp, options) =>
        {
            options.UseSqlite(configuration.GetConnectionString("Reflekt"));
            options.AddInterceptors(sp.GetRequiredService<DomainEventPublishingInterceptor>());
        });

        services.AddScoped<TeamRepository>();
        services.AddScoped<SprintRepository>();
        services.AddScoped<ParticipantRepository>();
        services.AddScoped<CardRepository>();
        services.AddScoped<VoteRepository>();
        services.AddScoped<CardGroupRepository>();

        return services;
    }

    public static WebApplication UseReflektPersistence(this WebApplication app)
    {
        var connectionString = app.Configuration.GetConnectionString("Reflekt")!;
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReflektDbContext>();
        db.Database.Migrate();

        if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Seed:Enabled"))
            DbInitializer.SeedAsync(db, app.Configuration).GetAwaiter().GetResult();

        return app;
    }
}
