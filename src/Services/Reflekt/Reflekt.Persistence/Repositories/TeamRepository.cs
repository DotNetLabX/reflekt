using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence.Repositories;

public sealed class TeamRepository(ReflektDbContext db)
{
    public async Task AddAsync(Team team, CancellationToken ct)
        => await db.Teams.AddAsync(team, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    public Task<List<Team>> GetByCreatorEmailAsync(string email, CancellationToken ct)
        => db.Teams
            .Where(t => EF.Functions.Collate(t.CreatorEmail, "NOCASE") == email)
            .ToListAsync(ct);

    public Task<bool> ExistsByCreatorEmailAsync(string email, CancellationToken ct)
        => db.Teams
            .AnyAsync(t => EF.Functions.Collate(t.CreatorEmail, "NOCASE") == email, ct);

    public Task<Team?> GetByIdAsync(int id, CancellationToken ct)
        => db.Teams.FirstOrDefaultAsync(t => t.Id == id, ct);
}
