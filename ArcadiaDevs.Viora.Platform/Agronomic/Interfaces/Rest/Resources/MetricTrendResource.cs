using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record MetricTrendResource(
    double? CurrentValue,
    double? PreviousValue,
    double? Change,
    double? ChangePercent,
    ETrendDirection Direction)
{
    public static MetricTrendResource StableUnknown()
    {
        return new MetricTrendResource(null, null, null, null, ETrendDirection.STABLE);
    }
}
