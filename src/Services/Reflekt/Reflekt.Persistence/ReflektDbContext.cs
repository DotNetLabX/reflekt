using BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence;

public class ReflektDbContext(DbContextOptions<ReflektDbContext> options) : ApplicationDbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<Participant> Participants => Set<Participant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReflektDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
