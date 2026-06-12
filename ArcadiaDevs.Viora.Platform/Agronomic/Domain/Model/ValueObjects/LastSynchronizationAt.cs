using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Last synchronization timestamp value object.
    /// </summary>
    public readonly record struct LastSynchronizationAt
    {
        public DateTimeOffset Value { get; }

        public LastSynchronizationAt(DateTimeOffset value)
        {
            if (value == default)
            {
                throw new ArgumentException("LastSynchronizationAt must be a valid date and time.", nameof(value));
            }

            Value = value;
        }
    }
}
