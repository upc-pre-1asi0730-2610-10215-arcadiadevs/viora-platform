using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Domain port that estimates the projected yield for a plot from real,
///     provider-derived inputs (NDVI from the latest <see cref="AgronomicStatistic"/>
///     and the plot's chill adequacy) plus the agronomic policy that drives
///     thresholds and dosages.
/// </summary>
/// <remarks>
///     This is a <strong>pure function</strong> with no I/O. The implementation
///     is registered as a singleton in <c>Program.cs</c> so the same math is
///     applied consistently across all callers. The signature follows design
///     <c>§5.1</c> of the Phase 2 design (engram #45).
/// </remarks>
public interface IYieldForecastEstimator
{
    /// <summary>
    ///     Estimates the projected yield (in tonnes, rounded to 2 decimals)
    ///     for a plot given the latest agronomic statistic, the plot's chill
    ///     requirement, and the dynamic-nutrition policy.
    /// </summary>
    /// <param name="plot">The plot being summarised.</param>
    /// <param name="latestStatistic">
    ///     The most recent <see cref="AgronomicStatistic"/> for the plot, or
    ///     <c>null</c> if no imagery has been ingested yet. A <c>null</c>
    ///     statistic degrades the estimate to the floor (multiplier = 0.5).
    /// </param>
    /// <param name="chillRequirement">
    ///     The plot's chill requirement (denominator for the chill ratio).
    /// </param>
    /// <param name="policy">
    ///     The configured dynamic-nutrition policy. Carried in the call
    ///     signature so the estimator is a deterministic pure function and
    ///     can be unit-tested without DI.
    /// </param>
    /// <returns>Estimated yield in tonnes, rounded to 2 decimals, never negative.</returns>
    decimal Estimate(
        Plot plot,
        AgronomicStatistic? latestStatistic,
        ChillRequirement chillRequirement,
        DynamicNutritionPolicyOptions policy);
}
