namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Acl;

public record AlertCardSummary(string Severity, string ProblemLabel);

public record AlertMatchContext(long PlotId, string ThreatType);

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

    Task<AlertCardSummary?> GetAlertCardSummaryAsync(long alertId, CancellationToken cancellationToken = default);

    Task<AlertMatchContext?> GetAlertMatchContextAsync(long alertId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Counts the active alerts (status <c>ACTIVE</c>) raised on each of the given plots.
    /// </summary>
    /// <param name="plotIds">The plot identifiers to look up.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A map of plot id to active alert count. Plots with no active alerts are omitted.</returns>
    Task<IReadOnlyDictionary<long, int>> CountActiveAlertsByPlotIdsAsync(IEnumerable<long> plotIds, CancellationToken cancellationToken = default);
}
