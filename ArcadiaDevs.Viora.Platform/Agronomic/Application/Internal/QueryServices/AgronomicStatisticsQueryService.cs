using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Implementation of agronomic statistics query service.
/// </summary>
public class AgronomicStatisticsQueryService : IAgronomicStatisticsQueryService
{
    private readonly IAgronomicStatisticRepository _agronomicStatisticRepository;
    private readonly IPlotRepository _plotRepository;
    private readonly ArcadiaDevs.Viora.Platform.Shared.Domain.IClock _clock;

    public AgronomicStatisticsQueryService(
        IAgronomicStatisticRepository agronomicStatisticRepository,
        IPlotRepository plotRepository,
        ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock)
    {
        _agronomicStatisticRepository = agronomicStatisticRepository;
        _plotRepository = plotRepository;
        _clock = clock;
    }

    public async Task<Result<IEnumerable<AgronomicStatistic>, Error>> Handle(
        GetAgronomicStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId != query.AuthenticatedUserId)
        {
            return new Result<IEnumerable<AgronomicStatistic>, Error>.Failure(
                AgronomicErrors.AgronomicStatisticsAccess
            );
        }

        var today = new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero);
        var dateRange = query.TimeRange.ToDateRange(today);

        if (!query.PlotId.HasValue)
        {
            var statistics = await _agronomicStatisticRepository.FindAllByUserIdAndDateBetweenAsync(
                query.UserId, dateRange.StartDate, dateRange.EndDate, cancellationToken);
            return new Result<IEnumerable<AgronomicStatistic>, Error>.Success(statistics);
        }

        var plotIdInt = (int)query.PlotId.Value;
        var plot = await _plotRepository.FindByIdAsync(plotIdInt, cancellationToken);

        if (plot == null || plot.IsDeleted)
        {
            return new Result<IEnumerable<AgronomicStatistic>, Error>.Failure(
                AgronomicErrors.PlotNotFound
            );
        }

        if (plot.OwnerUserId != query.UserId)
        {
            return new Result<IEnumerable<AgronomicStatistic>, Error>.Failure(
                AgronomicErrors.PlotOwnership with { Message = $"User {query.UserId} does not own plot {query.PlotId}." }
            );
        }

        var plotStatistics = await _agronomicStatisticRepository.FindAllByUserIdAndPlotIdAndDateBetweenAsync(
            query.UserId, query.PlotId.Value, dateRange.StartDate, dateRange.EndDate, cancellationToken);

        return new Result<IEnumerable<AgronomicStatistic>, Error>.Success(plotStatistics);
    }
}