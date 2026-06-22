using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class AgronomicStatisticRepository : BaseRepository<AgronomicStatistic>, IAgronomicStatisticRepository
{
    public AgronomicStatisticRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AgronomicStatistic>> FindAllByUserIdAndDateBetweenAsync(
        long userId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<AgronomicStatistic>()
            .Where(s => s.UserId == userId && s.MeasurementDate >= startDate && s.MeasurementDate <= endDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AgronomicStatistic>> FindAllByUserIdAndPlotIdAndDateBetweenAsync(
        long userId,
        long plotId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<AgronomicStatistic>()
            .Where(s => s.UserId == userId && s.PlotId == plotId && s.MeasurementDate >= startDate && s.MeasurementDate <= endDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<AgronomicStatistic?> FindByPlotIdAndDateAsync(
        long plotId,
        DateTimeOffset date,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<AgronomicStatistic>()
            .Where(s => s.PlotId == plotId && s.MeasurementDate.Date == date.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AgronomicStatistic?> FindLatestByPlotIdAsync(
        long plotId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<AgronomicStatistic>()
            .Where(s => s.PlotId == plotId)
            .OrderByDescending(s => s.MeasurementDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
