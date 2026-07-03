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
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class GetPlotDetailQueryService(
    IPlotRepository plotRepository,
    IIoTDeviceRepository ioTDeviceRepository,
    ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock) : IGetPlotDetailQueryService
{
    public async Task<Result<PlotDetailResource, Error>> Handle(GetPlotDetailQuery query, CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(query.PlotId, cancellationToken);
        if (plot is null || plot.IsDeleted)
            return new Result<PlotDetailResource, Error>.Failure(AgronomicErrors.PlotNotFound);

        if (plot.OwnerUserId != query.UserId)
            return new Result<PlotDetailResource, Error>.Failure(AgronomicErrors.PlotOwnership);

        var devices = await ioTDeviceRepository.FindAllByPlotIdsAsync(new[] { (long)plot.Id }, cancellationToken);
        var onlineDevices = devices.Count(d => d.Status == ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects.IoTDeviceStatus.Active);
        
        var polygon = plot.PolygonCoordinates.Points
            .Select(pt => (IEnumerable<double>)new double[] { (double)pt.Longitude, (double)pt.Latitude })
            .ToList();

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        var deviceResources = devices.Select(d => new PlotDeviceResource(
            d.Id,
            d.DeviceName,
            d.Status.ToString(),
            now, // default
            now  // default
        )).ToList();

        var activities = new List<RecentConfigurationActivityResource>
        {
            new RecentConfigurationActivityResource("Creation", "Plot registered", plot.CreatedAt ?? now)
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
            plot.CreatedAt ?? now,
            plot.CreatedAt ?? now,
            new MonitoringLinksResource("active", "active", now, now),
            new IoTDetailResource("active", devices.Count(), onlineDevices, now),
            deviceResources,
            activities
        );

        return new Result<PlotDetailResource, Error>.Success(resource);
    }
}
