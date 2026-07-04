using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Services;

/// <summary>
///     The downstream chain resolved for a single <see cref="InterventionRequest" />
///     (REQ-OV-1) — following only the links that actually exist — plus the
///     derived <see cref="Status" /> (REQ-OV-2). Produced by
///     <see cref="InterventionOverviewComposer" /> and shared by both WU8
///     query services (overview list, request metrics) so the FK-chain
///     traversal and status-derivation logic is written once.
/// </summary>
public record InterventionChainSnapshot(
    ServiceProposal? Proposal,
    TreatmentPrescription? Prescription,
    InterventionExecution? Execution,
    InterventionOutcome? Outcome,
    string Status);
