using ArcadiaDevs.Viora.Platform.Agronomic.Application.ReadModels;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Domain;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Assembles an <see cref="IoTDeviceResource"/> from an <see cref="IoTDevice"/> aggregate
///     or an <see cref="IoTDeviceReadout"/> readmodel.
/// </summary>
/// <remarks>
///     (TS012TASK005) Converts the domain aggregate (or its telemetry readmodel)
///     into the REST response DTO without exposing internal aggregate state directly.
///     <para>
///         Two paths:
///         <list type="bullet">
///             <item>
///                 <see cref="ToResourceFromEntity"/> — write responses (POST / PATCH / DELETE)
///                 where telemetry is not relevant. The 5 base fields are populated
///                 from the aggregate; the 5 telemetry fields are <c>null</c>; the
///                 <c>LastUpdate</c> field is set to <c>DateTime.UtcNow</c> as a
///                 write-path fallback (N14). On the write path the simulator is
///                 NOT invoked, so the response deliberately does not carry
///                 telemetry — write operations are administrative.
///             </item>
///             <item>
///                 <see cref="ToResourceFromReadout"/> — read responses (GET) with
///                 the current simulated telemetry. All 10 fields are populated
///                 from the readmodel; <c>LastUpdate</c> is the simulator's
///                 <c>CapturedAt</c> serialized as ISO-8601 (N10).
///             </item>
///         </list>
///     </para>
/// </remarks>
public static class IoTDeviceResourceFromEntityAssembler
{
    /// <summary>
    ///     Converts a device aggregate (without telemetry) to its REST resource,
    ///     used for write responses where readings are not yet relevant.
    /// </summary>
    /// <param name="device">The domain aggregate.</param>
    /// <returns>The REST resource with null telemetry and a <c>LastUpdate</c> fallback.</returns>
    public static IoTDeviceResource ToResourceFromEntity(this IoTDevice device, IClock clock) =>
        new(
            (int)device.Id,
            (int)device.PlotId,
            device.DeviceName,
            device.Status,
            Health: null,
            DeviceType: device.ActivationCode?.DeviceType().ToString(),
            SoilMoisture: null,
            Temperature: null,
            LeafHumidity: null,
            LastUpdate: clock.UtcNow.ToString("O"));

    /// <summary>
    ///     Converts a device readout (device + current telemetry) to its REST resource.
    /// </summary>
    /// <param name="readout">The device with its simulated readings.</param>
    /// <returns>The REST resource including telemetry, with <c>LastUpdate</c> as the simulator's <c>CapturedAt</c> ISO-8601 string.</returns>
    public static IoTDeviceResource ToResourceFromReadout(this IoTDeviceReadout readout)
    {
        var device = readout.Device;
        var readings = readout.Readings;

        return new IoTDeviceResource(
            (int)device.Id,
            (int)device.PlotId,
            device.DeviceName,
            device.Status,
            Health: readout.Health.ToString(),
            DeviceType: device.ActivationCode?.DeviceType().ToString(),
            SoilMoisture: readings?.SoilMoisture,
            Temperature: readings?.SoilTemperature,
            LeafHumidity: readings?.LeafHumidity,
            LastUpdate: readings?.CapturedAt.ToString("O"));
    }
}
