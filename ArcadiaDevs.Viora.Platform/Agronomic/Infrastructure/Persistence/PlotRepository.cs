using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates.Plot;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence;

/// <summary>
///     Repository implementation for Plot aggregate using Entity Framework Core.
/// </summary>
public class PlotRepository : BaseRepository<Plot>, IPlotRepository
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PlotRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public PlotRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Plot?> FindByIdAndOwnerUserIdAsync(
        int plotId,
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Plot>()
            .FirstOrDefaultAsync(
                p => p.Id == plotId && p.OwnerUserId == ownerUserId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByIdAndOwnerUserIdAsync(
        int plotId,
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Plot>()
            .AnyAsync(
                p => p.Id == plotId && p.OwnerUserId == ownerUserId,
                cancellationToken);
    }
}