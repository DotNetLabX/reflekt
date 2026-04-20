using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence.Repositories;

public sealed class TeamRepository(ReflektDbContext db) : Repository<Team>(db)
{
    public Task<List<Team>> GetByCreatorEmailAsync(string email, CancellationToken ct)
        => Db.Teams
            .Where(t => EF.Functions.Collate(t.CreatorEmail, "NOCASE") == email)
            .ToListAsync(ct);

    public Task<bool> ExistsByCreatorEmailAsync(string email, CancellationToken ct)
        => Db.Teams
            .AnyAsync(t => EF.Functions.Collate(t.CreatorEmail, "NOCASE") == email, ct);
}
