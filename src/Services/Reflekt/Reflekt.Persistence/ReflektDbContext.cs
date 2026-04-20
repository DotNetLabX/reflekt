using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence;

public class ReflektDbContext(DbContextOptions<ReflektDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType).Ignore("DomainEvents");
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReflektDbContext).Assembly);
    }
}
