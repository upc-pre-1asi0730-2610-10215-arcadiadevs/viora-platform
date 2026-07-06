using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Handles <see cref="ServiceProposal" /> read queries (REQ-SP-4).
/// </summary>
public class ServiceProposalQueryService(
    IServiceProposalRepository serviceProposalRepository,
    IInterventionRequestRepository interventionRequestRepository)
    : IServiceProposalQueryService
{
    public async Task<IReadOnlyList<ServiceProposal>> Handle(
        ListServiceProposalsByRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        // OS has an explicit ownsRequest() guard here; WA previously had none.
        // A non-owner (or a request that doesn't exist) sees an empty list,
        // same as an owned request with zero proposals — no existence leak.
        var request = await interventionRequestRepository.FindByIdAsync(query.InterventionRequestId, cancellationToken);
        if (request is null || request.GrowerId != query.GrowerId)
        {
            return Array.Empty<ServiceProposal>();
        }

        return await serviceProposalRepository.ListByInterventionRequestIdAsync(
            query.InterventionRequestId, cancellationToken);
    }
}
