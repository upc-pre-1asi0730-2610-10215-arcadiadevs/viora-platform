using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

internal class GetPlotNdviTileQueryService(
    IPlotRepository plotRepository,
    IAgroMonitoringImageryService imageryService) : IGetPlotNdviTileQueryService
{
    public async Task<Result<byte[], Error>> HandleAsync(GetPlotNdviTileQuery query, CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(query.PlotId, cancellationToken);
        if (plot == null)
        {
            return new Result<byte[], Error>.Failure(new Error("PLOT_NOT_FOUND", $"Plot {query.PlotId} not found."));
        }

        if (plot.OwnerUserId != query.UserId)
        {
            return new Result<byte[], Error>.Failure(new Error("PLOT_NOT_OWNED", $"Plot {query.PlotId} does not belong to user {query.UserId}."));
        }

        var isLinked = await imageryService.IsPlotLinkedAsync(plot, cancellationToken);
        if (!isLinked)
        {
            return new Result<byte[], Error>.Failure(new Error("PLOT_NOT_LINKED", "Plot is not linked to AgroMonitoring."));
        }

        var tileBytes = await imageryService.FetchCurrentNdviTileAsync(plot, query.Zoom, query.X, query.Y, cancellationToken);

        if (tileBytes == null || tileBytes.Length == 0)
        {
            return new Result<byte[], Error>.Failure(new Error("TILE_NOT_FOUND", "Could not fetch tile."));
        }

        return new Result<byte[], Error>.Success(tileBytes);
    }
}
