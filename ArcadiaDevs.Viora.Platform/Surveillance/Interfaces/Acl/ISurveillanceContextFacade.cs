namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Acl;

/// <summary>
///     Facade for the surveillance context.
/// </summary>
public interface ISurveillanceContextFacade
{
    /// <summary>
    ///     Checks whether an alert with the given identifier exists.
    /// </summary>
    /// <param name="alertId">The alert identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> if the alert exists; otherwise, <c>false</c>.</returns>
    Task<bool> AlertExistsAsync(long alertId, CancellationToken cancellationToken = default);
}
