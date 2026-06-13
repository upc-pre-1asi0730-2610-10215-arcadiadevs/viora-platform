using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class MonitoringSummaryQueryService : IMonitoringSummaryQueryService
{
    private readonly IPlotRepository _plotRepository;
    private readonly IIoTDeviceRepository _ioTDeviceRepository;
    private readonly IAgronomicStatisticRepository _statisticRepository;
    private readonly AgroMonitoringApiClient _agroMonitoringClient;
    private readonly ILogger<MonitoringSummaryQueryService> _logger;

    public MonitoringSummaryQueryService(
        IPlotRepository plotRepository,
        IIoTDeviceRepository ioTDeviceRepository,
        IAgronomicStatisticRepository statisticRepository,
        AgroMonitoringApiClient agroMonitoringClient,
        ILogger<MonitoringSummaryQueryService> logger)
    {
        _plotRepository = plotRepository;
        _ioTDeviceRepository = ioTDeviceRepository;
        _statisticRepository = statisticRepository;
        _agroMonitoringClient = agroMonitoringClient;
        _logger = logger;
    }

    public async Task<Result<MonitoringSummaryDto, Error>> Handle(
        GetCurrentMonitoringSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        // --- Step 1: Fetch data from internal sources (DB) ---
        var plots = (await _plotRepository.FindAllByOwnerUserIdAsync(query.UserId, cancellationToken)).ToList();
        var plotIds = plots.Select(p => (long)p.Id).ToList();
        
        var devices = plotIds.Any()
            ? (await _ioTDeviceRepository.FindAllByPlotIdsAsync(plotIds, cancellationToken)).ToList()
            : new List<IoTDevice>();

        var statistics = await _statisticRepository.FindLatestByUserIdAsync(
            query.UserId, 
            DateTimeOffset.UtcNow.AddDays(-30), 
            DateTimeOffset.UtcNow, 
            cancellationToken);

        // --- Step 2: Consolidate internal data ---
        var consolidatedNdvi = statistics.Any() ? statistics.Average(s => s.NdviValue) : 0m;
        var consolidatedChillHours = statistics.Any() ? statistics.Average(s => s.AccumulatedChillHours) : 0m;
        var latestMeasurementDate = statistics.Any() ? statistics.Max(s => s.MeasurementDate) : DateTimeOffset.UtcNow;

        // --- Step 3: Determine health status based on ideal logic (NDVI) ---
        var healthStatusStr = consolidatedNdvi switch
        {
            > 0.6m => "HEALTHY",
            > 0.3m => "WARNING",
            _ => "CRITICAL"
        };

        // --- Step 4: Fetch real-time external data (Weather) ---
        var weatherSnapshot = await FetchCurrentWeatherAsync(plots, cancellationToken);

        // --- Step 5: Evaluate consolidated risk ---
        var evaluator = new ClimateRiskEvaluator();
        var (consolidatedRisk, recommendation) = evaluator.EvaluateRisk(
            new AccumulatedChillHours(consolidatedChillHours),
            new AverageNdvi(consolidatedNdvi),
            weatherSnapshot);

        // --- Step 6: Calculate Yield Forecast ---
        var yieldForecast = (consolidatedNdvi * 100) + (consolidatedChillHours / 10);

        // --- Step 7: Create Aggregate and DTO ---
        var summaryResult = MonitoringSummary.Create(
            query.UserId,
            healthStatusStr,
            consolidatedNdvi,
            consolidatedChillHours,
            yieldForecast,
            weatherSnapshot,
            recommendation,
            latestMeasurementDate
        );

        if (summaryResult is Result<MonitoringSummary, Error>.Failure failure)
        {
            return new Result<MonitoringSummaryDto, Error>.Failure(failure.Error);
        }

        var aggregate = ((Result<MonitoringSummary, Error>.Success)summaryResult).Value;

        var windowStart = latestMeasurementDate;
        var windowEnd = windowStart.AddDays(7);

        var dto = new MonitoringSummaryDto
        {
            MonitoringSummaryId = aggregate.MonitoringSummaryId.Value,
            UserId = aggregate.UserId.Value,
            GeneralHealthStatus = aggregate.GeneralHealthStatus.ToString(),
            NdviValue = aggregate.AverageNdvi.Value,
            AccumulatedChillHours = aggregate.AccumulatedChillHours.Value,
            YieldForecast = aggregate.YieldProjection.Value,
            MeasurementDate = aggregate.LastSynchronizationAt.Value,
            WeatherSnapshot = new WeatherSnapshotDto
            {
                WeatherStatus = aggregate.WeatherSnapshot.WeatherStatus.ToString(),
                MeasurementDate = aggregate.WeatherSnapshot.LastValidatedReadingAt,
                ClimateRiskLevel = aggregate.WeatherSnapshot.ClimateRiskLevel.ToString(),
                Temperature = aggregate.WeatherSnapshot.CurrentTemperature
            },
            ClimateRiskLevel = consolidatedRisk.ToString(),
            MitigationRecommendations = new List<MitigationRecommendationDto>
            {
                new()
                {
                    ActionType = aggregate.MitigationRecommendation.ActionType,
                    NutritionInputRecommendation = aggregate.MitigationRecommendation.SuggestedInputs,
                    ApplicationWindowStart = windowStart,
                    ApplicationWindowEnd = windowEnd
                }
            }
        };

        return new Result<MonitoringSummaryDto, Error>.Success(dto);
    }

    private ClimateRiskLevel DeriveWeatherRisk(WeatherStatus status, decimal temperature)
    {
        if (status is WeatherStatus.Stormy) return ClimateRiskLevel.Critical;
        if (temperature > 38 || temperature < 2) return ClimateRiskLevel.High;
        if (status is WeatherStatus.Rainy or WeatherStatus.Windy) return ClimateRiskLevel.Medium;
        return ClimateRiskLevel.Low;
    }

    private async Task<WeatherSnapshot> FetchCurrentWeatherAsync(IReadOnlyList<Plot> plots, CancellationToken cancellationToken)
    {
        var plotWithCoords = plots.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.AgroMonitoringCenter));
        if (plotWithCoords == null) return WeatherSnapshot.CreateDefault();

        var center = plotWithCoords.GetCoordinates();
        if (center == null) return WeatherSnapshot.CreateDefault();

        var weatherResult = await _agroMonitoringClient.GetCurrentWeatherAsync(center.Value.Lat, center.Value.Lon, cancellationToken);

        if (weatherResult is Result<AgroMonitoringWeatherResponse, Error>.Success weatherSuccess)
        {
            var response = weatherSuccess.Value;
            var tempCelsius = Math.Round((decimal)response.Main.Temperature - 273.15m, 2);
            var status = Enum.TryParse<WeatherStatus>(response.Weather.FirstOrDefault()?.Main, true, out var weatherStatus)
                ? weatherStatus
                : WeatherStatus.Unknown;
            
            var weatherRisk = DeriveWeatherRisk(status, tempCelsius);
            
            return new WeatherSnapshot(tempCelsius, status, DateTimeOffset.FromUnixTimeSeconds(response.Timestamp), weatherRisk);
        }

        _logger.LogWarning("Could not fetch current weather from AgroMonitoring; returning default.");
        return WeatherSnapshot.CreateDefault();
    }
}

internal static class PlotExtensions
{
    public static (decimal Lat, decimal Lon)? GetCoordinates(this Plot plot)
    {
        if (string.IsNullOrWhiteSpace(plot.AgroMonitoringCenter)) return null;

        var center = plot.AgroMonitoringCenter.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (center.Length < 2 || !decimal.TryParse(center[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon) || !decimal.TryParse(center[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat))
        {
            return null;
        }
        
        return (lat, lon);
    }
}