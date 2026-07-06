using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Assembler that transforms an UpdateIoTDeviceResource into an UpdateIoTDeviceCommand.
/// </summary>
public static class UpdateIoTDeviceCommandFromResourceAssembler
{
    /// <summary>
    ///     Converts a REST resource plus path variables into a command.
    /// </summary>
    /// <param name="resource">The PATCH request body.</param>
    /// <param name="plotId">The plot identifier from the path.</param>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="deviceId">The device identifier from the path.</param>
    /// <returns>The assembled command.</returns>
    public static UpdateIoTDeviceCommand ToCommandFromResource(
        UpdateIoTDeviceResource resource,
        long plotId,
        int userId,
        long deviceId)
    {
        return new UpdateIoTDeviceCommand(
            (int)plotId,
            userId,
            (int)deviceId,
            resource.DeviceName,
            (IoTDeviceStatus)Enum.Parse(typeof(IoTDeviceStatus), resource.IotDeviceStatus, true)
        );
    }
}