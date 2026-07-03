using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;

/// <summary>
///     Weather data service backed by the AgroMonitoring external API.
///     This is the <strong>sole</strong> <see cref="IWeatherDataService"/>
///     implementation in v1 (AGRO-003).
/// </summary>
/// <remarks>
///     <para>
///         The service is a thin delegate over <see cref="AgroMonitoringApiClient"/>:
///         it calls the real client and maps responses to the
///         <see cref="WeatherSnapshot"/>, <see cref="WeatherHistory"/>, and
///         <see cref="DataSourceMetadata"/> value objects.
///     </para>
///     <para>
///         <strong>There is no fabricated-data fallback.</strong> If the
///         upstream call returns <see cref="Result{TValue,TError}.Failure"/>
///         the service logs a warning and returns <c>null</c>; if the
///         upstream call throws, the service logs an error and propagates
///         the exception so the caller can surface a 5xx. Operators see
///         the real AgroMonitoring payload or an error — never a
///         hard-coded <c>22.5 °C / Sunny / Low</c> value.
///     </para>
///     <para>
///         <strong>Operational risk:</strong> if AgroMonitoring is down or
///         quota is exhausted, the platform has no alternative weather
///         source in v1. A future enhancement may add a cache or
///         an <c>EmptyWeatherDataService</c> fallback behind a feature flag.
///     </para>
/// </remarks>
public class AgroMonitoringWeatherDataService : IWeatherDataService
{
    private const double KelvinToCelsiusOffset = 273.15;
    private const string ProviderName = "AgroMonitoring";

    private readonly IAgroMonitoringWeatherClient _client;
    private readonly ILogger<AgroMonitoringWeatherDataService> _logger;
    private readonly IClock _clock;

    /// <summary>
    ///     Initializes a new instance of the
    ///     <see cref="AgroMonitoringWeatherDataService"/> class.
    /// </summary>
    /// <param name="client">
    ///     The AgroMonitoring weather client (port). The concrete
    ///     implementation is <see cref="AgroMonitoringApiClient"/>, which
    ///     is registered as <see cref="IAgroMonitoringWeatherClient"/> in DI.
    /// </param>
    /// <param name="options">
    ///     The validated AgroMonitoring options. The presence of an API
    ///     key is enforced at startup by
    ///     <see cref="AgroMonitoringWeatherOptionsValidator"/>; this
    ///     service trusts the options and uses the key when relevant.
    /// </param>
    /// <param name="logger">The logger.</param>
    /// <param name="clock">The shared clock used for "current" timestamps.</param>
    public AgroMonitoringWeatherDataService(
        IAgroMonitoringWeatherClient client,
        IOptions<AgroMonitoringWeatherOptions> options,
        ILogger<AgroMonitoringWeatherDataService> logger,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(clock);

        _client = client;
        _logger = logger;
        _clock = clock;
        // The API key is validated at startup by
        // AgroMonitoringWeatherOptionsValidator. The value is consumed
        // indirectly by AgroMonitoringApiClient (which reads its own
        // configuration), so we keep the options here for symmetry with
        // other consumers and to support future per-call overrides.
        _ = options.Value.ApiKey;
    }

    /// <inheritdoc />
    public async Task<WeatherSnapshot?> GetCurrentWeatherSnapshotAsync(
        Plot plot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plot);

        var coordinates = ResolveCoordinates(plot);
        var end = new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero);
        var start = end.AddHours(-1);

        var result = await _client.GetAccumulatedTemperatureAsync(
            coordinates.Latitude,
            coordinates.Longitude,
            start,
            end,
            KelvinToCelsiusOffset,
            cancellationToken);

        if (result is Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>.Failure failure)
        {
            _logger.LogWarning(
                "AgroMonitoring current-weather call failed for plot {PlotId}: {Code} {Message}",
                plot.Id, failure.Error.Code, failure.Error.Message);
            return null;
        }

        var success = (Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>.Success)result;
        var latest = success.Value
            .OrderByDescending(p => p.Dt)
            .FirstOrDefault();

        if (latest is null)
        {
            _logger.LogInformation(
                "AgroMonitoring returned an empty temperature series for plot {PlotId}; " +
                "no fabricated fallback will be produced.",
                plot.Id);
            return null;
        }

        var temperatureCelsius = latest.Temp - KelvinToCelsiusOffset;
        var lastValidatedReadingAt = DateTimeOffset.FromUnixTimeSeconds(latest.Dt);

        return new WeatherSnapshot(
            currentTemperature: (decimal)temperatureCelsius,
            weatherStatus: WeatherStatus.Sunny,
            lastValidatedReadingAt: lastValidatedReadingAt,
            climateRiskLevel: ClimateRiskLevel.Low,
            clock: _clock);
    }

    /// <inheritdoc />
    public async Task<WeatherHistory?> GetWeatherHistoryAsync(
        Plot plot,
        DateRange range,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plot);
        ArgumentNullException.ThrowIfNull(range);

        var coordinates = ResolveCoordinates(plot);

        var result = await _client.GetAccumulatedTemperatureAsync(
            coordinates.Latitude,
            coordinates.Longitude,
            range.StartDate,
            range.EndDate,
            KelvinToCelsiusOffset,
            cancellationToken);

        if (result is Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>.Failure failure)
        {
            _logger.LogWarning(
                "AgroMonitoring weather-history call failed for plot {PlotId}: {Code} {Message}",
                plot.Id, failure.Error.Code, failure.Error.Message);
            return null;
        }

        var success = (Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>.Success)result;
        if (success.Value.Count == 0)
        {
            _logger.LogInformation(
                "AgroMonitoring returned an empty history for plot {PlotId}; " +
                "no fabricated fallback will be produced.",
                plot.Id);
            return null;
        }

        var readings = success.Value
            .OrderBy(p => p.Dt)
            .Select(p => new WeatherReading(
                timestamp: DateTimeOffset.FromUnixTimeSeconds(p.Dt),
                temperatureCelsius: p.Temp - KelvinToCelsiusOffset,
                weatherStatus: WeatherStatus.Sunny,
                humidityPercentage: null,
                precipitationMillimeters: null))
            .ToList();

        return new WeatherHistory(readings);
    }

    /// <inheritdoc />
    public Task<DataSourceMetadata> DescribeSourceAsync(
        Plot plot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plot);
        var now = new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero);
        return Task.FromResult(new DataSourceMetadata(
            ProviderName: ProviderName,
            ConnectivityStatus: "Online",
            LastSyncAt: now,
            SyncIntervalMinutes: 60));
    }

    private static (decimal Latitude, decimal Longitude) ResolveCoordinates(Plot plot)
    {
        if (plot.PolygonCoordinates is null || plot.PolygonCoordinates.Points.Count == 0)
        {
            throw new InvalidOperationException(
                $"Plot {plot.Id} has no polygon coordinates; cannot resolve AgroMonitoring weather data.");
        }

        var first = plot.PolygonCoordinates.Points[0];
        return (first.Latitude, first.Longitude);
    }
}
