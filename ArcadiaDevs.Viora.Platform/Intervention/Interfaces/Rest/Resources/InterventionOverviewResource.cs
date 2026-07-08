namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

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
