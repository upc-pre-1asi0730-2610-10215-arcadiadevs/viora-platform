using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Implementation of monitoring summary query service.
/// </summary>
public class MonitoringSummaryQueryService : IMonitoringSummaryQueryService
{
    private readonly IPlotRepository _plotRepository;
    private readonly IIoTDeviceRepository _ioTDeviceRepository;
    private readonly AgroMonitoringApiClient _agroMonitoringClient;
    private readonly ClimateRiskEvaluator _climateRiskEvaluator;
    private readonly ArcadiaDevs.Viora.Platform.Shared.Domain.IClock _clock;
    private readonly ILogger<MonitoringSummaryQueryService> _logger;

    public MonitoringSummaryQueryService(
        IPlotRepository plotRepository,
        IIoTDeviceRepository ioTDeviceRepository,
        AgroMonitoringApiClient agroMonitoringClient,
        ClimateRiskEvaluator climateRiskEvaluator,
        ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock,
        ILogger<MonitoringSummaryQueryService> logger)
    {
        _plotRepository = plotRepository;
        _ioTDeviceRepository = ioTDeviceRepository;
        _agroMonitoringClient = agroMonitoringClient;
        _climateRiskEvaluator = climateRiskEvaluator;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<MonitoringSummaryResource, Error>> Handle(
        GetCurrentMonitoringSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        // TS016TASK004: Using FindAllByOwnerUserIdAsync and FindAllByPlotIdsAsync which return AsNoTracking
        // keeping the SQL logic optimized inside the repositories.
        var plots = (await _plotRepository.FindAllByOwnerUserIdAsync(query.UserId, cancellationToken)).ToList();
        var plotIds = plots.Select(p => (long)p.Id).ToList();
        
        var devices = plotIds.Any()
            ? (await _ioTDeviceRepository.FindAllByPlotIdsAsync(plotIds, cancellationToken)).ToList()
            : new List<IoTDevice>();

        var totalPlots = plots.Count;
        var totalDevices = devices.Count;
        var activeDevices = devices.Count(d => d.Status == IoTDeviceStatus.Active);
        var inactiveDevices = devices.Count(d => d.Status == IoTDeviceStatus.Inactive);
        var maintenanceDevices = devices.Count(d => d.Status == IoTDeviceStatus.Maintenance);
        
        var averagePlotArea = totalPlots > 0
            ? Math.Round(plots.Average(p => p.AreaSize), 2)
            : 0m;
        
        var deviceHealthPercentage = totalDevices > 0
            ? Math.Round((decimal)activeDevices / totalDevices * 100, 2)
            : 0m;

        // Fetch real accumulated temperature data from AgroMonitoring.
        var chillHours = await FetchAccumulatedChillHoursAsync(plots, cancellationToken);

        // Simulated values for metrics that depend on external data we don't yet consume.
        var simulatedNdvi = totalPlots > 0 ? 0.65m : 0m;
        var simulatedYieldProjection = totalPlots > 0 ? 4500m : 0m;
        
        var healthStatusStr = deviceHealthPercentage >= 80 ? "Good" 
            : deviceHealthPercentage >= 50 ? "Moderate" 
            : "Critical";
            
        if (totalDevices == 0) healthStatusStr = "Moderate"; // Default when no devices

        var now = new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero);

        // TS016TASK007, TS016TASK008, TS016TASK009, TS016TASK010: Weather, Risk Evaluation and Mitigation
        var simulatedWeather = new WeatherSnapshot(
            22.5m,
            WeatherStatus.Sunny,
            now.AddMinutes(-30),
            ClimateRiskLevel.Medium);

        var recommendation = _climateRiskEvaluator.EvaluateRisk(
            new AccumulatedChillHours(chillHours),
            new AverageNdvi(simulatedNdvi),
            simulatedWeather);

        // Use the aggregate root to validate and create the summary
        var summaryResult = MonitoringSummary.Create(
            query.UserId,
            healthStatusStr,
            simulatedNdvi,
            chillHours,
            simulatedYieldProjection,
            simulatedWeather,
            recommendation,
            now
        );

        if (summaryResult is Result<MonitoringSummary, Error>.Failure failure)
        {
            return new Result<MonitoringSummaryResource, Error>.Failure(failure.Error);
        }

        var aggregate = ((Result<MonitoringSummary, Error>.Success)summaryResult).Value;

        var dto = new MonitoringSummaryResource
        {
            TotalPlots = totalPlots,
            TotalDevices = totalDevices,
            ActiveDevices = activeDevices,
            InactiveDevices = inactiveDevices,
            MaintenanceDevices = maintenanceDevices,
            AveragePlotArea = averagePlotArea,
            DeviceHealthPercentage = deviceHealthPercentage,
            ColdAccumulationIndex = aggregate.AccumulatedChillHours.Value,
            YieldProjection = aggregate.YieldProjection.Value,
            AverageNdvi = aggregate.AverageNdvi.Value,
            GeneralHealthStatus = aggregate.GeneralHealthStatus.ToString(),
            LastSynchronizationAt = aggregate.LastSynchronizationAt.Value,
            CurrentTemperature = aggregate.WeatherSnapshot.CurrentTemperature,
            WeatherStatus = aggregate.WeatherSnapshot.WeatherStatus.ToString(),
            LastValidatedReadingAt = aggregate.WeatherSnapshot.LastValidatedReadingAt,
            ClimateRiskLevel = aggregate.WeatherSnapshot.ClimateRiskLevel.ToString(),
            MitigationActionType = aggregate.MitigationRecommendation.ActionType,
            MitigationSuggestedInputs = aggregate.MitigationRecommendation.SuggestedInputs,
            MitigationRecommendedWindow = aggregate.MitigationRecommendation.RecommendedApplicationWindow
        };

        return new Result<MonitoringSummaryResource, Error>.Success(dto);
    }

