using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

public partial class PestSightingReport
{
    public bool Evaluated { get; private set; }
    public EAlertSeverity CalculatedRisk { get; private set; }
    public EThreatType ProbableThreat { get; private set; }
    public EReportStatus Status { get; private set; }
    public bool AlertConfirmed { get; private set; }

    public void EvaluateBiologicalRisk(double? currentNdvi, EThreatType inferredThreat)
    {
        bool ndviConfirmsDamage = currentNdvi != null && currentNdvi < 0.40;

        if (ObservedSeverity == EAlertSeverity.CRITICAL || (ObservedSeverity == EAlertSeverity.HIGH && ndviConfirmsDamage))
        {
            AlertConfirmed = true;
            Status = EReportStatus.CONFIRMED;
            CalculatedRisk = EAlertSeverity.HIGH;
            ProbableThreat = inferredThreat;
        }
        else
        {
            AlertConfirmed = false;
            Status = EReportStatus.UNDER_REVIEW;
            CalculatedRisk = EAlertSeverity.LOW;
            ProbableThreat = inferredThreat;
        }

        Evaluated = true;
    }
}
