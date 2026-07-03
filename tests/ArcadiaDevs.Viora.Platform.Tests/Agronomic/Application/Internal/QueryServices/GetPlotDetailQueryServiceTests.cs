using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Unit tests for <see cref="GetPlotDetailQueryService"/>.
///     Template B: query service with NSubstitute mocks.
///     Covers ownership check (IDOR closure) and FU-3 fabrication assertions.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class GetPlotDetailQueryServiceTests
{
    private readonly IPlotRepository _plotRepository = Substitute.For<IPlotRepository>();
    private readonly IIoTDeviceRepository _ioTDeviceRepository = Substitute.For<IIoTDeviceRepository>();
    private readonly IClock _clock = new FakeClock();
    private readonly GetPlotDetailQueryService _sut;

    public GetPlotDetailQueryServiceTests()
    {
        _sut = new GetPlotDetailQueryService(_plotRepository, _ioTDeviceRepository, _clock);
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

    /// <summary>
    ///     GIVEN a plot owned by user 1 with no IoT devices
    ///     WHEN <see cref="GetPlotDetailQueryService.Handle(GetPlotDetailQuery, CancellationToken)"/> is called by the owner
    ///     THEN the detail resource is returned with empty devices.
    /// </summary>
    [Fact]
    public async Task Handle_OwnerReturnsDetail()
    {
        // GIVEN a plot owned by user 1
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);
        _ioTDeviceRepository.FindAllByPlotIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IoTDevice>());

        var query = new GetPlotDetailQuery(PlotId: 10, UserId: 1);

        // WHEN the query is handled
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN the detail is returned
        Assert.True(result.IsSuccess);
        var detail = ((Result<PlotDetailResource, Error>.Success)result).Value!;
        Assert.Equal(10, detail.Id);
        Assert.Equal(1, detail.UserId);
    }

    /// <summary>
    ///     GIVEN a plot owned by user 1
    ///     WHEN user 2 requests the detail
    ///     THEN <see cref="AgronomicErrors.PlotOwnership"/> is returned (IDOR closure).
    /// </summary>
    [Fact]
    public async Task Handle_NonOwner_ReturnsForbidden()
    {
        // GIVEN a plot owned by user 1
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);

        // WHEN user 2 requests the detail — SECURITY CRITICAL: IDOR closure test
        var query = new GetPlotDetailQuery(PlotId: 10, UserId: 2);
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN PlotOwnership error is returned (403/404 equivalent)
        Assert.True(result.IsFailure);
        Assert.Equal(AgronomicErrors.PlotOwnership, ((Result<PlotDetailResource, Error>.Failure)result).Error);
    }

    /// <summary>
    ///     GIVEN any plot state
    ///     WHEN <see cref="GetPlotDetailQueryService.Handle(GetPlotDetailQuery, CancellationToken)"/> is called
    ///     THEN BoundaryStatus is ALWAYS "Valid" (FU-3 documented fabrication).
    ///     Source: GetPlotDetailQueryService.cs:65 — "Valid"
    /// </summary>
    [Fact]
    public async Task Handle_BoundaryStatus_AlwaysValid_DocumentedBehavior()
    {
        // GIVEN any valid plot
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);
        _ioTDeviceRepository.FindAllByPlotIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IoTDevice>());

        var query = new GetPlotDetailQuery(PlotId: 10, UserId: 1);

        // WHEN the detail is fetched
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN BoundaryStatus is "Valid" regardless of actual boundary state (FU-3)
        Assert.True(result.IsSuccess);
        var detail = ((Result<PlotDetailResource, Error>.Success)result).Value!;
        Assert.Equal("Valid", detail.BoundaryStatus); // FU-3: hardcoded, not computed
    }

    /// <summary>
    ///     GIVEN a plot with 2 IoT devices (1 active, 1 inactive)
    ///     WHEN <see cref="GetPlotDetailQueryService.Handle(GetPlotDetailQuery, CancellationToken)"/> is called
    ///     THEN IoTDetailResource.OnlineDeviceCount is computed from real device list.
    ///     FU-3 partial real data: OnlineCount IS real, while Status and LastActivityAt are fabricated.
    /// </summary>
    [Fact]
    public async Task Handle_IoTDeviceCount_ComputedFromRealDevices()
    {
        // GIVEN a plot with 2 devices (1 active, 1 inactive)
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);

        var activeDevice = BuildDevice(1, 10, "Device A");
        var activateResult = activeDevice.Activate();
        Assert.True(activateResult.IsSuccess); // Pending → Active

        var inactiveDevice = BuildDevice(2, 10, "Device B");
        var activateResult2 = inactiveDevice.Activate();
        Assert.True(activateResult2.IsSuccess); // Pending → Active
        var deactivateResult = inactiveDevice.Deactivate();
        Assert.True(deactivateResult.IsSuccess); // Active → Inactive

        _ioTDeviceRepository.FindAllByPlotIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IoTDevice> { activeDevice, inactiveDevice });

        var query = new GetPlotDetailQuery(PlotId: 10, UserId: 1);

        // WHEN the detail is fetched
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN device counts are real (FU-3: partial real data)
        Assert.True(result.IsSuccess);
        var detail = ((Result<PlotDetailResource, Error>.Success)result).Value!;
        Assert.Equal(2, detail.Iot.LinkedDeviceCount);  // real count
        Assert.Equal(1, detail.Iot.OnlineDeviceCount);   // real count of Active devices
    }

    /// <summary>
    ///     Builds an <see cref="IoTDevice"/> with a known Id.
    /// </summary>
    private static IoTDevice BuildDevice(long id, long plotId, string deviceName)
    {
        var result = IoTDevice.Claim(
            plotId,
            deviceName,
            new ActivationCode($"VIORA-WS01-{id:D4}"),
            new FakeClock());
        var device = ((Result<IoTDevice, Error>.Success)result).Value;
        typeof(IoTDevice).GetProperty(nameof(IoTDevice.Id))!.SetValue(device, id);
        return device;
    }
}
