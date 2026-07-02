using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Deterministic seeded simulator for IoT device soil/canopy telemetry.
///     Pure function: no I/O, no AgroMonitoring call, no DB read, no clock read
///     (<paramref name="now"/> is injected). Singleton lifetime — stateless.
///     <para>
///         C# port of the OS <c>SoilReadingSimulator.java</c> (120 lines). The
///         algorithm is preserved line-for-line; the <see cref="UnitOffset"/>
///         helper is the byte-for-byte equivalent of the OS's GLSL
///         <c>fract(sin(...))</c> hash (R7, NON-NEGOTIABLE — magic numbers
///         <c>12.9898</c> and <c>43758.5453</c> + salts <c>11/23/31</c> must
///         stay identical to keep cross-stack visual parity on the per-device
///         seed offset).
///     </para>
/// </summary>
public sealed class SoilReadingSimulator : ISoilReadingSimulator
{
    /// <inheritdoc />
    public SensorReadings Simulate(
        ActivationCode? code,
        IoTDeviceType type,
        (double Latitude, double Longitude)? location,
        double? latestNdvi,
        DateTimeOffset now)
    {
        // R10: C# string.GetHashCode() is NOT stable across .NET versions; the
        // OS's Java String.hashCode() IS stable. The seed is per-device (visual
        // variety) — cross-stack parity is at the algorithm level, not at the
        // per-seed-value level. This is the accepted divergence (spec R10,
        // design D-D10).
        long seed = code != null ? code.Value.GetHashCode() : 0L;
        double latitude = location.HasValue ? location.Value.Latitude : 0.0;
        double longitude = location.HasValue ? location.Value.Longitude : 0.0;

        var utc = now.UtcDateTime;
        int dayOfYear = utc.DayOfYear;
        double localHour = FloorMod((long)Math.Round(utc.Hour + longitude / 15.0), 24L);

        // Seasonal warmth: positive in the local hemisphere's summer. Day 80 ≈
        // the March equinox, so the northern peak lands near the June solstice.
        double hemisphere = latitude >= 0 ? 1.0 : -1.0;
        double seasonal = Math.Sin(2.0 * Math.PI * (dayOfYear - 80) / 365.0) * hemisphere; // [-1, 1]
        double warmth01 = (seasonal + 1.0) / 2.0;

        // Diurnal warmth: peaks mid-afternoon (~15h), bottoms before dawn (~3h).
        double diurnal = Math.Sin(2.0 * Math.PI * (localHour - 9.0) / 24.0); // [-1, 1]
        double diurnal01 = (diurnal + 1.0) / 2.0;

        double absLat = Math.Abs(latitude);
        double annualMean = Clamp(27.0 - 0.45 * absLat, -5.0, 30.0);
        double seasonalAmplitude = 2.0 + 0.30 * absLat;
        double diurnalAmplitude = 6.0;

        double airTemperature = annualMean
            + seasonalAmplitude * seasonal
            + diurnalAmplitude * diurnal
            + 1.5 * UnitOffset(seed, 11);

        // Soil temperature lags and dampens the air temperature.
        double soilTemperature = annualMean + 0.6 * (airTemperature - annualMean);

        // Soil moisture falls with heat/sun; vigorous canopy (high NDVI) reads moister.
        double dryness = 0.6 * warmth01 + 0.4 * diurnal01; // [0, 1]
        double moisture = 55.0 - 30.0 * dryness;
        if (latestNdvi != null)
        {
            moisture += Clamp((latestNdvi.Value - 0.4) * 25.0, -8.0, 12.0);
        }
        moisture += 5.0 * UnitOffset(seed, 23);
        int soilMoisture = (int)Math.Round(Clamp(moisture, 5.0, 95.0));

        // Leaf humidity is high overnight/at dawn and tracks soil water status.
        double leaf = 90.0 - 45.0 * diurnal01 + (moisture - 40.0) * 0.2 + 5.0 * UnitOffset(seed, 31);
        int leafHumidity = (int)Math.Round(Clamp(leaf, 20.0, 99.0));

        // Stagger the "last reading" a little so devices don't all share a timestamp.
        DateTimeOffset capturedAt = now.AddSeconds(-(FloorMod(seed, 180L) + 30L));

        return new SensorReadings(
            type.ReportsSoilMoisture() ? soilMoisture : null,
            type.ReportsSoilTemperature() ? Round1(soilTemperature) : null,
            type.ReportsLeafHumidity() ? leafHumidity : null,
            capturedAt);
    }

    /// <summary>
    ///     Deterministic per-device offset in <c>[-1, 1)</c>, salted so metrics differ.
    ///     <para>
    ///         R7 (NON-NEGOTIABLE): byte-for-byte from OS
    ///         <c>SoilReadingSimulator.java:108-111</c>. GLSL <c>fract(sin(...))</c>
    ///         hash. Magic numbers <c>12.9898</c> / <c>43758.5453</c> and the salt
    ///         scheme <c>11/23/31</c> MUST stay byte-for-byte so the per-device
    ///         visual offset matches the OS stack.
    ///     </para>
    /// </summary>
    private static double UnitOffset(long seed, int salt)
    {
        double s = Math.Sin((seed * 31L + salt) * 12.9898) * 43758.5453;
        return (s - Math.Floor(s)) * 2.0 - 1.0;
    }

    private static double Clamp(double value, double low, double high)
    {
        return Math.Max(low, Math.Min(high, value));
    }

    private static double Round1(double value)
    {
        return Math.Round(value * 10.0) / 10.0;
    }

    /// <summary>
    ///     Java-compatible <c>Math.floorMod</c>: the floor modulo (sign follows divisor).
    ///     Mirrors <c>java.lang.Math.floorMod(long, long)</c> in the OS.
    /// </summary>
    private static long FloorMod(long a, long b)
    {
        return ((a % b) + b) % b;
    }
}
