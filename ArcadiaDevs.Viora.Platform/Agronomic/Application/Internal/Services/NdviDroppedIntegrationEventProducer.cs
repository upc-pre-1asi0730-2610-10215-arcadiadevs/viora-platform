using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;

/// <summary>
///     Produces <see cref="NdviDroppedIntegrationEvent"/> for active plots whose
///     latest NDVI reading has dropped at least <see cref="DropThreshold"/> below
///     the plot's own historical average over the preceding 14 days. Requires at
///     least 2 historical readings so a single noisy sample can't trigger a false
///     alert.
/// </summary>
public class NdviDroppedIntegrationEventProducer(
    IPlotRepository plotRepository,
    IAgronomicStatisticRepository agronomicStatisticRepository,
    IMediator mediator,
    ILogger<NdviDroppedIntegrationEventProducer> logger,
    IClock clock)
    : INdviDroppedIntegrationEventProducer
{
    private const double DropThreshold = 0.15;
    private const int HistoryWindowDays = 14;
    private const int MinimumHistorySamples = 2;

    public async Task ProduceNdviDroppedEventsAsync(CancellationToken ct = default)
    {
        var plots = (await plotRepository.ListAsync(ct))
            .Where(p => p.IsActive)
            .ToList();

        if (plots.Count == 0)
        {
            logger.LogDebug("No active plots found for NDVI-drop evaluation.");
            return;
        }

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        foreach (var plot in plots)
        {
            var latest = await agronomicStatisticRepository.FindLatestByPlotIdAsync(plot.Id, ct);
            if (latest is null)
                continue;

            var historyEnd = latest.MeasurementDate.AddDays(-1);
            var historyStart = historyEnd.AddDays(-(HistoryWindowDays - 1));

            var history = (await agronomicStatisticRepository.FindAllByUserIdAndPlotIdAndDateBetweenAsync(
                plot.OwnerUserId, plot.Id, historyStart, historyEnd, ct)).ToList();

            if (history.Count < MinimumHistorySamples)
                continue;

            var historicalAverage = history.Average(s => s.NdviValue);
            if (historicalAverage <= 0)
                continue;

            var relativeDrop = (historicalAverage - latest.NdviValue) / historicalAverage;
            if (relativeDrop < DropThreshold)
                continue;

            await mediator.PublishAsync(
                new NdviDroppedIntegrationEvent(plot.Id, latest.NdviValue, historicalAverage),
                ct);

            logger.LogInformation(
                "Published NdviDroppedIntegrationEvent for PlotId={PlotId}, CurrentNdvi={CurrentNdvi}, HistoricalAverage={HistoricalAverage}.",
                plot.Id, latest.NdviValue, historicalAverage);
        }
    }
}
