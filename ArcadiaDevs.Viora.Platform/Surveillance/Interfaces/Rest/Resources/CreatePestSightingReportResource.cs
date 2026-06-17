namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
/// Resource used to map the incoming HTTP payload to create a pest sighting report.
/// </summary>
public record CreatePestSightingReportResource(
    long PlotId,
    long ReporterUserId,
    string RiskZone,
    List<string> Symptoms,
    string ObservedSeverity,
    string? Notes
);
