using Reflekt.Domain.Users;

namespace Reflekt.Persistence.Repositories;

public sealed class UserRepository(ReflektDbContext db) : RepositoryBase<ReflektDbContext, User>(db)
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => Db.Users
            .FirstOrDefaultAsync(u => EF.Functions.Collate(u.Email, "NOCASE") == email, ct);
}
