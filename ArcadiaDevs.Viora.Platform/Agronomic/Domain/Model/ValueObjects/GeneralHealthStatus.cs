using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Allowed health states for a monitoring summary.
    /// </summary>
    public enum GeneralHealthStatus
    {
        Good,
        Moderate,
        Critical
    }

    /// <summary>
    /// Extension and helper methods for GeneralHealthStatus.
    /// </summary>
    public static class GeneralHealthStatusExtensions
    {
        public static GeneralHealthStatus FromString(string value)
        {
            if (Enum.TryParse<GeneralHealthStatus>(value, true, out var status))
            {
                return status;
            }

            throw new ArgumentException(
                $"Invalid GeneralHealthStatus '{value}'. Allowed: GOOD, MODERATE, CRITICAL");
        }
    }
}
