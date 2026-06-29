using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using NSubstitute;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Application.Internal.CommandServices;

/// <summary>
/// Verifies that <see cref="AgronomicStatisticIngestionService"/> resolves the
/// "today" timestamp from the constructor-injected <see cref="IClock"/> rather
/// than calling <c>DateTimeOffset.UtcNow</c> directly (SHARED-008).
/// </summary>
public class AgronomicStatisticIngestionServiceClockTests
{
    [Fact]
    public async Task IngestAllActivePlotsAsync_UsesInjectedClock_ForToday()
    {
        // GIVEN a fixed IClock substitute
        var fixedNow = new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(fixedNow);

        // AND a service whose plot list is empty (so the loop does not run and
        // we are isolating the "today" calculation from any other path)
        var plotRepository = Substitute.For<IPlotRepository>();
        plotRepository.ListAsync(Arg.Any<CancellationToken>())
                      .Returns(new List<Plot>());

        var sut = new AgronomicStatisticIngestionService(
            plotRepository,
            Substitute.For<IAgronomicStatisticRepository>(),
            Substitute.For<IAgroMonitoringImageryService>(),
            Substitute.For<IWeatherDataService>(),
            Substitute.For<ChillAccumulationCalculator>(),
            Substitute.For<IAgroMonitoringPlotIntegrationRepository>(),
            clock);

        // WHEN ingesting all active plots
        var report = await sut.IngestAllActivePlotsAsync(CancellationToken.None);

        // THEN the clock was consulted exactly once (proving IClock is the
        // source of "today" and no hidden DateTimeOffset.UtcNow sneaks past it)
        _ = clock.Received(1).UtcNow;

        // AND the report is empty (no plots → nothing ingested, nothing skipped)
        Assert.Equal(0, report.Ingested);
        Assert.Equal(0, report.Skipped);
    }
}
