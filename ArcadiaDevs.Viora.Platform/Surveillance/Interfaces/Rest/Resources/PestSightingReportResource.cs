namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
/// Resource used to represent a pest sighting report in HTTP responses.
/// </summary>
public record PestSightingReportResource(
    long Id,
    long PlotId,
    long ReporterUserId,
    string RiskZone,
    List<string> Symptoms,
    string ObservedSeverity,
    string Notes,
    bool Evaluated,
    string CalculatedRisk,
    string ProbableThreat,
    string Status,
    bool AlertConfirmed
);
