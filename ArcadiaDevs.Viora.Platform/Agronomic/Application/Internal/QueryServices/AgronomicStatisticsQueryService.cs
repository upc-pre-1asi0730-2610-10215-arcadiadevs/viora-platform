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

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Implementation of agronomic statistics query service.
/// </summary>
public class AgronomicStatisticsQueryService : IAgronomicStatisticsQueryService
{
    private readonly IAgronomicStatisticRepository _agronomicStatisticRepository;
    private readonly IPlotRepository _plotRepository;

    public AgronomicStatisticsQueryService(
        IAgronomicStatisticRepository agronomicStatisticRepository,
        IPlotRepository plotRepository)
    {
        _agronomicStatisticRepository = agronomicStatisticRepository;
        _plotRepository = plotRepository;
    }

    public async Task<Result<IEnumerable<AgronomicStatistic>, Error>> Handle(
        GetAgronomicStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId != query.AuthenticatedUserId)
        {
            return new Result<IEnumerable<AgronomicStatistic>, Error>.Failure(
                new Error("AGRONOMIC_STATISTICS_ACCESS", "Authenticated user cannot access statistics from another user.")
            );
        }

        var today = DateTimeOffset.UtcNow;
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
                new Error("PLOT_NOT_FOUND", "The selected plot does not exist or is inactive.")
            );
        }

        if (plot.OwnerUserId != query.UserId)
        {
            return new Result<IEnumerable<AgronomicStatistic>, Error>.Failure(
                new Error("PLOT_OWNERSHIP", $"User {query.UserId} does not own plot {query.PlotId}.")
            );
        }

        var plotStatistics = await _agronomicStatisticRepository.FindAllByUserIdAndPlotIdAndDateBetweenAsync(
            query.UserId, query.PlotId.Value, dateRange.StartDate, dateRange.EndDate, cancellationToken);

        return new Result<IEnumerable<AgronomicStatistic>, Error>.Success(plotStatistics);
    }
}