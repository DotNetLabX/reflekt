using Blocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace Blocks.Persistence.Repositories;

public abstract class RepositoryBase<TContext, TEntity, TId>(TContext db)
    where TContext : DbContext
    where TEntity : Entity<TId>
    where TId : notnull
{
    protected TContext Db { get; } = db;

    public virtual Task AddAsync(TEntity entity, CancellationToken ct)
        => Db.Set<TEntity>().AddAsync(entity, ct).AsTask();

    public virtual Task<int> SaveChangesAsync(CancellationToken ct)
        => Db.SaveChangesAsync(ct);

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct)
        => await Db.Set<TEntity>().FindAsync([id], ct);

    public virtual IQueryable<TEntity> Query()
        => Db.Set<TEntity>().AsQueryable();

    public virtual async Task<TEntity?> FindByIdAsync(TId id, CancellationToken ct = default)
        => await Db.Set<TEntity>().FindAsync([id], ct);

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken ct = default)
        => await Db.Set<TEntity>().FindAsync([id], ct) is not null;
}

public abstract class RepositoryBase<TContext, TEntity>(TContext db)
    : RepositoryBase<TContext, TEntity, int>(db)
    where TContext : DbContext
    where TEntity : Entity<int>;
