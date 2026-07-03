using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Hosting;

/// <summary>
/// Background service that schedules agronomic statistic ingestion and chill deficit monitoring.
/// </summary>
/// <remarks>
/// <para>
/// This is the first <see cref="IHostedService"/> in the WA. It runs on a 24-hour
/// <see cref="PeriodicTimer"/> (no NuGet dependency; R3 mitigation).
/// </para>
/// <para>
/// The cron expression from <see cref="AgronomicStatisticsOptions.IngestionCron"/> is logged
/// on startup as an informational label; the actual scheduling uses the 24-hour timer.
/// </para>
/// <para>
/// The scheduler is gated by <see cref="AgronomicStatisticsOptions.ScheduledIngestionEnabled"/>
/// (default: <c>false</c>). When disabled, <see cref="ExecuteAsync"/> returns immediately (D14 + R4 opt-in).
/// </para>
/// <para>
/// The D14 chill deficit producer iterates active plots, queries the latest statistic,
/// calls <see cref="IChillDeficitEvaluator.HasDeficit"/>, and publishes
/// <see cref="AgronomicChillDeficitIntegrationEvent"/> when a deficit is detected.
/// </para>
/// </remarks>
public class AgronomicStatisticIngestionScheduler : BackgroundService
{
    private readonly IChillDeficitEvaluator _chillDeficitEvaluator;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<AgronomicStatisticsOptions> _options;
    private readonly ILogger<AgronomicStatisticIngestionScheduler> _logger;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgronomicStatisticIngestionScheduler"/> class.
    /// </summary>
    /// <remarks>
    /// Hosted services are singletons, so scoped dependencies (ingestion service,
    /// repositories, <see cref="ChillRequirementResolver"/>, <see cref="IMediator"/>)
    /// cannot be constructor-injected; they are resolved per ingestion cycle
    /// through <see cref="IServiceScopeFactory"/> in <see cref="IngestOnceAsync"/>.
    /// </remarks>
    public AgronomicStatisticIngestionScheduler(
        IChillDeficitEvaluator chillDeficitEvaluator,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<AgronomicStatisticsOptions> options,
        ILogger<AgronomicStatisticIngestionScheduler> logger,
        IClock clock)
    {
        _chillDeficitEvaluator = chillDeficitEvaluator;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;
        _logger = logger;
        _clock = clock;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Agronomic statistic ingestion scheduled with cron '{Cron}'. " +
            "Firing every 24h from app start (ScheduledIngestionEnabled={Enabled}).",
            _options.Value.IngestionCron,
            _options.Value.ScheduledIngestionEnabled);

        if (!_options.Value.ScheduledIngestionEnabled)
        {
            _logger.LogInformation("Scheduled ingestion is disabled. Scheduler will not run.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        do
        {
            try
            {
                await IngestOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during agronomic statistic ingestion cycle.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task IngestOnceAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IAgronomicStatisticIngestionService>();
        var plotRepository = scope.ServiceProvider.GetRequiredService<IPlotRepository>();
        var statisticRepository = scope.ServiceProvider.GetRequiredService<IAgronomicStatisticRepository>();
        var chillRequirementResolver = scope.ServiceProvider.GetRequiredService<ChillRequirementResolver>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // 1. Run ingestion
        var report = await ingestionService.IngestAllActivePlotsAsync(ct);
        _logger.LogInformation(
            "Agronomic statistic ingestion completed. Ingested={Ingested}, Skipped={Skipped}.",
            report.Ingested,
            report.Skipped);

        // 2. Run chill deficit producer (D14)
        var plots = (await plotRepository.ListAsync(ct))
            .Where(p => p.IsActive)
            .ToList();

        foreach (var plot in plots)
        {
            var latest = await statisticRepository.FindLatestByPlotIdAsync(plot.Id, ct);
            if (latest is null) continue;

            var requirement = chillRequirementResolver.ResolveFor(plot);

            if (_chillDeficitEvaluator.HasDeficit(requirement, (decimal)latest.ChillPortions))
            {
                await mediator.PublishAsync(
                    new AgronomicChillDeficitIntegrationEvent(
                        plot.Id,
                        (decimal)latest.ChillPortions,
                        (decimal)requirement.Portions.Value,
                        0m, // temperatureAnomaly placeholder
                        new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero)),
                    ct);

                _logger.LogInformation(
                    "Published AgronomicChillDeficitIntegrationEvent for PlotId={PlotId}.",
                    plot.Id);
            }
        }

        // 3. Run hydric stress producer (1.16.2 — D17: scoped via IServiceScopeFactory)
        var producer = scope.ServiceProvider.GetRequiredService<IHydricStressDetectedIntegrationEventProducer>();
        await producer.ProduceHydricStressEventsAsync(ct);
    }
}