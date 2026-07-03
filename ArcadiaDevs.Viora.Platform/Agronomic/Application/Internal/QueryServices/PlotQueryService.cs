using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class PlotQueryService(
    IPlotRepository plotRepository,
    IIoTDeviceRepository ioTDeviceRepository,
    ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock) : IPlotQueryService
{
    public async Task<Result<PlotResource, Error>> Handle(GetPlotByIdQuery query, CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(query.PlotId, cancellationToken);
        if (plot is null || plot.IsDeleted)
            return new Result<PlotResource, Error>.Failure(AgronomicErrors.PlotNotFound);

        var resource = MapToPlotResource(plot, clock);

        return new Result<PlotResource, Error>.Success(resource);
    }

    public async Task<Result<IEnumerable<PlotResource>, Error>> Handle(GetPlotsByUserIdQuery query, CancellationToken cancellationToken = default)
    {
        var plots = await plotRepository.ListAsync(cancellationToken);
        var userPlots = plots.Where(p => p.OwnerUserId == query.UserId && !p.IsDeleted)
                             .Select(p => MapToPlotResource(p, clock))
                             .ToList();

        return new Result<IEnumerable<PlotResource>, Error>.Success(userPlots);
    }

    public async Task<Result<IEnumerable<PlotWithCurrentImageryResource>, Error>> Handle(GetPlotsWithCurrentImageryQuery query, CancellationToken cancellationToken = default)
    {
        var plots = await plotRepository.ListAsync(cancellationToken);
        var userPlots = plots.Where(p => p.OwnerUserId == query.UserId && !p.IsDeleted)
                             .Select(p => MapToPlotWithImageryResource(p, clock))
                             .ToList();

        return new Result<IEnumerable<PlotWithCurrentImageryResource>, Error>.Success(userPlots);
    }

    public async Task<Result<MyPlotsOverviewResource, Error>> Handle(GetMyPlotsOverviewQuery query, CancellationToken cancellationToken = default)
    {
        var plots = await plotRepository.ListAsync(cancellationToken);
        var userPlots = plots.Where(p => p.OwnerUserId == query.UserId && !p.IsDeleted).ToList();

        var totalArea = userPlots.Sum(p => p.AreaSize);
        var userPlotIds = userPlots.Select(p => p.Id).ToList();
        var devices = userPlotIds.Any()
            ? await ioTDeviceRepository.FindAllByPlotIdsAsync(userPlotIds.Select(id => (long)id), cancellationToken)
            : new List<IoTDevice>();

        var activeDevices = devices.Count(d => d.Status == Domain.Model.ValueObjects.IoTDeviceStatus.Active);

        var overviewPlots = userPlots.Select(p =>
        {
            var polygon = p.PolygonCoordinates.Points
                .Select(pt => (IEnumerable<double>)new double[] { (double)pt.Longitude, (double)pt.Latitude })
                .ToList();

            var plotDevicesCount = devices.Count(d => d.PlotId == p.Id && d.Status == Domain.Model.ValueObjects.IoTDeviceStatus.Active);

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
                "Healthy",
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

    private static PlotResource MapToPlotResource(Plot plot, ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock)
    {
        var polygon = plot.PolygonCoordinates.Points
            .Select(p => (IEnumerable<double>)new double[] { (double)p.Longitude, (double)p.Latitude })
            .ToList();

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        return new PlotResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            polygon,
            plot.AreaSize,
            plot.CreatedAt ?? now,
            plot.CropType,
            plot.Variety,
            plot.Location,
            plot.Campaign,
            plot.Notes,
            plot.IsActive ? "active" : "inactive"
        );
    }

    private static PlotWithCurrentImageryResource MapToPlotWithImageryResource(Plot plot, ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock)
    {
        var polygon = plot.PolygonCoordinates.Points
            .Select(p => (IEnumerable<double>)new double[] { (double)p.Longitude, (double)p.Latitude })
            .ToList();

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        var imagery = new CurrentImageryResource(
            "img-" + plot.Id,
            plot.Id,
            "https://satellite.viora.local/tiles/" + plot.Id,
            now.AddDays(-1),
            0.65,
            0.05
        );

        return new PlotWithCurrentImageryResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            polygon,
            plot.AreaSize,
            plot.CreatedAt ?? now,
            plot.CropType,
            plot.Variety,
            plot.Location,
            plot.Campaign,
            plot.Notes,
            plot.IsActive ? "active" : "inactive",
            imagery
        );
    }
}
