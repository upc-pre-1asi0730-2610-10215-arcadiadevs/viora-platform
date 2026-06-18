using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Resource for agronomic statistics time series per plot.
/// </summary>
/// <remarks>
///     Returned by GET /api/v1/agronomic-statistics.
/// </remarks>
public record AgronomicStatisticsResource
{
    /// <summary>Identifier of the plot.</summary>
    public int PlotId { get; init; }

    /// <summary>Display name of the plot.</summary>
    public string PlotName { get; init; } = string.Empty;

    /// <summary>The requested time range.</summary>
    public string TimeRange { get; init; } = string.Empty;

    /// <summary>Ordered by timestamp ascending.</summary>
    public IReadOnlyList<DataPointResource> DataPoints { get; init; } = Array.Empty<DataPointResource>();
}
