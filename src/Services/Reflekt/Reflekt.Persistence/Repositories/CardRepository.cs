using Reflekt.Domain.Cards;

namespace Reflekt.Persistence.Repositories;

public sealed class CardRepository(ReflektDbContext db) : RepositoryBase<ReflektDbContext, Card>(db)
{
    public override Task<Card?> GetByIdAsync(int id, CancellationToken ct)
        => Db.Cards
            .Include(c => c.EvidenceLinks)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Card>> GetBySprintIdAsync(int sprintId, CancellationToken ct)
        => await Db.Cards
            .Include(c => c.EvidenceLinks)
            .Where(c => c.SprintId == sprintId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Card>> GetBySprintIdReadOnlyAsync(int sprintId, CancellationToken ct)
        => await Db.Cards
            .AsNoTracking()
            .Include(c => c.EvidenceLinks)
            .Where(c => c.SprintId == sprintId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<bool> HasCardsInGroupAsync(
        int sprintId, int groupId, IReadOnlyCollection<int> excludedCardIds, CancellationToken ct)
        => Db.Cards.AnyAsync(
            c => c.SprintId == sprintId && c.GroupId == groupId && !excludedCardIds.Contains(c.Id),
            ct);

    public async Task<IReadOnlyList<Card>> GetUnreviewedActionItemsBySprintIdAsync(int sprintId, CancellationToken ct)
        => await Db.Cards
            .AsNoTracking()
            .Where(c => c.SprintId == sprintId
                && c.CardType == CardType.ActionItem
                && c.ReviewedInSprintId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<int> CountUnreviewedActionItemsBySprintIdAsync(int sprintId, CancellationToken ct)
        => Db.Cards.CountAsync(c => c.SprintId == sprintId
            && c.CardType == CardType.ActionItem
            && c.ReviewedInSprintId == null, ct);

    public async Task<IReadOnlyList<Card>> GetActionItemsReviewedInSprintAsync(int reviewingSprintId, CancellationToken ct)
        => await Db.Cards
            .AsNoTracking()
            .Where(c => c.ReviewedInSprintId == reviewingSprintId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<int> CountActionItemsReviewedInSprintAsync(int reviewingSprintId, CancellationToken ct)
        => Db.Cards.CountAsync(c => c.ReviewedInSprintId == reviewingSprintId, ct);

    public Task<int> CountActionItemsBySprintIdAsync(int sprintId, CancellationToken ct)
        => Db.Cards
            .CountAsync(c => c.SprintId == sprintId && c.CardType == CardType.ActionItem, ct);
}
