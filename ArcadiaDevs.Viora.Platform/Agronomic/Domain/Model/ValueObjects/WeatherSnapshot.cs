using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public enum ClimateRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
///     Weather snapshot value object.
/// </summary>
public record WeatherSnapshot
{
    public decimal CurrentTemperature { get; init; }
    public WeatherStatus WeatherStatus { get; init; }
    public DateTimeOffset LastValidatedReadingAt { get; init; }
    public ClimateRiskLevel ClimateRiskLevel { get; init; }

    public WeatherSnapshot(
        decimal currentTemperature, 
        WeatherStatus weatherStatus, 
        DateTimeOffset lastValidatedReadingAt, 
        ClimateRiskLevel climateRiskLevel)
    {
        if (currentTemperature < -50 || currentTemperature > 60)
            throw new ArgumentException("Current temperature is out of valid range (-50 to 60).", nameof(currentTemperature));

        if (lastValidatedReadingAt > DateTimeOffset.UtcNow)
            throw new ArgumentException("Last validated reading cannot be in the future.", nameof(lastValidatedReadingAt));

        if (!Enum.IsDefined(typeof(WeatherStatus), weatherStatus))
            throw new ArgumentException("Invalid weather status.", nameof(weatherStatus));

        if (!Enum.IsDefined(typeof(ClimateRiskLevel), climateRiskLevel))
            throw new ArgumentException("Invalid climate risk level.", nameof(climateRiskLevel));

        CurrentTemperature = currentTemperature;
        WeatherStatus = weatherStatus;
        LastValidatedReadingAt = lastValidatedReadingAt;
        ClimateRiskLevel = climateRiskLevel;
    }
}
