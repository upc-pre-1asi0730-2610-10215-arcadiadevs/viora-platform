using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Domain service that evaluates the current chill season state for a plot,
///     composing <see cref="ChillAccumulationCalculator"/> and
///     <see cref="ChillRequirementResolver"/> to determine progress toward the
///     chill requirement and the seasonal status.
/// </summary>
/// <remarks>
/// <para>
///     Replaces the hardcoded <c>120.5</c> chill portions and
///     <c>10.2</c> weekly delta literals previously returned by
///     <c>GetPlotMonitoringSummaryQueryService</c>.
/// </para>
/// <para>
///     This is a stateless pure function. Registered as a singleton in DI.
/// </para>
/// </remarks>
public class ChillSeasonEvaluator
{
    /// <summary>
    ///     Evaluates the chill season state based on accumulated chill portions,
    ///     the plot's chill requirement, and the current date.
    /// </summary>
    /// <param name="accumulatedChillPortions">Accumulated chill portions from the latest statistic.</param>
    /// <param name="requirement">The plot's chill requirement.</param>
    /// <param name="now">Current timestamp (used for seasonal classification).</param>
    /// <returns>A <see cref="ChillSeasonState"/> with progress and status information.</returns>
    public ChillSeasonState Evaluate(
        decimal accumulatedChillPortions,
        ChillRequirement requirement,
        DateTimeOffset now)
    {
        var requirementPortions = (double)requirement.Portions.Value;
        var progress = requirementPortions > 0
            ? Math.Min(1.0, (double)accumulatedChillPortions / requirementPortions)
            : 1.0;

        var status = ClassifySeasonStatus(progress, now);

        return new ChillSeasonState
        {
            AccumulatedPortions = accumulatedChillPortions,
            RequiredPortions = (decimal)requirementPortions,
            ProgressRatio = progress,
            Status = status
        };
    }

    private static ChillSeasonStatus ClassifySeasonStatus(double progress, DateTimeOffset now)
    {
        // Northern-hemisphere heuristic: chill accumulation season is Oct–Feb.
        var month = now.Month;
        var inChillSeason = month >= 10 || month <= 2;

        if (progress >= 1.0)
            return ChillSeasonStatus.RequirementMet;

        if (inChillSeason && progress >= 0.5)
            return ChillSeasonStatus.OnTrack;

        if (inChillSeason)
            return ChillSeasonStatus.Accumulating;

        // Outside the chill season window.
        return progress > 0
            ? ChillSeasonStatus.OffSeason
            : ChillSeasonStatus.NotStarted;
    }
}

/// <summary>
///     Represents the evaluated state of the chill accumulation season for a plot.
/// </summary>
public record ChillSeasonState
{
    /// <summary>Accumulated chill portions.</summary>
    public decimal AccumulatedPortions { get; init; }

    /// <summary>Required chill portions for the plot.</summary>
    public decimal RequiredPortions { get; init; }

    /// <summary>Progress ratio (0.0 – 1.0).</summary>
    public double ProgressRatio { get; init; }

    /// <summary>Seasonal status classification.</summary>
    public ChillSeasonStatus Status { get; init; }
}

/// <summary>
///     Seasonal status classification for chill accumulation.
/// </summary>
public enum ChillSeasonStatus
{
    NotStarted,
    Accumulating,
    OnTrack,
    RequirementMet,
    OffSeason
}
