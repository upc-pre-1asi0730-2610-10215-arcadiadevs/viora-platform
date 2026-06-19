using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public static class EClimateRiskLevelExtensions
{
    public static EClimateRiskLevel FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ClimateRiskLevel is required");

        if (Enum.TryParse<EClimateRiskLevel>(value.Trim(), true, out var result))
        {
            return result;
        }

        throw new ArgumentException($"Invalid ClimateRiskLevel '{value}'. Allowed: LOW, MODERATE, HIGH, EXTREME, UNKNOWN");
    }
}
