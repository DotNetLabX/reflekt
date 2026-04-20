using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Persistence;

public abstract class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType).Ignore("DomainEvents");
        }
        base.OnModelCreating(modelBuilder);
    }
}
