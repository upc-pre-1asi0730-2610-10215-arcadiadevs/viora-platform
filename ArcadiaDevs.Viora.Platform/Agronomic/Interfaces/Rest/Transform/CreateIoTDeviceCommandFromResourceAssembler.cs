using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Static assembler that converts a <see cref="CreateIoTDeviceResource"/> REST request body
///     into a <see cref="CreateIoTDeviceCommand"/> application command.
///     (TS13-004)
/// </summary>
public static class CreateIoTDeviceCommandFromResourceAssembler
{
    /// <summary>
    ///     Converts a REST resource to the corresponding application command.
    /// </summary>
    /// <param name="resource">The incoming REST request body.</param>
    /// <param name="plotId">The plot identifier extracted from the URL path variable.</param>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <returns>The application command ready to be handled by the command service.</returns>
    public static CreateIoTDeviceCommand ToCommandFromResource(CreateIoTDeviceResource resource, int plotId, int userId)
    {
        IoTDeviceStatus effectiveStatus = resource.Status ?? IoTDeviceStatus.Active;

        return new CreateIoTDeviceCommand(
            plotId,
            userId,
            resource.DeviceName,
            effectiveStatus,
            resource.ActivationCode
        );
    }
}