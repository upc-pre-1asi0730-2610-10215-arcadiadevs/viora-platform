using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Handles <see cref="InterventionExecution" /> read queries.
/// </summary>
public class InterventionExecutionQueryService(IInterventionExecutionRepository interventionExecutionRepository)
    : IInterventionExecutionQueryService
{
    public async Task<InterventionExecution?> Handle(
        GetInterventionExecutionByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await interventionExecutionRepository.FindByIdAsync(query.Id, cancellationToken);
    }

    public async Task<InterventionExecution?> Handle(
        GetInterventionExecutionByTreatmentPrescriptionIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await interventionExecutionRepository.FindByTreatmentPrescriptionIdAsync(
            query.TreatmentPrescriptionId, cancellationToken);
    }
}
