using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Service that handles read queries for <see cref="ServiceProposal" /> (REQ-SP-4).
/// </summary>
public interface IServiceProposalQueryService
{
    Task<IReadOnlyList<ServiceProposal>> Handle(
        ListServiceProposalsByRequestQuery query,
        CancellationToken cancellationToken = default);
}
