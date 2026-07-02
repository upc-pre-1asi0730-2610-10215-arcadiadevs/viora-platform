using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

/// <summary>
/// Repository interface for managing <see cref="Alert"/> aggregates.
/// </summary>
public interface IAlertRepository : IBaseRepository<Alert>
{
    Task<IEnumerable<Alert>> FindByPlotIdInOrderByCreatedAtDescAsync(IEnumerable<long> plotIds, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the active alerts (status <c>ACTIVE</c>) raised on any of the given plots.
    /// </summary>
    /// <param name="plotIds">The plot identifiers to look up.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The active alerts for those plots.</returns>
    Task<IEnumerable<Alert>> FindActiveByPlotIdInAsync(IEnumerable<long> plotIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the alert linked to a specific pest sighting report.
    /// </summary>
    Task<Alert?> FindByLinkedReportIdAsync(long reportId, CancellationToken cancellationToken = default);
}
