using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;

public interface IAgronomicStatisticRepository : IBaseRepository<AgronomicStatistic>
{
    Task<IEnumerable<AgronomicStatistic>> FindAllByUserIdAndDateBetweenAsync(
        long userId, 
        DateTimeOffset startDate, 
        DateTimeOffset endDate, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<AgronomicStatistic>> FindAllByUserIdAndPlotIdAndDateBetweenAsync(
        long userId, 
        long plotId, 
        DateTimeOffset startDate, 
        DateTimeOffset endDate, 
        CancellationToken cancellationToken = default);

    Task<AgronomicStatistic?> FindByPlotIdAndDateAsync(
        long plotId, 
        DateTimeOffset date, 
        CancellationToken cancellationToken = default);

    Task<AgronomicStatistic?> FindLatestByPlotIdAsync(
        long plotId, 
        CancellationToken cancellationToken = default);
}
