using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Handles IoT device query operations.
/// </summary>
/// <remarks>
///     (TS012TASK004) Validates plot ownership via <see cref="IPlotRepository"/> before
///     delegating the device lookup to <see cref="IIoTDeviceRepository"/>.
/// </remarks>
public class IoTDeviceQueryService(
    IIoTDeviceRepository ioTDeviceRepository,
    IPlotRepository plotRepository) : IIoTDeviceQueryService
{
    /// <inheritdoc />
    /// <remarks>
    ///     Returns a 403-equivalent <see cref="Error"/> when the authenticated user
    ///     is not the owner of the requested plot.
    /// </remarks>
    public async Task<Result<IEnumerable<IoTDevice>, Error>> Handle(
        GetIoTDevicesByPlotIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var plotOwned = await plotRepository.ExistsByIdAndOwnerUserIdAsync(
            query.PlotId,
            query.AuthenticatedUserId,
            cancellationToken);

        if (!plotOwned)
            return new Result<IEnumerable<IoTDevice>, Error>.Failure(
                new Error(
                    "PLOT_OWNERSHIP_VIOLATION",
                    $"User {query.AuthenticatedUserId} does not own plot {query.PlotId}."));

        var devices = await ioTDeviceRepository.FindAllByPlotIdAsync(
            query.PlotId);

        return new Result<IEnumerable<IoTDevice>, Error>.Success(devices);
    }
}