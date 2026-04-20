using BuildingBlocks.Domain;
using BuildingBlocks.Persistence.Repositories;

namespace Reflekt.Persistence.Repositories;

public class Repository<TEntity>(ReflektDbContext db)
    : RepositoryBase<ReflektDbContext, TEntity>(db)
    where TEntity : Entity;
