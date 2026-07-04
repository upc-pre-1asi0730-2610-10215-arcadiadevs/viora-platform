namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to certify an <see cref="Aggregates.InterventionExecution" />
///     against a <see cref="Aggregates.TreatmentPrescription" /> (REQ-IE-1).
///     Fails with a conflict (409) unless <c>TreatmentPrescriptionId</c>
///     resolves to a prescription whose status is <c>PRESCRIBED</c> — a
///     deliberate improvement over OS parity, enforced at the command
///     service level (design decision 3). Also fails 409 if an execution
///     already exists for the given prescription (REQ-IE-2 idempotency).
/// </summary>
public record CertifyInterventionExecutionCommand(
    int TreatmentPrescriptionId,
    DateOnly ApplicationDate,
    decimal AppliedArea,
    string ExecutionStatus,
    string? FieldNote);
