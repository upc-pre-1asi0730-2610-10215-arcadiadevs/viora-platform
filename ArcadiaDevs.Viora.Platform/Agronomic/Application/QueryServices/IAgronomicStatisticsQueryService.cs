using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;

/// <summary>
///     Application contract for agronomic statistics queries.
/// </summary>
public interface IAgronomicStatisticsQueryService
{
    /// <summary>
    ///     Returns time series data for NDVI and cold portions for a user's plots.
    /// </summary>
    /// <param name="query">The query containing user, plot, and time range filters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    ///     A <see cref="Result{TValue,TError}"/> containing a list of <see cref="AgronomicStatisticsResource"/> on success,
    ///     or an <see cref="Error"/> describing the failure.
    /// </returns>
    Task<Result<IEnumerable<AgronomicStatistic>, Error>> Handle(
        GetAgronomicStatisticsQuery query,
        CancellationToken cancellationToken = default);
}
