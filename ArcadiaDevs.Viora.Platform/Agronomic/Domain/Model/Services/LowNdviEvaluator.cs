using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Pure-function implementation of <see cref="ILowNdviEvaluator"/>.
///     Compares the latest <c>AgronomicStatistic.NdviValue</c> against
///     the policy's <c>HighRiskNdviThreshold</c> using a strict
///     less-than comparison (an NDVI exactly equal to the threshold is
///     NOT a trigger, matching the OS "NDVI can only RAISE risk" rule).
///     <para>
///         A <c>null</c> latest statistic returns <c>false</c> — no
///         data means no trigger; the caller surfaces the missing
///         imagery separately (a "no data" case is different from a
///         "low NDVI" case).
///     </para>
/// </summary>
public sealed class LowNdviEvaluator : ILowNdviEvaluator
{
    /// <inheritdoc />
    public bool IsBelowThreshold(AgronomicStatistic? latest, DynamicNutritionPolicyOptions policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (latest is null)
        {
            return false;
        }

        // AgronomicStatistic.NdviValue is double (mirrors the OS aggregate);
        // the comparison runs in decimal so it matches the policy field type.
        return (decimal)latest.NdviValue < policy.HighRiskNdviThreshold;
    }
}
