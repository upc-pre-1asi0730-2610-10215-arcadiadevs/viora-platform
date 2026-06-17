namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Provides extension methods for the <see cref="EChillMetricModel"/> enum to
///     replicate Java's enum complex behavior (displayName and unitLabel).
/// </summary>
public static class EChillMetricModelExtensions
{
    /// <summary>
    ///     Gets the display name of the chill metric model.
    /// </summary>
    public static string DisplayName(this EChillMetricModel model) => model switch
    {
        EChillMetricModel.ChillingHours => "Chilling Hours",
        EChillMetricModel.Utah => "Utah",
        EChillMetricModel.Dynamic => "Dynamic Model",
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
    };

    /// <summary>
    ///     Gets the unit label of the chill metric model.
    /// </summary>
    public static string UnitLabel(this EChillMetricModel model) => model switch
    {
        EChillMetricModel.ChillingHours => "CH",
        EChillMetricModel.Utah => "CU",
        EChillMetricModel.Dynamic => "CP",
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
    };
}
