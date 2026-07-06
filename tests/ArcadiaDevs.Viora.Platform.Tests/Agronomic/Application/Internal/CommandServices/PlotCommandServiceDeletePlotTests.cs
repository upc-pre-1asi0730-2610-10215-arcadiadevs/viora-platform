using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Application.Internal.CommandServices;

/// <summary>
///     S3.10 — Plot deletion policy tests for the A3 deliverable.
///     The delete flow branches on <c>HasRelatedOperationalRecordsAsync</c>;
///     when the method reports <c>true</c> the plot is logically deactivated
///     (<c>Deactivate()</c> + <c>Update</c>); when <c>false</c> the plot is
///     physically removed (<c>Remove</c>).
/// </summary>
public class PlotCommandServiceDeletePlotTests
{
    // ---------- helpers ----------

    private static GeoPoint MakePoint(decimal lat, decimal lon) =>
        ((Result<GeoPoint, Error>.Success)GeoPoint.Create(lat, lon)).Value;

    private static PolygonCoordinates MakePolygon()
    {
        var points = new List<GeoPoint>
        {
            MakePoint(-12.0m, -77.0m),
            MakePoint(-12.0m, -77.1m),
            MakePoint(-12.1m, -77.1m),
            MakePoint(-12.1m, -77.0m),
            MakePoint(-12.0m, -77.0m)
        };
        return ((Result<PolygonCoordinates, Error>.Success)PolygonCoordinates.Create(points)).Value;
    }

    private static Plot CreateActivePlot() =>
        ((Result<Plot, Error>.Success)Plot.Create(
            ownerUserId: 1,
            plotName: $"Plot-{Guid.NewGuid():N}",
            polygonCoordinates: MakePolygon(),
            areaSize: 100m,
            cropType: "Coffee",
            variety: "Arabica",
            location: "Lima",
            campaign: "2026",
            notes: "test notes")).Value;

    private static AgroMonitoringApiClient BuildAgroClient()
    {
        // The PlotCommandService ctor requires an AgroMonitoringApiClient,
        // but the DELETE path never calls it. We just need a non-throwing
        // construction (the ctor reads IConfiguration["ExternalApis:..."]).
        var config = new TestConfiguration();
        return new AgroMonitoringApiClient(new HttpClient(), config, Substitute.For<ILogger<AgroMonitoringApiClient>>());
    }

    private static PlotCommandService BuildService(
        IPlotRepository plotRepository,
        IUnitOfWork unitOfWork,
        ILogger<PlotCommandService> logger)
    {
        return new PlotCommandService(
            plotRepository,
            unitOfWork,
            BuildAgroClient(),
            logger,
            new ChillRequirementResolver(new ChillRequirementPolicy(defaultRequirementPortions: 60.0)));
    }

    // ---------- S3.10 ----------

    [Fact]
    public async Task Handle_DeletePlot_WithDynamicNutritionPlan_TriggersLogicalDeletion()
    {
        // Arrange — repository reports a related record exists for the plot.
        // The command service must call Deactivate() (logical) and NOT Remove()
        // (physical). The Deactivate() effect is observable on the Plot
        // aggregate: IsActive=false, IsDeleted=true.
        var plot = CreateActivePlot();
        var plotRepository = Substitute.For<IPlotRepository>();
        plotRepository.FindByIdAsync(plot.Id, Arg.Any<CancellationToken>()).Returns(plot);
        plotRepository.HasRelatedOperationalRecordsAsync(plot.Id, Arg.Any<CancellationToken>()).Returns(true);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = BuildService(plotRepository, unitOfWork, Substitute.For<ILogger<PlotCommandService>>());

        // Act
        var result = await service.Handle(new DeletePlotCommand(plot.Id, 1), CancellationToken.None);

        // Assert — Result is Success
        Assert.True(result.IsSuccess);

        // Assert — logical deletion observable on the plot
        Assert.False(plot.IsActive);
        Assert.True(plot.IsDeleted);

        // Assert — physical Remove was NOT called; Update was called instead
        plotRepository.DidNotReceive().Remove(plot);
        plotRepository.Received(1).Update(plot);
    }

