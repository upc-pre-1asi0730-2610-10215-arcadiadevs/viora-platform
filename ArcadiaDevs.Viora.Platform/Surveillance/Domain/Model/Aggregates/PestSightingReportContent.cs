using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

public partial class PestSightingReport
{
    public bool Evaluated { get; private set; }
    public EAlertSeverity CalculatedRisk { get; private set; }
    public EThreatType ProbableThreat { get; private set; }
    public EReportStatus Status { get; private set; }
    public bool AlertConfirmed { get; private set; }

    /// <summary>
    ///     Triages the pest sighting report into one of three statuses based on the
    ///     observed severity and the NDVI corroboration signal. Mirrors the OS's
    ///     <c>evaluateBiologicalRisk</c> at
    ///     <c>PestSightingReport.java:122-161</c> (minus the quarantine 4th case
    ///     which is deferred to a future release).
    /// </summary>
    /// <param name="currentNdvi">The current NDVI value for the plot (may be null if unavailable).</param>
    /// <param name="inferredThreat">The threat type inferred from the reported symptoms.</param>
    public void EvaluateBiologicalRisk(double? currentNdvi, EThreatType inferredThreat)
    {
        bool ndviConfirmsDamage = currentNdvi != null && currentNdvi < 0.40;

        if (ObservedSeverity == EAlertSeverity.CRITICAL
            || (ObservedSeverity == EAlertSeverity.HIGH && ndviConfirmsDamage))
        {
            // CONFIRMED: subjective and objective signals agree (or self-reported as critical).
            AlertConfirmed = true;
            Status = EReportStatus.CONFIRMED;
            CalculatedRisk = EAlertSeverity.HIGH;
            ProbableThreat = inferredThreat;
        }
        else if (ObservedSeverity == EAlertSeverity.HIGH
                 || (ObservedSeverity == EAlertSeverity.MEDIUM && ndviConfirmsDamage))
        {
            // NEEDS_INSPECTION: a real signal exists but is not yet corroborated.
            AlertConfirmed = false;
            Status = EReportStatus.NEEDS_INSPECTION;
            CalculatedRisk = EAlertSeverity.MEDIUM;
            ProbableThreat = inferredThreat;
        }
        else
        {
            // LOGGED: weak signal — recorded for community epidemiology, no alert raised.
            AlertConfirmed = false;
            Status = EReportStatus.LOGGED;
            CalculatedRisk = EAlertSeverity.LOW;
            ProbableThreat = inferredThreat;
        }

        Evaluated = true;
    }
}
