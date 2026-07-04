using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Services;

/// <summary>
///     Composes the downstream FK chain for a single <see cref="InterventionRequest" />
///     — <c>InterventionRequest ← ServiceProposal ← TreatmentPrescription ←
///     InterventionExecution ← InterventionOutcome</c> (REQ-CC-3) — following
///     only the links that actually exist (REQ-OV-1), and derives the
///     query-time-only <c>status</c> (REQ-OV-2). Shared by
///     <c>InterventionOverviewQueryService</c> (WU8, REQ-OV-1) and
///     <c>InterventionRequestMetricsQueryService</c> (WU8, REQ-OV-3) so the
///     chain-traversal logic is written once — mirrors <c>SpecialistMatchingPolicy</c>'s
///     precedent of a plain domain service consumed by an application query
///     service, registered directly (no interface) via
///     <c>builder.Services.AddScoped&lt;InterventionOverviewComposer&gt;()</c>.
/// </summary>
/// <remarks>
///     Status derivation follows REQ-OV-2 with one necessary interpretation:
///     REQ-OV-2's <c>RECOVERY_MONITORING</c> bucket ("execution exists, no
///     outcome or outcome IMPACT_REPORTED") and its <c>READY_TO_CLOSE</c>
///     bucket ("outcome IMPACT_REPORTED and evaluation criteria for
///     readiness met") both list "outcome IMPACT_REPORTED" as a condition,
///     which is only consistent if <c>READY_TO_CLOSE</c> requires something
///     beyond the outcome's bare status — a criterion the spec does not
///     define anywhere else in the domain model. Since no additional signal
///     exists (an outcome has exactly two states, <c>IMPACT_REPORTED</c> and
///     <c>CLOSED</c>), this implementation treats "impact reported" itself
///     as the readiness signal: <c>RECOVERY_MONITORING</c> = execution
///     exists, no outcome yet; <c>READY_TO_CLOSE</c> = outcome exists and is
///     <c>IMPACT_REPORTED</c> (i.e. reported, awaiting the grower's closing
///     evaluation); <c>CLOSED</c> = outcome <c>CLOSED</c>. This is a
///     deliberate, documented interpretation, not an oversight — see WU8
///     apply-progress for the full rationale. A second interpretation not
///     covered by REQ-OV-2 at all: a request whose own <c>status</c> is
///     <c>DECLINED</c> (REQ-IREQ-3) is surfaced as <c>DECLINED</c> rather
///     than falling into <c>AWAITING_PRESCRIPTION</c> by default — showing a
///     declined request as "awaiting" would misrepresent a terminal state.
/// </remarks>
public class InterventionOverviewComposer(
    IServiceProposalRepository serviceProposalRepository,
    ITreatmentPrescriptionRepository treatmentPrescriptionRepository,
    IInterventionExecutionRepository interventionExecutionRepository,
    IInterventionOutcomeRepository interventionOutcomeRepository)
{
    /// <summary>
    ///     Resolves the downstream chain for <paramref name="request" />,
    ///     following only existing links, and derives its status.
    /// </summary>
    /// <remarks>
    ///     When more than one <see cref="ServiceProposal" /> exists for the
    ///     request (e.g. a rejected proposal followed by a later one), the
    ///     <c>ACCEPTED</c> proposal is preferred as "the" proposal that
    ///     carries the chain forward; if none is <c>ACCEPTED</c>, the most
    ///     recently submitted proposal (highest <c>Id</c>) is used, so the
    ///     overview always reflects the request's current engagement rather
    ///     than an arbitrary/oldest one.
    /// </remarks>
    public async Task<InterventionChainSnapshot> ComposeAsync(
        InterventionRequest request,
        CancellationToken cancellationToken = default)
    {
        var proposals = await serviceProposalRepository.ListByInterventionRequestIdAsync(request.Id, cancellationToken);
        var proposal = proposals.FirstOrDefault(p => p.Status == ServiceProposalStatus.ACCEPTED)
            ?? proposals.OrderByDescending(p => p.Id).FirstOrDefault();

        var prescription = proposal is null
            ? null
            : await treatmentPrescriptionRepository.FindByServiceProposalIdAsync(proposal.Id, cancellationToken);

        var execution = prescription is null
            ? null
            : await interventionExecutionRepository.FindByTreatmentPrescriptionIdAsync(prescription.Id, cancellationToken);

        var outcome = execution is null
            ? null
            : await interventionOutcomeRepository.FindByInterventionExecutionIdAsync(execution.Id, cancellationToken);

        var status = DeriveStatus(request, prescription, execution, outcome);

        return new InterventionChainSnapshot(proposal, prescription, execution, outcome, status);
    }

    /// <summary>
    ///     Derives the REQ-OV-2 status from the request's own status plus the
    ///     downstream chain — see the class remarks for the readiness-signal
    ///     interpretation and the DECLINED addition.
    /// </summary>
    private static string DeriveStatus(
        InterventionRequest request,
        TreatmentPrescription? prescription,
        InterventionExecution? execution,
        InterventionOutcome? outcome)
    {
        if (request.Status == InterventionStatus.DECLINED)
        {
            return "DECLINED";
        }

        if (outcome is not null && outcome.Status == InterventionOutcomeStatus.CLOSED)
        {
            return "CLOSED";
        }

        if (outcome is not null && outcome.Status == InterventionOutcomeStatus.IMPACT_REPORTED)
        {
            return "READY_TO_CLOSE";
        }

        if (execution is not null)
        {
            return "RECOVERY_MONITORING";
        }

        if (prescription is not null && prescription.Status == TreatmentPrescriptionStatus.PRESCRIBED)
        {
            return "PRESCRIPTION_ISSUED";
        }

        return "AWAITING_PRESCRIPTION";
    }
}
