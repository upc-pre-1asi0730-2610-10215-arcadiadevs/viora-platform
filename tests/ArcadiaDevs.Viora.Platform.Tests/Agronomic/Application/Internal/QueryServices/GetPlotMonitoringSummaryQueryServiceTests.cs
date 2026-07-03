using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Unit tests for <see cref="GetPlotMonitoringSummaryQueryService"/>.
///     Template B: query service with NSubstitute mocks.
///     Covers FU-4 (NDVI trend + chill weekly-delta approximation guards)
///     and FU-7 (Unknown sentinel for null weather).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class GetPlotMonitoringSummaryQueryServiceTests
{
    private readonly IPlotRepository _plotRepository = Substitute.For<IPlotRepository>();
    private readonly IAgroMonitoringImageryService _imageryService = Substitute.For<IAgroMonitoringImageryService>();
    private readonly IClock _clock = new FakeClock();
    private readonly IAgronomicStatisticRepository _agronomicStatisticRepository = Substitute.For<IAgronomicStatisticRepository>();
    private readonly PlotHealthEvaluator _plotHealthEvaluator = new();
    private readonly PhenologicalRiskEvaluator _phenologicalRiskEvaluator = new();
    private readonly ChillSeasonEvaluator _chillSeasonEvaluator = new();
    private readonly ChillRequirementResolver _chillRequirementResolver;
    private readonly IYieldForecastEstimator _yieldForecastEstimator = Substitute.For<IYieldForecastEstimator>();
    private readonly IWeatherDataService _weatherDataService = Substitute.For<IWeatherDataService>();
    private readonly IOptions<DynamicNutritionPolicyOptions> _policy;
    private readonly GetPlotMonitoringSummaryQueryService _sut;

    public GetPlotMonitoringSummaryQueryServiceTests()
    {
        var chillPolicy = new ChillRequirementPolicy(120.5);
        _chillRequirementResolver = new ChillRequirementResolver(chillPolicy);
        _policy = Options.Create(new DynamicNutritionPolicyOptions());

        _sut = new GetPlotMonitoringSummaryQueryService(
            _plotRepository,
            _imageryService,
            _clock,
            _agronomicStatisticRepository,
            _plotHealthEvaluator,
            _phenologicalRiskEvaluator,
            _chillSeasonEvaluator,
            _chillRequirementResolver,
            _yieldForecastEstimator,
            _weatherDataService,
            _policy);
    }

    private static Plot BuildPlot(int id, int ownerUserId)
    {
        var result = Plot.Create(
            ownerUserId: ownerUserId,
            plotName: $"Plot {id}",
            polygonCoordinates: ((Result<PolygonCoordinates, Error>.Success)PolygonCoordinates.Create(new List<GeoPoint>
            {
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-33.0m, -71.0m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-33.0m, -71.1m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-33.1m, -71.1m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-33.1m, -71.0m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-33.0m, -71.0m)).Value,
            })).Value,
            areaSize: 100m,
            cropType: "Coffee",
            variety: "Arabica",
            location: "Lima",
            campaign: "2026",
            notes: "test");
        var plot = ((Result<Plot, Error>.Success)result).Value!;
        typeof(Plot).GetProperty(nameof(Plot.Id))!.SetValue(plot, id);
        return plot;
    }

    private static AgronomicStatistic BuildStatistic(long plotId, double ndvi, double chillPortions)
    {
        var result = AgronomicStatistic.Create(
            userId: 1, plotId: plotId,
            measurementDate: new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero),
            ndviValue: ndvi, chillPortions: chillPortions, chillHours: chillPortions * 0.5,
            chillModelState: ChillModelState.Empty());
        return ((Result<AgronomicStatistic, Error>.Success)result).Value;
    }

    /// <summary>
    ///     GIVEN current NDVI = 0.75
    ///     WHEN <see cref="GetPlotMonitoringSummaryQueryService.Handle(GetPlotMonitoringSummaryQuery, CancellationToken)"/> builds the NDVI trend
    ///     THEN middle values are 0.675 and 0.825 (±10% approximation).
    ///     FU-4 regression guard: single-point approximation, not a real 3-point series.
    ///     Source: GetPlotMonitoringSummaryQueryService.cs:88-91
    /// </summary>
    [Fact]
    public async Task Handle_NdviTrend_UsesApproximation_DocumentedBehavior()
    {
        // GIVEN a plot with NDVI = 0.75
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);

        var statistic = BuildStatistic(10, ndvi: 0.75, chillPortions: 80.0);
        _agronomicStatisticRepository.FindLatestByPlotIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(statistic);

        var weather = new WeatherSnapshot(22.5m, WeatherStatus.Sunny, new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero), ClimateRiskLevel.Low, _clock);
        _weatherDataService.GetCurrentWeatherSnapshotAsync(plot, Arg.Any<CancellationToken>())
            .Returns(weather);

        _yieldForecastEstimator.Estimate(Arg.Any<Plot>(), Arg.Any<AgronomicStatistic?>(), Arg.Any<ChillRequirement>(), Arg.Any<DynamicNutritionPolicyOptions>())
            .Returns(1.5m);

        var query = new GetPlotMonitoringSummaryQuery(PlotId: 10, UserId: 1);

        // WHEN the summary is fetched
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN NDVI trend uses ±10% approximation (FU-4)
        Assert.True(result.IsSuccess);
        var summary = ((Result<PlotMonitoringSummaryResource, Error>.Success)result).Value!;
        var series = summary.NdviTrend.Series.ToList();
        Assert.Single(series);
        Assert.Equal(0.75, series[0].Mean);
        Assert.Equal(0.675, series[0].Minimum, 3); // 0.75 * 0.9 = 0.675
        Assert.Equal(0.825, series[0].Maximum, 3); // 0.75 * 1.1 = 0.825
    }

    /// <summary>
    ///     GIVEN chillSeason.ProgressRatio ≈ 0.42 (accumulated 50.61 / requirement 120.5)
    ///     WHEN <see cref="GetPlotMonitoringSummaryQueryService.Handle(GetPlotMonitoringSummaryQuery, CancellationToken)"/> computes chill weekly-delta
    ///     THEN the result equals ProgressRatio × 10.0.
    ///     FU-4 regression guard: the *10.0 magic number is a deliberate constant.
    ///     Source: GetPlotMonitoringSummaryQueryService.cs:104
    /// </summary>
    [Fact]
    public async Task Handle_ChillWeeklyDelta_UsesProgressRatioTimes10_DocumentedBehavior()
    {
        // GIVEN a plot with accumulated chill = 50.61, requirement = 120.5 → ProgressRatio ≈ 0.42
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);

        var statistic = BuildStatistic(10, ndvi: 0.75, chillPortions: 50.61);
        _agronomicStatisticRepository.FindLatestByPlotIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(statistic);

        var weather = new WeatherSnapshot(22.5m, WeatherStatus.Sunny, new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero), ClimateRiskLevel.Low, _clock);
        _weatherDataService.GetCurrentWeatherSnapshotAsync(plot, Arg.Any<CancellationToken>())
            .Returns(weather);

        _yieldForecastEstimator.Estimate(Arg.Any<Plot>(), Arg.Any<AgronomicStatistic?>(), Arg.Any<ChillRequirement>(), Arg.Any<DynamicNutritionPolicyOptions>())
            .Returns(1.5m);

        var query = new GetPlotMonitoringSummaryQuery(PlotId: 10, UserId: 1);

        // WHEN the summary is fetched
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN chill weekly-delta = ProgressRatio × 10.0 (FU-4)
        Assert.True(result.IsSuccess);
        var summary = ((Result<PlotMonitoringSummaryResource, Error>.Success)result).Value!;
        var expectedDelta = (50.61 / 120.5) * 10.0;
        Assert.Equal(expectedDelta, summary.ChillPortionsWeeklyDelta, 2);
    }

    /// <summary>
    ///     GIVEN a plot owned by user 1
    ///     WHEN user 2 requests the monitoring summary
    ///     THEN <see cref="AgronomicErrors.PlotOwnership"/> is returned (IDOR closure).
    /// </summary>
    [Fact]
    public async Task Handle_NonOwner_ReturnsForbidden()
    {
        // GIVEN a plot owned by user 1
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);

        // WHEN user 2 requests the monitoring summary — SECURITY CRITICAL: IDOR closure test
        var query = new GetPlotMonitoringSummaryQuery(PlotId: 10, UserId: 2);
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN PlotOwnership error is returned (403/404 equivalent)
        Assert.True(result.IsFailure);
        Assert.Equal(AgronomicErrors.PlotOwnership, ((Result<PlotMonitoringSummaryResource, Error>.Failure)result).Error);
    }

    /// <summary>
    ///     GIVEN IWeatherDataService returns null (upstream failure)
    ///     WHEN <see cref="GetPlotMonitoringSummaryQueryService.Handle(GetPlotMonitoringSummaryQuery, CancellationToken)"/> is called
    ///     THEN weatherStatusStr and climateRiskStr are "Unknown" (FU-7 sentinel).
    ///     Source: GetPlotMonitoringSummaryQueryService.cs:73-79
    /// </summary>
    [Fact]
    public async Task Handle_WeatherSnapshotNull_ReturnsUnknownSentinel_DocumentedBehavior()
    {
        // GIVEN a plot with no weather data available
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);

        var statistic = BuildStatistic(10, ndvi: 0.75, chillPortions: 80.0);
        _agronomicStatisticRepository.FindLatestByPlotIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(statistic);

        _weatherDataService.GetCurrentWeatherSnapshotAsync(plot, Arg.Any<CancellationToken>())
            .Returns((WeatherSnapshot?)null);

        _yieldForecastEstimator.Estimate(Arg.Any<Plot>(), Arg.Any<AgronomicStatistic?>(), Arg.Any<ChillRequirement>(), Arg.Any<DynamicNutritionPolicyOptions>())
            .Returns(1.5m);

        var query = new GetPlotMonitoringSummaryQuery(PlotId: 10, UserId: 1);

        // WHEN the summary is fetched
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN weather and climate risk are "Unknown" sentinel (FU-7)
        Assert.True(result.IsSuccess);
        var summary = ((Result<PlotMonitoringSummaryResource, Error>.Success)result).Value!;
        Assert.Equal("Unknown", summary.Weather.WeatherStatus);  // FU-7: no-data sentinel
        Assert.Equal("Unknown", summary.ClimateRiskLevel);        // FU-7: no-data sentinel
        Assert.Equal(0.0, summary.Weather.Temperature);           // null → 0.0 default
    }
}
