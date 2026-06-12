using System.Text.Json.Serialization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;

/// <summary>
///     A single accumulated temperature data point from the AgroMonitoring weather endpoint.
/// </summary>
public sealed record AgroMonitoringTemperatureDataPoint
{
    /// <summary>Unix timestamp of the reading.</summary>
    [JsonPropertyName("dt")]
    public long Dt { get; init; }

    /// <summary>Temperature value in Kelvin.</summary>
    [JsonPropertyName("temp")]
    public double Temp { get; init; }

    /// <summary>Number of readings accumulated.</summary>
    [JsonPropertyName("count")]
    public int Count { get; init; }
}
