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
        public IoTDeviceStatus Status { get; set; }

        public IoTDevice(PlotId plotId, IoTDeviceStatus status)
        {
            if (plotId == null)
                throw new ArgumentNullException(nameof(plotId), "IoTDevice requires a valid PlotId");

            PlotId = plotId.Value;
            Status = status;
        }
    }
}