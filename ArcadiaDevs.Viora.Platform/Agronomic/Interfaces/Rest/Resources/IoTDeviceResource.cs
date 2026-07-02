using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Response DTO for an IoT device, including the current (simulated) telemetry.
/// </summary>
/// <remarks>
///     (TS012TASK005) Returned by <c>GET /api/v1/plots/{plotId}/iot-devices</c>
///     and the write endpoints (POST / PATCH / DELETE). The write path
///     (<see cref="Transform.IoTDeviceResourceFromEntityAssembler.ToResourceFromEntity"/>)
///     returns the 5 base fields with telemetry nulled; the read path
///     (<see cref="Transform.IoTDeviceResourceFromEntityAssembler.ToResourceFromReadout"/>)
///     populates all 10 fields from the readmodel.
///     <para>
///         <b>BREAKING (1.17.0, D15)</b>: the previous <c>CreatedAt</c> field
///         (a LIE — it was set to <c>DateTime.UtcNow</c> at response time, not
///         to the device's real creation time) is DROPPED. The new
///         <see cref="LastUpdate"/> field carries the simulator's
///         <c>CapturedAt</c> timestamp (the sensor-reading instant, not the
///         device-registration instant) and is serialized as an ISO-8601
///         string. 5 new nullable telemetry fields
///         (<see cref="Health"/>, <see cref="DeviceType"/>, <see cref="SoilMoisture"/>,
///         <see cref="Temperature"/>, <see cref="LeafHumidity"/>) round out the
///         OS parity (the OS's <c>IoTDeviceResource.java</c> has 10 fields).
///     </para>
/// </remarks>
/// <param name="Id">The unique device identifier.</param>
/// <param name="PlotId">The identifier of the associated plot.</param>
/// <param name="DeviceName">The human-readable name of the device.</param>
/// <param name="Status">The current operational status of the device.</param>
/// <param name="Health">The derived health/alert level (HEALTHY / WARNING / CRITICAL / UNKNOWN), or null.</param>
/// <param name="DeviceType">The sensor kind (SoilProbe / LeafWetness / WeatherStation), or null.</param>
/// <param name="SoilMoisture">Current soil moisture percentage, or null if not reported.</param>
/// <param name="Temperature">Current soil temperature in °C, or null if not reported.</param>
/// <param name="LeafHumidity">Current leaf humidity percentage, or null if not reported.</param>
/// <param name="LastUpdate">ISO-8601 instant of the latest reading, or null.</param>
public record IoTDeviceResource(
    int Id,
    int PlotId,
    string DeviceName,
    IoTDeviceStatus Status,
    string? Health,
    string? DeviceType,
    int? SoilMoisture,
    double? Temperature,
    int? LeafHumidity,
    string? LastUpdate);