    // ---------- bonus: all three triggers present ----------

    [Fact]
    public async Task Handle_DeletePlot_WithAllThreeTriggers_TriggersLogicalDeletion()
    {
        // Arrange — all 3 intra-BC aggregates (IoTDevice, DynamicNutritionPlan,
        // AgronomicStatistic) trigger the check. The orchestrator returns true;
        // the command service must take the logical-deletion path.
        var plot = CreateActivePlot();
        var plotRepository = Substitute.For<IPlotRepository>();
        plotRepository.FindByIdAsync(plot.Id, Arg.Any<CancellationToken>()).Returns(plot);
        plotRepository.HasRelatedOperationalRecordsAsync(plot.Id, Arg.Any<CancellationToken>()).Returns(true);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = BuildService(plotRepository, unitOfWork, Substitute.For<ILogger<PlotCommandService>>());

        // Act
        await service.Handle(new DeletePlotCommand(plot.Id, 1), CancellationToken.None);

        // Assert
        Assert.True(plot.IsDeleted);
        plotRepository.DidNotReceive().Remove(Arg.Any<Plot>());
    }

    // ---------- bonus: no triggers → physical removal ----------

    [Fact]
    public async Task Handle_DeletePlot_WithNoTriggers_TriggersPhysicalDeletion()
    {
        // Arrange — repository reports NO related records. The command service
        // must take the physical-removal path: Remove is called, Update is NOT.
        var plot = CreateActivePlot();
        var plotRepository = Substitute.For<IPlotRepository>();
        plotRepository.FindByIdAsync(plot.Id, Arg.Any<CancellationToken>()).Returns(plot);
        plotRepository.HasRelatedOperationalRecordsAsync(plot.Id, Arg.Any<CancellationToken>()).Returns(false);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = BuildService(plotRepository, unitOfWork, Substitute.For<ILogger<PlotCommandService>>());

        // Act
        var result = await service.Handle(new DeletePlotCommand(plot.Id, 1), CancellationToken.None);

        // Assert — Result is Success
        Assert.True(result.IsSuccess);

        // Assert — physical removal path was taken
        plotRepository.Received(1).Remove(plot);
        plotRepository.DidNotReceive().Update(plot);
    }
}

/// <summary>
///     Minimal <see cref="IConfiguration"/> stub for tests that construct
///     <see cref="AgroMonitoringApiClient"/>. Only the AgroMonitoring key
///     is queried by the production ctor; everything else returns <c>null</c>.
/// </summary>
internal sealed class TestConfiguration : IConfiguration
{
    public string? this[string key]
    {
        get => key == "ExternalApis:AgroMonitoring:ApiKey" ? "test-api-key" : null;
        set { /* no-op */ }
    }

    public IConfigurationSection GetSection(string key) => new TestConfigurationSection(key);

    public IEnumerable<IConfigurationSection> GetChildren() => [];

    public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() =>
        new NoopChangeToken();
}

internal sealed class TestConfigurationSection : IConfigurationSection
{
    public TestConfigurationSection(string? path) { Path = path ?? string.Empty; }
    public string Path { get; set; }
    public string Key => Path;
    public string? Value { get; set; }
    public string? this[string key] { get => null; set { } }
    public IConfigurationSection GetSection(string key) => new TestConfigurationSection(key);
    public IEnumerable<IConfigurationSection> GetChildren() => [];
    public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() =>
        new NoopChangeToken();
}

/// <summary>
///     Minimal <see cref="Microsoft.Extensions.Primitives.IChangeToken"/>
///     implementation that never reports a change. Avoids a transitive
///     dependency on <c>Microsoft.Extensions.Configuration.Memory</c>.
/// </summary>
internal sealed class NoopChangeToken : Microsoft.Extensions.Primitives.IChangeToken
{
    public bool HasChanged => false;
    public bool ActiveChangeCallbacks => false;
    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new NoopDisposable();
    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
