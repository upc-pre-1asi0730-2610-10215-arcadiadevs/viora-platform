using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class GetPlotMonitoringSummaryQueryService(
    IPlotRepository plotRepository,
    IAgroMonitoringImageryService imageryService,
    ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock) : IGetPlotMonitoringSummaryQueryService
{
    public async Task<Result<PlotMonitoringSummaryResource, Error>> Handle(GetPlotMonitoringSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(query.PlotId, cancellationToken);
        if (plot is null || plot.IsDeleted)
            return new Result<PlotMonitoringSummaryResource, Error>.Failure(AgronomicErrors.PlotNotFound);

        if (plot.OwnerUserId != query.UserId)
            return new Result<PlotMonitoringSummaryResource, Error>.Failure(AgronomicErrors.PlotOwnership);

        // Trigger synchronization with external AgroMonitoring API cache
        await imageryService.FindCurrentImageryAsync(plot, cancellationToken);

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);
        var tomorrow = now.AddDays(1);

        var series = new List<NdviTrendSeriesResource>
        {
            new NdviTrendSeriesResource(now, 0.65, 0.60, 0.70, 0.65)
        };

        var trend = new NdviTrendResource("up", 0.05, series);
        var weather = new WeatherSummaryResource("Sunny", now.ToString("yyyy-MM-dd"), "Low", 22.5);
        
        var recommendations = new List<RecommendationResource>
        {
            new RecommendationResource("Irrigation", "Apply 10mm", now.ToString("yyyy-MM-dd"), tomorrow.ToString("yyyy-MM-dd"))
        };

        var source = new ExternalSourceResource("AgroMonitoring", "Online", now, 60);

        var resource = new PlotMonitoringSummaryResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            0.65,
            trend,
            120.5,
            10.2,
            400,
            "Davis",
            "Dynamic",
            "Portions",
            "Healthy",
            "Low",
            4500,
            weather,
            "Low",
            now,
            recommendations,
            source,
            source
        );

        return new Result<PlotMonitoringSummaryResource, Error>.Success(resource);
    }
}
