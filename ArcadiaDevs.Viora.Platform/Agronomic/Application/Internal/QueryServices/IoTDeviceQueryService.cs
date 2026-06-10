using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Application contract for IoT device queries.
/// </summary>
public interface IIoTDeviceQueryService
{
    /// <summary>
    ///     Returns all IoT devices for a given plot, provided the requesting user owns the plot.
    /// </summary>
    /// <param name="query">The query containing the plot and authenticated user identifiers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    ///     A <see cref="Result{TValue,TError}"/> containing a list of devices on success,
    ///     or a <see cref="Error"/> describing the ownership violation on failure.
    /// </returns>
    Task<Result<IEnumerable<IoTDevice>, Error>> Handle(
        GetIoTDevicesByPlotIdQuery query,
        CancellationToken cancellationToken = default);
}