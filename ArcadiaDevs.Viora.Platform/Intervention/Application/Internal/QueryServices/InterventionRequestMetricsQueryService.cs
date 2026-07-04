using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Handles aggregate request-metrics read queries (REQ-OV-3), scoped by
///     either <c>growerId</c> or <c>specialistId</c>.
/// </summary>
public class InterventionRequestMetricsQueryService(
    IInterventionRequestRepository interventionRequestRepository,
    InterventionOverviewComposer composer)
    : IInterventionRequestMetricsQueryService
{
    /// <remarks>
    ///     When scoped by <c>specialistId</c>, there is no dedicated
    ///     repository lookup (only <c>ListByGrowerIdAsync</c> exists on
    ///     <see cref="IInterventionRequestRepository" />), so the full
    ///     request list is loaded via <c>ListAsync</c> and filtered
    ///     in-memory — mirrors <c>SpecialistQueryService.Handle(GetSpecialistCandidatesQuery)</c>'s
    ///     existing precedent of ranking/filtering the full repository
    ///     result set at this codebase's scale, rather than adding a new
    ///     repository method for a single WU8 read model.
    /// </remarks>
    public async Task<InterventionRequestMetrics> Handle(
        GetInterventionRequestMetricsQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Domain.Model.Aggregates.InterventionRequest> requests;
        if (query.GrowerId is int growerId)
        {
            requests = await interventionRequestRepository.ListByGrowerIdAsync(growerId, null, cancellationToken);
        }
        else
        {
            var all = await interventionRequestRepository.ListAsync(cancellationToken);
            requests = all.Where(r => r.SpecialistId == query.SpecialistId).ToList();
        }

        var pending = 0;
        var awaitingResponse = 0;
        var proposalReceived = 0;
        var accepted = 0;
        var declined = 0;
        var closed = 0;

        foreach (var request in requests)
        {
            switch (request.Status)
            {
                case InterventionStatus.PENDING:
                    pending++;
                    break;
                case InterventionStatus.AWAITING_RESPONSE:
                    awaitingResponse++;
                    break;
                case InterventionStatus.PROPOSAL_RECEIVED:
                    proposalReceived++;
                    break;
                case InterventionStatus.ACCEPTED:
                    accepted++;
                    break;
                case InterventionStatus.DECLINED:
                    declined++;
                    break;
            }

            var chain = await composer.ComposeAsync(request, cancellationToken);
            if (chain.Status == "CLOSED")
            {
                closed++;
            }
        }

        var total = requests.Count;
        var acceptanceRate = total == 0 ? 0d : (double)accepted / total;
        var completionRate = total == 0 ? 0d : (double)closed / total;

        return new InterventionRequestMetrics(
            total,
            pending,
            awaitingResponse,
            proposalReceived,
            accepted,
            declined,
            acceptanceRate,
            closed,
            completionRate);
    }
}
