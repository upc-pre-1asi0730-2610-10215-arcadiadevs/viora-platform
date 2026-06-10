using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Value object that wraps the unique identifier of an IoT device.
    /// </summary>
    public readonly record struct IoTDeviceId
    {
        public long Value { get; }
        
        public IoTDeviceId(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentException("IoTDeviceId must be a positive value", nameof(value));
            }

            Value = value;
        }
    }
}