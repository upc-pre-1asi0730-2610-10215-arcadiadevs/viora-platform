namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     State carried over between ingestion windows for the Dynamic Model.
/// </summary>
public record ChillModelState(
    double IntermediateProduct,
    double? PreviousHourTemperatureCelsius,
    double? PriorHourTemperatureCelsius)
{
    public static ChillModelState Empty()
    {
        return new ChillModelState(0.0, null, null);
    }
}
