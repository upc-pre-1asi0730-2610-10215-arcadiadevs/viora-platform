namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Composed producer-facing overview resource (REQ-OV-1) — one
///     <c>InterventionRequest</c> plus its downstream chain's key
///     identifying fields and statuses, following only existing links.
///     Downstream fields are null when that stage of the chain has not been
///     reached yet. <c>status</c> is the REQ-OV-2 derived status.
/// </summary>
public record InterventionOverviewResource(
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
