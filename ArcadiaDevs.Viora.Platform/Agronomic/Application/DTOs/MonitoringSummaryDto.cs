using System;
using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;

/// <summary>
///     DTO for weather snapshot data.
/// </summary>
public record WeatherSnapshotDto
{
    public string WeatherStatus { get; init; } = string.Empty;
    public DateTimeOffset MeasurementDate { get; init; }
    public string ClimateRiskLevel { get; init; } = string.Empty;
    public decimal Temperature { get; init; }
}

/// <summary>
///     DTO for mitigation recommendation data.
/// </summary>
public record MitigationRecommendationDto
{
    public string ActionType { get; init; } = string.Empty;
    public string NutritionInputRecommendation { get; init; } = string.Empty;
    public DateTimeOffset ApplicationWindowStart { get; init; }
    public DateTimeOffset ApplicationWindowEnd { get; init; }
}

/// <summary>
///     DTO for monitoring summary KPI metrics.
/// </summary>
/// <remarks>
///     Returned by GET /api/v1/monitoring-summaries/current.
/// </remarks>
public record MonitoringSummaryDto
{
    public long MonitoringSummaryId { get; init; }
    public long UserId { get; init; }

    // These fields are calculated in the service but not exposed in the final resource.
    // They are kept here as they are part of the internal DTO logic.
    public int TotalPlots { get; init; }
    public int TotalDevices { get; init; }
    public int ActiveDevices { get; init; }
    public int InactiveDevices { get; init; }
    public int MaintenanceDevices { get; init; }
    public decimal AveragePlotArea { get; init; }
    public decimal DeviceHealthPercentage { get; init; }

    public string GeneralHealthStatus { get; init; } = string.Empty;
    public decimal NdviValue { get; init; }
    public decimal AccumulatedChillHours { get; init; }
    public decimal YieldForecast { get; init; }
    public DateTimeOffset MeasurementDate { get; init; }

    // Nested DTOs
    public WeatherSnapshotDto WeatherSnapshot { get; init; } = new();
    public string ClimateRiskLevel { get; init; } = string.Empty; // Top-level climate risk
    public IReadOnlyList<MitigationRecommendationDto> MitigationRecommendations { get; init; } = new List<MitigationRecommendationDto>();
}