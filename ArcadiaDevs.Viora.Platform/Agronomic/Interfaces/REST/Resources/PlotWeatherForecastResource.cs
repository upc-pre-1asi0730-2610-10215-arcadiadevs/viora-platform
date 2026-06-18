using System;
using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record HourlyForecastResource(
    DateTimeOffset Timestamp,
    double TemperatureCelsius,
    string WeatherStatus,
    int HumidityPercentage,
    double PrecipitationMillimeters,
    double WindSpeedMetersPerSecond,
    double WindGustMetersPerSecond
);

public record DailyForecastResource(
    string Date,
    double MinTemperatureCelsius,
    double MaxTemperatureCelsius,
    double AverageTemperatureCelsius,
    string DominantStatus,
    int AverageHumidityPercentage,
    double TotalPrecipitationMillimeters,
    double MaxWindGustMetersPerSecond
);

public record WeatherWarningResource(
    string Type,
    string Severity,
    string Date,
    string Message
);

public record PlotWeatherForecastResource(
    long PlotId,
    long UserId,
    string PlotName,
    DateTimeOffset GeneratedAt,
    IEnumerable<HourlyForecastResource> Hourly,
    IEnumerable<DailyForecastResource> Daily,
    double ThermalAnomalyCelsius,
    string OverallRisk,
    IEnumerable<WeatherWarningResource> Warnings,
    ExternalSourceResource Source
);
