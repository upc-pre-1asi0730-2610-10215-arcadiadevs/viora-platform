using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class PlotQueryService(
    IPlotRepository plotRepository,
    IIoTDeviceRepository ioTDeviceRepository,
    ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock,
    IAgronomicStatisticRepository agronomicStatisticRepository,
    PlotHealthEvaluator plotHealthEvaluator,
    PhenologicalRiskEvaluator phenologicalRiskEvaluator,
    ChillRequirementResolver chillRequirementResolver) : IPlotQueryService
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
        var userPlots = plots.Where(p => p.OwnerUserId == query.UserId && !p.IsDeleted).ToList();

        // Fetch latest agronomic statistics for imagery data.
        var results = new List<PlotWithCurrentImageryResource>();
        foreach (var p in userPlots)
        {
            var statistic = await agronomicStatisticRepository.FindLatestByPlotIdAsync(p.Id, cancellationToken);
            results.Add(MapToPlotWithImageryResource(p, clock, statistic));
        }

        return new Result<IEnumerable<PlotWithCurrentImageryResource>, Error>.Success(results);
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

        // Fetch latest agronomic statistics per plot. IAgronomicStatisticRepository
        // has no batch-by-ids lookup (same per-plot pattern used elsewhere in this
        // BC, e.g. IoTDeviceQueryService) — N queries for N plots, not a single batch.
        var plotStats = new Dictionary<long, AgronomicStatistic?>();
        foreach (var p in userPlots)
        {
            var stat = await agronomicStatisticRepository.FindLatestByPlotIdAsync(p.Id, cancellationToken);
            plotStats[p.Id] = stat;
        }

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        var overviewPlots = userPlots.Select(p =>
        {
            var polygon = p.PolygonCoordinates.Points
                .Select(pt => (IEnumerable<double>)new double[] { (double)pt.Longitude, (double)pt.Latitude })
                .ToList();

            var plotDevicesCount = devices.Count(d => d.PlotId == p.Id && d.Status == Domain.Model.ValueObjects.IoTDeviceStatus.Active);

            var statistic = plotStats.GetValueOrDefault(p.Id);
            var ndvi = statistic?.NdviValue;
            var chillPortions = statistic?.ChillPortions ?? 0.0;

            var healthStatus = plotHealthEvaluator.Evaluate(ndvi, p.CropType);
            var chillRequirement = chillRequirementResolver.ResolveFor(p);
            var phenologicalRisk = phenologicalRiskEvaluator.Evaluate(
                ndvi, chillRequirement, (decimal)chillPortions, p.CropType);

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
                ndvi ?? 0.0,
                chillPortions,
                healthStatus.ToString(),
                phenologicalRisk.ToString(),
                plotDevicesCount,
                0,
                now,
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

    private static PlotWithCurrentImageryResource MapToPlotWithImageryResource(
        Plot plot, ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock, AgronomicStatistic? statistic)
    {
        var polygon = plot.PolygonCoordinates.Points
            .Select(p => (IEnumerable<double>)new double[] { (double)p.Longitude, (double)p.Latitude })
            .ToList();

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        // Real NDVI from the latest agronomic statistic; 0.0 when no data available
        // (no fabricated fallback — CC-8).
        var ndviMean = statistic?.NdviValue ?? 0.0;
        // Cloud percentage is not available from the statistic; default to 0.0
        // until satellite imagery metadata is wired separately.
        var cloudPercentage = 0.0;

        var imagery = new CurrentImageryResource(
            "img-" + plot.Id,
            plot.Id,
            "https://satellite.viora.local/tiles/" + plot.Id,
            now.AddDays(-1),
            ndviMean,
            cloudPercentage
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
