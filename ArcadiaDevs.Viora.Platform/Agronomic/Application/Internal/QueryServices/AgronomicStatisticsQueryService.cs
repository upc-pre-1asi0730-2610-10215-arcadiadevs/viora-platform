using ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Implementation of agronomic statistics query service.
/// </summary>
public class AgronomicStatisticsQueryService : IAgronomicStatisticsQueryService
{
    private readonly IPlotRepository _plotRepository;
    private readonly Random _random = new();

    public AgronomicStatisticsQueryService(IPlotRepository plotRepository)
    {
        _plotRepository = plotRepository;
    }

    public async Task<Result<IEnumerable<AgronomicStatisticsDto>, Error>> Handle(
        GetAgronomicStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        var plots = new List<Domain.Model.Aggregate.Plot>();

        if (query.PlotId.HasValue)
        {
            var plot = await _plotRepository.FindByIdAndOwnerUserIdAsync(
                query.PlotId.Value, query.UserId, cancellationToken);
            if (plot == null)
            {
                return new Result<IEnumerable<AgronomicStatisticsDto>, Error>.Failure(
                    new Error("PLOT_ACCESS_DENIED", $"Plot {query.PlotId.Value} does not belong to user {query.UserId}."));
            }
            plots.Add(plot);
        }
        else
        {
            plots = (await _plotRepository.FindAllByOwnerUserIdAsync(query.UserId, cancellationToken)).ToList();
        }

        var statisticsList = new List<AgronomicStatisticsDto>();
        foreach (var plot in plots)
        {
            var dataPoints = GenerateDataPoints(plot.Id, query.TimeRange);
            statisticsList.Add(new AgronomicStatisticsDto
            {
                PlotId = plot.Id,
                PlotName = plot.PlotName,
                TimeRange = query.TimeRange,
                DataPoints = dataPoints
            });
        }

        return new Result<IEnumerable<AgronomicStatisticsDto>, Error>.Success(statisticsList);
    }

    private IReadOnlyList<DataPointDto> GenerateDataPoints(int plotId, string timeRange)
    {
        var now = DateTimeOffset.UtcNow;
        var startDate = timeRange.ToLowerInvariant() switch
        {
            "week" => now.AddDays(-7),
            "month" => now.AddMonths(-1),
            "quarter" => now.AddMonths(-3),
            "year" => now.AddYears(-1),
            _ => now.AddDays(-7) // default fallback
        };

        var dataPoints = new List<DataPointDto>();
        var current = startDate;
        while (current <= now)
        {
            dataPoints.Add(new DataPointDto
            {
                Timestamp = current,
                Ndvi = Math.Round((decimal)(_random.NextDouble() * 0.5 + 0.3), 2), // 0.3–0.8
                ColdPortion = Math.Round((decimal)(_random.NextDouble() * 100), 2) // 0–100
            });
            current = current.AddDays(1);
        }

        return dataPoints;
    }
}