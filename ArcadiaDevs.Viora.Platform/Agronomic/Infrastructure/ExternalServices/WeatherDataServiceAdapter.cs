using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;

/// <summary>
///     Mock adapter for weather data.
/// </summary>
public class WeatherDataServiceAdapter : IWeatherDataService
{
    public Task<WeatherSnapshot?> GetCurrentWeatherSnapshotAsync(Plot plot, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<WeatherSnapshot?>(new WeatherSnapshot(
            22.5m,
            WeatherStatus.Sunny,
            DateTimeOffset.UtcNow,
            ClimateRiskLevel.Low
        ));
    }

    public Task<WeatherHistory?> GetWeatherHistoryAsync(Plot plot, DateRange range, CancellationToken cancellationToken = default)
    {
        var readings = new List<WeatherReading>();
        var currentDate = range.StartDate;

        while (currentDate <= range.EndDate)
        {
            // Simulate 24 hourly readings for each day
            for (int i = 0; i < 24; i++)
            {
                var timestamp = currentDate.AddHours(i);
                // Simulate some temperature curve
                double temp = 15.0 + 10.0 * Math.Sin(Math.PI * (i - 6) / 12.0); 

                readings.Add(new WeatherReading(
                    timestamp,
                    temp,
                    WeatherStatus.Sunny,
                    50,
                    0.0
                ));
            }
            currentDate = currentDate.AddDays(1);
        }

        return Task.FromResult<WeatherHistory?>(new WeatherHistory(readings));
    }

    public Task<DataSourceMetadata> DescribeSourceAsync(Plot plot, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DataSourceMetadata(
            "MockWeather",
            "Online",
            DateTimeOffset.UtcNow,
            60
        ));
    }
}
