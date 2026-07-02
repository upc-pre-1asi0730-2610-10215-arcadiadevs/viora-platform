namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Sensor kind encoded in the prefix of an <see cref="ActivationCode"/>.
///     <para>
///         Each value corresponds to the two-letter token that follows
///         <c>VIORA-</c> in a well-formed activation code:
///         <c>SP</c> = soil probe, <c>LW</c> = leaf wetness, <c>WS</c> = weather station.
///     </para>
///     <para>
///         This is a BC-local enum (CC-11). It coexists with
///         <c>Surveillance.Domain.Model.ValueObjects.EThreatType</c> (13 values);
///         the C# namespaces keep them unambiguous. Use the fully-qualified name
///         at every call site that crosses BC boundaries.
///     </para>
///     <para>
///         The per-type predicate methods (<c>ReportsSoilMoisture()</c> etc.)
///         live as <see cref="IoTDeviceTypeExtensions"/> in this file because C#
///         disallows instance methods on <c>enum</c> types — extension methods
///         are the idiomatic C# equivalent of the OS's Java instance methods
///         (<c>IoTDeviceType.java:22-34</c>).
///     </para>
/// </summary>
public enum IoTDeviceType
{
    /// <summary>Soil moisture + soil temperature sensor.</summary>
    SoilProbe,

    /// <summary>Leaf humidity / wetness sensor.</summary>
    LeafWetness,

    /// <summary>Multi-metric weather station.</summary>
    WeatherStation
}

/// <summary>
///     Per-device-type predicates that mirror the OS's Java instance methods
///     on <c>IoTDeviceType.java:22-34</c>. Implemented as C# extension methods
///     because C# disallows instance methods on <c>enum</c> types.
/// </summary>
public static class IoTDeviceTypeExtensions
{
    /// <summary>
    ///     Reports whether the device carries a soil-moisture sensor (soil probes
    ///     and weather stations). Mirrors <c>IoTDeviceType.java:22-26</c>.
    /// </summary>
    public static bool ReportsSoilMoisture(this IoTDeviceType type) => type != IoTDeviceType.LeafWetness;

    /// <summary>
    ///     Reports whether the device carries a soil-temperature sensor (soil
    ///     probes and weather stations). Mirrors <c>IoTDeviceType.java:28-30</c>.
    /// </summary>
    public static bool ReportsSoilTemperature(this IoTDeviceType type) => type != IoTDeviceType.LeafWetness;

    /// <summary>
    ///     Reports whether the device carries a leaf-humidity sensor (leaf-wetness
    ///     sensors and weather stations). Mirrors <c>IoTDeviceType.java:32-34</c>.
    /// </summary>
    public static bool ReportsLeafHumidity(this IoTDeviceType type) => type != IoTDeviceType.SoilProbe;
}
