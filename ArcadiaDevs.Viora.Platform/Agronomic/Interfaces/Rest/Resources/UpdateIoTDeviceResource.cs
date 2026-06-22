using System.ComponentModel.DataAnnotations;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     PATCH request body for updating an IoT device.
/// </summary>
/// <param name="DeviceName">The new device name.</param>
/// <param name="IotDeviceStatus">The new operational status.</param>
public record UpdateIoTDeviceResource(
    [Required(AllowEmptyStrings = false)] string DeviceName,
    [Required(AllowEmptyStrings = false)] string IotDeviceStatus
);