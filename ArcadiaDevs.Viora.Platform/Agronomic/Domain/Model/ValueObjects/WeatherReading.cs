using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     A single weather reading at a point in time.
/// </summary>
public record WeatherReading
{
    public DateTimeOffset Timestamp { get; init; }
    public double TemperatureCelsius { get; init; }
    public WeatherStatus WeatherStatus { get; init; }
    public int? HumidityPercentage { get; init; }
    public double? PrecipitationMillimeters { get; init; }
    public double? MinTemperatureCelsius { get; init; }
    public double? MaxTemperatureCelsius { get; init; }
    public double? WindSpeedMetersPerSecond { get; init; }
    public double? WindGustMetersPerSecond { get; init; }

    public WeatherReading(
        DateTimeOffset timestamp,
        double temperatureCelsius,
        WeatherStatus weatherStatus,
        int? humidityPercentage = null,
        double? precipitationMillimeters = null,
        double? minTemperatureCelsius = null,
        double? maxTemperatureCelsius = null,
        double? windSpeedMetersPerSecond = null,
        double? windGustMetersPerSecond = null)
    {
        Timestamp = timestamp;
        TemperatureCelsius = temperatureCelsius;
        WeatherStatus = weatherStatus;
        HumidityPercentage = humidityPercentage;
        PrecipitationMillimeters = precipitationMillimeters;
        MinTemperatureCelsius = minTemperatureCelsius;
        MaxTemperatureCelsius = maxTemperatureCelsius;
        WindSpeedMetersPerSecond = windSpeedMetersPerSecond;
        WindGustMetersPerSecond = windGustMetersPerSecond;
    }
}
