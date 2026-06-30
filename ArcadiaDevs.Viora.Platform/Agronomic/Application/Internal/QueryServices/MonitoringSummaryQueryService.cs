using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Implementation of monitoring summary query service.
/// </summary>
/// <remarks>
///     A1 (PR-C) refactor: the legacy hard-coded
///     <c>simulatedNdvi = 0.65m</c>, <c>simulatedYieldProjection = 4500m</c>,
///     and <c>simulatedWeather = 22.5m/Sunny/Medium</c> literals (and the
///     <c>120.5m</c> chill-hours fallback) have been replaced with real
///     provider-backed reads. The chill fallback is now <c>0m</c> + a
///     <c>Warning</c>-level log line — never a fabricated value. The yield
///     projection is delegated to the new
///     <see cref="IYieldForecastEstimator"/>. The weather snapshot is
///     pulled from <see cref="IWeatherDataService"/>; a <c>null</c>
///     snapshot propagates as <see cref="AgronomicErrors.WeatherUnavailable"/>
///     (CC-8: no silent default).
/// </remarks>
public class MonitoringSummaryQueryService : IMonitoringSummaryQueryService
{
    private readonly IPlotRepository _plotRepository;
    private readonly IIoTDeviceRepository _ioTDeviceRepository;
    private readonly AgroMonitoringApiClient _agroMonitoringClient;
    private readonly ClimateRiskEvaluator _climateRiskEvaluator;
    private readonly ArcadiaDevs.Viora.Platform.Shared.Domain.IClock _clock;
    private readonly ILogger<MonitoringSummaryQueryService> _logger;
    private readonly IWeatherDataService _weatherDataService;
    private readonly IYieldForecastEstimator _yieldForecastEstimator;
    private readonly IAgronomicStatisticRepository _agronomicStatisticRepository;
    private readonly IOptions<DynamicNutritionPolicyOptions> _policy;
    private readonly ChillRequirementResolver _chillRequirementResolver;

    public MonitoringSummaryQueryService(
        IPlotRepository plotRepository,
        IIoTDeviceRepository ioTDeviceRepository,
        AgroMonitoringApiClient agroMonitoringClient,
        ClimateRiskEvaluator climateRiskEvaluator,
        ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock,
        ILogger<MonitoringSummaryQueryService> logger,
        IWeatherDataService weatherDataService,
        IYieldForecastEstimator yieldForecastEstimator,
        IAgronomicStatisticRepository agronomicStatisticRepository,
        IOptions<DynamicNutritionPolicyOptions> policy,
        ChillRequirementResolver chillRequirementResolver)
    {
        _plotRepository = plotRepository;
        _ioTDeviceRepository = ioTDeviceRepository;
        _agroMonitoringClient = agroMonitoringClient;
        _climateRiskEvaluator = climateRiskEvaluator;
        _clock = clock;
        _logger = logger;
        _weatherDataService = weatherDataService;
        _yieldForecastEstimator = yieldForecastEstimator;
        _agronomicStatisticRepository = agronomicStatisticRepository;
        _policy = policy;
        _chillRequirementResolver = chillRequirementResolver;
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

        var healthStatusStr = deviceHealthPercentage >= 80 ? "Good"
            : deviceHealthPercentage >= 50 ? "Moderate"
            : "Critical";

        if (totalDevices == 0) healthStatusStr = "Moderate"; // Default when no devices

        // S1.1 (no plots): return Result.Success with all fields 0, no provider calls.
        if (totalPlots == 0)
        {
            return new Result<MonitoringSummaryResource, Error>.Success(new MonitoringSummaryResource
            {
                GeneralHealthStatus = healthStatusStr
            });
        }

        // Representative plot = first plot ordered by IsActive desc, Id asc (deterministic).
        var representative = plots
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Id)
            .First();

        var now = new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero);

        // A1 (PR-C): real NDVI from the latest AgronomicStatistic for the representative plot.
        var latestStatistic = await _agronomicStatisticRepository
            .FindLatestByPlotIdAsync(representative.Id, cancellationToken);
        var averageNdvi = (decimal)(latestStatistic?.NdviValue ?? 0.0);

        // A1 (PR-C): real weather from IWeatherDataService; null propagates as a failure.
        var weather = await _weatherDataService
            .GetCurrentWeatherSnapshotAsync(representative, cancellationToken);
        if (weather is null)
        {
            _logger.LogWarning(
                "Live weather data is unavailable for plot {PlotId}; the platform does not provide a fabricated fallback.",
                representative.Id);
            return new Result<MonitoringSummaryResource, Error>.Failure(AgronomicErrors.WeatherUnavailable);
        }

        // A1 (PR-C): real chill hours from AgroMonitoring. The 120.5m literal
        // fallback is removed — a missing dataset yields 0m + a Warning log
        // (no fabricated value, no silent default).
        var chillHours = await FetchAccumulatedChillHoursAsync(plots, now, query.UserId, cancellationToken);

        // A1 (PR-C): real yield from the new IYieldForecastEstimator; no more
        // simulatedYieldProjection = 4500m constant.
        var chillRequirement = _chillRequirementResolver.ResolveFor(representative);
        var yieldProjection = _yieldForecastEstimator.Estimate(
            representative, latestStatistic, chillRequirement, _policy.Value);

        // TS016TASK007, TS016TASK008, TS016TASK009, TS016TASK010: Weather, Risk Evaluation and Mitigation.
        var recommendation = _climateRiskEvaluator.EvaluateRisk(
            new AccumulatedChillHours(chillHours),
            new AverageNdvi(averageNdvi),
            weather);

        // Use the aggregate root to validate and create the summary
        var summaryResult = MonitoringSummary.Create(
            query.UserId,
            healthStatusStr,
            averageNdvi,
            chillHours,
            yieldProjection,
            weather,
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
    ///     The legacy 120.5m fabricated fallback is REMOVED in PR-C (A1): a missing
    ///     dataset yields 0m + a Warning log line. There is no fabricated-data
    ///     fallback (CC-8).
    /// </summary>
    private async Task<decimal> FetchAccumulatedChillHoursAsync(
        IReadOnlyList<Plot> plots,
        DateTimeOffset now,
        int userId,
        CancellationToken cancellationToken)
    {
        // Chill hours use the 273.15 K threshold (0 °C).
        const double chillThresholdKelvin = 273.15;

        // Use a 30-day window for chill accumulation.
        var start = now.AddDays(-30);

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
                lat, lon, start, now, chillThresholdKelvin, cancellationToken);

            if (tempResult is Result<IReadOnlyList<Infrastructure.ExternalServices.AgroMonitoringTemperatureDataPoint>, Error>.Success tempSuccess
                && tempSuccess.Value.Count > 0)
            {
                // Sum the accumulated counts and convert to chill hours.
                var totalCount = tempSuccess.Value.Sum(t => t.Count);
                return Math.Round((decimal)totalCount / 24m, 2);
            }
        }

        // A1 (PR-C): the legacy 120.5m fabricated fallback is REMOVED.
        // A missing dataset yields 0m + a Warning log. No fabricated value.
        _logger.LogWarning(
            "No AgroMonitoring data available for chill hours for user {UserId} on {MeasurementDate}; " +
            "returning 0m (no fabricated fallback).",
            userId, now);
        return 0m;
    }
}
