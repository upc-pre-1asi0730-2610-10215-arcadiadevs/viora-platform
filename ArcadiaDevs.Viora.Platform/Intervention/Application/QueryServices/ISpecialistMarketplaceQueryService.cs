using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Query service for the specialist Intervention Marketplace read model.
/// </summary>
public interface ISpecialistMarketplaceQueryService
{
    Task<SpecialistMarketplaceResource> Handle(
        GetSpecialistMarketplaceQuery query,
        CancellationToken cancellationToken = default);
}
