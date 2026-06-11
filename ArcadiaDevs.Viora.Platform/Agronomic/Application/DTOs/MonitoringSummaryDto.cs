namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;

/// <summary>
///     DTO for monitoring summary KPI metrics.
/// </summary>
/// <remarks>
///     Returned by GET /api/v1/monitoring-summaries/current.
/// </remarks>
public record MonitoringSummaryDto
{
    /// <summary>Count of user's plots.</summary>
    public int TotalPlots { get; init; }

    /// <summary>Count of user's IoT devices.</summary>
    public int TotalDevices { get; init; }

    /// <summary>Count of active devices.</summary>
    public int ActiveDevices { get; init; }

    /// <summary>Count of inactive devices.</summary>
    public int InactiveDevices { get; init; }

    /// <summary>Count of devices in maintenance.</summary>
    public int MaintenanceDevices { get; init; }

    /// <summary>Average area across user's plots.</summary>
    public decimal AveragePlotArea { get; init; }

    /// <summary>Percentage of active devices.</summary>
    public decimal DeviceHealthPercentage { get; init; }

    /// <summary>Computed from plot data (placeholder).</summary>
    public decimal ColdAccumulationIndex { get; init; }

    /// <summary>Computed from plot data (placeholder).</summary>
    public decimal YieldProjection { get; init; }

    /// <summary>Computed average NDVI.</summary>
    public decimal AverageNdvi { get; init; }

    /// <summary>General health status.</summary>
    public string GeneralHealthStatus { get; init; } = string.Empty;

    /// <summary>Timestamp of last data sync.</summary>
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