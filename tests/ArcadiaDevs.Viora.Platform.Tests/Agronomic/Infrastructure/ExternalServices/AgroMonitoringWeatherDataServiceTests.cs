using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Infrastructure.ExternalServices;

/// <summary>
/// Verifies that <see cref="AgroMonitoringWeatherDataService"/> is a true delegate to
/// <see cref="AgroMonitoringApiClient"/>: it passes through the client's responses
/// and never fabricates data when the real call fails (AGRO-003).
/// </summary>
/// <remarks>
/// The previous <c>WeatherDataServiceAdapter</c> returned hard-coded
/// <c>22.5 °C / Sunny / Low risk</c> values for every plot. This test pins the
/// new behaviour: the service must call the client, and on a client failure it
/// must return <c>null</c> (or surface the exception) — never a fabricated
/// snapshot.
/// </remarks>
public class AgroMonitoringWeatherDataServiceTests
{
    private readonly IAgroMonitoringWeatherClient _client = Substitute.For<IAgroMonitoringWeatherClient>();
    private readonly IOptions<AgroMonitoringWeatherOptions> _options =
        Options.Create(new AgroMonitoringWeatherOptions { ApiKey = "test-key" });
    private readonly ILogger<AgroMonitoringWeatherDataService> _logger =
        Substitute.For<ILogger<AgroMonitoringWeatherDataService>>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly AgroMonitoringWeatherDataService _sut;

    public AgroMonitoringWeatherDataServiceTests()
    {
        var fixedNow = new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(fixedNow);
        _sut = new AgroMonitoringWeatherDataService(_client, _options, _logger, _clock);
    }

    private static Plot CreatePlotWithPolygon()
    {
        var coordinatesResult = PolygonCoordinates.Create(new List<GeoPoint>
        {
            new() { Latitude = -12.05m, Longitude = -77.04m },
            new() { Latitude = -12.06m, Longitude = -77.04m },
            new() { Latitude = -12.06m, Longitude = -77.05m },
            new() { Latitude = -12.05m, Longitude = -77.04m }
        });

        var plotResult = Plot.Create(
            ownerUserId: 42,
            plotName: "Test Plot",
            polygonCoordinates: ((Result<PolygonCoordinates, Error>.Success)coordinatesResult).Value,
            areaSize: 1000m);

        return ((Result<Plot, Error>.Success)plotResult).Value;
    }

    [Fact]
    public async Task GetCurrentWeatherSnapshotAsync_DelegatesToClient_AndReturnsMappedSnapshot_WhenClientSucceeds()
    {
        // Arrange
        var plot = CreatePlotWithPolygon();
        var now = new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero);
        var points = new List<AgroMonitoringTemperatureDataPoint>
        {
            new() { Dt = now.ToUnixTimeSeconds(), Temp = 295.15, Count = 1 }
        };
        _client.GetAccumulatedTemperatureAsync(
            Arg.Any<decimal>(),
            Arg.Any<decimal>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<double>(),
            Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>.Success(points));

        // Act
        var snapshot = await _sut.GetCurrentWeatherSnapshotAsync(plot, CancellationToken.None);

        // Assert
        Assert.NotNull(snapshot);
        await _client.Received(1).GetAccumulatedTemperatureAsync(
            Arg.Any<decimal>(),
            Arg.Any<decimal>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<double>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCurrentWeatherSnapshotAsync_ReturnsNull_WhenClientReturnsFailure_NotFabricatedData()
    {
        // Arrange
        var plot = CreatePlotWithPolygon();
        _client.GetAccumulatedTemperatureAsync(
            Arg.Any<decimal>(),
            Arg.Any<decimal>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<double>(),
            Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>.Failure(
                new Error("AGROMONITORING_NETWORK_ERROR", "Network error")));

        // Act
        var snapshot = await _sut.GetCurrentWeatherSnapshotAsync(plot, CancellationToken.None);

        // Assert — null is the only acceptable outcome; NO 22.5°C / Sunny hard-coded fallback
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task GetCurrentWeatherSnapshotAsync_PropagatesException_WhenClientThrows_NotFabricatedData()
    {
        // Arrange
        var plot = CreatePlotWithPolygon();
        _client.GetAccumulatedTemperatureAsync(
            Arg.Any<decimal>(),
            Arg.Any<decimal>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<double>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("DNS failure"));

        // Act + Assert — exception must propagate, NO silent fabricated fallback
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.GetCurrentWeatherSnapshotAsync(plot, CancellationToken.None));
    }

    [Fact]
    public async Task GetWeatherHistoryAsync_ReturnsNull_WhenClientReturnsFailure_NotFabricatedData()
    {
        // Arrange
        var plot = CreatePlotWithPolygon();
        var range = new DateRange(
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero));

        _client.GetAccumulatedTemperatureAsync(
            Arg.Any<decimal>(),
            Arg.Any<decimal>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<double>(),
            Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>.Failure(
                new Error("AGROMONITORING_TEMP_FAILED", "Temperature history failed")));

        // Act
        var history = await _sut.GetWeatherHistoryAsync(plot, range, CancellationToken.None);

        // Assert — null is the only acceptable outcome; NO fabricated 24-reading loop
        Assert.Null(history);
    }

    [Fact]
    public async Task DescribeSourceAsync_ReportsAgroMonitoringAsProvider()
    {
        // Arrange
        var plot = CreatePlotWithPolygon();

        // Act
        var metadata = await _sut.DescribeSourceAsync(plot, CancellationToken.None);

        // Assert — the source must be AgroMonitoring, never a "MockWeather" placeholder
        Assert.Equal("AgroMonitoring", metadata.ProviderName);
    }
}
