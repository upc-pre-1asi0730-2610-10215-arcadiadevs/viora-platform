using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Handles <see cref="InterventionOutcome" /> read queries.
/// </summary>
public class InterventionOutcomeQueryService(IInterventionOutcomeRepository interventionOutcomeRepository)
    : IInterventionOutcomeQueryService
{
    public async Task<InterventionOutcome?> Handle(
        GetInterventionOutcomeByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await interventionOutcomeRepository.FindByIdAsync(query.Id, cancellationToken);
    }

    public async Task<InterventionOutcome?> Handle(
        GetInterventionOutcomeByInterventionExecutionIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await interventionOutcomeRepository.FindByInterventionExecutionIdAsync(
            query.InterventionExecutionId, cancellationToken);
    }
}
