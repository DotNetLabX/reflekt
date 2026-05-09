using Blocks.Domain;
using Blocks.Persistence.Repositories;

namespace Reflekt.Persistence.Repositories;

public class Repository<TEntity>(ReflektDbContext db)
    : RepositoryBase<ReflektDbContext, TEntity>(db)
    where TEntity : Entity<int>;

public class Repository<TEntity, TId>(ReflektDbContext db)
    : RepositoryBase<ReflektDbContext, TEntity, TId>(db)
    where TEntity : Entity<TId>
    where TId : notnull;
