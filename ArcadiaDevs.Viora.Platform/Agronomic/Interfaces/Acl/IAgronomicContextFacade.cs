namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;

public record PlotCardSummary(string Name, string? Location, string? CropType, decimal AreaHectares);

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

    /// <summary>
    ///     Resolves the display name of a plot by its identifier.
    /// </summary>
    /// <param name="plotId">The plot identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The plot name if the plot exists; otherwise, null.</returns>
    Task<string?> GetPlotNameAsync(long plotId, CancellationToken cancellationToken = default);

    Task<PlotCardSummary?> GetPlotCardSummaryAsync(long plotId, CancellationToken cancellationToken = default);

    Task<int> CountPlotsByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<double?> DistanceKmFromPlotCentroidAsync(long plotId, double lat, double lng, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Finds plots whose centroid lies within <paramref name="radiusKm"/> of the reference
    ///     plot's centroid, excluding the reference plot itself. The result carries only
    ///     anonymized identifiers and distances (no owner or naming data), so it is safe for
    ///     community-risk diffusion across contexts.
    /// </summary>
    /// <param name="referencePlotId">The plot at the center of the search radius.</param>
    /// <param name="radiusKm">The search radius in kilometers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The neighbor plots within the radius, each with its distance.</returns>
    Task<IReadOnlyList<NeighborPlot>> FindNeighborPlotsWithinRadiusAsync(
        long referencePlotId,
        double radiusKm,
        CancellationToken cancellationToken = default);
}
