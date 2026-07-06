using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Unit tests for <see cref="PlotQueryService"/>.
///     Template B: query service with NSubstitute mocks.
///     Covers all 4 IPlotQueryService.Handle overloads plus the IDOR closure.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class PlotQueryServiceTests
{
    private readonly IPlotRepository _plotRepository = Substitute.For<IPlotRepository>();
    private readonly IIoTDeviceRepository _ioTDeviceRepository = Substitute.For<IIoTDeviceRepository>();
    private readonly IClock _clock = new FakeClock();
    private readonly IAgronomicStatisticRepository _agronomicStatisticRepository = Substitute.For<IAgronomicStatisticRepository>();
    private readonly PlotHealthEvaluator _plotHealthEvaluator = new();
    private readonly PhenologicalRiskEvaluator _phenologicalRiskEvaluator = new();
    private readonly ChillRequirementResolver _chillRequirementResolver;
    private readonly PlotQueryService _sut;

    public PlotQueryServiceTests()
    {
        var policy = new ChillRequirementPolicy(120.5);
        _chillRequirementResolver = new ChillRequirementResolver(policy);
        _sut = new PlotQueryService(
            _plotRepository,
            _ioTDeviceRepository,
            _clock,
            _agronomicStatisticRepository,
            _plotHealthEvaluator,
            _phenologicalRiskEvaluator,
            _chillRequirementResolver);
    }

    /// <summary>
    ///     Builds a <see cref="Plot"/> with a known Id via reflection.
    /// </summary>
    private static Plot BuildPlot(int id, int ownerUserId, string plotName = "Test Plot", bool isActive = true)
    {
        var result = Plot.Create(
            ownerUserId: ownerUserId,
            plotName: plotName,
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

        // Set Id via backing field (EF Core materialisation pattern)
        typeof(Plot).GetProperty(nameof(Plot.Id))!.SetValue(plot, id);

        if (!isActive)
            plot.Deactivate();

        return plot;
    }

    /// <summary>
    ///     GIVEN a valid plot ID owned by the requesting user
    ///     WHEN <see cref="PlotQueryService.Handle(GetPlotByIdQuery, CancellationToken)"/> is called
    ///     THEN the plot is returned as success.
    /// </summary>
    [Fact]
    public async Task Handle_GetPlotById_ValidId_ReturnsPlot()
    {
        // GIVEN a plot owned by user 1
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);

        var query = new GetPlotByIdQuery(PlotId: 10, UserId: 1);

        // WHEN the query is handled
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN the plot is returned
        Assert.True(result.IsSuccess);
        var resource = ((Result<PlotResource, Error>.Success)result).Value;
        Assert.NotNull(resource);
        Assert.Equal(10, resource!.Id);
        Assert.Equal(1, resource.UserId);
    }

    /// <summary>
    ///     GIVEN a non-existent plot ID
    ///     WHEN <see cref="PlotQueryService.Handle(GetPlotByIdQuery, CancellationToken)"/> is called
    ///     THEN <see cref="AgronomicErrors.PlotNotFound"/> is returned.
    /// </summary>
    [Fact]
    public async Task Handle_GetPlotById_InvalidId_ReturnsNotFound()
    {
        // GIVEN a non-existent plot
        _plotRepository.FindByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Plot?)null);

        var query = new GetPlotByIdQuery(PlotId: 999, UserId: 1);

        // WHEN the query is handled
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN PlotNotFound is returned
        Assert.True(result.IsFailure);
        Assert.Equal(AgronomicErrors.PlotNotFound, ((Result<PlotResource, Error>.Failure)result).Error);
    }

    /// <summary>
    ///     GIVEN a plot owned by user A
    ///     WHEN user B requests the plot via GetPlotByIdQuery
    ///     THEN <see cref="AgronomicErrors.PlotOwnership"/> is returned (403) — regression
    ///     guard for the IDOR closure: GetPlotByIdQuery now carries UserId and
    ///     PlotQueryService enforces ownership, matching GetPlotDetailQueryService,
    ///     GetPlotMonitoringSummaryQueryService, and GetPlotWeatherForecastQueryService.
    /// </summary>
    [Fact]
    public async Task Handle_GetPlotById_UserDoesNotOwnPlot_ReturnsOwnershipFailure()
    {
        // GIVEN a plot owned by user 1
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);

        // AND user 2 (not the owner) requests the plot
        var query = new GetPlotByIdQuery(PlotId: 10, UserId: 2);

        // WHEN the query is handled
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN ownership is enforced — the plot is NOT returned
        Assert.True(result.IsFailure);
        Assert.Equal(AgronomicErrors.PlotOwnership, ((Result<PlotResource, Error>.Failure)result).Error);
    }

    /// <summary>
    ///     GIVEN a user with owned plots
    ///     WHEN <see cref="PlotQueryService.Handle(GetPlotsByUserIdQuery, CancellationToken)"/> is called
    ///     THEN only the user's non-deleted plots are returned.
    /// </summary>
    [Fact]
    public async Task Handle_GetPlotsByUserId_ValidUser_ReturnsPlots()
    {
        // GIVEN user 1 owns 2 plots, user 2 owns 1
        var plot1 = BuildPlot(1, ownerUserId: 1, plotName: "Plot A");
        var plot2 = BuildPlot(2, ownerUserId: 1, plotName: "Plot B");
        var plot3 = BuildPlot(3, ownerUserId: 2, plotName: "Plot C");
        _plotRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Plot> { plot1, plot2, plot3 });

        var query = new GetPlotsByUserIdQuery(UserId: 1);

        // WHEN the query is handled
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN only user 1's plots are returned
        Assert.True(result.IsSuccess);
        var plots = ((Result<IEnumerable<PlotResource>, Error>.Success)result).Value!.ToList();
        Assert.Equal(2, plots.Count);
        Assert.All(plots, p => Assert.Equal(1, p.UserId));
    }

    /// <summary>
    ///     GIVEN a user with plots (some with AgroMonitoringCenter, some without)
    ///     WHEN <see cref="PlotQueryService.Handle(GetMyPlotsOverviewQuery, CancellationToken)"/> is called
    ///     THEN RegisteredPlotCount and ClimateLinkedPlotCount are EQUAL (conflation).
    ///     FU-1 regression guard: the overview uses userPlots.Count for BOTH fields.
    ///     Source: PlotQueryService.cs:129 — new MyPlotsOverviewResource(userPlots.Count, totalArea, userPlots.Count, activeDevices, overviewPlots)
    /// </summary>
    [Fact]
    public async Task Handle_GetMyPlotsOverview_ConflatesRegisteredAndClimateLinkedPlotCount_DocumentedBehavior()
    {
        // GIVEN 3 user plots, 0 with AgroMonitoringCenter
        var plot1 = BuildPlot(1, ownerUserId: 1);
        var plot2 = BuildPlot(2, ownerUserId: 1);
        var plot3 = BuildPlot(3, ownerUserId: 1);
        _plotRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Plot> { plot1, plot2, plot3 });
        _ioTDeviceRepository.FindAllByPlotIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IoTDevice>());
        _agronomicStatisticRepository.FindLatestByPlotIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns((AgronomicStatistic?)null);

        var query = new GetMyPlotsOverviewQuery(UserId: 1);

        // WHEN the overview is fetched
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN both counts are equal (conflation — FU-1)
        Assert.True(result.IsSuccess);
        var overview = ((Result<MyPlotsOverviewResource, Error>.Success)result).Value!;
        Assert.Equal(3, overview.RegisteredPlotCount);
        Assert.Equal(3, overview.ClimateLinkedPlotCount); // FU-1: conflated with RegisteredPlotCount
    }

    /// <summary>
    ///     GIVEN a user with plots
    ///     WHEN <see cref="PlotQueryService.Handle(GetPlotsWithCurrentImageryQuery, CancellationToken)"/> is called
    ///     THEN plots with current imagery are returned.
    /// </summary>
    [Fact]
    public async Task Handle_GetPlotsWithCurrentImagery_ValidUser_ReturnsWithImagery()
    {
        // GIVEN user 1 owns 1 plot
        var plot = BuildPlot(1, ownerUserId: 1);
        _plotRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Plot> { plot });
        _agronomicStatisticRepository.FindLatestByPlotIdAsync(1, Arg.Any<CancellationToken>())
            .Returns((AgronomicStatistic?)null);

        var query = new GetPlotsWithCurrentImageryQuery(UserId: 1);

        // WHEN the query is handled
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN the plot with imagery is returned
        Assert.True(result.IsSuccess);
        var plots = ((Result<IEnumerable<PlotWithCurrentImageryResource>, Error>.Success)result).Value!.ToList();
        Assert.Single(plots);
        Assert.NotNull(plots[0].CurrentImagery);
    }

    /// <summary>
    ///     GIVEN a user with a plot that has a non-null latest AgronomicStatistic
    ///     WHEN <see cref="PlotQueryService.Handle(GetPlotsWithCurrentImageryQuery, CancellationToken)"/> is called
    ///     THEN CloudPercentage is ALWAYS 0.0 regardless of statistic data.
    ///     FU-5 regression guard: CloudPercentage defaults to 0.0 until satellite imagery
    ///     metadata is wired separately.
    ///     Source: PlotQueryService.cs:170 — var cloudPercentage = 0.0;
    /// </summary>
    [Fact]
    public async Task Handle_GetPlotsWithCurrentImagery_CloudPercentage_AlwaysZero_DocumentedBehavior()
    {
        // GIVEN user 1 owns 1 plot with a non-null statistic
        var plot = BuildPlot(1, ownerUserId: 1);
        _plotRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Plot> { plot });
        var statisticResult = AgronomicStatistic.Create(
            userId: 1, plotId: 1,
            measurementDate: new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero),
            ndviValue: 0.75, chillPortions: 100.0, chillHours: 50.0,
            chillModelState: ChillModelState.Empty());
        var statistic = ((Result<AgronomicStatistic, Error>.Success)statisticResult).Value;
        _agronomicStatisticRepository.FindLatestByPlotIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(statistic);

        var query = new GetPlotsWithCurrentImageryQuery(UserId: 1);

        // WHEN the query is handled
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN CloudPercentage is 0.0 (FU-5: documented default, not derived from statistic)
        Assert.True(result.IsSuccess);
        var plots = ((Result<IEnumerable<PlotWithCurrentImageryResource>, Error>.Success)result).Value!.ToList();
        Assert.Equal(0.0, plots[0].CurrentImagery.CloudPercentage);
    }
}
