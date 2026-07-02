using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.QueryServices;

/// <summary>
///     Implementation of <see cref="IPestSightingReportQueryService"/>.
/// </summary>
public class PestSightingReportQueryService(IPestSightingReportRepository pestSightingReportRepository)
    : IPestSightingReportQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<PestSightingReport>> Handle(GetPestSightingReportsByUserQuery query, CancellationToken cancellationToken = default)
    {
        return await pestSightingReportRepository.FindByReporterUserIdAsync(query.ReporterUserId, cancellationToken);
    }
}
