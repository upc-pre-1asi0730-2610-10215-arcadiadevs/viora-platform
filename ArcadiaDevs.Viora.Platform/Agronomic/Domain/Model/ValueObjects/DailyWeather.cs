using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Aggregated daily weather summary.
/// </summary>
public record DailyWeather
{
    public DateOnly Date { get; init; }
    public double MinTemperatureCelsius { get; init; }
    public double MaxTemperatureCelsius { get; init; }
    public double AvgTemperatureCelsius { get; init; }
    public WeatherStatus DominantStatus { get; init; }
    public int? AverageHumidityPercentage { get; init; }
    public double TotalPrecipitationMillimeters { get; init; }
    public double? MaxWindGustMetersPerSecond { get; init; }

    public DailyWeather(
        DateOnly date,
        double minTemperatureCelsius,
        double maxTemperatureCelsius,
        double avgTemperatureCelsius,
        WeatherStatus dominantStatus,
        int? averageHumidityPercentage,
        double totalPrecipitationMillimeters,
        double? maxWindGustMetersPerSecond)
    {
        if (minTemperatureCelsius > avgTemperatureCelsius)
            throw new ArgumentException("Min temperature must be <= avg temperature.", nameof(minTemperatureCelsius));
        if (avgTemperatureCelsius > maxTemperatureCelsius)
            throw new ArgumentException("Avg temperature must be <= max temperature.", nameof(avgTemperatureCelsius));

        Date = date;
        MinTemperatureCelsius = minTemperatureCelsius;
        MaxTemperatureCelsius = maxTemperatureCelsius;
        AvgTemperatureCelsius = avgTemperatureCelsius;
        DominantStatus = dominantStatus;
        AverageHumidityPercentage = averageHumidityPercentage;
        TotalPrecipitationMillimeters = totalPrecipitationMillimeters;
        MaxWindGustMetersPerSecond = maxWindGustMetersPerSecond;
    }
}
