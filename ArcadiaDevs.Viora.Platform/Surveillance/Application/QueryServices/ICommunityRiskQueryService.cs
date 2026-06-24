using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;

/// <summary>
/// Query service for the anonymized community-risk snapshot around a reference plot.
/// </summary>
public interface ICommunityRiskQueryService
{
    /// <summary>
    /// Builds the community-risk snapshot for the query's reference plot and radius.
    /// </summary>
    /// <param name="query">The query carrying the reference plot and radius.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The snapshot, or null if the reference plot does not exist.</returns>
    Task<CommunityRiskResource?> Handle(GetCommunityRiskByPlotQuery query, CancellationToken cancellationToken = default);
}
