using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Acl;

/// <summary>
///     Facade for the agronomic context.
/// </summary>
/// <param name="monitoringSummaryQueryService">
///     The monitoring summary query service.
/// </param>
/// <param name="plotRepository">
///     The plot repository.
/// </param>
public class AgronomicContextFacade(
    IMonitoringSummaryQueryService monitoringSummaryQueryService,
    IPlotRepository plotRepository) : IAgronomicContextFacade
{
    // inheritedDoc
    public async Task<double?> FetchCurrentNdviByReporterAsync(int reporterUserId, CancellationToken cancellationToken = default)
    {
        var query = new GetCurrentMonitoringSummaryQuery(reporterUserId);
        var result = await monitoringSummaryQueryService.Handle(query, cancellationToken);

        if (result is Result<MonitoringSummaryResource, Error>.Success success)
        {
            return (double)success.Value.AverageNdvi;
        }

        return null;
    }

    // inheritedDoc
    public async Task<IReadOnlyDictionary<long, AgronomicPlotSummary>> FetchPlotsByOwnerUserAsync(int ownerUserId, CancellationToken cancellationToken = default)
    {
        var plots = await plotRepository.FindAllByOwnerUserIdAsync(ownerUserId, cancellationToken);
        var map = new Dictionary<long, AgronomicPlotSummary>();

        foreach (var plot in plots)
        {
            map[plot.Id] = new AgronomicPlotSummary(
                plot.PlotName,
                plot.AgroMonitoringCenter ?? "Unknown",
                (double)plot.AreaSize);
        }

        return map;
    }
}
