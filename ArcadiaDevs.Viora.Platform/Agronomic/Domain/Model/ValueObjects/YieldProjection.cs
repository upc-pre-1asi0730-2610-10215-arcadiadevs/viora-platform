using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Yield projection value object.
    /// </summary>
    public readonly record struct YieldProjection
    {
        public decimal Value { get; }

        public YieldProjection(decimal value)
        {
            if (value < 0)
            {
                throw new ArgumentException("YieldProjection cannot be negative.", nameof(value));
            }

            Value = value;
        }
    }
}