    /// <summary>
    ///     Fetches accumulated chill hours from AgroMonitoring for the first plot with coordinates.
    ///     Falls back to a simulated value if no plot has AgroMonitoring data or the API call fails.
    /// </summary>
    private async Task<decimal> FetchAccumulatedChillHoursAsync(
        IReadOnlyList<Plot> plots,
        CancellationToken cancellationToken)
    {
        // Chill hours use the 273.15 K threshold (0 °C).
        const double chillThresholdKelvin = 273.15;

        // Use a 30-day window for chill accumulation.
        var end = new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero);
        var start = end.AddDays(-30);

        foreach (var plot in plots)
        {
            if (string.IsNullOrWhiteSpace(plot.AgroMonitoringCenter))
                continue;

            // Parse center coordinates "[lon, lat]".
            var center = plot.AgroMonitoringCenter
                .Trim('[', ']')
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (center.Length < 2
                || !decimal.TryParse(center[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lon)
                || !decimal.TryParse(center[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lat))
            {
                continue;
            }

            var tempResult = await _agroMonitoringClient.GetAccumulatedTemperatureAsync(
                lat, lon, start, end, chillThresholdKelvin, cancellationToken);

            if (tempResult is Result<IReadOnlyList<Infrastructure.ExternalServices.AgroMonitoringTemperatureDataPoint>, Error>.Success tempSuccess
                && tempSuccess.Value.Count > 0)
            {
                // Sum the accumulated counts and convert to chill hours.
                var totalCount = tempSuccess.Value.Sum(t => t.Count);
                return Math.Round((decimal)totalCount / 24m, 2);
            }
        }

        // Fallback: simulated value when no plot has AgroMonitoring data.
        _logger.LogInformation(
            "No AgroMonitoring data available for chill hours; using simulated value");
        return plots.Count > 0 ? 120.5m : 0m;
    }
}