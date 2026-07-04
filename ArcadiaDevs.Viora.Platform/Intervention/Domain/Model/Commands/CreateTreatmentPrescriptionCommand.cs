namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to create a <see cref="Aggregates.TreatmentPrescription" />
///     for an accepted <see cref="Aggregates.ServiceProposal" /> (REQ-TP-1).
///     <c>ServiceProposalId</c> is validated to exist (REQ-CC-2: missing FK
///     maps to 404), but per OS parity creation is NOT blocked by the
///     proposal's status (REQ-TP-1 — intentional inherited absence).
///     Fails with a conflict (409) if a prescription already exists for the
///     given proposal (REQ-TP-4 idempotency).
/// </summary>
public record CreateTreatmentPrescriptionCommand(int ServiceProposalId);
