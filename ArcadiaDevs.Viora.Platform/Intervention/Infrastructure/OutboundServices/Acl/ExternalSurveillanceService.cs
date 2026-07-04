using ArcadiaDevs.Viora.Platform.Intervention.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Acl;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.OutboundServices.Acl;

/// <summary>
/// Implementation of the external surveillance service for the anti-corruption layer.
/// </summary>
public class ExternalSurveillanceService(ISurveillanceContextFacade surveillanceContextFacade) : IExternalSurveillanceService
{
    /// <inheritdoc/>
    public async Task<bool> AlertExistsAsync(long alertId, CancellationToken cancellationToken = default)
    {
        return await surveillanceContextFacade.AlertExistsAsync(alertId, cancellationToken);
    }
}
