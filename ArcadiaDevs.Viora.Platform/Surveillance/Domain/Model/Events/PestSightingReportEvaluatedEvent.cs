namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;

public record PestSightingReportEvaluatedEvent(
    long ReportId,
    long PlotId,
    long ReporterUserId,
    string CalculatedRisk,
    string ProbableThreat,
    bool AlertConfirmed
);
