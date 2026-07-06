using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

/// <summary>
///     Command to update an IoT device's metadata scoped to a plot.
/// </summary>
public record UpdateIoTDeviceCommand
{
    public int PlotId { get; init; }
    public int UserId { get; init; }
    public int DeviceId { get; init; }
    public string DeviceName { get; init; }
    public IoTDeviceStatus Status { get; init; }

    public UpdateIoTDeviceCommand(int plotId, int userId, int deviceId, string deviceName, IoTDeviceStatus? status)
    {
        if (plotId <= 0)
            throw new ArgumentException("UpdateIoTDeviceCommand requires a valid plotId", nameof(plotId));

        if (deviceId <= 0)
            throw new ArgumentException("UpdateIoTDeviceCommand requires a valid deviceId", nameof(deviceId));

        if (string.IsNullOrWhiteSpace(deviceName))
            throw new ArgumentException("UpdateIoTDeviceCommand requires a non-blank deviceName", nameof(deviceName));

        if (status == null)
            throw new ArgumentException("UpdateIoTDeviceCommand requires a valid status", nameof(status));

        PlotId = plotId;
        UserId = userId;
        DeviceId = deviceId;
        DeviceName = deviceName;
        Status = status.Value;
    }
}