using System.ComponentModel.DataAnnotations;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     REST request body for creating a new IoT device.
///     (TS13-004) Used by POST /api/v1/plots/{plotId}/iot-devices.
/// </summary>
/// <param name="DeviceName">The human-readable name of the device (required).</param>
/// <param name="Status">The desired status; Active by default when omitted.</param>
public record CreateIoTDeviceResource(
    [property: Required(ErrorMessage = "deviceName is required")]
    [property: StringLength(150, ErrorMessage = "deviceName must not exceed 150 characters")]
    string DeviceName,

    IoTDeviceStatus? Status
);