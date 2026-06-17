namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;

/// <summary>
/// Command to create a new pest sighting report.
/// </summary>
/// <param name="PlotId">The ID of the plot where the sighting occurred.</param>
/// <param name="ReporterUserId">The ID of the user reporting the sighting.</param>
/// <param name="RiskZone">The zone of the plot affected (e.g., FULL_PLOT).</param>
/// <param name="Symptoms">A list of symptom descriptions observed.</param>
/// <param name="ObservedSeverity">The observed severity level (e.g., LOW, MEDIUM, HIGH, CRITICAL).</param>
/// <param name="Notes">Optional additional notes regarding the sighting.</param>
public record CreatePestSightingReportCommand(
    long PlotId,
    long ReporterUserId,
    string RiskZone,
    List<string> Symptoms,
    string ObservedSeverity,
    string? Notes
);
