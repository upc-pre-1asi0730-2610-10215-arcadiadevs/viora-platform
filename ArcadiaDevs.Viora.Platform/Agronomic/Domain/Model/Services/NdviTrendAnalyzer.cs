using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
/// Domain service that analyzes NDVI history to determine the trend direction and change rate.
/// </summary>
/// <remarks>
/// <para>
/// The analyzer computes the trend direction based on the change rate between the earliest and latest
/// NDVI mean values. A stability threshold of 0.02 is used to classify the trend as stable.
/// </para>
/// <para>
/// This is a pure function with no I/O dependencies. It is registered as a singleton in DI.
/// </para>
/// </remarks>
public class NdviTrendAnalyzer
{
    /// <summary>
    /// Threshold for classifying a trend as stable (change rate between -0.02 and 0.02).
    /// </summary>
    private const double StabilityThreshold = 0.02;

    /// <summary>
    /// Analyzes the NDVI history to determine the trend direction and change rate.
    /// </summary>
    /// <param name="history">The NDVI history to analyze (must not be null).</param>
    /// <returns>
    /// An <see cref="NdviTrend"/> containing the direction, change rate, and the series of statistics.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="history"/> is null.</exception>
    public NdviTrend Analyze(NdviHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        var statistics = history.Statistics;
        var latest = statistics[^1]; // Last in sorted list = most recent
        var earliest = statistics[0]; // First in sorted list = earliest

        var changeRate = latest.Mean - earliest.Mean;

        NdviTrendDirection direction;
        if (changeRate > StabilityThreshold)
        {
            direction = NdviTrendDirection.Rising;
        }
        else if (changeRate < -StabilityThreshold)
        {
            direction = NdviTrendDirection.Falling;
        }
        else
        {
            direction = NdviTrendDirection.Stable;
        }

        return new NdviTrend(direction, changeRate, statistics);
    }
}