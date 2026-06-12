using ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Implementation of agronomic statistics query service.
/// </summary>
public class AgronomicStatisticsQueryService : IAgronomicStatisticsQueryService
{
    private readonly IPlotRepository _plotRepository;
    private readonly AgroMonitoringApiClient _agroMonitoringClient;
    private readonly ILogger<AgronomicStatisticsQueryService> _logger;
    private readonly Random _random = new();

    public AgronomicStatisticsQueryService(
        IPlotRepository plotRepository,
        AgroMonitoringApiClient agroMonitoringClient,
        ILogger<AgronomicStatisticsQueryService> logger)
    {
        _plotRepository = plotRepository;
        _agroMonitoringClient = agroMonitoringClient;
        _logger = logger;
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
            var dataPoints = await FetchDataPointsAsync(plot, query.TimeRange, cancellationToken);
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

    private async Task<IReadOnlyList<DataPointDto>> FetchDataPointsAsync(
        Domain.Model.Aggregate.Plot plot,
        string timeRange,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var startDate = timeRange.ToLowerInvariant() switch
        {
            "week" => now.AddDays(-7),
            "month" => now.AddMonths(-1),
            "quarter" => now.AddMonths(-3),
            "year" => now.AddYears(-1),
            _ => now.AddDays(-7)
        };

        // If the plot has an AgroMonitoring polygon ID, try to fetch real NDVI data.
        if (!string.IsNullOrWhiteSpace(plot.AgroMonitoringPolygonId))
        {
            var ndviResult = await _agroMonitoringClient.GetNdviHistoryAsync(
                plot.AgroMonitoringPolygonId, startDate, now, cancellationToken);

            if (ndviResult is Result<IReadOnlyList<AgroMonitoringNdviDataPoint>, Error>.Success ndviSuccess
                && ndviSuccess.Value.Count > 0)
            {
                return ndviSuccess.Value
                    .Select(dp => new DataPointDto
                    {
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(dp.Dt),
                        Ndvi = Math.Round((decimal)dp.Data.Mean, 4),
                        ColdPortion = 0m // Cold portion not available from NDVI endpoint
                    })
                    .ToList();
            }

            _logger.LogInformation(
                "NDVI history unavailable for plot '{PlotName}' (polygon {PolygonId}); falling back to simulated data",
                plot.PlotName, plot.AgroMonitoringPolygonId);
        }

        // Fallback: simulated data when AgroMonitoring is not configured or call fails.
        return GenerateSimulatedDataPoints(plot.Id, startDate, now);
    }

    private IReadOnlyList<DataPointDto> GenerateSimulatedDataPoints(
        int plotId,
        DateTimeOffset startDate,
        DateTimeOffset now)
    {
        var dataPoints = new List<DataPointDto>();
        var current = startDate;
        while (current <= now)
        {
            dataPoints.Add(new DataPointDto
            {
                Timestamp = current,
                Ndvi = Math.Round((decimal)(_random.NextDouble() * 0.5 + 0.3), 2),
                ColdPortion = Math.Round((decimal)(_random.NextDouble() * 100), 2)
            });
            current = current.AddDays(1);
        }

        return dataPoints;
    }
}