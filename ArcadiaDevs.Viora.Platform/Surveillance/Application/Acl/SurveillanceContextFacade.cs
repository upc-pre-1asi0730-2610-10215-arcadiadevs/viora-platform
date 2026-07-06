using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Acl;

using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Acl;

/// <summary>
///     Facade for the surveillance context.
/// </summary>
/// <param name="alertRepository">
///     The alert repository.
/// </param>
/// <param name="logger">
///     The logger.
/// </param>
public class SurveillanceContextFacade(
    IAlertRepository alertRepository,
    ILogger<SurveillanceContextFacade> logger) : ISurveillanceContextFacade
{
    // inheritedDoc
    public async Task<bool> AlertExistsAsync(long alertId, CancellationToken cancellationToken = default)
    {
        var alert = await alertRepository.FindByIdAsync((int)alertId, cancellationToken);

        if (alert is null)
        {
            logger.LogWarning("Alert {AlertId} was not found while resolving a cross-context reference.", alertId);
            return false;
        }

        return true;
    }

    // inheritedDoc
    public async Task<AlertCardSummary?> GetAlertCardSummaryAsync(long alertId, CancellationToken cancellationToken = default)
    {
        var alert = await alertRepository.FindByIdAsync((int)alertId, cancellationToken);

        if (alert is null)
        {
            logger.LogWarning("Alert {AlertId} was not found while resolving a cross-context reference.", alertId);
            return null;
        }

        return new AlertCardSummary(alert.Severity.ToString(), alert.Title);
    }

    // inheritedDoc
    public async Task<AlertMatchContext?> GetAlertMatchContextAsync(long alertId, CancellationToken cancellationToken = default)
    {
        var alert = await alertRepository.FindByIdAsync((int)alertId, cancellationToken);

        if (alert is null)
        {
            logger.LogWarning("Alert {AlertId} was not found while resolving a cross-context reference.", alertId);
            return null;
        }

        return new AlertMatchContext(alert.PlotId.Value, alert.Type.ToString());
    }
}
