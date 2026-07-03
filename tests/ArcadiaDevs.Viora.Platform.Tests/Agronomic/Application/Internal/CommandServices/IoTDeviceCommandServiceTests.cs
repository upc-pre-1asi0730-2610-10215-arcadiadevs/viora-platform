using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Application.Internal.CommandServices;

/// <summary>
///     Unit tests for <see cref="IoTDeviceCommandService"/> command handlers.
///     Template B: command service with NSubstitute mocks.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class IoTDeviceCommandServiceTests
{
    private readonly IIoTDeviceRepository _ioTDeviceRepository = Substitute.For<IIoTDeviceRepository>();
    private readonly IPlotRepository _plotRepository = Substitute.For<IPlotRepository>();
    private readonly IActivationCodeCatalog _catalog = Substitute.For<IActivationCodeCatalog>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = new FakeClock();
    private readonly IoTDeviceCommandService _sut;

    public IoTDeviceCommandServiceTests()
    {
        _sut = new IoTDeviceCommandService(
            _ioTDeviceRepository,
            _plotRepository,
            _catalog,
            _unitOfWork,
            _clock);
    }

    /// <summary>
    ///     Builds a <see cref="Plot"/> with a known Id via reflection.
    /// </summary>
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

        // Set Id via backing field (EF Core materialisation pattern)
        typeof(Plot).GetProperty(nameof(Plot.Id))!.SetValue(plot, id);
        return plot;
    }

    /// <summary>
    ///     Builds a <see cref="Plot"/> marked as deleted.
    /// </summary>
    private static Plot BuildDeletedPlot(int id, int ownerUserId)
    {
        var plot = BuildPlot(id, ownerUserId);
        plot.Deactivate(); // sets IsDeleted = true
        return plot;
    }

    /// <summary>
    ///     Builds an <see cref="IoTDevice"/> with a known Id and activation code.
    /// </summary>
    private static IoTDevice BuildDevice(long id, long plotId, string deviceName, ActivationCode? code = null)
    {
        var claimResult = IoTDevice.Claim(
            plotId,
            deviceName,
            code ?? new ActivationCode("VIORA-WS01-ABCD"),
            new FakeClock());
        var device = ((Result<IoTDevice, Error>.Success)claimResult).Value;

        // Set Id via backing field
        typeof(IoTDevice).GetProperty(nameof(IoTDevice.Id))!.SetValue(device, id);
        return device;
    }

    /// <summary>
    ///     GIVEN a valid plot and an unclaimed activation code
    ///     WHEN <see cref="IoTDeviceCommandService.Handle(CreateIoTDeviceCommand)"/> is called
    ///     THEN the device is persisted and returned as success.
    /// </summary>
    [Fact]
    public async Task Handle_CreateIoTDevice_ValidInput_ReturnsSuccess()
    {
        // GIVEN a valid plot
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(plot);

        // AND an unclaimed activation code
        var code = new ActivationCode("VIORA-WS01-ABCD");
        _catalog.IsIssued(code).Returns(true);
        _ioTDeviceRepository.ExistsByActivationCodeAsync(code, Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateIoTDeviceCommand(
            plotId: 10,
            deviceName: "Weather Station Alpha",
            activationCode: "VIORA-WS01-ABCD");

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the device is persisted and returned
        Assert.True(result.IsSuccess);
        var device = ((Result<IoTDevice, Error>.Success)result).Value;
        Assert.NotNull(device);
        Assert.Equal(10, device!.PlotId);
        Assert.Equal("Weather Station Alpha", device.DeviceName);
        Assert.Equal(IoTDeviceStatus.Pending, device.Status);
        Assert.Equal(code, device.ActivationCode);

        await _ioTDeviceRepository.Received(1).AddAsync(device, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN an activation code that was already claimed by another device
    ///     WHEN <see cref="IoTDeviceCommandService.Handle(CreateIoTDeviceCommand)"/> is called
    ///     THEN <see cref="AgronomicErrors.ActivationCodeAlreadyClaimed"/> is returned.
    /// </summary>
    [Fact]
    public async Task Handle_CreateIoTDevice_DuplicateActivationCode_ReturnsFailure()
    {
        // GIVEN a valid plot
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(plot);

        // AND the activation code is already claimed
        var code = new ActivationCode("VIORA-WS01-ABCD");
        _catalog.IsIssued(code).Returns(true);
        _ioTDeviceRepository.ExistsByActivationCodeAsync(code, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateIoTDeviceCommand(
            plotId: 10,
            deviceName: "Weather Station Alpha",
            activationCode: "VIORA-WS01-ABCD");

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the result is a failure with ActivationCodeAlreadyClaimed
        Assert.True(result.IsFailure);
        Assert.Equal(AgronomicErrors.ActivationCodeAlreadyClaimed, ((Result<IoTDevice, Error>.Failure)result).Error);

        // AND no persistence occurs
        await _ioTDeviceRepository.DidNotReceive().AddAsync(Arg.Any<IoTDevice>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN a plot that does not exist
    ///     WHEN <see cref="IoTDeviceCommandService.Handle(CreateIoTDeviceCommand)"/> is called
    ///     THEN <see cref="AgronomicErrors.PlotNotFound"/> is returned.
    /// </summary>
    [Fact]
    public async Task Handle_CreateIoTDevice_PlotNotFound_ReturnsFailure()
    {
        // GIVEN a non-existent plot
        _plotRepository.FindByIdAsync(999, Arg.Any<CancellationToken>())
            .Returns((Plot?)null);

        var command = new CreateIoTDeviceCommand(
            plotId: 999,
            deviceName: "Weather Station Alpha",
            activationCode: "VIORA-WS01-ABCD");

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the result is a failure with PlotNotFound
        Assert.True(result.IsFailure);
        Assert.Equal(AgronomicErrors.PlotNotFound, ((Result<IoTDevice, Error>.Failure)result).Error);

        // AND the catalog is never checked
        _catalog.DidNotReceive().IsIssued(Arg.Any<ActivationCode>());
    }

    /// <summary>
    ///     GIVEN a valid plot and an existing device
    ///     WHEN <see cref="IoTDeviceCommandService.Handle(UpdateIoTDeviceCommand)"/> is called
    ///     THEN the device is updated and returned as success.
    /// </summary>
    [Fact]
    public async Task Handle_UpdateIoTDevice_ValidInput_ReturnsSuccess()
    {
        // GIVEN a valid plot
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(plot);

        // AND an existing device
        var device = BuildDevice(id: 5, plotId: 10, deviceName: "Old Name");
        _ioTDeviceRepository.FindByIdAndPlotIdAsync(5, 10)
            .Returns(device);
        _ioTDeviceRepository.SaveAsync(Arg.Any<IoTDevice>())
            .Returns(device);

        var command = new UpdateIoTDeviceCommand(
            plotId: 10,
            deviceId: 5,
            deviceName: "Updated Name",
            status: IoTDeviceStatus.Active);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the device is updated and persisted
        Assert.True(result.IsSuccess);
        var updatedDevice = ((Result<IoTDevice, Error>.Success)result).Value;
        Assert.NotNull(updatedDevice);
        Assert.Equal("Updated Name", updatedDevice!.DeviceName);
        Assert.Equal(IoTDeviceStatus.Active, updatedDevice.Status);

        await _ioTDeviceRepository.Received(1).SaveAsync(device);
    }
}
