using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Service that assembles the specialist segment dashboard read model.
/// </summary>
public interface ISpecialistDashboardQueryService
{
    Task<SpecialistDashboardResource> Handle(
        GetSpecialistDashboardQuery query,
        CancellationToken cancellationToken = default);
}
