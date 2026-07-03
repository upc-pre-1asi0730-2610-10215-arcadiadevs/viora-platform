using ArcadiaDevs.Viora.Platform.Agronomic.Application.ReadModels;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
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
///     Unit tests for <see cref="IoTDevicesQueryController"/>.
///     Template C: controller tests with a fake <see cref="HttpContext"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class IoTDevicesQueryControllerTests
{
    private readonly IIoTDeviceQueryService _queryService = Substitute.For<IIoTDeviceQueryService>();
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

    private IoTDevicesQueryController CreateController()
    {
        var controller = new IoTDevicesQueryController(
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
    ///     Builds an <see cref="IoTDeviceReadout"/> with the specified device ID and plot ID.
    /// </summary>
    private static IoTDeviceReadout BuildReadout(long deviceId, long plotId, string deviceName)
    {
        var claimResult = IoTDevice.Claim(
            plotId,
            deviceName,
            new ActivationCode("VIORA-WS01-ABCD"),
            new FakeClock());
        var device = ((Result<IoTDevice, Error>.Success)claimResult).Value;

        // Set Id via backing field (EF Core materialisation pattern)
        typeof(IoTDevice).GetProperty(nameof(IoTDevice.Id))!.SetValue(device, deviceId);

        var readings = new SensorReadings(
            SoilMoisture: 45,
            SoilTemperature: 22.5,
            LeafHumidity: 68,
            CapturedAt: new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero));

        return new IoTDeviceReadout(device, readings, GeneralHealthStatus.Healthy);
    }

    /// <summary>
    ///     GIVEN a user owns plots with IoT devices
    ///     WHEN <see cref="IoTDevicesQueryController.GetIoTDevicesByUserId"/> is called
    ///     THEN the response contains all device resources mapped from readouts.
    /// </summary>
    [Fact]
    public async Task GetIoTDevices_ByUserId_ReturnsOkWithDevices()
    {
        // GIVEN a user owns plots with IoT devices
        var readouts = new List<IoTDeviceReadout>
        {
            BuildReadout(deviceId: 1, plotId: 10, deviceName: "Soil Probe A"),
            BuildReadout(deviceId: 2, plotId: 10, deviceName: "Weather Station B"),
        };
        _queryService.Handle(Arg.Any<GetIoTDevicesByUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<IEnumerable<IoTDeviceReadout>, Error>.Success(readouts));

        var controller = CreateController();

        // WHEN the controller handles GET /api/v1/iot-devices?userId=1
        var result = await controller.GetIoTDevicesByUserId(userId: 1, CancellationToken.None);

        // THEN the result is 200 OK with the device resources
        var ok = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<IoTDeviceResource>>(ok.Value).ToList();
        Assert.Equal(2, resources.Count);
        Assert.Equal("Soil Probe A", resources[0].DeviceName);
        Assert.Equal("Weather Station B", resources[1].DeviceName);
    }

    /// <summary>
    ///     GIVEN a user owns no active plots (or has no devices)
    ///     WHEN <see cref="IoTDevicesQueryController.GetIoTDevicesByUserId"/> is called
    ///     THEN an empty array is returned.
    /// </summary>
    [Fact]
    public async Task GetIoTDevices_NoDevices_ReturnsOkWithEmptyList()
    {
        // GIVEN no devices exist for this user
        _queryService.Handle(Arg.Any<GetIoTDevicesByUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<IEnumerable<IoTDeviceReadout>, Error>.Success([]));

        var controller = CreateController();

        // WHEN the controller handles GET /api/v1/iot-devices?userId=1
        var result = await controller.GetIoTDevicesByUserId(userId: 1, CancellationToken.None);

        // THEN the result is 200 OK with an empty enumerable
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var resources = Assert.IsAssignableFrom<IEnumerable<IoTDeviceResource>>(ok.Value);
        Assert.Empty(resources);
    }

    /// <summary>
    ///     GIVEN a user owns plots with devices at different plot IDs
    ///     WHEN <see cref="IoTDevicesQueryController.GetIoTDevicesByUserId"/> is called
    ///     THEN the response contains all devices across all plots with correct resource mapping.
    /// </summary>
    [Fact]
    public async Task GetIoTDevices_ReturnsCorrectIoTDeviceResources()
    {
        // GIVEN devices across multiple plots with different statuses
        var readouts = new List<IoTDeviceReadout>
        {
            BuildReadout(deviceId: 1, plotId: 10, deviceName: "Probe Alpha"),
            BuildReadout(deviceId: 2, plotId: 20, deviceName: "Station Beta"),
        };
        _queryService.Handle(Arg.Any<GetIoTDevicesByUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Result<IEnumerable<IoTDeviceReadout>, Error>.Success(readouts));

        var controller = CreateController();

        // WHEN the controller handles GET /api/v1/iot-devices?userId=1
        var result = await controller.GetIoTDevicesByUserId(userId: 1, CancellationToken.None);

        // THEN each resource maps the device fields correctly
        var ok = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<IoTDeviceResource>>(ok.Value).ToList();

        Assert.Equal(1, resources[0].Id);
        Assert.Equal(10, resources[0].PlotId);
        Assert.Equal("Probe Alpha", resources[0].DeviceName);
        Assert.Equal(IoTDeviceStatus.Pending, resources[0].Status);

        Assert.Equal(2, resources[1].Id);
        Assert.Equal(20, resources[1].PlotId);
        Assert.Equal("Station Beta", resources[1].DeviceName);
        Assert.Equal(IoTDeviceStatus.Pending, resources[1].Status);
    }
}
