namespace ArcadiaDevs.Viora.Platform.Intervention.Application.OutboundServices.Acl;

/// <summary>
///     Service contract for external agronomic operations consumed by the
///     Intervention BC (subset of Agronomic's full facade — REQ-IREQ-1
///     plotId validation). Distinct namespace from Surveillance's
///     same-named adapter (design's Cross-BC ACL Wiring table, obs #267).
/// </summary>
public interface IExternalAgronomicService
{
    /// <summary>
    ///     Resolves the display name of a plot.
    /// </summary>
    /// <param name="plotId">The plot identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The plot name if it exists; otherwise, null.</returns>
    Task<string?> GetPlotNameAsync(long plotId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether a plot with the given identifier exists.
    /// </summary>
    /// <param name="plotId">The plot identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the plot exists; otherwise, <c>false</c>.</returns>
    Task<bool> PlotExistsAsync(long plotId, CancellationToken cancellationToken = default);
}
