using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Accumulated chill hours value object.
    /// </summary>
    public readonly record struct AccumulatedChillHours
    {
        public decimal Value { get; }

        public AccumulatedChillHours(decimal value)
        {
            if (value < 0)
            {
                throw new ArgumentException("AccumulatedChillHours cannot be negative.", nameof(value));
            }

            Value = value;
        }
    }
}
