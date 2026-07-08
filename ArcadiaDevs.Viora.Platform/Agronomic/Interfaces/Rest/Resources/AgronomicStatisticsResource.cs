using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record AgronomicStatisticsResource
{
    public int PlotId { get; init; }

    public string PlotName { get; init; } = string.Empty;

    public string TimeRange { get; init; } = string.Empty;

    public IReadOnlyList<DataPointResource> DataPoints { get; init; } = Array.Empty<DataPointResource>();
}
