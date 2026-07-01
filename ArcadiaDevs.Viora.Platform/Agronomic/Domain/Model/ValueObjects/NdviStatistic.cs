using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Represents a single NDVI statistic measurement with optional statistical measures.
    /// </summary>
    /// <remarks>
    /// <para>All NDVI values must be in the range [-1, 1].</para>
    /// <para>The <see cref="Mean"/> is required; all other statistical measures are optional.</para>
    /// </remarks>
    public sealed record NdviStatistic
    {
        /// <summary>
        /// Gets the timestamp of the measurement.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the mean NDVI value.
        /// </summary>
        public double Mean { get; }

        /// <summary>
        /// Gets the minimum NDVI value, if available.
        /// </summary>
        public double? Minimum { get; }

        /// <summary>
        /// Gets the maximum NDVI value, if available.
        /// </summary>
        public double? Maximum { get; }

        /// <summary>
        /// Gets the median NDVI value, if available.
        /// </summary>
        public double? Median { get; }

        /// <summary>
        /// Gets the standard deviation of NDVI values, if available.
        /// </summary>
        public double? StandardDeviation { get; }

        /// <summary>
        /// Gets the 25th percentile NDVI value, if available.
        /// </summary>
        public double? Percentile25 { get; }

        /// <summary>
        /// Gets the 75th percentile NDVI value, if available.
        /// </summary>
        public double? Percentile75 { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NdviStatistic"/> record.
        /// </summary>
        /// <param name="timestamp">The timestamp of the measurement.</param>
        /// <param name="mean">The mean NDVI value.</param>
        /// <param name="minimum">The minimum NDVI value, if available.</param>
        /// <param name="maximum">The maximum NDVI value, if available.</param>
        /// <param name="median">The median NDVI value, if available.</param>
        /// <param name="standardDeviation">The standard deviation, if available.</param>
        /// <param name="percentile25">The 25th percentile, if available.</param>
        /// <param name="percentile75">The 75th percentile, if available.</param>
        public NdviStatistic(
            DateTimeOffset timestamp,
            double mean,
            double? minimum = null,
            double? maximum = null,
            double? median = null,
            double? standardDeviation = null,
            double? percentile25 = null,
            double? percentile75 = null)
        {
            Timestamp = timestamp;
            Mean = EnsureInRange(mean, nameof(mean));

            if (minimum.HasValue) Minimum = EnsureInRange(minimum.Value, nameof(minimum));
            if (maximum.HasValue) Maximum = EnsureInRange(maximum.Value, nameof(maximum));
            if (median.HasValue) Median = EnsureInRange(median.Value, nameof(median));
            if (percentile25.HasValue) Percentile25 = EnsureInRange(percentile25.Value, nameof(percentile25));
            if (percentile75.HasValue) Percentile75 = EnsureInRange(percentile75.Value, nameof(percentile75));

            if (standardDeviation.HasValue)
            {
                if (double.IsNaN(standardDeviation.Value) || double.IsInfinity(standardDeviation.Value))
                {
                    throw new ArgumentOutOfRangeException(nameof(standardDeviation),
                        "Standard deviation must be a finite number.");
                }

                if (standardDeviation.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(standardDeviation),
                        "Standard deviation must be non-negative.");
                }

                StandardDeviation = standardDeviation.Value;
            }
        }

        private static double EnsureInRange(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(paramName,
                    $"NDVI value must be a finite number in the range [-1, 1].");
            }

            if (value < -1.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(paramName,
                    $"NDVI value must be in the range [-1, 1]. Got {value}.");
            }

            return value;
        }
    }
}