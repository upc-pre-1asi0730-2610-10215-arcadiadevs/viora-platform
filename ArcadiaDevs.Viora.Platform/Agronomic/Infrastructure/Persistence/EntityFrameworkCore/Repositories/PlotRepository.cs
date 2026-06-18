using System.Collections.Generic;
using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

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

    /// <inheritdoc />
    public async Task<IEnumerable<Plot>> FindAllByOwnerUserIdAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Plot>()
            .AsNoTracking()
            .Where(p => p.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAndOwnerUserIdAsync(
        string plotName,
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Plot>()
            .AnyAsync(
                p => p.PlotName == plotName && p.OwnerUserId == ownerUserId && !p.IsDeleted,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAndOwnerUserIdAndIdIsNotAsync(
        string plotName,
        int ownerUserId,
        int excludePlotId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Plot>()
            .AnyAsync(
                p => p.PlotName == plotName && p.OwnerUserId == ownerUserId && p.Id != excludePlotId && !p.IsDeleted,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasRelatedOperationalRecordsAsync(
        int plotId,
        CancellationToken cancellationToken = default)
    {
        // Check if there are any related operational records
        // For example, IoTDevices, DynamicNutritionPlans, etc.
        // As a simple implementation we will check IoTDevices if they have a PlotId
        // In a real scenario we'd query multiple aggregate roots or use a domain event/read model
        
        bool hasDevices = await Context.Set<IoTDevice>()
            .AnyAsync(d => d.PlotId == plotId, cancellationToken);
            
        // Additional checks can be added here
        
        return hasDevices;
    }
}
