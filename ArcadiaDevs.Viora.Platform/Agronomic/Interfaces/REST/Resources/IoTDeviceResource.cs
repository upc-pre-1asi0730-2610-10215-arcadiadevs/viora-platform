using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Response DTO for an IoT device.
/// </summary>
/// <remarks>
///     (TS012TASK005) Returned by <c>GET /api/v1/plots/{plotId}/iot-devices</c>.
/// </remarks>
/// <param name="Id">The unique device identifier.</param>
/// <param name="PlotId">The identifier of the associated plot.</param>
/// <param name="DeviceName">The human-readable name of the device.</param>
/// <param name="Status">The current operational status of the device.</param>
/// <param name="CreatedAt">The UTC timestamp when the device was registered.</param>
public record IoTDeviceResource(
    int Id,
    int PlotId,
    string DeviceName,
    IoTDeviceStatus Status,
    DateTimeOffset CreatedAt);