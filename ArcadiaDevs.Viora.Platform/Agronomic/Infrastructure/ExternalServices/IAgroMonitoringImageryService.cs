using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;

public interface IAgroMonitoringImageryService
{
    Task<bool> IsPlotLinkedAsync(Plot plot, CancellationToken cancellationToken = default);
    Task FindCurrentImageryAsync(Plot plot, CancellationToken cancellationToken = default);
    Task<byte[]?> FetchCurrentNdviTileAsync(Plot plot, int zoom, int x, int y, CancellationToken cancellationToken = default);
}
