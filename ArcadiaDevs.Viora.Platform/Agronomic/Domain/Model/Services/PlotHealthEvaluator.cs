using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
/// Domain service that evaluates plot health status based on NDVI values.
/// </summary>
/// <remarks>
/// <para>
/// The evaluator classifies health status into four categories:
/// <list type="bullet">
///   <item><see cref="GeneralHealthStatus.Healthy"/> — NDVI ≥ 0.60 (or ≥ 0.45 for olive crops)</item>
///   <item><see cref="GeneralHealthStatus.Warning"/> — 0.30 ≤ NDVI &lt; 0.60 (or 0.30 ≤ NDVI &lt; 0.45 for olive crops)</item>
///   <item><see cref="GeneralHealthStatus.Critical"/> — NDVI &lt; 0.30</item>
///   <item><see cref="GeneralHealthStatus.Unknown"/> — no data available (null NDVI)</item>
/// </list>
/// </para>
/// <para>
/// Olive crop detection is based on the crop type string containing "oliv" (case-insensitive).
/// </para>
/// <para>
/// This is a pure function with no I/O dependencies. It is registered as a singleton in DI.
/// </para>
/// </remarks>
public class PlotHealthEvaluator
{
    /// <summary>
    /// NDVI threshold for critical health status.
    /// </summary>
    private const double CriticalNdviThreshold = 0.30;

    /// <summary>
    /// NDVI threshold for warning health status (generic crops).
    /// </summary>
    private const double WarningNdviThreshold = 0.60;

    /// <summary>
    /// NDVI threshold for warning health status (olive crops).
    /// </summary>
    private const double OliveWarningNdviThreshold = 0.45;

    /// <summary>
    /// Evaluates the health status based on the NDVI value.
    /// </summary>
    /// <param name="ndvi">The NDVI value, or null if no data is available.</param>
    /// <returns>
    /// The <see cref="GeneralHealthStatus"/> based on the NDVI value.
    /// </returns>
    public GeneralHealthStatus Evaluate(double? ndvi)
    {
        if (!ndvi.HasValue)
        {
            return GeneralHealthStatus.Unknown;
        }

        if (ndvi.Value < CriticalNdviThreshold)
        {
            return GeneralHealthStatus.Critical;
        }

        if (ndvi.Value < WarningNdviThreshold)
        {
            return GeneralHealthStatus.Warning;
        }

        return GeneralHealthStatus.Healthy;
    }

    /// <summary>
    /// Evaluates the health status based on the NDVI value and crop type.
    /// </summary>
    /// <param name="ndvi">The NDVI value, or null if no data is available.</param>
    /// <param name="cropType">The crop type string (used for olive crop detection).</param>
    /// <returns>
    /// The <see cref="GeneralHealthStatus"/> based on the NDVI value and crop type.
    /// </returns>
    public GeneralHealthStatus Evaluate(double? ndvi, string? cropType)
    {
        if (!ndvi.HasValue)
        {
            return GeneralHealthStatus.Unknown;
        }

        var warningThreshold = IsOlive(cropType) ? OliveWarningNdviThreshold : WarningNdviThreshold;

        if (ndvi.Value < CriticalNdviThreshold)
        {
            return GeneralHealthStatus.Critical;
        }

        if (ndvi.Value < warningThreshold)
        {
            return GeneralHealthStatus.Warning;
        }

        return GeneralHealthStatus.Healthy;
    }

    private static bool IsOlive(string? cropType) =>
        cropType?.Trim().ToLowerInvariant().Contains("oliv") ?? false;
}