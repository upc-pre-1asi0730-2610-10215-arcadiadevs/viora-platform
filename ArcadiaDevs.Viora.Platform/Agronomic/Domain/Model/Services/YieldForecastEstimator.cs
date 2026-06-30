using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Pure-function yield estimator. Replaces the legacy hard-coded
///     <c>simulatedYieldProjection = 4500m</c> literal in
///     <c>MonitoringSummaryQueryService</c> with a deterministic, policy-driven
///     calculation that reacts to the plot's actual NDVI and chill adequacy.
/// </summary>
/// <remarks>
///     <para>
///         <strong>Formula</strong>
///         (matches the design sketch in §5.1 of the Phase 2 design; deliberately
///         simpler than the OS's <c>YieldForecastEstimator.java</c>):
///         <code>
///             baseYield        = 5.5  // tonnes per hectare (OS default)
///             ndviMultiplier   = clamp(0.5 + 0.7 * ndvi, 0.5, 1.2)
///             chillRatio       = min(1, accumulatedChillPortions / requirementPortions)
///             yieldPerHectare  = baseYield * ndviMultiplier * chillRatio
///             return round(yieldPerHectare, 2)
///         </code>
///     </para>
///     <para>
///         <strong>Bounds:</strong> NDVI 0.0 → 0.5× multiplier, NDVI 1.0 → 1.2× multiplier
///         (clamped). Chill ratio caps at 1.0 (100% of requirement met). A
///         <c>null</c> statistic yields the floor (multiplier 0.5×, chill
///         ratio 0.0) so the caller surfaces a real 0m/0.0 rather than a
///         fabricated constant.
///     </para>
///     <para>
///         <strong>No fabricated-data fallback</strong> (CC-8). A 0m or low
///         multiplier is the real answer when there is no imagery; the caller
///         decides whether to surface that as a warning or a failure.
///     </para>
/// </remarks>
public sealed class YieldForecastEstimator : IYieldForecastEstimator
{
    /// <summary>
    ///     Base potential yield per hectare (tonnes). Mirrors the OS
    ///     <c>YieldEstimationPolicy.baseYieldTonnesPerHectare</c> default.
    /// </summary>
    private const decimal BaseYieldTonnesPerHectare = 5.5m;

    /// <summary>
    ///     Lower bound on the NDVI multiplier. At NDVI = 0 (no vegetation)
    ///     the multiplier is exactly this value.
    /// </summary>
    private const decimal NdviMultiplierFloor = 0.5m;

    /// <summary>
    ///     Upper bound on the NDVI multiplier. At NDVI = 1.0 (full canopy)
    ///     the multiplier is clamped to this value.
    /// </summary>
    private const decimal NdviMultiplierCeiling = 1.2m;

    /// <summary>
    ///     Linear slope of the NDVI multiplier. At NDVI = 0.0 the multiplier
    ///     is the floor (0.5); at NDVI = 1.0 it is 0.5 + 0.7 = 1.2 (exactly
    ///     the ceiling, no overshoot).
    /// </summary>
    private const decimal NdviMultiplierSlope = 0.7m;

    /// <inheritdoc />
    public decimal Estimate(
        Plot plot,
        AgronomicStatistic? latestStatistic,
        ChillRequirement chillRequirement,
        DynamicNutritionPolicyOptions policy)
    {
        ArgumentNullException.ThrowIfNull(plot);
        ArgumentNullException.ThrowIfNull(chillRequirement);
        ArgumentNullException.ThrowIfNull(policy);

        // NDVI multiplier in [floor, ceiling] (0.5 .. 1.2).
        // A null statistic yields the floor (no imagery data → no claim of vigor).
        var ndvi = latestStatistic is null ? 0m : (decimal)latestStatistic.NdviValue;
        var ndviMultiplier = Clamp(NdviMultiplierFloor + NdviMultiplierSlope * ndvi,
            NdviMultiplierFloor, NdviMultiplierCeiling);

        // Chill adequacy ratio. A null statistic has zero chill portions;
        // a zero requirement (degenerate input) is treated as fully adequate
        // so the multiplier doesn't artificially zero-out the yield.
        var requirementPortions = (decimal)chillRequirement.Portions.Value;
        var accumulatedPortions = latestStatistic is null ? 0m : (decimal)latestStatistic.ChillPortions;
        var chillRatio = requirementPortions > 0m
            ? Math.Min(1m, accumulatedPortions / requirementPortions)
            : 1m;

        // Policy is currently unused in the math (it gates the validator
        // and the future generator in PR-D2) but is part of the signature
        // so the estimator stays deterministic per configuration.
        _ = policy;

        var yieldTonnes = BaseYieldTonnesPerHectare * ndviMultiplier * chillRatio;
        return Math.Round(Math.Max(0m, yieldTonnes), 2);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
