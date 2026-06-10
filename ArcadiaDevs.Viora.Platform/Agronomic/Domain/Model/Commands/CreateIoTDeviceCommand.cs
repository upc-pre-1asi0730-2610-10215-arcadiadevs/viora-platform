
using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands
{
    /// <summary>
    ///     Command to register a new IoT device associated with a plot.
    ///     (TS13-002) When status is not provided, ACTIVE is assigned by default.
    /// </summary>
    /// <param name="PlotId">The plot to associate the device with.</param>
    /// <param name="DeviceName">The human-readable name of the device.</param>
    /// <param name="Status">The desired status; defaults to Active if null.</param>
    public record CreateIoTDeviceCommand
    {
        public int PlotId { get; init; }
        public string DeviceName { get; init; }
        public IoTDeviceStatus Status { get; init; }

        // Constructor público con validaciones y valor por defecto para Status
        public CreateIoTDeviceCommand(int plotId, string deviceName, IoTDeviceStatus? status = null)
        {
            if (plotId <= 0)
                throw new ArgumentException("CreateIoTDeviceCommand requires a valid plotId", nameof(plotId));

            if (string.IsNullOrWhiteSpace(deviceName))
                throw new ArgumentException("CreateIoTDeviceCommand requires a non-blank deviceName", nameof(deviceName));

            PlotId = plotId;
            DeviceName = deviceName;
            Status = status ?? IoTDeviceStatus.Active;
        }
    }
}