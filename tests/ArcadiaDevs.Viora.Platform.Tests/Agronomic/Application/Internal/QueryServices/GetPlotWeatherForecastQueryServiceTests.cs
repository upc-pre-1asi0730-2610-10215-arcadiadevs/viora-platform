using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
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
///     Unit tests for <see cref="GetPlotWeatherForecastQueryService"/>.
///     Template B: query service with NSubstitute mocks.
///     Covers ownership check (IDOR closure) and FU-7 Unknown sentinel.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class GetPlotWeatherForecastQueryServiceTests
{
    private readonly IPlotRepository _plotRepository = Substitute.For<IPlotRepository>();
    private readonly IClock _clock = new FakeClock();
    private readonly IWeatherDataService _weatherDataService = Substitute.For<IWeatherDataService>();
    private readonly GetPlotWeatherForecastQueryService _sut;

    public GetPlotWeatherForecastQueryServiceTests()
    {
        _sut = new GetPlotWeatherForecastQueryService(_plotRepository, _clock, _weatherDataService);
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
    ///     GIVEN a plot owned by user 1 with valid weather data
    ///     WHEN <see cref="GetPlotWeatherForecastQueryService.Handle(GetPlotWeatherForecastQuery, CancellationToken)"/> is called by the owner
    ///     THEN the forecast resource is returned.
    /// </summary>
    [Fact]
    public async Task Handle_OwnerReturnsForecast()
    {
        // GIVEN a plot owned by user 1
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);

        var weather = new WeatherSnapshot(22.5m, WeatherStatus.Sunny, new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero), ClimateRiskLevel.Low, _clock);
        _weatherDataService.GetCurrentWeatherSnapshotAsync(plot, Arg.Any<CancellationToken>())
            .Returns(weather);
        _weatherDataService.DescribeSourceAsync(plot, Arg.Any<CancellationToken>())
            .Returns(new DataSourceMetadata("AgroMonitoring", "Online", DateTimeOffset.UtcNow, 60));

        var query = new GetPlotWeatherForecastQuery(PlotId: 10, UserId: 1);

        // WHEN the query is handled
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN the forecast is returned
        Assert.True(result.IsSuccess);
        var forecast = ((Result<PlotWeatherForecastResource, Error>.Success)result).Value!;
        Assert.Equal(10, forecast.PlotId);
        Assert.Equal(1, forecast.UserId);
        Assert.Equal("Sunny", forecast.Hourly.First().WeatherStatus);
    }

    /// <summary>
    ///     GIVEN a plot owned by user 1
    ///     WHEN user 2 requests the forecast
    ///     THEN <see cref="AgronomicErrors.PlotOwnership"/> is returned (IDOR closure).
    /// </summary>
    [Fact]
    public async Task Handle_NonOwner_ReturnsForbidden()
    {
        // GIVEN a plot owned by user 1
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);

        // WHEN user 2 requests the forecast — SECURITY CRITICAL: IDOR closure test
        var query = new GetPlotWeatherForecastQuery(PlotId: 10, UserId: 2);
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN PlotOwnership error is returned
        Assert.True(result.IsFailure);
        Assert.Equal(AgronomicErrors.PlotOwnership, ((Result<PlotWeatherForecastResource, Error>.Failure)result).Error);
    }

    /// <summary>
    ///     GIVEN IWeatherDataService returns null (upstream failure)
    ///     WHEN <see cref="GetPlotWeatherForecastQueryService.Handle(GetPlotWeatherForecastQuery, CancellationToken)"/> is called
    ///     THEN <see cref="AgronomicErrors.WeatherUnavailable"/> is returned (no fabricated fallback).
    ///     FU-7 guard: the service surfaces unavailability instead of inventing a snapshot.
    ///     Source: GetPlotWeatherForecastQueryService.cs:54-55
    /// </summary>
    [Fact]
    public async Task Handle_WeatherSnapshotNull_ReturnsWeatherUnavailable()
    {
        // GIVEN a plot with no weather data
        var plot = BuildPlot(10, ownerUserId: 1);
        _plotRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(plot);
        _weatherDataService.GetCurrentWeatherSnapshotAsync(plot, Arg.Any<CancellationToken>())
            .Returns((WeatherSnapshot?)null);

        var query = new GetPlotWeatherForecastQuery(PlotId: 10, UserId: 1);

        // WHEN the query is handled
        var result = await _sut.Handle(query, CancellationToken.None);

        // THEN WeatherUnavailable is returned (FU-7: no fabricated fallback)
        Assert.True(result.IsFailure);
        Assert.Equal(AgronomicErrors.WeatherUnavailable, ((Result<PlotWeatherForecastResource, Error>.Failure)result).Error);
    }
}
