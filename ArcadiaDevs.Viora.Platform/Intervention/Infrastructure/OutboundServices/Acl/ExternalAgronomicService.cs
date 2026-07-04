using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Intervention.Application.OutboundServices.Acl;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.OutboundServices.Acl;

/// <summary>
///     Implementation of the external agronomic service for the
///     Intervention BC's anti-corruption layer.
/// </summary>
public class ExternalAgronomicService(IAgronomicContextFacade agronomicContextFacade) : IExternalAgronomicService
{
    /// <inheritdoc/>
    public async Task<string?> GetPlotNameAsync(long plotId, CancellationToken cancellationToken = default)
    {
        return await agronomicContextFacade.GetPlotNameAsync(plotId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> PlotExistsAsync(long plotId, CancellationToken cancellationToken = default)
    {
        var plotName = await agronomicContextFacade.GetPlotNameAsync(plotId, cancellationToken);
        return plotName is not null;
    }
}
