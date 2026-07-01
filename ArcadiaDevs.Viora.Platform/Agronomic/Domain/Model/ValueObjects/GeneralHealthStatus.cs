using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Allowed health states for a monitoring summary.
    /// </summary>
    /// <remarks>
    /// <para>Values:</para>
    /// <para><see cref="Healthy"/> — the plot is in good health (NDVI ≥ 0.60).</para>
    /// <para><see cref="Critical"/> — the plot is in critical condition (NDVI &lt; 0.30).</para>
    /// <para><see cref="Warning"/> — the plot shows warning signs (0.30 ≤ NDVI &lt; 0.60).</para>
    /// <para><see cref="Unknown"/> — health cannot be determined (no data available).</para>
    /// </remarks>
    public enum GeneralHealthStatus
    {
        Healthy,
        Critical,
        Warning,
        Unknown
    }

    /// <summary>
    /// Extension and helper methods for <see cref="GeneralHealthStatus"/>.
    /// </summary>
    public static class GeneralHealthStatusExtensions
    {
        /// <summary>
        /// Parses a string value into a <see cref="GeneralHealthStatus"/>.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <returns>The corresponding <see cref="GeneralHealthStatus"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value"/> does not match any allowed status.
        /// Legacy value "Good" is mapped to <see cref="GeneralHealthStatus.Healthy"/> (N4 backward-compat).
        /// </exception>
        public static GeneralHealthStatus FromString(string value)
        {
            if (string.Equals(value, "Good", StringComparison.OrdinalIgnoreCase))
            {
                return GeneralHealthStatus.Healthy; // N4 backward-compat alias
            }

            if (Enum.TryParse<GeneralHealthStatus>(value, true, out var status))
            {
                return status;
            }

            throw new ArgumentException(
                $"Invalid GeneralHealthStatus '{value}'. Allowed: HEALTHY, WARNING, CRITICAL, UNKNOWN");
        }
    }
}
