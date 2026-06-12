using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Monitoring summary identifier value object.
    /// </summary>
    public readonly record struct MonitoringSummaryId
    {
        public long Value { get; }

        public MonitoringSummaryId(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentException("MonitoringSummaryId must be a positive number.", nameof(value));
            }

            Value = value;
        }
    }
}
