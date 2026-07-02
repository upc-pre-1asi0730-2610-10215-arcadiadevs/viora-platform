using System;
using System.Collections.Generic;
using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.AdvisorValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Interface for the weather forecast advisor.
/// </summary>
public interface IWeatherForecastAdvisor
{
    WeatherForecastAnalysis Analyze(WeatherForecast forecast, double? baselineMeanTemperatureCelsius = null);
}

/// <summary>
///     Analyzes weather forecasts and generates warnings (OS parity).
/// </summary>
public class WeatherForecastAdvisor : IWeatherForecastAdvisor
{
    private const double FrostMinimumCelsius = 2.0;
    private const double HeatStressMaximumCelsius = 35.0;
    private const double HighWindGustMetersPerSecond = 17.0;
    private const double HeavyRainMillimeters = 20.0;

    public WeatherForecastAnalysis Analyze(WeatherForecast forecast, double? baselineMeanTemperatureCelsius = null)
    {
        if (forecast is null)
            throw new ArgumentNullException(nameof(forecast));

        // Group readings by UTC day
        var groupedByDay = forecast.Readings
            .GroupBy(r => r.Timestamp.Date)
            .OrderBy(g => g.Key);

        var dailyForecasts = new List<DailyWeather>();
        var allWarnings = new List<AgronomicWeatherWarning>();

        foreach (var dayGroup in groupedByDay)
        {
            var dayReadings = dayGroup.ToList();
            var date = DateOnly.FromDateTime(dayGroup.Key);

            var minTemp = dayReadings.Min(r => r.TemperatureCelsius);
            var maxTemp = dayReadings.Max(r => r.TemperatureCelsius);
            var avgTemp = dayReadings.Average(r => r.TemperatureCelsius);

            // Dominant status: most frequent WeatherStatus
            var dominantStatus = dayReadings
                .GroupBy(r => r.WeatherStatus)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            var avgHumidity = dayReadings
                .Where(r => r.HumidityPercentage.HasValue)
                .Select(r => r.HumidityPercentage!.Value)
                .DefaultIfEmpty(0)
                .Average();

            var totalPrecip = dayReadings
                .Where(r => r.PrecipitationMillimeters.HasValue)
                .Sum(r => r.PrecipitationMillimeters!.Value);

            var maxWindGust = dayReadings
                .Where(r => r.WindGustMetersPerSecond.HasValue)
                .Select(r => r.WindGustMetersPerSecond!.Value)
                .DefaultIfEmpty(0)
                .Max();

            var dailyWeather = new DailyWeather(
                date,
                minTemp,
                maxTemp,
                avgTemp,
                dominantStatus,
                (int?)avgHumidity,
                totalPrecip,
                maxWindGust > 0 ? maxWindGust : null);

            dailyForecasts.Add(dailyWeather);

            // Generate warnings for this day
            if (minTemp <= FrostMinimumCelsius)
            {
                allWarnings.Add(new AgronomicWeatherWarning(
                    WeatherWarningType.Frost,
                    AgronomicClimateRiskLevel.High,
                    date,
                    $"Minimum temperature {minTemp:F1}°C is at or below frost threshold ({FrostMinimumCelsius}°C)"));
            }

            if (maxTemp >= HeatStressMaximumCelsius)
            {
                allWarnings.Add(new AgronomicWeatherWarning(
                    WeatherWarningType.HeatStress,
                    AgronomicClimateRiskLevel.High,
                    date,
                    $"Maximum temperature {maxTemp:F1}°C exceeds heat stress threshold ({HeatStressMaximumCelsius}°C)"));
            }

            if (dominantStatus == WeatherStatus.Stormy)
            {
                allWarnings.Add(new AgronomicWeatherWarning(
                    WeatherWarningType.Storm,
                    AgronomicClimateRiskLevel.Extreme,
                    date,
                    "Stormy conditions detected in weather readings"));
            }

            if (maxWindGust >= HighWindGustMetersPerSecond)
            {
                allWarnings.Add(new AgronomicWeatherWarning(
                    WeatherWarningType.HighWind,
                    AgronomicClimateRiskLevel.High,
                    date,
                    $"Maximum wind gust {maxWindGust:F1} m/s exceeds high wind threshold ({HighWindGustMetersPerSecond} m/s)"));
            }

            if (totalPrecip >= HeavyRainMillimeters)
            {
                allWarnings.Add(new AgronomicWeatherWarning(
                    WeatherWarningType.HeavyRain,
                    AgronomicClimateRiskLevel.Moderate,
                    date,
                    $"Total precipitation {totalPrecip:F1} mm exceeds heavy rain threshold ({HeavyRainMillimeters} mm)"));
            }
        }

        // Compute thermal anomaly
        double? thermalAnomaly = null;
        if (baselineMeanTemperatureCelsius.HasValue && dailyForecasts.Count > 0)
        {
            var avgOfDailyAvgs = dailyForecasts.Average(d => d.AvgTemperatureCelsius);
            thermalAnomaly = avgOfDailyAvgs - baselineMeanTemperatureCelsius.Value;
        }
        else if (forecast.Readings.Count > 0)
        {
            thermalAnomaly = forecast.Readings.Average(r => r.TemperatureCelsius);
        }

        // Compute overall risk: worst warning severity
        var overallRisk = AgronomicClimateRiskLevel.Low;
        if (allWarnings.Count > 0)
        {
            var worstSeverity = allWarnings.Max(w => w.Severity);
            overallRisk = worstSeverity;
        }

        return new WeatherForecastAnalysis(
            dailyForecasts,
            thermalAnomaly,
            overallRisk,
            allWarnings);
    }
}
