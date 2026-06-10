using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Assembles an <see cref="IoTDeviceResource"/> from an <see cref="IoTDevice"/> aggregate.
/// </summary>
/// <remarks>
///     (TS012TASK005) Converts the domain aggregate into the REST response DTO without
///     exposing internal aggregate state directly.
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
}