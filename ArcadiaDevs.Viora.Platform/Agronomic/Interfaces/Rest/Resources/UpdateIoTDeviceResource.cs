using System.ComponentModel.DataAnnotations;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record UpdateIoTDeviceResource(
    [Required(AllowEmptyStrings = false)] string DeviceName,
    [Required(AllowEmptyStrings = false)] string IotDeviceStatus
);