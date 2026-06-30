using System.Collections.Generic;
using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

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

    /// <summary>
    ///     Checks whether the plot has any related operational records.
    /// </summary>
    /// <remarks>
    ///     A3 — short-circuits across the 3 intra-BC aggregates that own a
    ///     <c>PlotId</c> foreign key (<c>IoTDevice</c>, <c>DynamicNutritionPlan</c>,
    ///     <c>AgronomicStatistic</c>). Cross-BC checks (Surveillance <c>Alert</c>,
    ///     Surveillance <c>PestSightingReport</c>) are intentionally OUT of scope
    ///     for this PR (see locked decision #2 in engram #42 — fail-open
    ///     <c>return false</c>, status quo).
    ///     <para>
    ///     TODO AGRONOMIC-A3-CROSSBC: when SHARED-015 (<c>IAgronomicContextFacade</c>)
    ///     lands, replace this method's body with calls to
    ///     <c>IAgronomicContextFacade.AlertsExistForPlotAsync</c> and
    ///     <c>IAgronomicContextFacade.PestSightingReportsExistForPlotAsync</c> in
    ///     addition to the 3 intra-BC checks. Until then, plots with cross-BC
    ///     records will be physically deleted and leave orphan FK references in
    ///     Surveillance — this is the accepted limitation per engram #42.
    ///     </para>
    /// </remarks>
    public async Task<bool> HasRelatedOperationalRecordsAsync(
        int plotId,
        CancellationToken cancellationToken = default)
    {
        bool hasDevices = await Context.Set<IoTDevice>()
            .AnyAsync(d => d.PlotId == plotId, cancellationToken);

        if (hasDevices)
        {
            return true;
        }

        bool hasNutritionPlans = await Context.Set<DynamicNutritionPlan>()
            .AnyAsync(p => p.PlotId == plotId, cancellationToken);

        if (hasNutritionPlans)
        {
            return true;
        }

        bool hasAgronomicStatistics = await Context.Set<AgronomicStatistic>()
            .AnyAsync(s => s.PlotId == plotId, cancellationToken);

        return hasAgronomicStatistics;
    }
}
