using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Allowed operational states for an IoT device.
    /// </summary>
    public enum IoTDeviceStatus
    {
        Active,
        Inactive,
        Maintenance
    }

    /// <summary>
    /// Extension and helper methods for IoTDeviceStatus.
    /// </summary>
    public static class IoTDeviceStatusExtensions
    {
        public static IoTDeviceStatus FromString(string value)
        {

            if (Enum.TryParse<IoTDeviceStatus>(value, true, out var status))
            {
                return status;
            }

            throw new ArgumentException(
                $"Invalid IoTDeviceStatus '{value}'. Allowed: ACTIVE, INACTIVE, MAINTENANCE");
        }
    }
}