using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Historical weather observations for a plot location over a requested range.
/// </summary>
public record WeatherHistory
{
    public IReadOnlyList<WeatherReading> Readings { get; init; }

    public WeatherHistory(IEnumerable<WeatherReading> readings)
    {
        var readingList = readings?.ToList();
        if (readingList == null || readingList.Count == 0)
        {
            throw new ArgumentException("Weather history must contain at least one reading.");
        }

        Readings = readingList.OrderBy(r => r.Timestamp).ToList();
    }
}
