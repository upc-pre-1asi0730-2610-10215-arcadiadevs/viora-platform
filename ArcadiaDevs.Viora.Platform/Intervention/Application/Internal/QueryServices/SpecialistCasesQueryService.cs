using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Acl;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Assembles the specialist's cases read model (My Requests + Field
///     Inspection). Iterates the specialist's non-PENDING requests (PENDING
///     is the Marketplace inbox), composes each with its accepted/latest
///     proposal and downstream treatment-prescription / execution /
///     outcome, and enriches it across contexts. Mirrors OS's
///     <c>SpecialistCasesQueryServiceImpl</c>.
/// </summary>
/// <remarks>
///     <c>FieldStage</c> (accepted cases only) reflects the specialist's
///     on-site lifecycle: NEEDS_VISIT → FINDINGS_LOGGED → PRESCRIBED →
///     CLOSED. Execution and outcome are producer actions, so PRESCRIBED
///     covers "prescription sent, awaiting the producer" until the outcome
///     is CLOSED.
/// </remarks>
public class SpecialistCasesQueryService(
    IInterventionRequestRepository interventionRequestRepository,
    IServiceProposalRepository serviceProposalRepository,
    ITreatmentPrescriptionRepository treatmentPrescriptionRepository,
    IInterventionExecutionRepository interventionExecutionRepository,
    IInterventionOutcomeRepository interventionOutcomeRepository,
    ISurveillanceContextFacade surveillanceContextFacade,
    IAgronomicContextFacade agronomicContextFacade,
    IProfileContextFacade profileContextFacade,
    IClock clock)
    : ISpecialistCasesQueryService
{
    public async Task<SpecialistCasesResource> Handle(
        GetSpecialistCasesQuery query,
        CancellationToken cancellationToken = default)
    {
        var requests = (await interventionRequestRepository.FindBySpecialistIdAsync(query.SpecialistId, cancellationToken))
            .Where(r => r.Status != InterventionStatus.PENDING)
            .ToList();

        var cases = new List<SpecialistCasesResource.Case>(requests.Count);
        foreach (var request in requests)
        {
            cases.Add(await ToCaseAsync(request, cancellationToken));
        }

        var awaiting = cases.Count(c => IsAwaiting(c.RequestStatus));
        var closed = cases.Count(c => c.FieldStage == "CLOSED");
        var inProgress = cases.Count(c => c.RequestStatus == nameof(InterventionStatus.ACCEPTED) && c.FieldStage != "CLOSED");
        var declined = cases.Count(c => c.RequestStatus == nameof(InterventionStatus.DECLINED));
        var needsVisit = cases.Count(c => c.FieldStage == "NEEDS_VISIT");
        var prescriptionPending = cases.Count(c => c.FieldStage == "FINDINGS_LOGGED");
        var prescribed = cases.Count(c => c.FieldStage == "PRESCRIBED");

        var proposals = await serviceProposalRepository.FindBySpecialistIdAsync(query.SpecialistId, cancellationToken);

        return new SpecialistCasesResource(
            awaiting,
            inProgress,
            closed,
            declined,
            needsVisit,
            prescriptionPending,
            prescribed,
            AcceptanceRatePercent(proposals),
            cases,
            new DateTimeOffset(clock.UtcNow, TimeSpan.Zero).ToString("O"));
    }

    private async Task<SpecialistCasesResource.Case> ToCaseAsync(InterventionRequest request, CancellationToken cancellationToken)
    {
        var proposals = await serviceProposalRepository.ListByInterventionRequestIdAsync(request.Id, cancellationToken);
        var proposal = proposals.FirstOrDefault(p => p.Status == ServiceProposalStatus.ACCEPTED)
            ?? proposals.OrderByDescending(p => p.Id).FirstOrDefault();

        TreatmentPrescription? prescription = null;
        string? fieldStage = null;
        if (request.Status == InterventionStatus.ACCEPTED && proposal is not null)
        {
            prescription = await treatmentPrescriptionRepository.FindByServiceProposalIdAsync(proposal.Id, cancellationToken);
            fieldStage = await DeriveFieldStageAsync(prescription, cancellationToken);
        }

        var alert = request.AlertId is { } alertId
            ? await surveillanceContextFacade.GetAlertCardSummaryAsync(alertId, cancellationToken)
            : null;
        var plot = await agronomicContextFacade.GetPlotCardSummaryAsync(request.PlotId, cancellationToken);
        var producerName = await profileContextFacade.GetDisplayNameAsync(request.GrowerId, cancellationToken)
            ?? $"Grower #{request.GrowerId}";

        var cost = proposal?.CostEstimate;

        return new SpecialistCasesResource.Case(
            request.Id,
            $"REQ-{request.Id}",
            proposal?.Id,
            prescription?.Id,
            request.Status.ToString(),
            fieldStage,
            alert?.Severity,
            ProblemLabel(alert, request),
            producerName,
            plot?.Name ?? $"Plot #{request.PlotId}",
            BlankToNull(plot?.Location),
            cost?.Amount,
            cost?.Currency,
            proposal?.ProposedDate.ToString("O"),
            request.CreatedAt?.ToString("O"),
            request.UpdatedAt?.ToString("O"));
    }

    /// <summary>Derives the specialist's on-site field stage from the treatment prescription.</summary>
    private async Task<string> DeriveFieldStageAsync(TreatmentPrescription? prescription, CancellationToken cancellationToken)
    {
        if (prescription is null || prescription.Status == TreatmentPrescriptionStatus.PENDING_INSPECTION)
        {
            return "NEEDS_VISIT";
        }

        if (prescription.Status == TreatmentPrescriptionStatus.INSPECTED)
        {
            return "FINDINGS_LOGGED";
        }

        // PRESCRIBED: the ball is with the producer (application + outcome).
        // Once the outcome closes, the case is done for the specialist too.
        var execution = await interventionExecutionRepository.FindByTreatmentPrescriptionIdAsync(prescription.Id, cancellationToken);
        if (execution is not null)
        {
            var outcome = await interventionOutcomeRepository.FindByInterventionExecutionIdAsync(execution.Id, cancellationToken);
            if (outcome is not null && outcome.Status == InterventionOutcomeStatus.CLOSED)
            {
                return "CLOSED";
            }
        }

        return "PRESCRIBED";
    }

    private static bool IsAwaiting(string requestStatus)
    {
        return requestStatus == nameof(InterventionStatus.PROPOSAL_RECEIVED)
            || requestStatus == nameof(InterventionStatus.AWAITING_RESPONSE);
    }

    /// <summary>Prefers the alert's problem label; falls back to the request's stated reason.</summary>
    private static string? ProblemLabel(AlertCardSummary? alert, InterventionRequest request)
    {
        if (alert is not null && !string.IsNullOrWhiteSpace(alert.ProblemLabel))
        {
            return alert.ProblemLabel;
        }

        return string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason;
    }

    /// <summary>
    ///     Acceptance rate as accepted vs. decided proposals, or <c>null</c>
    ///     when the specialist has no decided proposals yet.
    /// </summary>
    private static double? AcceptanceRatePercent(IReadOnlyList<ServiceProposal> proposals)
    {
        var accepted = proposals.Count(p => p.Status == ServiceProposalStatus.ACCEPTED);
        var decided = proposals.Count(p =>
            p.Status == ServiceProposalStatus.ACCEPTED || p.Status == ServiceProposalStatus.REJECTED);

        if (decided == 0)
        {
            return null;
        }

        return Math.Round(accepted * 100.0 / decided, 1);
    }

    private static string? BlankToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
