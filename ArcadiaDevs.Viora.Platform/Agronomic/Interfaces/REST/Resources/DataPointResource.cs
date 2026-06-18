namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Resource for a single data point in agronomic statistics time series.
/// </summary>
public record DataPointResource
{
    /// <summary>Point in time.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Placeholder NDVI value.</summary>
    public decimal Ndvi { get; init; }

    /// <summary>Placeholder cold portion value.</summary>
    public decimal ColdPortion { get; init; }
}
