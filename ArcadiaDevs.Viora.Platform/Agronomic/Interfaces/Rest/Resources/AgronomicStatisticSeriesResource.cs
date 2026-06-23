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
        string timeRange,
        IReadOnlyList<string> labels,
        IReadOnlyList<double> ndviSeries,
        IReadOnlyList<double> cpSeries,
        IReadOnlyList<double> chillHoursSeries,
        double threshold,
        string chillRequirementSource,
        string chillMetricModel,
        string chillUnit,
        string trend,
        string statusLabel,
        string observation,
        MetricTrendResource ndviTrend,
        MetricTrendResource chillPortionsTrend,
        MetricTrendResource chillHoursTrend)
    {
        ArgumentNullException.ThrowIfNull(ndviTrend);
        ArgumentNullException.ThrowIfNull(chillPortionsTrend);
        ArgumentNullException.ThrowIfNull(chillHoursTrend);

        PlotId = plotId ?? 0; // Defaulting to 0 if null as seen in Java example
        TimeRange = timeRange;
        Labels = labels ?? Array.Empty<string>();
        NdviSeries = ndviSeries ?? Array.Empty<double>();
        CpSeries = cpSeries ?? Array.Empty<double>();
        ChillHoursSeries = chillHoursSeries ?? Array.Empty<double>();
        Threshold = threshold;
        ChillRequirementSource = chillRequirementSource;
        ChillMetricModel = chillMetricModel;
        ChillUnit = chillUnit;
        Trend = trend;
        StatusLabel = statusLabel;
        Observation = observation;
        NdviTrend = ndviTrend;
        ChillPortionsTrend = chillPortionsTrend;
        ChillHoursTrend = chillHoursTrend;
    }

    public long PlotId { get; init; }
    public string TimeRange { get; init; }
    public IReadOnlyList<string> Labels { get; init; }
    public IReadOnlyList<double> NdviSeries { get; init; }
    public IReadOnlyList<double> CpSeries { get; init; }
    public IReadOnlyList<double> ChillHoursSeries { get; init; }
    public double Threshold { get; init; }
    public string ChillRequirementSource { get; init; }
    public string ChillMetricModel { get; init; }
    public string ChillUnit { get; init; }
    public string Trend { get; init; }
    public string StatusLabel { get; init; }
    public string Observation { get; init; }
    public MetricTrendResource NdviTrend { get; init; }
    public MetricTrendResource ChillPortionsTrend { get; init; }
    public MetricTrendResource ChillHoursTrend { get; init; }
}
