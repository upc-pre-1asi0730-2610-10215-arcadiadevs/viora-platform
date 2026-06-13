using System;
using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Resource for weather snapshot data.
/// </summary>
public record WeatherSnapshotResource
{
    public string WeatherStatus { get; init; } = string.Empty;
    public DateTimeOffset MeasurementDate { get; init; }
    public string ClimateRiskLevel { get; init; } = string.Empty;
    public decimal Temperature { get; init; }
}

/// <summary>
///     Resource for mitigation recommendation data.
/// </summary>
public record MitigationRecommendationResource
{
    public string ActionType { get; init; } = string.Empty;
    public string NutritionInputRecommendation { get; init; } = string.Empty;
    public DateTimeOffset ApplicationWindowStart { get; init; }
    public DateTimeOffset ApplicationWindowEnd { get; init; }
}

/// <summary>
///     Resource for monitoring summary KPI metrics.
/// </summary>
public record MonitoringSummaryResource
{
    public long MonitoringSummaryId { get; init; }
    public long UserId { get; init; }
    public string GeneralHealthStatus { get; init; } = string.Empty;
    public decimal NdviValue { get; init; }
    public decimal AccumulatedChillHours { get; init; }
    public decimal YieldForecast { get; init; }
    public DateTimeOffset MeasurementDate { get; init; }
    public WeatherSnapshotResource WeatherSnapshot { get; init; } = new();
    public string ClimateRiskLevel { get; init; } = string.Empty;
    public IReadOnlyList<MitigationRecommendationResource> MitigationRecommendations { get; init; } = new List<MitigationRecommendationResource>();
}