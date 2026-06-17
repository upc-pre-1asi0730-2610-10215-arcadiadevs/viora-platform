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
}
