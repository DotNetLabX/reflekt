using Reflekt.Domain.Sprints;

namespace Reflekt.Persistence.Repositories;

public sealed class SprintRepository(ReflektDbContext db) : RepositoryBase<ReflektDbContext, Sprint>(db)
{
    public Task<Sprint?> GetByIdReadOnlyAsync(int id, CancellationToken ct)
        => Db.Sprints
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Sprint>> GetByTeamIdAsync(int teamId, CancellationToken ct)
        => await Db.Sprints
            .AsNoTracking()
            .Where(s => s.TeamId == teamId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public Task<Sprint?> GetMostRecentClosedByTeamIdAsync(int teamId, CancellationToken ct)
        => Db.Sprints
            .AsNoTracking()
            .Where(s => s.TeamId == teamId && s.State == SprintState.Closed)
            .OrderByDescending(s => s.ClosedAt)
            .FirstOrDefaultAsync(ct);

    public Task<bool> HasActiveSprintAsync(int teamId, CancellationToken ct)
        => Db.Sprints.AnyAsync(s => s.TeamId == teamId && s.State == SprintState.Active, ct);
}
