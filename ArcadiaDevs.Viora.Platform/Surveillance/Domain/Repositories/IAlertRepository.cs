using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

/// <summary>
/// Repository interface for managing <see cref="Alert"/> aggregates.
/// </summary>
public interface IAlertRepository : IBaseRepository<Alert>
{
    Task<IEnumerable<Alert>> FindByPlotIdInOrderByCreatedAtDescAsync(IEnumerable<long> plotIds, int limit, CancellationToken cancellationToken = default);
}
