using System;
using ArcadiaDevs.Viora.Platform.Shared.Domain;

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
        : this(currentTemperature, weatherStatus, lastValidatedReadingAt, climateRiskLevel, new Shared.Infrastructure.SystemClock())
    {
    }

    public WeatherSnapshot(
        decimal currentTemperature, 
        WeatherStatus weatherStatus, 
        DateTimeOffset lastValidatedReadingAt, 
        ClimateRiskLevel climateRiskLevel,
        IClock clock)
    {
        if (currentTemperature < -50 || currentTemperature > 60)
            throw new ArgumentException("Current temperature is out of valid range (-50 to 60).", nameof(currentTemperature));

        if (lastValidatedReadingAt > new DateTimeOffset(clock.UtcNow, TimeSpan.Zero))
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
