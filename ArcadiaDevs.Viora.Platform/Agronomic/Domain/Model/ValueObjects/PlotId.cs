using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Plot identifier value object.
    /// <para>
    /// Represents the unique identifier of a plot in the agronomic bounded context.
    /// </para>
    /// </summary>
    public readonly record struct PlotId
    {
        /// <summary>
        /// The raw numeric identifier.
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// Creates a plot identifier.
        /// </summary>
        /// <param name="value">The plot identifier.</param>
        /// <exception cref="ArgumentException">Thrown when value is less than or equal to zero.</exception>
        public PlotId(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentException("Plot ID must be a positive number.", nameof(value));
            }

            Value = value;
        }
    }
}