using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Weather forecast containing hourly readings.
/// </summary>
public record WeatherForecast
{
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyList<WeatherReading> Readings { get; init; }

    public WeatherForecast(DateTimeOffset generatedAt, IReadOnlyList<WeatherReading> readings)
    {
        if (readings is null || readings.Count == 0)
            throw new ArgumentException("Readings must not be null or empty.", nameof(readings));

        GeneratedAt = generatedAt;
        Readings = readings;
    }
}
