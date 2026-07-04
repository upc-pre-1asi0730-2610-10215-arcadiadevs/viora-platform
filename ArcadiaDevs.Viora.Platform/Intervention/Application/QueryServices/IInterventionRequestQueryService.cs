using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Service that handles read queries for <see cref="InterventionRequest" /> (REQ-IREQ-2).
/// </summary>
public interface IInterventionRequestQueryService
{
    Task<Result<InterventionRequest, Error>> Handle(
        GetInterventionRequestByIdQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InterventionRequest>> Handle(
        ListInterventionRequestsByGrowerQuery query,
        CancellationToken cancellationToken = default);
}
