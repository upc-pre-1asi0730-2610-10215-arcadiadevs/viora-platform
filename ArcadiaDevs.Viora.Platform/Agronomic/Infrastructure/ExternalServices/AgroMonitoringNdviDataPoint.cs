using System.Text.Json.Serialization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;

/// <summary>
///     A single NDVI data point from the AgroMonitoring NDVI history endpoint.
/// </summary>
public sealed record AgroMonitoringNdviDataPoint
{
    /// <summary>Unix timestamp of the reading.</summary>
    [JsonPropertyName("dt")]
    public long Dt { get; init; }

    /// <summary>NDVI statistics for this data point.</summary>
    [JsonPropertyName("data")]
    public AgroMonitoringNdviData Data { get; init; } = new();
}

/// <summary>
///     NDVI measurement statistics.
/// </summary>
public sealed record AgroMonitoringNdviData
{
    /// <summary>Mean NDVI value.</summary>
    [JsonPropertyName("mean")]
    public double Mean { get; init; }

    /// <summary>Median NDVI value.</summary>
    [JsonPropertyName("median")]
    public double Median { get; init; }

    /// <summary>Maximum NDVI value.</summary>
    [JsonPropertyName("max")]
    public double Max { get; init; }

    /// <summary>Minimum NDVI value.</summary>
    [JsonPropertyName("min")]
    public double Min { get; init; }
}
