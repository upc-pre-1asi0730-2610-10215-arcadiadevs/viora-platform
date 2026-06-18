using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;

/// <summary>
///     Repository for managing AgroMonitoringPlotIntegration persistent cache.
/// </summary>
public interface IAgroMonitoringPlotIntegrationRepository : IBaseRepository<AgroMonitoringPlotIntegration>
{
    Task<AgroMonitoringPlotIntegration?> FindByPlotIdAsync(int plotId, CancellationToken cancellationToken = default);
}
