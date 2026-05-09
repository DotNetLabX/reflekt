using Blocks.Core;
using Blocks.Domain;
using Blocks.Exceptions;
using Blocks.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blocks.Persistence;

public static class RepositoryExtensions
{
    public static async Task<TEntity> FindByIdOrThrowAsync<TEntity, TContext>(
        this RepositoryBase<TContext, TEntity> repository, int id)
        where TContext : DbContext
        where TEntity : Entity<int>
        => Guard.NotFound(await repository.FindByIdAsync(id));

    public static async Task<TEntity> GetByIdOrThrowAsync<TEntity, TContext>(
        this RepositoryBase<TContext, TEntity> repository, int id, CancellationToken ct = default)
        where TContext : DbContext
        where TEntity : Entity<int>
        => Guard.NotFound(await repository.GetByIdAsync(id, ct));

    public static async Task ExistsOrThrowAsync<TEntity, TContext>(
        this RepositoryBase<TContext, TEntity> repository, int id, CancellationToken ct = default)
        where TContext : DbContext
        where TEntity : Entity<int>
    {
        if (!await repository.ExistsAsync(id, ct))
            throw new NotFoundException($"{typeof(TEntity).Name}({id}) doesn't exist");
    }

    public static async Task EnsureNotExistsOrThrowAsync<TEntity, TContext>(
        this RepositoryBase<TContext, TEntity> repository, int id, CancellationToken ct = default)
        where TContext : DbContext
        where TEntity : Entity<int>
    {
        if (await repository.ExistsAsync(id, ct))
            throw new BadRequestException($"{typeof(TEntity).Name}({id}) already exists");
    }
}
