using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;

/// <summary>
///     Command service that ingests real agronomic statistic snapshots.
/// </summary>
public class AgronomicStatisticIngestionService : IAgronomicStatisticIngestionService
{
    private readonly IPlotRepository _plotRepository;
    private readonly IAgronomicStatisticRepository _statisticRepository;
    private readonly IAgroMonitoringImageryService _agroMonitoringImageryService;
    private readonly IWeatherDataService _weatherDataService;
    private readonly ChillAccumulationCalculator _chillAccumulationCalculator;
    private readonly IAgroMonitoringPlotIntegrationRepository _integrationRepository;
    private readonly ArcadiaDevs.Viora.Platform.Shared.Domain.IClock _clock;

    public AgronomicStatisticIngestionService(
        IPlotRepository plotRepository,
        IAgronomicStatisticRepository statisticRepository,
        IAgroMonitoringImageryService agroMonitoringImageryService,
        IWeatherDataService weatherDataService,
        ChillAccumulationCalculator chillAccumulationCalculator,
        IAgroMonitoringPlotIntegrationRepository integrationRepository,
        ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock)
    {
        _plotRepository = plotRepository;
        _statisticRepository = statisticRepository;
        _agroMonitoringImageryService = agroMonitoringImageryService;
        _weatherDataService = weatherDataService;
        _chillAccumulationCalculator = chillAccumulationCalculator;
        _integrationRepository = integrationRepository;
        _clock = clock;
    }

    public async Task<Result<AgronomicStatisticsIngestionReport, Error>> Handle(
        IngestAgronomicStatisticsCommand command,
        CancellationToken cancellationToken = default)
    {
        var plots = (await _plotRepository.FindAllByOwnerUserIdAsync((int)command.UserId, cancellationToken)).ToList();
        var report = await IngestAsync(plots, command.TargetDate, cancellationToken);
        return new Result<AgronomicStatisticsIngestionReport, Error>.Success(report);
    }

    public async Task<AgronomicStatisticsIngestionReport> IngestAllActivePlotsAsync(CancellationToken cancellationToken = default)
    {
        var plots = (await _plotRepository.ListAsync(cancellationToken)).ToList();
        return await IngestAsync(plots, new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero), cancellationToken);
    }

    public async Task<bool> IngestForPlotAsync(Plot plot, CancellationToken cancellationToken = default)
    {
        if (plot == null || plot.Id <= 0 || plot.IsDeleted)
        {
            return false;
        }

        return await IngestPlotAsync(plot, new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero), cancellationToken);
    }

    private async Task<AgronomicStatisticsIngestionReport> IngestAsync(
        List<Plot> plots, 
        DateTimeOffset today, 
        CancellationToken cancellationToken)
    {
        var report = AgronomicStatisticsIngestionReport.Empty();
        foreach (var plot in plots)
        {
            if (plot.IsDeleted)
            {
                continue;
            }

            bool ingested = await IngestPlotAsync(plot, today, cancellationToken);
            report = ingested ? report.WithIngested() : report.WithSkipped();
        }

        return report;
    }

    private async Task<bool> IngestPlotAsync(Plot plot, DateTimeOffset today, CancellationToken cancellationToken)
    {
        var existing = await _statisticRepository.FindByPlotIdAndDateAsync(plot.Id, today, cancellationToken);
        if (existing != null)
        {
            return false;
        }

        await _agroMonitoringImageryService.FindCurrentImageryAsync(plot, cancellationToken);
        var integration = await _integrationRepository.FindByPlotIdAsync(plot.Id, cancellationToken);
        if (integration == null || !integration.NdviMean.HasValue)
        {
            return false;
        }

        var baseStatistic = await _statisticRepository.FindLatestByPlotIdAsync(plot.Id, cancellationToken);
        var incomingState = baseStatistic?.ChillModelState ?? ChillModelState.Empty();

        var history = await _weatherDataService.GetWeatherHistoryAsync(plot, new DateRange(today, today), cancellationToken);
        
        ChillAccumulation chillAccumulation;
        if (history != null)
        {
            chillAccumulation = _chillAccumulationCalculator.Accumulate(history, incomingState);
        }
        else
        {
            chillAccumulation = new ChillAccumulation(0.0, 0.0, incomingState);
        }

        double accumulatedChillHours = Math.Max(0.0, (baseStatistic?.ChillHours ?? 0.0) + chillAccumulation.ChillHours);
        double accumulatedChillPortions = Math.Max(0.0, (baseStatistic?.ChillPortions ?? 0.0) + chillAccumulation.ChillPortions);

        // AGRO-002: route through the factory so private setters + validation
        // are the only way to instantiate an AgronomicStatistic. On validation
        // failure, skip the plot rather than poison the ingestion report.
        var createResult = AgronomicStatistic.Create(
            userId: plot.OwnerUserId,
            plotId: plot.Id,
            measurementDate: today,
            ndviValue: integration.NdviMean.Value,
            chillPortions: accumulatedChillPortions,
            chillHours: accumulatedChillHours,
            chillModelState: chillAccumulation.NewState);
        if (createResult is Result<AgronomicStatistic, Error>.Failure)
        {
            return false;
        }

        var newStatistic = ((Result<AgronomicStatistic, Error>.Success)createResult).Value;
        await _statisticRepository.AddAsync(newStatistic, cancellationToken);
        return true;
    }
}
