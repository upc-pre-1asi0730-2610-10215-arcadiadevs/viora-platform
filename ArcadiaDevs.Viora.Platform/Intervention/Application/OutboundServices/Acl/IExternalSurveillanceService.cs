namespace ArcadiaDevs.Viora.Platform.Intervention.Application.OutboundServices.Acl;

/// <summary>
/// Service contract for external surveillance operations.
/// </summary>
public interface IExternalSurveillanceService
{
    /// <summary>
    /// Checks whether an alert with the given identifier exists.
    /// </summary>
    /// <param name="alertId">The alert identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the alert exists; otherwise, <c>false</c>.</returns>
    Task<bool> AlertExistsAsync(long alertId, CancellationToken cancellationToken = default);
}
