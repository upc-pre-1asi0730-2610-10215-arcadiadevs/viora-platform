using System.ComponentModel.DataAnnotations;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
/// Resource used to map the incoming HTTP payload to create a pest sighting report.
/// </summary>
/// <param name="PlotId">The identifier of the plot where the sighting occurred.</param>
/// <param name="ReporterUserId">The identifier of the user reporting the sighting.</param>
/// <param name="RiskZone">The risk zone associated with the report (e.g. FULL_PLOT, PARTIAL).</param>
/// <param name="Symptoms">A list of observed symptoms.</param>
/// <param name="ObservedSeverity">The observed severity of the threat (e.g. LOW, MEDIUM, HIGH, CRITICAL).</param>
/// <param name="Notes">Optional notes regarding the sighting.</param>
public record CreatePestSightingReportResource(
    [Range(1, long.MaxValue)] long PlotId,
    [Range(1, long.MaxValue)] long ReporterUserId,
    [Required] string RiskZone,
    [Required, MinLength(1)] List<string> Symptoms,
    [Required] string ObservedSeverity,
    string? Notes
);
