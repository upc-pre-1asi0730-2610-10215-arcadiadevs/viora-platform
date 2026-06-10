using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

/// <summary>
///     DeleteIoTDevice command.
/// </summary>
/// <remarks>
///     Represents the intention of deleting an IoT device from a plot.
/// </remarks>
public record DeleteIoTDeviceCommand
{
    public int PlotId { get; init; }
    public int DeviceId { get; init; }

    /// <summary>
    ///     Constructor with validation logic for DeleteIoTDeviceCommand.
    /// </summary>
    public DeleteIoTDeviceCommand(int plotId, int deviceId)
    {
        if (plotId <= 0)
        {
            throw new ArgumentException("Plot ID must be a positive number.", nameof(plotId));
        }
        if (deviceId <= 0)
        {
            throw new ArgumentException("Device ID must be a positive number.", nameof(deviceId));
        }

        PlotId = plotId;
        DeviceId = deviceId;
    }
}