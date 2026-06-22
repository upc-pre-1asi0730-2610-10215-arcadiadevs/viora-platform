namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;

/// <summary>
///     Facade for the agronomic context.
/// </summary>
public interface IAgronomicContextFacade
{
    /// <summary>
    ///     Fetches the current average NDVI for the user identified as the reporter.
    /// </summary>
    /// <param name="reporterUserId">The reporter user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current NDVI value if available; otherwise, null.</returns>
    Task<double?> FetchCurrentNdviByReporterAsync(int reporterUserId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Fetches a map of plot summaries keyed by plot identifier for all plots owned by a user.
    /// </summary>
    /// <param name="ownerUserId">The owner user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only dictionary of plot summaries keyed by plot identifier.</returns>
    Task<IReadOnlyDictionary<long, AgronomicPlotSummary>> FetchPlotsByOwnerUserAsync(int ownerUserId, CancellationToken cancellationToken = default);
}
