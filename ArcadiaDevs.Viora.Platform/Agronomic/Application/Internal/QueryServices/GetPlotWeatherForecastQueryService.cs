using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Returns the current weather forecast for a plot by delegating to
///     <see cref="IWeatherDataService"/>. The sole registered implementation
///     in v1 is <c>AgroMonitoringWeatherDataService</c> (AGRO-003).
/// </summary>
/// <remarks>
///     <para>
///         This query service previously fabricated its response with
///         hard-coded <c>22.5 °C / Sunny</c> values. As of AGRO-003 it
///         must delegate to the real weather provider; if the upstream
///         call fails, the service returns
///         <see cref="AgronomicErrors.WeatherUnavailable"/> — there is
///         no fabricated fallback in v1.
///     </para>
/// </remarks>
public class GetPlotWeatherForecastQueryService(
    IPlotRepository plotRepository,
    ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock,
    IWeatherDataService weatherDataService) : IGetPlotWeatherForecastQueryService
{
    public async Task<Result<PlotWeatherForecastResource, Error>> Handle(
        GetPlotWeatherForecastQuery query,
        CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(query.PlotId, cancellationToken);
        if (plot is null || plot.IsDeleted)
            return new Result<PlotWeatherForecastResource, Error>.Failure(AgronomicErrors.PlotNotFound);

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        // Delegate to the real provider. A null result means the upstream
        // call failed (Result.Failure) and the provider does NOT fabricate
        // a fallback — surface the unavailability to the caller instead of
        // inventing a snapshot.
        var snapshot = await weatherDataService.GetCurrentWeatherSnapshotAsync(plot, cancellationToken);
        if (snapshot is null)
            return new Result<PlotWeatherForecastResource, Error>.Failure(AgronomicErrors.WeatherUnavailable);

        var source = await weatherDataService.DescribeSourceAsync(plot, cancellationToken);

        var hourly = new List<HourlyForecastResource>
        {
            new HourlyForecastResource(
                Timestamp: snapshot.LastValidatedReadingAt,
                TemperatureCelsius: (double)snapshot.CurrentTemperature,
                WeatherStatus: snapshot.WeatherStatus.ToString(),
                HumidityPercentage: 0,
                PrecipitationMillimeters: 0.0,
                WindSpeedMetersPerSecond: 0.0,
                WindGustMetersPerSecond: 0.0)
        };

        var daily = new List<DailyForecastResource>
        {
            new DailyForecastResource(
                Date: snapshot.LastValidatedReadingAt.ToString("yyyy-MM-dd"),
                MinTemperatureCelsius: (double)snapshot.CurrentTemperature,
                MaxTemperatureCelsius: (double)snapshot.CurrentTemperature,
                AverageTemperatureCelsius: (double)snapshot.CurrentTemperature,
                DominantStatus: snapshot.WeatherStatus.ToString(),
                AverageHumidityPercentage: 0,
                TotalPrecipitationMillimeters: 0.0,
                MaxWindGustMetersPerSecond: 0.0)
        };

        var warnings = new List<WeatherWarningResource>();

        var sourceResource = new ExternalSourceResource(
            Provider: source.ProviderName,
            Availability: source.ConnectivityStatus,
            LastReadingAt: source.LastSyncAt,
            UpdateFrequencyMinutes: source.SyncIntervalMinutes);

        var resource = new PlotWeatherForecastResource(
            PlotId: plot.Id,
            UserId: plot.OwnerUserId,
            PlotName: plot.PlotName,
            GeneratedAt: now,
            Hourly: hourly,
            Daily: daily,
            ThermalAnomalyCelsius: 0.0,
            OverallRisk: snapshot.ClimateRiskLevel.ToString(),
            Warnings: warnings,
            Source: sourceResource);

        return new Result<PlotWeatherForecastResource, Error>.Success(resource);
    }
}
