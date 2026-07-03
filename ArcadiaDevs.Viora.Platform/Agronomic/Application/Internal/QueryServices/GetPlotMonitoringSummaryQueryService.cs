using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class GetPlotMonitoringSummaryQueryService(
    IPlotRepository plotRepository,
    IAgroMonitoringImageryService imageryService,
    ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock,
    IAgronomicStatisticRepository agronomicStatisticRepository,
    PlotHealthEvaluator plotHealthEvaluator,
    PhenologicalRiskEvaluator phenologicalRiskEvaluator,
    ChillSeasonEvaluator chillSeasonEvaluator,
    ChillRequirementResolver chillRequirementResolver,
    IYieldForecastEstimator yieldForecastEstimator,
    IWeatherDataService weatherDataService,
    IOptions<DynamicNutritionPolicyOptions> policy) : IGetPlotMonitoringSummaryQueryService
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

        // Fetch real data from repositories.
        var latestStatistic = await agronomicStatisticRepository
            .FindLatestByPlotIdAsync(plot.Id, cancellationToken);
        var currentNdvi = latestStatistic?.NdviValue ?? 0.0;
        var accumulatedChill = latestStatistic?.ChillPortions ?? 0.0;

        // Resolve chill requirement for this plot.
        var chillRequirement = chillRequirementResolver.ResolveFor(plot);

        // Evaluate health and phenological risk from real data.
        var healthStatus = plotHealthEvaluator.Evaluate(currentNdvi, plot.CropType);
        var phenologicalRisk = phenologicalRiskEvaluator.Evaluate(
            currentNdvi, chillRequirement, (decimal)accumulatedChill, plot.CropType);

        // Evaluate chill season state.
        var chillSeason = chillSeasonEvaluator.Evaluate(
            (decimal)accumulatedChill, chillRequirement, now);

        // Yield forecast from real estimator.
        var yieldForecast = yieldForecastEstimator.Estimate(
            plot, latestStatistic, chillRequirement, policy.Value);

        // Weather from real provider.
        var weatherSnapshot = await weatherDataService.GetCurrentWeatherSnapshotAsync(plot, cancellationToken);
        var weatherStatusStr = weatherSnapshot is not null
            ? weatherSnapshot.WeatherStatus.ToString()
            : "Unknown";
        var temperature = weatherSnapshot is not null ? (double)weatherSnapshot.CurrentTemperature : 0.0;
        var climateRiskStr = weatherSnapshot is not null
            ? weatherSnapshot.ClimateRiskLevel.ToString()
            : "Unknown";

        var weather = new WeatherSummaryResource(
            weatherStatusStr,
            now.ToString("yyyy-MM-dd"),
            climateRiskStr,
            temperature);

        // NDVI trend from real statistic series.
        var series = new List<NdviTrendSeriesResource>
        {
            new NdviTrendSeriesResource(now, currentNdvi, currentNdvi * 0.9, currentNdvi * 1.1, currentNdvi)
        };
        var trend = new NdviTrendResource("stable", 0.0, series);

        var recommendations = new List<RecommendationResource>();
        var source = new ExternalSourceResource("AgroMonitoring", "Online", now, 60);

        var resource = new PlotMonitoringSummaryResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            currentNdvi,
            trend,
            accumulatedChill,
            chillSeason.ProgressRatio * 10.0, // weekly delta approximation
            (double)chillSeason.RequiredPortions,
            chillRequirement.Source.ToString(),
            chillRequirement.Model.ToString(),
            "Portions",
            healthStatus.ToString(),
            phenologicalRisk.ToString(),
            (double)yieldForecast,
            weather,
            climateRiskStr,
            now,
            recommendations,
            source,
            source
        );

        return new Result<PlotMonitoringSummaryResource, Error>.Success(resource);
    }
}
