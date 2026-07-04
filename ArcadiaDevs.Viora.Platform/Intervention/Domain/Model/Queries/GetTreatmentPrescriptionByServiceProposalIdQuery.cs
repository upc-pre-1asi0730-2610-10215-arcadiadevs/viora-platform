namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the (at most one) <see cref="Aggregates.TreatmentPrescription" />
///     linked to a given <c>ServiceProposalId</c> (REQ-TP-4 idempotency
///     lookup, reused as a read endpoint — mirrors <c>ServiceProposal</c>'s
///     list-by-parent-id read shape).
/// </summary>
public record GetTreatmentPrescriptionByServiceProposalIdQuery(int ServiceProposalId);
