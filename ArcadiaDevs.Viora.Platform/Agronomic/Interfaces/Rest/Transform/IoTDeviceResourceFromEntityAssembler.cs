using ArcadiaDevs.Viora.Platform.Agronomic.Application.ReadModels;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

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
///             <item><see cref="ToResourceFromEntity"/> — write responses (POST / PATCH / DELETE)
///             where telemetry is not relevant; the 5 base fields + <c>CreatedAt</c>.</item>
///             <item><see cref="ToResourceFromReadout"/> — read responses (GET) with the
///             current simulated telemetry. In 1.17.0-6 the resource still has the
///             5-field shape (CreatedAt = UtcNow placeholder); 1.17.0-9 (D15
///             BREAKING) rewrites the resource to 10 fields and this method to
///             populate them.</item>
///         </list>
///     </para>
/// </remarks>
public static class IoTDeviceResourceFromEntityAssembler
{
    public static IoTDeviceResource ToResourceFromEntity(this IoTDevice device) =>
        new(
            (int)device.Id,
            (int)device.PlotId,
            device.DeviceName,
            device.Status,
            DateTime.UtcNow);

    /// <summary>
    ///     Converts a device readout (device + current telemetry) to its REST resource.
    ///     <para>
    ///         Added in T1.17.0-6 so the controller's <c>readouts.Select(r =&gt; r.ToResourceFromReadout())</c>
    ///         call compiles. In 1.17.0-6 the resource still has the 5-field shape
    ///         (the 6 new fields are added in 1.17.0-9, D15 BREAKING). For now
    ///         the telemetry is dropped (the readmodel's readouts are intentionally
    ///         not serialized — the endpoint returns the same 5-field JSON as before
    ///         1.17.0, but resolved from a readmodel rather than a raw aggregate,
    ///         so the 1.17.0-6 fix closes R1 (DI failure) without breaking clients).
    ///     </para>
    /// </summary>
    public static IoTDeviceResource ToResourceFromReadout(this IoTDeviceReadout readout) =>
        new(
            (int)readout.Device.Id,
            (int)readout.Device.PlotId,
            readout.Device.DeviceName,
            readout.Device.Status,
            DateTime.UtcNow);
}