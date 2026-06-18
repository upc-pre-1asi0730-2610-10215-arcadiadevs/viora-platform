using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;

/// <summary>
///     Application contract for monitoring summary queries.
/// </summary>
public interface IMonitoringSummaryQueryService
{
    /// <summary>
    ///     Returns aggregated KPI metrics for a specific user.
    /// </summary>
    /// <param name="query">The query containing the user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    ///     A <see cref="Result{TValue,TError}"/> containing a <see cref="MonitoringSummaryResource"/> on success,
    ///     or an <see cref="Error"/> describing the failure.
    /// </returns>
    Task<Result<MonitoringSummaryResource, Error>> Handle(
        GetCurrentMonitoringSummaryQuery query,
        CancellationToken cancellationToken = default);
}
