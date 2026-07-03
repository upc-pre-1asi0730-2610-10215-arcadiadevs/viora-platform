using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Handles <see cref="ServiceProposal" /> read queries (REQ-SP-4).
/// </summary>
public class ServiceProposalQueryService(IServiceProposalRepository serviceProposalRepository)
    : IServiceProposalQueryService
{
    public async Task<IReadOnlyList<ServiceProposal>> Handle(
        ListServiceProposalsByRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        return await serviceProposalRepository.ListByInterventionRequestIdAsync(
            query.InterventionRequestId, cancellationToken);
    }
}
