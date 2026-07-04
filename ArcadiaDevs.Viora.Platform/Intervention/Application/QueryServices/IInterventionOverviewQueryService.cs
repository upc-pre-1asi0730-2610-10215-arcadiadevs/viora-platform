using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Service that handles the composed producer-facing overview read
///     query (REQ-OV-1, REQ-OV-2).
/// </summary>
public interface IInterventionOverviewQueryService
{
    Task<IReadOnlyList<InterventionOverviewItem>> Handle(
        GetInterventionOverviewByGrowerIdQuery query,
        CancellationToken cancellationToken = default);
}
