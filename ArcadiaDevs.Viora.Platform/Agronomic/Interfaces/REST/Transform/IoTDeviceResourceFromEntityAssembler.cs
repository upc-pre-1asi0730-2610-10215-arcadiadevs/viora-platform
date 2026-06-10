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
    /// <summary>
    ///     Maps an <see cref="IoTDevice"/> aggregate to its <see cref="IoTDeviceResource"/> representation.
    /// </summary>
    /// <param name="device">The domain aggregate to convert.</param>
    /// <returns>The REST response DTO.</returns>
    public static IoTDeviceResource ToResourceFromEntity(this IoTDevice device) =>
        new(
            (int)device.Id,
            (int)device.PlotId,
            device.DeviceName,
            device.Status,
            device.CreatedAt);
}