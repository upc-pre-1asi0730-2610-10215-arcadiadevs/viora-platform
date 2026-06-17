using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

/// <summary>
/// Repository interface for managing <see cref="PestSightingReport"/> aggregates.
/// </summary>
public interface IPestSightingReportRepository : IBaseRepository<PestSightingReport>
{
}
