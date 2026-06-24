using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.OutboundServices.Acl;

/// <summary>
/// Service contract for external agronomic operations.
/// </summary>
public interface IExternalAgronomicService
{
    /// <summary>
    /// Fetches the current NDVI index for a plot.
    /// </summary>
    /// <param name="plotId">The plot ID.</param>
    /// <param name="reporterUserId">The user ID requesting the data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The NDVI value or null if not found.</returns>
    Task<double?> FetchCurrentNdviByPlotIdAsync(long plotId, long reporterUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a map of PlotId -> PlotSummaryResource for all plots owned by a user.
    /// This map is useful for efficiently assembling Alert resources.
    /// </summary>
    Task<IDictionary<long, PlotSummaryResource>> GetPlotsForUserAsMapAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the display name of a plot.
    /// </summary>
    /// <param name="plotId">The plot identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The plot name if it exists; otherwise, null.</returns>
    Task<string?> GetPlotNameAsync(long plotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds anonymized neighbor plots within a radius of the reference plot.
    /// </summary>
    /// <param name="referencePlotId">The plot at the center of the search radius.</param>
    /// <param name="radiusKm">The search radius in kilometers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The neighbor plots (id + distance) within the radius.</returns>
    Task<IReadOnlyList<NeighborPlot>> FindNeighborPlotsWithinRadiusAsync(
        long referencePlotId,
        double radiusKm,
        CancellationToken cancellationToken = default);
}
