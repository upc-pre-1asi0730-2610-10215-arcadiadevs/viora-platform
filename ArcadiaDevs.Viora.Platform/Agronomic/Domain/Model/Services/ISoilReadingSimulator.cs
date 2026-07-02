using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Domain service that produces a device's current soil/canopy telemetry.
///     <para>
///         Viora has no physical sensor hardware, so readings are
///         <b>simulated</b> rather than ingested. The simulation is a pure,
///         deterministic function of the device's activation code (per-device
///         seed), the plot's geography (latitude → hemisphere, climate band and
///         seasonal swing; longitude → local solar hour), the time of day, the
///         season, and the plot's latest NDVI (canopy vigor → water status
///         nudge). It performs no external calls and no persistence, so it never
///         adds backend load: the values are computed on read.
///     </para>
///     <para>
///         Because it is seeded by the activation code, a given device reads
///         consistently and evolves smoothly across the day (hotter, drier
///         afternoons; cooler, moister nights; wetter soil on vigorous plots),
///         which keeps the dashboard coherent with the plot's real season and
///         location.
///     </para>
///     <para>
///         This is a C# port of the OS <c>SoilReadingSimulator.java</c>. The
///         core <see cref="UnitOffset"/> helper is preserved byte-for-byte from
///         the OS's GLSL <c>fract(sin(...))</c> hash (R7, NON-NEGOTIABLE) so the
///         deterministic seed yields the same per-device visual offset as the
///         sibling Java service.
///     </para>
/// </summary>
public interface ISoilReadingSimulator
{
    /// <summary>
    ///     Produces the current readings for a device.
    /// </summary>
    /// <param name="code">The device activation code (per-device seed); may be null.</param>
    /// <param name="type">The sensor kind, which decides the reported metrics.</param>
    /// <param name="location">The plot's representative point (centroid); may be null.</param>
    /// <param name="latestNdvi">The plot's most recent NDVI (canopy vigor), or null.</param>
    /// <param name="now">The reference instant.</param>
    /// <returns>The simulated readings, with unreported metrics left null.</returns>
    SensorReadings Simulate(
        ActivationCode? code,
        IoTDeviceType type,
        GeoPoint? location,
        double? latestNdvi,
        DateTimeOffset now);
}
