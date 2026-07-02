using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Adapters;

public class JpaPlotDetailMetadataProvider(
    IPlotRepository plotRepository,
    IIoTDeviceRepository ioTDeviceRepository) : IPlotDetailMetadataProvider
{
    public async Task<PlotMetadata?> FindByPlotIdAsync(int plotId, CancellationToken ct = default)
    {
        var plot = await plotRepository.FindByIdAsync(plotId, ct);
        if (plot is null)
            return null;

        var devices = (await ioTDeviceRepository.FindAllByPlotIdAsync(plotId)).ToList();

        return new PlotMetadata(
            plot.CreatedAt ?? DateTimeOffset.MinValue,
            null,
            MonitoringIntegration: null,
            devices.Select(d => new DeviceMetadata(
                d.Id,
                d.CreatedAt ?? DateTimeOffset.MinValue,
                d.UpdatedAt)).ToList());
    }
}
