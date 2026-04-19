using Microsoft.EntityFrameworkCore;

namespace Reflekt.Persistence;

public class ReflektDbContext(DbContextOptions<ReflektDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReflektDbContext).Assembly);
}
