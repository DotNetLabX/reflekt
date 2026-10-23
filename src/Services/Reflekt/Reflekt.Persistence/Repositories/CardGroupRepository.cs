using Reflekt.Domain.CardGroups;

namespace Reflekt.Persistence.Repositories;

public sealed class CardGroupRepository(ReflektDbContext db) : RepositoryBase<ReflektDbContext, CardGroup>(db)
{
    public async Task<IReadOnlyList<CardGroup>> GetBySprintIdAsync(int sprintId, CancellationToken ct)
        => await Db.CardGroups
            .Where(g => g.SprintId == sprintId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CardGroup>> GetBySprintIdReadOnlyAsync(int sprintId, CancellationToken ct)
        => await Db.CardGroups
            .AsNoTracking()
            .Where(g => g.SprintId == sprintId)
            .ToListAsync(ct);

    public Task<int> CountDiscussedBySprintIdAsync(int sprintId, CancellationToken ct)
        => Db.CardGroups.CountAsync(g => g.SprintId == sprintId && g.DiscussedAt != null, ct);

    public Task AddRangeAsync(IEnumerable<CardGroup> groups, CancellationToken ct)
        => Db.CardGroups.AddRangeAsync(groups, ct);
}
