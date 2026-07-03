using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
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
///     Unit tests for <see cref="AgronomicStatisticsController"/> route realignment.
///     Template C: controller tests with a fake <see cref="HttpContext"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class AgronomicStatisticsControllerRouteTests
{
    private readonly IAgronomicStatisticsQueryService _queryService = Substitute.For<IAgronomicStatisticsQueryService>();
    private readonly IAgronomicStatisticSeriesQueryService _seriesQueryService = Substitute.For<IAgronomicStatisticSeriesQueryService>();
    private readonly IAgronomicStatisticIngestionService _ingestionService = Substitute.For<IAgronomicStatisticIngestionService>();
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

    private AgronomicStatisticsController CreateController()
    {
        var controller = new AgronomicStatisticsController(
            _queryService,
            _seriesQueryService,
            _ingestionService,
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

    /// <summary>
    ///     Builds an <see cref="AgronomicStatistic"/> using the domain factory.
    /// </summary>
    private static AgronomicStatistic BuildStatistic(long userId, long plotId, DateTimeOffset date, double ndvi)
    {
        return ((Result<AgronomicStatistic, Error>.Success)AgronomicStatistic.Create(
            userId, plotId, date, ndvi, chillPortions: 10.0, chillHours: 5.0, ChillModelState.Empty())).Value;
    }

    /// <summary>
    ///     GIVEN a valid userId and time range
    ///     WHEN POST /api/v1/agronomic-statistics is called (base route)
    ///     THEN the response is 200 OK with an ingestion report.
    /// </summary>
    [Fact]
    public async Task Post_BaseRoute_ValidInputs_ReturnsOk()
    {
        // GIVEN a valid user requesting ingestion
        var report = new AgronomicStatisticsIngestionReport(Ingested: 5, Skipped: 1);
        _ingestionService.Handle(Arg.Any<IngestAgronomicStatisticsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Result<AgronomicStatisticsIngestionReport, Error>.Success(report));

        var controller = CreateController();

        // WHEN POST /api/v1/agronomic-statistics?userId=1
        var result = await controller.IngestAgronomicStatistics(userId: 1, CancellationToken.None);

        // THEN the result is 200 OK with the report
        var ok = Assert.IsType<OkObjectResult>(result);
        var resource = Assert.IsType<AgronomicStatisticsIngestionReportResource>(ok.Value);
        Assert.Equal(5, resource.Ingested);
        Assert.Equal(1, resource.Skipped);
    }

    /// <summary>
    ///     GIVEN statistics exist for a user within a time range
    ///     WHEN GET /api/v1/agronomic-statistics is called
    ///     THEN the response is 200 OK with an ordered array of <see cref="AgronomicStatisticResource"/>.
    /// </summary>
    [Fact]
    public async Task Get_TimeRange_ReturnsOkWithArray()
    {
        // GIVEN statistics for two dates (out of order)
        var stats = new List<AgronomicStatistic>
        {
            BuildStatistic(1, 10, new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), 0.72),
            BuildStatistic(1, 10, new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero), 0.65),
        };
        _queryService.Handle(Arg.Any<GetAgronomicStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<IEnumerable<AgronomicStatistic>, Error>.Success(stats));

        var controller = CreateController();

        // WHEN GET /api/v1/agronomic-statistics?userId=1&timeRange=LAST_30_DAYS
        var result = await controller.GetAgronomicStatistics(
            userId: 1, timeRange: "LAST_30_DAYS", cancellationToken: CancellationToken.None);

        // THEN the result is 200 OK with an array sorted by MeasurementDate
        var ok = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<AgronomicStatisticResource>>(ok.Value).ToList();
        Assert.Equal(2, resources.Count);
        // Ordered by date ascending
        Assert.Equal("2026-06-28", resources[0].MeasurementDate);
        Assert.Equal("2026-07-01", resources[1].MeasurementDate);
    }

    /// <summary>
    ///     GIVEN no statistics exist within the requested time range
    ///     WHEN GET /api/v1/agronomic-statistics is called
    ///     THEN the response is 200 OK with an empty array.
    /// </summary>
    [Fact]
    public async Task Get_EmptyRange_ReturnsOkWithEmptyArray()
    {
        // GIVEN no statistics for this user
        _queryService.Handle(Arg.Any<GetAgronomicStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<IEnumerable<AgronomicStatistic>, Error>.Success([]));

        var controller = CreateController();

        // WHEN GET /api/v1/agronomic-statistics?userId=1&timeRange=LAST_7_DAYS
        var result = await controller.GetAgronomicStatistics(
            userId: 1, timeRange: "LAST_7_DAYS", cancellationToken: CancellationToken.None);

        // THEN the result is 200 OK with an empty enumerable
        var ok = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<AgronomicStatisticResource>>(ok.Value);
        Assert.Empty(resources);
    }

    /// <summary>
    ///     GIVEN an invalid time range string
    ///     WHEN GET /api/v1/agronomic-statistics is called
    ///     THEN the response is 400 Bad Request instead of an unhandled ArgumentException
    ///     (previously Enum.Parse threw, which GlobalExceptionHandlerMiddleware mapped to 500).
    /// </summary>
    [Fact]
    public async Task Get_InvalidTimeRange_ReturnsBadRequest()
    {
        // GIVEN an invalid time range value
        var controller = CreateController();

        // WHEN GET /api/v1/agronomic-statistics?userId=1&timeRange=INVALID_RANGE
        var result = await controller.GetAgronomicStatistics(
            userId: 1, timeRange: "INVALID_RANGE", cancellationToken: CancellationToken.None);

        // THEN the result is 400 Bad Request, not a thrown exception
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    /// <summary>
    ///     GIVEN an invalid time range string
    ///     WHEN GET /api/v1/agronomic-statistics/series is called
    ///     THEN the response is 400 Bad Request (same guard as the base route above).
    /// </summary>
    [Fact]
    public async Task GetSeries_InvalidTimeRange_ReturnsBadRequest()
    {
        // GIVEN an invalid time range value
        var controller = CreateController();

        // WHEN GET /api/v1/agronomic-statistics/series?userId=1&timeRange=INVALID_RANGE
        var result = await controller.GetAgronomicStatisticSeries(
            userId: 1, timeRange: "INVALID_RANGE", cancellationToken: CancellationToken.None);

        // THEN the result is 400 Bad Request, not a thrown exception
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }
}
