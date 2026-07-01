using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Represents a history of NDVI statistics for a plot, sorted by timestamp ascending.
    /// </summary>
    public sealed record NdviHistory
    {
        /// <summary>
        /// Gets the list of NDVI statistics, sorted by timestamp ascending.
        /// </summary>
        public IReadOnlyList<NdviStatistic> Statistics { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NdviHistory"/> record.
        /// </summary>
        /// <param name="statistics">The list of NDVI statistics (must be non-empty).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="statistics"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="statistics"/> is empty.</exception>
        public NdviHistory(IReadOnlyList<NdviStatistic> statistics)
        {
            ArgumentNullException.ThrowIfNull(statistics);

            if (statistics.Count == 0)
            {
                throw new ArgumentException("NdviHistory must contain at least one statistic.", nameof(statistics));
            }

            Statistics = statistics.OrderBy(s => s.Timestamp).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the most recent NDVI statistic (the last in the sorted list).
        /// </summary>
        /// <returns>The most recent <see cref="NdviStatistic"/>.</returns>
        public NdviStatistic Latest() => Statistics[^1];
    }
}