using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Read-only input to <c>IDynamicNutritionPlanGenerator</c>
///     (A2 part 2 / PR-D2). Carries every piece of agronomic context the
///     generator needs to decide whether a plan is warranted and to derive
///     its rationale, window and recommendations.
///     <para>
///         Modelled as a single VO (rather than a tuple of separate
///         inputs) so the generator's contract is explicit and the
///         composing command service has one object to thread through
///         the per-risk evaluators before the call.
///     </para>
///     <para>
///         <see cref="ChillRequirement"/> and <see cref="LatestStatistic"/>
///         are nullable because the generator must still produce a
///         sensible answer (or refuse with
///         <c>DynamicNutritionPlanUnavailableException</c>) when the
///         pipeline has not yet accumulated enough data for the plot.
///     </para>
/// </summary>
/// <param name="ClimateRiskLevel">
///     The climate risk level reported on the latest
///     <see cref="WeatherSnapshot"/> for the plot.
/// </param>
/// <param name="NdviValue">
///     The latest <c>AgronomicStatistic.NdviValue</c> for the plot,
///     wrapped as a value object so the range invariant is enforced
///     (the underlying double is the same value the aggregate stores).
/// </param>
/// <param name="WeatherSnapshot">
///     The latest weather snapshot for the plot (temperature + status
///     + climate risk level + last validated reading timestamp).
/// </param>
/// <param name="ChillRequirement">
///     The plot's chill requirement (denominator for the chill
///     adequacy ratio). Nullable when the plot has no resolved
///     chill requirement yet.
/// </param>
/// <param name="LatestStatistic">
///     The most recent <see cref="AgronomicStatistic"/> for the plot,
///     or <c>null</c> when no imagery has been ingested yet.
/// </param>
public sealed record AgronomicRiskProfile(
    ClimateRiskLevel ClimateRiskLevel,
    NdviValue NdviValue,
    WeatherSnapshot WeatherSnapshot,
    ChillRequirement? ChillRequirement,
    AgronomicStatistic? LatestStatistic);
