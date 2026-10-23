using Blocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Blocks.Persistence.Repositories;

public abstract class RepositoryBase<TContext, TEntity, TId>(TContext db)
    where TContext : DbContext
    where TEntity : Entity<TId>
    where TId : notnull
{
    protected TContext Db { get; } = db;

    public virtual Task AddAsync(TEntity entity, CancellationToken ct)
        => Db.Set<TEntity>().AddAsync(entity, ct).AsTask();

    public virtual void Remove(TEntity entity)
        => Db.Set<TEntity>().Remove(entity);

    public virtual Task<int> SaveChangesAsync(CancellationToken ct)
        => Db.SaveChangesAsync(ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct)
        => Db.Database.BeginTransactionAsync(ct);

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct)
        => await Db.Set<TEntity>().FindAsync([id], ct);

    protected virtual IQueryable<TEntity> Query()
        => Db.Set<TEntity>().AsQueryable();
}

public abstract class RepositoryBase<TContext, TEntity>(TContext db)
    : RepositoryBase<TContext, TEntity, int>(db)
    where TContext : DbContext
    where TEntity : Entity<int>;
