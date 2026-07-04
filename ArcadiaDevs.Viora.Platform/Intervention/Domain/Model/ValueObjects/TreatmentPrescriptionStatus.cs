namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The lifecycle status of a <see cref="Aggregates.TreatmentPrescription" />
///     (REQ-TP-1..3). Self-guarded 3-stage sequence: <c>PENDING_INSPECTION</c>
///     → <c>INSPECTED</c> (via <see cref="Aggregates.TreatmentPrescription.LogFieldInspection" />)
///     → <c>PRESCRIBED</c> (via <see cref="Aggregates.TreatmentPrescription.PrescribeAgrochemical" />).
/// </summary>
public enum TreatmentPrescriptionStatus
{
    PENDING_INSPECTION,
    INSPECTED,
    PRESCRIBED
}
