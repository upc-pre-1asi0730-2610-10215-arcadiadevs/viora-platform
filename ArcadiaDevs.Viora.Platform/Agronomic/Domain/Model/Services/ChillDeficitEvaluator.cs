using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Pure-function implementation of <see cref="IChillDeficitEvaluator"/>.
///     <para>
///         A deficit is reported when
///         <c>accumulated &lt; requirement.Portions × ChillDeficitRatio</c>.
///         The ratio is read from
///         <c>DynamicNutritionPolicyOptions.ChillDeficitRatio</c> at call
///         time (the additive 1-line field added in this PR to the
///         options class) so the trigger is policy-driven rather than
///         hard-coded.
///     </para>
///     <para>
///         Defensive: a <c>null</c> requirement OR a <c>null</c> accumulated
///         value both return <c>false</c> (no data means no trigger).
///         The strict less-than comparison matches the spec's "deficit
///         is observed when the requirement is not met" semantics.
///     </para>
/// </summary>
public sealed class ChillDeficitEvaluator : IChillDeficitEvaluator
{
    /// <summary>
    ///     Hard-coded fallback ratio used only when the
    ///     <c>DynamicNutritionPolicyOptions</c> instance has not been
    ///     bound (e.g. in a unit test that exercises the evaluator
    ///     without DI). Matches the policy default so production and
    ///     test behaviour line up.
    /// </summary>
    private const decimal DefaultChillDeficitRatio = 0.7m;

    private readonly IOptions<DynamicNutritionPolicyOptions> _options;

    /// <summary>
    ///     Builds a new <see cref="ChillDeficitEvaluator"/> with the
    ///     configured policy.
    /// </summary>
    /// <param name="options">The bound options carrying the <c>ChillDeficitRatio</c>.</param>
    public ChillDeficitEvaluator(IOptions<DynamicNutritionPolicyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public bool HasDeficit(ChillRequirement? requirement, decimal? accumulated)
    {
        if (requirement is null || !accumulated.HasValue)
        {
            return false;
        }

        var ratio = _options.Value?.ChillDeficitRatio ?? DefaultChillDeficitRatio;

        // ChillPortions.Value is double (mirrors the OS record); the
        // comparison runs in decimal so the ratio arithmetic stays in
        // the same type as the policy field.
        var threshold = (decimal)requirement.Portions.Value * ratio;
        return accumulated.Value < threshold;
    }
}
