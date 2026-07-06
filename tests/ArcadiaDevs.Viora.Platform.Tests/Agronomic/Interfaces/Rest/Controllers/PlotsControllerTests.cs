using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     Unit tests for <see cref="PlotsController.GetPlotById"/>'s <c>?view=</c> dispatch.
///     Template C: controller tests with a fake <see cref="HttpContext"/>, following the
///     same shape as <c>AgronomicStatisticsControllerRouteTests</c>.
///     Closes a coverage gap: existing "IDOR closure" tests exercise the underlying query
///     services directly with mocks and never go through the controller/assembler boundary,
///     so the ?view= routing and the assembler's PlotOwnership -&gt; 403 mapping were untested
///     at the HTTP boundary.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class PlotsControllerTests
{
    private readonly IPlotCommandService _plotCommandService = Substitute.For<IPlotCommandService>();
    private readonly IPlotQueryService _plotQueryService = Substitute.For<IPlotQueryService>();
    private readonly IGetPlotDetailQueryService _getPlotDetailQueryService = Substitute.For<IGetPlotDetailQueryService>();
    private readonly IGetPlotMonitoringSummaryQueryService _getPlotMonitoringSummaryQueryService = Substitute.For<IGetPlotMonitoringSummaryQueryService>();
    private readonly IGetPlotWeatherForecastQueryService _getPlotWeatherForecastQueryService = Substitute.For<IGetPlotWeatherForecastQueryService>();
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer = StubLocalizer();
    private readonly ProblemDetailsFactory _problemDetailsFactory = new TestProblemDetailsFactory();
    private readonly IClock _clock = new FakeClock();

    private static IStringLocalizer<ErrorMessages> StubLocalizer()
    {
        var localizer = Substitute.For<IStringLocalizer<ErrorMessages>>();
        localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), value: call.ArgAt<string>(0)));
        return localizer;
    }

    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new()
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance,
            };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance,
            };
    }

    private PlotsController CreateController()
    {
        var controller = new PlotsController(
            _plotCommandService,
            _plotQueryService,
            _getPlotDetailQueryService,
            _getPlotMonitoringSummaryQueryService,
            _getPlotWeatherForecastQueryService,
            _errorLocalizer,
            _problemDetailsFactory,
            _clock);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };
        return controller;
    }

    private static PlotResource BuildPlotResource(long plotId, long userId) =>
        new(
            Id: plotId,
            UserId: userId,
            Name: "Plot",
            PolygonCoordinates: new List<IEnumerable<double>>(),
            AreaSize: 1m,
            LastUpdate: DateTimeOffset.UtcNow,
            CropType: "Coffee",
            Variety: "Arabica",
            Location: "Lima",
            Campaign: "2026",
            Notes: "",
            State: "Active");

    private static PlotDetailResource BuildDetailResource(long plotId, long userId) =>
        new(
            Id: plotId,
            UserId: userId,
            Name: "Plot",
            Location: "Lima",
            Campaign: "2026",
            CropType: "Coffee",
            Variety: "Arabica",
            Notes: "",
            PolygonCoordinates: new List<IEnumerable<double>>(),
            AreaSizeHectares: 1m,
            BoundaryPointCount: 5,
            BoundaryStatus: "Valid",
            RegisteredAt: DateTimeOffset.UtcNow,
            LastConfigurationUpdateAt: DateTimeOffset.UtcNow,
            MonitoringLinks: new MonitoringLinksResource("", "", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            Iot: new IoTDetailResource("None", 0, 0, DateTimeOffset.UtcNow),
            Devices: new List<PlotDeviceResource>(),
            RecentConfigurationActivity: new List<RecentConfigurationActivityResource>());

    private static PlotMonitoringSummaryResource BuildMonitoringResource(long plotId, long userId) =>
        new(
            PlotId: plotId,
            UserId: userId,
            PlotName: "Plot",
            CurrentNdvi: 0.7,
            NdviTrend: new NdviTrendResource("stable", 0.0, new List<NdviTrendSeriesResource>()),
            ChillPortions: 50.0,
            ChillPortionsWeeklyDelta: 1.0,
            ChillRequirementPortions: 100.0,
            ChillRequirementSource: "Default",
            ChillMetricModel: "Utah",
            ChillUnit: "Portions",
            HealthStatus: "Healthy",
            PhenologicalRisk: "Low",
            YieldForecastTonnes: 1.0,
            Weather: new WeatherSummaryResource("Sunny", "2026-07-01", "Low", 20.0),
            ClimateRiskLevel: "Low",
            LastUpdatedAt: DateTimeOffset.UtcNow,
            Recommendations: new List<RecommendationResource>(),
            ClimateSource: new ExternalSourceResource("AgroMonitoring", "Online", DateTimeOffset.UtcNow, 60),
            NdviSource: new ExternalSourceResource("AgroMonitoring", "Online", DateTimeOffset.UtcNow, 60));

    private static PlotWeatherForecastResource BuildWeatherResource(long plotId, long userId) =>
        new(
            PlotId: plotId,
            UserId: userId,
            PlotName: "Plot",
            GeneratedAt: DateTimeOffset.UtcNow,
            Hourly: new List<HourlyForecastResource>(),
            Daily: new List<DailyForecastResource>(),
            ThermalAnomalyCelsius: 0.0,
            OverallRisk: "Low",
            Warnings: new List<WeatherWarningResource>(),
            Source: new ExternalSourceResource("AgroMonitoring", "Online", DateTimeOffset.UtcNow, 60));

    /// <summary>
    ///     GIVEN <c>?view=detail</c>
    ///     WHEN <see cref="PlotsController.GetPlotById"/> is called
    ///     THEN the request is dispatched to <see cref="IGetPlotDetailQueryService"/> only,
    ///     with a query built from the route/query parameters, and the result is 200 OK.
    /// </summary>
    [Fact]
    public async Task GetPlotById_ViewDetail_DispatchesToDetailQueryServiceOnly()
    {
        // GIVEN the detail query service returns a resource
        var resource = BuildDetailResource(10, 1);
        _getPlotDetailQueryService
            .Handle(Arg.Any<GetPlotDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<PlotDetailResource, Error>.Success(resource));

        var controller = CreateController();

        // WHEN GET /api/v1/plots/10?view=detail&userId=1
        var result = await controller.GetPlotById(plotId: 10, view: PlotView.Detail, userId: 1, CancellationToken.None);

        // THEN 200 OK with the detail resource, and ONLY the detail service was called
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(resource, ok.Value);

        await _getPlotDetailQueryService.Received(1).Handle(
            Arg.Is<GetPlotDetailQuery>(q => q.PlotId == 10 && q.UserId == 1),
            Arg.Any<CancellationToken>());
        await _getPlotMonitoringSummaryQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotMonitoringSummaryQuery>(), Arg.Any<CancellationToken>());
        await _getPlotWeatherForecastQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotWeatherForecastQuery>(), Arg.Any<CancellationToken>());
        await _plotQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotByIdQuery>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN <c>?view=monitoring</c>
    ///     WHEN <see cref="PlotsController.GetPlotById"/> is called
    ///     THEN the request is dispatched to <see cref="IGetPlotMonitoringSummaryQueryService"/> only.
    /// </summary>
    [Fact]
    public async Task GetPlotById_ViewMonitoring_DispatchesToMonitoringQueryServiceOnly()
    {
        // GIVEN the monitoring query service returns a resource
        var resource = BuildMonitoringResource(10, 1);
        _getPlotMonitoringSummaryQueryService
            .Handle(Arg.Any<GetPlotMonitoringSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<PlotMonitoringSummaryResource, Error>.Success(resource));

        var controller = CreateController();

        // WHEN GET /api/v1/plots/10?view=monitoring&userId=1
        var result = await controller.GetPlotById(plotId: 10, view: PlotView.Monitoring, userId: 1, CancellationToken.None);

        // THEN 200 OK with the monitoring resource, and ONLY the monitoring service was called
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(resource, ok.Value);

        await _getPlotMonitoringSummaryQueryService.Received(1).Handle(
            Arg.Is<GetPlotMonitoringSummaryQuery>(q => q.PlotId == 10 && q.UserId == 1),
            Arg.Any<CancellationToken>());
        await _getPlotDetailQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotDetailQuery>(), Arg.Any<CancellationToken>());
        await _getPlotWeatherForecastQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotWeatherForecastQuery>(), Arg.Any<CancellationToken>());
        await _plotQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotByIdQuery>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN <c>?view=weather</c>
    ///     WHEN <see cref="PlotsController.GetPlotById"/> is called
    ///     THEN the request is dispatched to <see cref="IGetPlotWeatherForecastQueryService"/> only.
    /// </summary>
    [Fact]
    public async Task GetPlotById_ViewWeather_DispatchesToWeatherQueryServiceOnly()
    {
        // GIVEN the weather query service returns a resource
        var resource = BuildWeatherResource(10, 1);
        _getPlotWeatherForecastQueryService
            .Handle(Arg.Any<GetPlotWeatherForecastQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<PlotWeatherForecastResource, Error>.Success(resource));

        var controller = CreateController();

        // WHEN GET /api/v1/plots/10?view=weather&userId=1
        var result = await controller.GetPlotById(plotId: 10, view: PlotView.Weather, userId: 1, CancellationToken.None);

        // THEN 200 OK with the weather resource, and ONLY the weather service was called
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(resource, ok.Value);

        await _getPlotWeatherForecastQueryService.Received(1).Handle(
            Arg.Is<GetPlotWeatherForecastQuery>(q => q.PlotId == 10 && q.UserId == 1),
            Arg.Any<CancellationToken>());
        await _getPlotDetailQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotDetailQuery>(), Arg.Any<CancellationToken>());
        await _getPlotMonitoringSummaryQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotMonitoringSummaryQuery>(), Arg.Any<CancellationToken>());
        await _plotQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotByIdQuery>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN no <c>?view=</c> query parameter (null)
    ///     WHEN <see cref="PlotsController.GetPlotById"/> is called
    ///     THEN the request is dispatched to the plain <see cref="IPlotQueryService"/>
    ///     (the "null" case in the switch), preserving the legacy plain-plot-lookup route.
    /// </summary>
    [Fact]
    public async Task GetPlotById_ViewNull_DispatchesToPlainPlotQueryServiceOnly()
    {
        // GIVEN the plain plot query service returns a resource
        var resource = BuildPlotResource(10, 1);
        _plotQueryService
            .Handle(Arg.Any<GetPlotByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<PlotResource, Error>.Success(resource));

        var controller = CreateController();

        // WHEN GET /api/v1/plots/10?userId=1 (no view)
        var result = await controller.GetPlotById(plotId: 10, view: null, userId: 1, CancellationToken.None);

        // THEN 200 OK with the plain plot resource, and ONLY the plain query service was called
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(resource, ok.Value);

        await _plotQueryService.Received(1).Handle(
            Arg.Is<GetPlotByIdQuery>(q => q.PlotId == 10),
            Arg.Any<CancellationToken>());
        await _getPlotDetailQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotDetailQuery>(), Arg.Any<CancellationToken>());
        await _getPlotMonitoringSummaryQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotMonitoringSummaryQuery>(), Arg.Any<CancellationToken>());
        await _getPlotWeatherForecastQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotWeatherForecastQuery>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN <c>?view=overview</c> — a value valid for <see cref="PlotView"/> as an enum,
    ///     but NOT one of the cases the <c>GetPlotById</c> switch handles (Detail/Monitoring/
    ///     Weather/null); "overview" is only meaningful on the list endpoint (<c>GetUserPlots</c>).
    ///     WHEN <see cref="PlotsController.GetPlotById"/> is called
    ///     THEN it falls through to the switch's <c>default</c> arm and returns 400 Bad Request,
    ///     without calling any query service.
    /// </summary>
    [Fact]
    public async Task GetPlotById_UnrecognizedViewForThisEndpoint_ReturnsBadRequest()
    {
        var controller = CreateController();

        // WHEN GET /api/v1/plots/10?view=overview&userId=1
        var result = await controller.GetPlotById(plotId: 10, view: PlotView.Overview, userId: 1, CancellationToken.None);

        // THEN 400 Bad Request from the switch's default arm
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        await _getPlotDetailQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotDetailQuery>(), Arg.Any<CancellationToken>());
        await _getPlotMonitoringSummaryQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotMonitoringSummaryQuery>(), Arg.Any<CancellationToken>());
        await _getPlotWeatherForecastQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotWeatherForecastQuery>(), Arg.Any<CancellationToken>());
        await _plotQueryService.DidNotReceive()
            .Handle(Arg.Any<GetPlotByIdQuery>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN a non-owner request (the mocked query service returns
    ///     <see cref="AgronomicErrors.PlotOwnership"/>, simulating IDOR)
    ///     WHEN <see cref="PlotsController.GetPlotById"/> is called with <c>?view=detail</c>
    ///     THEN the REAL <see cref="ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform.AgronomicActionResultAssembler"/>
    ///     (not a mock) maps the failure to HTTP 403, proving the assembler wiring at the
    ///     controller boundary — not just the query service's own failure result.
    /// </summary>
    [Fact]
    public async Task GetPlotById_NonOwner_MapsToForbiddenThroughRealAssembler()
    {
        // GIVEN the detail query service reports a PlotOwnership failure (simulated IDOR)
        _getPlotDetailQueryService
            .Handle(Arg.Any<GetPlotDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<PlotDetailResource, Error>.Failure(AgronomicErrors.PlotOwnership));

        var controller = CreateController();

        // WHEN a non-owner requests the detail view
        var result = await controller.GetPlotById(plotId: 10, view: PlotView.Detail, userId: 2, CancellationToken.None);

        // THEN the REAL AgronomicActionResultAssembler maps PlotOwnership -> 403
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(AgronomicErrors.PlotOwnership.Code, problemDetails.Title);
    }
}
