using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

public partial class PestSightingReport
{
    public PestSightingReport()
    {
        RiskZone = ERiskZone.FULL_PLOT;
        Symptoms = new Symptoms(new List<Symptom>());
        ObservedSeverity = EAlertSeverity.LOW;
        Notes = string.Empty;
        CalculatedRisk = EAlertSeverity.LOW;
        ProbableThreat = EThreatType.UNKNOWN;
        Status = EReportStatus.UNDER_REVIEW;
    }

    public PestSightingReport(CreatePestSightingReportCommand command) : this()
    {
        PlotId = new PlotId(command.PlotId);
        ReporterUserId = new ReporterUserId(command.ReporterUserId);
        RiskZone = Enum.Parse<ERiskZone>(command.RiskZone, true);
        Symptoms = Symptoms.FromDescriptions(command.Symptoms);
        ObservedSeverity = Enum.Parse<EAlertSeverity>(command.ObservedSeverity, true);
        Notes = command.Notes ?? string.Empty;
        Evaluated = false;
        Status = EReportStatus.UNDER_REVIEW;
        AlertConfirmed = false;
        CalculatedRisk = EAlertSeverity.LOW;
        ProbableThreat = EThreatType.UNKNOWN;
    }

    public long Id { get; }
    public PlotId PlotId { get; private set; }
    public ReporterUserId ReporterUserId { get; private set; }
    public ERiskZone RiskZone { get; private set; }
    public Symptoms Symptoms { get; private set; }
    public EAlertSeverity ObservedSeverity { get; private set; }
    public string Notes { get; private set; }
}
