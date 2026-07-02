using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Worst-wins health evaluator for IoT device sensor readings.
///     <para>
///         Each reported metric is scored against the OS's frozen thresholds
///         (N12 contract for 1.16.2's <c>HydricStressDetectedIntegrationEvent</c>
///         producer): the worst severity wins. A device with no metrics
///         reported — or with null readings — resolves to
///         <see cref="GeneralHealthStatus.Unknown"/>.
///     </para>
///     <para>
///         C# port of the OS <c>SensorHealthEvaluator.java</c>. The 3 thresholds
///         (and their assignment to severity 0/1/2/-1) are byte-for-byte from
///         the OS source; any future change to them is a separate spec decision
///         because 1.16.2's producer will key off
///         <c>evaluator.Evaluate(readings) == Critical &amp;&amp; readings.SoilMoisture &lt; 20</c>.
///     </para>
/// </summary>
public sealed class SensorHealthEvaluator : ISensorHealthEvaluator
{
    /// <inheritdoc />
    public GeneralHealthStatus Evaluate(SensorReadings? readings)
    {
        if (readings is null)
        {
            return GeneralHealthStatus.Unknown;
        }

        int severity = -1;
        severity = Math.Max(severity, SoilMoistureSeverity(readings.SoilMoisture));
        severity = Math.Max(severity, SoilTemperatureSeverity(readings.SoilTemperature));
        severity = Math.Max(severity, LeafHumiditySeverity(readings.LeafHumidity));

        return severity switch
        {
            2 => GeneralHealthStatus.Critical,
            1 => GeneralHealthStatus.Warning,
            0 => GeneralHealthStatus.Healthy,
            _ => GeneralHealthStatus.Unknown
        };
    }

    /// <summary>
    ///     Dry soil is the risky end: below the wilting threshold is critical.
    ///     Mirrors <c>SensorHealthEvaluator.java:46-54</c>. The
    ///     <c>&lt; 20</c> boundary is 1.16.2's
    ///     <c>HydricStressDetectedIntegrationEvent</c> producer threshold.
    /// </summary>
    private static int SoilMoistureSeverity(int? soilMoisture)
    {
        if (soilMoisture is null) return -1;
        if (soilMoisture < 20) return 2;
        return soilMoisture <= 35 ? 1 : 0;
    }

    /// <summary>
    ///     Hot soil compounds water stress. Mirrors <c>SensorHealthEvaluator.java:57-65</c>.
    /// </summary>
    private static int SoilTemperatureSeverity(double? soilTemperature)
    {
        if (soilTemperature is null) return -1;
        if (soilTemperature > 30.0) return 2;
        return soilTemperature >= 25.0 ? 1 : 0;
    }

    /// <summary>
    ///     Prolonged canopy wetness is the risky end (fungal disease). Mirrors
    ///     <c>SensorHealthEvaluator.java:68-76</c>.
    /// </summary>
    private static int LeafHumiditySeverity(int? leafHumidity)
    {
        if (leafHumidity is null) return -1;
        if (leafHumidity > 85) return 2;
        return leafHumidity >= 70 ? 1 : 0;
    }
}
