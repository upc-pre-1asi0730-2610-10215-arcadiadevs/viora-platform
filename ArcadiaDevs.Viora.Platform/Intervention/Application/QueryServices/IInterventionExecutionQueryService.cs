using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Service that handles read queries for <see cref="InterventionExecution" />.
/// </summary>
public interface IInterventionExecutionQueryService
{
    Task<InterventionExecution?> Handle(
        GetInterventionExecutionByIdQuery query,
        CancellationToken cancellationToken = default);

    Task<InterventionExecution?> Handle(
        GetInterventionExecutionByTreatmentPrescriptionIdQuery query,
        CancellationToken cancellationToken = default);
}
