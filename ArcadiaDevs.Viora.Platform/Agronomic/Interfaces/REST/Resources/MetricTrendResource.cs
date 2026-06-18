using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     A metric's value compared against the previous comparable period.
/// </summary>
/// <param name="CurrentValue">Average over the current window, or null when no data exists.</param>
/// <param name="PreviousValue">Average over the previous window, or null when no data exists.</param>
/// <param name="Change">Difference (current minus previous), or null when not computable.</param>
/// <param name="ChangePercent">Percentage change relative to the previous value, or null.</param>
/// <param name="Direction">Classified direction of the change.</param>
public record MetricTrendResource(
    double? CurrentValue,
    double? PreviousValue,
    double? Change,
    double? ChangePercent,
    ETrendDirection Direction)
{
    /// <summary>
    ///     Creates a default stable unknown trend.
    /// </summary>
    public static MetricTrendResource StableUnknown()
    {
        return new MetricTrendResource(null, null, null, null, ETrendDirection.STABLE);
    }
}
