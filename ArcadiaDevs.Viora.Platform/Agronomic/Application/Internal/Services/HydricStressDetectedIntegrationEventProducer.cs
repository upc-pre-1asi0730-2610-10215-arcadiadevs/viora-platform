using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;

/// <summary>
///     Produces <see cref="HydricStressDetectedIntegrationEvent"/> for active plots
///     where an IoT device's simulated soil moisture falls below the critical
///     threshold (20%). Uses <see cref="ISoilReadingSimulator"/> and
///     <see cref="ISensorHealthEvaluator"/> from 1.17.0.
/// </summary>
public class HydricStressDetectedIntegrationEventProducer(
    IPlotRepository plotRepository,
    IIoTDeviceRepository ioTDeviceRepository,
    IAgronomicStatisticRepository agronomicStatisticRepository,
    ISoilReadingSimulator soilReadingSimulator,
    ISensorHealthEvaluator sensorHealthEvaluator,
    IMediator mediator,
    ILogger<HydricStressDetectedIntegrationEventProducer> logger)
    : IHydricStressDetectedIntegrationEventProducer
{
    private const double CriticalMoistureThreshold = 20.0;

    public async Task ProduceHydricStressEventsAsync(CancellationToken ct = default)
    {
        var plots = (await plotRepository.ListAsync(ct))
            .Where(p => p.IsActive)
            .ToList();

        if (plots.Count == 0)
        {
            logger.LogDebug("No active plots found for hydric stress evaluation.");
            return;
        }

        var plotsById = plots.ToDictionary(p => (long)p.Id);
        var now = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero);

        var devices = (await ioTDeviceRepository.FindAllByPlotIdsAsync(plotsById.Keys, ct))
            .Where(d => d.Status == IoTDeviceStatus.Active)
            .ToList();

        foreach (var device in devices)
        {
            if (!plotsById.TryGetValue(device.PlotId, out var plot))
                continue;

            var latestStat = await agronomicStatisticRepository.FindLatestByPlotIdAsync(plot.Id, ct);
            var latestNdvi = latestStat?.NdviValue;

            var location = plot.PolygonCoordinates.Centroid();
            var type = device.ActivationCode?.DeviceType() ?? IoTDeviceType.WeatherStation;

            var readings = soilReadingSimulator.Simulate(
                device.ActivationCode, type, location, latestNdvi, now);

            var health = sensorHealthEvaluator.Evaluate(readings);

            if (health == GeneralHealthStatus.Critical && readings.SoilMoisture is int moisture && moisture < (int)CriticalMoistureThreshold)
            {
                var sensorId = device.ActivationCode?.Value ?? device.DeviceName;

                await mediator.PublishAsync(
                    new HydricStressDetectedIntegrationEvent(
                        plot.Id,
                        sensorId,
                        moisture,
                        CriticalMoistureThreshold),
                    ct);

                logger.LogInformation(
                    "Published HydricStressDetectedIntegrationEvent for PlotId={PlotId}, SensorId={SensorId}, Moisture={Moisture}.",
                    plot.Id, sensorId, moisture);
            }
        }
    }
}
