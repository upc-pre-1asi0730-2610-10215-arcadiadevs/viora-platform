namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the (at most one) <see cref="Aggregates.InterventionExecution" />
///     linked to a given <c>TreatmentPrescriptionId</c> (REQ-IE-2 idempotency
///     lookup, reused as a read endpoint — mirrors <c>TreatmentPrescription</c>'s
///     find-by-parent-id read shape).
/// </summary>
public record GetInterventionExecutionByTreatmentPrescriptionIdQuery(int TreatmentPrescriptionId);
