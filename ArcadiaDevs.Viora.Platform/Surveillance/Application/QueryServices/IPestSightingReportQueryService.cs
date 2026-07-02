using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;

/// <summary>
///     Service that handles queries related to pest sighting reports.
/// </summary>
public interface IPestSightingReportQueryService
{
    /// <summary>
    ///     Handles the query for a reporter's submitted reports, newest first.
    /// </summary>
    /// <param name="query">The query containing the reporter's user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reporter's reports, newest first (empty when none exist).</returns>
    Task<IEnumerable<PestSightingReport>> Handle(GetPestSightingReportsByUserQuery query, CancellationToken cancellationToken = default);
}
