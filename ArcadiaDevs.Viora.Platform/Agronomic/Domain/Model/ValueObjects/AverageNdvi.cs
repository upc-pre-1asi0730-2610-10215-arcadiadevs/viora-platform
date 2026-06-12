using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Average NDVI value object.
    /// </summary>
    public readonly record struct AverageNdvi
    {
        public decimal Value { get; }

        public AverageNdvi(decimal value)
        {
            if (value < 0)
            {
                throw new ArgumentException("AverageNdvi cannot be negative.", nameof(value));
            }

            Value = value;
        }
    }
}
