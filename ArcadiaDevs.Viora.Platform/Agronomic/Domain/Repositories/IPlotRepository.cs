using System.Collections.Generic;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;

/// <summary>
///     Repository interface for Plot aggregate operations.
/// </summary>
/// <remarks>
///     Extends the base repository with plot-specific queries.
/// </remarks>
public interface IPlotRepository : IBaseRepository<Plot>
{
    /// <summary>
    ///     Finds a plot by its ID and owner user ID.
    /// </summary>
    /// <param name="plotId">The plot identifier.</param>
    /// <param name="ownerUserId">The owner user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The plot if found and owned by the specified user; otherwise, null.</returns>
    Task<Plot?> FindByIdAndOwnerUserIdAsync(
        int plotId,
        int ownerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a plot exists by its ID and owner user ID.
    /// </summary>
    /// <param name="plotId">The plot identifier.</param>
    /// <param name="ownerUserId">The owner user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the plot exists and is owned by the specified user; otherwise, false.</returns>
    Task<bool> ExistsByIdAndOwnerUserIdAsync(
        int plotId,
        int ownerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Finds all plots owned by a specific user.
    /// </summary>
    /// <param name="ownerUserId">The owner user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of plots owned by the specified user.</returns>
    Task<IEnumerable<Plot>> FindAllByOwnerUserIdAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a plot with the given name already exists for the user.
    /// </summary>
    Task<bool> ExistsByNameAndOwnerUserIdAsync(
        string plotName,
        int ownerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a plot with the given name already exists for the user, excluding a specific ID.
    /// </summary>
    Task<bool> ExistsByNameAndOwnerUserIdAndIdIsNotAsync(
        string plotName,
        int ownerUserId,
        int excludePlotId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if the plot has related operational records (e.g., IoT devices, nutrition plans).
    /// </summary>
    Task<bool> HasRelatedOperationalRecordsAsync(
        int plotId,
        CancellationToken cancellationToken = default);
}
