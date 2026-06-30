using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Pure-function evaluator that decides whether the latest
///     <c>AgronomicStatistic.NdviValue</c> for a plot falls below the
///     policy's <c>HighRiskNdviThreshold</c>.
///     <para>
///         One of the 3 per-risk evaluators introduced in A2 part 1 (this
///         PR). Compared strictly less-than: an NDVI exactly equal to the
///         threshold is NOT a trigger (matches the OS "NDVI can only
///         RAISE" semantics, mirrored in the spec).
///     </para>
/// </summary>
public interface ILowNdviEvaluator
{
    /// <summary>
    ///     Returns <c>true</c> when the latest NDVI is strictly below
    ///     the configured high-risk threshold, <c>false</c> otherwise
    ///     (including when the latest statistic is <c>null</c> — no
    ///     data means no trigger).
    /// </summary>
    /// <param name="latest">The most recent <see cref="AgronomicStatistic"/> for the plot; nullable.</param>
    /// <param name="policy">The configured dynamic-nutrition policy whose <c>HighRiskNdviThreshold</c> drives the comparison.</param>
    /// <returns>
    ///     <c>true</c> when a low-NDVI risk is observed; <c>false</c>
    ///     otherwise.
    /// </returns>
    bool IsBelowThreshold(AgronomicStatistic? latest, DynamicNutritionPolicyOptions policy);
}
