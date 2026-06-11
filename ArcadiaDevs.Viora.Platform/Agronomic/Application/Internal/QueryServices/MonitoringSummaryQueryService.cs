using ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Services;
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

    public MonitoringSummaryQueryService(
        IPlotRepository plotRepository,
        IIoTDeviceRepository ioTDeviceRepository)
    {
        _plotRepository = plotRepository;
        _ioTDeviceRepository = ioTDeviceRepository;
    }

    public async Task<Result<MonitoringSummaryDto, Error>> Handle(
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

        // Simulated values for metrics based on plots
        var simulatedNdvi = totalPlots > 0 ? 0.65m : 0m;
        var simulatedChillHours = totalPlots > 0 ? 120.5m : 0m;
        var simulatedYieldProjection = totalPlots > 0 ? 4500m : 0m;
        
        var healthStatusStr = deviceHealthPercentage >= 80 ? "Good" 
            : deviceHealthPercentage >= 50 ? "Moderate" 
            : "Critical";
            
        if (totalDevices == 0) healthStatusStr = "Moderate"; // Default when no devices

        // TS016TASK007, TS016TASK008, TS016TASK009, TS016TASK010: Weather, Risk Evaluation and Mitigation
        var simulatedWeather = new WeatherSnapshot(
            22.5m, 
            WeatherStatus.Sunny, 
            DateTimeOffset.UtcNow.AddMinutes(-30), 
            ClimateRiskLevel.Medium);

        var evaluator = new ClimateRiskEvaluator();
        var recommendation = evaluator.EvaluateRisk(
            new AccumulatedChillHours(simulatedChillHours),
            new AverageNdvi(simulatedNdvi),
            simulatedWeather);

        // Use the aggregate root to validate and create the summary
        var summaryResult = MonitoringSummary.Create(
            query.UserId,
            healthStatusStr,
            simulatedNdvi,
            simulatedChillHours,
            simulatedYieldProjection,
            simulatedWeather,
            recommendation,
            DateTimeOffset.UtcNow
        );

        if (summaryResult is Result<MonitoringSummary, Error>.Failure failure)
        {
            return new Result<MonitoringSummaryDto, Error>.Failure(failure.Error);
        }

        var aggregate = ((Result<MonitoringSummary, Error>.Success)summaryResult).Value;

        var dto = new MonitoringSummaryDto
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

        return new Result<MonitoringSummaryDto, Error>.Success(dto);
    }
}