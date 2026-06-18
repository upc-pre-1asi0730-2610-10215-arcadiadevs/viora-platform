using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.OutboundServices.Acl;

/// <summary>
/// Implementation of the external agronomic service for the anti-corruption layer.
/// </summary>
public class ExternalAgronomicService(
    IMonitoringSummaryQueryService monitoringSummaryQueryService,
    IPlotRepository plotRepository) : IExternalAgronomicService
{
    /// <inheritdoc/>
    public async Task<double?> FetchCurrentNdviByPlotIdAsync(long plotId, long reporterUserId, CancellationToken cancellationToken = default)
    {
        var query = new GetCurrentMonitoringSummaryQuery((int)reporterUserId); // Assuming reporterUserId is the user id for the query
        var result = await monitoringSummaryQueryService.Handle(query, cancellationToken);
        
        // As a simplification, if the result is successful, return its NDVI
        if (result is Result<MonitoringSummaryResource, Error>.Success success)
        {
            return (double)success.Value.AverageNdvi;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<IDictionary<long, PlotSummaryResource>> GetPlotsForUserAsMapAsync(long userId, CancellationToken cancellationToken = default)
    {
        var plots = await plotRepository.FindAllByOwnerUserIdAsync((int)userId, cancellationToken);
        var map = new Dictionary<long, PlotSummaryResource>();

        foreach (var plot in plots)
        {
            map[plot.Id] = new PlotSummaryResource(
                plot.PlotName,
                plot.AgroMonitoringCenter ?? "Unknown",
                (double)plot.AreaSize
            );
        }

        return map;
    }
}
