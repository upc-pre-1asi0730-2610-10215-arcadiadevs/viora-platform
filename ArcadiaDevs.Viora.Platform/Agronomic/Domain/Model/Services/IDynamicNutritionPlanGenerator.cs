using System;
using System.Collections.Generic;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Exceptions;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Domain port for the dynamic-nutrition plan generator. Encapsulates the
///     pure business rules that decide which nutrition inputs, dosages and
///     application window apply for a plot given its consolidated risk
///     profile and the current agronomic policy.
///     <para>
///         This is the second half of the A2 deliverable (A2 part 2 / PR-D2).
///         The first half (A2 part 1 / PR-D1) shipped the 3 per-risk
///         evaluators (<see cref="IChillDeficitEvaluator"/>,
///         <see cref="ILowNdviEvaluator"/>, <see cref="IHydricStressEvaluator"/>)
///         + the <see cref="AgronomicRiskProfile"/> VO + the BC-local
///         <c>EThreatType</c> enum + the <see cref="DynamicNutritionPolicy"/>
///         VO + the <see cref="DynamicNutritionPlanUnavailableException"/>.
///         This PR composes the per-risk evaluators' outputs into a
///         <see cref="DynamicNutritionPlan"/> aggregate.
///     </para>
///     <para>
///         Per locked decision #1 (engram #42, <c>FULL: 4 risks</c>), the
///         generator fires when ANY of the 5 <see cref="EThreatType"/>
///         values is present in <paramref name="risks"/> (ClimateHigh,
///         ClimateExtreme, ChillDeficit, LowNdvi, HydricStress). The
///         application-window length is driven by the climate risk level
///         when a climate risk is present; otherwise the high-risk window
///         applies (the non-climate triggers do not change the window
///         length, matching the OS's after-the-fact policy).
///     </para>
///     <para>
///         CC-7 contract: the generator throws
///         <see cref="DynamicNutritionPlanUnavailableException"/> on an
///         empty (or null) <paramref name="risks"/> collection. The command
///         service boundary catches the exception and converts it to
///         <c>Result.Failure(AgronomicErrors.NoTriggeringRisk)</c>. There
///         is no silent default — if no risk fires, no plan is created.
///     </para>
/// </summary>
public interface IDynamicNutritionPlanGenerator
{
    /// <summary>
    ///     Generates an <c>Active</c>
    ///     <see cref="DynamicNutritionPlan"/> aggregate for the given
    ///     plot condition, policy and risk set.
    /// </summary>
    /// <param name="userId">The owner user identifier.</param>
    /// <param name="plotId">The plot identifier.</param>
    /// <param name="risks">
    ///     The set of triggering <see cref="EThreatType"/> values
    ///     (typically the output of <c>AgronomicRiskTranslator.Translate</c>).
    ///     Must be non-empty; passing an empty or null collection throws
    ///     <see cref="DynamicNutritionPlanUnavailableException"/> (CC-7).
    /// </param>
    /// <param name="profile">
    ///     The read-only input profile (climate, NDVI, weather,
    ///     chill requirement, latest statistic). Carried in the
    ///     generator's contract for traceability and to drive the
    ///     rationale + window; not used to decide whether a plan is
    ///     warranted (that decision lives on <paramref name="risks"/>).
    /// </param>
    /// <param name="policy">
    ///     The validated, immutable policy VO. Carries the dosages,
    ///     window-day counts and chill-deficit ratio.
    /// </param>
    /// <param name="generatedDate">
    ///     The <see cref="DateTimeOffset"/> the plan is generated on
    ///     (provided by the caller's <c>IClock</c>; never
    ///     <c>DateTimeOffset.UtcNow</c> inline so the command
    ///     service stays testable).
    /// </param>
    /// <returns>
    ///     A new <see cref="DynamicNutritionPlan"/> in
    ///     <c>ENutritionPlanStatus.Active</c> state, with the
    ///     rationale + window + 3 input recommendations derived
    ///     from the policy and the risk set.
    /// </returns>
    /// <exception cref="DynamicNutritionPlanUnavailableException">
    ///     Thrown when <paramref name="risks"/> is null or empty
    ///     (no triggering risk → no plan, CC-7).
    /// </exception>
    DynamicNutritionPlan GeneratePlan(
        int userId,
        int plotId,
        IReadOnlyCollection<EThreatType> risks,
        AgronomicRiskProfile profile,
        DynamicNutritionPolicy policy,
        DateTimeOffset generatedDate);
}
