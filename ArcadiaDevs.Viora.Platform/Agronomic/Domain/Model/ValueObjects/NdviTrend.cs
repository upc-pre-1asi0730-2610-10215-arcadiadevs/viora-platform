using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Represents the NDVI trend analysis result for a plot.
    /// </summary>
    public sealed record NdviTrend
    {
        /// <summary>
        /// Gets the direction of the trend.
        /// </summary>
        public NdviTrendDirection Direction { get; }

        /// <summary>
        /// Gets the rate of change (latest mean minus earliest mean).
        /// </summary>
        public double ChangeRate { get; }

        /// <summary>
        /// Gets the series of statistics used for the analysis.
        /// </summary>
        public IReadOnlyList<NdviStatistic> Series { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NdviTrend"/> record.
        /// </summary>
        /// <param name="direction">The direction of the trend.</param>
        /// <param name="changeRate">The rate of change.</param>
        /// <param name="series">The series of statistics (must be non-empty).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="direction"/> or <paramref name="series"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="series"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="changeRate"/> is not finite.</exception>
        public NdviTrend(NdviTrendDirection direction, double changeRate, IReadOnlyList<NdviStatistic> series)
        {
            ArgumentNullException.ThrowIfNull(direction);
            ArgumentNullException.ThrowIfNull(series);

            if (series.Count == 0)
            {
                throw new ArgumentException("NdviTrend series must contain at least one statistic.", nameof(series));
            }

            if (double.IsNaN(changeRate) || double.IsInfinity(changeRate))
            {
                throw new ArgumentOutOfRangeException(nameof(changeRate),
                    "ChangeRate must be a finite number.");
            }

            Direction = direction;
            ChangeRate = changeRate;
            Series = series.ToList().AsReadOnly();
        }
    }
}