using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class GetMyPlotsOverviewQueryService(
    IPlotRepository plotRepository,
    IIoTDeviceRepository ioTDeviceRepository,
    ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock) : IGetMyPlotsOverviewQueryService
{
    public async Task<Result<MyPlotsOverviewResource, Error>> Handle(GetMyPlotsOverviewQuery query, CancellationToken cancellationToken = default)
    {
        var plots = await plotRepository.ListAsync(cancellationToken);
        var userPlots = plots.Where(p => p.OwnerUserId == query.UserId && !p.IsDeleted).ToList();

        var totalArea = userPlots.Sum(p => p.AreaSize);
        var userPlotIds = userPlots.Select(p => p.Id).ToList();
        var devices = userPlotIds.Any() ? await ioTDeviceRepository.FindAllByPlotIdsAsync(userPlotIds.Select(id => (long)id), cancellationToken) : new List<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates.IoTDevice>();
        
        var activeDevices = devices.Count(d => d.Status == ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects.IoTDeviceStatus.Active);

        var overviewPlots = userPlots.Select(p => {
            var polygon = p.PolygonCoordinates.Points
                .Select(pt => (IEnumerable<double>)new double[] { (double)pt.Longitude, (double)pt.Latitude })
                .ToList();

            var plotDevicesCount = devices.Count(d => d.PlotId == p.Id && d.Status == ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects.IoTDeviceStatus.Active);

            return new OverviewPlotResource(
                p.Id,
                p.OwnerUserId,
                p.PlotName,
                p.Location,
                p.Campaign,
                p.CropType,
                p.Variety,
                polygon,
                p.AreaSize,
                0.65,
                120.5,
                "Good",
                "Low",
                plotDevicesCount,
                0,
                new DateTimeOffset(clock.UtcNow, TimeSpan.Zero),
                "active",
                "active"
            );
        }).ToList();

        return new Result<MyPlotsOverviewResource, Error>.Success(
            new MyPlotsOverviewResource(userPlots.Count, totalArea, userPlots.Count, activeDevices, overviewPlots));
    }
}
