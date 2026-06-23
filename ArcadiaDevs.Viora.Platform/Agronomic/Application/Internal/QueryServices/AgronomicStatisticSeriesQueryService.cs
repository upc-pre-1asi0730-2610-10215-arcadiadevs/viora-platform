using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class AgronomicStatisticSeriesQueryService : IAgronomicStatisticSeriesQueryService
{
    private const double NdviStabilityEpsilon = 0.02;
    private const double ChillStabilityEpsilon = 1.0;

    private readonly IAgronomicStatisticRepository _statisticRepository;
    private readonly IPlotRepository _plotRepository;
    private readonly ChillRequirementResolver _chillRequirementResolver;

    public AgronomicStatisticSeriesQueryService(
        IAgronomicStatisticRepository statisticRepository,
        IPlotRepository plotRepository,
        ChillRequirementResolver chillRequirementResolver)
    {
        _statisticRepository = statisticRepository;
        _plotRepository = plotRepository;
        _chillRequirementResolver = chillRequirementResolver;
    }

    public async Task<Result<AgronomicStatisticSeriesResource, Error>> Handle(
        GetAgronomicStatisticSeriesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId != query.AuthenticatedUserId)
        {
            return new Result<AgronomicStatisticSeriesResource, Error>.Failure(
                AgronomicErrors.AgronomicStatisticsAccess
            );
        }

        var chillRequirement = _chillRequirementResolver.ResolveDefault();
        
        var plot = await _plotRepository.FindByIdAsync((int)query.PlotId.Value, cancellationToken);
        if (plot == null || plot.IsDeleted)
        {
            return new Result<AgronomicStatisticSeriesResource, Error>.Failure(
                AgronomicErrors.PlotNotFound
            );
        }

        if (plot.OwnerUserId != query.UserId)
        {
            return new Result<AgronomicStatisticSeriesResource, Error>.Failure(
                AgronomicErrors.PlotOwnership with { Message = $"User {query.UserId} does not own plot {query.PlotId}." }
            );
        }
        
        chillRequirement = _chillRequirementResolver.ResolveFor(plot);

        var today = DateTimeOffset.UtcNow;
        var currentWindow = query.TimeRange.ToDateRange(today);
        var windowDays = (currentWindow.EndDate - currentWindow.StartDate).TotalDays + 1;
        var previousEnd = currentWindow.StartDate.AddDays(-1);
        var previousStart = previousEnd.AddDays(-(windowDays - 1));

        var statistics = (await _statisticRepository.FindAllByUserIdAndPlotIdAndDateBetweenAsync(
            query.UserId, query.PlotId.Value, previousStart, currentWindow.EndDate, cancellationToken)).ToList();

        var currentStatistics = Within(statistics, currentWindow.StartDate, currentWindow.EndDate);
        var previousStatistics = Within(statistics, previousStart, previousEnd);

        var points = AggregatePoints(currentStatistics);
        var labels = points.Select(p => p.Date.ToString("yyyy-MM-dd")).ToList();
        var ndviSeries = points.Select(p => p.Ndvi).ToList();
        var cpSeries = points.Select(p => p.ChillPortions).ToList();
        var chillHoursSeries = points.Select(p => p.ChillHours).ToList();

        var ndviTrend = Trend(currentStatistics, previousStatistics, s => s.NdviValue, NdviStabilityEpsilon);
        var chillPortionsTrend = Trend(currentStatistics, previousStatistics, s => s.ChillPortions, ChillStabilityEpsilon);
        var chillHoursTrend = Trend(currentStatistics, previousStatistics, s => s.ChillHours, ChillStabilityEpsilon);

        var chillUnit = chillRequirement.Model == EChillMetricModel.Dynamic ? "Porciones de frío" : "Horas frío";
        var trend = chillPortionsTrend.Direction.ToString();
        var statusLabel = (chillPortionsTrend.CurrentValue ?? 0) >= chillRequirement.Portions.Value ? "Alcanzado" : "En Progreso";
        var observation = "Análisis basado en la ventana temporal seleccionada.";

        var series = new AgronomicStatisticSeriesResource(
            query.PlotId,
            query.TimeRange.ToString(),
            labels,
            ndviSeries,
            cpSeries,
            chillHoursSeries,
            chillRequirement.Portions.Value,
            chillRequirement.Source.ToString(),
            chillRequirement.Model.ToString(),
            chillUnit,
            trend,
            statusLabel,
            observation,
            ndviTrend,
            chillPortionsTrend,
            chillHoursTrend
        );

        return new Result<AgronomicStatisticSeriesResource, Error>.Success(series);
    }

    private List<AgronomicStatistic> Within(List<AgronomicStatistic> statistics, DateTimeOffset start, DateTimeOffset end)
    {
        var startDay = start.Date;
        var endDay = end.Date;
        return statistics.Where(s =>
        {
            var date = s.MeasurementDate.Date;
            return date >= startDay && date <= endDay;
        }).ToList();
    }

    private record PointData(DateTimeOffset Date, double Ndvi, double ChillPortions, double ChillHours);

    private IReadOnlyList<PointData> AggregatePoints(List<AgronomicStatistic> statistics)
    {
        return statistics
            .GroupBy(s => s.MeasurementDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new PointData(
                g.Key,
                Math.Round(g.Average(s => s.NdviValue), 3),
                Math.Round(g.Average(s => s.ChillPortions), 3),
                Math.Round(g.Average(s => s.ChillHours), 3)
            ))
            .ToList();
    }

    private MetricTrendResource Trend(
        List<AgronomicStatistic> current,
        List<AgronomicStatistic> previous,
        Func<AgronomicStatistic, double> metric,
        double epsilon)
    {
        double? currentValue = current.Any() ? Math.Round(current.Average(metric), 3) : null;
        double? previousValue = previous.Any() ? Math.Round(previous.Average(metric), 3) : null;

        if (currentValue == null && previousValue == null)
        {
            return MetricTrendResource.StableUnknown();
        }

        double? change = (currentValue.HasValue && previousValue.HasValue)
            ? Math.Round(currentValue.Value - previousValue.Value, 3)
            : null;

        double? changePercent = (change.HasValue && previousValue.HasValue && previousValue.Value != 0.0)
            ? Math.Round(change.Value / previousValue.Value * 100.0, 3)
            : null;

        return new MetricTrendResource(currentValue, previousValue, change, changePercent, Direction(change, epsilon));
    }

    private Domain.Model.ValueObjects.ETrendDirection Direction(double? change, double epsilon)
    {
        if (!change.HasValue || Math.Abs(change.Value) <= epsilon)
        {
            return Domain.Model.ValueObjects.ETrendDirection.STABLE;
        }
        return change.Value > 0 ? Domain.Model.ValueObjects.ETrendDirection.UP : Domain.Model.ValueObjects.ETrendDirection.DOWN;
    }
}
