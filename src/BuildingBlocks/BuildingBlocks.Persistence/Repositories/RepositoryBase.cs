using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Persistence.Repositories;

public abstract class RepositoryBase<TContext, TEntity>(TContext db)
    where TContext : DbContext
    where TEntity : Entity
{
    protected TContext Db { get; } = db;

    public virtual Task AddAsync(TEntity entity, CancellationToken ct)
        => Db.Set<TEntity>().AddAsync(entity, ct).AsTask();

    public virtual Task<int> SaveChangesAsync(CancellationToken ct)
        => Db.SaveChangesAsync(ct);

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct)
        => await Db.Set<TEntity>().FindAsync([id], ct);
}
