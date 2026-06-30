using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Pure-function evaluator that decides whether a plot's accumulated
///     chill portions are below the deficit threshold derived from the
///     <c>DynamicNutritionPolicy.ChillDeficitRatio</c> field.
///     <para>
///         One of the 3 per-risk evaluators introduced in A2 part 1 (this
///         PR). The <c>RecommendDynamicNutritionPlanCommandService</c>
///         (refactored in PR-D2) composes the outputs of all 3 evaluators
///         into the <see cref="AgronomicRiskProfile"/> consumed by
///         <c>IDynamicNutritionPlanGenerator</c>.
///     </para>
///     <para>
///         The implementation is stateless and registers as a DI singleton
///         (<c>ChillDeficitEvaluator</c>); the ratio is read from
///         <c>IOptions&lt;DynamicNutritionPolicyOptions&gt;</c> at call
///         time so configuration changes are picked up without a restart.
///     </para>
/// </summary>
public interface IChillDeficitEvaluator
{
    /// <summary>
    ///     Returns <c>true</c> when the accumulated chill portions are
    ///     below the policy-derived deficit threshold, <c>false</c>
    ///     otherwise. Defensive: a <c>null</c> requirement or a
    ///     <c>null</c> accumulated value both return <c>false</c> (no
    ///     data means no trigger).
    /// </summary>
    /// <param name="requirement">The plot's chill requirement; nullable.</param>
    /// <param name="accumulated">The latest accumulated chill portions for the plot; nullable.</param>
    /// <returns>
    ///     <c>true</c> when a chill deficit is observed; <c>false</c> when
    ///     the requirement is met, unknown, or the inputs are not yet
    ///     available.
    /// </returns>
    bool HasDeficit(ChillRequirement? requirement, decimal? accumulated);
}
