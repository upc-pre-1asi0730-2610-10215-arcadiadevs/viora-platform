using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Query service for the signed-in specialist's own cases read model
///     (My Requests + Field Inspection).
/// </summary>
public interface ISpecialistCasesQueryService
{
    Task<SpecialistCasesResource> Handle(
        GetSpecialistCasesQuery query,
        CancellationToken cancellationToken = default);
}
