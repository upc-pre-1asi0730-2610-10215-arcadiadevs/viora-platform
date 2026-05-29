using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

/// <summary>
///     Generic base repository providing CRUD operations for all entity types.
/// </summary>
/// <remarks>
///     This generic repository implements the IBaseRepository contract and provides
///     common CRUD operations against an EF Core DbContext. Derived repositories
///     can extend this class to add entity-specific query methods.
/// </remarks>
/// <typeparam name="TEntity">The entity type that this repository manages.</typeparam>
/// <param name="context">The EF Core database context.</param>
public class BaseRepository<TEntity>(AppDbContext context) : IBaseRepository<TEntity> where TEntity : class
{
    /// <summary>
    ///     The EF Core <see cref="AppDbContext"/> instance available to derived repositories.
    /// </summary>
    protected readonly AppDbContext Context = context;

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
    }

    /// <inheritdoc />
    public void Remove(TEntity entity)
    {
        Context.Set<TEntity>().Remove(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>().ToListAsync(cancellationToken);
    }
}