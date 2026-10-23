using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Reflekt.Persistence.Repositories;

namespace Reflekt.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddReflektPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<DomainEventPublishingInterceptor>();

        services.AddDbContext<ReflektDbContext>((sp, options) =>
        {
            options.UseSqlite(configuration.GetConnectionString("Reflekt"));
            options.AddInterceptors(sp.GetRequiredService<DomainEventPublishingInterceptor>());
        });

        services.AddScoped<UserRepository>();
        services.AddScoped<TeamRepository>();
        services.AddScoped<SprintRepository>();
        services.AddScoped<ParticipantRepository>();
        services.AddScoped<CardRepository>();
        services.AddScoped<VoteRepository>();
        services.AddScoped<CardGroupRepository>();
        services.AddScoped<PhaseActionRepository>();

        return services;
    }

    public static async Task MigrateAndSeedAsync(this IServiceProvider services, CancellationToken ct)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("Reflekt")!;
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReflektDbContext>();
        await db.Database.MigrateAsync(ct);

        var environment = services.GetRequiredService<IHostEnvironment>();
        var seedOptions = services.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (environment.IsDevelopment() && seedOptions.Enabled)
            await DbInitializer.SeedAsync(db, seedOptions, ct);
    }
}
