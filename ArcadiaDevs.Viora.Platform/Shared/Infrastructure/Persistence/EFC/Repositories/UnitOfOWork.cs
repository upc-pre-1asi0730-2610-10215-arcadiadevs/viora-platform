using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

/// <summary>
///     Unit of work implementation for coordinating database transactions.
/// </summary>
/// <remarks>
///     This class implements the IUnitOfWork contract and encapsulates EF Core's
///     SaveChangesAsync method. It represents a single database transaction scope,
///     ensuring that all pending entity changes are persisted atomically.
/// </remarks>
/// <param name="context">The EF Core database context.</param>
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    /// <inheritdoc />
    /// <remarks>
    ///     Persists all pending entity changes (Add, Modify, Remove) to the database
    ///     in a single atomic transaction.
    /// </remarks>
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}