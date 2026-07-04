namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;

/// <summary>
///     A single composed producer-facing overview row (REQ-OV-1) — one
///     <c>InterventionRequest</c> plus its downstream chain's key
///     identifying fields and statuses, following only existing links.
///     Downstream fields are null when that stage of the chain has not been
///     reached yet. <see cref="Status" /> is the REQ-OV-2 derived status,
///     computed at query time only — never persisted.
/// </summary>
public record InterventionOverviewItem(
    int InterventionRequestId,
    int GrowerId,
    long PlotId,
    int SpecialistId,
    string RequestStatus,
    int? ServiceProposalId,
    string? ProposalStatus,
    int? TreatmentPrescriptionId,
    string? PrescriptionStatus,
    int? InterventionExecutionId,
    string? ExecutionStatus,
    int? InterventionOutcomeId,
    string? OutcomeStatus,
    string Status);
