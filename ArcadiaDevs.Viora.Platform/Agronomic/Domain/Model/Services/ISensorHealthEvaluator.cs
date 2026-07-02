using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Domain service that derives a device's health/alert level from its
///     (simulated) telemetry. Pure function; singleton lifetime — stateless.
/// </summary>
/// <remarks>
///     C# port of the OS <c>SensorHealthEvaluator.java</c>. The alert level is
///     Viora's responsibility, not a manual choice: a device's operational
///     <c>status</c> (ACTIVE/INACTIVE) says whether the sensor is enabled, while
///     this evaluator computes whether its current readings are healthy. Each
///     reported metric is scored against the same thresholds the dashboard
///     cards use, and the <b>worst</b> metric wins. A device with no readings
///     is <see cref="GeneralHealthStatus.Unknown"/>.
/// </remarks>
public interface ISensorHealthEvaluator
{
    /// <summary>
    ///     Derives the health status implied by a device's current readings.
    /// </summary>
    /// <param name="readings">The current (simulated) telemetry; may be null.</param>
    /// <returns>The worst severity across reported metrics; <see cref="GeneralHealthStatus.Unknown"/> when none exist or when <paramref name="readings"/> is null.</returns>
    GeneralHealthStatus Evaluate(SensorReadings? readings);
}
