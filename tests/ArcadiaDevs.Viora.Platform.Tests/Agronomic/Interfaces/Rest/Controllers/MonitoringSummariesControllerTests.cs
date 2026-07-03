using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     Unit tests for <see cref="MonitoringSummariesController"/>.
///     Template C: controller tests with a fake <see cref="HttpContext"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class MonitoringSummariesControllerTests
{
    private readonly IMonitoringSummaryQueryService _queryService = Substitute.For<IMonitoringSummaryQueryService>();
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer = StubLocalizer();
    private readonly ProblemDetailsFactory _problemDetailsFactory = new TestProblemDetailsFactory();

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

    private MonitoringSummariesController CreateController()
    {
        var controller = new MonitoringSummariesController(
            _queryService,
            _errorLocalizer,
            _problemDetailsFactory);

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
    ///     Builds a valid <see cref="MonitoringSummaryResource"/> for testing.
    /// </summary>
    private static MonitoringSummaryResource BuildSummaryResource()
    {
        return new MonitoringSummaryResource
        {
            TotalPlots = 3,
            TotalDevices = 5,
            ActiveDevices = 4,
            InactiveDevices = 1,
            MaintenanceDevices = 0,
            AveragePlotArea = 2.5m,
            DeviceHealthPercentage = 80m,
            GeneralHealthStatus = "Healthy",
            AverageNdvi = 0.72m,
            ColdAccumulationIndex = 45.0m,
            YieldProjection = 5200m,
            CurrentTemperature = 22.5m,
            WeatherStatus = "Clear",
            ClimateRiskLevel = "Low"
        };
    }

    /// <summary>
    ///     GIVEN a valid userId
    ///     WHEN GET /api/v1/monitoring-summaries/current is called
    ///     THEN the response is 200 OK with the monitoring summary resource.
    /// </summary>
    [Fact]
    public async Task Get_ValidInput_ReturnsOk()
    {
        // GIVEN a valid user with monitoring data
        var summary = BuildSummaryResource();
        _queryService.Handle(Arg.Any<GetCurrentMonitoringSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<MonitoringSummaryResource, Error>.Success(summary));

        var controller = CreateController();

        // WHEN GET /api/v1/monitoring-summaries/current?userId=1
        var result = await controller.GetCurrent(userId: 1, CancellationToken.None);

        // THEN the result is 200 OK with the summary resource
        var ok = Assert.IsType<OkObjectResult>(result);
        var resource = Assert.IsType<MonitoringSummaryResource>(ok.Value);
        Assert.Equal(3, resource.TotalPlots);
        Assert.Equal(5, resource.TotalDevices);
        Assert.Equal(80m, resource.DeviceHealthPercentage);
    }

    /// <summary>
    ///     GIVEN a valid userId where the query service returns a failure
    ///     WHEN GET /api/v1/monitoring-summaries/current is called
    ///     THEN the response is a problem-details error result.
    /// </summary>
    [Fact]
    public async Task Get_ServiceFailure_ReturnsProblemDetails()
    {
        // GIVEN the query service fails (e.g., weather unavailable)
        _queryService.Handle(Arg.Any<GetCurrentMonitoringSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<MonitoringSummaryResource, Error>.Failure(
                new Error("Agronomic.WeatherUnavailable", "Weather data unavailable")));

        var controller = CreateController();

        // WHEN GET /api/v1/monitoring-summaries/current?userId=1
        var result = await controller.GetCurrent(userId: 1, CancellationToken.None);

        // THEN the result is a problem details error (status mapped from error code)
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.NotNull(objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    /// <summary>
    ///     GIVEN an invalid userId (zero or negative)
    ///     WHEN GET /api/v1/monitoring-summaries/current is called
    ///     THEN the query constructor throws <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public async Task Get_InvalidUserId_ThrowsArgumentException()
    {
        // GIVEN an invalid userId
        var controller = CreateController();

        // WHEN GET /api/v1/monitoring-summaries/current?userId=0
        // THEN the query constructor throws ArgumentException
        await Assert.ThrowsAsync<ArgumentException>(() =>
            controller.GetCurrent(userId: 0, CancellationToken.None));
    }
}
