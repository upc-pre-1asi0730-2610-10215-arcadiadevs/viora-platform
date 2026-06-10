using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates
{
    /// <summary>
    /// IoTDevice aggregate root.
    /// Represents a sensor or IoT device associated with a plot.
    /// </summary>
    public class IoTDevice
    {
        public long Id { get; set; }
        public long PlotId { get; set; }
        public string DeviceName { get; set; }
        public IoTDeviceStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        protected IoTDevice()
        {
        }

        public IoTDevice(
            PlotId plotId,
            string deviceName,
            IoTDeviceStatus status)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
                throw new ArgumentException(
                    "IoTDevice requires a valid DeviceName",
                    nameof(deviceName));

            PlotId = plotId.Value;
            DeviceName = deviceName;
            Status = status;
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}