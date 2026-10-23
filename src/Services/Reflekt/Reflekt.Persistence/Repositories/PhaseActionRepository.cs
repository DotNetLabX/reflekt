using Reflekt.Domain.Sprints;

namespace Reflekt.Persistence.Repositories;

public sealed class PhaseActionRepository(ReflektDbContext db)
{
    public async Task<IReadOnlyList<PhaseAction>> GetAllAsync(CancellationToken ct)
        => await db.PhaseActions.AsNoTracking().ToListAsync(ct);
}
