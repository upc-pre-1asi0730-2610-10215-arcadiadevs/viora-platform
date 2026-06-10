namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;

/// <summary>
///     DTO for a single data point in agronomic statistics time series.
/// </summary>
public record DataPointDto
{
    /// <summary>Point in time.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Placeholder NDVI value.</summary>
    public decimal Ndvi { get; init; }

    /// <summary>Placeholder cold portion value.</summary>
    public decimal ColdPortion { get; init; }
}