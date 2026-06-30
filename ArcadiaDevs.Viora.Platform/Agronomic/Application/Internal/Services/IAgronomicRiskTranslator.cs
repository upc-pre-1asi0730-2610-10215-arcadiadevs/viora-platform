using System.Collections.Generic;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;

/// <summary>
///     Pure-function application port that maps the observed risk signal
///     onto the set of <see cref="EThreatType"/> values consumed by
///     <c>IDynamicNutritionPlanGenerator</c> (A2 part 2 / PR-D2).
///     <para>
///         The translator is the single point where the 4-risk trigger
///         expansion (locked decision #1: <c>FULL: 4 risks</c>) is
///         realised. It collapses the 3 per-risk evaluators' outputs
///         (<c>chillDeficit</c>, <c>lowNdvi</c>, <c>hydricStress</c>)
///         and the snapshot's <see cref="ClimateRiskLevel"/> into one
///         <c>IReadOnlyCollection&lt;EThreatType&gt;</c> the generator
///         can iterate. The mapping is deterministic; an empty input
///         yields an empty collection (the generator raises
///         <c>DynamicNutritionPlanUnavailableException</c> on empty,
///         per CC-7).
///     </para>
///     <para>
///         The translator lives in <c>Application/Internal/Services/</c>
///         (NOT in <c>Domain/Model/Services/</c>) because it is a
///         CQRS-layer helper: it does not own domain state, it is
///         consumed exclusively by the <c>RecommendDynamicNutritionPlan
///         CommandService</c>, and it depends on no aggregate. Mirrors
///         the OS's package layout (<c>application/internal</c>).
///     </para>
/// </summary>
public interface IAgronomicRiskTranslator
{
    /// <summary>
    ///     Translates the per-risk boolean signals + the snapshot's
    ///     climate risk level into the <see cref="EThreatType"/> set
    ///     the generator iterates.
    /// </summary>
    /// <param name="climate">
    ///     The <see cref="ClimateRiskLevel"/> on the latest
    ///     <c>WeatherSnapshot</c> for the plot.
    /// </param>
    /// <param name="chillDeficit">
    ///     Output of <c>IChillDeficitEvaluator.HasDeficit</c>.
    /// </param>
    /// <param name="lowNdvi">
    ///     Output of <c>ILowNdviEvaluator.IsBelowThreshold</c>.
    /// </param>
    /// <param name="hydricStress">
    ///     Output of <c>IHydricStressEvaluator.IsUnderStress</c>.
    /// </param>
    /// <returns>
    ///     The <see cref="EThreatType"/> set. Empty when no signal
    ///     fires; the generator converts an empty set into
    ///     <c>AgronomicErrors.NoTriggeringRisk</c> at the command
    ///     service boundary (CC-7).
    /// </returns>
    IReadOnlyCollection<EThreatType> Translate(
        ClimateRiskLevel climate,
        bool chillDeficit,
        bool lowNdvi,
        bool hydricStress);
}
