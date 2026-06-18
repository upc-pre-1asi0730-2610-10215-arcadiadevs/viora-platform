using System;
using System.Collections.Generic;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Chart-oriented projection of agronomic statistics over a time window.
/// </summary>
public record AgronomicStatisticSeriesResource
{
    public AgronomicStatisticSeriesResource(
        long? plotId,
        ETimeRange timeRange,
        IReadOnlyList<PointResource> points,
        MetricTrendResource ndviTrend,
        MetricTrendResource chillPortionsTrend,
        MetricTrendResource chillHoursTrend,
        double chillPortionsThreshold,
        EChillRequirementSource chillRequirementSource,
        EChillMetricModel chillMetricModel)
    {
        ArgumentNullException.ThrowIfNull(ndviTrend);
        ArgumentNullException.ThrowIfNull(chillPortionsTrend);
        ArgumentNullException.ThrowIfNull(chillHoursTrend);

        PlotId = plotId;
        TimeRange = timeRange;
        Points = points ?? Array.Empty<PointResource>();
        NdviTrend = ndviTrend;
        ChillPortionsTrend = chillPortionsTrend;
        ChillHoursTrend = chillHoursTrend;
        ChillPortionsThreshold = chillPortionsThreshold;
        ChillRequirementSource = chillRequirementSource;
        ChillMetricModel = chillMetricModel;
    }

    public long? PlotId { get; init; }
    public ETimeRange TimeRange { get; init; }
    public IReadOnlyList<PointResource> Points { get; init; }
    public MetricTrendResource NdviTrend { get; init; }
    public MetricTrendResource ChillPortionsTrend { get; init; }
    public MetricTrendResource ChillHoursTrend { get; init; }
    public double ChillPortionsThreshold { get; init; }
    public EChillRequirementSource ChillRequirementSource { get; init; }
    public EChillMetricModel ChillMetricModel { get; init; }

    /// <summary>
    ///     A single charted point.
    /// </summary>
    public record PointResource(DateTimeOffset Date, double Ndvi, double ChillPortions, double ChillHours);
}
