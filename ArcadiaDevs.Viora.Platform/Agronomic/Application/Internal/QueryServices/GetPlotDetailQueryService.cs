using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class GetPlotDetailQueryService(
    IPlotRepository plotRepository,
    IIoTDeviceRepository ioTDeviceRepository) : IGetPlotDetailQueryService
{
    public async Task<Result<PlotDetailResource, Error>> Handle(GetPlotDetailQuery query, CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(query.PlotId, cancellationToken);
        if (plot is null || plot.IsDeleted)
            return new Result<PlotDetailResource, Error>.Failure(new Error("PLOT_NOT_FOUND", "Plot not found."));

        var devices = await ioTDeviceRepository.FindAllByPlotIdsAsync(new[] { (long)plot.Id }, cancellationToken);
        var onlineDevices = devices.Count(d => d.Status == ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects.IoTDeviceStatus.Active);
        
        var polygon = plot.PolygonCoordinates.Points
            .Select(pt => (IEnumerable<double>)new double[] { (double)pt.Longitude, (double)pt.Latitude })
            .ToList();

        var deviceResources = devices.Select(d => new PlotDeviceResource(
            d.Id,
            d.DeviceName,
            d.Status.ToString(),
            DateTimeOffset.UtcNow, // default
            DateTimeOffset.UtcNow // default
        )).ToList();

        var activities = new List<RecentConfigurationActivityResource>
        {
            new RecentConfigurationActivityResource("Creation", "Plot registered", plot.CreatedAt ?? DateTimeOffset.UtcNow)
        };

        var resource = new PlotDetailResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            plot.Location,
            plot.Campaign,
            plot.CropType,
            plot.Variety,
            plot.Notes,
            polygon,
            plot.AreaSize,
            polygon.Count,
            "Valid",
            plot.CreatedAt ?? DateTimeOffset.UtcNow,
            plot.CreatedAt ?? DateTimeOffset.UtcNow,
            new MonitoringLinksResource("active", "active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            new IoTDetailResource("active", devices.Count(), onlineDevices, DateTimeOffset.UtcNow),
            deviceResources,
            activities
        );

        return new Result<PlotDetailResource, Error>.Success(resource);
    }
}
