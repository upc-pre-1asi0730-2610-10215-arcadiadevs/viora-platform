namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The outcome status of an <see cref="Aggregates.InterventionExecution" />'s
///     field application (REQ-IE-3). Set once at certification time —
///     unlike <c>TreatmentPrescriptionStatus</c>, this is not a self-guarded
///     transition sequence; the caller supplies the observed outcome
///     directly when certifying the execution.
/// </summary>
public enum InterventionExecutionStatus
{
    APPLIED_AS_PRESCRIBED,
    PARTIALLY_APPLIED,
    NOT_APPLIED
}
