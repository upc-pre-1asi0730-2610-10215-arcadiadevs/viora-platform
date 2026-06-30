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
