using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.OutboundServices.Acl;

/// <summary>
/// Implementation of the external agronomic service for the anti-corruption layer.
/// </summary>
public class ExternalAgronomicService(IAgronomicContextFacade agronomicContextFacade) : IExternalAgronomicService
{
    /// <inheritdoc/>
    public async Task<double?> FetchCurrentNdviByPlotIdAsync(long plotId, long reporterUserId, CancellationToken cancellationToken = default)
    {
        return await agronomicContextFacade.FetchCurrentNdviByReporterAsync((int)reporterUserId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IDictionary<long, PlotSummaryResource>> GetPlotsForUserAsMapAsync(long userId, CancellationToken cancellationToken = default)
    {
        var summaries = await agronomicContextFacade.FetchPlotsByOwnerUserAsync((int)userId, cancellationToken);
        var map = new Dictionary<long, PlotSummaryResource>();

        foreach (var summary in summaries)
        {
            map[summary.Key] = new PlotSummaryResource(
                summary.Value.Name,
                summary.Value.AgroMonitoringCenter,
                summary.Value.AreaSize);
        }

        return map;
    }
}
