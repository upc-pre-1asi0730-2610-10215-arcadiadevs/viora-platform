using System.ComponentModel.DataAnnotations;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record CreateIoTDeviceResource(
    [Required(ErrorMessage = "deviceName is required")]
    [StringLength(150, ErrorMessage = "deviceName must not exceed 150 characters")]
    string DeviceName,

    IoTDeviceStatus? Status,

    [Required(ErrorMessage = "activationCode is required")]
    [StringLength(20, ErrorMessage = "activationCode must not exceed 20 characters")]
    string ActivationCode
);