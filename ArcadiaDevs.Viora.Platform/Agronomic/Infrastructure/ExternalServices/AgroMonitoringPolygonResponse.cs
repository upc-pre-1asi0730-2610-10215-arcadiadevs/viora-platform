using System.Text.Json.Serialization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;

/// <summary>
///     Response from the AgroMonitoring polygon creation endpoint.
/// </summary>
public sealed record AgroMonitoringPolygonResponse
{
    /// <summary>The polygon identifier assigned by AgroMonitoring.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The computed area of the polygon (in acres or hectares, per API docs).</summary>
    [JsonPropertyName("area")]
    public double Area { get; init; }

    /// <summary>The center coordinates [lon, lat].</summary>
    [JsonPropertyName("center")]
    public double[] Center { get; init; } = [];
}
