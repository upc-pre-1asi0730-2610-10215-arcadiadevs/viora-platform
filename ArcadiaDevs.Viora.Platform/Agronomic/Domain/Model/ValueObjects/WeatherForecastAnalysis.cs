using System;
using System.Collections.Generic;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.AdvisorValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Analysis of a weather forecast with daily summaries, warnings, and risk assessment.
/// </summary>
public record WeatherForecastAnalysis
{
    public IReadOnlyList<DailyWeather> DailyForecasts { get; init; }
    public double? ThermalAnomalyCelsius { get; init; }
    public AgronomicClimateRiskLevel OverallRisk { get; init; }
    public IReadOnlyList<AgronomicWeatherWarning> Warnings { get; init; }

    public WeatherForecastAnalysis(
        IReadOnlyList<DailyWeather> dailyForecasts,
        double? thermalAnomalyCelsius,
        AgronomicClimateRiskLevel overallRisk,
        IReadOnlyList<AgronomicWeatherWarning> warnings)
    {
        DailyForecasts = dailyForecasts ?? throw new ArgumentNullException(nameof(dailyForecasts));
        ThermalAnomalyCelsius = thermalAnomalyCelsius;
        OverallRisk = overallRisk;
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
    }
}
