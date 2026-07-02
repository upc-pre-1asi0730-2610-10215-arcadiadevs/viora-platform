namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Current (simulated) telemetry of an IoT device. Metrics a device does not
///     report for its <see cref="IoTDeviceType"/> are <c>null</c>.
/// </summary>
/// <remarks>
///     Ported from the OS <c>SensorReadings.java</c> (lines 14-19). The plural
///     name matches the OS convention: this record holds a snapshot of multiple
///     metrics, not one. The OS's <c>Instant capturedAt</c> is mapped to
///     <see cref="DateTimeOffset"/> for .NET; the simulator's <c>CapturedAt</c>
///     is the deterministic sensor-reading instant derived from the device seed
///     and the call-site <c>now</c>.
/// </remarks>
/// <param name="SoilMoisture">
///     Soil moisture percentage (0-100), or <c>null</c> when the device type
///     does not report it.
/// </param>
/// <param name="SoilTemperature">
///     Soil temperature in °C (rounded to one decimal), or <c>null</c> when the
///     device type does not report it.
/// </param>
/// <param name="LeafHumidity">
///     Leaf humidity percentage (0-100), or <c>null</c> when the device type
///     does not report it.
/// </param>
/// <param name="CapturedAt">
///     The instant the reading was produced (UTC). Never null.
/// </param>
public sealed record SensorReadings(
    int? SoilMoisture,
    double? SoilTemperature,
    int? LeafHumidity,
    DateTimeOffset CapturedAt);
