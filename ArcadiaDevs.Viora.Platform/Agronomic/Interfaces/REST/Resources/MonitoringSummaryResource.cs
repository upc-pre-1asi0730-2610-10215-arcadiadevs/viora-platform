using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Resource for monitoring summary KPI metrics.
/// </summary>
public record MonitoringSummaryResource
{
    public int TotalPlots { get; init; }
    public int TotalDevices { get; init; }
    public int ActiveDevices { get; init; }
    public int InactiveDevices { get; init; }
    public int MaintenanceDevices { get; init; }
    public decimal AveragePlotArea { get; init; }
    public decimal DeviceHealthPercentage { get; init; }
    public decimal ColdAccumulationIndex { get; init; }
    public decimal YieldProjection { get; init; }
    public decimal AverageNdvi { get; init; }
    public string GeneralHealthStatus { get; init; } = string.Empty;
    public DateTimeOffset LastSynchronizationAt { get; init; }
    
    // Weather and Risk Fields
    public decimal CurrentTemperature { get; init; }
    public string WeatherStatus { get; init; } = string.Empty;
    public DateTimeOffset LastValidatedReadingAt { get; init; }
    public string ClimateRiskLevel { get; init; } = string.Empty;

    // Mitigation Recommendation Fields
    public string MitigationActionType { get; init; } = string.Empty;
    public string MitigationSuggestedInputs { get; init; } = string.Empty;
    public string MitigationRecommendedWindow { get; init; } = string.Empty;
}
