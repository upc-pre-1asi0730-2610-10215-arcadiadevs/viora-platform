using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

/// <summary>
/// Repository interface for managing <see cref="Alert"/> aggregates.
/// </summary>
public interface IAlertRepository : IBaseRepository<Alert>
{
    /// <summary>
    /// Finds an alert by its <see cref="Alert.Id"/> (a <c>long</c>/bigint PK).
    /// </summary>
    /// <remarks>
    /// Distinct from the inherited <see cref="IBaseRepository{TEntity}.FindByIdAsync"/>,
    /// which takes an <c>int</c> and is unusable for Alert's bigint key — narrowing
    /// a <c>long</c> id down to <c>int</c> before calling it throws at runtime
    /// (EF's <c>DbSet.Find</c> validates the key CLR type against the model).
    /// </remarks>
    Task<Alert?> FindByIdAsync(long id, CancellationToken cancellationToken = default);

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
