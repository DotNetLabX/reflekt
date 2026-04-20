using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Cards;

namespace Reflekt.Persistence.Repositories;

public sealed class CardRepository(ReflektDbContext db) : Repository<Card>(db)
{
    public Task<List<Card>> GetBySprintIdAsync(int sprintId, CancellationToken ct)
        => Db.Cards
            .Where(c => c.SprintId == sprintId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public void Remove(Card card) => Db.Cards.Remove(card);
}
