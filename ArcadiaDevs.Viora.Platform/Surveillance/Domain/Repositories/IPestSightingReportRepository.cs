using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

/// <summary>
/// Repository interface for managing <see cref="PestSightingReport"/> aggregates.
/// </summary>
public interface IPestSightingReportRepository : IBaseRepository<PestSightingReport>
{
    /// <summary>
    /// Finds every report submitted by the given reporter, ordered newest first.
    /// </summary>
    /// <param name="reporterUserId">The id of the reporting user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reporter's reports, newest first (empty when none exist).</returns>
    Task<IEnumerable<PestSightingReport>> FindByReporterUserIdAsync(long reporterUserId, CancellationToken cancellationToken = default);
}
