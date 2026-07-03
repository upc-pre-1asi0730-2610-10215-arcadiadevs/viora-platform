using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Domain service that evaluates phenological risk level based on NDVI,
///     chill adequacy, and crop type.
/// </summary>
/// <remarks>
/// <para>
///     This evaluator replaces the hardcoded <c>"Low"</c> phenological risk
///     literal previously returned by <c>PlotQueryService.GetMyPlotsOverview</c>
///     and <c>GetPlotMonitoringSummaryQueryService</c>.
/// </para>
/// <para>
///     <strong>Classification logic</strong> (mirrors OS PhenologicalRiskEvaluator):
///     <list type="bullet">
///       <item><see cref="ClimateRiskLevel.Critical"/> — NDVI &lt; 0.30 and chill adequacy &lt; 40%</item>
///       <item><see cref="ClimateRiskLevel.High"/> — NDVI &lt; 0.45 or chill adequacy &lt; 60%</item>
///       <item><see cref="ClimateRiskLevel.Medium"/> — NDVI &lt; 0.60 or chill adequacy &lt; 80%</item>
///       <item><see cref="ClimateRiskLevel.Low"/> — otherwise (healthy range)</item>
///     </list>
/// </para>
/// <para>
///     This is a pure function with no I/O dependencies. Registered as a singleton in DI.
/// </para>
/// </remarks>
public class PhenologicalRiskEvaluator
{
    private const double CriticalNdviThreshold = 0.30;
    private const double HighNdviThreshold = 0.45;
    private const double MediumNdviThreshold = 0.60;

    private const double CriticalChillAdequacy = 0.40;
    private const double HighChillAdequacy = 0.60;
    private const double MediumChillAdequacy = 0.80;

    /// <summary>
    ///     Evaluates the phenological risk level for a plot.
    /// </summary>
    /// <param name="ndvi">Current NDVI value, or null if no data.</param>
    /// <param name="requirement">The plot's chill requirement.</param>
    /// <param name="accumulatedChill">Accumulated chill portions so far.</param>
    /// <param name="cropType">Crop type string (currently unused but reserved for crop-specific thresholds).</param>
    /// <returns>The <see cref="ClimateRiskLevel"/> classification.</returns>
    public ClimateRiskLevel Evaluate(
        double? ndvi,
        ChillRequirement requirement,
        decimal accumulatedChill,
        string? cropType)
    {
        var ndviValue = ndvi ?? 0.0;
        var requirementPortions = (double)requirement.Portions.Value;
        var chillAdequacy = requirementPortions > 0
            ? Math.Min(1.0, (double)accumulatedChill / requirementPortions)
            : 1.0;

        // Use the WORST of the two signals (NDVI + chill adequacy).
        if (ndviValue < CriticalNdviThreshold && chillAdequacy < CriticalChillAdequacy)
            return ClimateRiskLevel.Critical;

        if (ndviValue < HighNdviThreshold || chillAdequacy < HighChillAdequacy)
            return ClimateRiskLevel.High;

        if (ndviValue < MediumNdviThreshold || chillAdequacy < MediumChillAdequacy)
            return ClimateRiskLevel.Medium;

        return ClimateRiskLevel.Low;
    }
}
